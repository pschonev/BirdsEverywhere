# TODO

### fix
- make terrainfeature birds saver, in case the index + location doesn't find their appropiate terrain feature
- birds running into bushes still don't behave properly...

### next
- birds flying into trees
- fix bush birds doing nothing before running into bush (also flight distance)
- parser for birdData (inside birdData)
  - multiple levels of default values -> BirdData - template - default spawner - advanced)
- implement possibilities to have dimorphism (gender), different plumage (breeding season), babies (e.g. ducks) in season with advanced, young birds (gulls, swans)
- fix bird list GUI (instead use custom collections tab?)

### also
- make behaviors modular?
- validate every config when loading (locations, files etc)
- sounds
- cache textures (necessary?)
- try to make condition factory so that singlebirdspawner doesn't have to create spawner instances
- maybe have a dictionary spawner : condition to save them all once?
- Kuhreiher auf Kühen


## Spawners

- add directional spawners to
  - e.g. bottom up
  - this allows control over where roughly a bird spawns but is agnostic of the actual map file (important for compatibility)
- spawners for couples (breeding season?) or juveniles
- spawning on trees, fences, near bushes etc. (see below for requirements related to bird types)

## Bird Types

- new bird types
  - water birds that can swim away
  - birds that sit in tree (visible) and you can shake them away
  - birds at shore
  - flying birds that are passing by (migrating) or fly "around" (predators)
  - bird type for ground birds hiding in bushes ✔️
  - birds that don't fly away but just fly to another spot (Bird and PerchingBird)
  - tame birds that won't fly away
  - birds on trunk (woodpeckers et al.) ✔️
  - water and hybrid birds ✔️
- some birds could show special behavior (more/different animation)
  - e.g. hunting bird, dancing bird
- bird feeder that spawns birds in trees around it which will fly to it

## Daily Spawner
- consider luck for spawning rarer birds
  - give certain chance to spawn ANY bird (even rare ones) to shake up list order a bit
- save bird spawn locations and coordinates. then allow multiple bird species to spawn in the same location but with a certain distance between them
  - this could become important later when spawning multiple birds on the farm
- newly calculate bird positions when chance % or when no bird spawns

## General

- add eligible locations to spawn when making progress? same for biomes?


# Birding

- need to find lore friendly way to display birds seen or just implement bird list into menu ✔️
- seeing more birds should unlock benefits to find even rarer birds or receive other new features
- add letters, events, dialogue etc. around birding to Demetrius (and Professor S)
- 