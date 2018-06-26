﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace monoconta
{
	abstract class Entity
	{
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
				double loansMade = MainClass.Entities.SelectMany(e => e.Liabilities).Where(debt => debt.Key == this).Sum(debt => debt.Value);
				double deposits = this.Deposits.Sum(deposit => deposit.CurrentCapitalBase);
				double shares = MainClass.Entities.OfType<Partnership>().Where(ent => ent.ShareholderStructure.ContainsKey(this)).Sum(ent => ent.ShareholderStructure[this] * ent.ShareValue);
				return loansMade + deposits+shares;
			}
		}
		public double TotalLiabilitiesValue
		{
			get
			{
				return LiabilityTowardsBank + Liabilities.Sum(pair => pair.Value);
			}
		}

		public virtual void PrintStructure() {
			
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
                Console.WriteLine("\tPrincipal = {0:C}, InterestAcc = {1:C}, Period: {2}/{3}\t[{4}]", deposit.Principal, deposit.TotalInterest, deposit.RoundsPassed, deposit.TotalRounds, deposit.UID);
				chargeOnCapital += deposit.CurrentCapitalBase * deposit.InterestRate / 100 * ((this is Player) && this == MainClass.admin ? MainClass._m_ : 1);
                financialAssets += deposit.CurrentCapitalBase;
            }

			Console.WriteLine("Shares in companies: ");
			foreach (var company in MainClass.Partnerships)
			{
                if (company.ShareholderStructure.ContainsKey(this))
				{
					double sharesOwned = company.ShareholderStructure[this];
					double sharePrice = company.ShareValue;
					double ownershipPercentage = sharesOwned*100 / company.ShareCount;
					Console.WriteLine("\t{0} shares of {1}, [x{2:C} = {3:C}]\t[{4:F2}%]", sharesOwned, company.Name, sharePrice, sharesOwned*sharePrice, ownershipPercentage);
					financialAssets += sharesOwned * sharePrice;
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
            Console.WriteLine();

            Console.WriteLine("Charge on capital: {0:C}/round\t[{1:F2}%]", chargeOnCapital, Math.Abs(financialAssets) < 1 ? (100*chargeOnCapital / financialAssets) : 0);
            Console.WriteLine("Cost of capital: {0:C}/round\t[{1:F2}%]", costOfCapital, Math.Abs(financialLiabilities) < 1 ? (100*costOfCapital / financialLiabilities ) : 0);
            Console.WriteLine("Net capital income: {0:C}/round\t\t[{2}{1:F2}%]", income, (income / prevCapIncome - 1) * 100, (income - prevCapIncome) < 0 ? "" : "+");
            setPrevCapIncome = income;


            Console.WriteLine("-----\n");
		}
  
        protected double setPrevCapIncome = 0;
        protected double prevCapIncome = 1;
	}
}