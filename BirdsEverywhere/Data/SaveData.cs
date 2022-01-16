using System.Collections.Generic;
using StardewModdingAPI;

namespace BirdsEverywhere
{
    public class SaveData
    {
        public HashSet<string> seenBirds { get; set; } = new HashSet<string>();
        public Dictionary<string, ObservationData> birdObservations { get; set; } = new Dictionary<string, ObservationData>();

    }

    public class ObservationData
    {
        public string observationLocation;
        public string observationTime;
        public string playerName;
        public bool feather = false;
        public bool egg = false;
        public bool picture = false;
    }
}
