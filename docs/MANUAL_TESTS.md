# Manual Test Cases

Run these in a disposable save with SMAPI and the current mod on each client. Setup and command reference: [CONTRIBUTING.md](../CONTRIBUTING.md). Mark results **Pass / Fail / Blocked / Not run** per build and attach screenshots or SMAPI logs for failures; the tables define cases, not completed results. Record build/version, date, save/day, season/weather, location, seed, host/farmhand roles and command(s) with each run. Use natural spawns for frequency checks; forced spawns only for behavior and sync checks.

## Smoke and content (one client)

| ID | Action | Expected result |
| --- | --- | --- |
| S01 | Launch through SMAPI, load a save, inspect the startup log. | Mod loads without new errors/warnings; day plan is available through `birds_plan`. |
| S02 | Visit planned locations using `birds_plan` and `birds_live` across several days and seasons. | Eligible birds appear in plausible habitats with correct sprites, frames and depth; absent birds follow rule conditions. |
| S03 | Observe each available bird type (land, water-land, tree, bush) from a distance, then approach. | Idle behaviors vary (partridge looks around or crouches); walking/hopping/swimming/perching animate, partridge runs with a smooth 2-frame cycle, birds flee or run as appropriate and eventually despawn without getting stuck. Tit and gull groups are 1-3 birds outside winter, larger in winter; only one woodpecker or partridge appears per area. |
| S04 | Remove a tree or bush that has an anchored bird. | Bird despawns cleanly; no crash or orphaned bird remains. |
| S05 | Visit areas with vanilla critters, then open the bird collection tab, hover entries and observe a bird. | Targeted vanilla birds are removed, unrelated critters remain; icons, hover text and seen state are correct. |
| S05b | Hover a seen and an unseen bird in the collection tab. | Seen: three stacked boxes: name with scientific name below, wrapped description, then coloured rows for player, location and date (season icon), all readable and on screen; unseen: `???`. |
| S06 | Save, quit, reload and inspect collection and `birds_xp`. | Observations and birding progress persist; no duplicate or lost sightings. |
| S07 | Approach a visible herring gull from about eight tiles away, then move within six tiles on water and three tiles after it swims away. Repeat near a shore and on land. | Discovery registers before fleeing; on water it swims away slowly (not faster than ordinary drifting by much) first, then flies at close range, sometimes landing on free water and sometimes on walkable land. On land it can fly to water; after landing it swims or walks on the appropriate surface without sticking. |
| S08 | Watch a gull at rest on water and on land for several in-game minutes, including near a shoreline. | It drifts slightly on water without entering land, and walks more often on land without walking through obstacles. |

## Debug tools (host unless noted)

| ID | Action | Expected result |
| --- | --- | --- |
| D01 | In each client's SMAPI console run `birds_debug on`, then `birds_debug off`. | Name/state labels show/hide locally; day-plan HUD appears when debug is on at day start. |
| D02 | Run `birds_rules blue_tit`, `birds_check blue_tit` and `birds_plan`. | Rules and eligibility print; dry run reports candidates/placements without changing the plan. |
| D03 | In a valid location run `birds_spawn blue_tit -l Farm -f`, then `birds_live` and `birds_plan`. | Bird(s) appear on the Farm and the plan/live lists reflect them; `-f` can fall back to an adjacent free tile here. |
| D04 | Run `birds_clear` in the current location; then `birds_replan 42` twice with the same day and conditions. | Clear removes birds and local plan entries; replan repopulates with repeatable placements for that seed. |
| D05 | Run `birds_reload`, inspect SMAPI log, then try a host-only command in the farmhand console. | Definitions reload without errors (textures may stay cached); farmhand is told to run host-only commands on the host. |

## Time windows and conditions (one client)

| ID | Action | Expected result |
|---|---|---|
| T01 | Temporarily give a rule `"when": { "times": [["dusk", "dark"]] }`, `birds_reload`, `birds_replan`, then watch the clock in the location across dusk. | Birds appear when the game starts getting dark and not before; birds already present stay after the window closes. |
| T02 | Give a rule `"when": { "times": [["sunrise", "sunrise+120"]] }` and start a day. | Birds are present from 0600 to 0800 only; `birds_rules` shows the anchored text. |
| T03 | Give a rule two clauses in `when.any` (for example spring all day, summer only `["dusk-60", "dusk"]`) and check a spring and a summer day. | Active in all of spring; in summer only in the dusk window; `birds_rules` prints the clauses joined by `OR`. |
| T04 | Repeat T01 in a location with a different dark time (for example a mod or a season with earlier dusk). | The window opens at that location's own dusk time. |
| T05 | Put `any` and a top-level `seasons` in one rule and `birds_reload`. | Load error naming the field and saying to set the inherited field to `null`. |

## Group following (one client)

| ID | Action | Expected result |
|---|---|---|
| F01 | Spawn a water group with one `parent` and 6 `follow`ing juveniles, then walk the parent around the pond. | Juveniles trail the parent spread around it without overlapping each other or the parent; none stack on one spot. |
| F02 | Let the parent turn and change direction repeatedly, then watch the juveniles. | Juveniles keep their relative places and do not jitter; slots on land are replaced by the parent's position. |

## Canopy hide (one client)

| ID | Action | Expected result |
|---|---|---|
| C01 | Stand near a mature tree, spawn a great tit 5-8 tiles away and walk toward it. | The tit flies into the tree and vanishes with a leaf rustle (or lands on a farther ground spot); it does not leave the screen while a tree or landing spot is available. |
| C02 | Stay near the tree after C01. | The tit stays hidden while you are within its flee radius of the tree. |
| C03 | Walk away from the tree for about 10 s. | The tit drops to the ground near the tree and resumes normal behavior. |
| C04 | Repeat C01, then shake the tree or fell it while the tit hides. | The tit bursts out and flies offscreen. |
| C05 | With no mature tree nearby, approach a tit. | It lands on a distant ground spot or flies offscreen as before. |

## Trunk hop (one client)

| ID | Action | Expected result |
|---|---|---|
| W01 | Spawn a black woodpecker on a trunk with other mature trees around and approach from one side. | It flies to another trunk on the far side from you, not past you. |
| W02 | Repeat with no tree on the far side. | It flees offscreen; it never hops toward you. |

## Gull shore safety (one client)

| ID | Action | Expected result |
|---|---|---|
| G01 | At the Beach, approach a swimming gull from the open-water side toward the pier. | It takes off before touching the shore; no left/right jitter beside the pier. |
| G02 | Approach a gull standing on sand or the pier, then approach it again after it lands. | It flies to other ground or open water, and can do so a second time. |
| G03 | Approach a gull on open water at 3 tiles. | It swims away first (radius 6), then flies if you keep closing in; it prefers landing on water. |

## Departures and return (one client unless noted)

| ID | Action | Expected result |
|---|---|---|
| F01 | Make a bird fly offscreen, leave the location and re-enter at once. | That bird is absent; others are unchanged. |
| F02 | Repeat, then wait at least 2 in-game hours elsewhere and re-enter. | The bird is back on a different tile near its planned spot, at least 10 tiles from you. |
| F03 | Scare a whole group offscreen; return after 2 in-game hours. | The group returns together, not on the old tiles. |
| F04 | Two players: farmhand scares a bird offscreen and leaves; host enters at once, then after 2 in-game hours. | Absent for the host at once; present after the delay. |

## Reachability and Beach bridge (one client)

| ID | Action | Expected result |
|---|---|---|
| G01 | Fresh save, bridge unrepaired: visit the Beach several days. | No bird on the unreachable east side; gulls never fly or land there. |
| G02 | Repair the bridge, then visit the Beach after the next day starts. | Birds may spawn east of the bridge. |
| G03 | Run `birds_check` for a Beach bird before and after the repair. | Candidate tiles never include the east side before repair. |

## Bush birds (one client)

| ID | Action | Expected result |
|---|---|---|
| H01 | Enter a location with large bushes and find a bush bird (e.g. grey partridge). | It sits 2–5 tiles from a large bush, on the side facing the entrance. |
| H02 | Walk toward it to about 10 tiles. | It freezes on `stand`; it returns to idling after you walk away. |
| H03 | Stay 4–11 tiles away for over 3 s. | It runs to the bush and vanishes. |
| H04 | Walk straight at it from the open side. | It bolts to the bush at 3 tiles, never through you. |

## Group cohesion (one client)

| ID | Action | Expected result |
|---|---|---|
| I01 | Walk at a flock of tits from one side. | Members react with small staggered delays and flee away from you, landing in a few clusters. |
| I02 | Approach a gull flock on the beach. | Gulls fan out to both sides of your approach, none flies through you. |
| I03 | Stand in the middle of a flock. | Birds on each side flee away from you on their own side. |
| I04 | Walk away after a flock scattered; wait 10 s. | Distant birds fly back to land near a mate, once. |
| I05 | Let a flock fly offscreen, leave and re-enter. | Returning birds appear near a remaining mate. |
| I06 | Spook a bird, then run to its landing spot while it flies. | It re-targets mid-flight to another spot away from you. |
| I07 | Spook a bird with one player on each side. | No spot is free, so it flies offscreen. |
| I08 | Repeat I06 many times on one bird. | At most two mid-flight changes per flight, no zigzag; tits, gulls and woodpeckers behave the same. |

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
| M11 | With both players watching a gull, approach it from water and from land until it relocates; repeat after an owner handoff. | Flight and the final water/land state agree on both clients, with no duplicate observations or stranded replicas. |

## Result log

Append one row per executed case; leave the definition tables unchanged so tests can be rerun on later builds.

| Date / build | ID | Setup (save, roles, location, seed) | Result | Evidence / issue |
| --- | --- | --- | --- | --- |