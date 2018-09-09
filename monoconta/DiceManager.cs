using System;
using System.Text;

namespace monoconta
{
    public class DiceManager
    {
        private byte[] array;
        private Random randomizer;
        int pos = 0;
        public DiceManager()
        {
            array = new byte[256];
            randomizer = new Random();
            generate();
        }

        void generate() {
            Console.WriteLine("Generating random numbers...");
            randomizer.NextBytes(array);
        }

        public void GetDice(out int d1, out int d2) {
            if (pos+2>255) {
                generate();
                pos = 0;
                 GetDice(out d1,out d2);
            }
            else {
                d1 = (int)array[pos++]%6+1;
                d2 = (int)array[pos++]%6+1;
            }
        }
    }
}
