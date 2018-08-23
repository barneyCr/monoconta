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
            
            try
            {
                var document_ = XDocument.Load(File.OpenText(Environment.CurrentDirectory + "/ClassicBuildings.xml"));
                var board = document_.Root;
                var nbhds = board.Element("neighbourhoods").Elements("neighbourhood");
                List<Neighbourhood> neighbourhoods = new List<Neighbourhood>();
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
                foreach (var ppty in pptys)
                {
                    Property p = new Property();
                    p.Name = ppty.Element("name").Value;
                    p.ID = int.Parse(ppty.Attribute("pid").Value);
                    p.Type = ppty.Element("type").Value;
                    p.ParentID = int.Parse(ppty.Element("parent").Value);
                    p.Rents = ppty.Element("rents").Elements().Select(el => double.Parse(el.Value)).ToArray();
                    p.BuyPrice = double.Parse(ppty.Element("costs").Element("card").Value);
                    p.ConstructionBaseCost = double.Parse(ppty.Element("costs").Element("cons").Value);

                    neighbourhoods.First(nb => nb.NID == p.ParentID).Properties.Add(p);

                    properties.Add(p);
                }
                neighbourhoods_ = neighbourhoods;
                properties_ = properties;
                return true;
            }
            catch (Exception e) {
                Console.WriteLine("Could not read Real Estate Properties file\n{0}", e.Message);
                neighbourhoods_ = null;
                properties_ = null;
                return false;
            }
        }
    }


    class Neighbourhood {
        public string Name;
        public int NID, Spaces;
        public List<Property> Properties = new List<Property>();
    }
}
