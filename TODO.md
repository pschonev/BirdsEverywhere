# TODO
### next
- birds in trees? flying into trees (see squirrel)
- implement possibilities to have dimorphism (gender), different plumage (breeding season), babies (e.g. ducks) in season with advanced, young birds (gulls, swans)
- change layerdepth of drawing birds that are not flying
- spawner direction
- newly calculate bird positions when chance % or when no bird spawns
- implement stuff from Bird and PerchingBird like birds flying to a new spot

### also
- PerchingBird keeps track of all bird locations. syncing position and state of every bird in multiplayer possible after al??
- parse configs properly (multiple levels of default values -> BirdData - template - default spawner - advanced)
- luck factor e.g. 3% chance that ANY new bird will spawn (even rare ones)
- implement many more behaviors
- validate every config when loading (locations, files etc)
- sounds
- fix bird list
- cache textures
- Kuhreiher auf Kühen

## Multiplayer Sprite sync
- player who warped somewhere new checks if there are other players there 
- if not, spawn birds yourself. if there are others send the first possible one a message
- the one who got the message does the following:
  - pack all values of birds a location using CustomBird.getCurrentParameters
  - send parameters in message
  - reset Random() with the birdID as seed (birdID is created every time someone enters empty location)
- all players who receive the message and are at the specified location
  - if they are the original player who warped there: use the currentBirdParameters to rebuild them
  - everyone: seed random() with birdID (again)

- currentParams is internal class only used by CustomBirdType and derivatives
- it includes position, currentFrame, frameloop, state, birdID, yoffset, index of terrain feature etc
- currenParams gets created by running CustomBirdType.saveParams()
  - this creates a derivative of currentParams that is defined in the derivative of CustomBirdType
  - different behaviors defined in overwritten saveParams() function in CustomBirdType derivative
  - inside the currentParams class there is createBirdFromParams() which goes the other way around
  - it creates a CustomBirdType of the specific derivative

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