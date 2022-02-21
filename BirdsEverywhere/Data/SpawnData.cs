using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BirdsEverywhere
{
    public class SpawnData
    {
        private int _textureIndex;
        private List<string> _textureList;

        public List<string> locations { get; set; } = new List<string>() { "BusStop" }; // possible spawn locations
        public string spawnPattern { get; set; } = "SpawnableGroundSpawner"; // how to spawn the bird at location
        public string birdType { get; set; } = "LandBird"; // Bird class derivative that determines behavior
        public Dictionary<string, int> textures { get; set; } = new Dictionary<string, int>(); // textures like gender, seasonal plumage, juveniles with ratio value
        public int minGroupCount { get; set; } = 1; //minimum amount of groups that are created
        public int maxGroupCount { get; set; } = 2;
        public int minGroupSize { get; set; } = 1; //minimum amount of birds in a group
        public int maxGroupSize { get; set; } = 5;
        public List<string> weather { get; set; } = new List<string>() { "sun", "rain" }; // possible weather
        public List<TimeRange> timeOfDayRanges { get; set; } = new List<TimeRange>() { new TimeRange(600, 2400), new TimeRange(600, 2400) }; // during which time of the day they can spawn
        public List<TimeRange> daysRanges { get; set; } = new List<TimeRange>() { new TimeRange(1, 28) }; // when in the season they can spawn

        public bool isDayPossible(int day) => this.daysRanges.Any(x => x.isInRange(day));
        public bool isTimeOfDayPossible(int time) => this.timeOfDayRanges.Any(x => x.isInRange(time));
        public bool isWeatherPossible(bool isRaining)
        {
            if ((isRaining && this.weather.Contains("rain")) || !isRaining && this.weather.Contains("sun"))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        // allow spawning different textures
        private string getNextTextureModifier()
        {
            string modifier = this._textureList[_textureIndex % this._textureList.Count];
            this._textureIndex++;
            return modifier;
        }

        public string getNextTexture(string id)
        {
            string modifier = getNextTextureModifier();
            if (modifier == "")
            {
                return id;
            }
            else
            {
                return $"{id}_{modifier}";
            }

        }

        public void initTextureList()
        {
            this._textureIndex = 0;
            this._textureList = getTextureListByRatio();
        }

        private List<string> getTextureListByRatio()
        {
            if (this.textures.Count <= 0)
                return new List<string>() { "" };

            List<string> textureList = new List<string>();
            foreach (KeyValuePair<string, int> kvp in this.textures)
            {
                for (int i = 0; i < kvp.Value; i++)
                {
                    textureList.Add(kvp.Key);
                }
            }
            return textureList;
        }
    }

    public class TimeRange
    {
        public int minTime { get; set; }
        public int maxTime { get; set; }

        public TimeRange(int minTime, int maxTime)
        {
            this.minTime = minTime;
            this.maxTime = maxTime;
        }

        public bool isInRange(int time)
        {
            return (time >= minTime && time <= maxTime);
        }

    }
}
