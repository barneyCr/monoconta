using System;
using System.Collections.Generic;
using System.Linq;

namespace monoconta
{
	class Company : Entity
	{
		public static int ID_COUNTER_BASE;
		public Dictionary<Entity, double> ShareholderStructure { get; set; }
		public double ShareCount { get { return ShareholderStructure.Sum(p => p.Value); } }
		public double ShareValue
		{
			get
			{
				if (ShareCount > 1)
					return (TotalAssetValue - TotalLiabilitiesValue + Money) / ShareCount;
				return ___value;
			}
		}

		public Company(string name, Entity shareholder, double capital, double initialValue = 10)
		{
			if (capital < 100)
			{
				throw new Exception("Capital must be a minimum of $100.");
			}
			if (initialValue < .001)
			{
				throw new Exception("Minimum $0.001 initial share price.");
			}
			this.Name = name;
			this.ShareholderStructure = new Dictionary<Entity, double>();
			this.ID = ++ID_COUNTER_BASE;
			SetInitialShareValue(initialValue);
			SubscribeNewShareholder(shareholder, capital, 0);

			this.Deposits = new List<Deposit>();
			this.Liabilities = new Dictionary<Entity, double>();
		}

		private double ___value;
		void SetInitialShareValue(double value)
		{
			___value = value;
		}

		public virtual int SubscribeNewShareholder(Entity entity, double capital, double premiumPctg)
		{
			if (entity == this || (entity is Company && (entity as Company).GetSharesOwnedBy(this) > 0))
				return 0;
			double newSharesIssued = (int)(capital / ShareValue);
			double paid = newSharesIssued * ShareValue;
			entity.Money -= paid * (1 + premiumPctg/100);
			this.Money += paid*(1+premiumPctg/100);
			if (ShareholderStructure.ContainsKey(entity))
			{
				ShareholderStructure[entity] += newSharesIssued;
			}
			else
			{
				ShareholderStructure.Add(entity, newSharesIssued);
			}
			return (int)newSharesIssued;
		}

		public override void PrintStructure()
		{
			Console.WriteLine("\nShare value: {0:C}\nShare count: {1}\nMarket value: {2:C}", this.ShareValue, this.ShareCount, this.ShareValue * this.ShareCount);
			Console.WriteLine("Shareholders:");
			foreach (var shareholder in this.ShareholderStructure)
			{
				Console.WriteLine("\t{0} owns {1} shares\t[=> {2:C}]   ({3:F2}%)", shareholder.Key.Name, shareholder.Value, shareholder.Value * this.ShareValue, shareholder.Value * 100 / this.ShareCount);
			}
		}
        
		public virtual void BuyBackShares(Entity holder, int shares, double premiumPctg)
		{
			if (ShareholderStructure.ContainsKey(holder) && ShareholderStructure[holder] >= shares)
			{
				double valueNow = ShareValue;
                double pricePaid = shares * valueNow * (1 + premiumPctg / 100);
                holder.Money += pricePaid;
                this.Money -= pricePaid;
				ShareholderStructure[holder] -= shares;
				if (ShareholderStructure[holder] < 1)
				{
					ShareholderStructure.Remove(holder);
				}
			}
		}

		public void SellShares(Entity holder, Entity buyer, int shareCount, double sharePrice, bool managerCondition)
		{
			if (buyer == this || holder == this)
				return;
			if (buyer is Company && (buyer as Company).GetSharesOwnedBy(this) > 0)
				return;

			if (sharePrice < 0)
				sharePrice = this.ShareValue;
            if (GetSharesOwnedBy(holder) >= shareCount)
			{
				this.ShareholderStructure[holder] -= shareCount;
                if (ShareholderStructure[holder] < 1 && !managerCondition)
                    ShareholderStructure.Remove(holder);
				holder.Money += shareCount * sharePrice;
				if (ShareholderStructure.ContainsKey(buyer))
					ShareholderStructure[buyer] += shareCount;
				else 
					ShareholderStructure.Add(buyer, shareCount);
				buyer.Money -= shareCount * sharePrice;
			}
		}

		internal void IssueDividend(double amountPerShare)
		{
			foreach (var shareholder in this.ShareholderStructure)
			{
				double payment = shareholder.Value * amountPerShare;
				shareholder.Key.Money += payment;
				Console.WriteLine("Paid {0:C} to {1}   [{2:F2}%]", payment, shareholder.Key.Name, shareholder.Value * 100 / ShareCount);
			}         
			this.Money -= amountPerShare * this.ShareCount;
		}

		public int GetSharesOwnedBy(Entity shareholder) {
			if (this.ShareholderStructure.ContainsKey(shareholder))
				return (int)ShareholderStructure[shareholder];
			return 0;
		}
	}
}
