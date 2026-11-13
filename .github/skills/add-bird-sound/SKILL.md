---
name: add-bird-sound
description: Add a bird sound to the Birding mod from a Xeno-canto recording. Use when asked to add, cut, wire or tune a bird call, song, alarm or other sound given an XC number, including when and how often the bird should emit it.
---
# Add a bird sound from Xeno-canto

Turns an XC recording into a shipped cue and wires it to a species. Two parts: **cut** the audio with `tools/audio.py`, then **wire** it in the species JSON. Verification is by compile, tests and `analyze` only; never claim how it sounds or behaves in game.

## 1. Collect parameters (ask once, with `ask_user`; use defaults for the rest)

| Parameter | Meaning | Default |
|---|---|---|
| `xc` | Xeno-canto number | required |
| `bird` | bird id in `Birding/assets/<id>/` | required |
| `name` | cue and file name, snake_case (`<bird>_<role>`) | `<bird>_<role>` |
| `role` | sound role the species uses: `song`, `call`, `alarm`, `flight`, ... | required |
| `part` | which stretch: explicit `[start, end]` seconds, several segments to join, or "find it" | find it |
| `repeat` | how often to play the cut in game | none (play once) |
| `edit` | `lowcut`, `highcut`, `gain`, `noise` stretch, `noise_strength`, `fade`, `pad` | tool defaults |
| `trigger` | when the bird emits it (see 4) | required |
| `chance`, `when`, `role gate` | roll per idle pick, season/time/weather clause, group role such as `male` | no gate |
| `exclusive` | lock name plus `exclusiveCooldownMs` so birds do not overlap | none |

## 2. Licence check (before any work)

Fetch `https://xeno-canto.org/<xc>` (the tool does this during `build`). Stop and ask if the licence is **ND** (`by-nd`, `by-nc-nd`): cutting and filtering is a derivative, so it is not allowed. `by-nc` and `by-nc-sa` are allowed but bind the mod's audio (non-commercial, share-alike); say so and prefer CC0 or CC BY when the user has a choice. Credits are generated, never hand-written.

## 3. Cut the audio

1. The source file must exist as `~/Downloads/XC<xc> *.*` (`--sources DIR` for another place). If it is missing, ask the user to download it from the recording page.
2. Find the part: for tonal sounds (whistles, songs, tit calls) run `uv run tools/audio.py analyze <file> --start S --end E` and read note onsets, durations and pitch. Cut in the quiet gaps between notes, with about 0.1 s lead-in before the first note and the tail of the last note kept. For non-tonal sounds (drumming, rattles, wing noise) `analyze` finds nothing; cut by the user's timestamps.
3. Add an entry to `tools/audio.toml` (`xc`, `segments`, optional edit keys). Choose `lowcut` just below the call's lowest frequency to drop rumble and low coos. Use `noise = [start, end]` from a quiet stretch with no other birds only when background noise sits in the same band; it can make the sound thin, so keep `noise_strength` at 1 or lower by default.
4. Run `uv run tools/audio.py build <name>`. Check the written length, rerun `analyze` on the output and compare note count and spacing with the source. A cue meant to repeat must be a single unit with a clean start and end.
5. Report what was cut and why. Say that quality was not heard, and ask the user to listen. Iterate on their feedback only (the user is the judge of tone).

`build` writes `Birding/assets/audio/<name>.ogg` and regenerates `Birding/assets/audio/CREDITS.md`. Mono OGG only.

## 4. Wire it to the species

Read the species JSON, its template, and the bird type in `Birding/Content/BirdTypeProfiles.cs` first. Sound roles map to cues in `sounds` as `"audio:<name>"`; `""` silences a role.

Pick the trigger that fits, within what exists today:

- **At flee or take-off:** set the species' `sounds.alarm` (the flee action's `sound` role; `soundChance` in `actions.flee`). Only for species that really call on take-off; silence it otherwise.
- **Idle call or song:** a species `actions.<id>` with `"do": "stay"`, a `clip` (an existing pose such as `stand`), `"sound": "<role>"`, optionally `soundRepeat` `[min, max]` and `soundIntervalMs` (set to the cue length) and `exclusive` plus `exclusiveCooldownMs`; then a `rare` entry in a routine file under `Birding/assets/routines/` with `chance`, `when` (seasons, times, `sunrise`/`dusk` anchors) and `if: { "role": "male" }` when gated. Routines cannot extend others, so copy the bird type's default routine into a new file (see `songbird_idle.json`) and reference it with `"routine": "<id>"`. Rare ids must be actions the species defines, or validation fails.
- **Role gates** (`male`, `female`) need `spawnParams.group.members` with `required: true` for the gated role.
- **Not yet supported** (planned in Phase 10 step 10; say so and offer to add the design note instead): sounds during an action (flight calls), group chatter, a voice cap, several cues per role, carrying beyond the `song` role, ambience. Tree-trunk birds have no custom idle actions yet, so woodpecker drumming needs engine work first.
- The distance carry (full on screen, 20% floor elsewhere) applies only to the role named `song`; other roles use the game's falloff (silent about 12 tiles off screen).

## 5. Verify and document

1. `dotnet test Birding.sln --arch x64` and a Release compile (`cd Birding && dotnet build Birding.csproj -c Release -p:EnableModDeploy=false -p:EnableModZip=false`). Add or adapt a content test (see `ContentTests.GreatTit_MalesSingInSpringAndSummerMornings`); keep each rule in one layer only (schema owns shape, loader/validator own semantics).
2. If a role, field or behavior was added, update `docs/ARCHITECTURE.md`, `docs/REFACTOR.md` and `CONTRIBUTING.md`.
3. Add a manual check row to `docs/MANUAL_TESTS.md` for each new trigger (what to do, what to hear, what must not happen, for example overlapping singers).
4. Finish with the generated `CREDITS.md` diff and a one-line Conventional Commit suggestion.

## Guardrails

- Never write or paste Xeno-canto metadata by hand; it comes from the page.
- One recording per species. Do not pitch-shift one species' recording to stand in for another.
- Do not touch unrelated uncommitted files.
