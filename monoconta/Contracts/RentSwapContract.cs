using System;
using System.Collections.Generic;
using System.Linq;

namespace monoconta.Contracts
{
    class RentSwapContract : IDescribable
    {
        /// <summary>
        /// Usually the one who sells insurance (variable rent)
        /// </summary>
        /// <value>The long party.</value>
        public Entity LongParty { get; set; }
        /// <summary>
        /// Usually the one who buys insurance (locked rent)
        /// </summary>
        /// <value>The short party.</value>
        public Entity ShortParty { get; set; }
        /// <summary>
        /// Contract ID
        /// </summary>
        public int ID;
        public string Name;
        public ContractTerms Terms;
        public int PropertyID;
        public int RoundsPassed;
        public double RoundRentMultiplier;

        public Dictionary<int, ContractRecord> ContractEntries { get; set; }
        public string Type { get => "Rent Swap Contract"; }

        public RentSwapContract(string name, Entity longP, Entity shortP, int propertyID, double fixedRent, double rentMulti, int rounds)
        {
            this.LongParty = longP;
            this.ShortParty = shortP;
            this.Terms = new ContractTerms(fixedRent, rounds);
            this.RoundRentMultiplier = rentMulti;
            this.ID = ++Contract.ContractIDCounter;
            this.PropertyID = propertyID;
            this.Name = name;
            this.ContractEntries = new Dictionary<int, ContractRecord>();
        }

        public bool PassedStartEvent()
        {
            if (++RoundsPassed <= Terms.Rounds)
            {
                double moneyTransferred = Terms.Sum * Math.Pow((1 + RoundRentMultiplier / 100), RoundsPassed - 1);
                this.LongParty.Money -= moneyTransferred;
                this.ShortParty.Money += moneyTransferred;
                ContractRecord record;
                this.ContractEntries.Add(ContractEntries.Count + 1, record = new ContractRecord()
                {
                    Message = string.Format("Passing rent of {0:C} was paid to {1}", moneyTransferred, ShortParty.Name),
                    ObjectTransferred = moneyTransferred,
                    ContractElement = 0
                });
                Console.WriteLine(record.Message);
                return true;
            }
            else
            {
                return false;
            }
        }

        public void ReceivedRentEvent(double sum)
        {
            this.LongParty.Money += sum;
            this.ShortParty.Money -= sum;
            ContractRecord record = new ContractRecord()
                {
                    Message = string.Format("Active rent of {0:C} was paid to {1}", sum, LongParty.Name),
                    ObjectTransferred = 0,
                    ContractElement = sum
                };
            this.ContractEntries.Add(ContractEntries.Count + 1, record);
            Console.WriteLine(record.Message);
        }

        #region IDescribable implementation
        int IDescribable.ID { get => this.ID; }

        public string DescribeGeneral()
        {
            return string.Format("Contract \"{0}\" with ID {1}\n" +
                                "\tbetween long(v) {2} and short(f) {3}\n" +
                                "\tof type {4}\n" +
                                "\tlocked rent {5:C}, rounds {6}/{7}\n" +
                                "\tbumper: {8}%",
                                this.Name, this.ID,
                                this.LongParty.Name, this.ShortParty.Name,
                                this.Type, this.Terms.Sum, this.RoundsPassed, this.Terms.Rounds, this.RoundRentMultiplier);
        }
        public void DescribeSpecific()
        {
            Console.WriteLine(this.DescribeGeneral());
            Console.WriteLine("Transactions: ");
            foreach (var entry in this.ContractEntries)
            {
                Console.WriteLine("\t({0}): {1}", entry.Key, entry.Value.Message);
            }
            double passingRentPaid = this.ContractEntries.Sum(entry => entry.Value.ObjectTransferred);
            double activeRentPaid = this.ContractEntries.Sum(entry => entry.Value.ContractElement);
            Console.WriteLine("Passing rent paid: {0:C} to {1}\nActive rent paid: {2:C} to {3}", passingRentPaid, ShortParty.Name, activeRentPaid, LongParty.Name);
        }
        #endregion
    }
}
