using Microsoft.Xna.Framework;

namespace Birding.BirdTypes;

/// <summary>The slow sideways drift of a bird resting on water: it moves a quarter pixel in its facing direction and turns around at the edge of its range or of open water.</summary>
internal static class WaterDrift
{
    public const float StepPx = 0.25f;
    public const float RangePx = 64f;

    /// <summary>One drift step. An anchor the bird has already left (it swam or walked away since) is replaced by its current position, a turn happens only when the way back is free, and a bird that can move in neither direction stays still and keeps its facing, so it never flips back and forth without moving.</summary>
    public static (Vector2 Position, bool Flip, Vector2 Anchor) Step(Vector2 position, bool flip, Vector2 anchor, Func<Vector2, Vector2, bool> canMove)
    {
        if (MathF.Abs(position.X - anchor.X) > RangePx)
        {
            anchor = position;
        }
        float step = flip ? StepPx : -StepPx;
        Vector2 next = position + new Vector2(step, 0f);
        if (MathF.Abs(next.X - anchor.X) <= RangePx && canMove(position, next))
        {
            return (next, flip, anchor);
        }
        Vector2 back = position - new Vector2(step, 0f);
        return MathF.Abs(back.X - anchor.X) <= RangePx && canMove(position, back) ? (position, !flip, anchor) : (position, flip, anchor);
    }
}
