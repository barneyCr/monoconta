using System;
namespace monoconta
{
    public interface IDescribable
    {
        int ID { get; }
        string DescribeGeneral();
        void DescribeSpecific();
    }
}
