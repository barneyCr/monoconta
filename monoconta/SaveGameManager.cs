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

        internal void set(
            List<Player> players, List<Company> companies, List<HedgeFund> hedgeFunds, List<Property> properties, List<Neighbourhood> neighbourhoods)
        {
            this.Players = players;
            this.Companies = companies;
            this.HedgeFunds = hedgeFunds;
            this.Properties = properties;
            this.Neighbourhoods = neighbourhoods;
        }

        private string gameName;
        private string _pathToFile;

        public void Save()
        {
            var settings = new XmlWriterSettings
            {
                Indent = true
            };

            using (XmlWriter xml = XmlWriter.Create(Environment.CurrentDirectory + "/" + _pathToFile, settings))
            {
                xml.WriteStartDocument();
                xml.WriteStartElement("game");
                xml.WriteAttributeString("name", gameName);

                xml.WriteStartElement("entities");
                //var entities = Players.Cast<Entity>().Union(Companies.Cast<Entity>()).Union(HedgeFunds.Cast<Entity>());

                xml.WriteStartElement("players");
                foreach (var player in this.Players)
                {
                    xml.WriteStartElement("player");
                    xml.WriteAttributeString("uid", player.ID.ToString());
                    this.WriteBasicEntityProperties(xml, player);
                    xml.WriteEndElement(); // player
                }
                xml.WriteEndElement(); // players

                xml.WriteStartElement("firms");
                xml.WriteStartElement("companies");
                foreach (var comp in Companies)
                {
                    xml.WriteStartElement("company");
                    xml.WriteAttributeString("cid", comp.ID.ToString());
                    this.WriteBasicEntityProperties(xml, comp);
                    this.WriteBasicCompanyProperties(xml, comp);
                    xml.WriteEndElement();//company
                }
                xml.WriteEndElement(); // companies

                xml.WriteStartElement("hedgefunds");
                foreach (var fund in HedgeFunds)
                {
                    xml.WriteStartElement("hedgefund");
                    xml.WriteAttributeString("hid", fund.ID.ToString());
                    this.WriteBasicEntityProperties(xml, fund);
                    this.WriteBasicCompanyProperties(xml, fund);
                    xml.WriteStartElement("management");
                    {
                        xml.WriteStartElement("manager");
                        {
                            xml.WriteElementString("id", fund.Manager.ID.ToString());
                            xml.WriteElementString("fullname", fund.Manager.Name);
                        }
                        xml.WriteEndElement(); // manager
                        xml.WriteStartElement("compensation");
                        {
                            xml.WriteElementString("assetfee", fund.Compensation.AssetFee.ToF3Double());
                            xml.WriteElementString("performancefee", fund.Compensation.PerformanceFee.ToF3Double());
                        }
                        xml.WriteElementString("votepower", fund.ManagerVoteMultiplier.ToString());
                        xml.WriteEndElement(); // compensation
                    }
                    xml.WriteEndElement(); // management
                    xml.WriteEndElement();//hedgefund
                }
                xml.WriteEndElement(); // hedgefunds


                xml.WriteEndElement(); // firms

                xml.WriteEndElement(); // entities
                xml.WriteEndDocument();
            }
        }

        private void WriteBasicCompanyProperties(XmlWriter xml, Company comp)
        {
            xml.WriteStartElement("shareholderstructure");
            xml.WriteElementString("sharecount", comp.ShareCount.ToString());
            foreach (var shareholder in comp.ShareholderStructure)
            {
                xml.WriteStartElement("shareholder");
                xml.WriteAttributeString("id", shareholder.Key.ID.ToString());
                xml.WriteElementString("fullname", shareholder.Key.Name);
                xml.WriteElementString("shares", shareholder.Value.ToString());
                xml.WriteElementString("pctg", string.Format("{0:F3}%", comp.GetOwnershipPctg(shareholder.Key)));
                xml.WriteEndElement(); // shareholder
            }

            xml.WriteEndElement(); // shareholderstructure
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
            }
            xml.WriteEndElement(); // liabilities
        }
    }
}
