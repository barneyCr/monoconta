using System;
namespace monoconta
{
	class Deposit {      
        public double Principal;
        public double TotalInterest;
        public double InterestRate;
        public int TotalRounds, RoundsPassed;
        public int DepositID;
        
        public Deposit(double principal, double rate, int rounds, int id)
        {
            this.Principal = principal;
            this.InterestRate = rate;
            this.TotalRounds = rounds;
            this.DepositID = id;
        }

        public Deposit(double principal, double acc, int passedR, int totalR, int id, double irb) {
            this.Principal = principal;
            this.TotalInterest =acc;
            this.RoundsPassed = passedR;
            this.TotalRounds = totalR;
            this.DepositID = id;
            this.InterestRate = CalculateDepositInterestRate(TotalRounds, irb);
        }

        public double CurrentCapitalBase 
        {
            get { return this.Principal + this.TotalInterest; }
        }

        public void RecalculateInterestRate()
        {
            this.InterestRate = CalculateDepositInterestRate(this.TotalRounds);
        }

        public static double CalculateDepositInterestRate(int rounds, double? irb = null, bool temper = false)
        {
            double depositBase = 5 * (irb ?? MainClass.InterestRateBase) / 18;
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
