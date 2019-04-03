using System;
namespace monoconta.Contracts
{
    public interface IDescribable
    {
        int ID { get; }
        string DescribeGeneral();
        void DescribeSpecific();
    }
}
