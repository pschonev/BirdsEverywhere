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

        public string template;
        public List<string> seasons = ["spring", "summer", "fall", "winter"];
        public SpawnData defaultSpawn = new SpawnData();
        public Dictionary<string, SpawnData> advancedSpawn; // advanced spawn patters in the form season : SpawnData
    }

    public class SpawnData
    {
        public List<string> locations; // possible spawn locations
        public List<string> spawnPatterns; // how to spawn the bird at location
        public string birdType; // Bird class derivative that determines behavior
        public double chance = 0.95; //chance that the bird will be added to today's birds
        public int maxGroupCount = 20; //maximum amount of groups that are created
        public int maxGroupSize = 6; //maximum amount of birds in a group
        public List<timeRange> timeOfDayRanges = new List<timeRange>() { new timeRange(), new timeRange() }; // during which time of the day they can spawn
        public List<timeRange> daysRanges = new List<timeRange>() { new timeRange(1, 30) }; // when in the season they can spawn
    }

    public class timeRange
    {
        public int minTime = 600;
        public int maxTime = 1800;

        public timeRange()
        {

        }

        public timeRange(int minTime, int maxTime)
        {
            this.minTime = minTime;
            this.maxTime = maxTime;
        }

    }
}
