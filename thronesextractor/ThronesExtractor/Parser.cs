using System;
using System.Collections.Generic;
using System.Net;
using System.IO;
using HtmlAgilityPack;
using System.Text;

namespace GathererExtracter
{
    class Parser
    {

        public static int progress = 0;
        public static string status = "Idle";

        public static string sideSeperator = "\r\n//\r\n";//"&#xD;&#xA;//&#xD;&#xA;";
        public static string lineSeperator = "\r\n";//"&#xD;&#xA;";
        public static string nonBreakingSideSeperator = " // ";

        public List<Dictionary<string, string>> GetCards(List<string> multiverseIDList)
        {
            Parser.status = "Working";
            Parser.progress = 0;
            List<Dictionary<string, string>> ret = new List<Dictionary<string, string>>();
            int i = 0;
            foreach (string id in multiverseIDList)
            {
                Parser.progress = i;
                ret.Add(GetNormalCardData(id, null));
                i++;
            }
            Parser.progress = 100;
            Parser.status = "Done";

            return (ret);
        }

        private string FindCardID(string name, string expansion)
        {
            string ret = string.Empty;
            string[] splitted = name.Split(' ');
            string searchArgument = string.Empty;
            if (splitted.Length > 1)
            {
                foreach (string s in splitted)
                {
                    searchArgument = searchArgument + "+[" + s + "]";
                }
            }
            else
            {
                searchArgument = "+[" + name + "]";
            }
            WebRequest request = null;
            if (expansion.Length < 1)
            {
                request = WebRequest.Create("http://gatherer.wizards.com/Pages/Search/Default.aspx?name=" + searchArgument + "");
            }
            else
            {
                request = WebRequest.Create("http://gatherer.wizards.com/Pages/Search/Default.aspx?name=" + searchArgument + "&set=[\"" + expansion + "\"]");
            }
            WebResponse response = request.GetResponse();
            if (response.ResponseUri.Query.Contains("multiverseid"))
            {
                ret = response.ResponseUri.Query.Substring(response.ResponseUri.Query.IndexOf("=") + 1);

            }
            response.Close();
            return (ret);
        }


        public List<Dictionary<string, string>> GetCards2(List<CardSetListing> cardList)
        {
            Parser.status = "Working";
            Parser.progress = 0;
            List<Dictionary<string, string>> ret = new List<Dictionary<string, string>>();
            int i = 0;
            foreach (CardSetListing card in cardList)
            {
                Parser.progress = i;
                ret.Add(GetSingleCard(card));
                i++;
            }
            Parser.progress = 100;
            Parser.status = "Done";

            return (ret);
        }

        public Dictionary<string, string> GetSingleCard(CardSetListing card)
        {
            Dictionary<string, string> ret = new Dictionary<string, string>();
            switch (card.Flag)
            {
                case ((int)Utility.CardFlags.normal):
                    WebRequest request = WebRequest.Create("http://gatherer.wizards.com/Pages/Card/Details.aspx?multiverseid=" + card.MultiverseID);
                    WebResponse response = request.GetResponse();
                    StreamReader reader = new StreamReader(response.GetResponseStream());

                    HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
                    doc.Load(reader);
                    HtmlNode node = doc.GetElementbyId("ctl00_ctl00_ctl00_MainContent_SubContent_SubContent_cardComponent0");
                    if (node != null)
                    {
                        ret = GetCardData(node, card);
                    }
                    else
                    {

                    }
                    reader.Close();
                    response.Close();

                    break;
                case ((int)Utility.CardFlags.split):
                    ret = GetSplitCardData(card);
                    break;
                case ((int)Utility.CardFlags.transform):
                    ret = TransformCardParser(card);
                    break;
                case ((int)Utility.CardFlags.flip):
                    ret = FlipCardParser(card);
                    break;
            }
            return (ret);
        }

        protected Dictionary<string, string> FlipCardParser(CardSetListing card)
        {
            Dictionary<string, string> ret = new Dictionary<string, string>();

            WebRequest request = WebRequest.Create("http://gatherer.wizards.com/Pages/Card/Details.aspx?multiverseid=" + card.MultiverseID);
            WebResponse response = request.GetResponse();
            StreamReader reader = new StreamReader(response.GetResponseStream());

            HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
            doc.Load(reader);
            HtmlNode node = doc.GetElementbyId("ctl00_ctl00_ctl00_MainContent_SubContent_SubContent_cardComponent0");
            Dictionary<string, string> dic1 = GetCardData(node, card);
            node = null;
            node = doc.GetElementbyId("ctl00_ctl00_ctl00_MainContent_SubContent_SubContent_cardComponent1");
            if ((node.InnerHtml.Trim().Length < 25))
            {
                node = null;
            }
            Dictionary<string, string> dic2 = null;
            if (node != null)
            {
                dic2 = GetCardData(node, card);
            }
            card.Properties = dic1;
            if (dic2 != null)
            {
                card.Alternate.Add("flip", dic2);
            }
            reader.Close();
            response.Close();

            return (ret);
        }

        protected Dictionary<string, string> TransformCardParser(CardSetListing card)
        {
            Dictionary<string, string> ret = new Dictionary<string, string>();

            WebRequest request = WebRequest.Create("http://gatherer.wizards.com/Pages/Card/Details.aspx?multiverseid=" + card.MultiverseID);
            WebResponse response = request.GetResponse();
            StreamReader reader = new StreamReader(response.GetResponseStream());

            HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
            doc.Load(reader);
            HtmlNode node = doc.GetElementbyId("ctl00_ctl00_ctl00_MainContent_SubContent_SubContent_cardComponent0");
            Dictionary<string, string> dic1 = GetCardData(node, card);
            node = null;
            node = doc.GetElementbyId("ctl00_ctl00_ctl00_MainContent_SubContent_SubContent_cardComponent1");
            Dictionary<string, string> dic2 = null;
            if (node != null)
            {
                dic2 = GetCardData(node, card);
            }
            reader.Close();
            response.Close();

            card.Properties = dic1;
            if (dic2 != null)
            {
                card.Alternate.Add("transform", dic2);
            }

            return (ret);
        }

        protected Dictionary<string, string> MergeDics(Dictionary<string, string> dic1, Dictionary<string, string> dic2)
        {
            Dictionary<string, string> ret = new Dictionary<string, string>();
            Dictionary<string, string>.KeyCollection keys1 = dic1.Keys;
            Dictionary<string, string>.KeyCollection keys2 = dic2.Keys;
            List<string> allKeys = new List<string>();
            allKeys.Add("Card");
            foreach (string key in keys1)
            {
                if (!allKeys.Contains(key))
                {
                    allKeys.Add(key);
                }
            }
            foreach (string key in keys2)
            {
                if (!allKeys.Contains(key))
                {
                    allKeys.Add(key);
                }
            }
            foreach (string key in allKeys)
            {
                string left = string.Empty;
                if (dic1.ContainsKey(key))
                {
                    left = dic1[key];
                }
                string right = string.Empty;
                if (dic2.ContainsKey(key))
                {
                    right = dic2[key];
                }
                string final = "";

                switch (key)
                {
                    case "Card":
                        final = left + Parser.nonBreakingSideSeperator + right;
                        break;
                    case "Rarity":
                    case "Artist":
                    case "MultiverseId":
                    case "Number":
                        final = left;
                        break;
                    default:
                        final = left + sideSeperator + right;
                        break;
                }
                //                string final = string.Format(FormatStrings.GetFormatString(key + "Split"), left, right);
                ret.Add(key, final);
            }
            return (ret);
        }


        protected Dictionary<string, string> GetCardData(HtmlNode node, CardSetListing card)
        {
            Dictionary<string, string> dict = new Dictionary<string, string>();

            Dictionary<string, HtmlNode> nodeDict = new Dictionary<string, HtmlNode>();
            string currentLabel = string.Empty;
            string currentValue = string.Empty;
            foreach (HtmlNode subNode in node.Descendants("div"))
            {
                if (subNode.Attributes.Contains("class") && subNode.Attributes["class"].Value.Equals("label"))
                {
                    if (!currentLabel.Equals(subNode.InnerHtml.Trim()))
                    {
                        currentLabel = subNode.InnerHtml.Trim();
                    }
                }
                if (subNode.Attributes.Contains("class") && subNode.Attributes["class"].Value.Equals("value"))
                {
                    if (!currentValue.Equals(subNode.InnerHtml.Trim()))
                    {
                        nodeDict.Add(currentLabel, subNode);
                        currentValue = subNode.InnerHtml.Trim();
                    }
                }
                if (!dict.ContainsValue(currentValue) && !dict.ContainsKey(currentLabel) && !currentValue.Equals(string.Empty) && !currentLabel.Equals("Community Rating:"))
                {
                    dict.Add(currentLabel, currentValue);
                }
            }
            Dictionary<string, string> cleandDict = CleanCardFormat(nodeDict, null);
            if (card != null)
            {
                cleandDict.Add("MultiverseId", card.MultiverseID);
            }
            card.Properties = cleandDict;
            return (cleandDict);
        }

        protected Dictionary<string, string> GetNormalCardData(string multiverseid, CardSetListing card)
        {
            return (GetNormalCardData(multiverseid, card, string.Empty));
        }

        protected Dictionary<string, string> GetNormalCardData(string multiverseid, CardSetListing card, string part)
        {
            Dictionary<string, string> dict = new Dictionary<string, string>();

            WebRequest request = null;
            if (part == string.Empty)
            {
                request = WebRequest.Create("http://gatherer.wizards.com/Pages/Card/Details.aspx?multiverseid=" + multiverseid);
            }
            else
            {
                request = WebRequest.Create("http://gatherer.wizards.com/Pages/Card/Details.aspx?multiverseid=" + multiverseid + "&part=" + part);
            }


            WebResponse response = request.GetResponse();
            StreamReader reader = new StreamReader(response.GetResponseStream());

            HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
            doc.Load(reader);
            Dictionary<string, HtmlNode> nodeDict = new Dictionary<string, HtmlNode>();
            string currentLabel = string.Empty;
            string currentValue = string.Empty;
            HtmlNode node = doc.GetElementbyId("ctl00_ctl00_ctl00_MainContent_SubContent_SubContent_cardComponent0");
            foreach (HtmlNode subNode in node.Descendants("div"))
            {
                if (subNode.Attributes.Contains("class") && subNode.Attributes["class"].Value.Equals("label"))
                {
                    if (!currentLabel.Equals(subNode.InnerHtml.Trim()))
                    {
                        currentLabel = subNode.InnerHtml.Trim();
                    }
                }
                if (subNode.Attributes.Contains("class") && subNode.Attributes["class"].Value.Equals("value"))
                {
                    if (!currentValue.Equals(subNode.InnerHtml.Trim()))
                    {
                        nodeDict.Add(currentLabel, subNode);
                        currentValue = subNode.InnerHtml.Trim();
                    }
                }
                if (!dict.ContainsValue(currentValue) && !dict.ContainsKey(currentLabel) && !currentValue.Equals(string.Empty) && !currentLabel.Equals("Community Rating:"))
                {
                    dict.Add(currentLabel, currentValue);
                }
            }
            Dictionary<string, string> cleandDict = CleanCardFormat(nodeDict, null);
            if (card != null)
            {
                cleandDict.Add("MultiverseId", card.MultiverseID);
            }
            else
            {
                cleandDict.Add("MultiverseId", request.RequestUri.Query.Substring(request.RequestUri.Query.IndexOf("=") + 1));
            }
            reader.Close();
            response.Close();

            return (cleandDict);
        }

        protected Dictionary<string, string> GetSplitCardData(CardSetListing card)
        {
            Dictionary<string, string> ret = new Dictionary<string, string>();
            Dictionary<string, string> dict1 = new Dictionary<string, string>();
            Dictionary<string, string> dict2 = new Dictionary<string, string>();
            //string multiverseid = "";
            string part1 = "";
            string part2 = "";

            string cardName = card.Name;
            if (cardName.Contains("//"))
            {
                //string[] splitted = cardName.Split(new string[]{"//", " "}, StringSplitOptions.RemoveEmptyEntries);
            }
            string[] splitted = cardName.Split(new string[] { "//", " " }, StringSplitOptions.RemoveEmptyEntries);
            string searchArgument = string.Empty;
            if (splitted.Length > 1)
            {
                foreach (string s in splitted)
                {
                    searchArgument = searchArgument + "+[" + s + "]";
                }
            }
            else
            {
                searchArgument = "+[" + card.Name + "]";
            }
            WebRequest request = null;
            request = WebRequest.Create("http://gatherer.wizards.com/Pages/Search/Default.aspx?name=" + searchArgument + "");
            WebResponse response = request.GetResponse();
            StreamReader reader = new StreamReader(response.GetResponseStream());
            HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
            doc.Load(reader);
            HtmlNode node = doc.GetElementbyId("ctl00_ctl00_ctl00_MainContent_SubContent_SubContent_ctl00_listRepeater_ctl00_cardImageLink");
            string href = node.Attributes["href"].Value;
            //multiverseid = splitID(href);
            part1 = splitPart(href);

            node = doc.GetElementbyId("ctl00_ctl00_ctl00_MainContent_SubContent_SubContent_ctl00_listRepeater_ctl01_cardImageLink");
            href = node.Attributes["href"].Value;
            part2 = splitPart(href);

            if (splitted[0].Contains(part1))
            {
                dict1 = GetNormalCardData(card.MultiverseID, card, part1);
                dict2 = GetNormalCardData(card.MultiverseID, card, part2);
                card.Alternate.Add("splitA", dict1);
                card.Alternate.Add("splitB", dict2);
            }
            else
            {
                dict1 = GetNormalCardData(card.MultiverseID, card, part2);
                dict2 = GetNormalCardData(card.MultiverseID, card, part1);
                card.Alternate.Add("splitA", dict1);
                card.Alternate.Add("splitB", dict2);
            }

            ret = MergeDics(dict1, dict2);
            card.Properties = ret;
            reader.Close();
            response.Close();

            return (ret);
        }

        private string splitID(string href)
        {
            string ret = "";
            href = href.Substring(href.IndexOf('?') + 1);
            string[] split1 = href.Split('&');
            foreach (string a in split1)
            {
                if (a.StartsWith("multiverseid"))
                {
                    ret = a.Split('=')[1];
                }
            }
            return (ret);
        }
        private string splitPart(string href)
        {
            string ret = "";
            href = href.Substring(href.IndexOf('?') + 1);
            string[] split1 = href.Split('&');
            foreach (string a in split1)
            {
                if (a.StartsWith("part"))
                {
                    ret = a.Split('=')[1];
                }
            }
            return (ret);
        }


        protected Dictionary<string, string> CleanCardFormat(Dictionary<string, HtmlNode> nodeDict, CardSetListing card)
        {
            Dictionary<string, string> dict = new Dictionary<string, string>();
            dict.Add("Card", nodeDict["Card Name:"].InnerText.Trim());// Utility.MakeXMLSafe(nodeDict["Card Name:"].InnerText.Trim()));
            if (nodeDict.ContainsKey("Mana Cost:"))
            {
                List<string> manaTypes = new List<string>();
                List<string> manaCosts = new List<string>();
                foreach (HtmlNode node in nodeDict["Mana Cost:"].Descendants("img"))
                {
                    manaCosts.Add(node.Attributes["alt"].Value);
                }
                dict.Add("Cost", Utility.MakeXMLSafe(Utility.ConvertAltToManaCost(Utility.ConvertListToCSV(manaCosts))));
                dict.Add("CMC", nodeDict["Converted Mana Cost:"].InnerText.Trim());
                dict.Add("Color", Utility.ConvertAltToCardColor(Utility.ConvertListToCSV(manaCosts)));
            }
            else
            {
                //dict.Add("Cost", "{0}");
                dict.Add("CMC", "0");
                dict.Add("Color", "Colorless");
            }
            if (nodeDict.ContainsKey("Color Indicator:"))
            {
                string colorIndicator = nodeDict["Color Indicator:"].InnerText.Trim();
                if (colorIndicator.Contains(","))
                {
                    dict["Color"] = Utility.ConvertAltToCardColor(colorIndicator.Replace(" ", ""));
                }
                else
                {
                    dict["Color"] = Utility.ConvertAltToCardColor(colorIndicator);
                }
            }
            if (nodeDict["Types:"].InnerText.Contains("—"))
            {
                dict.Add("Type", nodeDict["Types:"].InnerText.Trim().Split(new char[] { '—' }, StringSplitOptions.None)[0].Trim());
                dict.Add("Subtype", nodeDict["Types:"].InnerText.Trim().Split(new char[] { '—' }, StringSplitOptions.None)[1].Trim());
            }
            else
            {
                dict.Add("Type", nodeDict["Types:"].InnerText.Trim());
                //dict.Add("SubType", "");
            }
            string rules = string.Empty;
            if (nodeDict.ContainsKey("Card Text:"))
            {
                List<HtmlNode> editBoxes = new List<HtmlNode>();
                foreach (HtmlNode node in nodeDict["Card Text:"].Descendants("div"))
                {
                    editBoxes.Add(node);
                }

                if (editBoxes.Count > 0)
                {
                    foreach (HtmlNode node in editBoxes)
                    {
                        if (rules.Length > 1)
                        {
                            rules += Parser.lineSeperator;
                        }
                        foreach (HtmlNode subNode in node.ChildNodes)
                        {
                            switch (subNode.Name)
                            {
                                case "img":
                                    rules = string.Concat(rules, Utility.ConvertAltToManaCost(subNode.Attributes["Alt"].Value));
                                    break;
                                case "#text":
                                    rules = string.Concat(rules, subNode.InnerText);
                                    break;
                                case "i":
                                    if (subNode.HasChildNodes)
                                    {
                                        foreach (HtmlNode subSubNode in subNode.ChildNodes)
                                        {
                                            switch (subSubNode.Name)
                                            {
                                                case "img":
                                                    rules = string.Concat(rules, Utility.ConvertAltToManaCost(subSubNode.Attributes["Alt"].Value));
                                                    break;
                                                case "#text":
                                                    rules = string.Concat(rules, subSubNode.InnerText);
                                                    break;
                                            }
                                        }
                                    }
                                    break;
                            }
                        }
                    }
                }
            }
            dict.Add("Rarity", nodeDict["Rarity:"].LastChild.InnerText.Trim());
            dict.Add("Rules", rules.Trim());//Utility.MakeXMLSafe(rules.Trim()));
            if (nodeDict.ContainsKey("P/T:"))
            {
                dict.Add("Power", nodeDict["P/T:"].InnerText.Trim().Split(new char[] { '/' }, StringSplitOptions.None)[0]);
                dict.Add("Toughness", nodeDict["P/T:"].InnerText.Trim().Split(new char[] { '/' }, StringSplitOptions.None)[1]);
                dict.Add("PT Box", string.Format(FormatStrings.PTBox, dict["Power"], dict["Toughness"]));// nodeDict["P/T:"].InnerText.Trim());
            }
            if (nodeDict.ContainsKey("<b>P/T:</b>"))
            {
                dict.Add("Power", nodeDict["<b>P/T:</b>"].InnerText.Trim().Split(new char[] { '/' }, StringSplitOptions.None)[0].Trim());
                dict.Add("Toughness", nodeDict["<b>P/T:</b>"].InnerText.Trim().Split(new char[] { '/' }, StringSplitOptions.None)[1].Trim());
                dict.Add("PT Box", string.Format(FormatStrings.PTBox, dict["Power"], dict["Toughness"]).Trim());
            }
            if (nodeDict.ContainsKey("Loyalty:"))
            {
                dict.Add("PT Box", nodeDict["Loyalty:"].InnerText.Trim());
            }
            dict.Add("Artist", nodeDict["Artist:"].LastChild.InnerText.Trim());// Utility.MakeXMLSafe(nodeDict["Artist:"].LastChild.InnerText.Trim()));
            if (nodeDict.ContainsKey("Card Number:"))
            {
                dict.Add("Number", string.Format("{0:000}", double.Parse(nodeDict["Card Number:"].InnerText.Trim().Replace("a", "").Replace("b", ""))));
            }
            if (nodeDict.ContainsKey("Flavor Text:"))
            {
                dict.Add("Flavor", nodeDict["Flavor Text:"].InnerText.Trim());// Utility.MakeXMLSafe(nodeDict["Flavor Text:"].InnerText.Trim()));
            }
            if (nodeDict.ContainsKey("Watermark:"))
            {
                dict.Add("Faction", nodeDict["Watermark:"].InnerText.Trim());//Utility.MakeXMLSafe(nodeDict["Watermark:"].InnerText.Trim()));
            }
            return (dict);
        }
    }
}
