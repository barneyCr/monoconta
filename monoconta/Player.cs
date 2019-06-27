using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace monoconta
{
	[DebuggerDisplay("{Name}, {ID}, {Money}")]
    internal class Player:Entity
    {
        public static int IDBASE = 0;
        public Player(string name, int id)
        {
            this.Name = name;
            this.ID = id;
            //Liabilities = new Dictionary<Entity, double>();
            this.LoansContracted = new Dictionary<Entity, List<DebtStructure>>();
            Deposits = new List<Deposit>();
			this.PeggedCompanies = new List<Company>();
         }

		public List<Company> PeggedCompanies = new List<Company>();


        /*
		public override void PrintSituation(bool passing)
        {
        
            PrintCash();
            double costOfCapital = 0, chargeOnCapital = 0;
            double financialAssets = 0, financialLiabilities = 0;
            Console.WriteLine("IN: {0:C}, OUT: {1:C}", MoneyIn, MoneyOut);
            Console.WriteLine("Passed start {0} times", PassedStartCounter);
            Console.WriteLine("Liabilities: ");
            Console.WriteLine("\t{0:C} towards bank", LiabilityTowardsBank);
            costOfCapital += LiabilityTowardsBank * MainClass.InterestRateBase / 100 / 2;
            financialLiabilities += LiabilityTowardsBank;
            foreach (var credit in Liabilities)
            {
                Console.WriteLine("\t{0:C} towards {1}", credit.Value, credit.Key.Name);
                costOfCapital += credit.Value * MainClass.InterestRateBase / 100 / 3;
                financialLiabilities += credit.Value;
            }

            Console.WriteLine("Loans made: ");
            var book = from player in MainClass.Players
                       from debt in player.Liabilities
                       where debt.Key == this
                       select new { Debtor = player, Debt = debt.Value };
            foreach (var credit in book)
            {
                Console.WriteLine("\t{0:C} to be received from {1}", credit.Debt, credit.Debtor.Name);
                chargeOnCapital += credit.Debt * MainClass.InterestRateBase / 100 / 3;
                financialAssets += credit.Debt;
            }

            Console.WriteLine("Deposits: ");
            foreach (var deposit in this.Deposits)
            {
                Console.WriteLine("\tPrincipal = {0:C}, InterestAcc = {1:C}, Period: {2}/{3}\t[{4}]", deposit.Principal, deposit.TotalInterest, deposit.RoundsPassed, deposit.TotalRounds, deposit.UID);
                chargeOnCapital += deposit.CurrentCapitalBase * deposit.InterestRate / 100 * (this == MainClass.admin ? MainClass._m_ : 1);
                financialAssets += deposit.CurrentCapitalBase;
            }

            double fIstrWorth = financialAssets - financialLiabilities, income = chargeOnCapital - costOfCapital;
            if (passing)
                prevCapIncome = setPrevCapIncome;

            Console.WriteLine();
            Console.WriteLine("Financial assets: {0:C}", financialAssets);
            Console.WriteLine("Financial liabilities: {0:C}", financialLiabilities);
            Console.WriteLine("Financial instruments worth: {0:C}\t[{1:C}]", fIstrWorth, fIstrWorth + Money);
            Console.WriteLine();

            Console.WriteLine("Charge on capital: {0:C}/round\t[{1:F2}%]", chargeOnCapital, Math.Abs(financialAssets) < 1 ? (chargeOnCapital / financialAssets * 100) : 0);
            Console.WriteLine("Cost of capital: {0:C}/round\t[{1:F2}%]", costOfCapital, Math.Abs(financialLiabilities) < 1 ? (costOfCapital / financialLiabilities * 100) : 0);
            Console.WriteLine("Net capital income: {0:C}/round\t\t[{2}{1:F2}%]", income, (income / prevCapIncome - 1) * 100, (income - prevCapIncome) < 0 ? "" : "+");
            setPrevCapIncome = income;


            Console.WriteLine("-----\n");
        }
        */

        /*public string Name;
        public int ID, PassedStartCounter;
        public double Money
        {
            get { return _money; }
            set
            {
                if (value > _money) MoneyIn += value - _money;
                else MoneyOut += value - _money;
                _money = value;
            }
        }
        public double MoneyIn { get; private set; }
        public double MoneyOut { get; private set; }

        public double LiabilityTowardsBank = 0;
        public Dictionary<Player, double> Liabilities;
        public List<Deposit> Deposits;

        double _money;

        public void PrintCash()
        {
            Console.WriteLine("[({0}){1}] Cash: {2:C}", ID, Name, Money);
        }*/
        /*
        double setPrevCapIncome = 0;
        double prevCapIncome = 1;
        */
    }

}
