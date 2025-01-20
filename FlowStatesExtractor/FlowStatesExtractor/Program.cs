using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace FlowStatesExtractor
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 3)
            {
                Console.WriteLine("<filename> <node id> <delta tau> parameters expected.");
                return;
            }
            string dynFilename = args[0];
            string nodeId = args[1];
            float delta_tau = float.Parse(args[2]);
            string[] dynData = File.ReadAllLines(dynFilename);
            bool nodeFound = false;
            StringBuilder output = new StringBuilder();
            int iteration = 0;
            output.Append((0.0f).ToString("F4"));
            for (int i = 1; i < dynData.Length; i++)
            {
                string line = dynData[i];
                if (!nodeFound)
                {
                    if (line.Contains("WT:"))
                    {
                        Console.WriteLine("Specified node not found.");
                        return;
                    }
                    if (line.StartsWith(nodeId + "\t"))
                    {
                        nodeFound = true;
                    }
                }
                if (line.StartsWith("WT: "))
                {
                    float step = float.Parse(line.Substring(4));
                    output.Append((step * delta_tau).ToString("F4"));
                }
                if (line.StartsWith(nodeId + "\t"))
                {
                    string[] values = line.Split('\t');
                    output.Append("\t" + values[1] + "\n");
                    iteration++;
                }
            }
            File.WriteAllText("node" + nodeId + "_" + delta_tau.ToString("0.0E-0") + ".flux", output.ToString());
        }
    }
}
