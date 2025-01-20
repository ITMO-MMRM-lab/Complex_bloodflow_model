using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;


namespace Refinement
{  

    class Node
    {
        public Node(int _id, int _part_id, double _x, double _y, double _z)
        { 
            id      = _id     ;
            part_id = _part_id;
            gain = float.NegativeInfinity;
            adj_list = new List<Node>();

            x = _x;
            y = _y;
            z = _z;
        }

        public void addNeighbours(Node _node)
        {
            if (!adj_list.Contains(_node))
            {
                adj_list.Add(_node);
                _node.addNeighbours(this);
            }

        }

        public double x;
        public double y;
        public double z;

        public int id;
        public int part_id;
        public List <Node> adj_list;

        public double gain;
    }

    struct NodePriorety
    {
        public NodePriorety(Node _n, double _pr)
        { n = _n; priorety = _pr; }

        public Node n;
        public double priorety;
        public static int Compare(NodePriorety x, NodePriorety y)
        {
            if (x.priorety > y.priorety) return 1;
            else if (x.priorety < y.priorety) return -1;
            else return 0;
        }        
    }

    class Hill
    {
        public Hill(int _part_id)
        {            
            cnt_list     = new List<Node>        ();
            hill_queue   = new List<NodePriorety>();
            hill_part_id = _part_id;
        }

        public void popQueue()
        {
            cnt_list.Add(hill_queue.Last().n);
            hill_queue.RemoveAt(hill_queue.Count - 1);
        }

        public int getPartId()
        {
            return hill_part_id;
        }

        
        public List<Node> cnt_list;       
        //public List<int> partial_gain;
        public List<NodePriorety> hill_queue;
        private int hill_part_id;
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
        public List<Node>   cnt_list;
        public List<int>    adj_part_id;

        private List<Node>  edge;
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
                        double x = double.Parse(node_match.Groups[2].Value);
                        double y = double.Parse(node_match.Groups[3].Value);
                        double z = double.Parse(node_match.Groups[4].Value);

                        int part_id = (int)Math.Round(double.Parse(node_match.Groups[5].Value));

                        //if (part_id >= graph.partitions.Count)
                        //    graph.partitions.Add(new Partition(graph.partitions.Count+1));

                        graph.addNode(new Node(id, part_id, x,y,z));
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
                            nd.addNeighbours(graph.nodes.Find(x => x.id == str[s]));
                    }

                    break;
                }
            }
     //       Hill.num_of_partitions = graph.partitions.Count;
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

        public void HillScan()
        {
            for (int I = 0; I < MAX_ITER; I++ )
            {            
                foreach (var p in partitions)
                    p.refreshEdge();

                List<NodePriorety> main_queue = new List<NodePriorety>();

                foreach (var p in partitions)
                    foreach (var p_n in p.getEdge())
                        main_queue.Add(new NodePriorety(p_n, calcPriorety(p_n)));

                main_queue = main_queue.Distinct().ToList();
                main_queue.Sort(NodePriorety.Compare);

                // основная очередь готова;

                // построение хиллов для каждой вершины в порядке приоретита;
                int move_count = 0;
                while (main_queue.Count != 0)
                {
                    Node curr_node = main_queue.Last().n;
                    main_queue.RemoveAt(main_queue.Count - 1);

                    Hill new_hill;
                    Tuple<int, double> gain_tuple = buildHill(SEARCH_DEPTH, curr_node, out new_hill);
                    if (gain_tuple != null)
                    {
                        if (MoveHillTo(new_hill, gain_tuple.Item1))
                            move_count++;
                    }
                }
                if (move_count == 0)
                    break;
            }
        }

        public void InitializeHill(Hill h, Node n)
        {
            h.cnt_list.Add(n);
            h.hill_queue = new List<NodePriorety>();
            
            foreach(var nn in n.adj_list)
                h.hill_queue.Add(new NodePriorety(nn, calcPriorety(nn)));

            h.hill_queue.Sort(NodePriorety.Compare);            
        }

        public bool MoveHillTo(Hill h, int p_id)
        {
            int max_size    = 0;                                
            foreach (var p in partitions)
            {
                int size = p.cnt_list.Count;
                if (p.id == h.getPartId())
                    size -= h.cnt_list.Count;
                if (p.id == p_id)
                    size += h.cnt_list.Count;

                if (max_size < size)                
                    max_size = size;
            }

            double balance_rate = partitions.Count * ((double) max_size / (double)nodes.Count);
            if (balance_rate - BALANCE_CONSTR > 1)
                return false;

            int source_id = h.getPartId();
            foreach (var n in h.cnt_list)
            {
                partitions[partition_id2indx[source_id]].cnt_list.Remove(n);
                partitions[partition_id2indx[p_id]].cnt_list.Add(n);
                n.part_id = p_id;
            }
            List <int> refreshed_ids = new List <int>();
            refreshed_ids.AddRange(partitions[partition_id2indx[source_id]].adj_part_id);
            refreshed_ids.AddRange(partitions[partition_id2indx[p_id]     ].adj_part_id);
            refreshed_ids = refreshed_ids.Distinct().ToList();
            foreach (var p in refreshed_ids)
                partitions[partition_id2indx[p]].refreshEdge();

            return true;
        }

        public Tuple<int,double> buildHill(int depth, Node curr_node, out Hill hill)
        {
            int level_count = 0;

            hill = new Hill(curr_node.part_id);
            hill.hill_queue.Add(new NodePriorety(curr_node, calcPriorety(curr_node)));
            

            while (hill.hill_queue.Count>0 && level_count <= depth)
            {
                hill.popQueue();
                List <double> gain = calcGain(hill);
                double self_cnt = gain[partition_id2indx[hill.getPartId()]];
                for (int i = 0; i < partitions.Count; i++)
                    gain[i] -= self_cnt;

                int curr_p_id = hill.getPartId();

                try
                {
                    int max_id = partition_id2indx.First(x => x.Key != curr_p_id).Key;
                    foreach (var pair in partition_id2indx)
                    {
                        if (gain[partition_id2indx[pair.Key]] > gain[partition_id2indx[max_id]])
                            if (pair.Key != hill.getPartId())
                                max_id = pair.Key;
                    }


                    if (gain[partition_id2indx[max_id]] > 0)
                        return new Tuple<int, double>(max_id, gain[partition_id2indx[max_id]]);
                }
                catch
                { };

                foreach(var n in hill.cnt_list)
                    foreach(var nn in n.adj_list)
                        if(!hill.cnt_list.Contains(nn)&&nn.part_id==hill.getPartId())
                            hill.hill_queue.Add(new NodePriorety(nn, calcPriorety(nn)));

                hill.hill_queue = hill.hill_queue.Distinct().ToList();
                hill.hill_queue.Sort(NodePriorety.Compare);

                level_count++;
            }

            return null;
        }

        public List <double> calcGain(Hill h)
        {
            List<double> gain = new List<double>();
            gain = Enumerable.Repeat(0.0, partitions.Count).ToList(); 

            List <Node> hill_adj_list = new List <Node> ();
            foreach (var n in h.cnt_list)                
                hill_adj_list.AddRange(n.adj_list);

            hill_adj_list = hill_adj_list.Distinct().ToList();
            hill_adj_list.RemoveAll(x => h.cnt_list.Contains(x));

            foreach (var n in hill_adj_list)
                gain[partition_id2indx[n.part_id]]++;

            double N_av = (double)nodes.Count / (double)partitions.Count;

            foreach (var n in hill_adj_list)
            {
                double disbalance_before = Math.Abs((partitions[partition_id2indx[h.getPartId()]].cnt_list.Count - N_av) / N_av) 
                    + Math.Abs((partitions[partition_id2indx[n.part_id]].cnt_list.Count - N_av) / N_av);
                double disbalance_after  = Math.Abs((partitions[partition_id2indx[h.getPartId()]].cnt_list.Count - h.cnt_list.Count - N_av) / N_av)
                    + Math.Abs((partitions[partition_id2indx[n.part_id]].cnt_list.Count + h.cnt_list.Count - N_av) / N_av);
                gain[partition_id2indx[n.part_id]] += disbalance_before - disbalance_after;
            }


            return gain;
        }

        public static double calcPriorety(Node _n)
        {
            List <int> adj_part_ids = new List<int>();
            int d_ext = 0;
            int d_int = 0;
            foreach (var nn in _n.adj_list)
            {
                if (!adj_part_ids.Contains(nn.part_id) && nn.part_id!=_n.part_id)
                    adj_part_ids.Add(nn.part_id);
                if (nn.part_id == _n.part_id)
                    d_int++;
                else
                    d_ext++;
            }

            return d_ext / Math.Sqrt(adj_part_ids.Count+1) - d_int;
        }

        //public static double calcPriorety(Hill _n)
        //{
        //
        //}

        public void addNode(Node _node)
        {
            try
            {
                Partition p = partitions.Find(x=>x.id==_node.part_id);
                p.cnt_list.Add(_node);
            }
            catch
            {
                partitions.Add(new Partition(_node.part_id));
                partition_id2indx.Add(_node.part_id, partitions.Count - 1);
                partitions.Last().cnt_list.Add(_node);                
            }
            nodes.Add(_node);
        }
        
        private List<Node     >                  nodes;
        private List<Partition>             partitions;
        private Dictionary<int, int> partition_id2indx;

        private string name;

        public       int      SEARCH_DEPTH;
        public       double BALANCE_CONSTR;
        public const int         MAX_ITER = 100;
    }
}
