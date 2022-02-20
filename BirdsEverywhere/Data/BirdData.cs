using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BirdsEverywhere
{
    public class BirdData
    {
        public string id { get; set; }
        public string name { get; set; }
        public string scName { get; set; }
        public string description { get; set; } = "";

        public string egg { get; set; } = "";
        public string breedingSeason { get; set; } = "spring";
        public string feather { get; set; } = "";
        public bool dimorphism { get; set; } = false;

        public string template { get; set; } = "";
        public List<string> seasons = new List<string>() { "spring", "summer", "fall", "winter" };

        public SpawnData spawnData { get; set; } = new SpawnData();
        public Dictionary<string, List<SpawnData>> advancedSpawn { get; set; } = new Dictionary<string, List<SpawnData>>(); // advanced spawn patters in the form season : SpawnData


    }

    public class SpawnData
    {
        public List<string> locations { get; set; } = new List<string>() { "Backwoods" }; // possible spawn locations
        public string spawnPattern { get; set; } = "SpawnableGroundSpawner"; // how to spawn the bird at location
        public string birdType { get; set; } = "LandBird"; // Bird class derivative that determines behavior
        public int minGroupCount { get; set; } = 1; //minimum amount of groups that are created
        public int maxGroupCount { get; set; } = 2;
        public int minGroupSize { get; set; } = 1; //minimum amount of birds in a group
        public int maxGroupSize { get; set; } = 5;
        public bool requireRain { get; set; } = false;
        public List<timeRange> timeOfDayRanges { get; set; } = new List<timeRange>() { new timeRange(), new timeRange() }; // during which time of the day they can spawn
        public List<timeRange> daysRanges { get; set; } = new List<timeRange>() { new timeRange(1, 30) }; // when in the season they can spawn
    }

    public class timeRange
    {
        public int minTime { get; set; } = 600;
        public int maxTime { get; set; } = 1800;

        public timeRange(){}

        public timeRange(int minTime, int maxTime)
        {
            this.minTime = minTime;
            this.maxTime = maxTime;
        }

    }
}
