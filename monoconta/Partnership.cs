using System;
using System.Collections.Generic;
using System.Linq;

namespace monoconta
{
	class Partnership : Entity
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

		public Partnership(string name, Entity shareholder, double capital, double initialValue = 10)
		{
			if (capital < 1000) {
				throw new Exception("Capital must be a minimum of 1000.");
			}
			if (initialValue < 1) {
				throw new Exception("Minimum $1 initial share price.");
			}
			this.Name = name;
			this.ShareholderStructure = new Dictionary<Entity, double>();
			this.ID = ++ID_COUNTER_BASE;
			SetInitialShareValue(initialValue);
			SubscribeNewShareholder(shareholder, capital);

			this.Deposits = new List<Deposit>();
			this.Liabilities = new Dictionary<Entity, double>();
		}
        
		private double ___value;
		void SetInitialShareValue(double value) {
			___value = value;
		}

		public void SubscribeNewShareholder(Entity entity, double capital)
		{
			double newShareCount = (int)(capital / ShareValue);
			double paid = newShareCount * ShareValue;
            entity.Money -= paid;
            this.Money += paid;
			if (ShareholderStructure.ContainsKey(entity))
			{
				ShareholderStructure[entity] += newShareCount;
			}
			else
			{
				ShareholderStructure.Add(entity, newShareCount);
			}
		}

		public override void PrintStructure()
		{
			Console.WriteLine("\nShare value: {0:C}\nShare count: {1}\nMarket value: {2:C}", this.ShareValue, this.ShareCount, this.ShareValue * this.ShareCount);
			Console.WriteLine("Shareholders:");
			foreach (var shareholder in this.ShareholderStructure)
			{
				Console.WriteLine("\t{0} owns {1} shares\t[=> {2:C}]   ({3:F2}%)", shareholder.Key.Name, shareholder.Value, shareholder.Value * this.ShareValue, shareholder.Value * 100 /this.ShareCount);
			}
		}

		public void BuyBackShares(Entity holder, int shares) {
			if (ShareholderStructure.ContainsKey(holder) && ShareholderStructure[holder] >= shares)
			{
				double valueNow = ShareValue;
				holder.Money += shares * valueNow;
				this.Money -= shares * valueNow;
				ShareholderStructure[holder] -= shares;
			}
		}
	}
}
