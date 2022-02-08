using NUnit.Framework;
using StardewValley;
using StardewModdingAPI;
using Newtonsoft.Json;

namespace BirdsEverwhyere.UnitTests
{
    public class JSONConverterTests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void SendAnimatedSprite()
        {
            var sprite = new StardewValley.AnimatedSprite();
            string jsonString = JsonConvert.SerializeObject(sprite);
        }
    }
}