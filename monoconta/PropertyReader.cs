using System;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Linq;
using System.IO;
using System.Diagnostics;

namespace monoconta
{
    partial class MainClass
    {
        static bool loadedPropertyXML_ = false;
        static bool LoadFromXML(out List<Neighbourhood> neighbourhoods_, out List<Property> properties_)
        {
            loadedPropertyXML_ = false;
            try
            {
                var document_ = XDocument.Load(File.OpenText(Environment.CurrentDirectory + "/ClassicBuildings.xml"));
                var board = document_.Root;
                var nbhds = board.Element("neighbourhoods").Elements("neighbourhood");
                List<Neighbourhood> neighbourhoods = new List<Neighbourhood>();
                Console.Write(".");
                foreach (var nbhd in nbhds)
                {
                    Neighbourhood neighbourhood = new Neighbourhood()
                    {
                        Name = nbhd.Attribute("name").Value,
                        NID = int.Parse(nbhd.Attribute("nid").Value),
                        Spaces = int.Parse(nbhd.Attribute("spaces").Value)
                    };
                    neighbourhoods.Add(neighbourhood);
                }
                var pptys = board.Element("properties").Elements("property");
                List<Property> properties = new List<Property>();
                Console.Write(".");
                foreach (var ppty in pptys)
                {
                    Property p = new Property
                    {
                        Name = ppty.Element("name").Value,
                        ID = int.Parse(ppty.Attribute("pid").Value),
                        Type = ppty.Element("type").Value,
                        ParentID = int.Parse(ppty.Element("parent").Value),
                        Rents = ppty.Element("rents").Elements().Select(el => double.Parse(el.Value)).ToArray(),
                        BuyPrice = double.Parse(ppty.Element("costs").Element("card").Value),
                        ConstructionBaseCost = double.Parse(ppty.Element("costs").Element("cons").Value)
                    };

                    neighbourhoods.First(nb => nb.NID == p.ParentID).Properties.Add(p);

                    properties.Add(p);
                }
                neighbourhoods_ = neighbourhoods;
                properties_ = properties;

                Console.Write(".  ");
                var constants = board.Element("constants");
                Property.LevelRentMultiplier = double.Parse(constants.Element("levelrentmultiplier").Value) / 100 + 1;
                Property.BalanceSheetValueMultiplier = double.Parse(constants.Element("balancesheetmultiplier").Value);
                Property.LevelCostMultiplier = double.Parse(constants.Element("levelcostmultiplier").Value);
                Property.LevelCostReductionPace = double.Parse(constants.Element("levelcostreductionpace").Value);
                Property.LevelCostReduction = bool.Parse(constants.Element("levelcostreduction").Value);

                loadedPropertyXML_ = true;
            }
            catch (Exception e) {
                Console.WriteLine("Could not read Real Estate Properties file\n{0}", e.Message);
                neighbourhoods_ = null;
                properties_ = null;
                loadedPropertyXML_ = false;
            }
            return loadedPropertyXML_;
        }
    }



}
