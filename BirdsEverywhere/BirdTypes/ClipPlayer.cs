using BirdsEverywhere.Domain;
using StardewValley;

namespace BirdsEverywhere.BirdTypes;

internal static class ClipPlayer
{
    /// <summary>Plays frames, then repeat rolled count times, then end; ranges roll on <paramref name="random"/> in that order.</summary>
    public static List<FarmerSprite.AnimationFrame> Frames(
        SpriteClip clip,
        int baseFrame,
        bool flip,
        Random random,
        Func<string, AnimatedSprite.endOfAnimationBehavior> events,
        AnimatedSprite.endOfAnimationBehavior? onEnd)
    {
        List<FarmerSprite.AnimationFrame> frames = new();
        Add(frames, clip.Frames, baseFrame, flip, random, events);
        if (clip.Repeat is { } repeat)
        {
            int count = repeat.Count.Roll(random);
            for (int i = 0; i < count; i++)
            {
                Add(frames, repeat.Frames, baseFrame, flip, random, events);
            }
        }
        Add(frames, clip.End, baseFrame, flip, random, events);
        if (onEnd is not null && frames.Count > 0)
        {
            FarmerSprite.AnimationFrame last = frames[^1];
            frames[^1] = new FarmerSprite.AnimationFrame(last.frame, last.milliseconds, secondaryArm: false, last.flip, last.frameStartBehavior + onEnd);
        }
        return frames;
    }

    public static int FirstCell(SpriteClip clip) =>
        clip.Frames.Count > 0 ? clip.Frames[0].Cell : clip.Repeat?.Frames[0].Cell ?? clip.End[0].Cell;

    private static void Add(
        List<FarmerSprite.AnimationFrame> frames,
        IReadOnlyList<ClipFrame> source,
        int baseFrame,
        bool flip,
        Random random,
        Func<string, AnimatedSprite.endOfAnimationBehavior> events)
    {
        foreach (ClipFrame frame in source)
        {
            frames.Add(new FarmerSprite.AnimationFrame(baseFrame + frame.Cell, frame.Ms.Roll(random), secondaryArm: false, frame.Flip && flip,
                frame.Event is null ? null : events(frame.Event)));
        }
    }
}
