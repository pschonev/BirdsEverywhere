using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StardewValley;

namespace BirdsEverywhere
{
    class Utils
    {
        private static double transform(int p)
        {
            return Math.Pow(Game1.random.NextDouble(), 1.0 / (double)p) * -1;
        }

        public static List<T> shuffleByWeights<T>(List<T> items, List<int> weights)
        {
            List<int> indexes = Enumerable.Range(0, items.Count).ToList();
            indexes.Sort(delegate (int x, int y) {
                return transform(weights[x]).CompareTo(transform(weights[y]));
            });
            return indexes.Select(x => items[x]).ToList();
        }
        public static T getRandomElementFromList<T>(List<T> listToPickFrom)
        {
            int index = Game1.random.Next(0, Math.Max(listToPickFrom.Count - 1, 0));
            return listToPickFrom[index];
        }
    }
}
