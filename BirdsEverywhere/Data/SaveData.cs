using System.Collections.Generic;
using StardewModdingAPI.Utilities;

namespace BirdsEverywhere
{
    public class SaveData
    {
        public HashSet<string> seenBirds { get; set; } = new HashSet<string>();
        public Dictionary<string, ObservationData> birdObservations { get; set; } = new Dictionary<string, ObservationData>();

        /// <summary>Counts accepted observations; peers use it to detect missed updates. Saves from before it existed load as 0.</summary>
        public int logVersion { get; set; }
    }

    public class ObservationData
    {
        public string observationLocation { get; set; }
        public SDate observationDate { get; set; }
        public int observationTime { get; set; }
        public string playerName { get; set; }
        public bool feather { get; set; } = false;
        public bool egg { get; set; } = false;
        public bool picture { get; set; } = false;

        public ObservationData(string observationLocation, SDate observationDate, int observationTime, string playerName)
        {
            this.observationLocation = observationLocation;
            this.observationDate = observationDate;
            this.observationTime = observationTime;
            this.playerName = playerName;
        }
    }
}
