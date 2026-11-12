# /// script
# requires-python = ">=3.11"
# dependencies = ["numpy", "scipy", "soundfile"]
# ///
"""Cut bird sounds from Xeno-canto recordings and keep CREDITS.md in sync.

uv run tools/audio.py build [NAME ...] [--sources DIR]   render sounds from tools/audio.toml, rewrite CREDITS.md (recording details are fetched from xeno-canto.org)
uv run tools/audio.py analyze FILE [--start S] [--end S]  list whistle notes (to find cut points)

Source recordings are found as DIR/XC<id> *.* (default DIR: ~/Downloads).
Sound keys and defaults: see DEFAULTS and tools/audio.toml.
"""

import argparse
import re
import tomllib
import urllib.request
from pathlib import Path

import numpy as np
import soundfile as sf
from scipy import signal as dsp

SAMPLE_RATE = 44100
ROOT = Path(__file__).resolve().parent.parent
MANIFEST = Path(__file__).resolve().parent / "audio.toml"
OUT_DIR = ROOT / "Birding/assets/audio"
CREDITS = OUT_DIR / "CREDITS.md"
DEFAULTS = {
    "lowcut": 1800.0,
    "highcut": 12000.0,
    "gain": 0.25,
    "fade": 0.01,
    "pad": 0.05,
    "noise_strength": 1.0,
}
REQUIRED = {"xc", "segments"}
OPTIONAL = {"noise"}
NOISE_FRAME = 2048
NOISE_FLOOR = 0.1
BAND_HZ = (1500.0, 8000.0)
FRAME = 1024
HOP = 128

XC_URL = "https://xeno-canto.org/{}"
XC_TITLE = re.compile(r"og:title' content='XC\d+ - ([^-]+?) - ")
XC_RECORDIST = re.compile(r"jp-xc-recordist'>([^<]+)<")
XC_LICENSE = re.compile(r"href='(https://creativecommons\.org/(?:licenses/([\w-]+)/([\d.]+)|publicdomain/zero/([\d.]+))/)'")


def fetch_recording(xc: int) -> dict[str, str]:
    """Species, recordist and license of a recording, read from its xeno-canto page."""
    try:
        html = urllib.request.urlopen(XC_URL.format(xc), timeout=20).read().decode()
    except OSError as error:
        raise SystemExit(f"XC{xc}: cannot fetch recording page ({error})")
    title, recordist, lic = XC_TITLE.search(html), XC_RECORDIST.search(html), XC_LICENSE.search(html)
    if not (title and recordist and lic):
        raise SystemExit(f"XC{xc}: unexpected page layout, cannot read credit details")
    url, kind, version, zero = lic.groups()
    name = f"CC {kind.upper()} {version}" if kind else f"CC0 {zero}"
    return {"species": title.group(1), "recordist": recordist.group(1), "license": f"[{name}]({url})"}


def find_source(sources: Path, xc: int) -> Path:
    matches = sorted(sources.expanduser().glob(f"XC{xc} *.*"))
    if not matches:
        raise SystemExit(f"no recording 'XC{xc} *.*' in {sources}")
    return matches[0]


def load(path: Path) -> np.ndarray:
    data, rate = sf.read(path, always_2d=True)
    mono = data.mean(axis=1)
    if rate != SAMPLE_RATE:
        mono = np.interp(np.arange(0, len(mono), rate / SAMPLE_RATE), np.arange(len(mono)), mono)
    return mono


def cut(signal: np.ndarray, start: float, end: float | None) -> np.ndarray:
    stop = None if end is None else int(end * SAMPLE_RATE)
    return signal[int(start * SAMPLE_RATE) : stop]


def denoise(signal: np.ndarray, noise: np.ndarray, strength: float) -> np.ndarray:
    """Spectral subtraction using the mean spectrum of a noise-only stretch; keeps the original phase."""
    _, _, profile = dsp.stft(noise, SAMPLE_RATE, nperseg=NOISE_FRAME)
    profile = np.abs(profile).mean(axis=1, keepdims=True)
    _, _, spectrum = dsp.stft(signal, SAMPLE_RATE, nperseg=NOISE_FRAME)
    mag = np.abs(spectrum)
    kept = np.maximum(mag - strength * profile, NOISE_FLOOR * mag)
    _, out = dsp.istft(spectrum * kept / (mag + 1e-12), SAMPLE_RATE, nperseg=NOISE_FRAME)
    return out[: len(signal)]


def render(source: np.ndarray, config: dict) -> np.ndarray:
    if "noise" in config:
        source = denoise(source, cut(source, *config["noise"]), config["noise_strength"])
    sos = dsp.butter(4, [config["lowcut"], config["highcut"]], btype="bandpass", fs=SAMPLE_RATE, output="sos")
    filtered = dsp.sosfiltfilt(sos, source)
    fade = int(config["fade"] * SAMPLE_RATE)
    parts = []
    for start, end in config["segments"]:
        part = cut(filtered, start, end).copy()
        part[:fade] *= np.linspace(0, 1, fade)
        part[-fade:] *= np.linspace(1, 0, fade)
        parts.append(part)
    pad = np.zeros(int(config["pad"] * SAMPLE_RATE))
    padded = np.concatenate([pad, *parts, pad])
    return config["gain"] * padded / (np.abs(padded).max() or 1.0)


def resolve(name: str, sound: dict, shared: dict) -> dict:
    config = {**DEFAULTS, **shared, **sound}
    missing = REQUIRED - config.keys()
    if missing:
        raise SystemExit(f"{name}: missing {', '.join(sorted(missing))}")
    unknown = config.keys() - REQUIRED - OPTIONAL - DEFAULTS.keys()
    if unknown:
        raise SystemExit(f"{name}: unknown key(s) {', '.join(sorted(unknown))}")
    return config


def credits(sounds: dict[str, dict]) -> str:
    rows = []
    for name, config in sounds.items():
        info = fetch_recording(config["xc"])
        rows.append(
            f"| {name}.ogg | [XC{config['xc']}]({XC_URL.format(config['xc'])}) {info['species']} | {info['recordist']} | {info['license']} |"
        )
    return "\n".join(
        [
            "# Audio credits",
            "",
            "Cut from [Xeno-canto](https://xeno-canto.org) recordings (trimmed, filtered, normalized, converted to OGG).",
            "Each file is shared under its recording's license. Generated by `tools/audio.py build`.",
            "",
            "| File | Recording | Recordist | License |",
            "|---|---|---|---|",
            *rows,
            "",
        ]
    )


def build(names: list[str], sources: Path) -> None:
    manifest = tomllib.loads(MANIFEST.read_text())
    shared = manifest.pop("defaults", {})
    sounds = {name: resolve(name, sound, shared) for name, sound in manifest.items()}
    unknown = set(names) - sounds.keys()
    if unknown:
        raise SystemExit(f"unknown sound(s): {', '.join(sorted(unknown))}")
    credits_text = credits(sounds)
    OUT_DIR.mkdir(parents=True, exist_ok=True)
    for name in names or sounds:
        config = sounds[name]
        out = render(load(find_source(sources, config["xc"])), config)
        sf.write(OUT_DIR / f"{name}.ogg", out, SAMPLE_RATE)
        print(f"wrote {name}.ogg ({len(out) / SAMPLE_RATE:.2f}s)")
    CREDITS.write_text(credits_text)
    print(f"wrote {CREDITS.name}")


def track(signal: np.ndarray) -> tuple[np.ndarray, np.ndarray]:
    """Per-frame dominant frequency (Hz) and amplitude in the whistle band."""
    window = np.hanning(FRAME)
    freqs = np.fft.rfftfreq(FRAME, 1 / SAMPLE_RATE)
    band = (freqs >= BAND_HZ[0]) & (freqs <= BAND_HZ[1])
    band_freqs = freqs[band]
    starts = range(0, len(signal) - FRAME, HOP)
    pitch = np.zeros(len(starts))
    amp = np.zeros(len(starts))
    for i, s in enumerate(starts):
        mag = np.abs(np.fft.rfft(signal[s : s + FRAME] * window))[band]
        k = int(mag.argmax())
        offset = 0.0
        if 0 < k < len(mag) - 1:
            a, b, c = np.log(mag[k - 1 : k + 2] + 1e-12)
            offset = 0.5 * (a - c) / (a - 2 * b + c)
        pitch[i] = band_freqs[k] + offset * (freqs[1] - freqs[0])
        amp[i] = mag[k]
    return pitch, amp


def notes(signal: np.ndarray) -> list[tuple[float, float, float, float]]:
    """Whistle notes as (onset s, duration s, start Hz, end Hz)."""
    pitch, amp = track(signal)
    on = amp > 0.2 * amp.max()
    hop = HOP / SAMPLE_RATE
    out, begin = [], None
    for i in range(len(pitch) + 1):
        split = i == len(pitch) or not on[i] or (begin is not None and abs(pitch[i] - pitch[i - 1]) > 500)
        if begin is not None and split:
            if i - begin >= 8:
                seg = pitch[begin:i]
                out.append((begin * hop, (i - begin) * hop, float(np.median(seg[:4])), float(np.median(seg[-4:]))))
            begin = None
        if begin is None and i < len(pitch) and on[i]:
            begin = i
    return out


def analyze(path: Path, start: float, end: float | None) -> None:
    prev = None
    for onset, dur, f0, f1 in notes(cut(load(path), start, end)):
        gap = "" if prev is None else f"  +{onset - prev:.2f}s"
        print(f"{onset:6.2f}s  dur {dur:.2f}s  {f0:5.0f} -> {f1:5.0f} Hz{gap}")
        prev = onset


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    sub = parser.add_subparsers(dest="mode", required=True)
    b = sub.add_parser("build")
    b.add_argument("names", nargs="*")
    b.add_argument("--sources", type=Path, default=Path("~/Downloads"))
    a = sub.add_parser("analyze")
    a.add_argument("file", type=Path)
    a.add_argument("--start", type=float, default=0.0)
    a.add_argument("--end", type=float)
    args = parser.parse_args()
    if args.mode == "build":
        build(args.names, args.sources)
    else:
        analyze(args.file, args.start, args.end)


if __name__ == "__main__":
    main()
