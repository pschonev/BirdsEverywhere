using System.Collections.Generic;
using StardewModdingAPI;

namespace BirdsEverywhere
{
    public class SaveData
    {
        public List<string> unseenBirds { get; set; }
        public HashSet<string> seenBirds { get; set; } = new HashSet<string>();

    }
}
