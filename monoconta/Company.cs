using System;
using System.Collections.Generic;
using System.Linq;

namespace monoconta
{
    class Company : Entity
    {
        public static int ID_COUNTER_BASE;
        public Dictionary<Entity, int> ShareholderStructure { get; set; }
        public Dictionary<KeyValuePair<Entity, Entity>, double> ShortSellingActivity { get; set; }
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
            this.ShareholderStructure = new Dictionary<Entity, int>();
            this.ID = ++ID_COUNTER_BASE;
            SetInitialShareValue(initialValue);
            SubscribeNewShareholder(shareholder, capital, 0);

            this.Deposits = new List<Deposit>();
            this.Liabilities = new Dictionary<Entity, double>();
            this.ShortSellingActivity = new Dictionary<KeyValuePair<Entity, Entity>, double>();
        }

        internal Company()
        {
            this.ShareholderStructure = new Dictionary<Entity, int>();
            this.Deposits = new List<Deposit>();
            this.Liabilities = new Dictionary<Entity, double>();
            this.ShortSellingActivity = new Dictionary<KeyValuePair<Entity, Entity>, double>();
            SetInitialShareValue(1);
        }
        private double ___value;
        void SetInitialShareValue(double value)
        {
            ___value = value;
        }

        public virtual int SubscribeNewShareholder(Entity entity, double capital, double premiumPctg)
        {
            if (entity == this || (entity is Company && (entity as Company).GetSharesOwnedBy(this, false) > 0))
                return 0;
            double sharePriceAfterPremium = ShareValue * (1 + premiumPctg / 100);
            int newSharesIssued = (int)(capital / sharePriceAfterPremium);
            double paid = newSharesIssued * sharePriceAfterPremium; // we do this because 110$/(20$/share) => only 5 shares => 100$ is paid only
            entity.Money -= paid; // cap exp
            this.Money += paid; // cash infusion
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
            Console.WriteLine("\nShare value: {0:C2}\nShare count: {1}\nMarket value: {2:C}", this.ShareValue, this.ShareCount, this.ShareValue * this.ShareCount);
            Console.WriteLine("Shareholders:");
            foreach (var shareholder in this.ShareholderStructure)
            {
                int sharesLent = GetSharesLent(shareholder.Key);
                Console.WriteLine("\t{0} owns {1} shares\t[=> {2:C}]   ({3:F3}%)\t{4}\t",
                shareholder.Key.Name,
                shareholder.Value,
                shareholder.Value * this.ShareValue,
                shareholder.Value * 100 / this.ShareCount,
                sharesLent > 0 ? string.Format("({0} shares lent, {1:C})", sharesLent, sharesLent * this.ShareValue * MainClass.SSFR18 * MainClass.InterestRateBase / 324*151/117/100).PadLeft(30) : "");
            }
            Console.WriteLine("Short sellers [{0:F2}%]:", ShortSellingActivity.Sum(p => p.Value) / ShareCount * 100);

            foreach (ShortSellingStructure pair in GetShortSellers())
            {
                int sharesShorted = GetSharesShortedBy(pair.ShortSeller);
                Console.WriteLine("\t{0} shorted {1} shares\t[=> {2:C}]   ({3:F3}%)",
                pair.ShortSeller.Name,
                pair.ShareCount,
                pair.ShareCount * this.ShareValue,
                pair.ShareCount * 100 / this.ShareCount);
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
            if (buyer is Company && (buyer as Company).GetSharesOwnedBy(this, true) > 0)
                return;

            if (sharePrice < 0)
                sharePrice = this.ShareValue;
            if (GetSharesOwnedBy(holder, true) >= shareCount)
            {
                this.ShareholderStructure[holder] -= shareCount;
                if (ShareholderStructure[holder] < 1 && !managerCondition) {
                    ShareholderStructure.Remove(holder);
                    if (this is HedgeFund)
                    {
                        ((HedgeFund)this).CompensationRules.Remove(holder);
                    }
                }
                holder.Money += shareCount * sharePrice;
                if (ShareholderStructure.ContainsKey(buyer))
                    ShareholderStructure[buyer] += shareCount;
                else {
                    ShareholderStructure.Add(buyer, shareCount);
                    if (this is HedgeFund)
                    {
                        (this as HedgeFund).CompensationRules.Add(buyer, ((HedgeFund)this).DefaultCompensationRules);
                    }
                }
                buyer.Money -= shareCount * sharePrice;
            }
        }

        internal void IssueDividend(double amountPerShare)
        {
            foreach (var shareholder in this.ShareholderStructure)
            {
                double payment = GetSharesOwnedBy(shareholder.Key, false) * amountPerShare;
                shareholder.Key.Money += payment;
                Console.WriteLine("Paid {0:C} to {1}   [{2:F2}%]", payment, shareholder.Key.Name, GetOwnershipPctg(shareholder.Key, false));
            }

            foreach (var pair in GetShortSellers())
            {
                double payment = pair.ShareCount * amountPerShare;
                pair.ShortSeller.Money -= payment;
                Console.WriteLine("  Short seller {0} paid {1:C}   [{2:F2}%]", pair.ShortSeller.Name, payment, pair.ShareCount*100/this.ShareCount);
            }
            this.Money -= amountPerShare * this.ShareCount;
        }
        public int GetSharesOwnedBy(Entity shareholder, bool excludeSharesLent)
        {
            if (this.ShareholderStructure.ContainsKey(shareholder))
            {
                KeyValuePair<KeyValuePair<Entity, Entity>, double> sharesLent = this.ShortSellingActivity.FirstOrDefault(pair => pair.Key.Key == shareholder);
                return (int)ShareholderStructure[shareholder] + (int)(excludeSharesLent ?0:sharesLent.Value);
            }
            return 0;
        }

        public int GetSharesShortedBy(Entity shortSeller)
        {
            return (int)GetSharesShortedCollectionBy(shortSeller).Sum(pair => pair.Value);
        }

        public int GetSharesLent(Entity shareHolderLender)
        {
            return GetSharesOwnedBy(shareHolderLender, false) - GetSharesOwnedBy(shareHolderLender, true);
        }

        public int GetSharesLent(Entity shareHolderLender, Entity shortSeller)
        {
            return (int)ShortSellingActivity[new KeyValuePair<Entity, Entity>(shareHolderLender, shortSeller)];
        }

        public IEnumerable<KeyValuePair<KeyValuePair<Entity, Entity>, double>> GetSharesShortedCollectionBy(Entity shortSeller)
        {
            IEnumerable<KeyValuePair<KeyValuePair<Entity, Entity>, double>> sharesShortedCollection = this.ShortSellingActivity.Where(pair => pair.Key.Value == shortSeller);
            return sharesShortedCollection;
        }

        public IEnumerable<ShortSellingStructure> GetShortSellers()
        {
            return from shortAction in ShortSellingActivity
                   group shortAction by shortAction.Key.Value into shortSellerGroup
                   select new ShortSellingStructure(shortSellerGroup.Key, this, shortSellerGroup.Sum(act => act.Value));
        }

        public double GetOwnershipPctg(Entity shareholder, bool excludeSharesLent)
        {
            try
            {
                return ((double)GetSharesOwnedBy(shareholder, excludeSharesLent) / this.ShareCount) * 100;
            }
            catch { return 0; }
        }
    }
}
