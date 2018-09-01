using System;
using System.Linq;
namespace monoconta
{
    class Property
    {
        public static double LevelRentMultiplier;
        public static double BalanceSheetValueMultiplier;
        public static double LevelCostMultiplier;
        public static double LevelCostReductionPace;
        public static bool LevelCostReduction;

        public Property()
        {

        }
        public int ID { get; set; }
        public Entity Owner { get; set; }
        public string Name { get; set; }
        //public double Value { get; set; }
        public Entity OptionOwner { get; set; }
        public int CompleteLevels { get; set; }
        public int Appartments { get; set; }
        public int Hotels { get; set; }
        public int ParentID { get; set; }
        public string Type { get; set; }
        public double[] Rents { get; set; }
        public double BuyPrice { get; set; }
        public double ConstructionBaseCost { get; set; }
        public double RentFlowIn = 0;
        public double MoneyFlowOut = 0;

        public double Value
        {
            get
            {
                double r = ResidentialRent * BalanceSheetValueMultiplier;
                double op = OptionValue * 9 / 7;
                return Max(r, op, BuyPrice);
            }
        }

        public double OptionValue
        {
            get
            {
                int owned = MainClass.Properties.Count(
                    prop => prop.Neighbourhood == this.Neighbourhood &&
                    ((prop.Owner != null && prop.Owner == this.OptionOwner) ||
                     (prop.OptionOwner != null && prop.OptionOwner == this.OptionOwner) ||
                     (prop.OptionOwner != null && prop.OptionOwner == this.Owner) ||
                     (prop.Owner != null && prop.Owner == this.Owner))
                );
                double numerator = owned * 13;
                double denominator = 6 + this.Neighbourhood.Spaces * 11 / 6; // 1.8333
                double r = ResidentialRent * BalanceSheetValueMultiplier;
                return (numerator / denominator) * (r < BuyPrice ? BuyPrice : r);
            }
        }

        public double ResidentialRent
        {
            get
            {
                if (Type == "res")
                {
                    if (CompleteLevels == 0 && Appartments == 0 && Hotels == 0)
                    {
                        return CanBeBuiltOn ? Rents[0] * 2 : Rents[0];
                    }
                    double overallRent = 0;
                    double m = 1;
                    for (int i = 0; i < CompleteLevels; i++, m *= LevelRentMultiplier)
                    {
                        overallRent += Rents[5] * m;
                    }
                    double incompleteRent = Rents[Appartments] * m;
                    overallRent += incompleteRent;
                    return overallRent;
                }
                else { return 0; }
            }
        }

        public bool _authorized = false;
        public bool CanBeBuiltOn
        {
            get
            {
                var nbhd = from ppty in MainClass.Properties
                           where ppty.ParentID == this.ParentID
                           select ppty;
                return Type == "res" && (nbhd.All(pp => pp.Owner == this.Owner) || _authorized);
            }
        }

        public bool HasBuildings
        {
            get
            {
                return CompleteLevels != 0 || Appartments != 0 || Hotels != 0;
            }
        }

        public Neighbourhood Neighbourhood
        {
            get
            {
                return MainClass.Neighbourhoods.First(nb => nb.NID == this.ParentID);
            }
        }

        public double GetBuildAppartmentsCost(int count)
        {
            double costFactor = 1;
            if (Property.LevelCostReduction)
            {
                double b = Property.LevelCostMultiplier;
                for (double i = 0; i < CompleteLevels && b >= 1; i++, b -= Property.LevelCostReductionPace)
                {
                    costFactor *= b;
                }
            }
            else
            {
                costFactor *= Math.Pow(1.5, CompleteLevels);
            }
            return costFactor * ConstructionBaseCost * count;
        }

        public void BuildAppartments(int count)
        {
            if (Appartments + count <= 4)
            {
                double cost = GetBuildAppartmentsCost(count);
                Owner.Money -= cost;
                this.MoneyFlowOut += cost;
                this.Appartments += count;
            }
        }

        public void BuildHotel()
        {
            if (Appartments == 4)
            {
                double cost = GetBuildAppartmentsCost(2);
                Owner.Money -= cost;
                this.MoneyFlowOut += cost;
                this.Appartments = this.Hotels = 0; // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                this.CompleteLevels++;
            }
        }

        public void BuildWholeLevel()
        {
            double cost = GetBuildAppartmentsCost(6 - Appartments);
            Owner.Money -= cost;
            this.MoneyFlowOut += cost;
            this.CompleteLevels++;
            this.Appartments = this.Hotels = 0;
        }

        public void SetBuildings(int levels, int app, int hotels)
        {
            this.CompleteLevels = levels;
            this.Appartments = app;
            this.Hotels = hotels;
        }

        public void SetRentFlowCounter(double rent)
        {
            this.RentFlowIn = rent;
        }

        public void SetConstructionCostCounter(double cost)
        {
            this.MoneyFlowOut = cost;
        }

        public static double Max(double a, double b, double c)
        {
            return (a < b) ? (b < c ? c : b) : (a < c ? c : a);
        }
    }
}