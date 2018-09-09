using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;


namespace monoconta
{
    [DebuggerDisplay("{Name}, {ID}, {Money}")]
	abstract class Entity
	{
        static Entity() {
            OrderPropertiesByID = true;
        }

		public int ID { get; set; }
		public string Name { get; set; }
		public int PassedStartCounter { get; set; }

		protected double _money;
		public double MoneyIn { get; set; }
		public double MoneyOut { get; set; }
		public Double Money
		{
			get { return _money; }
			set
			{
				if (value > _money) MoneyIn += value - _money;
				else MoneyOut += value - _money;
				_money = value;
			}
		}

		public double LiabilityTowardsBank { get; set; }
		public List<Deposit> Deposits { get; set; }
		public Dictionary<Entity, double> Liabilities { get; set; }
       
		public double TotalAssetValue
		{
			get
			{
				double loansMade = MainClass.Entities.SelectMany(entity => entity.Liabilities).Where(debt => debt.Key == this).Sum(debt => debt.Value);
				double deposits = this.Deposits.Sum(deposit => deposit.CurrentCapitalBase);
				double shares = MainClass.Entities.OfType<Company>().Where(entity => entity.ShareholderStructure.ContainsKey(this)).Sum(entity => entity.ShareholderStructure[this] * entity.ShareValue);
                double reAssets = this.RealEstateAssetsValue;
                return loansMade + deposits + shares + reAssets;
			}
		}
		public double TotalLiabilitiesValue
		{
			get
			{
				return LiabilityTowardsBank + Liabilities.Sum(pair => pair.Value);
			}
		}

        public double RealEstateAssetsValue {
            get{
                double properties = MainClass.Properties.Where(prop=>prop.Owner == this).Sum(prop => prop.Value);
                double options = MainClass.Properties.Where(prop => prop.OptionOwner == this).Sum(prop => prop.OptionValue);
                return properties + options;
            }
        }

		public virtual void PrintStructure() {
			//...
		}

		public virtual void OnPassedStart() {
			this.PassedStartCounter++;         
		}

		public virtual void RegisterBook() {
			
		}


        public static bool OrderPropertiesByID { get; set; }
		public void PrintCash()
        {
            Console.WriteLine("\n-------\n[({0}) {1}] Cash: {2:C}", ID, Name, Money);
        }      
		public virtual void PrintSituation(bool passing) 
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
			var book = from entity in MainClass.Entities
                       from debt in entity.Liabilities
                       where debt.Key == this
                       select new { Debtor = entity, Debt = debt.Value };
            foreach (var credit in book)
            {
                Console.WriteLine("\t{0:C} to be received from {1}", credit.Debt, credit.Debtor.Name);
                chargeOnCapital += credit.Debt * MainClass.InterestRateBase / 100 / 3;
                financialAssets += credit.Debt;
            }

            Console.WriteLine("Deposits: ");
            foreach (var deposit in this.Deposits)
            {
                Console.WriteLine("\tPrincipal = {0:C}, InterestAcc = {1:C}, Period: {2}/{3}\t[{4}]", deposit.Principal, deposit.TotalInterest, deposit.RoundsPassed, deposit.TotalRounds, deposit.DepositID);
				chargeOnCapital += deposit.CurrentCapitalBase * deposit.InterestRate / 100 * (((this is Player) && this == MainClass.admin) || ((this is Company) && MainClass.admin != null && MainClass.admin.PeggedCompanies.Contains(this as Company)) ? MainClass._m_ : 1);
                financialAssets += deposit.CurrentCapitalBase;
            }

			Console.WriteLine("Shares in companies: ");
            foreach (var company in MainClass.Companies)
            {
                if (company.ShareholderStructure.ContainsKey(this))
                {
                    double sharesOwned = company.ShareholderStructure[this];
                    double sharePrice = company.ShareValue;
                    double ownershipPercentage = sharesOwned * 100 / company.ShareCount;
                    Console.WriteLine("\t{0} shares of {1}, [x{2:C} = {3:C}]\t[{4:F2}%]", sharesOwned, company.Name, sharePrice, sharesOwned * sharePrice, ownershipPercentage);
                    financialAssets += sharesOwned * sharePrice;
                }
			}
			Console.WriteLine("Shares in hedge funds: ");
			foreach (var fund in MainClass.HedgeFunds)
            {
                if (fund.ShareholderStructure.ContainsKey(this))
                {
                    double sharesOwned = fund.ShareholderStructure[this];
                    double sharePrice = fund.ShareValue;
                    double ownershipPercentage = sharesOwned * 100 / fund.ShareCount;
                    Console.WriteLine("\t{0} shares of {1}, [x{2:C} = {3:C}]\t[{4:F2}%]", sharesOwned, fund.Name, sharePrice, sharesOwned * sharePrice, ownershipPercentage);
                    financialAssets += sharesOwned * sharePrice;
                }
            }

            Console.WriteLine("Real estate assets: ");
            foreach (var ppty in MainClass.Properties.OrderBy(prop=>OrderPropertiesByID ? prop.ID : prop.ParentID))
            {
                if (ppty.Owner == this)
                {
                    Console.WriteLine("\t[{0}] {1} in {2} neighbourhood, valued at {3:C}\t{4}", 
                                      ppty.ID, 
                                      ppty.Name, 
                                      MainClass.Neighbourhoods.First(n=>ppty.ParentID==n.NID).Name, 
                                      ppty.Value,
                                      ppty.CanBeBuiltOn ? (ppty.HasBuildings ? "(authorized)" : "NEWLY AUTHORIZED"): "");
                }
            }
            Console.WriteLine("Real estate options: ");
            foreach (var ppty in MainClass.Properties.OrderBy(prop => OrderPropertiesByID ? prop.ID : prop.ParentID))
            {
                if (ppty.OptionOwner == this) {
                    Console.WriteLine("\tOption on {0} in {1} neighbourhood, valued at {2:C}", ppty.Name, ppty.Neighbourhood.Name,ppty.OptionValue);
                }
            }

            PrintStructure();
            
			double fIstrWorth = financialAssets - financialLiabilities, income = chargeOnCapital - costOfCapital;
            if (passing)
                prevCapIncome = setPrevCapIncome;

            Console.WriteLine();
            Console.WriteLine("Financial assets: {0:C}", financialAssets);
            Console.WriteLine("Financial liabilities: {0:C}", financialLiabilities);
            Console.WriteLine("Financial instruments worth: {0:C}\t[{1:C}]", fIstrWorth, fIstrWorth + Money);
            Console.WriteLine("Real estate: {0:C}\n\tGrand Total: {1:C}", RealEstateAssetsValue, fIstrWorth+Money+RealEstateAssetsValue);
            Console.WriteLine();


            double charge, cost, net;
            GetAllCashFlow(out charge, out cost, out net);

            Console.WriteLine("Charge on capital: {0:C}/round\t[{1:F2}%]", chargeOnCapital, Math.Abs(financialAssets) > 1 ? (100*chargeOnCapital / financialAssets) : 0);
            Console.WriteLine("Cost of capital: {0:C}/round\t[{1:F2}%]", costOfCapital, Math.Abs(financialLiabilities) > 1 ? (100*costOfCapital / financialLiabilities ) : 0);
            Console.WriteLine("Net capital income: {0:C}/round\t\t[{2}{1:F2}%]", income, (income / prevCapIncome - 1) * 100, (income - prevCapIncome) < 0 ? "" : "+");
            //setPrevCapIncome = income;

            Console.WriteLine("\n----");

            charge += chargeOnCapital;
            cost += costOfCapital;
            net += income; 
            Console.WriteLine("Charge on all capital: {0:C}/round\t[{1:F2}%]", charge, Math.Abs(financialAssets) > 1 ? (100*charge / financialAssets) : 0);
            Console.WriteLine("Cost of all capital: {0:C}/round\t[{1:F2}%]", cost, Math.Abs(financialLiabilities) > 1 ? (100*cost / financialLiabilities ) : 0);
            Console.WriteLine("Net capital income: {0:C}/round\t\t[{2}{1:F2}%]", net, (net / prevCapIncome - 1) * 100, (net - prevCapIncome) < 0 ? "" : "+");
            setPrevCapIncome = net;


            Console.WriteLine("-----\n");
		}
        
        private void GetAllCashFlow(out double charge, out double cost, out double net)
        {
            double chg = 0, cst = 0, nt = 0;
            foreach (Company entity in MainClass.Entities.OfType<Company>())
            {
                int sharesOwned = entity.GetSharesOwnedBy(this);
                if (sharesOwned > 0)
                {
                    double pctg = ((double)sharesOwned) / ((double)entity.ShareCount);
                    double chg_, cst_, nt_;
                    entity.GetAllCashFlow(out chg_, out cst_, out nt_);
                    chg_ *= pctg;
                    cst_ *= pctg;
                    nt_ *= pctg;
                    double depCharge = 0, creditCharge = 0, costCharge = 0;
                    foreach (var deposit in entity.Deposits)
                    {
                        depCharge += deposit.CurrentCapitalBase * deposit.InterestRate / 100 * ((MainClass.admin != null && MainClass.admin.PeggedCompanies.Contains(this as Company)) ? MainClass._m_ : 1);
                    }
                    depCharge *= pctg;
                    var book = from e in MainClass.Entities
                               from debt in e.Liabilities
                               where debt.Key == this
                               select new { Debtor = entity, Debt = debt.Value };
                    foreach (var credit in book)
                    {
                        //Console.WriteLine("\t{0:C} to be received from {1}", credit.Debt, credit.Debtor.Name);
                        creditCharge += credit.Debt * MainClass.InterestRateBase / 300;
                    }
                    creditCharge *= pctg;
                    foreach (var debts in entity.Liabilities)
                    {
                        costCharge += debts.Value * MainClass.InterestRateBase / 300;
                    }
                    costCharge += entity.LiabilityTowardsBank * MainClass.InterestRateBase / 200;
                    costCharge *= pctg;
                    chg += depCharge;
                    chg += creditCharge;
                    cst += costCharge;
                }
            }
            nt = chg - cst;
            charge = chg;
            cost = cst;
            net = nt;
        }


        protected double setPrevCapIncome = 0;
        protected double prevCapIncome = 1;
	}
}
