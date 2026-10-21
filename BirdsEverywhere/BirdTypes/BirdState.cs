using BirdsEverywhere.Domain;
using Microsoft.Xna.Framework;

namespace BirdsEverywhere.BirdTypes;

/// <summary>Explicit snapshot of a live bird for multiplayer messages. Type-specific fields keep their defaults on other types.</summary>
internal sealed record BirdState(
    string BirdTypeName,
    long BirdId,
    string BirdName,
    string Texture,
    BehaviorStatus State,
    Vector2 Position,
    Vector2 StartingPosition,
    bool Flip,
    float GravityAffectedDY,
    float YOffset,
    float YJumpOffset,
    int BaseFrame,
    CurrentAnimatedSprite Sprite,
    int CharacterCheckTimer,
    long IdleTick,
    long IdleRevision,
    IdleCue? IdleCue,
    BirdComposition? Composition,
    float FlightOffset = 0,
    int WalkTimer = 0,
    Vector2? AnchorTile = null,
    string? LocationName = null,
    int PeckTimer = 0,
    int FlightDistance = 0,
    int Relocations = 0,
    Vector2? RelocateTarget = null);
