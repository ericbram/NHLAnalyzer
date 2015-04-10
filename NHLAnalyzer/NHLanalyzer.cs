using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace NHLAnalyzer
{
    public class NHLanalyzer
    {

        private static void Main(string[] args)
        {
            // ***************  these are the customizable variables for this simple scrape file *************** 

            string inputfile = @"C:\Users\ebram\Desktop\stats.xml"; // this is the file from NHLScrape
            string OURTEAM = "PIT";                                 // this is the team you are analyzing

            // *************************************************************************************************

            // handle the results of both a 3 goal lead, and a 3 goal deficit with Lists
            List<AnalyzeInfo> resultsLead = new List<AnalyzeInfo>();
            List<AnalyzeInfo> resultsBehind = new List<AnalyzeInfo>();

            // deserialize the input file
            XmlSerializer ser = new XmlSerializer(typeof (FullTeamInfo));
            using (FileStream fs = new FileStream(inputfile, FileMode.Open))
            {
                FullTeamInfo fti = (FullTeamInfo) ser.Deserialize(fs);

                // now that we have the object, iterate over the games
                foreach (GameInfo gi in fti.Info)
                {
                    // goal differential (at every point in the game)
                    int differential = 0;
                    bool threeGoalLead = false;
                    bool threeGoalDef = false;
                    foreach (string s in gi.GoalList)
                    {
                        // if our team scores, we have a +1 differential, -1 for the other teams goal
                        // with this, we can tell the goal difference after each goal
                        if (s.Equals(OURTEAM))
                            differential++;
                        else
                            differential--;

                        // record if at any point in the game, we hit a +/- 3 goal differential
                        if (differential == 3)
                        {
                            // at this point we have a 3 goal lead
                            threeGoalLead = true;
                        }
                        else if (differential == -3)
                        {
                            // at this point we have a 3 goal deficit
                            threeGoalDef = true;
                        }
                    }

                    if (threeGoalLead)
                    {
                        // this was a +3 goal differential game
                        resultsLead.Add(new AnalyzeInfo
                                            {
                                                GInfo = gi,
                                                Differential = differential,
                                                Win = differential > 0
                                            });
                    }
                    else if (threeGoalDef)
                    {
                        // this was a -3 goal differential game
                        resultsBehind.Add(new AnalyzeInfo
                                              {
                                                  GInfo = gi,
                                                  Differential = differential,
                                                  Win = differential > 0
                                              });
                    }
                }
            }

            // this block is used to make a dictionary based on the season

            // we have a key that is the season, and the value is a list of games from that season
            Dictionary<string, List<AnalyzeInfo>> leadanalysis = new Dictionary<string, List<AnalyzeInfo>>();
            foreach (AnalyzeInfo ai in resultsLead)
            {
                if (!leadanalysis.ContainsKey(ai.GInfo.DataString))
                {
                    leadanalysis.Add(ai.GInfo.DataString, new List<AnalyzeInfo>());
                }
                leadanalysis[ai.GInfo.DataString].Add(ai);
            }

            // Same as above, this time for the three goal deficit
            Dictionary<string, List<AnalyzeInfo>> defanalysis = new Dictionary<string, List<AnalyzeInfo>>();
            foreach (AnalyzeInfo ai in resultsBehind)
            {
                if (!defanalysis.ContainsKey(ai.GInfo.DataString))
                {
                    defanalysis.Add(ai.GInfo.DataString, new List<AnalyzeInfo>());
                }
                defanalysis[ai.GInfo.DataString].Add(ai);
            }

            // finally, time to do the analysis (+3 goal section)
            // this dictionary is again indexed by season, with the values being another dictionary.  
            // the inner dictionary is indexed by goal differential, and the value is the count of that goal differential
            Dictionary<string, Dictionary<int, int>> leadResults = new Dictionary<string, Dictionary<int, int>>();

            // loop through each season
            foreach (string s in leadanalysis.Keys)
            {
                if (!leadResults.ContainsKey(s))
                {
                    // create a new dictionary of this season if it's not added yet
                    leadResults.Add(s, new Dictionary<int, int>());
                }
                // for each game that was a +3 goal lead THIS season
                foreach (AnalyzeInfo ai in leadanalysis[s])
                {
                    // if this goal differential has not been added yet, add it with a zero count
                    if (!leadResults[s].ContainsKey(ai.Differential))
                    {
                        leadResults[s].Add(ai.Differential, 0);
                    }

                    // increase the amount of games with this final goal differential
                    (leadResults[s])[ai.Differential]++;
                }

                // turn the list of final goal differentials into a list so we can sort it
                var list = leadResults[s].Keys.ToList();
                list.Sort();

                // print the results
                Console.WriteLine("Season:  " + s);
                foreach (var goalDiff in list)
                {
                    Console.WriteLine("Final Differential of " + goalDiff + ":  " + (leadResults[s])[goalDiff]);
                }
            }

            // space it out
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine();

            // do the analysis (-3 goal section)
            // this dictionary is again indexed by season, with the values being another dictionary.  
            // the inner dictionary is indexed by goal differential, and the value is the count of that goal differential
            Dictionary<string, Dictionary<int, int>> defResults = new Dictionary<string, Dictionary<int, int>>();

            // loop through each season
            foreach (string s in defanalysis.Keys)
            {
                if (!defResults.ContainsKey(s))
                {
                    // create a new dictionary of this season if it's not added yet
                    defResults.Add(s, new Dictionary<int, int>());
                }

                // for each game that was a -3 goal lead THIS season
                foreach (AnalyzeInfo ai in defanalysis[s])
                {
                    // if this goal differential has not been added yet, add it with a zero count
                    if (!defResults[s].ContainsKey(ai.Differential))
                    {
                        defResults[s].Add(ai.Differential, 0);
                    }

                    // increase the amount of games with this final goal differential
                    (defResults[s])[ai.Differential]++;
                }

                // turn the list of final goal differentials into a list so we can sort it
                var list = defResults[s].Keys.ToList();
                list.Sort();

                // print the results
                Console.WriteLine("Season:  " + s);
                foreach (var goalDiff in list)
                {
                    Console.WriteLine("Final Differential of " + goalDiff + ":  " + (defResults[s])[goalDiff]);
                }
            }

            Console.ReadKey();

        }
    }

    public struct AnalyzeInfo
    {
        private GameInfo gInfo;
        private bool win;
        private int differential;

        public GameInfo GInfo
        {
            get { return gInfo; }
            set { gInfo = value; }
        }

        public bool Win
        {
            get { return win; }
            set { win = value; }
        }

        public int Differential
        {
            get { return differential; }
            set { differential = value; }
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
