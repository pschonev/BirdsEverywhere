# TODO

- multiplayer: let host handle exact spawn locations of birds and then send it to others. each time bird is seen send it to host so he can save it.
- change layerdepth of drawing birds that are not flying
- birdsToday should have fixed positions for each spawning bird calculated at start of day and then synched

## Spawners

- add directional spawners to
  - e.g. bottom up
  - this allows control over where roughly a bird spawns but is agnostic of the actual map file (important for compatibility)
- spawners for couples (breeding season?) or juveniles
- spawning on trees, fences, near bushes etc. (see below for requirements related to bird types)

## Bird Types

- new bird types
  - bird type for ground birds hiding in bushes
  - birds that don't fly away but just fly to another spot
  - tame birds that won't fly away
  - birds that will only fly by
  - birds on trunk (woodpeckers et al.)
  - water and hybrid birds
- some birds could show special behavior (more/different animation)
  - e.g. hunting bird, dancing bird

## Daily Spawner
- consider luck for spawning rarer birds
- save bird spawn locations and coordinates. then allow multiple bird species to spawn in the same location but with a certain distance between them
  - this could become important later when spawning multiple birds on the farm

## General

- add eligible locations to spawn when making progress? same for biomes?


# Birding

- need to find lore friendly way to display birds seen or just implement bird list into menu
- seeing more birds should unlock benefits to find even rarer birds or receive other new features
- add letters, events, dialogue etc. around birding to Demetrius (and Professor S)
- 