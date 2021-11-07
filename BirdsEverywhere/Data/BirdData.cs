using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BirdsEverywhere
{
    public class BirdData
    {
        public string id;
        public string name;
        public string scName;
        public string description = "";

        public string egg = "";
        public string breedingSeason = "spring";
        public string feather = "";
        public bool dimorphism = false;

        public string template = "";
        public List<string> seasons = new List<string>() { "spring", "summer", "fall", "winter" };

        public SpawnData spawnData = new SpawnData();
        public Dictionary<string, List<SpawnData>> advancedSpawn = new Dictionary<string, List<SpawnData>>(); // advanced spawn patters in the form season : SpawnData
    }

    public class SpawnData
    {
        public List<string> locations = new List<string>() { "Backwoods" }; // possible spawn locations
        public string spawnPattern = "GroundSpawner"; // how to spawn the bird at location
        public string birdType = "LandBird"; // Bird class derivative that determines behavior
        public int minGroupCount = 1; //minimum amount of groups that are created
        public int maxGroupCount = 2;
        public int minGroupSize = 3; //minimum amount of birds in a group
        public int maxGroupSize = 5; 
        public List<timeRange> timeOfDayRanges = new List<timeRange>() { new timeRange(), new timeRange() }; // during which time of the day they can spawn
        public List<timeRange> daysRanges = new List<timeRange>() { new timeRange(1, 30) }; // when in the season they can spawn
    }

    public class timeRange
    {
        public int minTime = 600;
        public int maxTime = 1800;

        public timeRange(){}

        public timeRange(int minTime, int maxTime)
        {
            this.minTime = minTime;
            this.maxTime = maxTime;
        }

    }
}
