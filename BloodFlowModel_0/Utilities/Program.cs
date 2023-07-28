using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using BloodFlow;

namespace Utilities
{
    class IO_Module
    {
        public static void readMapFile(string map_filename, out Dictionary<int, int> map_dictionary)
        {
            string text = File.ReadAllText(map_filename);
            string[] lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            Regex regex = new Regex(@"^(\d+)\t+(\d+)$", RegexOptions.IgnoreCase);
            map_dictionary = new Dictionary<int, int>();
            for (int i = 0; i < lines.GetLength(0); i++)
            {
                Match clot_match = regex.Match(lines[i]);
                if (clot_match.Groups.Count != 3)
                    continue;

                int id_l = int.Parse(clot_match.Groups[1].Value);
                int id_h = int.Parse(clot_match.Groups[2].Value);
                map_dictionary.Add(id_h, id_l);
            }
        }

        public static List<int> readCenterFile(string filename)
        {
            string text = File.ReadAllText(filename);
            string[] lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            Regex regex = new Regex(@"^(\d+)$", RegexOptions.IgnoreCase);
            List<int> result = new List<int>();
            for (int i = 0; i < lines.GetLength(0); i++)
            {

                Match match = regex.Match(lines[i]);
                if (match.Groups.Count != 2)
                    continue;

                int id = int.Parse(match.Groups[1].Value);

                result.Add(id);
            }
            return result;
        }
    }

    class Program
    {
        static void Main(string[] args)
        {                      
            int MIN_ARTERY_LEN = 7;
            string top_text = File.ReadAllText(@"0_2_cutoff.top");
            VascularNet v_net_ref = new VascularNet();
            try
            {
                BloodFlow.IO_Module.LoadTopologyFromString(top_text, out v_net_ref);
            }
            catch { }

            getFloatValueDelegate getProximaDst;
            setFloatValueDelegate setProximaDst;
            v_net_ref.defineNodeDirVectros(out getProximaDst, out setProximaDst);
            v_net_ref.defineNet(getProximaDst, setProximaDst);


            top_text = File.ReadAllText("0_6_cutoff.top");
            VascularNet v_net_cmp = new VascularNet();
            try
            {
                BloodFlow.IO_Module.LoadTopologyFromString(top_text, out v_net_cmp);
            }
            catch { }

            v_net_cmp.defineNodeDirVectros(out getProximaDst, out setProximaDst);
            v_net_cmp.defineNet(getProximaDst, setProximaDst);

            Dictionary<int, int> map_dictionary;
            IO_Module.readMapFile("0_6_to_0_2_map.map", out map_dictionary);
            List<int> centers_hi = IO_Module.readCenterFile("centers_0_2.txt"); //hi-d  centers            
            List<int> centers_low = new List<int>();

            foreach (var c in centers_hi)
            {
                if (!map_dictionary.ContainsKey(c))
                    continue;

                int node_id = map_dictionary[c];
                VascularNode node = v_net_cmp.vascular_system.Find(x => x.id == node_id);
                Thread tr = v_net_cmp.node2thread[node];

                bool continue_sign = false;
                for (int i = 0; i < MIN_ARTERY_LEN / 2; i++)
                    if (tr.nodes[i].id == node_id || tr.nodes[tr.nodes.GetLength(0) - i - 1].id == node_id)
                    { continue_sign = true; break; }

                if (continue_sign)
                    continue;

                centers_low.Add(node_id);
            }

            string centers_low_string = "";
            foreach (var c in centers_low)
                centers_low_string = centers_low_string + c + "\n";

            File.WriteAllText(@"centers_0_6.txt", centers_low_string);
        }
    }
}
