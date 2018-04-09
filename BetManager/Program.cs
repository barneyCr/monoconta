using System;
using System.Linq;

namespace BetManager
{
    class MainClass
    {
        public static void Main(string[] args)
        {
			int[] uids = args.SelectMany(arg => arg.Split(',')).Select(s=>int.Parse(s)).ToArray();

			var pairs = from arg in args
						let pair = arg.Split(',')
						select new { ID = int.Parse(pair[0]), Sum = double.Parse(pair[1]) };

			double sum = pairs.Sum(a => a.Sum);
			var quotas = from pair in pairs
						 select new { ID = pair.ID, quota = pair.Sum / sum * 100 };
			
			int rand = new Random().Next(1, sum);
        }
    }
}
