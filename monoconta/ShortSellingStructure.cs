using System;
namespace monoconta
{
    public class ShortSellingStructure
    {
        internal ShortSellingStructure(Entity seller, Company shorted, double c)
        {
            this.ShortSeller = seller;
            this.ShortedCompany = shorted;
            this.ShareCount = c;
        }

        internal Entity ShortSeller;
        internal Company ShortedCompany;
        internal double ShareCount;
    }
}
