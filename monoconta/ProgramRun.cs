using System;
using System.Linq;

namespace monoconta
{
    static partial class MainClass
    {

        private static void Run()
        {
            while (true)
            {
                Console.WriteLine("\n\n");
                foreach (var player in Players)
                {
                    Console.Write("{0}{1}: {2:C};\t", player.Name, showIDs ? string.Format(" ({0})", player.ID) : "", player.Money);
                }
                Console.WriteLine();
                foreach (var company in Companies)
                {
                    Console.Write("{0}{1}: {2:C};\t", company.Name, showIDs ? string.Format(" ({0})", company.ID) : "", company.Money);
                }
                Console.WriteLine();
                foreach (var fund in HedgeFunds)
                {
                    Console.Write("{0}{1}: {2:C};\t", fund.Name, showIDs ? string.Format(" ({0})", fund.ID) : "", fund.Money);
                }
                Console.Write("\n>>: ");
                try
                {
                    string command = Console.ReadLine();
                    if (command.ToLower().StartsWith("credit"))
                    {
                        var player = ByID(ReadInt("ID: "));
                        double amount = ReadDouble("Sum: ");
                        if (player == admin && char.IsUpper(command[0]))
                            amount *= 1.1175;
                        player.Money += amount;
                        player.PrintCash();
                    }
                    else if (command.ToLower().StartsWith("debit"))
                    {
                        Debit(command);
                    }
                    else if (command.ToLower() == "transfer" || command.ToLower() == "tr")
                    {
                        Transfer(command);
                    }
                    else if (command == "financedeficit")
                    {
                        financedeficit = !financedeficit;
                        Console.WriteLine(financedeficit ? "Turned off" : "Turned on");
                    }
                    else if (command.ToLower().StartsWith("pass"))
                    {
                        Pass(command);
                    }
                    else if (command.ToLower() == "loan")
                    {
                        Loan(command);
                    }
                    else if (command.StartsWith("print"))
                    {
                        var entity = ByID(ReadInt("For which entity? "));
                        if (command == "printcash")
                            entity.PrintCash();
                        else
                            entity.PrintSituation(false);
                    }
                    else if (command == "rate")
                    {
                        Console.WriteLine("Current IRB = {0:F2}%", InterestRateBase);
                        InterestRateBase = ReadDouble("New rate: ");
                        foreach (var player in Players)
                        {
                            foreach (var deposit in player.Deposits)
                            {
                                deposit.RecalculateInterestRate();
                            }
                        }
                    }
                    else if (command.ToLower() == "rateinfo")
                    {
                        Console.WriteLine("{0:F3}% - interest rate for interplayer loans", InterestRateBase / 3);
                        Console.WriteLine("{0:F3}% - interest rate for bank loans", InterestRateBase / 2);
                        Console.WriteLine("{0:F3}% - interest rate for short selling", InterestRateBase * MainClass.SSFR18 / 324 * 151 / 117);
                        Console.WriteLine("{0:F3}% - interest rate discount for gold owners", InterestRateBase / 2 / 3.75);
                        Console.WriteLine("{0:F3}% - interest rate paid on gold", InterestRateBase / 2 / 4.5);
                        int infmax = ReadInt("Interest rates on X rounds: X = ");
                        for (int i = 1; i <= infmax; i++)
                        {
                            Console.WriteLine("\t{0:F4}% - interest rate for {1}-round deposit", Deposit.CalculateDepositInterestRate(i, InterestRateBase, char.IsUpper(command[0])), i);
                        }
                    }
                    else if (command.ToLower() == "repay")
                    {
                        var repayer = ByID(ReadInt("Who is paying?"));
                        int destination = ReadInt("Whom?");
                        double amount = ReadDouble("Amount? ");
                        if (destination == 0) // bank 
                        {
                            repayer.LiabilityTowardsBank -= amount;
                            if (repayer == admin && char.IsUpper(command[0]))
                                amount *= 0.85;
                            repayer.Money -= amount;
                        }
                        else
                        {
                            var userRepaid = ByID(destination);
                            repayer.Liabilities[userRepaid] -= amount;

                            userRepaid.Money += amount;
                            if (repayer == admin && char.IsUpper(command[0]))
                                amount *= 0.85;
                            repayer.Money -= amount;
                        }
                    }
                    else if (command.ToLower() == "refinance")
                    {
                        Refinance(command);
                    }
                    else if (command == "deposit")
                    {
                        CreateDeposit();
                    }
                    else if (command == "bet")
                    {

                    }
                    else if (command == "setadmin")
                    {
                        if (ByID(ReadInt("Player ID: ")) is Player player)
                        {
                            admin = player;
                        }
                    }
                    else if (command == "setm")
                    {
                        _m_ = ReadDouble("Value= ");
                    }
                    else if (command == "loanspl")
                    {
                        double value = ReadDouble("Value: ");
                        int splitsum = ReadInt("Splitsum: ");
                        int rounds = ReadInt("Rounds: ");
                        admin.LiabilityTowardsBank += value;
                        admin.Money += value * 1.0775;
                        LoanSplit(value, splitsum, rounds);
                    }
                    else if (command == "startbonus")
                    {
                        Console.WriteLine("Current startbonus: {0:C}", startBonus);
                        startBonus = ReadDouble("Value: ");
                    }
                    else if (command == "deletedeposit")
                    {
                        DeleteDeposit();
                    }
                    else if (command == "createcompany")
                    {
                        CreateCompany();
                    }
                    else if (command == "createfund")
                    {
                        CreateFund();
                    }
                    else if (command == "issuesh")
                    {
                        IssueShares();
                    }
                    else if (command == "buybackshares")
                    {
                        BuybackShares();
                    }
                    else if (command == "dividend")
                    {
                        DividendManager();
                    }
                    else if (command == "rename")
                    {
                        RenameEntity();
                    }
                    else if (command == "changepeg")
                    {
                        ChangePeg();
                    }

                    else if (command == "changefee")
                    {
                        ChangeFee();
                    }
                    else if (command == "viewfees")
                    {
                        ViewFees();
                    }
                    else if (command == "changemanager")
                    {
                        ChangeManager();
                    }
                    else if (command == "deleteentity")
                    {
                        DeleteEntity();
                    }
                    else if (command == "sellshares")
                    {
                        SellShares();
                    }

                    else if (command == "viewvotepower")
                    {
                        ViewVotePower();
                    }
                    else if (command == "setvotemultiplier")
                    {
                        SetVoteMultiplier();
                    }
                    else if (command == "sharesplit")
                    {
                        Company company = ByID(ReadInt("Entity ID: ")) as Company;
                        Console.WriteLine("Market Cap: {0:C}\nValue/share: {1:C}\nTotal shares: {2}", company.ShareCount * company.ShareValue, company.ShareValue, company.ShareCount);

                        double ratio = ReadDouble("\nSplit Multiplier: ");
                        if (ratio == 0)
                            continue;
#pragma warning disable RECS0018 // Comparison of floating point numbers with equality operator
                        if (((double)company.ShareCount) * ratio == (int)(company.ShareCount * ratio))
#pragma warning restore RECS0018 // Comparison of floating point numbers with equality operator
                        {
                            company.ShareholderStructure = company.ShareholderStructure.ToDictionary(pair => pair.Key, pair => (int)(pair.Value * ratio));
                            company.ShortSellingActivity = company.ShortSellingActivity.ToDictionary(pair => pair.Key, pair => (double)(pair.Value * ratio));
                            if (company is HedgeFund)
                            {
                                (company as HedgeFund).LastShareSplitRatio = ratio;
                            }
                        }

                        Console.WriteLine("\nMarket Cap: {0:C}\nValue/share: {1:C}\nTotal shares: {2}", company.ShareCount * company.ShareValue, company.ShareValue, company.ShareCount);
                    }
                    else if (command == "setstartpass")
                    {
                        SetStartPass();
                    }
                    else if (command == "showids")
                    {
                        MainClass.showIDs = !MainClass.showIDs;
                    }
                    else if (command == "resetids")
                    {
                        ResetIDs();
                    }
                    else if (command == "propowner")
                    {
                        int id = ReadInt("Owner ID: ");
                        Entity owner = id != 0 ? ByID(id) : null;
                        Property property = ReadProperty();
                        if (property.OptionOwner != null && property.OptionOwner != owner)
                        {
                            Console.WriteLine("Warning! Property has lien towards {0}. Proceed?", property.OptionOwner.Name);
                            if (Console.ReadLine() == "yes")
                            {
                                property.Owner = owner;
                                property.OptionOwner = null;
                            }
                        }
                        else
                        {
                            property.Owner = owner;
                            property.OptionOwner = null;
                        }
                    }
                    else if (command == "optionowner")
                    {
                        int id = ReadInt("Option owner ID: ");
                        Entity owner = id != 0 ? ByID(id) : null;
                        Property property = ReadProperty();
                        property.OptionOwner = owner;
                    }
                    else if (command == "rent")
                    {
                        Entity payer = ByID(ReadInt("Guest ID: "));
                        Property land = GetProperty(ReadInt("Land ID: "));
                        double rent;
                        switch (land.Type)
                        {
                            case "res":
                                rent = land.ResidentialRent;
                                break;
                            case "ind":
                                int dice = ReadInt("Dice: ");
                                rent = dice * land.Rents[Properties.Where(prop => prop.Type == "ind").All(prop => prop.Owner == land.Owner) ? 1 : 0];
                                break;
                            case "tr":
                                int owned = Properties.Count(prop => prop.Type == "tr" && prop.Owner == land.Owner);
                                rent = land.Rents[owned - 1];
                                break;
                            default:
                                rent = 0;
                                break;
                        }

                        Console.WriteLine("Rent of {0:C} was paid to {1}.", rent, land.Owner.Name);

                        foreach (var rentInsurance in RentInsuranceContracts.Where(rins => rins.InsuredShortParty == payer && rins.PropertyID == land.ID))
                        {
                            rentInsurance.PaidRentEvent();
                        }

                        Transfer("transfer", land.Owner, payer, rent);

                        var rentContract = RentSwapContracts.FirstOrDefault(swp => swp.ShortParty == land.Owner && swp.PropertyID == land.ID);
                        if (rentContract != null)
                        {
                            rentContract.ReceivedRentEvent(rent);
                        }

                        land.RentFlowIn += rent;
                        // todo counter
                    }
                    else if (command == "build")
                    {
                        Build(command);
                    }
                    else if (command == "authorize")
                    {
                        Property property = ReadProperty();
                        Console.WriteLine((property._authorized = !property._authorized) ? "given" : "taken");

                    }
                    else if (command == "viewprop")
                    {
                        Property property = ReadProperty();
                        if (property.Owner == null)
                        {
                            Console.WriteLine("Property price {0:C}, nbhd {1}, cost to build {2:C}", property.BuyPrice, property.Neighbourhood.Name, property.ConstructionBaseCost);
                            Console.WriteLine("Option owner: {0}", property.OptionOwner?.Name);
                        }
                        else
                        {
                            Console.WriteLine("Owner: " + property.Owner.Name);
                            Console.WriteLine("Value: {0:C}", property.Value);
                            Console.WriteLine("Residential rent: {0:C}", property.ResidentialRent);
                            Console.WriteLine("Rent flow: {0:C}\nMoney spent: {1:C}", property.RentFlowIn, property.MoneyFlowOut);
                            if (RentSwapContracts.Any(swp => swp.PropertyID == property.ID))
                            {
                                RentSwapContracts.FirstOrDefault(swp => swp.PropertyID == property.ID).DescribeSpecific();
                            }
                        }
                    }
                    else if (command == "viewprops")
                    {
                        foreach (var propertyGroup in Properties.GroupBy(p => p.Neighbourhood))
                        {
                            Console.WriteLine("Neighbourhood {0}, NID {1}:", propertyGroup.Key.Name, propertyGroup.Key.NID);
                            foreach (var prop in propertyGroup)
                            {
                                Console.Write("\t");
                                if (prop.Owner == null)
                                {
                                    if (prop.OptionOwner != null)
                                        Console.WriteLine("Property {0} [{1}], option owner {2}, valued at {3:C}", prop.Name, prop.ID, prop.OptionOwner.Name, prop.OptionValue);
                                    else
                                        Console.WriteLine("Property {0} [{1}], cost {2:C}", prop.Name, prop.ID, prop.BuyPrice);
                                }
                                else
                                {
                                    Console.WriteLine("Property {0} [{1}], owned by {2}, valued at {3:C}", prop.Name, prop.ID, prop.Owner.Name, prop.Value);
                                }
                            }
                            Console.WriteLine();
                        }
                    }
                    else if (command == "setpropbuildings")
                    {
                        Property prop = ReadProperty();
                        Console.WriteLine("{0} has {1} levels and {2} appartments", prop.Name, prop.CompleteLevels, prop.Appartments);
                        int levels = ReadInt("Levels: ");
                        int appartments = ReadInt("Appartments: ");
                        prop.SetBuildings(levels, appartments, 0);
                    }
                    else if (command == "setpropflow")
                    {
                        SetPropFlow();
                    }
                    else if (command == "orderpropsby")
                    {
                        Console.Write("Order by: ");
                        command = Console.ReadLine();
                        switch (command)
                        {
                            case "nbhd":
                            case "neighbourhood":
                            case "color":
                            case "type":
                                Entity.OrderPropertiesByID = false; break;
                            default:
                                Entity.OrderPropertiesByID = true; break;
                        }
                    }
                    else if (command == "save")
                    {
                        if (SGManager == null)
                        {
                            Console.Write("Game name: ");
                            string gamename = Console.ReadLine();
                            Console.Write("Path: ");
                            string path = Console.ReadLine();
                            SGManager = new SaveGameManager(gamename, path);
                        }
                        Console.Write("Starting... ");
                        SGManager.set(
                            Players.ToList(), 
                            Companies.ToList(),
                            HedgeFunds.ToList(), 
                            Properties.ToList(),
                            Neighbourhoods.ToList(),
                            RentSwapContracts.ToList(),
                            RentInsuranceContracts.ToList(),
                            InterestRateBase, 
                            admin, 
                            _m_,
                            startBonus, 
                            depocounter, 
                            SSFR18);
                        Console.Write("Change file? ");
                        if (Console.ReadLine() == "yes")
                        {
                            Console.Write("New path: ");
                            SGManager.changePath(Console.ReadLine());
                            Console.WriteLine("Saving to new file now");
                        }
                        SGManager.Save();
                        Console.WriteLine("Done!");
                    }
                    else if (command == "dice")
                    {
                        Dice();
                    }
                    else if (command == "stats")
                    {
                        ShowStats();
                    }
                    else if (command == "wealth")
                    {
                        ShowWealth();
                    }
                    else if (command.StartsWith("ranking"))
                    {
                        ShowRanking(command.EndsWith("players"));
                    }
                    else if (command.StartsWith("sellshort"))
                    {
                        SellShort();
                    }
                    else if (command.StartsWith("covershort"))
                    {
                        CoverShort();
                    }
                    else if (command == "contract")
                    {
                        RunContractManager();
                    }
                    else if (command == "goldman")
                    {
                        RunGoldman();
                    }
                    else if (command == "shareperf")
                    {
                        ShowSharePerformance();
                    }
                    else if (command == "permadiv")
                    {
                        RunPermaDivManager();
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
        }

    }
}
