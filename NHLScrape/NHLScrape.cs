using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using HtmlAgilityPack;

namespace NHLScrape
{
    public class NHLScrape
    {
        private static void Main(string[] args)
        {
            // *****  these are the customizable variables for this simple scrape file ***** 

            int yearstart = 2007;                                   // year to start recording.  For instance, "2007" starts with the 2007-2008 season
            int yearend = 2015;                                     // year to end recording.  For instance, "2015" ends with 2014-2015 season
            string LOCALTEAM = "Pittsburgh";                        // team name
            string LOCALTEAMABV = "PIT";                            // team abbreviation
            string statsFile = @"C:\Users\ebram\Desktop\stats.xml"; // where to save the xml file

            // ******************************************************************************




            FullTeamInfo games = new FullTeamInfo{Info = new List<GameInfo>()};

            for (int years = yearstart; years < yearend; years++)
            {
                HtmlWeb web = new HtmlWeb();
                HtmlDocument doc =
                    web.Load("http://www.nhl.com/ice/schedulebyseason.htm?season=" +
                             years.ToString() +
                             (years + 1).ToString() +
                             "&team=" + LOCALTEAMABV);

                // foreach season defined
                foreach (HtmlNode l in doc.DocumentNode.SelectNodes("//*[contains(concat(' ', @class, ' '), ' data ')]")
                    )
                {
                    // foreach game
                    foreach (HtmlNode link in l.SelectNodes(".//tbody/tr"))
                    {
                        // get the date using xpath
                        string date = "";
                        try
                        {
                            HtmlNode tmpnode =
                                link.SelectSingleNode(".//*[contains(concat(' ', @class, ' '), ' skedStartDateSite ')]");
                            if (tmpnode != null)
                            {
                                date = tmpnode.InnerHtml;
                            }
                            else
                            {
                                // might be a row with nothing
                                continue;
                            }
                        }
                        catch (Exception e)
                        {
                            // might be a row with nothing or malformed
                            continue;
                        }

                        // had a real date so it must be a real game

                        // get the 2 teams using xpath
                        HtmlNodeCollection teams =
                            link.SelectNodes(".//*[contains(concat(' ', @class, ' '), ' teamName ')]");
                        string team = "";
                        string team1 = teams[0].InnerText;
                        string team2 = teams[1].InnerText;

                        // since we are defining who "our" team is, only store the one that is the opponent
                        team = team1.Equals(LOCALTEAM) ? team2 : team1;

                        // most important part, get the URL for the actual game recap
                        HtmlNode html =
                            link.SelectSingleNode(".//*[contains(concat(' ', @class, ' '), ' skedLinks ')]/a");
                        string hreflink = html.GetAttributeValue("href", "");

                        // create most of the game info
                        GameInfo gi = new GameInfo
                                          {
                                              Date = DateTime.Parse(date),
                                              Opponent = team,
                                              DataString = years.ToString() + " - " + (years + 1).ToString(),
                                              GoalList = new List<string>()
                                          };

                        if (DateTime.Now < DateTime.Parse(date))
                        {
                            // don't count games that haven't happened yet
                            continue;
                        }

                        // now we need the actual game score
                        HtmlDocument gamedoc = web.Load(hreflink);

                        // sections lists all of the goals that occurred
                        HtmlNodeCollection sections =
                            gamedoc.DocumentNode.SelectNodes("//*[contains(concat(' ', @class, ' '), ' section ')]");

                        HtmlNode gamenode = null;


                        // sections may be null if no goals were scored
                        if (sections != null)
                        {
                            foreach (HtmlNode hn in sections)
                            {
                                // find the section of the page that is the scoring summary
                                if (hn.InnerHtml.Contains("scoring summary"))
                                {
                                    gamenode = hn;
                                }
                            }
                        }

                        // gamenode could be null if this is malformed or no goals were scored
                        if (gamenode != null)
                        {
                            try
                            {
                                // get a list of goals based on xpath
                                HtmlNodeCollection col =
                                    gamenode.SelectNodes(".//*[contains(concat(' ', @class, ' '), ' timeAndTeam ')]");
                                if (col != null && col.Count != 0)
                                {
                                    // if the list of goals is found and greater than zero, we have some goals to record!
                                    foreach (
                                        HtmlNode hn in
                                            gamenode.SelectNodes(
                                                ".//*[contains(concat(' ', @class, ' '), ' timeAndTeam ')]"))
                                    {
                                        // this is a goal, add it to the list
                                        // note that by default this will be stored in chronological order
                                        gi.GoalList.Add(hn.ChildNodes[2].InnerText);
                                    }
                                }
                            }
                            catch (Exception E)
                            {
                                
                            }

                        }

                        // we have added all of the goals to this info so we can conclude this game
                        games.Info.Add(gi);
                    }
                }
            }

            
            // now we need to save this file
            XmlSerializer ser = new XmlSerializer(typeof(FullTeamInfo));
            using (FileStream fs = new FileStream(statsFile, FileMode.Create))
            {
                ser.Serialize(fs, games);
            }
        }


        public struct FullTeamInfo
        {
            private List<GameInfo> info;
            [XmlArray("Info"), XmlArrayItem(typeof(GameInfo), ElementName = "GameInfo")]
            public List<GameInfo> Info
            {
                get { return info; }
                set { info = value; }
            }
        }

        [XmlRoot("GameInfo")]
        public struct GameInfo
        {
            private DateTime date;
            private string dataString;
            private String opponent;
            private List<string> goalList;

            public List<string> GoalList
            {
                get { return goalList; }
                set { goalList = value; }
            }

            public DateTime Date
            {
                get { return date; }
                set { date = value; }
            }

            public string DataString
            {
                get { return dataString; }
                set { dataString = value; }
            }

            public string Opponent
            {
                get { return opponent; }
                set { opponent = value; }
            }
        }
    }
}
