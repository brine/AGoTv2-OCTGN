using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Net;
using System.Xml;
using System.IO;
using System.Data;
using System.Xml.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Reflection;
using System.Windows.Controls;
using System.Windows.Data;

namespace ThronesExtractor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private List<Card> cardList = new List<Card>();
        private List<Set> setList = new List<Set>();

        public MainWindow()
        {
            InitializeComponent();
            LoadPropertyTable();

            string cardsurl = new WebClient().DownloadString("http://www.thronesdb.com/api/public/cards/");
            JArray cardsjson = (JArray)JsonConvert.DeserializeObject(cardsurl);
            foreach (var jcard in cardsjson)
            {
                var card = new Card();
                card.Name = jcard.Value<string>("name");
                card.Pack = jcard.Value<string>("pack_code");
                card.Id = jcard.Value<string>("octgn_id") == null ? Guid.NewGuid() : Guid.Parse(jcard.Value<string>("octgn_id"));
                foreach (var prop in Properties)
                {
                    var value = jcard.Value<string>(prop.DbName);
                    if (value != null)
                    {
                        if (prop.Type == PropertyTypes.Bool)
                            value = (value == "True") ? "Yes" : "No";
                        else
                            value = MakeXMLSafe(value);
                        card.Properties.Add(prop, value);
                    }
                }
                card.Size = card.getProperty("Type") == "Plot" ? "HorizontalCards" : null;
                cardList.Add(card);
            }

            string url = new WebClient().DownloadString("http://www.thronesdb.com/api/public/packs");
            JArray json = (JArray)JsonConvert.DeserializeObject(url);
            foreach (var jset in json)
            {
                if (jset.Value<string>("available") == "") continue;
                var set = new Set();
                set.Id = GetSetGuid(jset.Value<string>("cycle_position"), jset.Value<string>("position"));
                set.Name = jset.Value<string>("name");
                set.Code = jset.Value<string>("code");
                set.Cards = new List<Card>(cardList.Where(x => x.Pack == set.Code));
                setList.Add(set);
            }
            foreach (var prop in Properties)
            {
                OutputGrid.Columns.Add(new DataGridTextColumn
                {
                    Binding = new Binding
                    {
                        Path = new PropertyPath("GetProperty", prop.OctgnName.ToArray()),
                        Mode = BindingMode.OneTime
                    },
                    Header = prop.OctgnName
                });
            }
            SetsPanel.ItemsSource = setList;
        }
                

        private void UpdateDataGrid(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            OutputGrid.ItemsSource = ((Set)e.NewValue).Cards;
        }


        public static string MakeXMLSafe(string makeSafe)
        {
            makeSafe = makeSafe.Replace("&", "&amp;");
            makeSafe = makeSafe.Replace("\n", "&#xd;&#xa;");
            makeSafe = makeSafe.Replace("[thenightswatch]", "<s value=\"nightswatch\">[The Night's Watch]</s>");
            makeSafe = makeSafe.Replace("[baratheon]", "<s value=\"baratheon\">[House Baratheon]</s>");
            makeSafe = makeSafe.Replace("[targaryen]", "<s value=\"targaryen\">[House Targaryen]</s>");
            makeSafe = makeSafe.Replace("[stark]", "<s value=\"stark\">[House Stark]</s>");
            makeSafe = makeSafe.Replace("[martell]", "<s value=\"martell\">[House Martell]</s>");
            makeSafe = makeSafe.Replace("[tyrell]", "<s value=\"tyrell\">[House Tyrell]</s>");
            makeSafe = makeSafe.Replace("[lannister]", "<s value=\"lannister\">[House Lannister]</s>");
            makeSafe = makeSafe.Replace("[greyjoy]", "<s value=\"greyjoy\">[House Greyjoy]</s>");
            makeSafe = makeSafe.Replace("[intrigue]", "<s value=\"intrigue\">[Intrigue]</s>");
            makeSafe = makeSafe.Replace("[military]", "<s value=\"military\">[Military]</s>");
            makeSafe = makeSafe.Replace("[power]", "<s value=\"power\">[Power]</s>");
            return (makeSafe);
        }

        private static List<Property> Properties = new List<Property>();

        private static void LoadPropertyTable()
        {
            foreach (var propdef in XDocument.Load("propertytable.xml").Document.Descendants("property"))
            {
                var prop = new Property()
                {
                    DbName = propdef.Attribute("db_name").Value,
                    OctgnName = propdef.Attribute("octgn_name").Value,
                    Type = (propdef.Attribute("type") == null) ? PropertyTypes.String : PropertyTypes.Bool
                };
                Properties.Add(prop);
            }
        }

        private static XDocument setGuidTable = null;

        private static XDocument GetSetGuidTable()
        {
            if (setGuidTable == null)
            {
                setGuidTable = XDocument.Load("setguids.xml");
            }
            return (setGuidTable);
        }

        private static string GetSetGuid(string cycle, string set)
        {
            XDocument doc = GetSetGuidTable();
            var ret = doc.Document.Descendants("cycle").First(x => x.Attribute("value").Value == cycle).Descendants("set").First(x => x.Attribute("name").Value == set).Attribute("value").Value;
            return (ret);
        }

        private void UpdateAllXml(object sender, RoutedEventArgs e)
        {
            foreach (var set in setList)
            {
                SaveXml(set);
            }
        }

        private void UpdateXml(object sender, RoutedEventArgs e)
        {
            if (SetsPanel.SelectedItem == null) return;
            SaveXml((Set)SetsPanel.SelectedItem);
        }

        private void SaveXml(Set set)
        {
            if (set == null) return;
            string dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string saveDir = Path.Combine(dir, "Saved", set.Id);
            string savePath = Path.Combine(saveDir, "set.xml");
            if (!Directory.Exists(saveDir))
            {
                Directory.CreateDirectory(saveDir);
            }
            if (File.Exists(savePath))
            {
                File.Delete(savePath);
            }

            var xml = new XmlDocument();
            xml.AppendChild(xml.CreateXmlDeclaration("1.0", "utf-8", "yes"));

            XmlNode root = xml.CreateElement("set");
            root.Attributes.Append(CreateAttribute(xml, "name", set.Name));
            root.Attributes.Append(CreateAttribute(xml, "id", set.Id));
            root.Attributes.Append(CreateAttribute(xml, "gameId", "30c200c9-6c98-49a4-a293-106c06295c05"));
            root.Attributes.Append(CreateAttribute(xml, "version", "1.0.0.0"));
            root.Attributes.Append(CreateAttribute(xml, "gameVersion", "1.0.0.0"));
            xml.AppendChild(root);

            XmlNode cardsNode = xml.CreateElement("cards");
            root.AppendChild(cardsNode);


            foreach (var c in set.Cards)
            {
                XmlNode cardNode = xml.CreateElement("card");
                cardNode.Attributes.Append(CreateAttribute(xml, "name", c.Name));
                cardNode.Attributes.Append(CreateAttribute(xml, "id", c.Id.ToString()));
                if (c.Size != null)
                {
                    cardNode.Attributes.Append(CreateAttribute(xml, "size", c.Size));
                }

                foreach (KeyValuePair<Property, string> kvi in c.Properties)
                {
                    XmlNode prop = xml.CreateElement("property");
                    prop.Attributes.Append(CreateAttribute(xml, "name", kvi.Key.OctgnName));
                    if (kvi.Key.OctgnName == "Text")
                    {
                        var propdoc = new XmlDocument();
                        propdoc.LoadXml("<root>" + kvi.Value + "</root>");
                        foreach (XmlNode node in propdoc.FirstChild.ChildNodes)
                        {
                            prop.AppendChild(xml.ImportNode(node, true));
                        }
                    }
                    else
                    {
                        prop.Attributes.Append(CreateAttribute(xml, "value", kvi.Value));
                    }
                    cardNode.AppendChild(prop);
                }
                cardsNode.AppendChild(cardNode);
            }
                                                
            xml.Save(savePath);
        }


        private XmlAttribute CreateAttribute(XmlDocument doc, string name, string value)
        {
            XmlAttribute ret = doc.CreateAttribute(name);
            ret.Value = value;
            return (ret);
        }
    }

    public enum PropertyTypes
    {
        Bool,
        String
    }

    public class Property
    {
        public string OctgnName { get; set; }
        public string DbName { get; set; }
        public PropertyTypes Type { get; set; }

        public Property()
        { }
    }

    public class Card
    {
        public string Name { get; set; }
        public Guid Id { get; set; }
        public string Size { get; set; }
        public string Pack { get; set; }
        public Dictionary<Property, string> Properties { get; set; }

        public Card()
        {
            Properties = new Dictionary<Property, string>();
        }

        public string getProperty(string propName)
        {
            return Properties.First(x => x.Key.OctgnName == propName).Value;
        }
    }

    public class Set
    {
        public string Name { get; set; }
        public string Id { get; set; }
        public string Code { get; set; }

        public List<Card> Cards { get; set; }

        public Set()
        {
            Cards = new List<Card>();
        }
    }
}
