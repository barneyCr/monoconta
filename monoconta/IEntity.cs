using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using monoconta.Contracts;

namespace monoconta
{
    [DebuggerDisplay("{Name}, {ID}, {Money}")]
	public abstract class Entity
	{
        static Entity() {
            OrderPropertiesByID = true;
        }
        public static bool OrderPropertiesByID { get; set; }


        /// <summary>
        /// Unique Identifier.
        /// </summary>
        /// <value>The identifier.</value>
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
		public Dictionary<Entity, double> Liabilities { get; set; }
        public List<Deposit> Deposits { get; set; }


        /// <summary>
        /// Includes loans made, deposits, shares in other companies and all real estate assets. Excludes cash.
        /// </summary>
        /// <value>The total asset value.</value>
        public double TotalAssetValue
		{
			get
            {
                double loansMade = this.CashLoansMadeValue;
                double deposits = this.DepositsValue;
                double shares = this.SharesInOtherCompaniesValue;
                double sharesLent = this.SharesLentToOthersValue; 
                double reAssets = this.RealEstateAssetsValue;

                double goldValue = GoldManager.GetGoldBarsValue(this);

                return loansMade + deposits + shares + sharesLent + reAssets + (goldValue > 0 ? goldValue : 0);
            }
        }

        /// <summary>
        /// Made up of bank debt and debts owed to other players.
        /// </summary>
        /// <value>The total liabilities value.</value>
        public double TotalLiabilitiesValue
		{
            get
            {
                double goldValue = GoldManager.GetGoldBarsValue(this);
                return LiabilityTowardsBank + Liabilities.Sum(pair => pair.Value) + ShortedSharesValue + (goldValue < 0 ? -goldValue : 0);
            }
		}
         /// <summary>
         /// Does not include shares lent to others.
         /// </summary>
         /// <value>The shares in other companies value.</value>
        public double SharesInOtherCompaniesValue
        {
            get
            {
                return MainClass.Entities.OfType<Company>().Where(entity => entity.ShareholderStructure.ContainsKey(this)).Sum(entity => entity.ShareholderStructure[this] * entity.ShareValue);
            }
        }

        public double DepositsValue
        {
            get
            {
                return this.Deposits.Sum(deposit => deposit.CurrentCapitalBase);
            }
        }

        public double CashLoansMadeValue
        {
            get
            {
                return MainClass.Entities.SelectMany(entity => entity.Liabilities).Where(debt => debt.Key == this).Sum(debt => debt.Value);
            }
        }

        public IEnumerable<ShortSellingStructure> ShortedShareStructures {
            get
            {
                return MainClass.Entities.OfType<Company>().SelectMany(c => c.GetShortSellers().Where(structure => structure.ShortSeller == this));
            }
        }
        /// <summary>
        /// Positive value!
        /// </summary>
        /// <value>The shorted shares value.</value>
        public double ShortedSharesValue
        {
            get
            {
                return ShortedShareStructures.Sum(act => act.ShareCount * act.ShortedCompany.ShareValue);
            }
        }

        /// <summary>
        /// Gets the shorted shares lent value.
        /// </summary>
        /// <value>The shorted shares lent value.</value>
        public double SharesLentToOthersValue {
            get
            {
                return MainClass.Entities.OfType<Company>().
                    Where(company =>
                    company.ShortSellingActivity.Any(pair => pair.Key.Key == this)).
                    Sum(
                        company => company.ShareValue * company.ShortSellingActivity.Where(
                            pair => pair.Key.Key == this).Sum(pair => pair.Value));
            }
        }

        /// <summary>
        /// Includes properties and options.
        /// </summary>
        /// <value>The real estate assets' value.</value>
        public double RealEstateAssetsValue {
            get{
                double properties = MainClass.Properties.Where(prop=>prop.Owner == this).Sum(prop => prop.Value);
                double options = MainClass.Properties.Where(prop => prop.OptionOwner == this).Sum(prop => prop.OptionValue);
                return properties + options;
            }
        }

        public double NetWorth
        {
            get
            {
                return TotalAssetValue + Money - TotalLiabilitiesValue;
            }
        }
        public double CreditExtended
        {
            get
            {
                return CashLoansMadeValue + SharesLentToOthersValue;
            }
        }


        public virtual void PrintStructure() {
			//...
		}

        public virtual void OnPassedStart()
        {
            this.PassedStartCounter++;

            var interestRateSwaps = MainClass.InterestRateSwapContracts.Where(swap => swap.ShortParty == this);
            foreach (var swap in interestRateSwaps)
            {
                swap.Call();
            }

            ManageRentSwaps();
            ManageRentInsurances();
        }

        private void ManageRentSwaps()
        {
            List<RentSwapContract> rentContractsToRemove = new List<RentSwapContract>();
            foreach (var rentSwap in MainClass.RentSwapContracts.Where(swap => swap.ShortParty == this))
            {
                if (!rentSwap.PassedStartEvent())
                {
                    rentSwap.DescribeSpecific();
                    Console.WriteLine("\n\tContract {0} has approached the end. Reenact?", rentSwap.Name);
                    if (Console.ReadLine() != "yes")
                    {
                        rentContractsToRemove.Add(rentSwap);
                        Console.WriteLine("Contract terminated.");
                    }
                    else
                    {
                        Console.WriteLine("Contract re-enacted for another {0} rounds", rentSwap.Terms.Rounds);
                        rentSwap.Terms.Rounds *= 2;
                    }
                }
            }
            MainClass.RentSwapContracts.RemoveAll(rsw => rentContractsToRemove.Contains(rsw));
        }
        private void ManageRentInsurances()
        {
            List<RentInsuranceContract> rentInsurancesToRemove = new List<RentInsuranceContract>();
            foreach (var rentInsurance in MainClass.RentInsuranceContracts.Where(swap => swap.InsuredShortParty == this))
            {
                if (!rentInsurance.PassedStartEvent())
                {
                    rentInsurance.DescribeSpecific();
                    Console.WriteLine("\n\tContract {0} has approached the end. Reenact?", rentInsurance.Name);
                    if (Console.ReadLine() != "yes")
                    {
                        rentInsurancesToRemove.Add(rentInsurance);
                        Console.WriteLine("Contract terminated.");
                    }
                    else
                    {
                        Console.WriteLine("Contract re-enacted for another {0} rounds", rentInsurance.Terms.Rounds);
                        rentInsurance.Terms.Rounds *= 2;
                    }
                }
            }
            MainClass.RentInsuranceContracts.RemoveAll(rsw => rentInsurancesToRemove.Contains(rsw));
        }

        public virtual void RegisterBook() {
			
		}

        public void DoLiabilitiesOnPass(string command)
        {
            double bankRate = MainClass.InterestRateBase / (this == MainClass.admin && command.ToLower() == "passs" ? 2.25 : 2);

            double bankInterest = this.LiabilityTowardsBank * (bankRate / 100);
            double goldBankInterestCredit = GoldManager.CalculateBankInterestReduction(this);

            double modifiedBankInterest = bankInterest - goldBankInterestCredit;
            if (modifiedBankInterest > 0)
                this.LiabilityTowardsBank += modifiedBankInterest;

            foreach (var creditor in this.Liabilities.Keys.ToList())
            {
                this.Liabilities[creditor] *= ((MainClass.InterestRateBase / 3) / 100 + 1);
            }
        }

        public double ReceiveInterestOnGold()
        {
            double goldInterestReceived = GoldManager.CalculateGoldInterestReceive(this);
            this.Money += goldInterestReceived;
            return goldInterestReceived;
        }

        public double GetDirectLiabilitiesInterest()
        {
            double totalLiab = TotalLiabilitiesValue;
            double mainRate = MainClass.InterestRateBase;
            double cost = LiabilityTowardsBank * mainRate / 200 + Liabilities.Sum(p => p.Value) * mainRate / 300 + ShortedSharesValue * MainClass.SSFR18 / 324 * 151 / 117 * mainRate / 100;
            return cost / totalLiab * 100;
         }
         
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
            var particularSharesOwed = from comp in MainClass.Entities.OfType<Company>()
                                       from shortDict in comp.ShortSellingActivity
                                       where shortDict.Key.Value == this
                                       select new { Company = comp, Information = shortDict };
            foreach (var item in particularSharesOwed)
            {
                Console.WriteLine("\t{0} shares of {1} towards {2} [{3:C}]", item.Information.Value, item.Company.Name, item.Information.Key.Key.Name, item.Information.Value * item.Company.ShareValue);
            }

            Console.WriteLine("GOLD bars owned: {0:F2} kg x {1:C} = {2:C}",
                GoldManager.GetGoldBarsNumberOwned(this),
                GoldManager.CurrentGoldPrice,
                GoldManager.GetGoldBarsValue(this));
            Console.WriteLine("\tbank interest reduction: {0:C}", GoldManager.CalculateBankInterestReduction(this));
            Console.WriteLine("\tinterest on bars: {0:C}", GoldManager.CalculateGoldInterestReceive(this));
            if (GoldManager.GetGoldBarsNumberOwned(this) > 0)
                financialAssets += GoldManager.GetGoldBarsValue(this);
            else financialLiabilities += -GoldManager.GetGoldBarsValue(this);


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
            var particularSharesLent = from comp in MainClass.Entities.OfType<Company>()
                                       from shortDict in comp.ShortSellingActivity
                                       where shortDict.Key.Key == this
                                       select new { Company = comp, Information = shortDict };
            foreach (var item in particularSharesLent)
            {
                Console.WriteLine("\t{0} shares of {1} towards {2} [{3:C}]", item.Information.Value, item.Company.Name, item.Information.Key.Value.Name, item.Information.Value * item.Company.ShareValue);
                // shares lent value added below, in shares in companies
            }

            Console.WriteLine("Deposits:\t\t[{0:C}]", this.Deposits.Sum(dep=>dep.CurrentCapitalBase));
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
                    int sharesLent = company.GetSharesLent(this);
                    double sharesOwned = company.ShareholderStructure[this];
                    double sharePrice = company.ShareValue;
                    double ownershipPercentage = sharesOwned * 100 / company.ShareCount;
                    Console.WriteLine("\t{0} shares of {1}, [x{2:C} = {3:C}]\t[{4:F2}%]\t{5}", 
                        sharesOwned, 
                        company.Name, 
                        sharePrice, 
                        sharesOwned * sharePrice,
                        ownershipPercentage,
                        sharesLent > 0 ? string.Format("({0} shares lent)", sharesLent).PadLeft(19) : "");
                    financialAssets += (sharesOwned+sharesLent) * sharePrice;
                }
			}
			Console.WriteLine("Shares in hedge funds: ");
			foreach (var fund in MainClass.HedgeFunds)
            {
                if (fund.ShareholderStructure.ContainsKey(this))
                {
                    int sharesLent = fund.GetSharesLent(this);
                    double sharesOwned = fund.ShareholderStructure[this];
                    double sharePrice = fund.ShareValue;
                    double ownershipPercentage = sharesOwned * 100 / fund.ShareCount;
                    Console.WriteLine("\t{0} shares of {1}, [x{2:C} = {3:C}]\t[{4:F2}%]\t{5}",
                        sharesOwned,
                        fund.Name,
                        sharePrice,
                        sharesOwned * sharePrice,
                        ownershipPercentage,
                        sharesLent > 0 ? string.Format("({0} shares lent)", sharesLent).PadLeft(19) : "");
                    financialAssets += (sharesOwned + sharesLent) * sharePrice;

                }
            }
            Console.WriteLine("Shorted shares: ");
            foreach (ShortSellingStructure shortAct in this.ShortedShareStructures)
            {
                double sharesShorted = shortAct.ShareCount;
                double sharePrice = shortAct.ShortedCompany.ShareValue;

                Console.WriteLine("\t{0} shares of {1}, [x{2:C} = {3:C}]\t[{4:F2}%]\t(SSI = {5:C})",
                    shortAct.ShareCount,
                    shortAct.ShortedCompany.Name,
                    sharePrice,
                    sharesShorted * sharePrice,
                    sharesShorted / shortAct.ShortedCompany.ShareCount * 100,
                    sharesShorted * sharePrice * MainClass.SSFR18 / 324 * 151 / 117 * MainClass.InterestRateBase / 100
                    );
                financialLiabilities += (sharesShorted) * sharePrice;
                costOfCapital += sharesShorted * sharePrice * MainClass.SSFR18 / 324 * 151 / 117 * MainClass.InterestRateBase / 100;
            }

            Console.WriteLine("Real estate assets: ");
            foreach (var ppty in MainClass.Properties.OrderBy(prop=>OrderPropertiesByID ? prop.ID : prop.ParentID))
            {
                if (ppty.Owner == this)
                {
                    Console.WriteLine("\t[{0}] {1} in {2} neighbourhood, valued at {3:C}\t{4}", 
                                      ppty.ID, 
                                      ppty.Name, 
                                      ppty.Neighbourhood.Name, 
                                      ppty.Value,
                                      ppty.CanBeBuiltOn ? (ppty.HasBuildings ? "(authorized)" : "NEWLY AUTHORIZED"): "");
                }
            }
            Console.WriteLine("Real estate options: ");
            var orderedProperties = MainClass.Properties.OrderBy(prop => OrderPropertiesByID ? prop.ID : prop.ParentID);
            foreach (var ppty in orderedProperties)
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
                int sharesOwned = entity.GetSharesOwnedBy(this,false);
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
