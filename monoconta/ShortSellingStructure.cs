using System;
namespace monoconta
{
    public class ShortSellingStructure
    {
        internal ShortSellingStructure(Entity shortSeller, Company shortedCompany, double shareCount)
        {
            this.ShortSeller = shortSeller;
            this.ShortedCompany = shortedCompany;
            this.ShareCount = shareCount;
        }

        internal Entity ShortSeller;
        internal Company ShortedCompany;
        internal double ShareCount;
    }
}
