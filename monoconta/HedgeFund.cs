using System;
using System.Collections.Generic;
using System.Linq;

namespace monoconta
{
	class HedgeFund : Company
    {
		public Entity Manager { get; set; }
		public CompStructure Compensation { get; set; }

        public double ManagerVoteMultiplier { get; set; }

		public HedgeFund(string name, Entity founder, double capital, double initialValue, CompStructure comp, Entity manager)
			: base(name, founder, capital, initialValue)
		{
			//NewlySubscribedFunds = ShareholderStructure.ToDictionary(pair => pair.Key, pair => 0.0);
			this.Manager = manager;
			this.Compensation = comp;
			this.PreviousShareValues = new Dictionary<int, double>(100);
			this.PreviousShareValues[0] = initialValue;
            this.ManagerVoteMultiplier = 30; // default
        }

		public override int SubscribeNewShareholder(Entity entity, double capital, double premiumPctg)
		{
			int sharesBought = base.SubscribeNewShareholder(entity, capital, premiumPctg);
			if (NewlySubscribedFunds == null)
			{
				NewlySubscribedFunds = ShareholderStructure.ToDictionary(pair => pair.Key, pair => 0.0);
				return sharesBought; // this is founding
			}
			if (NewlySubscribedFunds.ContainsKey(entity))
				NewlySubscribedFunds[entity] += sharesBought;
			else
				NewlySubscribedFunds.Add(entity, sharesBought);

			return sharesBought;
		}

		public override void RegisterBook() {
			PreviousShareValues[PassedStartCounter + 1] = ShareValue;
		}
		private Dictionary<int, double> PreviousShareValues { get; set; } 

		public override void OnPassedStart()
		{
			base.OnPassedStart();
			var oldSharesDict = NewlySubscribedFunds.ToDictionary(pair => pair.Key, pair => (ShareholderStructure[pair.Key] - pair.Value));
			double oldSharesCount = oldSharesDict.Sum(p => p.Value);
			double oldValue = PreviousShareValues[this.PassedStartCounter == 1 ? 0 : this.PassedStartCounter];
			double newValue = this.ShareValue;
			double profitAdded = newValue - oldValue;
			double gainAsQuota = profitAdded / oldValue;
			double transferableGainsQuota = this.Compensation.PerformanceFee / 100.0 * gainAsQuota;

			int totalSharesTransferred = 0;
			Dictionary<Entity, double> newStructure = this.ShareholderStructure.ToDictionary(pair=>pair.Key, pair=>pair.Value);
			foreach (var holder in this.ShareholderStructure)
			{
				if (holder.Key == Manager)
					continue;
				double holderShares = holder.Value;
				int sharesTransferedPerformanceFee = Math.Max(0,(int)Math.Ceiling(transferableGainsQuota * holderShares));
				int sharesTransferedFixedFee = Math.Max(0, (int)Math.Floor(this.Compensation.AssetFee / 100.0 * holderShares));
				int shareTransfer = sharesTransferedFixedFee + sharesTransferedPerformanceFee;
				totalSharesTransferred += shareTransfer;
				newStructure[holder.Key] -= shareTransfer;
				newStructure[Manager] += shareTransfer;
			}
			this.ShareholderStructure = newStructure;
			Console.WriteLine("Fees amount to {0} shares, or {1:F2}%", totalSharesTransferred, totalSharesTransferred*100/oldSharesCount);
			NewlySubscribedFunds = ShareholderStructure.ToDictionary(pair => pair.Key, pair => 0.0);
		}

		public override void BuyBackShares(Entity holder, int shares, double premiumPctg)
		{
			if (ShareholderStructure.ContainsKey(holder) && ShareholderStructure[holder] >= shares)
            {
                double valueNow = ShareValue;
                double pricePaid = shares * valueNow * (1 - premiumPctg / 100);
                holder.Money += pricePaid;
                this.Money -= pricePaid;
                ShareholderStructure[holder] -= shares;
				if (ShareholderStructure[holder] < 1 && holder != Manager)
                {
                    ShareholderStructure.Remove(holder);
					NewlySubscribedFunds.Remove(holder);
                }
            }
		}



		public override void PrintStructure()
		{
			Console.WriteLine("Manager: {0}", this.Manager.Name);
			Console.WriteLine("Fees: {0}% and {1}%", this.Compensation.AssetFee, this.Compensation.PerformanceFee);
			base.PrintStructure();         
		}

		public Dictionary<Entity, double> NewlySubscribedFunds { get; set; }
    }

	struct CompStructure {
		public CompStructure(double assetFee, double performanceFee)
		{
			AssetFee = assetFee;
			PerformanceFee = performanceFee;
		}
		public double AssetFee;
		public double PerformanceFee;
	}
}
