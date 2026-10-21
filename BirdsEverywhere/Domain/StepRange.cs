namespace BirdsEverywhere.Domain;

/// <summary>Inclusive range rolled as <c>Next(Min / Step, Max / Step + 1) * Step</c>.</summary>
public readonly record struct StepRange(int Min, int Max, int Step)
{
    /// <summary>Fixed values draw nothing, so legacy RNG streams stay aligned.</summary>
    public int Roll(Random random) => Min == Max ? Min : random.Next(Min / Step, Max / Step + 1) * Step;
}
