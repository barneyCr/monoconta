using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Xml.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading;
using System.Xml;

namespace monoconta
{
    public class ReadGameManager
    {
        string _path;
        bool verboseDebug;
        public ReadGameManager(string path, bool verbose)
        {
            _path = path; 
            verboseDebug = verbose;

            this.UnresolvedLiabilityHoldingDictionary = new Dictionary<Entity, Dictionary<int, double>>();
            this.UnresolvedShareholdingDictionary = new Dictionary<Company, Dictionary<int, int>>();
            this.UnresolvedShortSellingDictionary = new Dictionary<Company, Dictionary<KeyValuePair<int, int>, int>>();
            this.UnresolvedFundManagersDictionary = new Dictionary<HedgeFund, int>();
            this.UnresolvedFundCompStructuresDictionary = new Dictionary<HedgeFund, Dictionary<int, HedgeFund.CompStructure>>();
            this.UnresolvedNewMoneyInFundDictionary = new Dictionary<HedgeFund, Dictionary<int, int>>();
        }

        private List<Player> Players = new List<Player>();
        private List<Company> Companies = new List<Company>();
        private List<HedgeFund> HedgeFunds = new List<HedgeFund>();
        private List<Property> Properties = new List<Property>();
        //private List<Neighbourhood> Neighbourhoods = new List<Neighbourhood>();
        private List<RentSwapContract> RentSwapContracts = new List<RentSwapContract>();
        private Dictionary<Entity, double> GoldRegister = new Dictionary<Entity, double>();

        private string gameName;
        private double IRB;
        private Player admin;
        private double _m, startbonus;
        private int depocounter;
        private double SSFR18;

        internal void Integrate(
            out List<Player> players, out List<Company> companies,
            out List<HedgeFund> hedgeFunds,
            out List<RentSwapContract> rentSwapContracts,
            out Dictionary<Entity, double> prevGoldRegister,
            out Player admin, out string name,
            out double irb, out double m,
            out double startBonus, out int depoCounter, out double ssfr18
        )
        {
            players = this.Players;
            companies = this.Companies;
            hedgeFunds = this.HedgeFunds;
            rentSwapContracts = this.RentSwapContracts;
            prevGoldRegister = this.GoldRegister;
            admin = this.admin;
            name = this.gameName;
            irb = this.IRB;
            m = this._m;
            startBonus = this.startbonus;
            depoCounter = this.depocounter;
            ssfr18 = this.SSFR18;
            try
            {
                Company.ID_COUNTER_BASE = GetAllEntities().Max(e => e.ID);
            }
            catch (Exception) {
 throw new Exception("No entities...");
            }
        }

        private int ___adminID;
        private readonly Dictionary<Entity, Dictionary<int, double>> UnresolvedLiabilityHoldingDictionary;
        private readonly Dictionary<Company, Dictionary<int, int>> UnresolvedShareholdingDictionary;
        private readonly Dictionary<Company, Dictionary<KeyValuePair<int,int>, int>> UnresolvedShortSellingDictionary;
        private readonly Dictionary<HedgeFund, int> UnresolvedFundManagersDictionary;
        private readonly Dictionary<HedgeFund, Dictionary<int, int>> UnresolvedNewMoneyInFundDictionary;
        private Dictionary<HedgeFund, Dictionary<int, HedgeFund.CompStructure>> UnresolvedFundCompStructuresDictionary;

        private readonly Func<XElement, string, string> readerFunc = (el, str) => el.Element(str).Value;

        /// <summary>
        /// Yields all players, companies and funds.
        /// </summary>
        /// <returns>The all entities.</returns>
        IEnumerable<Entity> GetAllEntities()
        {
            foreach (var player in Players)
            {
                yield return player;
            }
            foreach (var company in Companies)
            {
                yield return company;
            }
            foreach (var fund in HedgeFunds)
            {
                yield return fund;
            }
        }


        internal void Read(List<Property> propertiesTemplate)
        {
            try
            {
                Stopwatch watch = Stopwatch.StartNew();
                using (StreamReader reader = File.OpenText(_path))
                {
                    XDocument document = XDocument.Load(reader);
                    XElement game = document.Root;
                    this.gameName = game.Attribute("name").Value;
                    Console.WriteLine("Loading game {0}...", gameName);

                    ReadConfig(game);
                    ReadEntities(game.Element("entities")); // after this there are no missing links!!!
                    ReadProperties(game.Element("properties"), propertiesTemplate);
                    ReadContracts(game.Element("contracts"));
                    ReadGold(game.Element("gold"));

                    watch.Stop();
                    Console.WriteLine("Completed in {0:F2} ms!", watch.Elapsed.TotalMilliseconds);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        private void ReadConfig(XElement game)
        {
            XElement config = game.Element("config");
            this.IRB = config.Element("irb").Value.ToDouble();
            // we only read admin's ID and only if it exists
            if (config.Elements().Any(elem => elem.Name == "admin"))
                this.___adminID = config.Element("admin").Value.ToInt();
            this._m = config.Element("m").Value.ToDouble();
            this.startbonus = config.Element("startbonus").Value.ToDouble();
            this.depocounter = config.Element("depocounter").Value.ToInt();
            if (config.Elements().Any(e => e.Name == "shortsellratefactor18"))
                this.SSFR18 = config.Element("shortsellratefactor18").Value.ToDouble();
            else
                this.SSFR18 = 19.4;
            Console.WriteLine("\tConfig loaded");
        }

        private void ReadEntities(XElement entitiesElement)
        {
            ReadPlayers(entitiesElement.Element("players"));
            ReadCompanies(entitiesElement.Element("firms").Element("companies"));
            ReadHedgeFunds(entitiesElement.Element("firms").Element("hedgefunds"));
            ResolveMissingLinks();
        }

        private void ReadProperties(XElement propertiesElement, List<Property> propertiesTemplate)
        {
            this.Properties = propertiesTemplate;
            foreach (var propertyElement in propertiesElement.Elements("property"))
            {
                int id = propertyElement.Attribute("pid").Value.ToInt();
                Func<string, int> rint = str => readerFunc(propertyElement, str).ToInt();
                Func<string, double> rouble = str => readerFunc(propertyElement, str).ToDouble();
                Property property = Properties.FirstOrDefault(prop => prop.ID == id);
                if (property == null)
                {
                    Console.WriteLine("Warning: undefined property (ID = {0})", id);
                }
                else
                {
                    if (propertyElement.Element("ownerid") != null)
                    {
                        int ownerID = rint("ownerid");
                        int levels = rint("levels");
                        int appartments = rint("appartments");
                        double rentflowin = rouble("rentflowin");
                        double moneyflowout = rouble("moneyflowout");
                        property.Owner = GetAllEntities().FirstOrDefault(e => e.ID == ownerID);
                        property.CompleteLevels = levels;
                        property.Appartments = appartments;
                        property.RentFlowIn = rentflowin;
                        property.MoneyFlowOut = moneyflowout;
                    }
                    else if (propertyElement.Element("optionownerid") != null)
                    {
                        int optionOwnerID = rint("optionownerid");
                        property.OptionOwner = GetAllEntities().FirstOrDefault(e => e.ID == optionOwnerID);
                    }
                }
            }
        }

        private void ReadContracts(XElement xElement)
        {
            this.RentSwapContracts = new List<RentSwapContract>();
            foreach (var rentSwapElement in xElement.Element("rentswaps").Elements("rentswap"))
            {
                Func<string, string> read = str => readerFunc(rentSwapElement, str);
                //Action<Func<string,string>, string> changeElem = (func, el) => read = (str) => readerFunc(rentSwapElement.Element(el), str);
                int swapID = rentSwapElement.Attribute("id").Value.ToInt();
                string name = read("name");
                int propID = read("propID").ToInt();
                double fixedRent = read("fixedRent").ToDouble();
                double roundrentmulti = read("roundrentmulti").ToDouble();
                int roundspassed = read("roundspassed").ToInt();
                int totalrounds = read("totalrounds").ToInt();

                int longPartyID = rentSwapElement.Element("longparty").Attribute("id").Value.ToInt();
                int shortPartyID = rentSwapElement.Element("shortparty").Attribute("id").Value.ToInt();

                Dictionary<int, ContractRecord> entries = new Dictionary<int, ContractRecord>();
                foreach (var entryElem in rentSwapElement.Element("entries").Elements("entry"))
                {
                    read = str => readerFunc(entryElem, str);
                    int entryNo = entryElem.Attribute("no").Value.ToInt();
                    double fixedRentPaid = read("fixed").ToDouble();
                    double varRentPaid = read("var").ToDouble();
                    string msg = read("msg");
                    ContractRecord record = new ContractRecord(fixedRentPaid, varRentPaid, msg);
                    entries.Add(entryNo, record);
                }

                Entity longParty = GetAllEntities().FirstOrDefault(e => e.ID == longPartyID);
                Entity shortParty = GetAllEntities().FirstOrDefault(e => e.ID == shortPartyID);

                RentSwapContract contract = new RentSwapContract(
                    name, longParty, shortParty, propID, fixedRent, roundrentmulti, totalrounds
                )
                {
                    ID = swapID,
                    RoundsPassed = roundspassed,
                    ContractEntries = entries
                };
                this.RentSwapContracts.Add(contract);
            }
            Console.WriteLine("\tContracts loaded");
        }

        private void ReadGold(XElement goldElement)
        {
            GoldManager._initialHardReadPrice = goldElement.Element("price")?.Value.ToDouble() ?? 1500;

            double readDelta(string str, double def) => goldElement.Element("deltas")?.Element(str)?.Value.ToDouble() ?? def;
            GoldManager.DownDeltaMax = readDelta("downmax", -9);
            GoldManager.DownDeltaMin = readDelta("downmin", -4);
            GoldManager.UpDeltaMin = readDelta("upmin", 6);
            GoldManager.UpDeltaMax = readDelta("upmax", 16);
            GoldManager.MaximumFiveDeviation = readDelta("fiverange", 65);

            foreach (var entryElement in goldElement.Element("register").Elements("entry"))
            {
                int id = entryElement.Attribute("id").Value.ToInt();
                double quantity = entryElement.Element("qty").Value.ToDouble();

                Entity owner = GetAllEntities().FirstOrDefault(e => e.ID == id);

                GoldRegister.Add(owner, quantity);
            }
            Console.WriteLine("\tGold register loaded");
        }

        private void ResolveMissingLinks()
        {
            int missingLinkCounter = 0;
            foreach (KeyValuePair<Entity, Dictionary<int, double>> debt in this.UnresolvedLiabilityHoldingDictionary)
            {
                Entity debitor = debt.Key;
                debitor.Liabilities = debt.Value.ToDictionary(pair => GetAllEntities().First(entity => entity.ID == pair.Key), pair => pair.Value);
                missingLinkCounter++;
            }
            foreach (KeyValuePair<Company, Dictionary<int, int>> shareholding in this.UnresolvedShareholdingDictionary)
            {
                Company company = shareholding.Key;
                company.ShareholderStructure = shareholding.Value.ToDictionary(pair => GetAllEntities().First(entity => entity.ID == pair.Key), pair => pair.Value); 
                missingLinkCounter++;
            }
            foreach (KeyValuePair<Company, Dictionary<KeyValuePair<int, int>, int>> shortSale in this.UnresolvedShortSellingDictionary)
            {
                Company company = shortSale.Key;
                company.ShortSellingActivity = shortSale.Value.ToDictionary(
                    pair=> { IEnumerable<Entity> entities = GetAllEntities();
                        return new KeyValuePair<Entity, Entity>(entities.First(e => e.ID == pair.Key.Key), entities.First(e => e.ID == pair.Key.Value)); },
                    pair=>(double)pair.Value
                );
                missingLinkCounter++;
            }
            foreach (KeyValuePair<HedgeFund, int> management in this.UnresolvedFundManagersDictionary)
            {
                HedgeFund fund = management.Key;
                fund.Manager = GetAllEntities().First(entity => entity.ID == management.Value); 
                missingLinkCounter++;
            }
            foreach (KeyValuePair<HedgeFund, Dictionary<int, int>> newMoneySituation in this.UnresolvedNewMoneyInFundDictionary)
            {
                HedgeFund fund = newMoneySituation.Key;
                fund.NewlySubscribedFunds = newMoneySituation.Value.ToDictionary(pair => GetAllEntities().First(entity => entity.ID == pair.Key), pair => pair.Value); 
                missingLinkCounter++;
            }
            foreach (KeyValuePair<HedgeFund, Dictionary<int, HedgeFund.CompStructure>> compStructure in this.UnresolvedFundCompStructuresDictionary)
            {
                HedgeFund fund = compStructure.Key;
                fund.CompensationRules = compStructure.Value.ToDictionary(pair => GetAllEntities().First(entity => entity.ID == pair.Key), pair => pair.Value);
                missingLinkCounter++;
            }
            Console.WriteLine("\tResolved{0} missing entity links", verboseDebug ? " " + missingLinkCounter.ToString(): "");
        }

        private void ReadPlayers(XElement xPlayersElement)
        {
            foreach (var playerElement in xPlayersElement.Elements("player"))
            {
                Player player = new Player("", 0);
                ReadBasicEntityInformation(playerElement, player);
                if (player.ID == ___adminID)
                    this.admin = player;
                this.Players.Add(player);
            }
            Console.WriteLine("\tPlayers loaded");
        }

        private void ReadCompanies(XElement companiesElement)
        {
            foreach (var companyElement in companiesElement.Elements("company"))
            {
                Company company = new Company();
                ReadBasicCompanyInformation(companyElement, company);
                this.Companies.Add(company);
            }
            Console.WriteLine("\tCompanies loaded");
        }

        private void ReadHedgeFunds(XElement fundsElement)
        {
            foreach (var fundElement in fundsElement.Elements("hedgefund"))
            {
                HedgeFund fund = new HedgeFund();
                ReadBasicCompanyInformation(fundElement, fund);

                // manager ID:
                XElement managementElement = fundElement.Element("management");
                int managerID = managementElement.Element("manager").Attribute("id").Value.ToInt();
                this.UnresolvedFundManagersDictionary.Add(fund, managerID);

                // compensation:
                XElement compElement = managementElement.Element("compensation");
                if (compElement.Element("defaults") != null) // new type of compensation rules
                {
                    // defaults:
                    double defaultAssetFee = compElement.Element("defaults").Element("assetfee").Value.ToDouble();
                    double defaultPerformanceFee = compElement.Element("defaults").Element("performancefee").Value.ToDouble();
                    fund.DefaultCompensationRules = new HedgeFund.CompStructure(defaultAssetFee, defaultPerformanceFee);

                    // particulars:
                    Dictionary<int, HedgeFund.CompStructure> compensationRules = new Dictionary<int, HedgeFund.CompStructure>();
                    foreach (var shareholderCompRuleElement in compElement.Element("rules").Elements("shareholder"))
                    {
                        int id = shareholderCompRuleElement.Attribute("id").Value.ToInt();
                        double assetFee = shareholderCompRuleElement.Element("assetfee").Value.ToDouble();
                        double performanceFee = shareholderCompRuleElement.Element("performancefee").Value.ToDouble();
                        compensationRules.Add(id, new HedgeFund.CompStructure(assetFee, performanceFee));
                    }
                    this.UnresolvedFundCompStructuresDictionary.Add(fund, compensationRules);
                }
                else // old files' compensation rules
                {
                    double assetFee = compElement.Element("assetfee").Value.ToDouble();
                    double performanceFee = compElement.Element("performancefee").Value.ToDouble();
                    fund.DefaultCompensationRules = new HedgeFund.CompStructure(assetFee, performanceFee);
                    this.UnresolvedFundCompStructuresDictionary.Add(fund,
                        UnresolvedShareholdingDictionary[fund].ToDictionary(pair => pair.Key, pair => fund.DefaultCompensationRules));
                }
                // vote power
                int votePowerMultiplier = managementElement.Element("votepower").Value.ToInt();
                fund.ManagerVoteMultiplier = votePowerMultiplier;

                //previous shares' values:
                XElement historyElem = fundElement.Element("history");
                foreach (var pair in historyElem.Elements("pair"))
                {
                    fund.PreviousShareValues.Add(pair.Attribute("index").Value.ToInt(), pair.Value.ToDouble());
                }
                

                // new money:
                Dictionary<int, int> unresolvedNewMoney = new Dictionary<int, int>();
                XElement newFundsElem = fundElement.Element("newfunds");
                foreach (var purchaseElem in newFundsElem.Elements("purchase"))
                {
                    unresolvedNewMoney.Add(purchaseElem.Attribute("holderID").Value.ToInt(), purchaseElem.Value.ToInt());
                }
                this.UnresolvedNewMoneyInFundDictionary.Add(fund, unresolvedNewMoney);
                this.HedgeFunds.Add(fund);
            }
            Console.WriteLine("\tFunds loaded");
        }

        private void ReadBasicCompanyInformation(XElement companyElement, Company company)
        {
            ReadBasicEntityInformation(companyElement, company);

            XElement shareholderStructure = companyElement.Element("shareholderstructure");
            Dictionary<int, int> unresolvedSharebook = new Dictionary<int, int>();
            foreach (var shareholderElement in shareholderStructure.Elements("shareholder"))
            {
                int shareHolderID = shareholderElement.Attribute("id").Value.ToInt();
                int shares = shareholderElement.Element("shares").Value.ToInt();
                unresolvedSharebook.Add(shareHolderID, shares);
            }
            this.UnresolvedShareholdingDictionary.Add(company, unresolvedSharebook);
            Dictionary<KeyValuePair<int, int>, int> unresolvedShortSellingBook = new Dictionary<KeyValuePair<int, int>, int>();
            foreach (var shortSellerElement in shareholderStructure.Elements("shortseller"))
            {
                int shortSellerID = shortSellerElement.Attribute("id").Value.ToInt();
                int shareLenderID = shortSellerElement.Element("lender").Value.ToInt();
                int shareCount = shortSellerElement.Element("shares").Value.ToInt();
                unresolvedShortSellingBook.Add(new KeyValuePair<int, int>(shortSellerID, shareLenderID), shareCount);
            }
            this.UnresolvedShortSellingDictionary.Add(company, unresolvedShortSellingBook);

            int peg = companyElement.Element("peg").Value.ToInt();
            Players.First(player => player.ID == peg).PeggedCompanies.Add(company);
        }

        private void ReadBasicEntityInformation(XElement element, Entity entity)
        {
            Func<string, string> read = str => readerFunc(element, str);

            entity.ID = element.Attribute("id").Value.ToInt();
            entity.Name = read("name");
            entity.Money = read("cash").ToDouble();
            entity.MoneyIn = read("cashin").ToDouble();
            entity.MoneyOut = read("cashout").ToDouble();
            entity.PassedStartCounter = read("passcounter").ToInt();
            entity.LiabilityTowardsBank = element.Element("liabilities").Element("bankdebt").Value.ToDouble();
            List<Deposit> deposits = new List<Deposit>();
            foreach (var depositElement in element.Element("deposits").Elements("deposit"))
            {
                read = str => readerFunc(depositElement, str);
                int depoID = depositElement.Attribute("id").Value.ToInt();
                double principal = read("principal").ToDouble();
                double interestAcc = read("accinterest").ToDouble();
                int roundsPassed = read("roundspassed").ToInt();
                int totalRounds = read("totalrounds").ToInt();

                Deposit deposit = new Deposit(principal, interestAcc, roundsPassed, totalRounds, depoID, IRB);
                deposits.Add(deposit);
            }
            entity.Deposits = deposits;
            entity.Liabilities = new Dictionary<Entity, double>(10);
            Dictionary<int, double> liabilities = new Dictionary<int, double>();
            foreach (var debtElement in element.Element("liabilities").Elements("debt"))
            {
                read = str => readerFunc(debtElement, str);
                int creditorID = debtElement.Attribute("to").Value.ToInt();
                double value = read("value").ToDouble();
                liabilities.Add(creditorID, value);
            }
            this.UnresolvedLiabilityHoldingDictionary.Add(entity, liabilities);
        }
    }
}