using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using static monoconta.HedgeFund;
using System.Threading;

#pragma warning disable RECS0063 // Warns when a culture-aware 'StartsWith' call is used by default.

namespace monoconta
{
    static partial class MainClass
    {
        public static IEnumerable<Entity> Entities
        {
            get
            {
                return Players.Cast<Entity>().Union(Companies.Cast<Entity>()).Union(HedgeFunds.Cast<Entity>());
            }
        }

        public static List<Player> Players;
        public static List<Company> Companies;
        public static List<HedgeFund> HedgeFunds;

        public static List<Property> Properties;
        public static List<Neighbourhood> Neighbourhoods;

        public static double InterestRateBase = 1;
        public static string GameName = "";
        public static Player admin;
        public static bool showIDs = false;

        public static double _m_ = (double)5 / 3;
        public static double startBonus = 2000;

        public static int depocounter = 0;

        public static SaveGameManager SGManager;
        public static DiceManager DiceManager;

        public static void LoadGame(string[] args)
        {
            Stopwatch watch = Stopwatch.StartNew();
            Console.Write("Loading");
            if (LoadFromXML(out Neighbourhoods, out Properties))
            {
                Console.WriteLine("Successfully loaded all real estate\nfrom ClassicBuildings.xml file (in {0:F2} seconds)", watch.Elapsed.TotalSeconds);
            }
            Console.Write("Load previous session? ");
            string fileName = Console.ReadLine();
            fileName = fileName.EndsWith(".xml") || string.IsNullOrWhiteSpace(fileName) ? fileName : fileName + ".xml";

            args = fileName == "" ? args : new[] { Environment.CurrentDirectory + "/" + fileName };

            if (args.Length >= 1)
            {
                Console.Write("Verbose debug: ");

                ReadGameManager readManager = new ReadGameManager(fileName, Console.ReadLine() == "yes");
                readManager.Read(Properties);
                readManager.Integrate(out Players, out Companies, out HedgeFunds, out admin, out GameName, out InterestRateBase, out _m_, out startBonus, out depocounter);
                SGManager = new SaveGameManager(GameName, fileName);
            }
            else
            {
                Console.WriteLine("Players' names:");
                string[] names = Console.ReadLine().Split(',').Select(s => s.Trim()).ToArray();
                double startingAmount = ReadDouble("\nStarting amount: ");
                Players = new List<Player>(names.Select(n => new Player(n, ++Player.IDBASE) { Money = startingAmount }));
                MainClass.admin = Players.FirstOrDefault(p => char.IsUpper(p.Name[0]));
                InterestRateBase = ReadDouble("Set interest rate base: ");

                Company.ID_COUNTER_BASE = Player.IDBASE;

                Companies = new List<Company>();
                HedgeFunds = new List<HedgeFund>();
                Console.WriteLine("Done!\n\n");
            }
            DiceManager = new DiceManager();
            watch.Stop();
        }

        public static void Main(string[] args)
        {
            LoadGame(args);

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
                    else if (command.ToLower() == "transfer")
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
                    else if (command == "createfund")
                    {
                        CreateFund();
                    }
                    else if (command == "changefee")
                    {
                        ChangeFee();
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
#pragma warning disable RECS0018 // Comparison of floating point numbers with equality operator
                        if (((double)company.ShareCount) * ratio == (int)(company.ShareCount * ratio))
#pragma warning restore RECS0018 // Comparison of floating point numbers with equality operator
                        {
                            company.ShareholderStructure = company.ShareholderStructure.ToDictionary(pair => pair.Key, pair => (int)(pair.Value * ratio));
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
                        Transfer("transfer", land.Owner, payer, rent);
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
                        property._authorized = true;
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
                        }
                    }
                    else if (command == "setpropbuildings")
                    {
                        Property prop = ReadProperty();
                        int levels = ReadInt("Levels: ");
                        int appartments = ReadInt("Appartments: ");
                        prop.SetBuildings(levels, appartments, 0);
                    }
                    else if (command == "setpropflow")
                    {
                        Property property = ReadProperty();
                        double rentflow = ReadDouble("Rent flow in: ");
                        double consflow = ReadDouble("Cost flow out: ");
                        property.SetRentFlowCounter(rentflow);
                        property.SetConstructionCostCounter(consflow);
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
                                Entity.OrderPropertiesByID = false;
                                break;
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
                            Players.ToList(), Companies.ToList(),
                            HedgeFunds.ToList(), Properties.ToList(),
                            Neighbourhoods.ToList(),
                            InterestRateBase, admin, _m_,
                            startBonus, depocounter);
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
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
        }

        private static void Dice()
        {
            int d1, d2;
            DiceManager.GetDice(out d1, out d2);
            Console.WriteLine("{0} -- {1}", d1, d2);
        }

        private static Property ReadProperty()
        {
            return GetProperty(ReadInt("Property ID: "));

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
            var player = ByID(ReadInt("ID: "));
            double amount = ReadDouble("Sum: ");
            if (player == admin && char.IsUpper(command[0]))
                amount *= 0.9125;
            player.Money -= amount;
            if (player.Money < 0 && financedeficit)
            {
                // let's see how we can finance this
                Console.WriteLine("Debitor is out of cash. \n\t>Loan (bank, interplayer) [loan]\n\t>Share sale [sellshares,shares,sell,sh]");
                string variant = Console.ReadLine();
                if (variant == "loan")
                {
                    int from = ReadInt("Creditor ID: ");
                    Loan("loan", player, from, -player.Money);
                }
                else if (variant == "sellshares" || variant == "shares" || variant == "sell" || variant == "sh")
                {
                    Company sharesOf = ByID(ReadInt("Shares of: ")) as Company;
                    double price = ReadDouble("Price: ");
                    price = price == 0 ? sharesOf.ShareValue : price;
                    int shareCount = (int)Math.Ceiling((-player.Money) / price);
                    int shareBuyerID = ReadInt("Beneficiary ID: ");
                    SellShares(ByID(shareBuyerID), player, sharesOf, shareCount, sharesOf.ShareValue);
                }
            }
        }

        static bool financedeficit = true;
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
                Console.WriteLine("Debitor is out of cash. \n\t>Loan (bank, interplayer) [loan]\n\t>Share transfer [sellshares]");
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
                player.LiabilityTowardsBank *= ((InterestRateBase / (player == admin && command.ToLower() == "passs" ? 2.25 : 2)) / 100 + 1);
                foreach (var creditor in player.Liabilities.Keys.ToList())
                {
                    player.Liabilities[creditor] *= ((InterestRateBase / 3) / 100 + 1);
                }
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
                    foreach (var ent in player.PeggedCompanies)
                    {
                        ent.RegisterBook();
                        ent.LiabilityTowardsBank *= ((InterestRateBase / ((player == admin && command.ToLower() == "passs") ? 2.25 : 2)) / 100 + 1);
                        foreach (var creditor in ent.Liabilities.Keys.ToList())
                        {
                            ent.Liabilities[creditor] *= ((InterestRateBase / 3) / 100 + 1);
                        }
                        foreach (var deposit in ent.Deposits.ToList())
                        {
                            if (!deposit.PassRound(player == admin && command.ToLower() == "passs"))
                            {
                                ent.Money += deposit.Principal;
                                ent.Money += deposit.TotalInterest;
                                if (showpegged)
                                {
                                    Console.WriteLine("Deposit {0} reached term", deposit.DepositID);
                                    Console.WriteLine("Received principal of {0:C} and total accumulated interest of {1:C}", deposit.Principal, deposit.TotalInterest);
                                }
                                ent.Deposits.Remove(deposit);
                            }
                        }
                        ent.OnPassedStart();
                        if (showpegged)
                            ent.PrintSituation(true);
                    }
                }
                if (command.StartsWith("PASS"))
                {
                    command = null;
                    goto repeat;
                }
                player.PrintSituation(true);
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
                Console.WriteLine("Current fees: {0:F3}% and {1:F3}%", fund.Compensation.AssetFee, fund.Compensation.PerformanceFee);
                double ff = ReadDouble("Fixed fee: ");
                double pf = ReadDouble("Performance fee: ");
                fund.Compensation = new CompStructure(ff, pf);
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
            if (entity is Company)
                Companies.Remove(entity as Company);
            else if (entity is HedgeFund)
                HedgeFunds.Remove(entity as HedgeFund);

            foreach (var player in Players)
            {
                if (player.PeggedCompanies.Contains(entity))
                    player.PeggedCompanies.Remove(entity as Company);
                if (player.Liabilities.ContainsKey(entity))
                    player.Liabilities.Remove(entity);
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
            (ByID(ReadInt("Hedge fund ID: ")) as HedgeFund).ManagerVoteMultiplier = ReadDouble("Value: ");
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

        static Random rand = new Random();
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

        public static double ReadDouble(string str = null)
        {
            if (str != null)
                Console.Write(str);
            return double.TryParse(Console.ReadLine(), out double s) ? s : 0;
        }
        public static int ReadInt(string str = null)
        {
            if (str != null)
                Console.Write(str);
            return int.TryParse(Console.ReadLine(), out int s) ? s : 0;
        }

        public static Entity ByID(string s)
        {
            int parsedID = int.Parse(s);
            return Entities.First(p => p.ID == parsedID);
        }

        public static Entity ByID(int s)
        {
            return Entities.First(p => p.ID == s);
        }

        public static Property GetProperty(int id)
        {
            return Properties.First(p => p.ID == id);
        }
        public static Property GetProperty(string s)
        {
            int parsedID = int.Parse(s);
            return Properties.First(p => p.ID == parsedID);
        }
    }
}
#pragma warning restore RECS0063 // Warns when a culture-aware 'StartsWith' call is used by default.
