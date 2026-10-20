# Manual Test Cases

Run these in a disposable save with SMAPI and the current mod on each client. Setup and command reference: [CONTRIBUTING.md](../CONTRIBUTING.md). Mark results **Pass / Fail / Blocked / Not run** per build and attach screenshots or SMAPI logs for failures; the tables define cases, not completed results. Record build/version, date, save/day, season/weather, location, seed, host/farmhand roles and command(s) with each run. Use natural spawns for frequency checks; forced spawns only for behavior and sync checks.

## Smoke and content (one client)

| ID | Action | Expected result |
| --- | --- | --- |
| S01 | Launch through SMAPI, load a save, inspect the startup log. | Mod loads without new errors/warnings; day plan is available through `birds_plan`. |
| S02 | Visit planned locations using `birds_plan` and `birds_live` across several days and seasons. | Eligible birds appear in plausible habitats with correct sprites, frames and depth; absent birds follow rule conditions. |
| S03 | Observe each available bird type (land, water-land, tree, bush) from a distance, then approach. | Idle behaviors vary; walking/hopping/swimming/perching animate, birds flee or run as appropriate and eventually despawn without getting stuck. |
| S04 | Remove a tree or bush that has an anchored bird. | Bird despawns cleanly; no crash or orphaned bird remains. |
| S05 | Visit areas with vanilla critters, then open the bird collection tab, hover entries and observe a bird. | Targeted vanilla birds are removed, unrelated critters remain; icons, hover text and seen state are correct. |
| S06 | Save, quit, reload and inspect collection and `birds_xp`. | Observations and birding progress persist; no duplicate or lost sightings. |

## Debug tools (host unless noted)

| ID | Action | Expected result |
| --- | --- | --- |
| D01 | In each client's SMAPI console run `birds_debug on`, then `birds_debug off`. | Name/state labels show/hide locally; day-plan HUD appears when debug is on at day start. |
| D02 | Run `birds_rules blue_tit`, `birds_check blue_tit` and `birds_plan`. | Rules and eligibility print; dry run reports candidates/placements without changing the plan. |
| D03 | In a valid location run `birds_spawn blue_tit -l Farm -f`, then `birds_live` and `birds_plan`. | Bird(s) appear on the Farm and the plan/live lists reflect them; `-f` can fall back to an adjacent free tile here. |
| D04 | Run `birds_clear` in the current location; then `birds_replan 42` twice with the same day and conditions. | Clear removes birds and local plan entries; replan repopulates with repeatable placements for that seed. |
| D05 | Run `birds_reload`, inspect SMAPI log, then try a host-only command in the farmhand console. | Definitions reload without errors (textures may stay cached); farmhand is told to run host-only commands on the host. |

## Multiplayer synchronization (two SMAPI clients)

| ID | Action | Expected result |
| --- | --- | --- |
| M01 | Host enters a location with birds; farmhand joins it later. Compare overlay and `birds_live` on both. | Farmhand receives current birds, poses and plan; no duplicates or permanent frozen replicas. |
| M02 | Keep both players in one location and watch idle pecks/rests/hops/walks, then a flight or bush run. | Same bird and owner-chosen actions appear on both screens; replica animations and committed motion continue between snapshots without restarting. Some short-lead idle cues can appear late on the farmhand. |
| M03 | With both watching one bird, host approaches it; repeat with farmhand approaching another bird. | Either player's approach triggers one authoritative reaction, flight and eventual despawn on both screens; no observation/world effect is duplicated. |
| M04 | Farmhand enters a location first; host joins after birds start moving. | Farmhand owns that visit's simulation; host sees current birds and actions without duplicates or stale positions. |
| M05 | Have the location's first visitor warp out while the other stays, then return. | Remaining player takes ownership; birds continue, new arrivals get current state, no removed bird respawns in the same window. |
| M06 | Disconnect the owner while the other stays, then reconnect. | Remaining player takes over from latest state; reconnection resynchronizes without duplicates or crashes. |
| M07 | With both players connected, let a farmhand discover a new species; inspect both collection tabs, save and reload. | Host accepts it once, both players see it and it persists after reload. |
| M08 | Have both players discover different species close together; then join a late third client if available. | Both discoveries persist; late joiner receives current plan, birds and observations. |
| M09 | If a controllable delayed network is available, repeat M02 and M03 with added latency. | Motion continues locally, late reactions start without rewind, snapshots correct drift, no duplicate birds or permanent divergent state. Mark Blocked if delay cannot be applied reliably. |
| M10 | Keep both players connected through sleep/day change; revisit a previously populated location. | New plan is shared, stale birds disappear and today's birds appear only once; consoles show no sync errors. |

## Result log

Append one row per executed case; leave the definition tables unchanged so tests can be rerun on later builds.

| Date / build | ID | Setup (save, roles, location, seed) | Result | Evidence / issue |
| --- | --- | --- | --- | --- |