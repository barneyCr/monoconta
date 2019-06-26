using System;
using System.Collections.Generic;
using System.Linq;

namespace monoconta
{
    static partial class MainClass
    {
        public static double CalculateIPOR(double marginsExcludedPctg = 0)
        {
            double allLoansSummed = Entities.Sum(ent => ent.LoansContracted.Sum(list => list.Value.Sum(loan => loan.Sum)));
            double calculatedRate = 0;

            if (marginsExcludedPctg == 0)
            {
                foreach (var loan in Entities.SelectMany(ent => ent.LoansContracted.Values.SelectMany(d => d)))
                {
                    calculatedRate += (loan.Sum / allLoansSummed) * loan.InterestRate;
                }
                return calculatedRate;
            }
            else
            {
                double grossMarginAmount = allLoansSummed * marginsExcludedPctg / 100;

                var loansOrdered = from entity in Entities
                                   from loanBook in entity.LoansContracted.Values
                                   from debtStructure in loanBook
                                   orderby debtStructure.InterestRate ascending
                                   select debtStructure;
                double marginTouched = 0;
                double centralValueDecreasing = allLoansSummed - 2 * grossMarginAmount;
                foreach (var debt in loansOrdered)
                {
                    if (debt.Sum + marginTouched < grossMarginAmount)
                    {
                        marginTouched += debt.Sum;
                    }
                    else
                    {
                        if (marginTouched < grossMarginAmount && centralValueDecreasing >= 0)
                        {
                            centralValueDecreasing -= (marginTouched + debt.Sum - grossMarginAmount);
                            if (centralValueDecreasing > 0)
                                calculatedRate += (marginTouched + debt.Sum - grossMarginAmount) / (allLoansSummed) * debt.InterestRate;
                            else
                                calculatedRate += (-centralValueDecreasing) / (allLoansSummed) * debt.InterestRate;
                            marginTouched = grossMarginAmount;
                        }
                        else if (marginTouched == grossMarginAmount && centralValueDecreasing >= 0)
                        {
                            centralValueDecreasing -= debt.Sum;
                            if (centralValueDecreasing > 0)
                                calculatedRate += (debt.Sum) / (allLoansSummed) * debt.InterestRate;
                            else
                                calculatedRate += (-centralValueDecreasing) / (allLoansSummed) * debt.InterestRate;
                        }
                    }
                }
                return calculatedRate;
            }
        }

        private static void LoanNew(string command, Entity debitor_ = null, int? source = null, double? sum = null)
        {
            var debtor = debitor_ ?? ByID(ReadInt("Who is getting the loan? "));
            int from = source ?? ReadInt("From whom? ");
            double amount = sum ?? ReadDouble("Amount: ");
            if (from == 0) // bank
            {
                debtor.LiabilityTowardsBank += amount;
                if (debtor == admin && char.IsUpper(command[0]))
                    amount *= 1.1;
                debtor.Money += amount;
            }
            else
            {
                double rate = ReadDouble("Differential: ");
                var creditor = ByID(from);
                if (debtor.LoansContracted.ContainsKey(creditor))
                {
                    List<DebtStructure> debtStructures = debtor.LoansContracted[creditor];
                    DebtStructure debt = debtStructures.FirstOrDefault(db => db.InterestRate - (rate + MainClass.InterplayerBaseRate) < .001);
                    if (debt != null)
                    {
                        debt.Sum += amount;
                    }
                    else
                    {
                        debtStructures.Add(new DebtStructure(creditor, debtor, MainClass.InterplayerBaseRate + rate, amount));
                    }
                }
                else
                {
                    debtor.LoansContracted.Add(creditor, new List<DebtStructure>() { new DebtStructure(creditor, debtor, MainClass.InterplayerBaseRate + rate, amount) });
                }
                creditor.Money -= amount;
                debtor.Money += amount;
                Console.WriteLine("Done");
                Console.WriteLine("IPOR(0):  {0:F3}%", CalculateIPOR(0));
                Console.WriteLine("IPOR(15): {0:F3}%", CalculateIPOR(15));
            }
        }

        private static void RepayNew(string command)
        {
            var debtor = ByID(ReadInt("Who is paying?"));
            int destination = ReadInt("Whom?");
            double amount = ReadDouble("Amount? ");
            if (destination == 0) // bank 
            {
                debtor.LiabilityTowardsBank -= amount;
                if (debtor == admin && char.IsUpper(command[0]))
                    amount *= 0.85;
                debtor.Money -= amount;
            }
            else
            {
                var creditor = ByID(destination);

                if (debtor.LoansContracted.ContainsKey(creditor))
                {
                    var loans = debtor.LoansContracted[creditor].OrderByDescending(loan => loan.InterestRate);
                    double amountRemaining = amount;
                    foreach (var loan in loans)
                    {
                        if (loan.Sum <= amountRemaining)
                        {
                            amountRemaining -= loan.Sum;
                            loan.Sum = 0;
                        }
                        else
                        {
                            loan.Sum -= amountRemaining;
                            amountRemaining = 0;
                        }
                    }
                    debtor.LoansContracted[creditor].RemoveAll(debt => debt.Sum < 0);

                    if (amountRemaining > 0)
                        creditor.Money += amount - amountRemaining;
                    if (debtor == admin && char.IsUpper(command[0]))
                        amount *= 0.85;
                    debtor.Money -= amount - amountRemaining;

                    Console.WriteLine("Done");
                    Console.WriteLine("IPOR(0):  {0:F3}%", CalculateIPOR(0));
                    Console.WriteLine("IPOR(15): {0:F3}%", CalculateIPOR(15));
                }
            }
        }
    }
}
