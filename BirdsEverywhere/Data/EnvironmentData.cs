using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BirdsEverywhere
{
    public class EnvironmentData
    {
        public List<Biome> biomes;
    }

    public class Biome
    {
        public string name;
        public List<string> birds;
        public List<string> locations;
    }
}
