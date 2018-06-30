using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

#pragma warning disable RECS0063 // Warns when a culture-aware 'StartsWith' call is used by default.

namespace monoconta
{
	    

	class MainClass
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
		public static double InterestRateBase=1;
		public static Player admin;

		public static double _m_ = (double)5 / 3;
		public static double startBonus = 2000;

		public static int depocounter = 0;

		public static void Main(string[] args)
		{
			LoadGame();
			Console.WriteLine("Done!\n\n");
			while (true)
			{
				Console.WriteLine("\n\n");
				foreach (var player in Players)
				{
					Console.Write("{0}: {1:C};\t", player.Name, player.Money);
				}
				Console.WriteLine();
                foreach (var company in Companies)
                {
                    Console.Write("{0}: {1:C};\t", company.Name, company.Money);
				}Console.WriteLine();
				foreach (var company in HedgeFunds)
                {
                    Console.Write("{0}: {1:C};\t", company.Name, company.Money);
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
						var player = ByID(ReadInt("ID: "));
						double amount = ReadDouble("Sum: ");
						if (player == admin && char.IsUpper(command[0]))
							amount *= 0.9125;
						player.Money -= amount;
						player.PrintCash();
					}
					else if (command.ToLower() == "transfer")
					{
						var beneficiary = ByID(ReadInt("ID of person receiving money: "));
						var payer = ByID(ReadInt("ID of person paying: "));
						double sum = ReadDouble("Sum: ");
						beneficiary.Money += sum * (beneficiary == admin && char.IsUpper(command[0]) ? 1.125 : 1);
						payer.Money -= sum * (payer == admin && char.IsUpper(command[0]) ? 0.9 : 1);
						beneficiary.PrintCash();
						payer.PrintCash();
					}
					else if (command.ToLower().StartsWith("pass"))
					{
						Player player = ByID(ReadInt("ID: ")) as Player;
						if (player != null)
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
							if (player.PeggedEntities.Count > 0)
							{
								Console.Write("Show pegged entities status?");
								bool showpegged = Console.ReadLine() == "yes";
								foreach (var ent in player.PeggedEntities)
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
												Console.WriteLine("Deposit reached term");
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
					else if (command.ToLower() == "loan")
					{
						var player = ByID(ReadInt("Who is getting the loan? "));
						int from = ReadInt("From whom? ");
						double amount = ReadDouble("Amount: ");
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
						//foreach (var deposit in Players.SelectMany(p => p.Deposits))
						//{

						//}
					}
					else if (command.ToLower() == "rateinfo")
					{
						Console.WriteLine("{0:F3}% - interest rate for interplayer loans", InterestRateBase / 3);
						Console.WriteLine("{0:F3}% - interest rate for bank loans", InterestRateBase / 2);
						int infmax = ReadInt("Interest rates on X rounds: X = ");
						for (int i = 1; i <= infmax; i++)
						{
							Console.WriteLine("\t{0:F4}% - interest rate for {1}-round deposit", Deposit.CalculateDepositInterestRate(i, char.IsUpper(command[0])), i);
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
						var debtor = ByID(ReadInt("Who has the debt?"));
						var financier = ByID(ReadInt("Who is offering the new debt?"));
						double amount = ReadDouble("How much of the debt?");
						double commission = ReadDouble("Commission (%): ") / 100;
						debtor.LiabilityTowardsBank -= amount;
						if (debtor.Liabilities.ContainsKey(financier))
							debtor.Liabilities[financier] += amount * (1 + commission);
						else
							debtor.Liabilities.Add(financier, amount * (1 + commission));
						financier.Money -= amount * (financier == admin && char.IsUpper(command[0]) ? .945 : 1);
					}
					else if (command == "deposit")
					{
						CreateDeposit();
					}
					else if (command == "bet")
					{

					}
					else if (command == "whoisadmin")
					{
						if (admin != null)
							Console.WriteLine(admin.Name);
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
						int id = ReadInt("ID: ");
						var data = from player in Players
								   from deposit in player.Deposits
								   where deposit.UID == id
								   select new { Deposit = deposit, Player = player };

						var removed = data.FirstOrDefault();
						if (removed != null)
						{
							removed.Player.Deposits.Remove(removed.Deposit);
							removed.Player.Money += removed.Deposit.Principal;
						}
					}
					else if (command == "createcompany")
					{
						Console.Write("Name: ");
						string name = Console.ReadLine();
						Entity founder = ByID(ReadInt("Founder ID: "));
						double capital = ReadDouble("Initial capital: ");
						double shareprice = ReadDouble("Share price: ");
						Player pegPlayer = ByID(ReadInt("Pegged player ID: ")) as Player;
						if (pegPlayer != null)
						{
							Company company = new Company(name, founder, capital, shareprice);
							Companies.Add(company);
							pegPlayer.PeggedEntities.Add(company);
						}
					}
					else if (command == "issueshares")
					{
						Entity buyer = ByID(ReadInt("Buyer ID: "));
						Company issuer = ByID(ReadInt("Issuer ID: ")) as Company;
						if (buyer == issuer)
							throw new Exception("An entity cannot buy shares of itself!");
						double sum = ReadDouble("Cash invested: ");
						if (sum > 0)
						{
							if (issuer != null)
							{
								(issuer as Company).SubscribeNewShareholder(buyer, sum);
							}
						}
					}
					else if (command == "buybackshares")
					{
						Entity holder = ByID(ReadInt("Holder ID: "));
						Entity buyer = ByID(ReadInt("Buyer ID: "));
						if (holder == buyer)
							throw new Exception("An entity cannot buy shares of itself!");
						int shares = ReadInt("Shares: ");
						if (shares > 0)
						{
							if (buyer is Company)
							{
								(buyer as Company).BuyBackShares(holder, shares);
							}
						}
					}
					else if (command == "dividend")
					{
						Company issuer = ByID(ReadInt("Which entity? ID: ")) as Company;
						if (issuer != null)
						{
							Console.WriteLine("Share price: {0:C}, liquid/share: {1:C}.", issuer.ShareValue, issuer.Money / issuer.ShareCount);
							double amountPerShare = ReadDouble("Amount/share: ");
							issuer.IssueDividend(amountPerShare);
						}
					}
					else if (command == "rename")
					{
						Entity entity = ByID(ReadInt("Which entity? ID: "));
						Console.Write("New name: ");
						entity.Name = Console.ReadLine();
					}
					else if (command == "changepeg")
					{
						Entity pegged = ByID(ReadInt("Which entity? ID: "));
						Player newPlayer = ByID(ReadInt("New player ID: ")) as Player;
						if (newPlayer != null)
						{
							newPlayer.PeggedEntities.Add(pegged);
							MainClass.Players.FirstOrDefault(p => p.PeggedEntities.Contains(pegged)).PeggedEntities.Remove(pegged);
						}
					}
					else if (command == "createfund")
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
							pegPlayer.PeggedEntities.Add(fund);
						}
					}
					else if (command == "changefee")
					{
						HedgeFund fund = ByID(ReadInt("Fund ID: ")) as HedgeFund;
						if (fund != null)
						{
							Console.WriteLine("{0:F2}% and {1:F2}%", fund.Compensation.AssetFee, fund.Compensation.PerformanceFee);
							double ff = ReadDouble("Fixed fee: ");
							double pf = ReadDouble("Performance fee: ");
							fund.Compensation = new CompStructure(ff, pf);
						}
					}
					else if (command == "changemanager")
					{
						HedgeFund fund = ByID(ReadInt("Fund ID: ")) as HedgeFund;
						Entity manager = ByID(ReadInt("New Manager ID: ")) ?? fund.Manager;
						fund.Manager = manager;
					}
					else if (command == "deleteentity")
					{
						Entity entity = ByID(ReadInt("Entity ID: "));
						if (entity is Company)
							Companies.Remove(entity as Company);
						else if (entity is HedgeFund)
							HedgeFunds.Remove(entity as HedgeFund);

						foreach (var player in Players)
						{
							if (player.PeggedEntities.Contains(entity))
								player.PeggedEntities.Remove(entity);
						}
					}
				}
				catch (Exception e)
				{
					Console.WriteLine(e.Message);
				}
			}
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
				deposits.Add(new Deposit(now, Deposit.CalculateDepositInterestRate(rounds, true), rounds, ++depocounter));
				generatedSum += now;
			}
			deposits.Add(new Deposit(val - generatedSum, Deposit.CalculateDepositInterestRate(rounds, true), rounds, ++depocounter));
			admin.Deposits.AddRange(deposits);
            admin.Money -= val;
		}

		public static void LoadGame() {
			Console.WriteLine("Players' names:");
			string[] names = Console.ReadLine().Split(',').Select(s => s.Trim()).ToArray();
			double startingAmount = ReadDouble("\nStarting amount: ");
			Players = new List<Player>(names.Select(n => new Player(n, ++Player.IDBASE) { Money = startingAmount }));
			MainClass.admin = Players.FirstOrDefault(p => char.IsUpper(p.Name[0]));
			InterestRateBase = ReadDouble("Set interest rate base: ");
			Company.ID_COUNTER_BASE = Player.IDBASE;

			Companies = new List<Company>();
			HedgeFunds = new List<HedgeFund>();
		}

		public static double ReadDouble(string str = null) {
			if (str != null) 
				Console.Write(str);
			if (double.TryParse(Console.ReadLine(), out double s))
				return s;
			else return 0;
		}
		public static int ReadInt(string str = null)
        {
            if (str != null)
                Console.Write(str);
			if (int.TryParse(Console.ReadLine(), out int s))
				return s;
			else return 0;
        }

		public static Entity ByID(string s) {
			return Entities.First(p => p.ID == int.Parse(s));
		}

		public static Entity ByID(int s) {
			return Entities.First(p => p.ID == s);
		}
	}
}
#pragma warning restore RECS0063 // Warns when a culture-aware 'StartsWith' call is used by default.
