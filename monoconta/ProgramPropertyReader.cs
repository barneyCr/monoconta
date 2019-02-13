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
                var neighbourhoodElementsCollection = board.Element("neighbourhoods").Elements("neighbourhood");
                List<Neighbourhood> neighbourhoods = new List<Neighbourhood>();
                Console.Write(".");
                foreach (var neighbourhoodElement in neighbourhoodElementsCollection)
                {
                    Neighbourhood neighbourhood = new Neighbourhood()
                    {
                        Name = neighbourhoodElement.Attribute("name").Value,
                        NID = int.Parse(neighbourhoodElement.Attribute("nid").Value),      

                    Spaces = int.Parse(neighbourhoodElement.Attribute("spaces").Value)
                    };
                    neighbourhoods.Add(neighbourhood);
                }
                var propertyElementCollection = board.Element("properties").Elements("property");
                List<Property> properties = new List<Property>();
                Console.Write(".");
                foreach (var propertyElement in propertyElementCollection)
                {
                    Property p = new Property
                    {
                        Name = propertyElement.Element("name").Value,
                        ID = int.Parse(propertyElement.Attribute("pid").Value),
                        Type = propertyElement.Element("type").Value,
                        ParentID = int.Parse(propertyElement.Element("parent").Value),
                        Rents = propertyElement.Element("rents").Elements().Select(el => double.Parse(el.Value)).ToArray(),
                        BuyPrice = double.Parse(propertyElement.Element("costs").Element("card").Value),
                        ConstructionBaseCost = double.Parse(propertyElement.Element("costs").Element("cons").Value)
                    };

                    neighbourhoods.First(nb => nb.NID == p.ParentID).Properties.Add(p);

                    properties.Add(p);
                }
                neighbourhoods_ = neighbourhoods;
                properties_ = properties;

                Console.Write(".  ");
                var constants = board.Element("constants");
                string elem(string str) => constants.Element(str).Value;

                Property.LevelRentMultiplier = double.Parse(elem("levelrentmultiplier")) / 100 + 1;
                Property.BalanceSheetValueMultiplier = double.Parse(elem("balancesheetmultiplier"));
                Property.LevelCostMultiplier = double.Parse(elem("levelcostmultiplier"));
                Property.LevelCostReductionPace = double.Parse(elem("levelcostreductionpace"));
                Property.LevelCostReduction = bool.Parse(elem("levelcostreduction"));

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
