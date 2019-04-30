using System;
using System.Linq;
using System.Collections.Generic;

namespace monoconta.Contracts
{
    internal class RentInsuranceContract : IDescribable, ITwoPartyContract
    {
        /// <summary>
        /// Usually the one who sells insurance
        /// </summary>
        /// <value>The long party.</value>
        public Entity InsurerLongParty { get; set; }
        /// <summary>
        /// Usually the one who buys insurance
        /// </summary>
        /// <value>The short party.</value>
        public Entity InsuredShortParty { get; set; }
        /// <summary>
        /// Contract ID
        /// </summary>
        public int ID;
        public string Name;
        public ContractTerms Terms;
        public double InsuredSum;
        public int PropertyID;
        public int RoundsPassed;
        //public double RoundPremiumMultiplier;

        public Dictionary<int, ContractRecord> ContractEntries { get; set; }
        public string Type { get => "Rent Insurance Contract"; }


        public RentInsuranceContract(string name, Entity insurerParty, Entity insuredParty, int propertyID, double policyPremium, double insuredSum, /*double roundMultiplier,*/ int rounds)
        {
            this.InsurerLongParty = insurerParty;
            this.InsuredShortParty = insuredParty;
            this.Terms = new ContractTerms(policyPremium, rounds);
            this.InsuredSum = insuredSum;
            //this.RoundPremiumMultiplier = roundMultiplier;
            this.ID = ++Contract.ContractIDCounter;
            this.PropertyID = propertyID;
            this.Name = name;
            this.ContractEntries = new Dictionary<int, ContractRecord>();
        }

        /// <summary>
        /// Runs the event
        /// </summary>
        /// <returns><c>true</c>, if start event still has room to grow, <c>false</c> when it should be ended.</returns>
        public bool PassedStartEvent()
        {
            if (++RoundsPassed <= Terms.Rounds)
            {
                double moneyTransferred = Terms.Sum;// * Math.Pow((1 + RoundRentMultiplier / 100), RoundsPassed - 1);
                this.InsurerLongParty.Money += moneyTransferred;
                this.InsuredShortParty.Money -= moneyTransferred;
                ContractRecord record;
                this.ContractEntries.Add(ContractEntries.Count + 1, record = new ContractRecord()
                {
                    Message = string.Format("Premium of {0:C} was paid to {1} by {2}", moneyTransferred, InsurerLongParty.Name, InsuredShortParty.Name),
                    ObjectTransferred = 0,
                    ContractElement = moneyTransferred
                });
                Console.WriteLine(record.Message);
                return RoundsPassed < Terms.Rounds;
            }
            else
            {
                return false;
            }
        }

        public void PaidRentEvent()
        {
            this.InsurerLongParty.Money -= this.InsuredSum;
            this.InsuredShortParty.Money += this.InsuredSum;
            ContractRecord record = new ContractRecord()
            {
                Message = string.Format("Reimbursement of {0:C} was paid to {1} by {2}", this.InsuredSum, InsuredShortParty.Name, InsurerLongParty.Name),
                ObjectTransferred = this.InsuredSum, // todo
                ContractElement = 0
            };
            this.ContractEntries.Add(ContractEntries.Count + 1, record);
            Console.WriteLine(record.Message);
        }


        #region Interface implementation
        int IDescribable.ID => this.ID;

        Entity ITwoPartyContract.LongParty => this.InsurerLongParty;

        Entity ITwoPartyContract.ShortParty => this.InsuredShortParty;

        public string DescribeGeneral()
        {
            return string.Format("Contract \"{0}\" with ID {1}\n" +
                                "\tbetween insurer(l) {2} and insured(s) {3}\n" +
                                "\tof type {4}\n" +
                                "\tpremium {5:C}, insured sum {6:C}\n" +
                                "\trounds {7}/{8}",
                                this.Name, this.ID,
                                this.InsurerLongParty.Name, this.InsuredShortParty.Name,
                                this.Type, this.Terms.Sum, this.InsuredSum, this.RoundsPassed, this.Terms.Rounds);
        }
        public void DescribeSpecific()
        {
            Console.WriteLine(this.DescribeGeneral());
            Console.WriteLine("Transactions: ");
            foreach (var entry in this.ContractEntries)
            {
                Console.WriteLine("\t({0}): {1}", entry.Key, entry.Value.Message);
            }
            double premiumsPaid = this.ContractEntries.Sum(entry => entry.Value.ContractElement);
            double reimbursementsPaid = this.ContractEntries.Sum(entry => entry.Value.ObjectTransferred);
            Console.WriteLine("Premiums paid: {0:C} to {1}\nReimbursements paid: {2:C} to {3}", premiumsPaid, InsurerLongParty.Name, reimbursementsPaid, InsuredShortParty.Name);
        }
        #endregion
    }
}
