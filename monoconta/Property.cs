using System;
using System.Linq;
namespace monoconta
{
    class Property
    {
        public const double LevelMultiplier = 102.5 / 100;
        public const double ValueRentMultiplier = 3.5;

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

        public double Value
        {
            get
            {
                double r = Rent * 5;
                return r < BuyPrice ? BuyPrice : r;
            }
        }

        public double Rent
        {
            get
            {
                if (CompleteLevels == 0 && Appartments == 0 && Hotels == 0)
                {
                    return CanBeBuiltOn ? Rents[0] * 2 : Rents[0];
                }
                double overallRent = 0;
                double m = 1;
                for (int i = 0; i < CompleteLevels; i++, m *= LevelMultiplier)
                {
                    overallRent += Rents[5] * m;
                }
                double incompleteRent = Rents[Appartments] * m;
                overallRent += incompleteRent;
                return overallRent;
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
                return nbhd.All(pp => pp.Owner == this.Owner) || _authorized;
            }
        }

        public bool HasBuildings {
            get{
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

        public double GetBuildAppartmentsCost(int count) {
            return Math.Pow(1.5, CompleteLevels) * ConstructionBaseCost * count;
        }

        public void BuildAppartments(int count) {
            if (Appartments + count <= 4) {
                double cost = GetBuildAppartmentsCost(count);
                Owner.Money -= cost;
                this.Appartments += count;
            }
        }

        public void BuildHotel() {
            if (Appartments == 4) {
                double cost = GetBuildAppartmentsCost(2);
                Owner.Money -= cost;
                this.Appartments = this.Hotels = 0; // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                this.CompleteLevels++;
            }
        } 

        public void BuildWholeLevel() {
            double cost = GetBuildAppartmentsCost(6 - Appartments);
            Owner.Money -= cost;
            this.CompleteLevels++;
            this.Appartments = this.Hotels = 0;
        }

        public void SetBuildings(int levels, int app, int hotels) {
            this.CompleteLevels = levels;
            this.Appartments = app;
            this.Hotels = hotels;
        }
    }
}