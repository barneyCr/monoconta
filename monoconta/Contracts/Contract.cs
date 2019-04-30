using System;
using System.Collections.Generic;

namespace monoconta.Contracts
{
    public delegate void OuterAction<T>(T obj);

    class Contract
    {
        public static int ContractIDCounter = 0;
        /// <summary>
        /// Usually the one who sells insurance (variable rate)
        /// </summary>
        /// <value>The long party.</value>
        public Entity LongParty { get; set; }
        /// <summary>
        /// Usually the one who buys insurance (locked rate)
        /// </summary>
        /// <value>The short party.</value>
        public Entity ShortParty { get; set; }
        public int ID;
        public string Name;
        protected double _setObject;
        protected Func<double> _getObject;
        protected Action<double, double> Event;
        protected OuterAction<Contract> Termination;
        public ContractTerms Terms;
        public int RoundsPassed;

        public Dictionary<int, ContractRecord> ContractEntries { get; set; }

        public Contract(string name, Entity longP, Entity shortP, double fixObj, Func<double> varObj, Action<double, double> act, OuterAction<Contract> termination, ContractTerms terms)
        {
            this.LongParty = longP;
            this.ShortParty = shortP;
            this._setObject = fixObj;
            this._getObject = varObj;
            this.Event = act;
            this.Termination = termination;
            this.Terms = terms;
            this.ID = ++Contract.ContractIDCounter;
            this.Name = name;
            this.ContractEntries = new Dictionary<int, ContractRecord>();
        }

        public virtual bool Call()
        {
            if (this.RoundsPassed++ >= Terms.Rounds)
            {
                if (Event != null)
                    this.Event(_setObject, _getObject());
                return true;
            }
            else
            {
                Termination(this);
                return false;
            }
        }

        public virtual string Type { get; set; }

    }

    internal struct ContractTerms
    {
        public double Sum;
        public int Rounds;

        public ContractTerms(double sum, int rounds)
        {
            Sum = sum;
            Rounds = rounds;
        }
    }

    internal struct ContractRecord
    {
        /// <summary>
        /// For example the money transferred as per the contract
        /// </summary>
        public double ObjectTransferred;
        /// <summary>
        /// For example the variable interest rate which the contract is based on
        /// </summary>
        public double ContractElement;
        /// <summary>
        /// A message containing explanations
        /// </summary>
        public string Message;

        public ContractRecord(double objectTransferred, double contractElement, string message)
        {
            ObjectTransferred = objectTransferred;
            ContractElement = contractElement;
            Message = message;
        }
    }

}