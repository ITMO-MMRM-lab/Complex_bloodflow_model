using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;

using System.Diagnostics;

namespace VNetStatCalc
{
  

    public class Node
    {
        private const int MAX_SELECTION_DEPTH = 100;

        public Node(int _id, /*Vector3 _position,*/ double _rad)
        {
            id = _id;
            //Position = _position;
            neighbours = new List<Node>();
            lumen_sq = Math.PI * _rad * _rad;
            lumen_sq_0 = lumen_sq;
            is_processed = false;
            member_of_protoknot = false;
            curvature = 0;

            rad = _rad;
            value = _rad;

            tailSelectionFlag = false;

            //mappedVertices = new List<UniqueVertexIdentifier>();

            selectedToShow = true;
           // structureLeafContainer = null;

            beta = 0.0;
        }

        public Node(int _id, /*Vector3 _position,*/ double _rad, double _beta)
            : this(_id,/* _position,*/ _rad)
        {
            beta = _beta;
        }

        public void addNeighbours(Node[] _neighbours)
        {
            foreach (var n in _neighbours)
                neighbours.Add(n);
            neighbours = neighbours.Distinct().ToList<Node>();
            neighbours.RemoveAll(x => x == this);
        }

        public void setId(int _id)
        { id = _id; }
        public int getId()
        { return id; }
        public List<Node> getNeighbours()
        {
            return neighbours;
        }
       // public Vector3 Position { get; set; }
        public virtual double getLumen_sq()
        { return lumen_sq; }
       // public virtual double getVelocity(Vector3 _dir_vector)
      //  { return velocity * Math.Sign(Vector3.Dot(dir_vector, _dir_vector)); }
        public double getLumen_sq0()
        { return lumen_sq_0; }
        public void setLumen_sq0(double val)
        { lumen_sq_0 = val; setLumen_sq(val); }


       public virtual void setLumen_sq(double val)
        { lumen_sq = val; rad = Math.Sqrt(lumen_sq / Math.PI); value = rad; }
      //  public virtual void setVelocity(double val, Vector3 _dir_vector)
      //  { velocity = val * Math.Sign(Vector3.Dot(dir_vector, _dir_vector)); }

        public double[] Related_data
        {
            set { related_data = value; }
            get { return related_data; }
        }

        public virtual double calcPressure()
        { return Node.diastolic_pressure; }

        public virtual double calcPressure(double lumen)
        { return Node.diastolic_pressure; }

        public virtual void nextTimeLayer()
        { }

        public virtual double calcLumen(double pressure)
        { return lumen_sq; }

        //public virtual double calcFlux(Vector3 _dir_vector)
        //{ return velocity * lumen_sq * Math.Sign(Vector3.Dot(dir_vector, _dir_vector)); }

        //public virtual double calcFlux(double _velocity, Vector3 _dir_vector)
        //{ return _velocity * lumen_sq * Math.Sign(Vector3.Dot(dir_vector, _dir_vector)); }

        public static double Wall_time
        {
            set { past_wall_time = wall_time; wall_time = value; }
            get { return wall_time; }
        }
        public static void Reset_wall_time(double start_time)
        {
            wall_time = start_time;
            past_wall_time = start_time;
        }

        public double Beta
        {
            get
            {
                return beta;
            }
            set
            {
                beta = value;
            }
        }


        public const double EPS = 0.001f;


        public static double diastolic_pressure = 10.9e+3f;
        public static double density = 1060;
        public static double viscosity = 3.5e-3f; //Pa*s        

        protected int id;
       // public Vector3 dir_vector;
        public bool is_processed;
        public bool member_of_protoknot;
        protected List<Node> neighbours;
        public double curvature;

        protected double velocity;
        protected double lumen_sq;
        public double lumen_sq_0;

        protected double[] related_data;

        protected static double past_wall_time;
        protected static double wall_time;

        private bool tailSelectionFlag;

    //    private List<UniqueVertexIdentifier> mappedVertices;

        private double rad;
        private double value;

  //      private TreeNode structureLeafContainer;
        private bool selectedToShow;

        private double beta;

        public bool SelectedToShow
        {
            get
            {
                return selectedToShow;
            }
            set
            {
                selectedToShow = value;
            }
        }

        //public TreeNode StructureLeafContainer
        //{
        //    get
        //    {
        //        return structureLeafContainer;
        //    }
        //    set
        //    {
        //        structureLeafContainer = value;
        //    }
        //}

        public double Value
        {
            get
            {
                return value;
            }
            set
            {
                this.value = value;
                Rad = value;
            }
        }

        public float ValueF
        {
            get
            {
                return (float)value;
            }
        }

        public double Rad
        {
            get
            {
                return rad;
            }
            set
            {
                rad = value;
                this.value = value;
                lumen_sq = Math.PI * value * value;
                setLumen_sq0(lumen_sq);
            }
        }
        
        public int GroupId
        {
            get
            {
                return (int)(Rad * 1000);
            }
            set
            {
                Rad = ((float)value / 1000);
            }
        }

        //public bool TailSelectionFlag
        //{
        //    get
        //    {
        //        return tailSelectionFlag;
        //    }
        //    set
        //    {
        //        tailSelectionFlag = value;
        //    }
        //}

        //public void selectTail(Node except, int depth)
        //{
        //    tailSelectionFlag = true;
        //    if (depth >= MAX_SELECTION_DEPTH)
        //        return;
        //    foreach (Node n in neighbours)
        //    {
        //        if (!n.Equals(except))
        //            n.selectTail(this, depth + 1);
        //    }
        //}

        //public void setRadiusToMean()
        //{
        //    if (neighbours.Count == 0)
        //        return;
        //    double radSum = 0.0;
        //    foreach (Node n in neighbours)
        //    {
        //        radSum += n.Rad;
        //    }
        //    Rad = radSum / neighbours.Count;
        //}


        //public static int CompareNodesByID(Node x, Node y)
        //{
        //    if (x == null)
        //    {
        //        if (y == null)
        //        {
        //            // If x is null and y is null, they're
        //            // equal. 
        //            return 0;
        //        }
        //        else
        //        {
        //            // If x is null and y is not null, y
        //            // is greater. 
        //            return -1;
        //        }
        //    }
        //    else
        //    {
        //        // If x is not null...
        //        //
        //        if (y == null)
        //        // ...and y is null, x is greater.
        //        {
        //            return 1;
        //        }
        //        else
        //        {
        //            // ...and y is not null, compare Y, then X, then Z.
        //            //
        //            return x.id.CompareTo(y.id);
        //        }
        //    }
        //}

        //public List<UniqueVertexIdentifier> MappedVertices
        //{
        //    get
        //    {
        //        return mappedVertices;
        //    }
        //}

        public override bool Equals(object obj)
        {
            if (obj.GetType() != typeof(Node))
                return false;
            return id.Equals(((Node)obj).getId());
        }

        public override int GetHashCode()
        {
            return id.GetHashCode();
        }
    };

    public class VascularNet
    {
        const int MIN_TERMINAL_BRANCHES_TO_SIMPLIFY = 3;

        //static public int NumOfNeigbours(Node node)
        //{
        //    return node.getNeighbours().Count;
        //}

  

        public static string LoadFromFile(VascularNet vnet, String file_path,
            float measurePos, double measureR, double measureC, double measureBeta, int max_preallocated_size)
        {
            bool load_curvature = false;
            bool load_beta = false;
            Stopwatch sw = new Stopwatch();
            sw.Start();

            vnet.vascular_system.Clear();
            
            Node[] preallocated_vascular_system;

            string protocol = "VascularNet loading protocol:\n";

            string[] readText = File.ReadAllLines(file_path);
            Regex regex = new Regex(@"^name:\s*(\w+)$", RegexOptions.IgnoreCase);

            Regex point_without_curvature = new Regex(@"^\s*(\d+)\s+X:(-*\d+.\d+)\s+Y:(-*\d+.\d+)\s+Z:(-*\d+.\d+)\s+R:(-*\d+.\d+)$", RegexOptions.IgnoreCase);
            Regex point_with_curvature = new Regex(@"^\s*(\d+)\s+X:(-*\d+.\d+)\s+Y:(-*\d+.\d+)\s+Z:(-*\d+.\d+)\s+R:(-*\d+.\d+)\s+C:(-*\d+.\d+)$", RegexOptions.IgnoreCase);
            Regex point_c_beta = new Regex(@"^\s*(\d+)\s+X:(-*\d+.\d+)\s+Y:(-*\d+.\d+)\s+Z:(-*\d+.\d+)\s+R:(-*\d+.\d+)\s+C:(-*\d+.\d+)\s+B:(-*\d+.\d+)$", RegexOptions.IgnoreCase);
            Regex neighbour_id = new Regex(@"(\d+)\s*", RegexOptions.IgnoreCase);
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
            vnet.name = name_match.Groups[1].Value;

            protocol += "The name was read: " + vnet.name;
            protocol += ";\n";
            protocol += "Time: " + sw.ElapsedMilliseconds.ToString() + "\n";
            Regex regex_1 = new Regex(@"^Coordinates:\s*$", RegexOptions.IgnoreCase);
            Regex regex_2 = new Regex(@"^Bonds:\s*$", RegexOptions.IgnoreCase);

            List<List<int>> bonds_index = new List<List<int>>();
            int node_count = 0;
            int bond_string_count = 0;
            Node newNode;

            int max_node_id = -1;

            while (true)
            {

                i++;

                if (regex_1.IsMatch(readText[i]))
                {
                    if (i + 1 >= readText.Length)
                        break;
                    load_curvature = readText[i + 1].Contains("C:");
                    load_beta = readText[i + 1].Contains("B:");
                    if (!load_beta)
                    {
                        if (load_curvature)
                        {
                            regex = point_with_curvature;
                        }
                        else
                        {
                            regex = point_without_curvature;
                        }
                    }
                    else
                    {
                        regex = point_c_beta;
                    }
                    while (true)
                    {
                        i++;
                        if (i >= readText.Length)
                            break;
                        Match node_match = regex.Match(readText[i]);

                        if (!load_beta)
                        {
                            if (load_curvature)
                            {
                                if (node_match.Groups.Count < 7)
                                    break;
                            }
                            else
                            {
                                if (node_match.Groups.Count < 6)
                                    break;
                            }
                        }
                        else
                        {
                            if (node_match.Groups.Count < 8)
                                break;
                        }

                        int id = int.Parse(node_match.Groups[1].Value);
                        if (id > max_node_id)
                            max_node_id = id;
                        //Vector3 position = new Vector3((float.Parse(node_match.Groups[2].Value) * measurePos),
                        //                               (float.Parse(node_match.Groups[3].Value) * measurePos),
                        //                               (float.Parse(node_match.Groups[4].Value) * measurePos));
                        double rad = double.Parse(node_match.Groups[5].Value); // .Replace('.', ',')
                        newNode = new Node(id,/* position, */ rad * measureR);
                        if (load_curvature)
                        {
                            newNode.curvature = double.Parse(node_match.Groups[6].Value) * measureC;
                        }
                        if (load_beta)
                        {
                            newNode.Beta = double.Parse(node_match.Groups[7].Value) * measureBeta;
                        }
                        vnet.vascular_system.Add(newNode);
                        node_count++;
                    }

                    protocol += node_count.ToString() + " nodes were read;\n";
                    protocol += "Time: " + sw.ElapsedMilliseconds.ToString() + "\n";
                }
                else
                    if (regex_2.IsMatch(readText[i]))
                    {
                        while (true)
                        {
                            i++;
                            if (i >= readText.Length)
                                break;

                            regex = neighbour_id;
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
                        protocol += bond_string_count.ToString() + " bonds strings were read;\n";
                        protocol += "Time: " + sw.ElapsedMilliseconds.ToString() + "\n";
                    }

                if (i >= readText.Length - 1)
                {
                    break;
                }
            }

            if (i >= readText.Length - 1)
            {

                if (0 == node_count)
                {
                    protocol += "0 nodes were read, skipping neighbours search.\n";
                    if (bond_string_count > 0)
                        protocol += "There are some bonds between 0 nodes. Did you forget to check 'Load curvature' checkbox?\n";
                    protocol += "Final time: " + sw.ElapsedMilliseconds.ToString() + "\n";
                    sw.Stop();
                    return protocol;
                }

                protocol += "Max NID = " + max_node_id.ToString() + " cmp to " + max_preallocated_size + ".\n";

                int current_id = -1;
                Node[] neighbours;
                int current_bonds_count = 0;
                int currrent_bond_id = -1;
                if (max_node_id < max_preallocated_size)
                {
                    // Use optimization (higer speed, higer memory usage).
                    preallocated_vascular_system = new Node[max_node_id + 1];
                    foreach (Node n in vnet.vascular_system)
                    {
                        preallocated_vascular_system[n.getId()] = n;
                    }

                    foreach (List<int> list in bonds_index)
                    {
                        current_id = list[0];
                        current_bonds_count = list.Count - 1;
                        neighbours = new Node[current_bonds_count];
                        for (currrent_bond_id = 0; currrent_bond_id < current_bonds_count; currrent_bond_id++)
                        {
                            neighbours[currrent_bond_id] = preallocated_vascular_system[list[currrent_bond_id+1]];
                        }
                        preallocated_vascular_system[current_id].addNeighbours(neighbours);
                    }
                    // Copying nodes back to vnet.vascular_system is unnecessary.
                }
                else
                {
                    // Use common method (low speed, low memory usage).
                    foreach (var str in bonds_index)
                    {
                        Node nd = vnet.vascular_system.Find(x => x.getId() == str[0]);
                        for (int s = 1; s < str.Count; s++)
                            nd.addNeighbours(new Node[] { vnet.vascular_system.Find(x => x.getId() == str[s]) });
                    }
                }

                // Fix inconsistency of nodes connections.
                foreach (Node node in vnet.vascular_system)
                {
                    foreach (Node n in node.getNeighbours())
                    {
                        if (!n.getNeighbours().Contains(node))
                        {
                            n.addNeighbours(new Node[] { node });
                        }
                    }
                }

                protocol += "Bonds data processed.\n";
                protocol += "Final time: " + sw.ElapsedMilliseconds.ToString() + "\n";
                sw.Stop();
            }

            return protocol;
        }

        //public static void Fix(VascularNet vnet, double resolution, double maxTermBranchLength, 
        //    bool enableSimplification, double maxTerminalRadius, bool saveTopology)
        //{
        //    vnet.setResolution(resolution, saveTopology);
        //    //vnet.reindexNodes();
        //    if (!saveTopology)
        //    {
        //        vnet.mergeCoupleNode(resolution);
        //    }
        //    vnet.fillBifurcationNodes();
        //    if (enableSimplification)
        //        vnet.simplifyTerminalNodes(maxTermBranchLength, maxTerminalRadius);
        //}

        public VascularNet(string _name)
        {
            name = new string(_name.ToCharArray());
            vascular_system = new List<Node>();
        }

  

        //private int getMinId()
        //{
        //    int minId = int.MaxValue;
        //    foreach (Node n in vascular_system)
        //    {
        //        if (n.getId() < minId)
        //            minId = n.getId();
        //    }
        //    return minId;
        //}

        //private int getMaxId()
        //{
        //    int maxId = int.MinValue;
        //    foreach (Node n in vascular_system)
        //    {
        //        if (n.getId() > maxId)
        //            maxId = n.getId();
        //    }
        //    return maxId;
        //}

        //private void shiftAllIds(int shift)
        //{
        //    int id;
        //    foreach (Node n in vascular_system)
        //    {
        //        id = n.getId();
        //        n.setId(id + shift);
        //    }
        //}

 

        //public bool UpdatePreallocatedVascularSystem(int max_preallocated_size)
        //{
        //    int max_node_id = getMaxId();
        //    if (max_node_id >= max_preallocated_size)
        //        return false;

        //    preallocated_vascular_system = new Node[max_node_id + 1];
        //    foreach (Node n in vascular_system)
        //    {
        //        preallocated_vascular_system[n.getId()] = n;
        //    }
        //    return true;
        //}

        //public int NodesCount
        //{
        //    get
        //    {
        //        return vascular_system.Count;
        //    }
        //}

        public List<Node> Nodes
        {
            get
            {
                return vascular_system;
            }
        }

        //public List<Node> NodesCopy
        //{
        //    get
        //    {
        //        return vascular_system.GetRange(0, vascular_system.Count);
        //    }
        //}

   

        //public Node[] PreallocatedVascularSystem
        //{
        //    get
        //    {
        //        return preallocated_vascular_system;
        //    }
        //    set
        //    {
        //        preallocated_vascular_system = value;
        //    }
        //}

        private string name;
        private List<Node> vascular_system;
      //  Node[] preallocated_vascular_system;

   
    };
}
