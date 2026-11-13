---
name: add-backlog-bird
description: Add a bird species or sighting to docs/BIRD_BACKLOG.md as a candidate row, following the document's rules. Use when asked to add, note or plan a new species, a seasonal sighting or an open-sea or special-location bird for the Birding mod, including its habitat, bird type, availability and notes.
---
# Add a bird to the backlog

Writes one candidate row in `docs/BIRD_BACKLOG.md`. It only plans: it creates no sprites, config, sounds or code. Re-read the document's "How to use this list" section each time, because its rules are the source of truth.

## 1. Collect parameters (ask once with `ask_user`; fill what you can from knowledge and state it)

| Parameter | Meaning | Default |
|---|---|---|
| `species` | common name and scientific name, written `Common Name (*Genus species*)` | required |
| `region` | the document's region heading: Valley, Desert, Ginger Island, or Special locations | infer from the habitat |
| `where` | the habitat in the user's words (for example "open sea in the south of the Forest") | required |
| `bird type` | Ground, Water, Tree trunk, Bush, Canopy (planned) or Air | infer |
| `availability` | season or time: All year, a season, Winter, Night only, Open-sea sighting, or Migration | required |
| `rarity` | ★ to ★★★★★, or TBD | TBD |
| `behaviour` | what the bird needs to do and what it can skip | none |
| `notes` | range, art, unlock, mechanics | none |

## 2. Check before writing

1. The species is not already shipped (`Birding/assets/<id>/`) and not already a row in the document. If it is, update that row instead of adding another.
2. The location group exists in `Birding/assets/regions.json` and in the document's "Location groups" table. If it does not, write `— (group needed)` in the group column, and say in `Notes` what the group has to be. If the habitat is only part of a map (for example a southern sea, or an area behind a bridge), say it needs a derived sub-area like `Beach:East` or a fish area. Never invent a group name or add locations to a row.
3. The needed behaviour exists in the bird types (`LandBird`, `WaterLandBird`, `TreeTrunkBird`, `BushBird`) and the planned list in `docs/REFACTOR.md` (backlog) and `docs/ARCHITECTURE.md` ("Canopy and aerial birds"). Put each missing mechanic or art need in `Notes`. A bird that cannot be approached needs no flee behaviour, so say so.
4. Real-world facts (range, season) come from general knowledge. State them as such, and keep them to the abbreviations the document defines (`EU`, `AS`, `NA`, `CAM`, `SA`, `AU`, `AF`). Add an abbreviation to the document's list before using a new one.

## 3. Write the row

- Status is `Candidate`. Use `Ready` only when the user says the region, habitat and required work are decided.
- Columns, in order: Status, Species, Location group, Bird type, Availability, Rarity, Notes.
- Location groups are written with backticks and `@`, for example `@coast`. Use `—` when none applies.
- Sort the table by location group, then bird type, then availability, then rarity. Text is alphabetical; fewer stars come first; `TBD` and missing values come last. Insert the row at the right place, not at the end.
- Keep `Notes` short: range abbreviation, the mechanic or art it needs, one line of behaviour. No empty headings, no new sections unless a region has none (then replace "No candidates selected yet." with the table).
- If a group changed, update `regions.json` first and mirror it in the "Location groups" table. Do not do that for a "group needed" row.

## 4. Finish

Show the inserted row and what it still needs, state which facts came from general knowledge, and end with a one-line Conventional Commit suggestion such as `docs(backlog): add great white pelican`.

## Guardrails

- Edit only `docs/BIRD_BACKLOG.md` (and `regions.json` if the user asked for a new group). Do not touch unrelated uncommitted files.
- No sprites, JSON species files or code. Use the `add-bird-sound` skill for sounds.
