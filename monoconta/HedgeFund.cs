using System;
using System.Collections.Generic;
using System.Linq;

namespace monoconta
{
    class HedgeFund : Company
    {
        public Entity Manager { get; set; }
        public Dictionary<Entity, CompStructure> CompensationRules { get; set; }
        private CompStructure _defaultComp;
        public CompStructure DefaultCompensationRules { get => _defaultComp; set => _defaultComp = value; }

        public double ManagerVoteMultiplier { get; set; }

        public HedgeFund(string name, Entity founder, double capital, double initialValue, CompStructure comp, Entity manager)
            : base(name, founder, capital, initialValue)
        {
            //NewlySubscribedFunds = ShareholderStructure.ToDictionary(pair => pair.Key, pair => 0.0);
            this.Manager = manager;
            this.CompensationRules = new Dictionary<Entity, CompStructure>() { { founder, comp } };
            if (manager != founder)
            {
                this.CompensationRules.Add(manager, comp);
            }
            this._defaultComp = comp;
            this.PreviousShareValues = new Dictionary<int, double>(100);
            this.PreviousDividendValues = new Dictionary<int, double>(100);
            this.PreviousShareValues[0] = initialValue;
            this.PreviousDividendValues[0] = 0;
            this.LastShareSplitRatio = 1; // super important
            this.ManagerVoteMultiplier = 5; // default
        }

        internal HedgeFund() : base()
        {
            this.Manager = null;
            this.LastShareSplitRatio = 1; // super important
            this.ManagerVoteMultiplier = 5; // default
            this.PreviousShareValues = new Dictionary<int, double>(100);// { { 0, 10 } };
            this.PreviousDividendValues = new Dictionary<int, double>(100);
        }

        public override int SubscribeNewShareholder(Entity entity, double capital, double premiumPctg)
        {
            int sharesBought = base.SubscribeNewShareholder(entity, capital, premiumPctg);
            if (NewlySubscribedFunds == null)
            {
                NewlySubscribedFunds = ShareholderStructure.ToDictionary(pair => pair.Key, pair => 0);
                this.CompensationRules = new Dictionary<Entity, CompStructure>
                {
                    { entity, _defaultComp }
                };
                return sharesBought; // this is founding
            }
            if (NewlySubscribedFunds.ContainsKey(entity))
                NewlySubscribedFunds[entity] += sharesBought;
            else
                NewlySubscribedFunds.Add(entity, sharesBought);
            if (!this.CompensationRules.ContainsKey(entity))
                this.CompensationRules.Add(entity, _defaultComp);

            return sharesBought;
        }

        public override void RegisterBook()
        {
            PreviousShareValues[PassedStartCounter + 1] = ShareValue;
            if (!PreviousDividendValues.ContainsKey(PassedStartCounter + 1))
            {
                PreviousDividendValues.Add(PassedStartCounter + 1, 0);
            }
        }
        public Dictionary<int, double> PreviousShareValues { get; set; }
        public double LastShareSplitRatio { private get; set; }
        public double LastFeesPaid { get; private set; }


        public Dictionary<int, double> PreviousDividendValues { get; set; }
        internal override void IssueDividend(double amountPerShare)
        {
            base.IssueDividend(amountPerShare);
            if (PreviousDividendValues.ContainsKey(PassedStartCounter + 1))
            {
                PreviousDividendValues[PassedStartCounter + 1] += amountPerShare;
            }
            else
            {
                PreviousDividendValues.Add(PassedStartCounter + 1, amountPerShare);
            }
        }

        public override void OnPassedStart()
        {
            base.OnPassedStart();
            var oldSharesDict = NewlySubscribedFunds.ToDictionary(
                pair => pair.Key, pair => ShareholderStructure.ContainsKey(pair.Key) ? (ShareholderStructure[pair.Key] - pair.Value) : 0);
            double oldSharesCount = oldSharesDict.Sum(p => p.Value);
            double oldValue = PreviousShareValues[this.PassedStartCounter == 1 ? 0 : this.PassedStartCounter];
            double newValue = (this.ShareValue + this.PreviousDividendValues[PassedStartCounter]) * this.LastShareSplitRatio; // resetat ratio la final functie
            double profitAdded = newValue - oldValue;
            double gainAsQuota = profitAdded / oldValue;

            int totalSharesTransferred = 0;
            Dictionary<Entity, int> newStructure = this.ShareholderStructure.ToDictionary(pair => pair.Key, pair => pair.Value);
            if (!newStructure.ContainsKey(Manager))
                newStructure.Add(Manager, 0);
            foreach (var holder in this.ShareholderStructure)
            {
                if (holder.Key == this.Manager)
                    continue;
                double transferableGainsQuota = this.CompensationRules[holder.Key].PerformanceFee / 100.0 * gainAsQuota;
                double holderShares = holder.Value;
                int sharesTransferedPerformanceFee = Math.Max(0, (int)Math.Ceiling(transferableGainsQuota * holderShares));
                int sharesTransferedFixedFee = Math.Max(0, (int)Math.Floor(this.CompensationRules[holder.Key].AssetFee / 100.0 * holderShares));
                int shareTransfer = sharesTransferedFixedFee + sharesTransferedPerformanceFee;
                totalSharesTransferred += shareTransfer;
                newStructure[holder.Key] -= shareTransfer;
                newStructure[Manager] += shareTransfer;
            }
            this.ShareholderStructure = newStructure;
            this.LastFeesPaid = totalSharesTransferred * ShareValue;
            Console.WriteLine("Fees amount to {0} shares, {1:C}, or {3:F2}%, hedge fund {2}", totalSharesTransferred, this.LastFeesPaid, this.Name, totalSharesTransferred * 100 / oldSharesCount);
            NewlySubscribedFunds = ShareholderStructure.ToDictionary(pair => pair.Key, pair => 0);
            this.LastShareSplitRatio = 1; // daca nu, split-ul ramane si pe urm tura
        }

        public override void BuyBackShares(Entity holder, int shares, double premiumPctg)
        {
            if (ShareholderStructure.ContainsKey(holder) && ShareholderStructure[holder] >= shares)
            {
                double valueNow = ShareValue;
                double pricePaid = shares * valueNow * (1 + premiumPctg / 100);
                holder.Money += pricePaid;
                this.Money -= pricePaid;
                ShareholderStructure[holder] -= shares;
                if (ShareholderStructure[holder] < 1 && holder != Manager)
                {
                    ShareholderStructure.Remove(holder);
                    this.CompensationRules.Remove(holder);
                    NewlySubscribedFunds.Remove(holder);
                }
            }
        }


        //private double AUM => this.NetWorth - this.GetSharesOwnedBy(this.Manager, false) * this.ShareValue;
        public override void PrintStructure()
        {
            Console.WriteLine();
            Console.WriteLine("Manager: {0}", this.Manager.Name);
            //Console.WriteLine("AUM: {0:C}", this.AUM);
            Console.WriteLine("Default fees: {0}% and {1}%", this._defaultComp.AssetFee, this._defaultComp.PerformanceFee);
            this.GetWeightedFees(out double waf, out double wpf);
            Console.WriteLine("Weighted fees: {0:F3}% and {1:F3}%", waf, wpf);

            Console.WriteLine("\tlast round's fees: {0:C}", this.LastFeesPaid);
            base.PrintStructure();
        }


        public void GetWeightedFees(out double assetFeeTotal, out double perfFeeTotal)
        {
            assetFeeTotal = 0;
            perfFeeTotal = 0;
            foreach (var shareholder in this.ShareholderStructure)
            {
                if (shareholder.Key == this.Manager)
                    continue;
                double ownershipRatio = this.ShareholderStructure[shareholder.Key] / this.ShareCount;
                CompStructure compRule = this.CompensationRules[shareholder.Key];
                assetFeeTotal += ownershipRatio * compRule.AssetFee;
                perfFeeTotal += ownershipRatio * compRule.PerformanceFee;
            }
        }

        public void ChangeShareholderFee(Entity shareholder, CompStructure comp)
        {
            if (ShareholderStructure.ContainsKey(shareholder))
            {
                this.CompensationRules[shareholder] = comp;
            }
            else
            {
                Console.WriteLine("{0} does not own any shares in {1}", shareholder.Name, this.Name);
            }
        }

        public Dictionary<Entity, int> NewlySubscribedFunds { get; set; }


        public struct CompStructure
        {
            public CompStructure(double assetFee, double performanceFee)
            {
                AssetFee = assetFee;
                PerformanceFee = performanceFee;
            }
            public double AssetFee;
            public double PerformanceFee;
        }
    }
}