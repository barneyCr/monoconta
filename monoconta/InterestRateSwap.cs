using System;

namespace monoconta
{
    class InterestRateSwap : Contract, IDescribable
    {
        public InterestRateSwap(string name, Entity longP, Entity shortP, double fixObj, Func<double> varObj, Action<double, double> act, OuterAction<Contract> termination, ContractTerms terms)
            : base(name, longP, shortP, fixObj, varObj, act, termination, terms)
        {
        }

        public override bool Call()
        {
            if (base.Call())
            {
                double lockedInterestRate = base._setObject; // rata pe care a fixat-o contractul, pretul fix pe care il plateste asiguratul
                double currentInterestRate = base._getObject(); // rata pe care pariaza asiguratorul

                double moneyTransferredToLongParty = (currentInterestRate - lockedInterestRate) / 100 * base.Terms.Sum;

                base.LongParty.Money += moneyTransferredToLongParty;
                base.ShortParty.Money -= moneyTransferredToLongParty;

                string printMessage;
                if (moneyTransferredToLongParty > 0)
                    printMessage = String.Format("{0:C} was transferred from {1} to {2}", moneyTransferredToLongParty, LongParty.Name, ShortParty.Name);
                else
                    printMessage = String.Format("{0:C} was transferred from {1} to {2}", moneyTransferredToLongParty, ShortParty.Name, LongParty.Name);
                base.ContractEntries.Add(RoundsPassed,
                    new ContractRecord(
                        moneyTransferredToLongParty,
                        currentInterestRate,
                        printMessage));
                Console.WriteLine("Contract {0} transaction: {1}", base.Name, printMessage);
                return true;
            }
            return false;
        }

        public override string Type { get => "Lock Interest Rate Swap"; set => base.Type = value; }

        public string DescribeGeneral()
        {
            return string.Format("Contract {0} with ID {1}\n" +
                            "\tbetween long {2} and short {3}\n" +
                            "\tof type {4}\n" +
                            "\tsum {5}, rounds {6}/{7}",
                            this.Name, this.ID,
                            this.LongParty.Name, this.ShortParty.Name,
                            this.Type, this.Terms.Sum, this.RoundsPassed, this.Terms.Rounds);
        }

        public void DescribeSpecific()
        {
            Console.WriteLine(this.DescribeGeneral());
            Console.WriteLine("Transactions: ");
            foreach (var entry in this.ContractEntries)
            {
                Console.WriteLine("\t({0}): {1}", entry.Key, entry.Value.Message);
            }
        }

        int IDescribable.ID { get => base.ID; }
    }
}