using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace VNetStatCalc
{
    public class AggrStat
    {
        public int aggrId;
        public int nodesCount;
        public int borderNodesConut;

        public AggrStat(int _aggrId)
        {
            aggrId = _aggrId;
            nodesCount = 0;
            borderNodesConut = 0;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is AggrStat))
                return false;
            AggrStat ao = (AggrStat)obj;
            return aggrId == ao.aggrId;
        }

        public override int GetHashCode()
        {
            return aggrId.GetHashCode();
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");

            if (args.Length < 2)
            {
                Console.WriteLine("stat.exe vnet.top stat.txt");
                return;
            }
            string pathVnet = args[0];
            string pathStat = args[1];

            VascularNet vnet = new VascularNet("Vnet");
            VascularNet.LoadFromFile(vnet, pathVnet, 1.0f, 0.001f, 1.0f, 1.0f, 1000000);

            List<AggrStat> aggrNodesStat = new List<AggrStat>();

            aggrNodesStat.Clear();
            foreach (var v in vnet.Nodes)
            {
                int gid = v.GroupId;
                // Assume gid = 0, 1, 2, ...
                if (gid >= aggrNodesStat.Count)
                {
                    for (int aid = aggrNodesStat.Count; aid <= gid; aid++)
                    {
                        aggrNodesStat.Add(new AggrStat(aid));
                    }
                }
                int listPos = gid;
                aggrNodesStat[listPos].nodesCount++;
                bool diffGidFound = false;
                foreach (var n in v.getNeighbours())
                {
                    if (n.GroupId != gid)
                    {
                        diffGidFound = true;
                        break;
                    }
                }
                if (diffGidFound)
                {
                    aggrNodesStat[listPos].borderNodesConut++;
                }
            }

            StringBuilder output = new StringBuilder();

            int totalNodes = 0;
            int totalBorderNodes = 0;

            for (int listPos = 0; listPos < aggrNodesStat.Count; listPos++)
            {
                totalNodes += aggrNodesStat[listPos].nodesCount;
                totalBorderNodes += aggrNodesStat[listPos].borderNodesConut;
            }

            float meanNodes = (float)totalNodes / aggrNodesStat.Count;
            float meanBorderNodes = (float)totalBorderNodes / aggrNodesStat.Count;

            float sumDiffSqNodes = 0.0f;
            float sumDiffSqBorderNodes = 0.0f;

            for (int listPos = 0; listPos < aggrNodesStat.Count; listPos++)
            {
                sumDiffSqNodes += (aggrNodesStat[listPos].nodesCount - meanNodes) *
                                  (aggrNodesStat[listPos].nodesCount - meanNodes);
                sumDiffSqBorderNodes += (aggrNodesStat[listPos].borderNodesConut - meanBorderNodes) *
                                        (aggrNodesStat[listPos].borderNodesConut - meanBorderNodes);
            }

            float Dnodes = (aggrNodesStat.Count > 1) ?
                sumDiffSqNodes / (aggrNodesStat.Count - 1)
                : 0.0f;
            float Dbordernodes = (aggrNodesStat.Count > 1) ?
                sumDiffSqBorderNodes / (aggrNodesStat.Count - 1)
                : 0.0f;

            float rmsqNodes = (float)Math.Sqrt(Dnodes);
            float rmsqBorderNodes = (float)Math.Sqrt(Dbordernodes);

            for (int listPos = 0; listPos < aggrNodesStat.Count; listPos++)
            {
                output.AppendFormat("{0}\t{1}\n", aggrNodesStat[listPos].nodesCount, aggrNodesStat[listPos].borderNodesConut);
            }
            output.Append("-------------\n");
            output.AppendFormat("{0}\t{1}\n", meanNodes.ToString("F8"), meanBorderNodes.ToString("F8"));
            output.AppendFormat("{0}\t{1}\n", rmsqNodes.ToString("F8"), rmsqBorderNodes.ToString("F8"));
            File.WriteAllText(pathStat, output.ToString());
        }
    }
}
