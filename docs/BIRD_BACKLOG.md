# Bird backlog

Planning list for bird species and special sightings that are not implemented
yet. One row represents a proposed appearance, rather than necessarily a new
species: the same bird can have several rows when it is intended for distinct
regions or seasonal rules.

## How to use this list

- Keep ideas as rows; do not add empty location or season headings.
- Use **Candidate** until its region, habitat, and implementation needs have
  been decided. Change it to **Ready** once it is specific enough to make an
  asset/content issue from it.
- Use the location-group reference shown below. Add or change a group in
  [`regions.json`](../Birding/assets/regions.json) first, then mirror that
  change in this document; do not repeat its individual location names in a
  candidate row.
- `Availability` is the intended season/time. `Migration` means a seasonal passage
  sighting, not merely a species that is absent for part of the year.
- `Bird type` describes the intended sighting: Ground, Water, Tree trunk,
  Bush, Canopy, or Air. Air sightings need flight/soaring behaviour; Canopy
  (birds that sit or hide in tree crowns) is planned, see
  [ARCHITECTURE.md](ARCHITECTURE.md#canopy-and-aerial-birds-ideas-not-scheduled).
  The existing runtime types are `LandBird`, `WaterLandBird`, `TreeTrunkBird`,
  and `BushBird`.
- `Rarity` ranges from ★ (common) to ★★★★★ (very rare); `TBD` means undecided.
  This is a planning rating to translate into config, not a spawn probability.
- Sort each table by location group, bird type, availability, then rarity:
  alphabetically for text, fewest stars first, with `TBD`/missing values last.
- Put unusual mechanics, art, unlock requirements, and abbreviated real-world
  ranges in `Notes`: `EU` = Europe, `AS` = Asia, `NA` = North America,
  `CAM` = Central America, `SA` = South America, `AU` = Australia, `AF` = Africa.

## Location groups

This table mirrors the `groups` objects in
[`regions.json`](../Birding/assets/regions.json). An em dash means that the
region has no groups yet; add the group there before assigning candidates to
individual locations.

| Game region | Location group | Locations |
| --- | --- | --- |
| Valley | `@big_lake` | Farm_Wilderness |
| Valley | `@city` | Town |
| Valley | `@coast` | Beach |
| Valley | `@farm_pond` | Farm_Standard |
| Valley | `@forest_lake` | Woods, Farm_Forest |
| Valley | `@mountains` | Mountain |
| Valley | `@rivers` | Forest, Town, Farm_Riverland, Farm_Hilltop |
| Valley | `@valley_gardens` | Forest, Farm, Backwoods |
| Desert | — | No groups configured |
| Ginger Island | `@island_jungle` | IslandEast, IslandWest, IslandNorth |
| Ginger Island | `@island_shore` | IslandSouth, IslandSouthEast |

## Candidates

### Valley (Europe)

| Status | Species | Location group | Bird type | Availability | Rarity | Notes |
| --- | --- | --- | --- | --- | --- | --- |
| Candidate | Pigeon (species TBD) | `@city` | Ground | All year | ★ | — |
| Candidate | King Eider (*Somateria spectabilis*) | `@coast` | Water | Winter | ★★★★★ | Arctic EU/AS/NA; Valley coast only |
| Candidate | Golden Eagle (*Aquila chrysaetos*) | `@mountains` | Air | All year | TBD | EU/AS/NA; soaring/flyovers only; needs air behaviour |
| Candidate | White-throated Dipper (*Cinclus cinclus*) | `@rivers` | Water | All year | TBD | EU/AS |
| Candidate | Common Swift (*Apus apus*) | `@valley_gardens` | Air | Summer | ★★ | EU/AS/AF; shadow-only flyover plus calls, never drawn; needs a shadow body and a "heard or seen overhead" observation |
| Candidate | Grey Partridge (*Perdix perdix*) | `@valley_gardens` | Bush | All year | ★★ | EU/NA |
| Candidate | European Goldfinch (*Carduelis carduelis*) | `@valley_gardens` | Canopy | All year | ★★ | EU/AS/AF; flocks in one tree; needs several perch points per tree, `inTree` group layout and perhaps a tree-species restriction |
| Candidate | Bohemian Waxwing (*Bombycilla garrulus*) | `@valley_gardens` | Tree trunk | All year | TBD | EU/AS/NA; flock; needs a special tree? Possible intoxicated behaviour? |
| Candidate | Long-eared Owl (*Asio otus*) | `@valley_gardens` | Tree trunk | Night only | TBD | EU/AS/NA; Bus Stop needs a group before inclusion |

### Desert (Australian)

No candidates selected yet.

### Ginger Island (Central America / North American migrants)

| Status | Species | Location group | Bird type | Availability | Rarity | Notes |
| --- | --- | --- | --- | --- | --- | --- |
| Candidate | Mottled Owl (*Strix virgata*) | `@island_jungle` | Tree trunk | All year | TBD | CAM/SA; “crazy red-eyed owl” |
| Candidate | Black Skimmer (*Rynchops niger*) | `@island_shore` | Water | Winter | TBD | NA/CAM/SA |

### Special locations

| Status | Species | Location group | Bird type | Availability | Rarity | Notes |
| --- | --- | --- | --- | --- | --- | --- |
| Candidate | Skua (species TBD) | — | Water | Open-sea sighting | TBD | Add an open-sea group if this becomes spawn content |
| Candidate | Albatross (species TBD) | — | Water | Open-sea sighting | TBD | Add an open-sea group if this becomes spawn content |
| Candidate | Great White Pelican (*Pelecanus onocrotalus*) | — (group needed) | Water | Summer | TBD | EU/AS/AF; open sea in the south of the Forest, needs a derived sub-area like `Beach:East` (confirm the sea's tiles on the map); swims and occasionally dips its bill (`peck`), no flight art; out of reach, so no flee reaction |

## Location concepts

These are features or locations that can host candidates above, not species
entries themselves.

| Status | Location | Access / purpose | Notes |
| --- | --- | --- | --- |
| Idea | Mines | Special sightings | No species selected yet |
| Idea | Deep Forest | Rare birds | Available only with a supporting mod |
| Idea | Southern Observation Platform | Rare migratory and open-sea birds | Unlock through purchase or quest line |
| Idea | Dream | Special sightings | No species selected yet |
| Idea | Livestock | Purchasable birds | No species selected yet |
| Idea | Pet | Purchasable birds | No species selected yet |

## Implementation checklist

Move a candidate to **Ready** when its species, sighting, and required work are
clear. For implementation:

- Create sprites for the required animations and variants.
- Write the config file and add the species to its region.
- Add sounds and descriptions.
- Implement any missing behaviour or location/unlock support noted in the row.
- Check the bird in-game through SMAPI.
