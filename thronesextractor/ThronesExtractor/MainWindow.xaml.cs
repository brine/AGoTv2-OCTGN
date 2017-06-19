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

namespace ThronesExtractor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            cardList = new List<Card>();
            setList = new List<Set>();
            
            string cardsurl = new WebClient().DownloadString("http://www.thronesdb.com/api/public/cards/");
            JArray cardsjson = (JArray)JsonConvert.DeserializeObject(cardsurl);
            foreach (var jcard in cardsjson)
            {
                var card = new Card();
                card.Name = jcard.Value<string>("name");
                card.Pack = jcard.Value<string>("pack_code");
                card.Id = jcard.Value<string>("octgn_id") == null ? Guid.NewGuid() : Guid.Parse(jcard.Value<string>("octgn_id"));
                card.Properties = GetCardProperties(jcard);
                card.Size = card.Properties["Type"] == "Plot" ? "HorizontalCards" : null;
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
                set.Xml = GenerateXml(set);
                setList.Add(set);
            }

            SetsPanel.ItemsSource = setList;
        }

        private List<Card> cardList;
        private List<Set> setList;
       
        private Dictionary<string, string>GetCardProperties(JToken props)
        {
            var ret = new Dictionary<string, string>();

            ret["Type"] = props.Value<string>("type_name");
            ret["Faction"] = props.Value<string>("faction_name");
            ret["Text"] = MakeXMLSafe(props.Value<string>("text") ?? ""); //todo - sterilize text box
            ret["Traits"] = props.Value<string>("traits") ?? "";
            ret["CardNumber"] = props.Value<string>("position");
            ret["Illustrator"] = props.Value<string>("illustrator");
            switch (ret["Type"])
            {
                case "Character":
                    ret["Unique"] = (props.Value<string>("is_unique") == "True") ? "Yes" : "No";
                    ret["Loyal"] = (props.Value<string>("is_loyal") == "True") ? "Yes" : "No";
                    ret["Military"] = (props.Value<string>("is_military") == "True") ? "Yes" : "No";
                    ret["Intrigue"] = (props.Value<string>("is_intrigue") == "True") ? "Yes" : "No";
                    ret["Power"] = (props.Value<string>("is_power") == "True") ? "Yes" : "No";
                    ret["Cost"] = props.Value<string>("cost");
                    ret["Strength"] = props.Value<string>("strength");
                    break;
                case "Location":
                    ret["Unique"] = (props.Value<string>("is_unique") == "True") ? "Yes" : "No";
                    ret["Loyal"] = (props.Value<string>("is_loyal") == "True") ? "Yes" : "No";
                    ret["Cost"] = props.Value<string>("cost");
                    break;
                case "Event":
                    ret["Loyal"] = (props.Value<string>("is_loyal") == "True") ? "Yes" : "No";
                    ret["Cost"] = props.Value<string>("cost");
                    break;
                case "Attachment":
                    ret["Loyal"] = (props.Value<string>("is_loyal") == "True") ? "Yes" : "No";
                    ret["Unique"] = (props.Value<string>("is_unique") == "True") ? "Yes" : "No";
                    ret["Cost"] = props.Value<string>("cost");
                    break;
                case "Plot":
                    ret["Loyal"] = (props.Value<string>("is_loyal") == "True") ? "Yes" : "No";
                    ret["DeckLimit"] = props.Value<string>("deck_limit");
                    ret["PlotGoldIncome"] = props.Value<string>("income");
                    ret["PlotInitiative"] = props.Value<string>("initiative");
                    ret["PlotClaim"] = props.Value<string>("claim");
                    ret["PlotReserve"] = props.Value<string>("reserve");
                    break;
                case "Agenda":
                    break;
                case "Title":
                    break;
            }

            return ret;
        }
        
        private XmlDocument GenerateXml(Set set)
        {
            var ret = new XmlDocument();
            ret.AppendChild(ret.CreateXmlDeclaration("1.0", "utf-8", "yes"));
            XmlNode root = ret.CreateElement("set");
            
            root.Attributes.Append(CreateAttribute(ret, "name", set.Name));
            root.Attributes.Append(CreateAttribute(ret, "id", set.Id));
            root.Attributes.Append(CreateAttribute(ret, "gameId", "30c200c9-6c98-49a4-a293-106c06295c05"));
            root.Attributes.Append(CreateAttribute(ret, "version", "1.0.0.0"));
            root.Attributes.Append(CreateAttribute(ret, "gameVersion", "1.0.0.0"));

            ret.AppendChild(root);

            XmlNode cardsNode = ret.CreateElement("cards");
            root.AppendChild(cardsNode);

            foreach (var c in set.Cards)
            {
                XmlNode cardNode = ret.CreateElement("card");
                cardNode.Attributes.Append(CreateAttribute(ret, "name", c.Name));
                cardNode.Attributes.Append(CreateAttribute(ret, "id", c.Id.ToString()));
                if (c.Size != null)
                {
                    cardNode.Attributes.Append(CreateAttribute(ret, "size", c.Size));
                }

                foreach (KeyValuePair<string, string> kvi in c.Properties)
                {
                    XmlNode prop = ret.CreateElement("property");
                    prop.Attributes.Append(CreateAttribute(ret, "name", kvi.Key));
                    prop.Attributes.Append(CreateAttribute(ret, "value", kvi.Value));
                    cardNode.AppendChild(prop);
                }
                cardsNode.AppendChild(cardNode);
            }

            StringWriter sw = new StringWriter();
            XmlTextWriter tx = new XmlTextWriter(sw);
            ret.WriteTo(tx);

            string str = sw.ToString();// 

            Clipboard.SetText(str);
            return ret;
        }

        private XmlAttribute CreateAttribute(XmlDocument doc, string name, string value)
        {
            XmlAttribute ret = doc.CreateAttribute(name);
            ret.Value = value;
            return (ret);
        }
        
        public static string MakeXMLSafe(string makeSafe)
        {
            makeSafe = makeSafe.Replace("<b>", "<");
            makeSafe = makeSafe.Replace("</b>", ">");
            makeSafe = makeSafe.Replace("<i>", "<");
            makeSafe = makeSafe.Replace("</i>", ">");
            makeSafe = makeSafe.Replace("\n", "\r\n");
            makeSafe = makeSafe.Replace("[thenightswatch]", "[The Night's Watch]");
            makeSafe = makeSafe.Replace("[baratheon]", "[Baratheon]");
            makeSafe = makeSafe.Replace("[targaryen]", "[Targaryen]");
            makeSafe = makeSafe.Replace("[stark]", "[Stark]");
            makeSafe = makeSafe.Replace("[martell]", "[Martell]");
            makeSafe = makeSafe.Replace("[tyrell]", "[Tyrell]");
            makeSafe = makeSafe.Replace("[lannister]", "[Lannister]");
            makeSafe = makeSafe.Replace("[greyjoy]", "[Greyjoy]");
            makeSafe = makeSafe.Replace("[intrigue]", "[Intrigue]");
            makeSafe = makeSafe.Replace("[military]", "[Military]");
            makeSafe = makeSafe.Replace("[power]", "[Power]");
            return (makeSafe);
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
            set.Xml.Save(savePath);
        }
    }

    public class Card
    {
        public string Name { get; set; }
        public Guid Id { get; set; }
        public string Size { get; set; }
        public string Pack { get; set; }
        public Dictionary<string, string> Properties { get; set; }

        public Card()
        {
        }

    }
    public class Set
    {
        public string Name { get; set; }
        public string Id { get; set; }
        public string Code { get; set; }
        public XmlDocument Xml { get; set; }

        public List<Card> Cards { get; set; }

        public Set()
        {
            Cards = new List<Card>();
        }
    }
}
