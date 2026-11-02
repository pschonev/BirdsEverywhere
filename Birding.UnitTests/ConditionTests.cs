using Birding.Content;
using Birding.Domain;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Birding.UnitTests;

public class ConditionTests
{
    private static readonly TimeAnchors _summer = new(600, 1800, 2000);
    private static readonly TimeAnchors _winter = new(630, 1650, 1830);

    private static (SpawnConditions Conditions, List<string> Errors) Parse(string json)
    {
        List<string> errors = new();
        SpawnConditions conditions = ConditionParser.Parse(JToken.Parse(json), "$.when", (path, message) => errors.Add($"{path} {message}"));
        return (conditions, errors);
    }

    [TestCase("sunrise", 600, 630)]
    [TestCase("sunrise+120", 800, 830)]
    [TestCase("sunrise-120", 600, 600)]
    [TestCase("dusk-30", 1730, 1620)]
    [TestCase("dusk", 1800, 1650)]
    [TestCase("dark+60", 2100, 1930)]
    [TestCase("dark+999", 2600, 2600)]
    [TestCase("sunrise+5", 610, 640)]
    public void TimeBound_ResolvesPerAnchorsSnappedAndClamped(string text, int summer, int winter)
    {
        Assert.That(TimeBound.TryParse(text, out TimeBound bound), Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(bound.Resolve(_summer), Is.EqualTo(summer));
            Assert.That(bound.Resolve(_winter), Is.EqualTo(winter));
            Assert.That(bound.ToString(), Is.EqualTo(text));
        });
    }

    [TestCase("noon")]
    [TestCase("sunrise+")]
    [TestCase("sunrise+1000")]
    [TestCase("sunrise*2")]
    [TestCase("dusk+x1")]
    [TestCase("")]
    public void TimeBound_RejectsMalformedText(string text) => Assert.That(TimeBound.TryParse(text, out _), Is.False);

    [Test]
    public void Parse_PlainWhenIsOneClauseAndAnyIsSeveral()
    {
        (SpawnConditions plain, List<string> plainErrors) = Parse(@"{""seasons"":[""spring""],""times"":[[""sunrise"",""sunrise+120""]]}");
        (SpawnConditions any, List<string> anyErrors) = Parse(@"{""any"":[{""seasons"":[""spring""]},{""weather"":[""rain""]}]}");

        Assert.Multiple(() =>
        {
            Assert.That(plainErrors.Concat(anyErrors), Is.Empty);
            Assert.That(plain.Any, Has.Length.EqualTo(1));
            Assert.That(any.Any, Has.Length.EqualTo(2));
            Assert.That(ConditionParser.ToJson(plain).ToString(Newtonsoft.Json.Formatting.None),
                Is.EqualTo(@"{""seasons"":[""Spring""],""times"":[[""sunrise"",""sunrise+120""]]}"));
            Assert.That(ConditionParser.ToJson(any)["any"], Has.Count.EqualTo(2));
        });
    }

    [TestCase(@"{""any"":[{""seasons"":[""spring""]}],""weather"":[""rain""]}", "cannot be combined with 'any'")]
    [TestCase(@"{""any"":{""seasons"":[""spring""]}}", "must be a list")]
    [TestCase(@"{""moon"":[""full""]}", "not a known condition")]
    [TestCase(@"{""times"":[[""noon"",1200]]}", "is not a time")]
    [TestCase(@"{""times"":[[500,900]]}", "is not a time")]
    [TestCase(@"{""times"":[[2000,900]]}", "from must not be after to")]
    [TestCase(@"{""times"":[[""dark"",""dusk""]]}", "")]
    [TestCase(@"{""seasons"":[""monsoon""]}", "unknown season")]
    [TestCase(@"{""days"":[[0,3]]}", "expected [min, max]")]
    public void Parse_ReportsProblemsWithPath(string json, string message)
    {
        (_, List<string> errors) = Parse(json);

        if (message.Length == 0)
        {
            Assert.That(errors, Is.Empty, "anchored bounds of different kinds are ordered only at resolve time");
        }
        else
        {
            Assert.That(errors, Has.Some.Contains(message));
        }
    }

    [Test]
    public void Parse_EmptyAnyIsReportedNotUnconditional()
    {
        (SpawnConditions conditions, _) = Parse(@"{""any"":[]}");

        Assert.That(conditions.IsUnconditional, Is.True, "empty list falls back to Always; the schema rejects it");
    }

    [Test]
    public void Matches_AnyClauseOverlappingClausesCountOnce()
    {
        (SpawnConditions conditions, _) = Parse(
            @"{""any"":[{""seasons"":[""spring""],""times"":[[""sunrise"",""sunrise+120""]]},{""weather"":[""rain""]},{""weather"":[""rain""],""times"":[[700,900]]}]}");

        Assert.Multiple(() =>
        {
            Assert.That(conditions.Matches(Season.Spring, Weather.Sun, 3, 700, _summer), Is.True);
            Assert.That(conditions.Matches(Season.Spring, Weather.Sun, 3, 1000, _summer), Is.False);
            Assert.That(conditions.Matches(Season.Summer, Weather.Rain, 3, 2200, _summer), Is.True, "rain clause has no times: all day");
            Assert.That(conditions.Matches(Season.Spring, Weather.Sun, 3, 700, _winter), Is.True, "winter sunrise 0630-0830");
            Assert.That(conditions.Matches(Season.Spring, Weather.Sun, 3, 840, _winter), Is.False);
            Assert.That(conditions.TimesOn(Season.Summer, Weather.Rain, 3, _summer), Is.Empty, "a matching clause without times means all day");
            Assert.That(conditions.TimesOn(Season.Spring, Weather.Sun, 3, _winter), Is.EqualTo(new[] { new IntRange(630, 830) }));
            Assert.That(conditions.MatchesDay(Season.Fall, Weather.Sun, 3), Is.False);
        });
    }

    [Test]
    public void TimesOn_UnionsMatchingClausesAndKeepsEmptyWindows()
    {
        (SpawnConditions conditions, _) = Parse(@"{""any"":[{""times"":[[""dark"",2600]]},{""times"":[[700,800]]},{""times"":[[""dusk+150"",""dark""]]}]}");

        IReadOnlyList<IntRange> windows = conditions.TimesOn(Season.Spring, Weather.Sun, 1, _summer);

        Assert.That(windows, Is.EqualTo(new[] { new IntRange(2000, 2600), new IntRange(700, 800), new IntRange(2030, 2000) }));
        Assert.That(windows[2].Contains(2010), Is.False, "an empty window never contains a time");
    }

    [Test]
    public void Unconditional_ClauseOrEmptyWhenMatchesEverything()
    {
        Assert.Multiple(() =>
        {
            Assert.That(SpawnConditions.Always.Matches(Season.Winter, Weather.Snow, 28, 2600, _winter), Is.True);
            Assert.That(Parse(@"{""any"":[{""seasons"":[""spring""]},{}]}").Conditions.IsUnconditional, Is.True);
            Assert.That(ConditionParser.Describe(SpawnConditions.Always), Is.EqualTo("always"));
        });
    }

    [Test]
    public void Describe_JoinsClausesWithOr()
    {
        (SpawnConditions conditions, _) = Parse(@"{""any"":[{""seasons"":[""spring""],""times"":[[""sunrise"",""sunrise+120""]]},{""weather"":[""rain""]}]}");

        Assert.That(ConditionParser.Describe(conditions), Is.EqualTo("seasons Spring | times sunrise–sunrise+120  OR  weather Rain"));
    }

    [Test]
    public void RareAction_MatchesAgainstWorldAnchors()
    {
        (SpawnConditions conditions, _) = Parse(@"{""times"":[[""dusk"",""dark""]]}");
        RareAction rare = new("song", 0.1, conditions);

        Assert.Multiple(() =>
        {
            Assert.That(rare.Matches(new IdleWorld(Season.Spring, Weather.Sun, 1, 1700, _winter)), Is.True);
            Assert.That(rare.Matches(new IdleWorld(Season.Spring, Weather.Sun, 1, 1700, _summer)), Is.False);
            Assert.That(rare.Matches(new IdleWorld(Season.Spring, Weather.Sun, 1, 1900)), Is.True, "no anchors: default 1800–2000");
            Assert.That(rare.Matches(null), Is.False);
        });
    }
}
