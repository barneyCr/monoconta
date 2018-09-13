using System;
using System.Text;

namespace monoconta
{
    public class DiceManager
    {
        private byte[] array;
        private readonly Random randomizer;
        int pos = 0;
        public DiceManager()
        {
            this.array = new byte[32];
            this.randomizer = new Random();
            this.Generate();
        }

        void Generate()
        {
            Console.WriteLine("Generating set of random numbers...");
            this.randomizer.NextBytes(array);
        }

        public void GetDice(out int d1, out int d2)
        {
            if (pos + 1 > 31)
            {
                this.Generate();
                this.pos = 0;
                this.GetDice(out d1, out d2);
            }
            else
            {
                d1 = (int)array[pos++] % 6 + 1;
                d2 = (int)array[pos++] % 6 + 1;
            }
        }
    }
}
