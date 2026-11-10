using Birding.BirdTypes.Behaviors;
using Birding.Domain;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.TerrainFeatures;

namespace Birding.BirdTypes;

internal sealed class TreeTrunkBird : BirdCritter
{
    private Tree? _tree;

    public override string BirdTypeName { get; } = "TreeTrunkBird";
    public Vector2 AnchorTile { get; set; }
    public string? LocationName { get; set; }
    public int PeckTimer { get; set; }

    public TreeTrunkBird() { }

    internal override BirdState CaptureExtras(BirdState state) =>
        state with { AnchorTile = AnchorTile, LocationName = LocationName, PeckTimer = PeckTimer };

    internal override void RestoreExtras(BirdState state)
    {
        PeckTimer = state.PeckTimer;
        SetAnchor(state.AnchorTile ?? Vector2.Zero, state.LocationName);
    }

    protected override void ApplyReplicaExtras(BirdCritter source)
    {
        if (source is not TreeTrunkBird owner)
        {
            return;
        }
        if (owner.AnchorTile != AnchorTile || owner.LocationName != LocationName)
        {
            SetAnchor(owner.AnchorTile, owner.LocationName);
        }
    }

    public TreeTrunkBird(int tileX, int tileY, string birdName, string texture, BirdServices services)
        : base(0, tileX, tileY, birdName, texture, services, 16, 16)
    {
        position.X += 32f;
        startingPosition = position;
    }

    public TreeTrunkBird SetAnchor(Vector2 tile, string? locationName)
    {
        AnchorTile = tile;
        LocationName = locationName;
        _tree = new TerrainAnchor(locationName, tile).ResolveTree();
        return this;
    }

    public override void drawAboveFrontLayer(SpriteBatch b)
    {
        sprite.draw(b, Game1.GlobalToLocal(Game1.viewport, position + new Vector2(-80f, -64f + yJumpOffset + yOffset)), 1f, 0, 0, Color.White, flip, 4f);
    }

    public override void draw(SpriteBatch b)
    {
        b.Draw(Game1.shadowTexture, Game1.GlobalToLocal(Game1.viewport, position + new Vector2(0f, -4f)), Game1.shadowTexture.Bounds, Color.White, 0f, new Vector2(Game1.shadowTexture.Bounds.Center.X, Game1.shadowTexture.Bounds.Center.Y), 3f + Math.Max(-3f, (yJumpOffset + yOffset) / 16f), SpriteEffects.None, (position.Y - 1f) / 10000f);
    }

    public override bool update(GameTime time, GameLocation environment)
    {
        CurrentLocation = environment;
        ActionDefinition flee = Action("flee");
        if (IsReplica)
        {
            Vector2 velocity = State switch
            {
                BehaviorStatus.FlyingAway => new Vector2(flip ? flee.Speed!.Value : -flee.Speed!.Value, 0f),
                BehaviorStatus.Relocating when RelocateTarget is { } target => Step(TrunkPosition(target), flee.Speed!.Value),
                _ => default,
            };
            return UpdateReplica(time, velocity, State == BehaviorStatus.FlyingAway ? -flee.Climb!.Value : 0f);
        }

        if (State == BehaviorStatus.Relocating)
        {
            Fly(environment, flee.Speed!.Value);
            return base.update(time, environment);
        }
        if (environment == null || sprite == null || _tree == null)
        {
            return true;
        }
        if (yJumpOffset < 0f && State != BehaviorStatus.FlyingAway)
        {
            HopDrift(environment);
        }
        PeckTimer -= time.ElapsedGameTime.Milliseconds;
        if (State != BehaviorStatus.FlyingAway && PeckTimer <= 0 && sprite.CurrentAnimation == null)
        {
            StartPeck();
        }

        bool pollNow = CheckIntervalElapsed(time);
        Character? threat = pollNow ? ScanThreats(environment, Threat).Flee : null;
        bool treeFelled = pollNow && _tree.stump.Value;
        BirdContext context = new(State, threat != null || treeFelled, sprite.CurrentAnimation == null, yJumpOffset < 0f, Rng);
        if (BirdBehaviors.Decide(BirdBehaviors.FleeOnly, context) == BirdAction.Flee)
        {
            if (threat != null && RelocationTarget(environment, threat) is { } tree)
            {
                StartRelocate(tree, environment);
            }
            else
            {
                StartFlee(threat, environment);
            }
        }

        if (State == BehaviorStatus.FlyingAway)
        {
            position.X += flip ? flee.Speed!.Value : -flee.Speed!.Value;
            yOffset -= flee.Climb!.Value;
        }
        return base.update(time, environment);
    }

    internal void StartPeck() => PlayClip(Action("peck").Clip!, Rng, DonePecking);

    private static Vector2 TrunkPosition(Vector2 tile) => tile * 64f + new Vector2(32f, 0f);

    /// <summary>A free eligible tree for an <c>otherTree</c> escape option; occupied counts other trunk birds' anchors and flight targets.</summary>
    private Vector2? RelocationTarget(GameLocation environment, Character threat)
    {
        if (!Threat.CanEscapeTo("otherTree"))
        {
            return null;
        }
        HashSet<Vector2> occupied = environment.critters?.OfType<TreeTrunkBird>().Where(bird => bird != this)
            .SelectMany(bird => bird.RelocateTarget is { } target ? new[] { bird.AnchorTile, target } : new[] { bird.AnchorTile }).ToHashSet() ?? new();
        return EscapePlanner.Tree(new Spawners.GameLocationQuery(environment), AnchorTile, threat.Tile, Threat, Relocations, occupied, Rng);
    }

    private void StartRelocate(Vector2 tree, GameLocation environment)
    {
        PlayActionSound(Action("flee"));
        State = BehaviorStatus.Relocating;
        BeginRelocation(tree, TrunkPosition(tree), environment);
        StartFlyAnimation();
    }

    private void Fly(GameLocation environment, float speed)
    {
        if (RelocateTarget is not { } tile)
        {
            StartFlee(null, environment);
            return;
        }
        if (!AdvanceRelocation(TrunkPosition(tile), speed))
        {
            return;
        }
        SetAnchor(tile, environment.Name);
        if (_tree == null)
        {
            StartFlee(null, environment);
            return;
        }
        startingPosition = position;
        State = BehaviorStatus.Pecking;
        sprite.loop = false;
        sprite.CurrentAnimation = null;
        ShowPose(Action("peck").Clip!);
        PeckTimer = Action("peck").PauseMs is { } pause ? pause.Roll(Rng) : 0;
    }

    private void StartFlee(Character? threat, GameLocation environment)
    {
        PlayActionSound(Action("flee"));
        State = BehaviorStatus.FlyingAway;
        AddBirdObservation(BirdName, environment.Name);
        flip = threat == null || threat.Position.X <= position.X;
        StartFlyAnimation();
    }

    internal void StartFlyAnimation() => PlayClip(Action("flee").Clip!, VisualRng);

    private void HopDrift(GameLocation environment)
    {
        if (!flip && !IsBlocked(environment, -2))
        {
            position.X -= 2f;
        }
        else if (!IsBlocked(environment, 2))
        {
            position.X += 2f;
        }
    }

    private bool IsBlocked(GameLocation environment, int dx) =>
        environment.isCollidingPosition(getBoundingBox(dx, 0), Game1.viewport, isFarmer: false, 0, glider: false, null, pathfinding: false, projectile: false, ignoreCharacterRequirement: true);

    private void DonePecking(Farmer who)
    {
        PeckTimer = Action("peck").PauseMs is { } pause ? pause.Roll(Rng) : 0;
    }

    private void PlayFlap(Farmer who)
    {
        if (Utility.isOnScreen(position, 64))
        {
            PlaySound("flap");
        }
    }

    private void PlayPeck(Farmer who)
    {
        if (Utility.isOnScreen(position, 64))
        {
            PlaySound("peck");
        }
    }

    protected override AnimatedSprite.endOfAnimationBehavior ClipEvent(string name) => name switch
    {
        "peck" => PlayPeck,
        "flap" => PlayFlap,
        _ => base.ClipEvent(name),
    };
}
