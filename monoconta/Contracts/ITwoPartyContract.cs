using System;
namespace monoconta.Contracts
{
    public interface ITwoPartyContract
    {
        Entity LongParty { get; }
        Entity ShortParty { get; }
    }
}
