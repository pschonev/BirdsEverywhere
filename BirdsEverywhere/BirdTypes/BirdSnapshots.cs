using Newtonsoft.Json;

namespace BirdsEverywhere.BirdTypes;

internal static class BirdSnapshots
{
    private static readonly Dictionary<string, Func<BirdCritter>> _emptyByType = new(StringComparer.Ordinal)
    {
        ["LandBird"] = () => new LandBird(),
        ["WaterLandBird"] = () => new WaterLandBird(),
        ["TreeTrunkBird"] = () => new TreeTrunkBird(),
        ["BushBird"] = () => new BushBird(),
    };

    public static BirdState Capture(BirdCritter bird) => bird.CaptureExtras(new BirdState(
        bird.BirdTypeName, bird.BirdID, bird.BirdName, bird.Texture, bird.State, bird.position, bird.startingPosition, bird.flip,
        bird.gravityAffectedDY, bird.yOffset, bird.yJumpOffset, bird.baseFrame, new CurrentAnimatedSprite(bird.sprite),
        bird.CharacterCheckTimer, bird.IdleTick, bird.IdleRevision, bird.IdleCue, bird.Composition) with
        { GroupId = bird.GroupId, Role = bird.Role, Stage = bird.Stage });

    /// <summary>Recreates a bird (loading its texture); null for an unknown bird type.</summary>
    public static BirdCritter? Restore(BirdState state)
    {
        if (!_emptyByType.TryGetValue(state.BirdTypeName, out Func<BirdCritter>? create))
        {
            return null;
        }

        BirdCritter bird = create();
        bird.BirdID = state.BirdId;
        bird.BirdName = state.BirdName;
        bird.Texture = state.Texture;
        bird.State = state.State;
        bird.position = state.Position;
        bird.startingPosition = state.StartingPosition;
        bird.flip = state.Flip;
        bird.gravityAffectedDY = state.GravityAffectedDY;
        bird.yOffset = state.YOffset;
        bird.yJumpOffset = state.YJumpOffset;
        bird.baseFrame = state.BaseFrame;
        bird.CharacterCheckTimer = state.CharacterCheckTimer;
        bird.IdleTick = state.IdleTick;
        bird.IdleRevision = state.IdleRevision;
        bird.IdleCue = state.IdleCue;
        bird.Composition = state.Composition;
        bird.GroupId = state.GroupId;
        bird.Role = state.Role;
        bird.Stage = state.Stage;
        bird.sprite = CurrentAnimatedSprite.CurrentToAnimatedSprite(state.Sprite);
        bird.RestoreExtras(state);
        return bird;
    }

    public static string Serialize(IEnumerable<BirdCritter> birds) => JsonConvert.SerializeObject(birds.Select(Capture).ToList());

    public static List<BirdCritter> Deserialize(string json) =>
        (JsonConvert.DeserializeObject<List<BirdState>>(json) ?? new List<BirdState>()).Select(Restore).OfType<BirdCritter>().ToList();
}
