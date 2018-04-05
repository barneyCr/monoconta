using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

#pragma warning disable RECS0063 // Warns when a culture-aware 'StartsWith' call is used by default.

namespace monoconta
{
	[DebuggerDisplay("{Name}, {ID}, {Money}")]
	class Player
	{
		public static int IDBASE = 0;
		public Player(string name, int id)
		{
			this.Name = name;
			this.ID = id;
			Liabilities = new Dictionary<Player, double>();
		}

		public string Name;
		public int ID, PassedStartCounter;
		public double Money
		{
			get { return _money; }
			set
			{
				if (value > _money) MoneyIn += value-_money;
				else MoneyOut += value-_money;
				_money = value;
			}
		}
		public double MoneyIn { get; private set; }
		public double MoneyOut { get; private set; }

		public double LiabilityTowardsBank = 0;
		public Dictionary<Player, double> Liabilities;

		double _money;

		public void PrintCash() {
			Console.WriteLine("[({0}){1}] Cash: {2}", ID, Name, Money);
		}
		public void PrintSituation() {
			PrintCash();
			Console.WriteLine("IN: {0}, OUT: {1}", MoneyIn, MoneyOut);
			Console.WriteLine("Passed start {0} times", PassedStartCounter);
			Console.WriteLine("Liabilities: ");
			Console.WriteLine("\t{0} towards bank", LiabilityTowardsBank);
			foreach (var credit in Liabilities)
			{
				Console.WriteLine("\t{0} towards {1}", credit.Value, credit.Key.Name);
			}
			Console.WriteLine("Loans made: ");

			var book = from player in MainClass.Players
					   from debt in player.Liabilities
					   where debt.Key == this
					   select new { Debtor = player, Debt = debt.Value };

			foreach (var credit in book)
			{
				Console.WriteLine("\t{0} to be received from {1}", credit.Debt, credit.Debtor);
			}
            Console.WriteLine("-----\n");
		}
	}

	class MainClass
	{      
		public static List<Player> Players;
		public static double InterestRateBase=1;
		public static Player admin;

		public static void Main(string[] args)
		{
			LoadGame();
			Console.WriteLine("Done!\n\n");
			while (true)
			{
				Console.WriteLine("\n\n");
				foreach (var player in Players)
				{
					Console.Write("{0}: {1};\t", player.Name, player.Money);
				}
				Console.Write("\n>>: ");
				try
				{
					string command = Console.ReadLine();
					if (command.ToLower().StartsWith("credit"))
					{
						var player = ByID(ReadInt("ID: "));
						double amount = ReadDouble("Sum: ");
						if (player == admin&& char.IsUpper(command[0]))
							amount *= 1.0875;
						player.Money += amount;
						player.PrintCash();
					}
					else if (command.ToLower().StartsWith("debit"))
					{
						var player = ByID(ReadInt("ID: "));
						double amount = ReadDouble("Sum: ");
						if (player == admin&& char.IsUpper(command[0]))
							amount *= 0.925;
						player.Money -= amount;
						player.PrintCash();
					}
					else if (command == "transfer")
					{
						var beneficiary = ByID(ReadInt("ID of person receiving money: "));
						var payer = ByID(ReadInt("ID of person paying: "));
						double sum = ReadDouble("Sum: ");
						beneficiary.Money += sum;
						payer.Money -= sum;
						beneficiary.PrintCash();
						payer.PrintCash();
					}
					else if (command.ToLower().StartsWith("pass"))
					{
						var player = ByID(ReadInt("ID: "));
						player.PassedStartCounter++;
						player.Money += 2000 * (command.StartsWith("PASS") ? 2 : 1);
                        
						player.LiabilityTowardsBank *= ((InterestRateBase / 3) / 100 + 1);

						foreach (var creditor in player.Liabilities.Keys.ToList())
						{
							player.Liabilities[creditor] *= ((InterestRateBase / 2) / 100 + 1);
						}
					}
					else if (command.ToLower() == "loan")
					{
						var player = ByID(ReadInt("Who is getting the loan? "));
						int from = ReadInt("From whom? ");
						double amount = ReadDouble("Amount: ");
						if (from == 0) // bank
						{
							player.LiabilityTowardsBank += amount;
							if (player == admin&& char.IsUpper(command[0]))
								amount *= 1.1;
							player.Money += amount;
						}
						else
						{
							var creditor = ByID(from);
							if (creditor.Money > amount)
							{
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
							else
							{
								Console.WriteLine("Not enough money in creditor's account");
							}
						}
					}
					else if (command.StartsWith("print"))
					{
						var player = ByID(ReadInt("For which player? "));
						if (command == "printcash")
							player.PrintCash();
						else 
							player.PrintSituation();
					}
					else if (command == "rate") {
						InterestRateBase = ReadDouble("New rate: ");
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
							if (repayer == admin&& char.IsUpper(command[0]))
								amount *= 0.85;
							repayer.Money -= amount;
						}

 					}
					else if (command.ToLower()=="refinance") {
						var debtor = ByID(ReadInt("Who has the debt?"));
						var financier = ByID(ReadInt("Who is offering the new debt?"));
						double amount = ReadDouble("How much of the debt?");
						debtor.LiabilityTowardsBank -= amount;
						if (financier == admin && char.IsUpper(command[0]))
						    debtor.Liabilities[financier] += amount*1.05;
						financier.Money -= amount;
					}
					else if (command == "whoisadmin") {
						if(admin!=null)
							Console.WriteLine(admin.Name);
					}
				}
				catch (Exception e)
				{
					Console.WriteLine(e.Message);
				}
			}
		}
        
		public static void LoadGame() {
			Console.WriteLine("Players' names:");
			string[] names = Console.ReadLine().Split(',').Select(s => s.Trim()).ToArray();
			double startingAmount = ReadDouble("\nStarting amount: ");
			Players = new List<Player>(names.Select(n => new Player(n, ++Player.IDBASE) { Money = startingAmount }));
			MainClass.admin = Players.FirstOrDefault(p => char.IsUpper(p.Name[0]));
			InterestRateBase = ReadDouble("Set interest rate base: ");
		}

		public static double ReadDouble(string str = null) {
			if (str != null) 
				Console.Write(str);
			if (double.TryParse(Console.ReadLine(), out double s))
				return s;
			else return double.NaN;
		}
		public static int ReadInt(string str = null)
        {
            if (str != null)
                Console.Write(str);
			if (int.TryParse(Console.ReadLine(), out int s))
				return s;
			else return int.MinValue;
        }

		public static Player ByID(string s) {
			return Players.First(p => p.ID == int.Parse(s));
		}

		public static Player ByID(int s) {
			return Players.First(p => p.ID == s);
		}
	}
}
#pragma warning restore RECS0063 // Warns when a culture-aware 'StartsWith' call is used by default.
