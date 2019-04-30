using System;
using System.Collections.Generic;
using System.Linq;
using monoconta.Contracts;
using static monoconta.HedgeFund;

namespace monoconta
{
    static partial class MainClass
    {
        private static void Dice()
        {
            int d1, d2;
            DiceManager.GetDice(out d1, out d2);
            Console.WriteLine("{0} -- {1}", d1, d2);
        }
        static void ShowStats()
        {
            void write(string s1, double d2)
            {
                Console.Write(s1.PadRight(30)); Console.WriteLine(d2.ToPaddedLeftCashString(45));
            }
            double sum(Func<Entity, double> x)
            {
                return MainClass.Entities.Sum(x);
            }

            write("Total assets:", sum(ent => ent.TotalAssetValue + ent.Money));
            write("Total illiquid assets:", sum(ent => ent.TotalAssetValue));
            write("Total liquidities:", sum(ent => ent.Money));
            write("Total liabilities:", sum(ent => ent.TotalLiabilitiesValue));
            write("Total bank debt:", sum(ent => ent.LiabilityTowardsBank));
            write("Total real money:", sum(ent => ent.NetWorth));
            write("Total real estate value:", sum(ent => ent.RealEstateAssetsValue));
        }

        static void SellShort()
        {
            Console.WriteLine("Short seller ID: ");
            Entity shortSeller = ByID(Console.ReadLine());
            Console.WriteLine("Share lender ID: ");
            Entity shareLender = ByID(Console.ReadLine());
            Console.WriteLine("Shorted company ID: ");
            Company company = ByID(Console.ReadLine()) as Company;
            Console.WriteLine("Buyer of shares ID: ");
            Entity buyer = ByID(Console.ReadLine());

            Console.WriteLine("Share count: ");
            int shareNumber = int.Parse(Console.ReadLine());

            if (company.GetSharesOwnedBy(shortSeller, false) > 0)
                Console.WriteLine("Cannot short sell a stock you own.");
            else if (company.GetSharesOwnedBy(shareLender, true) < shareNumber)
                Console.WriteLine("Lender does not have enough shares.");
            else
            {
                if (!company.ShortSellingActivity.Keys.Any(pair => pair.Key == shareLender && pair.Value == shortSeller))
                {
                    company.ShortSellingActivity.Add(new KeyValuePair<Entity, Entity>(shareLender, shortSeller), shareNumber);
                }
                else
                {
                    company.ShortSellingActivity[new KeyValuePair<Entity, Entity>(shareLender, shortSeller)] += shareNumber;
                    // KeyValuePair<T,T'>  este STRUCT !!!
                }
                // find buyer of said shares to sell to

                double sharePrice = company.ShareValue;
                if (buyer == company || shortSeller == company)
                    return;
                if (buyer is Company && (buyer as Company).GetSharesOwnedBy(company, true) > 0)
                    return;

                company.ShareholderStructure[shareLender] -= shareNumber;
                if (company.ShareholderStructure[shareLender] < 1 && (company is HedgeFund ? ((HedgeFund)company).Manager != shareLender : true))
                    company.ShareholderStructure.Remove(shareLender);
                shortSeller.Money += shareNumber * sharePrice;
                if (company.ShareholderStructure.ContainsKey(buyer))
                    company.ShareholderStructure[buyer] += shareNumber;
                else
                    company.ShareholderStructure.Add(buyer, shareNumber);
                buyer.Money -= shareNumber * sharePrice;
            }
        }
        static void CoverShort()
        {
            Console.WriteLine("Short buyer ID: ");
            Entity shortBuyer = ByID(Console.ReadLine());
            Console.WriteLine("Share lender ID: ");
            Entity shareLender = ByID(Console.ReadLine());
            Console.WriteLine("Shorted company ID: ");
            Company company = ByID(Console.ReadLine()) as Company;
            Console.WriteLine("Seller of shares ID: ");
            Entity shareSeller = ByID(Console.ReadLine());

            Console.WriteLine("Share count: ");
            int shareNumber = int.Parse(Console.ReadLine());

            if (company.GetSharesShortedBy(shortBuyer) == 0)
                Console.WriteLine("Cannot buy to cover a stock you never shorted.");
            else if (company.GetSharesLent(shareLender, shortBuyer) < shareNumber)
                Console.WriteLine("Lender did not lend enough shares.");
            else
            {
                if (company.ShortSellingActivity.Keys.Any(pair => pair.Key == shareLender && pair.Value == shortBuyer))
                {
                    var kvpair = new KeyValuePair<Entity, Entity>(shareLender, shortBuyer); // KeyValuePair<T,T'>  este STRUCT !!!
                    company.ShortSellingActivity[kvpair] -= shareNumber;
                    if (company.ShortSellingActivity[kvpair] < 1)
                        company.ShortSellingActivity.Remove(kvpair);
                }
                // find buyer of said shares to sell to

                double sharePrice = company.ShareValue;
                if (shortBuyer == company || shareLender == company || shareSeller == company || shareSeller == shortBuyer)
                    return;
                if (shareSeller is Company && (shareSeller as Company).GetSharesOwnedBy(company, true) > 0)
                    return;

                company.ShareholderStructure[shareSeller] -= shareNumber;
                if (company.ShareholderStructure[shareSeller] < 1 && (company is HedgeFund ? ((HedgeFund)company).Manager != shareSeller : true))
                    company.ShareholderStructure.Remove(shareSeller);
                shareSeller.Money += shareNumber * sharePrice;
                if (company.ShareholderStructure.ContainsKey(shareLender))
                    company.ShareholderStructure[shareLender] += shareNumber;
                else
                    company.ShareholderStructure.Add(shareLender, shareNumber);
                shortBuyer.Money -= shareNumber * sharePrice;
            }
        }


        private static void CalculateShortSellingInterest()
        {
            double baseRate = InterestRateBase;

        }

        private static void ShowRanking(bool playersOnly)
        {
            IEnumerable<Entity> collection = playersOnly ? Players : Entities;
            Console.WriteLine("By net worth: ");
            int i = 0;
            foreach (var entity in collection.OrderByDescending(p => p.NetWorth))
            {
                Console.WriteLine("\t{0}. {1} {2}", ++i, entity.Name.PadRight(30), entity.NetWorth.ToString("C").PadLeft(25));
            }
            Console.WriteLine("\nBy total assets: ");
            i = 0;
            foreach (var entity in collection.OrderByDescending(p => p.TotalAssetValue + p.Money))
            {
                Console.WriteLine("\t{0}. {1} {2}", ++i, entity.Name.PadRight(30), (entity.TotalAssetValue + entity.Money).ToString("C").PadLeft(25));
            }
            Console.WriteLine("\nBy total liabilities: ");
            i = 0;
            foreach (var entity in collection.OrderByDescending(p => p.TotalLiabilitiesValue))
            {
                Console.WriteLine("\t{0}. {1} {2}", ++i, entity.Name.PadRight(30), entity.TotalLiabilitiesValue.ToString("C").PadLeft(25));
            }
            Console.WriteLine("\nBy bank debt: ");
            i = 0;
            foreach (var entity in collection.OrderByDescending(p => p.LiabilityTowardsBank))
            {
                Console.WriteLine("\t{0}. {1} {2}", ++i, entity.Name.PadRight(30), entity.LiabilityTowardsBank.ToString("C").PadLeft(25));
            }
            Console.WriteLine("\nBy total cash: ");
            i = 0;
            foreach (var entity in collection.OrderByDescending(p => p.Money))
            {
                Console.WriteLine("\t{0}. {1} {2}", ++i, entity.Name.PadRight(30), entity.Money.ToString("C").PadLeft(25));
            }
            Console.WriteLine("\nBy total credit extended: ");
            i = 0;
            foreach (var entity in collection.OrderByDescending(p => p.CreditExtended))
            {
                Console.WriteLine("\t{0}. {1} {2}", ++i, entity.Name.PadRight(30), entity.CreditExtended.ToString("C").PadLeft(25));
            }
            Console.WriteLine("\nBy real estate values: ");
            i = 0;
            foreach (var entity in collection.OrderByDescending(CalculateRealEstateValues))
            {
                Console.WriteLine("\t{0}. {1} {2}", ++i, entity.Name.PadRight(30), CalculateRealEstateValues(entity).ToString("C").PadLeft(25));
            }
        }

        private static void ShowWealth()
        {
            Entity entity = ByID(ReadInt("Entity ID: "));
            double netWorth = entity.NetWorth;

            double cash = entity.Money;
            double sharesInOtherCompaniesValue = entity.SharesInOtherCompaniesValue;
            double reValue = entity.RealEstateAssetsValue;
            double sharesLentValue = entity.SharesLentToOthersValue;
            double cashMoneyLentValue = entity.CashLoansMadeValue;
            double depositsValue = entity.DepositsValue;

            double sharesBorrowedValue = entity.ShortedSharesValue;
            double bankDebt = entity.LiabilityTowardsBank;
            double cashMoneyDebt = entity.Liabilities.Sum(pair => pair.Value);

            double goldValue = GoldManager.GetGoldBarsValue(entity);

            double assets = entity.TotalAssetValue+cash;
            double liabilities = entity.TotalLiabilitiesValue;

            //netWorth *= 100;

            const int categoryPad = 25, valuePad = 40, percentagePad = 12;

            Console.WriteLine("\n\n");
            Console.WriteLine("Cash: ".PadRight(categoryPad) + string.Format("{0:C}", cash).PadLeft(valuePad) + (string.Format("{0:P2}", cash/netWorth)).PadLeft(percentagePad));
            Console.WriteLine("Deposits: ".PadRight(categoryPad) + string.Format("{0:C}", depositsValue).PadLeft(valuePad) + (string.Format("{0:P2}", depositsValue / netWorth)).PadLeft(percentagePad));
            Console.WriteLine("Long shares: ".PadRight(categoryPad) + string.Format("{0:C}", sharesInOtherCompaniesValue).PadLeft(valuePad) + (string.Format("{0:P2}", sharesInOtherCompaniesValue / netWorth)).PadLeft(percentagePad));
            Console.WriteLine("Shares lent: ".PadRight(categoryPad) + string.Format("{0:C}", sharesLentValue).PadLeft(valuePad) + (string.Format("{0:P2}", sharesLentValue / netWorth)).PadLeft(percentagePad));
            Console.WriteLine("Real estate assets: ".PadRight(categoryPad) + string.Format("{0:C}", reValue).PadLeft(valuePad) + (string.Format("{0:P2}", reValue / netWorth)).PadLeft(percentagePad));
            Console.WriteLine("Loans made: ".PadRight(categoryPad) + string.Format("{0:C}", cashMoneyLentValue).PadLeft(valuePad) + (string.Format("{0:P2}", cashMoneyLentValue / netWorth)).PadLeft(percentagePad));
            if (goldValue >= 0)
                Console.WriteLine("Gold owned: ".PadRight(categoryPad) + string.Format("{0:C}", goldValue).PadLeft(valuePad) + (string.Format("{0:P2}", goldValue / netWorth)).PadLeft(percentagePad));

            Console.WriteLine();

            Console.WriteLine("Shorted shares: ".PadRight(categoryPad) + string.Format("{0:C}", sharesBorrowedValue).PadLeft(valuePad) + (string.Format("{0:P2}", sharesBorrowedValue / netWorth)).PadLeft(percentagePad));
            Console.WriteLine("Debt to bank: ".PadRight(categoryPad) + string.Format("{0:C}", bankDebt).PadLeft(valuePad) + (string.Format("{0:P2}", bankDebt / netWorth)).PadLeft(percentagePad));
            Console.WriteLine("Debt to players: ".PadRight(categoryPad) + string.Format("{0:C}", cashMoneyDebt).PadLeft(valuePad) + (string.Format("{0:P2}", cashMoneyDebt / netWorth)).PadLeft(percentagePad));
            if (goldValue < 0)
                Console.WriteLine("Gold shorted: ".PadRight(categoryPad) + string.Format("{0:C}", -goldValue).PadLeft(valuePad) + (string.Format("{0:P2}", -goldValue / netWorth)).PadLeft(percentagePad));


            Console.WriteLine();

            Console.WriteLine("Net worth: ".PadRight(categoryPad) + string.Format("{0:C}", netWorth).PadLeft(valuePad) + (string.Format("{0:P2}", netWorth / assets)).PadLeft(percentagePad));
            Console.WriteLine("Total asset value: ".PadRight(categoryPad) + string.Format("{0:C}", assets).PadLeft(valuePad) + (string.Format("{0:P2}", assets / netWorth)).PadLeft(percentagePad));
            Console.WriteLine("Total liabilities value: ".PadRight(categoryPad) + string.Format("{0:C}", liabilities).PadLeft(valuePad) + (string.Format("{0:P2}", liabilities / assets)).PadLeft(percentagePad));
        }


        private static void ShowSharePerformance()
        {
            if (ByID(ReadInt("Fund ID: ")) is HedgeFund fund)
            {
                //var totalReturnValues = from shareValue in fund.PreviousShareValues
                //                        join divValue in fund.PreviousDividendValues
                //                            on shareValue.Key equals divValue.Key
                //                        select new { shareValue.Key, Value = shareValue.Value + divValue.Value };

                //Dictionary<int, double> totalReturnDictionary =
                //totalReturnValues.ToDictionary(a => a.Key, a => a.Value);

                Dictionary<int, double> totalReturnDictionary = new Dictionary<int, double>();

                foreach (var item in fund.PreviousShareValues)
                {
                    totalReturnDictionary.Add(item.Key, item.Value);
                    if (fund.PreviousDividendValues.ContainsKey(item.Key))
                        totalReturnDictionary[item.Key] += fund.PreviousDividendValues[item.Key];
                }
                foreach (var item in fund.PreviousDividendValues.Where(k => !fund.PreviousShareValues.ContainsKey(k.Key)))
                {
                    totalReturnDictionary.Add(item.Key, fund.PreviousShareValues[item.Key - 1] + item.Value);
                    // this is for the current round, for which the price is not fully set, 
                    // but we take the dividend into account
                }

                Console.WriteLine("Round 0: {0:C}", totalReturnDictionary[0]);

                for (int i = 1; i < totalReturnDictionary.Count; i++)
                {
                    double last = totalReturnDictionary[i - 1];
                    double current = totalReturnDictionary[i];
                    string sign = current >= last ? "+" : "-";
                    Console.WriteLine("Round {0}: {1:C}\t{2} {3:P2}", i.ToString().PadLeft(3), current.ToPaddedLeftCashString(10), sign, Math.Abs(current / last - 1));
                }
            }
        }

        private static void RunPermaDivManager()
        {
            if (ByID(ReadInt("Company ID: ")) is Company company)
            {
                Console.WriteLine("Perma-div is currently: {0:C}/share, state {1}", company.PermaDividendPerShare, company.PermaDividendOn ? "ON" : "OFF");
                Console.Write("Switch state?");
                if (Console.ReadLine() == "yes")
                {
                    if (company.PermaDividendOn)
                        company.StopPermaDividend();
                    else
                    {
                        company.StartPermaDividend(company.PermaDividendPerShare);

                        Console.Write("Modify amount? yes/no: ");
                        if (Console.ReadLine() == "yes")
                        {
                            double amount = ReadDouble("Amount: ");
                            company.StartPermaDividend(amount);
                        }
                    }
                }
            }
        }

        private static void ChangeGoldDeltas()
        {
            GoldManager.DownDeltaMax = ReadDouble("Down max: ");
            GoldManager.DownDeltaMin = ReadDouble("Down min: ");
            GoldManager.UpDeltaMin = ReadDouble("Up min: ");
            GoldManager.UpDeltaMax = ReadDouble("Up max: ");
            GoldManager.MaximumFiveDeviation = ReadDouble("Five range: ");
        }

        private static string Build(string command)
        {
            Property property = GetProperty(ReadInt("Property ID: "));
            if (property.CanBeBuiltOn)
            {
                Console.WriteLine("{4} has {0} levels, {1} ({2}) appartments, ({3}) hotels", property.CompleteLevels, property.Appartments, property.CompleteLevels * 4 + property.Appartments, property.CompleteLevels, property.Name);
                Console.Write("app/hotel/level: ");
                command = Console.ReadLine();
                if (command == "app")
                {
                    int count = ReadInt("How many? ");
                    Console.Write("Cost: {0:C}. Agree? ", property.GetBuildAppartmentsCost(count));
                    if (Console.ReadLine() == "yes")
                    {
                        property.BuildAppartments(count);
                    }
                }
                else if (command == "hotel")
                {
                    Console.Write("Cost: {0:C}. Agree? ", property.GetBuildAppartmentsCost(2));
                    if (Console.ReadLine() == "yes")
                    {
                        property.BuildHotel();
                    }
                }
                else if (command == "level")
                {
                    Console.Write("Cost: {0:C}. Agree? ", property.GetBuildAppartmentsCost(6));
                    if (Console.ReadLine() == "yes")
                    {
                        property.BuildWholeLevel();
                    }
                }

            }
            else
            {
                Console.WriteLine("Not authorized!");
            }

            return command;
        }

        private static void Debit(string command)
        {
            var entity = ByID(ReadInt("ID: "));
            double amount = ReadDouble("Sum: ");
            if (entity == admin && char.IsUpper(command[0]))
                amount *= 0.9125;
            entity.Money -= amount;
            if (entity.Money < 0 && financedeficit)
            {
                // let's see how we can finance this
                Console.WriteLine("Debitor is out of cash. \n\t>Loan (bank, interplayer) [loan]\n\t>Share sale [sellshares,shares,sell,sh]");
                string variant = Console.ReadLine();
                if (variant == "loan")
                {
                    int from = ReadInt("Creditor ID: ");
                    Loan("loan", entity, from, -entity.Money);
                }
                else if (variant == "sellshares" || variant == "shares" || variant == "sell" || variant == "sh")
                {
                    Company sharesOf = ByID(ReadInt("Shares of: ")) as Company;
                    double price = ReadDouble("Price: ");
                    price = price == 0 ? sharesOf.ShareValue : price;
                    int shareCount = (int)Math.Ceiling((-entity.Money) / price);
                    int shareBuyerID = ReadInt("Beneficiary ID: ");
                    SellShares(ByID(shareBuyerID), entity, sharesOf, shareCount, sharesOf.ShareValue);
                }
            }
        }

        private static void Transfer(string command, Entity beneficiary_ = null, Entity payer_ = null, double? sum_ = null)
        {
            var beneficiary = beneficiary_ ?? ByID(ReadInt("ID of person receiving money: "));
            var payer = payer_ ?? ByID(ReadInt("ID of person paying: "));
            double sum = sum_ ?? ReadDouble("Sum: ");
            beneficiary.Money += sum * (beneficiary == admin && char.IsUpper(command[0]) ? 1.125 : 1);
            payer.Money -= sum * (payer == admin && char.IsUpper(command[0]) ? 0.9 : 1);
            //beneficiary.PrintCash();
            //payer.PrintCash();
            if (payer.Money < 0 && financedeficit)
            {
                // let's see how we can finance this
                Console.WriteLine("Debitor {0} is out of cash. \n\t>Loan (bank, interplayer) [loan]\n\t>Share transfer [sellshares]", payer.Name);
                string variant = Console.ReadLine();
                if (variant == "loan")
                {
                    int from = ReadInt("Creditor ID: ");
                    Loan("loan", payer, from, -payer.Money);
                }
                else if (variant == "sellshares" || variant == "shares" || variant == "sell" || variant == "sh")
                {
                    Company sharesOf = ByID(ReadInt("Shares of: ")) as Company;
                    double price = ReadDouble("Price: ");
                    price = price == 0 ? sharesOf.ShareValue : price;
                    int shareCount = (int)Math.Ceiling((-payer.Money) / price);
                    if (sharesOf == beneficiary)
                        BuybackShares(payer, beneficiary as Company, shareCount, price / sharesOf.ShareValue * 100 - 100);
                    else
                        SellShares(beneficiary, payer, sharesOf, shareCount, sharesOf.ShareValue);
                }
            }
        }


        private static void Pass(string command)
        {
            if (ByID(ReadInt("ID: ")) is Player player)
            {
            repeat:
                player.OnPassedStart();
                player.Money += startBonus;

                player.DoLiabilitiesOnPass(command);
                Console.WriteLine("{0} received {1:C} as interest on gold", player.Name, player.ReceiveInterestOnGold());


                foreach (var deposit in player.Deposits.ToList())
                {
                    if (!deposit.PassRound(player == admin))
                    {
                        Console.WriteLine("Deposit reached term");
                        player.Money += deposit.Principal;
                        player.Money += deposit.TotalInterest;
                        Console.WriteLine("Received principal of {0:C} and total accumulated interest of {1:C}", deposit.Principal, deposit.TotalInterest);
                        player.Deposits.Remove(deposit);
                    }
                }
                if (player.PeggedCompanies.Count > 0)
                {
                    Console.Write("Show pegged entities status?");
                    bool showpegged = Console.ReadLine() == "yes";
                    foreach (var company in player.PeggedCompanies)
                    {
                        company.RegisterBook();
                        company.DoLiabilitiesOnPass(command);

                        Console.WriteLine("{0} received {1:C} as interest on gold", company.Name, company.ReceiveInterestOnGold());

                        foreach (KeyValuePair<KeyValuePair<Entity, Entity>, double> shortSale in company.ShortSellingActivity)
                        {
                            Entity lender = shortSale.Key.Key;
                            Entity shortSeller = shortSale.Key.Value;
                            int sharesLent = (int)shortSale.Value;
                            double totalSharesLentValue = sharesLent * company.ShareValue;
                            double SHORT_SALE_INTEREST_RATE_FACTOR = (1.0 /*SSFR18*/ / 18.0) / 18 * (1.0 + 34.0 / 117); // SSFR18 / 18 * 151 / 117
                            double interestRate = InterestRateBase * SHORT_SALE_INTEREST_RATE_FACTOR * SSFR18 / 100;
                            double interestPayment = totalSharesLentValue * interestRate;
                            if (interestPayment > shortSeller.Money)
                            {
                                double paid = shortSeller.Money >= 0 ? shortSeller.Money : 0;
                                lender.Money += paid;
                                shortSeller.Money = 0;
                                double differencePlusInterplayerInterest = (interestPayment - paid) * (InterestRateBase / 300 + 1);
                                if (shortSeller.Liabilities.ContainsKey(lender))
                                {
                                    shortSeller.Liabilities[lender] += differencePlusInterplayerInterest;
                                }
                                else
                                    shortSeller.Liabilities.Add(lender, differencePlusInterplayerInterest);
                            }
                            else
                            {
                                lender.Money += interestPayment;
                                shortSeller.Money -= interestPayment;
                            }
                            Console.WriteLine("{0} paid {1:C} short selling interest to {2} ({3})", shortSeller.Name, interestPayment, lender.Name, company.Name);
                        }
                        foreach (var deposit in company.Deposits.ToList())
                        {
                            if (!deposit.PassRound(player == admin && command.ToLower() == "passs"))
                            {
                                company.Money += deposit.Principal;
                                company.Money += deposit.TotalInterest;
                                if (showpegged)
                                {
                                    Console.WriteLine("Deposit {0} reached term", deposit.DepositID);
                                    Console.WriteLine("Received principal of {0:C} and total accumulated interest of {1:C}", deposit.Principal, deposit.TotalInterest);
                                }
                                company.Deposits.Remove(deposit);
                            }
                        }
                        company.OnPassedStart();
                        if (showpegged)
                            company.PrintSituation(true);
                    }
                }
                if (command.StartsWith("PASS"))
                {
                    command = null;
                    goto repeat;
                }
                player.PrintSituation(true);
                GoldManager.PushForNextPrice();
            }
        }

        private static void Loan(string command, Entity debitor = null, int? source = null, double? sum = null)
        {
            var player = debitor ?? ByID(ReadInt("Who is getting the loan? "));
            int from = source ?? ReadInt("From whom? ");
            double amount = sum ?? ReadDouble("Amount: ");
            if (from == 0) // bank
            {
                player.LiabilityTowardsBank += amount;
                if (player == admin && char.IsUpper(command[0]))
                    amount *= 1.1;
                player.Money += amount;
            }
            else
            {
                var creditor = ByID(from);
                if (player.Liabilities.ContainsKey(creditor))
                {
                    player.Liabilities[creditor] += amount;
                }
                else
                {
                    player.Liabilities.Add(creditor, amount);
                }
                creditor.Money -= amount;
                player.Money += amount;
                Console.WriteLine("Done");
            }
        }

        private static void Refinance(string command)
        {
            var debtor = ByID(ReadInt("Who has the debt?"));
            var financier = ByID(ReadInt("Who is offering the new debt?"));
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
                if (originalCreditor != null && debtor.Liabilities.ContainsKey(originalCreditor))
                {
                    debtor.Liabilities[originalCreditor] -= amount;
                    originalCreditor.Money += amount;
                }
            }
            if (debtor.Liabilities.ContainsKey(financier))
                debtor.Liabilities[financier] += amount * (1 + commission);
            else
                debtor.Liabilities.Add(financier, amount * (1 + commission));
            financier.Money -= amount * (financier == admin && char.IsUpper(command[0]) ? .945 : 1);
        }

        private static void DeleteDeposit()
        {
            int id = ReadInt("ID: ");
            var data = from entity in Entities
                       from deposit in entity.Deposits
                       where deposit.DepositID == id
                       select new { Deposit = deposit, Entity = entity };

            var removed = data.FirstOrDefault();
            if (removed != null)
            {
                removed.Entity.Deposits.Remove(removed.Deposit);
                removed.Entity.Money += removed.Deposit.Principal;
            }
        }

        private static void CreateCompany()
        {
            Console.Write("Name: ");
            string name = Console.ReadLine();
            Entity founder = ByID(ReadInt("Founder ID: "));
            double capital = ReadDouble("Initial capital: ");
            double shareprice = ReadDouble("Share price: ");
            if (ByID(ReadInt("Pegged player ID: ")) is Player pegPlayer)
            {
                Company company = new Company(name, founder, capital, shareprice);
                Companies.Add(company);
                pegPlayer.PeggedCompanies.Add(company);
            }
        }

        private static void IssueShares()
        {
            Entity buyer = ByID(ReadInt("Buyer ID: "));
            Company issuer = ByID(ReadInt("Issuer ID: ")) as Company;
            if (buyer == issuer)
                throw new ShareOwnershipConflictException("An entity cannot buy shares of itself!");
            if ((buyer is Company) && (buyer as Company).ShareholderStructure.ContainsKey(issuer))
            {
                throw new ShareOwnershipConflictException("Cannot buy shares of a company that owns the other");
            }
            double sum = ReadDouble("Cash invested: ");
            if (sum > 0 && issuer != null)
            {
                double premiumPercent = ReadDouble("Premium: ");
                (issuer as Company).SubscribeNewShareholder(buyer, sum, premiumPercent);
            }
        }

        private static void BuybackShares(Entity holder_ = null, Company buyer_ = null, int? shares = null, double? premium_ = null)
        {
            Entity holder = holder_ ?? ByID(ReadInt("Holder ID: "));
            Company buyer = buyer_ ?? ByID(ReadInt("Buyer ID: ")) as Company;
            if (buyer != null)
            {
                if (holder == buyer)
                    throw new ShareOwnershipConflictException("An entity cannot buy shares of itself!");
                int value;
                if (shares == null)
                {
                    Console.WriteLine("Shares or cash: ");
                    string read = Console.ReadLine();
                    if (read == "sh" || read == "shares" || read == "s")
                        value = ReadInt("Shares: ");
                    else
                        value = (int)(ReadInt("Cash: ") / (buyer as Company).ShareValue);
                }
                else value = (int)shares;
                double premiumPctg = premium_ ?? ReadDouble("Premium: ");
                if (value > 0)
                {
                    buyer.BuyBackShares(holder, value, premiumPctg);
                }
            }
        }

        private static void DividendManager()
        {
            if (ByID(ReadInt("Which entity? ID: ")) is Company issuer)
            {
                Console.WriteLine("Share price: ${0:F4}, liquid/share: ${1:F4}.", issuer.ShareValue, issuer.Money / issuer.ShareCount);
                double amountPerShare = ReadDouble("Amount/share: ");
                issuer.IssueDividend(amountPerShare);
            }
        }

        private static void RenameEntity()
        {
            Entity entity = ByID(ReadInt("Which entity? ID: "));
            Console.Write("New name: ");
            entity.Name = Console.ReadLine();
        }

        private static void ChangePeg()
        {
            Company pegged = ByID(ReadInt("Which company? ID: ")) as Company;
            if (ByID(ReadInt("New player ID: ")) is Player newPlayer)
            {
                newPlayer.PeggedCompanies.Add(pegged);
                MainClass.Players.FirstOrDefault(p => p.PeggedCompanies.Contains(pegged)).PeggedCompanies.Remove(pegged);
            }
        }

        private static void CreateFund()
        {
            Console.Write("Name: ");
            string name = Console.ReadLine();
            Entity founder = ByID(ReadInt("Founder ID: "));
            Entity manager = ByID(ReadInt("Manager ID: ")) ?? founder;
            double capital = ReadDouble("Initial capital: ");
            double shareprice = ReadDouble("Share price: ");
            Player pegPlayer = ByID(ReadInt("Pegged player ID: ")) as Player;
            double fixedFee = ReadDouble("Fixed fee: ");
            double performanceFee = ReadDouble("Performance fee: ");
            CompStructure comp = new CompStructure(fixedFee, performanceFee);
            if (pegPlayer != null && founder != null && shareprice > 0 && capital > 0)
            {
                HedgeFund fund = new HedgeFund(name, founder, capital, shareprice, comp, manager);
                HedgeFunds.Add(fund);
                pegPlayer.PeggedCompanies.Add(fund);
            }
        }

        private static void ChangeFee()
        {
            if (ByID(ReadInt("Fund ID: ")) is HedgeFund fund)
            {
                Console.Write("Default fees (<d>, <all>) or specific <s>? ");
                string command = Console.ReadLine().ToLower();
                if (command == "d" || command == "all")
                {
                    Console.WriteLine("Current fees: {0:F3}% and {1:F3}%", fund.DefaultCompensationRules.AssetFee, fund.DefaultCompensationRules.PerformanceFee);

                    double ff = ReadDouble("Fixed fee: ");
                    double pf = ReadDouble("Performance fee: ");
                    fund.DefaultCompensationRules = new CompStructure(ff, pf);
                    fund.CompensationRules = fund.CompensationRules.ToDictionary(shareholder => shareholder.Key, shareholder => fund.DefaultCompensationRules);
                }
                else if (command == "s")
                {
                    Entity shareholder = ByID(ReadInt("Shareholder ID: "));
                    Console.WriteLine("Current fees: {0:F3}% and {1:F3}%", fund.CompensationRules[shareholder].AssetFee, fund.CompensationRules[shareholder].PerformanceFee);

                    double ff = ReadDouble("Fixed fee: ");
                    double pf = ReadDouble("Performance fee: ");
                    fund.ChangeShareholderFee(shareholder, new CompStructure(ff, pf));
                }
            }
        }

        private static void ViewFees()
        {
            if (ByID(ReadInt("Fund ID: ")) is HedgeFund fund)
            {
                Console.WriteLine("Default fees: {0:F3}% and {1:F3}%", fund.DefaultCompensationRules.AssetFee, fund.DefaultCompensationRules.PerformanceFee);
                fund.GetWeightedFees(out double assetFeeTotal, out double perfFeeTotal);
                Console.WriteLine("Weighted fees: {0:F3}% and {1:F3}%", assetFeeTotal, perfFeeTotal);
                Console.WriteLine("SHAREHOLDER".PadRight(25) + "ASSET FEE".PadLeft(15) + "PERFORMANCE FEE".PadLeft(18));
                foreach (var compRule in fund.CompensationRules)
                {
                    Console.WriteLine(compRule.Key.Name.PadRight(25) + compRule.Value.AssetFee.ToString("F3").PadLeft(15) + compRule.Value.PerformanceFee.ToString("F3").PadLeft(18));
                }

                Console.Write("Do you wish to change the fees?");
                if (Console.ReadLine() == "yes")
                {
                    ChangeFee();
                }
            }
        }


        private static void ChangeManager()
        {
            HedgeFund fund = ByID(ReadInt("Fund ID: ")) as HedgeFund;
            Entity manager = ByID(ReadInt("New Manager ID: ")) ?? fund.Manager;
            fund.Manager = manager;
            if (!fund.ShareholderStructure.ContainsKey(manager))
            {
                fund.ShareholderStructure.Add(manager, 0);
                fund.NewlySubscribedFunds.Add(manager, 0);
            }
        }

        private static void DeleteEntity()
        {
            Entity entity = ByID(ReadInt("Entity ID: "));

            if (HedgeFunds.Any(fund => fund.Manager == entity) || (entity is Player && (entity as Player).PeggedCompanies.Any()))
            {
                Console.WriteLine("{0} is a fund manager or has pegged a company to its name," +
                    "\nresolve before eliminating entity");
                return;
            }

            if (entity is Company)
                Companies.Remove(entity as Company);
            else if (entity is HedgeFund)
                HedgeFunds.Remove(entity as HedgeFund);
            Console.WriteLine("Removed from collection");

            foreach (var player in Players)
            {
                if (player.PeggedCompanies.Contains(entity))
                {
                    player.PeggedCompanies.Remove(entity as Company);
                    Console.WriteLine("Removed pegging");
                }
                if (player.Liabilities.ContainsKey(entity))
                {
                    player.Liabilities.Remove(entity);
                    Console.WriteLine("Removed liabilities' book");
                }
            }

            GoldManager.GoldRegister.Remove(entity);
            Console.WriteLine("Removed from gold register");

            List<ITwoPartyContract> contractsToBeRemoved = new List<ITwoPartyContract>();
            foreach (var contract in ContractCollection.Cast<ITwoPartyContract>())
            {
                if (contract.LongParty == entity || contract.ShortParty == entity)
                    contractsToBeRemoved.Add(contract);
            }
            RentSwapContracts.RemoveAll(c => contractsToBeRemoved.Contains(c));
            RentInsuranceContracts.RemoveAll(c => contractsToBeRemoved.Contains(c));
            Console.WriteLine("Removed contracts implicating entity");

            foreach (var property in Properties)
            {
                if (property.OptionOwner == entity)
                {
                    property.OptionOwner = null;
                    Console.WriteLine("Removed real estate option contract");
                }
                if (property.Owner == entity)
                {
                    property.Owner = null;
                    Console.WriteLine("Removed real estate ownership");
                }
            }
        }

        private static void SellShares(Entity buyer_ = null, Entity seller_ = null, Company sharesOf_ = null, int? shareCount = null, double? price_ = null)
        {
            Entity buyer = buyer_ ?? ByID(ReadInt("Buyer: "));
            Entity seller = seller_ ?? ByID(ReadInt("Seller: "));
            Company company = sharesOf_ ?? ByID(ReadInt("Shares of: ")) as Company;
            int shares = shareCount ?? ReadInt("Share count: ");
            double price;
            if (price_ == null)
            {
                Console.Write("Price: ");
                string priceStr = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(priceStr))
                    price = company.ShareValue;
                else price = double.Parse(priceStr);
            }
            else price = (double)price_;
            if (company != null && company.ShareholderStructure.ContainsKey(seller))
            {
                company.SellShares(seller, buyer, shares, price, company is HedgeFund && ((HedgeFund)company).Manager == seller);
            }
        }

        private static void SetStartPass()
        {
            Entity e = ByID(ReadInt("ID: "));
            int count = ReadInt("Value: ");
            e.PassedStartCounter = count;
        }

        private static void ViewVotePower()
        {
            Company entity = ByID(ReadInt("Company ID: ")) as Company;
            Entity managerIfAny = (entity is HedgeFund) ? (entity as HedgeFund).Manager : null;
            Dictionary<Entity, int> votePower = entity.ShareholderStructure.ToDictionary(
                pair => pair.Key,
                (pair) =>
                {
                    if (managerIfAny == pair.Key)
                        return (int)(pair.Value * (entity as HedgeFund).ManagerVoteMultiplier);
                    return (int)pair.Value;
                });
            double totalVotes = votePower.Values.Sum();
            foreach (var voter in votePower)
            {
                Console.WriteLine("{0} has {1} votes\t{2:F3}%", voter.Key.Name, voter.Value, ((double)voter.Value) * 100 / totalVotes);
            }
        }

        private static void ResetIDs()
        {
            foreach (var entity in Entities)
            {
                Console.WriteLine("Name: \"{0}\", ID: {1}", entity.Name, entity.ID);
            }
            int[] newIDs = Console.ReadLine().Split(',').Select(s => int.Parse(s)).ToArray();
            int i = 0;
            if (newIDs.Length == Entities.Count())
            {
                foreach (var entity in Entities)
                {
                    entity.ID = newIDs[i++];
                }
            }
        }

        private static void SetVoteMultiplier()
        {
            if (ByID(ReadInt("Hedge fund ID: ")) is HedgeFund fund)
                fund.ManagerVoteMultiplier = ReadDouble("Value: ");
        }

        private static void SetPropFlow()
        {
            Property property = ReadProperty();
            double rentflow = ReadDouble("Rent flow in: ");
            double consflow = ReadDouble("Cost flow out: ");
            property.SetRentFlowCounter(rentflow);
            property.SetConstructionCostCounter(consflow);
        }

        private static void CreateDeposit()
        {
            var depositer = ByID(ReadInt("Depositer: "));
            int rounds = ReadInt("Rounds money is locked: ");
            double sum = ReadDouble("Sum of money: ");
            double setInterestRate = Deposit.CalculateDepositInterestRate(rounds);
            Console.WriteLine("Interest rate calculated at {0:F4}%/round. Sign? yes/no", setInterestRate);
            if (Console.ReadLine() == "yes")
            {
                depositer.Deposits.Add(new Deposit(sum, setInterestRate, rounds, ++depocounter));
                depositer.Money -= sum;
                Console.WriteLine("Done");
            }
            else Console.WriteLine("Cancelled.");
        }

        private static void LoanSplit(double val, int splitsum, int rounds)
        {
            double generatedSum = 0;
            List<Deposit> deposits = new List<Deposit>();
            while (val - generatedSum >= splitsum)
            {
                double now = rand.Next((int)(splitsum * 0.9), (int)(splitsum * 1.1));
                deposits.Add(new Deposit(now, Deposit.CalculateDepositInterestRate(rounds, InterestRateBase, true), rounds, ++depocounter));
                generatedSum += now;
            }
            deposits.Add(new Deposit(val - generatedSum, Deposit.CalculateDepositInterestRate(rounds, InterestRateBase, true), rounds, ++depocounter));
            admin.Deposits.AddRange(deposits);
            admin.Money -= val;
        }

        private static void RunContractManager()
        {
            Console.WriteLine("Welcome to the derivatives manager.");
            Console.WriteLine("View contracts = view\n" +
                "Create new contract = new\n" +
                "End contract = end");
            string command;
            void readCommand() => command = Console.ReadLine();
            readCommand();
            if (command == "view")
            {
                Console.Write("All or specific ID?");
                readCommand();
                if (command == "all")
                {
                    foreach (var contract in InterestRateSwapContracts)
                    {
                        Console.WriteLine(contract.DescribeGeneral());
                    }
                    foreach (var rentswap in RentSwapContracts)
                    {
                        Console.WriteLine(rentswap.DescribeGeneral());
                    }
                    foreach (var rentIns in RentInsuranceContracts)
                    {
                        Console.WriteLine(rentIns.DescribeGeneral());
                    }
                }
                else
                {
                    int id = ReadInt("Contract ID: ");
                    var contract = InterestRateSwapContracts.Cast<IDescribable>().Union(RentSwapContracts).Union(RentInsuranceContracts).FirstOrDefault(c => c.ID == id);
                    if (contract == null)
                    {
                        Console.WriteLine("Contract with that ID does not exist");
                        return;
                    }
                    contract.DescribeSpecific();
                }
            }
            else if (command == "new")
            {
                Console.WriteLine("Available types: " +
                    "\n\tlock interest rate swap (irswap)" +
                    "\n\trent swap (rentswap)" +
                    "\n\trent insurance (rentins)");
                readCommand();
                if (command == "irswap")
                {
                    Console.Write("Name: ");
                    string name = Console.ReadLine();
                    Entity longParty = ByID(ReadInt("Long party: "));
                    Entity shortParty = ByID(ReadInt("Short party: "));
                    double fixedRate = ReadDouble("Locked (fixed) rate: ");
                    Func<double> getCurrentRate = shortParty.GetDirectLiabilitiesInterest;
                    OuterAction<Contract> termination = (t) => { InterestRateSwapContracts.Remove((InterestRateSwap)t); Console.WriteLine("Contract {0} ({1}) terminated."); };
                    double sum = ReadDouble("Sum protected: ");
                    int rounds = ReadInt("Rounds: ");

                    InterestRateSwap contract = new InterestRateSwap(
                        name, longParty, shortParty, fixedRate, getCurrentRate, null, termination, new ContractTerms(sum, rounds));
                    InterestRateSwapContracts.Add(contract);
                    Console.WriteLine("Contract {0} created", contract.ID);

                }
                else if (command == "rentswap")
                {
                    Console.Write("Name: ");
                    string name = Console.ReadLine();
                    Entity longParty = ByID(ReadInt("Long (variable) party: "));
                    Entity shortParty = ByID(ReadInt("Short (fixed) party: "));
                    double fixedRent = ReadDouble("Locked (fixed) rent: ");
                    int propID = ReadInt("Property ID: ");
                    int rounds = ReadInt("Rounds: ");
                    double rentMulti = ReadDouble("Rent/round multiplier: ");

                    RentSwapContract contract = new RentSwapContract(name, longParty, shortParty, propID, fixedRent, rentMulti, rounds);
                    RentSwapContracts.Add(contract);
                    Console.WriteLine("Contract {0} created", contract.ID);
                }
                else if (command=="rentins")
                {
                    Console.Write("Name: ");
                    string name = Console.ReadLine();
                    Entity longParty = ByID(ReadInt("Long (insurer) party: "));
                    Entity shortParty = ByID(ReadInt("Short (insured) party: "));
                    double premium = ReadDouble("Premium: ");
                    double insuredSum = ReadDouble("Insured sum: ");

                    int propID = ReadInt("Property ID: ");
                    int rounds = ReadInt("Rounds: ");

                    RentInsuranceContract contract = new RentInsuranceContract(name, longParty, shortParty, propID, premium, insuredSum, rounds);
                    RentInsuranceContracts.Add(contract);
                    Console.WriteLine("Contract {0} created", contract.ID);
                }
            }
            else if (command == "end")
            {
                Console.Write("Contract ID: ");
                readCommand();
                var contract = ContractCollection.FirstOrDefault(c => c.ID == int.Parse(command));
                if (contract == null)
                {
                    Console.WriteLine("Contract with that ID does not exist");
                    return;
                }
                else
                {
                    contract.DescribeSpecific();
                    Console.WriteLine("Are you sure?");
                    if (Console.ReadLine() == "yes")
                    {
                        if (contract is InterestRateSwap)
                            InterestRateSwapContracts.Remove(contract as InterestRateSwap);
                        else if (contract is RentSwapContract)
                            RentSwapContracts.Remove(contract as RentSwapContract);
                    }
                }
            }
        }

        public static void RunGoldman()
        {
            Console.WriteLine("Current price: {0:C}/kg", GoldManager.CurrentGoldPrice);
            Console.WriteLine("<buy>, <sell>, <prices>, <params>");
            string command = Console.ReadLine();
            if (command == "buy" || command == "sell")
            {
                Entity buyer = ByID(ReadInt("ID: "));
                double bars = GoldManager.GetGoldBarsNumberOwned(buyer);
                Console.WriteLine("Current holdings: {0:F3} bars (= {1:C})", bars, GoldManager.CurrentGoldPrice);
                Console.WriteLine("Bars(b) or dollars?");
                string commandType = Console.ReadLine();
                double dollars;
                if (commandType == "bars" || commandType == "b")
                {
                    dollars = ReadDouble("Total kgs:") * GoldManager.CurrentGoldPrice;
                }
                else
                {
                    dollars = commandType.ToDouble();
                }
                var function = command == "buy" ? (Action<Entity, double>)GoldManager.BuyGold : GoldManager.SellGold;
                function(buyer, dollars);
            }
            else if (command.ToLower() == "history" || command.ToLower() == "prices")
            {
                int i = 0;
                foreach (var price in GoldManager.HistoricalGoldPrices)
                {
                    if (++i % 10 == 0)
                        Console.WriteLine();
                    Console.WriteLine("{0}. {1:C}", i, price);
                }
                Console.WriteLine();
            }
            else if (command.ToLower() == "mk")
            {
                int i = 0;
                foreach (var price in GoldManager.FutureGoldPrices)
                {
                    Console.WriteLine("{0}. {1:C}", i++, price);
                }
            }
            else if (command.ToLower() == "params")
            {
                Console.WriteLine("\tDown delta max: " + GoldManager.DownDeltaMax);
                Console.WriteLine("\tDown delta min: " + GoldManager.DownDeltaMin);
                Console.WriteLine("\tUp delta min:    " + GoldManager.UpDeltaMin);
                Console.WriteLine("\tUp delta max:    " + GoldManager.UpDeltaMax);
                Console.WriteLine("\tMax five range:  " + GoldManager.MaximumFiveDeviation);

                Console.Write("Change params? ");
                if (Console.ReadLine() == "yes")
                {
                    ChangeGoldDeltas();
                }
            }
            else if (command.ToLower () == "reset")
            {
                GoldManager.ResetPrices();
            }
        }
    }
}
