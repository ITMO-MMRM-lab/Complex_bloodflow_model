using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.IO;

namespace FullSearch
{
    class Node
    {
        public Node(int _id, int _part_id, double _weight, double _x, double _y, double _z)
        {
            id = _id;
            part_id = _part_id;
            gain = float.NegativeInfinity;
            adj_list = new Tuple<Node, double>();

            x = _x;
            y = _y;
            z = _z;
            weight = _weight;
        }

        public void addNeighbours(Node _node, double weight)
        {
            try
            {
                Tuple<Node, double> list_el = adj_list.Find(x => x.Item1 == _node);
                list_el = new Tuple<Node,double>(_node, list_el.Item2 + weight);
            }
            catch
            {
                adj_list.Add(new Tuple<Node, double>(_node, weight));
            }
        }

        public double x;
        public double y;
        public double z;

        public double weight;

        public int id;
        public int part_id;
        public List<Tuple<Node,double>> adj_list;

        public double gain;
    }

    class Partition
    {
        public Partition(int _id)
        {
            //      adj_list = new List<Node>();
            cnt_list = new List<Node>();
            adj_part_id = new List<int>();
            edge = new List<Node>();
            id = _id;
        }

        public void refreshEdge()
        {
            //adj_list.Clear();
            edge.Clear();
            adj_part_id.Clear();

            foreach (var n in cnt_list)
                foreach (var nn in n.adj_list)
                    if (nn.part_id != n.part_id)
                    {
                        // adj_list.Add(nn);
                        edge.Add(n);
                        adj_part_id.Add(nn.part_id);
                    }

            //adj_list = adj_list.Distinct().ToList();
            edge = edge.Distinct().ToList();
            adj_part_id = adj_part_id.Distinct().ToList();
            //  foreach (var n in edge)
            //       calcGain(n);

            //  edge.Sort(Node.CompareGain);
        }

        public List<Node> getEdge()
        {
            return edge;
        }

        /*    private void calcGain(Node _n)
            {
                double d_ext = 0; double d_int = 0;
                    adj_part_id = new List<int>();            
                foreach(var nn in _n.adj_list)
                {
                    if (nn.part_id != _n.part_id)
                    {
                        d_ext++;
                        adj_part_id.Add(nn.part_id);
                    }
                    else
                        d_int++;
                }

                adj_part_id = new List<int>(adj_part_id.Distinct());

                _n.gain=d_ext/Math.Sqrt(adj_part_id.Count) - d_int;
            }*/



        public int id;
        //  public List<Node>   adj_list;
        public List<Node> cnt_list;
        public List<int> adj_part_id;

        private List<Node> edge;
    }

    class Graph
    {
        public Graph(string _name)
        {
            name = new string(_name.ToCharArray());
            nodes = new List<Node>();
            partitions = new List<Partition>();
            partition_id2indx = new Dictionary<int, int>();
        }

        public static string LoadFromFile(Graph graph, String file_path)
        {
            graph.nodes.Clear();

            string protocol = "VescularNet loading protocol:\n";

            string[] readText = File.ReadAllLines(file_path);
            Regex regex = new Regex(@"^name:\s*(\w+)$", RegexOptions.IgnoreCase);
            int i = 0;
            while (!regex.IsMatch(readText[i]))
            {
                i++;
                if (i >= readText.Length)
                {
                    protocol += "Error: No correct name string was found!\n";
                    return protocol;
                }
            }

            Match name_match = regex.Match(readText[i]);
            graph.name = name_match.Groups[1].Value;

            protocol += "The name was read: " + graph.name;
            protocol += ";\n";

            Regex regex_1 = new Regex(@"^Coordinates:\s*$", RegexOptions.IgnoreCase);
            Regex regex_2 = new Regex(@"^Bonds:\s*$", RegexOptions.IgnoreCase);

            List<List<int>> bonds_index = new List<List<int>>();
            int node_count = 0;
            int bond_string_count = 0;

            graph.total_weight = 0;
            graph.av_p_size = 0;

            while (true)
            {

                i++;

                if (regex_1.IsMatch(readText[i]))
                    while (true)
                    {
                        i++;
                        if (i >= readText.Length)
                            break;


                        regex = new Regex(@"^\s*(\d+)\s+X:(-*\d+.\d+)\s+Y:(-*\d+.\d+)\s+Z:(-*\d+.\d+)\s+R:(-*\d+.\d+)\s+C:(\d+.\d+)$", RegexOptions.IgnoreCase);
                        regex = new Regex(@"^\s*(\d+)\s+X:(-*\d+.\d+)\s+Y:(-*\d+.\d+)\s+Z:(-*\d+.\d+)\s+R:(-*\d+.\d+)\s+C:(\d+.\d+)$", RegexOptions.IgnoreCase);
                        Match node_match = regex.Match(readText[i]);

                        if (node_match.Groups.Count < 6)
                        {
                            regex = new Regex(@"^\s*(\d+)\s+X:(-*\d+.\d+)\s+Y:(-*\d+.\d+)\s+Z:(-*\d+.\d+)\s+R:(-*\d+.\d+)$", RegexOptions.IgnoreCase);
                            node_match = regex.Match(readText[i]);
                            if (node_match.Groups.Count < 6)
                                break;
                        }


                        int id      =         int.Parse(node_match.Groups[1].Value);
                        double x = double.Parse(node_match.Groups[2].Value.Replace('.', ','));
                        double y = double.Parse(node_match.Groups[3].Value.Replace('.', ','));
                        double z = double.Parse(node_match.Groups[4].Value.Replace('.', ','));

                        int part_id = -1;// (int)double.Parse(node_match.Groups[5].Value.Replace('.', ','));

                        //if (part_id >= graph.partitions.Count)
                        //    graph.partitions.Add(new Partition(graph.partitions.Count+1));

                        graph.addNode(new Node(id, part_id, 1.0, x,y,z));
                        graph.total_weight += graph.nodes.Last().weight;
                        node_count++;
                    }
                else
                    if (regex_2.IsMatch(readText[i]))
                        while (true)
                        {
                            i++;
                            if (i >= readText.Length)
                                break;

                            regex = new Regex(@"(\d+)\s*", RegexOptions.IgnoreCase);
                            MatchCollection node_match = regex.Matches(readText[i]);
                            if (node_match.Count < 2)
                                break;


                            int id = int.Parse(node_match[0].Value);
                            bonds_index.Add(new List<int>());
                            bonds_index[bonds_index.Count - 1].Add(id);

                            for (int n = 1; n < node_match.Count; n++)
                                bonds_index[bonds_index.Count - 1].Add(int.Parse(node_match[n].Value));

                            bond_string_count++;
                        }

                if (i >= readText.Length - 1)
                {
                    protocol += node_count.ToString() + " nodes were read;\n";
                    protocol += bond_string_count.ToString() + " bonds strings were read;\n";

                    foreach (var str in bonds_index)
                    {
                        Node nd = graph.nodes.Find(x => x.id == str[0]);
                        for (int s = 1; s < str.Count; s++)
                            nd.addNeighbours(graph.nodes.Find(x => x.id == str[s]), 1.0);
                    }

                    break;
                }
            }     
            graph.av_p_size = graph.total_weight;
            return protocol;
        }

        public void writeToFile(String fname)
        {
            List<string> lines = new List<string>();
            List<Node> nodes_graph = new List<Node>(nodes);
            lines.Add("Name: ");
            lines[lines.Count - 1] += name;
            string section_title = "Coordinates:";
            lines.Add(section_title);
                        
            foreach (var n in nodes_graph)
            {   
                string pos = n.id.ToString() + " ";
                pos = pos + "X:" + n.x.ToString("F6") + " ";
                pos = pos + "Y:" + n.y.ToString("F6") + " ";
                pos = pos + "Z:" + n.z.ToString("F6") + " ";

                pos = pos + "R:" + n.part_id.ToString("F6") + " C:0.00";
                lines.Add(pos);
            }

            section_title = "\nBonds:";
            lines.Add(section_title);
            foreach (var n in nodes_graph)
            {
                string bonds = n.id.ToString() + " ";
                foreach (var nn in n.adj_list)
                    bonds += nn.id.ToString() + " ";

                lines.Add(bonds);
            }
            System.IO.File.WriteAllLines(fname, lines.ToArray());
        }

        public void prepareParitions(int num)
        {
            av_p_size = total_weight / num;
            partitions = new List<Partition>();
            for(int i=0; i<num; i++)
                partitions.Add(new Partition(i));            
        }


        public void doFullSearch()
        {
            int level = 0;
            for (int i = 0; i < nodes.Count - partitions.Count + (level - 1); i++)
            {
                recursiveSearch(i + 1, level);
                //estimation
            }
        }

        public double PartitionEstimation(double adj_ratio, double balance_ratio)
        {
            double adj_est = 0;

            foreach (var p in partitions)
                foreach (var n in p.cnt_list)
                {
                    double adj_count = 0;
                    foreach (var nn in n.adj_list)
                        if (nn.Item1.part_id != p.id)
                            adj_count += nn.Item2;
                    adj_est += adj_count / n.weight;
                }


            double[] p_size_vec = new double[partitions.Count];
            foreach (var p in partitions)
            {              
                foreach (var n in p.cnt_list)
                    p_size_vec[partition_id2indx[p.id]] += n.weight;
                p_size_vec[partition_id2indx[p.id]] -= av_p_size;
            }

            double balance_est = 0;
            for (int i = 0; i < partitions.Count; i++)
                balance_est += p_size_vec[i] * p_size_vec[i];
            balance_est = Math.Sqrt(balance_est);

            return balance_est*balance_ratio + adj_ratio*adj_est / 2; 
        }

        private void recursiveSearch(int s_indx, int level)
        {            
            partitions[level].cnt_list.Add(nodes[s_indx]);
            level = level + 1;
            if(level<partitions.Count)
                for (int i = s_indx + 1; i < nodes.Count - partitions.Count + (level-1); i++)
                    recursiveSearch(i, level);
        }
        

        public void addNode(Node _node)
        {   
            nodes.Add(_node);
        }
        
        private List<Node     >                  nodes;
        private List<Partition>             partitions;
        private Dictionary<int, int> partition_id2indx;

        private double av_p_size;
        private double total_weight;

        private string name;

        public       int      SEARCH_DEPTH;
        public       double BALANCE_CONSTR;
        public const int         MAX_ITER = 1000;
    }
}
