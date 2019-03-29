using System;
using System.Collections.Generic;
using System.Linq;

namespace monoconta
{
    public static class GoldManager
    {
        internal static Dictionary<Entity, double> GoldRegister { get; set; }
        internal static double[] FutureGoldPrices = new double[10];
        internal static List<double> HistoricalGoldPrices = new List<double>();
        internal static Random Randomiser = new Random();

        internal static double DownDeltaMax = -9;
        internal static double DownDeltaMin = -4;
        internal static double UpDeltaMin   = +6;
        internal static double UpDeltaMax   = +16;

        internal static double MaximumFiveDeviation = 65;

        internal static double _initialHardReadPrice;

        internal static double CurrentGoldPrice
        {
            get => FutureGoldPrices[0];
        }

        static GoldManager()
        {

        }

        public static void InitializeNew(Dictionary<Entity, double> prevRegister)
        {
            if (prevRegister != null)
            {
                FutureGoldPrices = new double[10];
                for (int i = 0; i < FutureGoldPrices.Length; i++)
                {
                    FutureGoldPrices[i] = _initialHardReadPrice;
                }
            }
            else FutureGoldPrices = new double[] { 1500, 1500, 1500, 1500, 1500, 1500, 1500, 1500, 1500, 1500 };
            for (int i = 0; i < 9; i++)
            {
                PushForNextPrice();
            }
            HistoricalGoldPrices.Clear();
            GoldRegister = prevRegister ?? MainClass.Entities.ToDictionary(entity => entity, entity => 0.0);
        }

        public static void PushForNextPrice()
        {
            int len = FutureGoldPrices.Length;
            HistoricalGoldPrices.Add(CurrentGoldPrice);
            for (int i = 0; i < len - 1; i++)
            {
                FutureGoldPrices[i] = FutureGoldPrices[i + 1];
            }
            FutureGoldPrices[len - 1] = GenerateNewPrice(FutureGoldPrices.Reverse().Take(len < 5 ? len : 5).Reverse().ToArray());
        }

        static double GenerateNewPrice(double[] previousFive)
        {
            double newPrice;
            double priceFiveChangesAgo = previousFive[0];
            double deviation;
            do
            {
                double newPriceDeviation;
                do
                {
                    newPriceDeviation = ((double)Randomiser.Next((int)(DownDeltaMax * 100), (int)(UpDeltaMax * 100))) / 100;
                }
                while (newPriceDeviation > DownDeltaMin && newPriceDeviation < UpDeltaMin);

                newPrice = previousFive[previousFive.Length - 1] * (1 + newPriceDeviation / 100);
                newPrice = Math.Round(newPrice, 2);

                deviation = Math.Abs(100 * (newPrice / priceFiveChangesAgo - 1));
            }
            while (deviation > MaximumFiveDeviation);

            return newPrice;
        }

        public static double GetGoldBarsNumberOwned(Entity goldOwner)
        {
            if (GoldRegister.ContainsKey(goldOwner))
                return GoldRegister[goldOwner];
            GoldRegister.Add(goldOwner, 0);
            return 0;
        }

        public static double GetGoldBarsValue(Entity goldOwner)
        {
            return GetGoldBarsNumberOwned(goldOwner) * CurrentGoldPrice;
        }

        public static double CalculateBankInterestReduction(Entity goldOwner)
        {
            double reductionRate = MainClass.InterestRateBase / 2 / 3.75 / 100;
            return reductionRate * GetGoldBarsValue(goldOwner);
        }

        public static double CalculateGoldInterestReceive(Entity goldOwner)
        {
            double interestRate = MainClass.InterestRateBase / 2 / 4.5 / 100;
            return interestRate * GetGoldBarsValue(goldOwner);
        }

        public static void BuyGold(Entity buyer, double dollars)
        {
            double bars = dollars / CurrentGoldPrice;
            if (GoldRegister.ContainsKey(buyer))
                GoldRegister[buyer] += bars;
            else
                GoldRegister.Add(buyer, bars);
            buyer.Money -= dollars;
        }

        public static void SellGold(Entity seller, double dollars)
        {
            double bars = dollars / CurrentGoldPrice;
            if (GoldRegister.ContainsKey(seller))
                GoldRegister[seller] -= bars;
            else
                GoldRegister.Add(seller, -bars);
            seller.Money += dollars;
        }
    }
}
