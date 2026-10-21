using BirdsEverywhere.BirdTypes;
using BirdsEverywhere.BirdTypes.Behaviors;
using BirdsEverywhere.Domain;
using NUnit.Framework;
using StardewValley;

namespace BirdsEverwhyere.UnitTests;

// Golden legacy animations and RNG draws; Phase 10 clips must reproduce them exactly. Run with --arch x64.
public class AnimationCharacterizationTests
{
    private const long BirdId = 42;

    private static void Golden(string actual, string expected) => Assert.That(actual, Is.EqualTo(expected), $"actual: {actual}");

    // cell:ms, "f" = frame flip, "*" = frame callback, " loop" = looping clip
    private static string Frames(AnimatedSprite sprite) =>
        string.Join(" ", sprite.CurrentAnimation.Select(frame =>
            $"{frame.frame}:{frame.milliseconds}{(frame.flip ? "f" : "")}{((frame.frameStartBehavior ?? frame.frameEndBehavior) is null ? "" : "*")}"))
        + (sprite.loop ? " loop" : "");

    private static T Bird<T>(int visualSeed, bool flip = false) where T : BirdCritter, new()
    {
        T bird = new() { BirdID = BirdId, sprite = new AnimatedSprite(), flip = flip, Services = TestBirds.Services };
        bird.SeedVisualRandom(visualSeed);
        return bird;
    }

    private static string Run<T>(int visualSeed, bool flip, Action<T> start) where T : BirdCritter, new()
    {
        T bird = Bird<T>(visualSeed, flip);
        start(bird);
        return $"{Frames(bird.sprite)} | next {bird.OwnerRandom.Next(1000)}";
    }

    [TestCase(1, false, "2:480 3:170 4:170 3:70 4:100* 3:100 2:70 1:70 0:500* | next 668")]
    [TestCase(2, false, "2:480 3:170 4:170 3:70 4:100* 3:70 4:100* 3:70 4:100* 3:70 4:100* 3:100 2:70 1:70 0:500* | next 668")]
    [TestCase(3, true, "2:480 3:170f 4:170f 3:70 4:100f* 3:70 4:100f* 3:100 2:70f 1:70f 0:500f* | next 668")]
    public void LandPeck(int seed, bool flip, string expected) =>
        Golden(Run<LandBird>(seed, flip, bird => bird.StartPeckAnimation()), expected);

    [TestCase(1, "0:100 1:100 loop | timer 700 flip False")]
    [TestCase(2, "0:100 1:100 loop | timer 1200 flip False")]
    public void LandWalk(int seed, string expected)
    {
        LandBird bird = Bird<LandBird>(seed);
        bird.StartWalk();
        Golden($"{Frames(bird.sprite)} | timer {bird.WalkTimer} flip {bird.flip}", expected);
    }

    [TestCase(false, "6:70 7:60* 8:70 7:60 loop | next 668")]
    [TestCase(true, "6:70 7:60f* 8:70 7:60 loop | next 668")]
    public void LandFly(bool flip, string expected) =>
        Golden(Run<LandBird>(1, flip, bird => bird.StartFlyAnimation()), expected);

    [TestCase(1, "2:100 3:100 4:200 5:200 4:200 5:200 | next 668")]
    [TestCase(2, "2:100 3:100 4:200 5:200 4:200 5:200 4:200 5:200 4:200 5:200 | next 668")]
    [TestCase(4, "2:100 3:100 4:200 5:200 4:200 5:200 4:200 5:200 4:200 5:200 4:200 5:200 | next 668")]
    [TestCase(9, "2:100 3:100 4:200 5:200 4:200 5:200 4:200 5:200 | next 668")]
    public void WaterLandPeck(int seed, string expected) =>
        Golden(Run<WaterLandBird>(seed, false, bird => bird.StartPeck()), expected);

    [TestCase(1, "6:1370 | next 668")]
    [TestCase(2, "6:3198 | next 668")]
    public void WaterLandRest(int seed, string expected) =>
        Golden(Run<WaterLandBird>(seed, false, bird => bird.StartRest()), expected);

    [TestCase(1, "6:500 7:100* 8:100 | next 668")]
    [TestCase(2, "6:500 7:100* 8:100 7:100 8:100 7:100 8:100 | next 668")]
    [TestCase(4, "6:500 7:100* 8:100 7:100 8:100 7:100 8:100 | next 668")]
    [TestCase(5, "6:500 7:100* 8:100 7:100 8:100 | next 668")]
    public void WaterLandHop(int seed, string expected) =>
        Golden(Run<WaterLandBird>(seed, false, bird => bird.StartHop()), expected);

    [TestCase(1, "0:200 1:200 loop | flip True")]
    [TestCase(2, "0:200 1:200 loop | flip False")]
    public void WaterLandWalk(int seed, string expected)
    {
        WaterLandBird bird = Bird<WaterLandBird>(seed);
        bird.StartWalk();
        Golden($"{Frames(bird.sprite)} | flip {bird.flip}", expected);
    }

    [Test]
    public void WaterLandFly() =>
        Golden(Run<WaterLandBird>(1, false, bird => bird.StartFlyAnimation()), "10:80 11:80 12:80 13:100 loop | next 668");

    [TestCase(42, "0:100 1:100* 0:100 1:100* 0:100 1:100* 0:100 1:100* 0:100 1:100* 0:100 1:100* 0:200* | next 140")]
    [TestCase(7, "0:100 1:100* 0:100 1:100* 0:100 1:100* 0:100 1:100* 0:200* | next 871")]
    public void TreeTrunkPeck(long birdId, string expected)
    {
        TreeTrunkBird bird = new() { BirdID = birdId, sprite = new AnimatedSprite(), Services = TestBirds.Services };
        bird.StartPeck();
        Golden($"{Frames(bird.sprite)} | next {bird.OwnerRandom.Next(1000)}", expected);
    }

    [TestCase(false, "2:70 3:60* 4:70 3:60 loop | next 668")]
    [TestCase(true, "2:70 3:60f* 4:70 3:60 loop | next 668")]
    public void TreeTrunkFly(bool flip, string expected) =>
        Golden(Run<TreeTrunkBird>(1, flip, bird => bird.StartFlyAnimation()), expected);

    [Test]
    public void BushRun() =>
        Golden(Run<BushBird>(1, false, bird => bird.StartRunAnimation()), "0:40 1:40 2:40 3:100 5:70 5:40 loop | next 668");

    [Test]
    public void LandPeckEnd_RollsNextStateOnOwnerRandom()
    {
        List<BehaviorStatus> states = new();
        LandBird bird = Bird<LandBird>(1);
        for (int i = 0; i < 8; i++)
        {
            bird.StartPeckAnimation();
            bird.sprite.CurrentAnimation[^1].frameStartBehavior(null!);
            states.Add(bird.State);
        }
        Golden(string.Join(",", states), "Stopped,Pecking,Pecking,Stopped,Pecking,Pecking,Stopped,Stopped");
    }

    [Test]
    public void TreeTrunkPeckEnd_RollsWaitOnOwnerRandom()
    {
        TreeTrunkBird bird = new() { BirdID = BirdId, sprite = new AnimatedSprite(), Services = TestBirds.Services };
        List<int> waits = new();
        for (int i = 0; i < 4; i++)
        {
            bird.StartPeck();
            bird.sprite.CurrentAnimation[^1].frameStartBehavior(null!);
            waits.Add(bird.PeckTimer);
        }
        Golden(string.Join(",", waits), "1281,2045,1525,2025");
    }

    private sealed class IdleProbe : BirdCritter
    {
        public override string BirdTypeName => "IdleProbe";

        public BirdAction Next(Routine routine) => AdvanceIdle(routine, eligible: true);
    }

    private static string IdleSequence(string routineId)
    {
        Routine routine = TestBirds.Services.Routines[routineId];
        IdleProbe bird = new() { BirdID = BirdId };
        List<string> cues = new();
        for (int tick = 1; cues.Count < 12 && tick < 100_000; tick++)
        {
            BirdAction action = bird.Next(routine);
            if (action != BirdAction.None)
            {
                cues.Add($"{tick}:{action}:{bird.VisualRandom.Next(1000)}");
            }
        }
        return string.Join(" ", cues);
    }

    [Test]
    public void LandIdleSchedule() => Golden(IdleSequence("land_idle"),
        "24:TurnAndHop:48 90:Hop:557 195:Hop:404 344:Sleep:984 612:TurnAndHop:96 788:Walk:55 831:Hop:309 1071:Walk:61 1216:Hop:367 1234:TurnAndHop:322 1282:Hop:910 1513:TurnAndHop:52");

    [Test]
    public void WaterLandIdleSchedule() => Golden(IdleSequence("waterland_idle"),
        "24:Hop:48 90:Rest:557 616:Hop:96 837:Rest:309 1077:Walk:61 1292:Rest:910 2277:Rest:310 2700:Hop:591 3025:Walk:210 3208:Rest:18 3239:Peck:857 3267:Hop:425");
}
