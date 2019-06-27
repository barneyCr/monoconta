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

        private static void LoanNew(Entity debitor_ = null, int? source = null, double? sum = null, double? rate_ = null)
        {
            var debtor = debitor_ ?? ByID(ReadInt("Who is getting the loan? "));
            int from = source ?? ReadInt("From whom? ");
            double amount = sum ?? ReadDouble("Amount: ");
            if (from == 0) // bank
            {
                debtor.LiabilityTowardsBank += amount;
                debtor.Money += amount;
            }
            else
            {
                double rate = rate_ ?? ReadDouble("Rate (real): ");
                var creditor = ByID(from);
                if (debtor.LoansContracted.ContainsKey(creditor))
                {
                    List<DebtStructure> debtStructures = debtor.LoansContracted[creditor];
                    DebtStructure debt = debtStructures.FirstOrDefault(dbt => Math.Abs(dbt.InterestRate - (rate)) < .001);
                    if (debt != null)
                    {
                        debt.Sum += amount;
                    }
                    else
                    {
                        debtStructures.Add(new DebtStructure(creditor, debtor, rate, amount));
                    }
                }
                else
                {
                    debtor.LoansContracted.Add(creditor, new List<DebtStructure>() { new DebtStructure(creditor, debtor, rate, amount) });
                }
                creditor.Money -= amount;
                debtor.Money += amount;
                Console.WriteLine("Loan created");
                Console.WriteLine("IPOR(0):  {0:F3}%", CalculateIPOR(0));
                Console.WriteLine("IPOR(15): {0:F3}%", CalculateIPOR(15));
            }
        }

        private static void RepayNew(string command)
        {
            var debtor = ByID(ReadInt("Who is paying?"));
            int destination = ReadInt("Whom?");
            double amount = ReadDouble("Amount? ");
            double rate = ReadDouble("@Rate (default is @highest): ");

            if (destination == 0) // bank 
            {
                debtor.LiabilityTowardsBank -= amount;
                //if (debtor == admin && char.IsUpper(command[0]))
                //    amount *= 0.85;
                debtor.Money -= amount;
            }
            else
            {
                var creditor = ByID(destination);

                if (debtor.LoansContracted.ContainsKey(creditor))
                {

                    double maxRate = debtor.LoansContracted[creditor].Max(debt => debt.InterestRate);
                    if (rate == maxRate || rate == 0)
                    {
                        double amountRemaining = amount;

                        var loans = debtor.LoansContracted[creditor].OrderByDescending(loan => loan.InterestRate);
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
                        debtor.LoansContracted[creditor].RemoveAll(debt => debt.Sum <= 0);

                        creditor.Money += amount - amountRemaining;
                        debtor.Money -= amount - amountRemaining;
                    }
                    else
                    {
                        var loan = debtor.LoansContracted[creditor].FirstOrDefault(l => l.InterestRate == rate);
                        if (loan != null)
                        {
                            if (amount > loan.Sum)
                            {
                                creditor.Money += loan.Sum;
                                debtor.Money -= loan.Sum;
                                loan.Sum = 0;
                                Console.WriteLine("Warning: could have reached negative debt balance on account {0:F3}%", rate);
                            }
                            else
                            {
                                loan.Sum -= amount;
                                creditor.Money += amount;
                                debtor.Money -= amount;
                            }
                        }
                    }

                    Console.WriteLine("Done");
                    Console.WriteLine("IPOR(0):  {0:F3}%", CalculateIPOR(0));
                    Console.WriteLine("IPOR(15): {0:F3}%", CalculateIPOR(15));
                }
                else
                {
                    Console.WriteLine("No running debt account with this creditor!");
                }
            }
        }

        private static void RefinanceNew(string command)
        {
            var debtor = ByID(ReadInt("Who has the debt?"));
            double oldRate = ReadDouble("Old rate (real): ");

            var financier = ByID(ReadInt("Who is offering the new debt?"));
            double newRate = ReadDouble("New rate (real): ");

            int uidOriginalCreditor = ReadInt("Original creditor (0 = bank): ");
            double amount = ReadDouble("How much of the debt?");

            double commission = ReadDouble("Commission (%): ") / 100;

            if (uidOriginalCreditor == 0)
            {
                debtor.LiabilityTowardsBank -= amount;
            }
            else
            {
                var originalCreditor = ByID(uidOriginalCreditor);
                if (originalCreditor != null && debtor.LoansContracted.ContainsKey(originalCreditor))
                {
                    var loanBook = debtor.LoansContracted[originalCreditor];

                    var loan = loanBook.FirstOrDefault(debt => debt.InterestRate == oldRate);
                    loan.Sum -= amount;
                    originalCreditor.Money += amount;
                }
            }


            LoanNew(debtor, financier.ID, amount * (1 + commission), newRate);
            /*
            if (debtor.LoansContracted.ContainsKey(financier))
            {
                debtor.LoansContracted[financier] += amount * (1 + commission);

            }
            else
                debtor.Liabilities.Add(financier, amount * (1 + commission));
            financier.Money -= amount * (financier == admin && char.IsUpper(command[0]) ? .945 : 1);
            */
        }

    }
}
