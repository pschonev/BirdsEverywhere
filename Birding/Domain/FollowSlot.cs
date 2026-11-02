namespace Birding.Domain;

/// <summary>Spreads followers of one leader on a golden-angle spiral so they never share a spot.</summary>
public static class FollowSlot
{
    private const float InnerRadius = 56f;
    private const float RadiusStep = 28f;
    private const float GoldenAngle = 2.3999632f;

    /// <summary>Pixel offset from the leader for the follower with the given zero-based <paramref name="index"/>.</summary>
    public static (float X, float Y) Offset(int index)
    {
        float radius = InnerRadius + (RadiusStep * MathF.Sqrt(index));
        float angle = index * GoldenAngle;
        return (radius * MathF.Cos(angle), radius * MathF.Sin(angle));
    }
}
