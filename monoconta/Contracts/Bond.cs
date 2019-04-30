using System;
namespace monoconta.Contracts
{
    public class Bond :ITwoPartyContract,IDescribable
    {
        public Bond()
        {

        }

        public Entity LongParty => throw new NotImplementedException();

        public Entity ShortParty => throw new NotImplementedException();

        public int ID => throw new NotImplementedException();

        public string DescribeGeneral()
        {
            throw new NotImplementedException();
        }

        public void DescribeSpecific()
        {
            throw new NotImplementedException();
        }
    }
}
