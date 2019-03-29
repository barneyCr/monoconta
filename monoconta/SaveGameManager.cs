﻿using System;
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
    public class SaveGameManager
    {
        public SaveGameManager(string name, string path)
        {
            this.gameName = name;
            this._pathToFile = path;
        }

        private List<Player> Players;
        private List<Company> Companies;
        private List<HedgeFund> HedgeFunds;
        private List<Property> Properties;
        private List<Neighbourhood> Neighbourhoods;
        private List<InterestRateSwap> InterestRateSwaps;
        private List<RentSwapContract> RentSwaps;

        private double IRB;
        private Player admin;
        private double _m, startbonus;
        private int depocounter;
        private double SSRF18;

        internal void set(
            List<Player> players, List<Company> companies, List<HedgeFund> hedgeFunds,
            List<Property> properties, List<Neighbourhood> neighbourhoods, List<RentSwapContract> rentSwaps,
            double irb, Player admin, double m, double bonus, int depocounter, double ssfr18)
        {
            this.Players = players;
            this.Companies = companies;
            this.HedgeFunds = hedgeFunds;
            this.Properties = properties;
            this.Neighbourhoods = neighbourhoods;
            //this.InterestRateSwaps = interestRateSwaps;
            this.RentSwaps = rentSwaps;
            this.IRB = irb;
            this.admin = admin;
            this._m = m;
            this.startbonus = bonus;
            this.depocounter = depocounter;
            this.SSRF18 = ssfr18;
        }

        private string gameName;
        private string _pathToFile;

        public void changePath(string newOne) {
            this._pathToFile = newOne;
        }

        public void Save()
        {
            var settings = new XmlWriterSettings
            {
                Indent = true
            };

            if (!_pathToFile.EndsWith(".xml"))
                _pathToFile += ".xml";
            using (XmlWriter xml = XmlWriter.Create(Environment.CurrentDirectory + "/" + _pathToFile, settings))
            {
                xml.WriteStartDocument();

                xml.WriteStartElement("game");
                xml.WriteAttributeString("name", gameName);

                WriteConfig(xml);
                WriteAllEntities(xml);
                WriteProperties(xml);
                WriteContracts(xml);
                WriteGold(xml);

                xml.WriteEndElement(); // game
                xml.WriteEndDocument();
            }
        }

        private void WriteConfig(XmlWriter xml)
        {
            xml.WriteStartElement("config");
            xml.WriteElementString("irb", IRB.ToF3Double());
            if (admin != null)
                xml.WriteElementString("admin", admin.ID.ToString());
            xml.WriteElementString("m", _m.ToF3Double());
            xml.WriteElementString("startbonus", startbonus.ToF3Double());
            xml.WriteElementString("depocounter", depocounter.ToString());
            xml.WriteElementString("shortsellratefactor18", SSRF18 == 0 ? 19.4.ToF3Double() : SSRF18.ToF3Double());
            xml.WriteEndElement(); // config
        }

        private void WriteAllEntities(XmlWriter xml)
        {
            xml.WriteStartElement("entities");

            xml.WriteStartElement("players");
            foreach (var player in this.Players)
            {
                xml.WriteStartElement("player");
                xml.WriteAttributeString("id", player.ID.ToString());
                this.WriteBasicEntityProperties(xml, player);
                xml.WriteEndElement(); // player
            }
            xml.WriteEndElement(); // players

            xml.WriteStartElement("firms");
            xml.WriteStartElement("companies");
            foreach (var comp in Companies)
            {
                xml.WriteStartElement("company");
                xml.WriteAttributeString("id", comp.ID.ToString());
                {
                    this.WriteBasicEntityProperties(xml, comp);
                    this.WriteBasicCompanyProperties(xml, comp);
                }
                xml.WriteEndElement();//company
            }
            xml.WriteEndElement(); // companies

            xml.WriteStartElement("hedgefunds");
            foreach (var fund in HedgeFunds)
            {
                xml.WriteStartElement("hedgefund");
                xml.WriteAttributeString("id", fund.ID.ToString());
                {
                    this.WriteBasicEntityProperties(xml, fund);
                    this.WriteBasicCompanyProperties(xml, fund);
                    xml.WriteStartElement("management");
                    {
                        xml.WriteStartElement("manager");
                        {
                            xml.WriteAttributeString("id", fund.Manager.ID.ToString());
                            xml.WriteString(fund.Manager.Name);
                        }
                        xml.WriteEndElement(); // manager
                        xml.WriteStartElement("compensation");
                        {
                            xml.WriteElementString("assetfee", fund.Compensation.AssetFee.ToF3Double());
                            xml.WriteElementString("performancefee", fund.Compensation.PerformanceFee.ToF3Double());
                        }
                        xml.WriteEndElement(); // compensation
                        xml.WriteElementString("votepower", fund.ManagerVoteMultiplier.ToString());
                        xml.WriteEndElement(); // management

                        xml.WriteStartElement("history");
                        {
                            foreach (var item in fund.PreviousShareValues)
                            {
                                xml.WriteStartElement("pair");
                                xml.WriteAttributeString("index", item.Key.ToString());
                                xml.WriteString(item.Value.ToF3Double());
                                xml.WriteEndElement(); // pair
                            }
                        }
                        xml.WriteEndElement(); // history
                        xml.WriteStartElement("newfunds");
                        {
                            foreach (var item in fund.NewlySubscribedFunds)
                            {
                                xml.WriteStartElement("purchase");
                                xml.WriteAttributeString("holderID", item.Key.ID.ToString());
                                xml.WriteString(item.Value.ToString());
                                xml.WriteEndElement(); // purchase
                            }
                        }
                        xml.WriteEndElement(); // newfunds
                    }
                }
                xml.WriteEndElement();//hedgefund
            }
            xml.WriteEndElement(); // hedgefunds
            xml.WriteEndElement(); // firms
            xml.WriteEndElement(); // entities
        }

        private void WriteProperties(XmlWriter xml)
        {
            xml.WriteStartElement("properties");
            foreach (var prop in (from pr in Properties where pr.Owner != null || pr.OptionOwner != null select pr))
            {
                xml.WriteStartElement("property");
                xml.WriteAttributeString("pid", prop.ID.ToString());
                xml.WriteElementString("fullname", prop.Name);
                if (prop.Owner != null)
                {
                    xml.WriteElementString("ownerid", prop.Owner.ID.ToString());
                    xml.WriteElementString("levels", prop.CompleteLevels.ToString());
                    xml.WriteElementString("appartments", prop.Appartments.ToString());
                    xml.WriteElementString("rentflowin", prop.RentFlowIn.ToString());
                    xml.WriteElementString("moneyflowout", prop.MoneyFlowOut.ToString());
                }
                else if (prop.OptionOwner != null)
                {
                    xml.WriteElementString("optionownerid", prop.OptionOwner.ID.ToString());
                }
                xml.WriteEndElement();
            }
            xml.WriteEndElement();
        }

        private void WriteBasicCompanyProperties(XmlWriter xml, Company comp)
        {
            xml.WriteStartElement("shareholderstructure");
            xml.WriteElementString("sharecount", comp.ShareCount.ToString());
            xml.WriteElementString("sharevalue", comp.ShareValue.ToF3Double());
            foreach (var shareholder in comp.ShareholderStructure)
            {
                xml.WriteStartElement("shareholder");
                xml.WriteAttributeString("id", shareholder.Key.ID.ToString());
                xml.WriteElementString("fullname", shareholder.Key.Name);
                xml.WriteElementString("shares", comp.GetSharesOwnedBy(shareholder.Key,false).ToString());
                xml.WriteElementString("pctg", string.Format("{0:F3}%", comp.GetOwnershipPctg(shareholder.Key, false)));
                xml.WriteEndElement(); // shareholder
            }
            foreach (var shortSeller in comp.ShortSellingActivity)
            {
                xml.WriteStartElement("shortseller");
                xml.WriteAttributeString("id", shortSeller.Key.Value.ID.ToString());
                xml.WriteElementString("fullname", shortSeller.Key.Value.Name);
                xml.WriteElementString("lender", shortSeller.Key.Key.ID.ToString());
                xml.WriteElementString("shares", shortSeller.Value.ToString());
                xml.WriteElementString("pctg", string.Format("{0:F3}%", comp.GetSharesShortedBy(shortSeller.Key.Value)/comp.ShareCount*100));
                xml.WriteEndElement(); // shortseller
            }

            xml.WriteEndElement(); // shareholderstructure
            xml.WriteElementString("peg", Players.First(player => player.PeggedCompanies.Contains(comp)).ID.ToString());
        }

        private void WriteBasicEntityProperties(XmlWriter xml, Entity entity)
        {
            xml.WriteElementString("name", entity.Name);
            xml.WriteElementString("cash", entity.Money.ToF3Double());
            xml.WriteElementString("cashin", entity.MoneyIn.ToF3Double());
            xml.WriteElementString("cashout", entity.MoneyOut.ToF3Double());
            xml.WriteElementString("passcounter", entity.PassedStartCounter.ToString());

            xml.WriteStartElement("deposits");
            foreach (var deposit in entity.Deposits)
            {
                xml.WriteStartElement("deposit");
                xml.WriteAttributeString("id", deposit.DepositID.ToString());
                xml.WriteElementString("principal", deposit.Principal.ToF3Double());
                xml.WriteElementString("accinterest", deposit.TotalInterest.ToF3Double());
                xml.WriteElementString("capbase", deposit.CurrentCapitalBase.ToF3Double());
                xml.WriteElementString("roundspassed", deposit.RoundsPassed.ToString());
                xml.WriteElementString("totalrounds", deposit.TotalRounds.ToString());
                xml.WriteEndElement();
            }
            xml.WriteEndElement(); // deposits

            xml.WriteStartElement("liabilities");
            xml.WriteElementString("bankdebt", entity.LiabilityTowardsBank.ToF3Double());
            foreach (var liab in entity.Liabilities)
            {
                xml.WriteStartElement("debt");
                xml.WriteAttributeString("to", liab.Key.ID.ToString());
                xml.WriteElementString("fullname", liab.Key.Name.ToString());
                xml.WriteElementString("value", liab.Value.ToF3Double());
                xml.WriteEndElement();
            }
            xml.WriteEndElement(); // liabilities
        }

        private void WriteContracts(XmlWriter xml)
        {
            xml.WriteStartElement("contracts");

            xml.WriteStartElement("rentswaps");
            foreach (var swap in this.RentSwaps)
            {
                xml.WriteStartElement("rentswap");
                xml.WriteAttributeString("id", swap.ID.ToString());
                {
                    xml.WriteElementString("name", swap.Name);
                    {
                        xml.WriteStartElement("longparty");
                        xml.WriteAttributeString("id", swap.LongParty.ID.ToString());
                        xml.WriteAttributeString("name", swap.LongParty.Name);
                        xml.WriteEndElement(); // longparty
                    }
                    {
                        xml.WriteStartElement("shortparty");
                        xml.WriteAttributeString("id", swap.ShortParty.ID.ToString());
                        xml.WriteAttributeString("name", swap.ShortParty.Name);
                        xml.WriteEndElement(); // shortparty
                    }
                    xml.WriteElementString("propID", swap.PropertyID.ToString());
                    xml.WriteElementString("fixedRent", swap.Terms.Sum.ToF3Double());
                    xml.WriteElementString("roundrentmulti", swap.RoundRentMultiplier.ToF3Double());
                    xml.WriteElementString("roundspassed", swap.RoundsPassed.ToString());
                    xml.WriteElementString("totalrounds", swap.Terms.Rounds.ToString());
                    xml.WriteStartElement("entries");
                    {
                        foreach (var entry in swap.ContractEntries)
                        {
                            xml.WriteStartElement("entry");
                            xml.WriteAttributeString("no", entry.Key.ToString());
                            xml.WriteElementString("fixed", entry.Value.ObjectTransferred.ToF3Double());
                            xml.WriteElementString("var", entry.Value.ContractElement.ToF3Double());
                            xml.WriteElementString("msg", entry.Value.Message);
                            xml.WriteEndElement();//entry
                        }
                    }
                    xml.WriteEndElement();//entries
                }
                xml.WriteEndElement(); // rentswap
            }
            xml.WriteEndElement(); // rentswaps

            xml.WriteStartElement("irswaps");
            //foreach (var swap in this.InterestRateSwaps)
            //{
            //    xml.WriteStartElement("irswap");
            //    xml.WriteAttributeString("id", swap.ID.ToString());
            //    {
            //        //this.WriteBasicEntityProperties(xml, swap);
            //        //this.WriteBasicCompanyProperties(xml, swap);
            //    }
            //    xml.WriteEndElement();//irswap
            //}
            xml.WriteEndElement(); // irswaps

            xml.WriteEndElement(); // contracts

        }

        private void WriteGold(XmlWriter xml)
        {
            xml.WriteStartElement("gold");
            {
                xml.WriteElementString("price", GoldManager.CurrentGoldPrice.ToF3Double());
                xml.WriteStartElement("deltas");
                {
                    xml.WriteElementString("downmax", GoldManager.DownDeltaMax.ToF3Double());
                    xml.WriteElementString("downmin", GoldManager.DownDeltaMin.ToF3Double());
                    xml.WriteElementString("upmin", GoldManager.UpDeltaMin.ToF3Double());
                    xml.WriteElementString("upmax", GoldManager.UpDeltaMax.ToF3Double());
                    xml.WriteElementString("fiverange", GoldManager.MaximumFiveDeviation.ToF3Double());
                }
                xml.WriteEndElement(); // deltas
                xml.WriteStartElement("register");
                foreach (var item in GoldManager.GoldRegister)
                {
                    xml.WriteStartElement("entry");
                    xml.WriteAttributeString("id", item.Key.ID.ToString());
                    xml.WriteElementString("fullname", item.Key.Name);
                    xml.WriteElementString("qty", item.Value.ToF3Double());
                    xml.WriteEndElement(); //entry
                }
                xml.WriteEndElement(); //register
            }
            xml.WriteEndElement(); //gold
        }
    }
}
