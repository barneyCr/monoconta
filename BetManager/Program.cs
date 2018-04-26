using System;
using System.Linq;

namespace BetManager
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            args =args.Length !=0 ? args : Console.ReadLine().Split(';');

            var pairs = from arg in args
                        let ar = arg.Split(',')
                        select new { id = int.Parse(ar[0]), bet = double.Parse(ar[1])};

            double[] array = new double[pairs.Count()];
            double c=0;
            foreach (var pair in pairs)
            {
                c+=pair.bet;
                array[pair.id-1] = c;
            }

            int rand = new Random().Next(1, (int)c);
            var winner = pairs.First(p => array[p.id-1] >= rand);

            foreach (var p in pairs)
            {
                Console.WriteLine("Chance: {0:F2}%", p.bet*100/c);
            }
            Console.WriteLine("\tWinner: {0}", winner.id);
            Console.ReadLine();
        }
    }
}
