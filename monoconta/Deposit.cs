using System;
namespace monoconta
{
	class Deposit {      
        public double Principal;
        public double TotalInterest;
        public double InterestRate;
        public int TotalRounds, RoundsPassed;
        public int UID;
        
        public Deposit(double principal, double rate, int rounds, int id)
        {
            this.Principal = principal;
            this.InterestRate = rate;
            this.TotalRounds = rounds;
            this.UID = id;
        }

        public double CurrentCapitalBase 
        {
            get { return this.Principal + this.TotalInterest; }
        }

        public void RecalculateInterestRate()
        {
            this.InterestRate = CalculateDepositInterestRate(this.TotalRounds);
        }

        public static double CalculateDepositInterestRate(int rounds, bool temper = false)
        {
            double depositBase = 5 * MainClass.InterestRateBase / 18;
            double playerDepositInterestSpread = (MainClass.InterestRateBase / 3 - depositBase);
            //const double riskMultiplier = 34 / 117;
            
            double multi = temper ? MainClass._m_ : 1;
            return (depositBase + playerDepositInterestSpread * 34 / 117 * (rounds - 1))*multi;
        }
        
        public bool PassRound(bool cmd) {
            this.TotalInterest = (this.CurrentCapitalBase) * (this.InterestRate*(cmd?((double)MainClass._m_):1)/100 + 1) - this.Principal;
            return ++RoundsPassed < TotalRounds;
        }
    }
    
}
