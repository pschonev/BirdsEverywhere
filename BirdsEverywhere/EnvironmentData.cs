using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BirdsEverywhere
{
    public class EnvironmentData
    {
        public BirdLists birds;
        public Locations locations;
    }

    public class BirdLists
    {
        public List<string> valleyBirds;
        public List<string> desertBirds;
        public List<string> islandBirds;

    }

    public class Locations
    {
        public List<string> valley;
        public List<string> desert;
        public List<string> island;

    }
}
