using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using static monoconta.HedgeFund;
using System.Threading;
using monoconta.Contracts;

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

        public static IEnumerable<IDescribable> ContractCollection
        {
            get => InterestRateSwapContracts.Cast<IDescribable>().Union(RentSwapContracts).Union(RentInsuranceContracts);
        }

        public static List<Player> Players;
        public static List<Company> Companies;
        public static List<HedgeFund> HedgeFunds;

        public static List<Property> Properties;
        public static List<Neighbourhood> Neighbourhoods;

        public static List<InterestRateSwap> InterestRateSwapContracts;
        public static List<RentSwapContract> RentSwapContracts;
        public static List<RentInsuranceContract> RentInsuranceContracts;

        public static double InterestRateBase = 1;
        public static string GameName = "";
        public static Player admin;
        public static bool showIDs = true;

        public static double _m_ = (double)5 / 3;
        public static double startBonus = 2000;

        public static int depocounter = 0;
        public static double SSFR18 = 0;

        public static SaveGameManager SGManager;
        public static DiceManager DiceManager;

        static Random rand = new Random();
        public static bool financedeficit = true;

        public static double InterplayerBaseRate { get => InterestRateBase / 3; }
        public static double BankBaseRate { get => InterestRateBase / 2; }

        public static void Main(string[] args)
        {
            LoadGame(args);
            Run();
        }

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

            Dictionary<Entity, double> goldRegToBeRead = null;

            if (args.Length >= 1)
            {
                Console.Write("Verbose debug: ");

                ReadGameManager readManager = new ReadGameManager(fileName, Console.ReadLine() == "yes");
                readManager.Read(Properties);
                readManager.Integrate(out Players, out Companies, out HedgeFunds, out RentSwapContracts, out RentInsuranceContracts, out goldRegToBeRead, out admin, out GameName, out InterestRateBase, out _m_, out startBonus, out depocounter, out SSFR18);
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

                RentSwapContracts = new List<RentSwapContract>();
                RentInsuranceContracts = new List<RentInsuranceContract>();

                Console.WriteLine("Done!\n\n");
            }
            InterestRateSwapContracts = new List<InterestRateSwap>();
            GoldManager.InitializeNew(goldRegToBeRead);
            DiceManager = new DiceManager();
            watch.Stop();
        }

        static double CalculateRealEstateValues(Entity arg)
        {
            double personal = arg.RealEstateAssetsValue;
            return personal + 0; // todo
        }
       
        private static Property ReadProperty()
        {
            return GetProperty(ReadInt("Property ID: "));

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
