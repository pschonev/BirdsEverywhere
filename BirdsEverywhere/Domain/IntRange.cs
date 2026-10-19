namespace BirdsEverywhere.Domain;

public readonly record struct IntRange(int Min, int Max)
{
    public bool Contains(int value) => value >= Min && value <= Max;
}