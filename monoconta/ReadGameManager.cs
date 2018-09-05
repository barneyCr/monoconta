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
            this.UnresolvedFundManagersDictionary = new Dictionary<HedgeFund, int>();
            this.UnresolvedNewMoneyInFundDictionary = new Dictionary<HedgeFund, Dictionary<int, int>>();
        }

        private List<Player> Players = new List<Player>();
        private List<Company> Companies = new List<Company>();
        private List<HedgeFund> HedgeFunds = new List<HedgeFund>();
        private List<Property> Properties = new List<Property>();
        private List<Neighbourhood> Neighbourhoods = new List<Neighbourhood>();

        private string gameName;
        private double IRB;
        private Player admin;
        private double _m, startbonus;
        private int depocounter;

        internal void Integrate(
            out List<Player> players, out List<Company> companies,
            out List<HedgeFund> hedgeFunds, List<Property> properties,
            out Player admin, out string name,
            out double irb, out double m,
            out double startbonus, out int depocounter
        )
        {
            players = this.Players;
            companies = this.Companies;
            hedgeFunds = this.HedgeFunds;
            //properties = this.Properties;
            admin = this.admin;
            name = this.gameName;
            irb = this.IRB;
            m = this._m;
            startbonus = this.startbonus;
            depocounter = this.depocounter;
            try
            {
                Company.ID_COUNTER_BASE = GetAllEntities().Max(e => e.ID);
            }
            catch (Exception e) {
                throw new Exception("No entities...");
            }
        }

        private int ___adminID;
        private readonly Dictionary<Entity, Dictionary<int, double>> UnresolvedLiabilityHoldingDictionary;
        private readonly Dictionary<Company, Dictionary<int, int>> UnresolvedShareholdingDictionary;
        private readonly Dictionary<HedgeFund, int> UnresolvedFundManagersDictionary;
        private readonly Dictionary<HedgeFund, Dictionary<int, int>> UnresolvedNewMoneyInFundDictionary;

        private Func<XElement, string, string> readerFunc = (el, str) => el.Element(str).Value;

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


        public void Read()
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
                    ReadEntities(game.Element("entities"));
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
            Console.WriteLine("\tConfig loaded");
        }

        private void ReadEntities(XElement entitiesElement)
        {
            ReadPlayers(entitiesElement.Element("players"));
            ReadCompanies(entitiesElement.Element("firms").Element("companies"));
            ReadHedgeFunds(entitiesElement.Element("firms").Element("hedgefunds"));
            ResolveMissingLinks();
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
                double assetFee = compElement.Element("assetfee").Value.ToDouble();
                double performanceFee = compElement.Element("performancefee").Value.ToDouble();
                fund.Compensation = new HedgeFund.CompStructure(assetFee, performanceFee);
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