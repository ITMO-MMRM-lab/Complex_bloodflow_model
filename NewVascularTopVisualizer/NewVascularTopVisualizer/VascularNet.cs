using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Mathematics;
using System.IO;
using System.Text.RegularExpressions;

using System.Diagnostics;

namespace NewVascularTopVisualizer
{
    public struct DeepVascularThread
    {
        public DeepVascularThread(int _threadsPassed, VascularThread _notProcessedThread)
        {
            threadsPassed = _threadsPassed;
            notProcessedThread = _notProcessedThread;
        }

        public int threadsPassed;
        public VascularThread notProcessedThread;

        public override bool Equals(object obj)
        {
            if (!(obj is DeepVascularThread))
                return false;
            DeepVascularThread dvt = (DeepVascularThread)obj;
            return notProcessedThread.Equals(dvt.notProcessedThread);
        }

        public override int GetHashCode()
        {
            return notProcessedThread.GetHashCode();
        }
    }

    public struct VascularThread
    {
        public Node[] nodes;
        public double[] velocity;
        public double[] pressure;
        public double[] lumen_sq;
        public double[] flux;
        public Vector3[] dir_vector;

        public Node getNodeBehindTheEnd(double length, int _id)
        {
            Node lastNode = nodes[nodes.Length - 1];
            Node prelastNode = nodes[nodes.Length - 2];
            Vector3 dir = lastNode.Position - prelastNode.Position;
            Vector3 newPosition = lastNode.Position + dir / dir.Length() * (float)(length - getLength());
            Node newNode = new Node(_id, newPosition, lastNode.Rad);
            newNode.curvature = lastNode.curvature;
            return newNode;
        }

        public Node getInterNode(double length, bool linearA0notR, int _id, ref int idBefore, ref int idAfter)
        {
            double way = 0;
            int curr_id = -1;
            for (int i = 0; i < nodes.GetLength(0) - 1; i++)
            {
                way += Vector3.Distance(nodes[i].Position, nodes[i + 1].Position);
                if (way > length)
                {
                    way -= Vector3.Distance(nodes[i].Position, nodes[i + 1].Position);
                    curr_id = i;
                    break;
                }
            }

            // Check if new node needs to be placed in the first or last segment of a thread.
            Node borderNode = null;
            Node borderNodeNext = null;
            Node borderNodeNextNext = null;
            bool needSpecialBorderProcessing = false;
            bool borderNodesSet = false;
            bool shortThreadFound = false; // 2 nodes
            double correctBorderSq0 = 0.0;
            double correctBorderBeta = 0.0;
            if (nodes.Length <= 2)
            {
                // The code below will fail to process a thread in this case.
                //throw new ArgumentException("Threads of 2 nodes or less cannot be processed by getInterNode(). Node id: " + 
                //    nodes[0].getId());
                shortThreadFound = true;

            }
            if (!shortThreadFound)
            {
                if (curr_id == 0)
                {
                    // First segment
                    borderNode = nodes[curr_id];
                    borderNodeNext = nodes[curr_id + 1];
                    borderNodeNextNext = nodes[curr_id + 2];
                    borderNodesSet = true;
                }
                if (curr_id == nodes.Length - 2)
                {
                    // Last segment
                    borderNode = nodes[curr_id + 1];
                    borderNodeNext = nodes[curr_id];
                    borderNodeNextNext = nodes[curr_id - 1];
                    borderNodesSet = true;
                }
            }

            if (borderNodesSet)
            {
                if (borderNode.getNeighbours().Count == 1)
                {
                    // Terminal node.
                    // Normal processing will work for it.
                    // Nothing to do.
                }
                else
                {
                    // Non-terminal node = bifurcation.

                    // Assume that sq0 (and radius) for bifurcation node is obtained from the incoming thread.
                    if (borderNode.getLumen_sq0() <= borderNodeNext.getLumen_sq0())
                    {
                        // No changes in radius or 
                        // radius decreases from middle node to border one, so the border radius is correct.
                        //
                        // Normal processing will work for it.
                        // Nothing to do.
                    }
                    else
                    {
                        // borderNode.getLumen_sq0() > borderNodeNext.getLumen_sq0()
                        // Radius increases from middle node to border one.

                        // In this case, the border radius may be incorrect for this thread.
                        // So, we need to guess a correct radius using borderNodeNext and borderNodeNextNext nodes.

                        Vector3 v_bnn_bn = borderNodeNext.Position - borderNodeNextNext.Position;
                        Vector3 v_bn_b = borderNode.Position - borderNodeNext.Position;
                        double sq0_deriv = (borderNodeNext.getLumen_sq0() - borderNodeNextNext.getLumen_sq0()) / 
                            v_bnn_bn.Length();

                        /////////////////////////////////////////////////////////
                        // TODO sqo_deriv should be positive.
                        correctBorderSq0 = borderNodeNext.getLumen_sq0() + sq0_deriv * v_bn_b.Length();

                        // Beta needs to be corrected too.

                        double beta_deriv = (borderNodeNext.Beta - borderNodeNextNext.Beta) /
                            v_bnn_bn.Length();

                        correctBorderBeta = borderNodeNext.Beta + beta_deriv * v_bn_b.Length();
                        
                        needSpecialBorderProcessing = true;
                    }
                }
            }

            double inter_multiplier = 0;
            inter_multiplier = (length - way) / (Vector3.Distance(nodes[curr_id].Position, nodes[curr_id + 1].Position));

            //if (bordernodesset)
            //{
            //    if (nodes[curr_id].lumen_sq_0 < nodes[curr_id + 1].lumen_sq_0)
            //    {
            //        inter_multiplier = 0.0;
            //    }
            //    else
            //    {
            //        inter_multiplier = 1.0;
            //    }
            //    needspecialborderprocessing = false;
            //}

            Vector3 inter_vec = nodes[curr_id + 1].Position - nodes[curr_id].Position;
            Vector3 position = new Vector3(nodes[curr_id].Position.X, nodes[curr_id].Position.Y, nodes[curr_id].Position.Z) +
                inter_vec * (float)inter_multiplier;
            double sq0;
            double rad = 0.0;
            double beta;
            //if (needSpecialBorderProcessing)
            //{
            //    double curr_node_lsq0 = (nodes[curr_id].Equals(borderNode) ? correctBorderSq0 : nodes[curr_id].lumen_sq_0);
            //    double curr_p1_node_lsq0 = (nodes[curr_id+1].Equals(borderNode) ? correctBorderSq0 : nodes[curr_id+1].lumen_sq_0);
            //    sq0 = curr_node_lsq0 + inter_multiplier * (curr_p1_node_lsq0 - curr_node_lsq0);

            //    double curr_node_beta = (nodes[curr_id].Equals(borderNode) ? correctBorderBeta : nodes[curr_id].Beta);
            //    double curr_p1_node_beta = (nodes[curr_id + 1].Equals(borderNode) ? correctBorderBeta : nodes[curr_id + 1].Beta);
            //    beta = (curr_node_beta + inter_multiplier * (curr_p1_node_beta - curr_node_beta));
            //}
            //else
            //{
            //    sq0 = nodes[curr_id].lumen_sq_0 + inter_multiplier * (nodes[curr_id + 1].lumen_sq_0 - nodes[curr_id].lumen_sq_0);
            //    beta = (nodes[curr_id].Beta + inter_multiplier * (nodes[curr_id + 1].Beta - nodes[curr_id].Beta));
            //}

            if (linearA0notR)
            {
                // Linear A0 (area) change.
                sq0 = nodes[curr_id].lumen_sq_0 + inter_multiplier * (nodes[curr_id + 1].lumen_sq_0 - nodes[curr_id].lumen_sq_0);
                rad = Math.Sqrt(sq0 / Math.PI);
            }
            else
            {
                // Linear radius change.
                rad = Math.Sqrt(nodes[curr_id].lumen_sq_0 / Math.PI) + inter_multiplier * (Math.Sqrt(nodes[curr_id + 1].lumen_sq_0 / Math.PI) - Math.Sqrt(nodes[curr_id].lumen_sq_0 / Math.PI));
            }
            beta = (nodes[curr_id].Beta + inter_multiplier * (nodes[curr_id + 1].Beta - nodes[curr_id].Beta));

            //double rad = Math.Sqrt(nodes[curr_id].lumen_sq_0 / Math.PI) + inter_multiplier * (Math.Sqrt(nodes[curr_id + 1].lumen_sq_0 / Math.PI) - Math.Sqrt(nodes[curr_id].lumen_sq_0 / Math.PI));

            double curvature = (nodes[curr_id + 1].curvature + nodes[curr_id].curvature) / 2.0f;
            idBefore = curr_id;
            idAfter = curr_id + 1;
            Node inter_node = new Node(_id, position, rad);
            inter_node.curvature = curvature;
            inter_node.Beta = beta;
            return inter_node;
        }


        public Node getInterNode(double length, bool linearA0notR, int _id)
        {
            int idBefore = 0;
            int idAfter = 0;
            return getInterNode(length, linearA0notR, _id, ref idBefore, ref idAfter);
        }

        public double getPartialLength(int iFrom, int iTo)
        {
            double length = 0;
            for (int i = iFrom; i < iTo; i++)
            {
                length += Vector3.Distance(nodes[i].Position, nodes[i + 1].Position);
            }
            return length;
        }

        public double getLength()
        {
            return getPartialLength(0, nodes.Length - 1);
        }

        public void setProcessed(bool value)
        {
            foreach (var n in nodes)
                n.is_processed = value;
        }

        public void setSelectionFlag(bool value)
        {
            foreach (var n in nodes)
                n.TailSelectionFlag = value;
        }

        public double getMaxRadius()
        {
            double rad = 0.0;
            foreach (Node n in nodes)
            {
                if (n.Rad > rad)
                    rad = n.Rad;
            }
            return rad;
        }

        public double getMinRadius()
        {
            double rad = double.MaxValue;
            foreach (Node n in nodes)
            {
                if (n.Rad < rad)
                    rad = n.Rad;
            }
            return rad;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is VascularThread))
                return false;
            VascularThread th = (VascularThread)obj;
            if (nodes.Length != th.nodes.Length)
                return false;
            if (nodes.Length == 0)
                return true;
            if (nodes[0].Equals(th.nodes[0]))
            {
                for (int i = 0; i < nodes.Length; i++)
                    if (!nodes[i].Equals(th.nodes[i]))
                        return false;
                return true;
            }
            if (nodes[nodes.Length - 1].Equals(th.nodes[0]))
            {
                for (int i = 0; i < nodes.Length; i++)
                    if (!nodes[nodes.Length - 1 - i].Equals(th.nodes[i]))
                        return false;
                return true;
            }
            return false;
        }

        public override int GetHashCode()
        {
            int hash = 0;
            foreach (var n in nodes)
                hash ^= n.GetHashCode();
            return hash;
        }
    }

    public struct Knot
    {
        public int inlet_id;

        public Node[] nodes;
        public double[] flux;
        public double[] velocity;
        public double[] pressure;
        public double[] lumen_sq;
        public double outlet_lumen_sq;
        public double inlet_flux;

        public Vector3[] dir_vectors;
        public Node center_node;
    }

    //public delegate int NodeFilter(Node node);

    public class Node
    {
        private const int MAX_SELECTION_DEPTH = 100;

        public Node(int _id, Vector3 _position, double _rad)
        {
            id = _id;
            Position = _position;
            neighbours = new List<Node>();
            lumen_sq = Math.PI * _rad * _rad;
            lumen_sq_0 = lumen_sq;
            is_processed = false;
            member_of_protoknot = false;
            curvature = 0;

            rad = _rad;
            value = _rad;

            tailSelectionFlag = false;

            mappedVertices = new List<UniqueVertexIdentifier>();

            selectedToShow = true;
            structureLeafContainer = null;

            beta = 0.0;
        }

        public Node(int _id, Vector3 _position, double _rad, double _beta)
            : this(_id, _position, _rad)
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

        public void addNeighbour(Node _neighbour)
        {
            neighbours.Add(_neighbour);
        }

        public void setId(int _id)
        { id = _id; }
        public int getId()
        { return id; }
        public List<Node> getNeighbours()
        {
            return neighbours;
        }
        public Vector3 Position { get; set; }
        public virtual double getLumen_sq()
        { return lumen_sq; }
        public virtual double getVelocity(Vector3 _dir_vector)
        { return velocity * Math.Sign(Vector3.Dot(dir_vector, _dir_vector)); }
        public double getLumen_sq0()
        { return lumen_sq_0; }
        public void setLumen_sq0(double val)
        { lumen_sq_0 = val; setLumen_sq(val); }


        public virtual void setLumen_sq(double val)
        { lumen_sq = val; rad = Math.Sqrt(lumen_sq / Math.PI); value = rad; }
        public virtual void setVelocity(double val, Vector3 _dir_vector)
        { velocity = val * Math.Sign(Vector3.Dot(dir_vector, _dir_vector)); }

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

        public virtual double calcFlux(Vector3 _dir_vector)
        { return velocity * lumen_sq * Math.Sign(Vector3.Dot(dir_vector, _dir_vector)); }

        public virtual double calcFlux(double _velocity, Vector3 _dir_vector)
        { return _velocity * lumen_sq * Math.Sign(Vector3.Dot(dir_vector, _dir_vector)); }

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
        public Vector3 dir_vector;
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

        private List<UniqueVertexIdentifier> mappedVertices;

        private double rad;
        private double value;

        private TreeNode structureLeafContainer;
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

        public TreeNode StructureLeafContainer
        {
            get
            {
                return structureLeafContainer;
            }
            set
            {
                structureLeafContainer = value;
            }
        }

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

        public bool TailSelectionFlag
        {
            get
            {
                return tailSelectionFlag;
            }
            set
            {
                tailSelectionFlag = value;
            }
        }

        public void selectTail(Node except, int depth)
        {
            tailSelectionFlag = true;
            if (depth >= MAX_SELECTION_DEPTH)
                return;
            foreach (Node n in neighbours)
            {
                if (!n.Equals(except))
                    n.selectTail(this, depth + 1);
            }
        }

        public void setRadiusToMean()
        {
            if (neighbours.Count == 0)
                return;
            double radSum = 0.0;
            foreach (Node n in neighbours)
            {
                radSum += n.Rad;
            }
            Rad = radSum / neighbours.Count;
        }

        public static int CompareNodesByXYZ(Node x, Node y)
        {
            if (x == null)
            {
                if (y == null)
                {
                    // If x is null and y is null, they're
                    // equal. 
                    return 0;
                }
                else
                {
                    // If x is null and y is not null, y
                    // is greater. 
                    return -1;
                }
            }
            else
            {
                // If x is not null...
                //
                if (y == null)
                // ...and y is null, x is greater.
                {
                    return 1;
                }
                else
                {
                    // ...and y is not null, compare Y, then X, then Z.
                    //
                    int ret = x.Position.Y.CompareTo(y.Position.Y);
                    if (ret != 0)
                        return ret;
                    ret = x.Position.X.CompareTo(y.Position.X);
                    if (ret != 0)
                        return ret;
                    return x.Position.Z.CompareTo(y.Position.Z);
                }
            }
        }

        public static int CompareNodesByID(Node x, Node y)
        {
            if (x == null)
            {
                if (y == null)
                {
                    // If x is null and y is null, they're
                    // equal. 
                    return 0;
                }
                else
                {
                    // If x is null and y is not null, y
                    // is greater. 
                    return -1;
                }
            }
            else
            {
                // If x is not null...
                //
                if (y == null)
                // ...and y is null, x is greater.
                {
                    return 1;
                }
                else
                {
                    // ...and y is not null, compare Y, then X, then Z.
                    //
                    return x.id.CompareTo(y.id);
                }
            }
        }

        public List<UniqueVertexIdentifier> MappedVertices
        {
            get
            {
                return mappedVertices;
            }
        }

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

        static public int NumOfNeigbours(Node node)
        {
            return node.getNeighbours().Count;
        }

        public static string WriteToFile(VascularNet vnet, int offset, String file_path,
            float measurePos, double measureR, double measureC, double measureBeta, bool printBeta)
        {
            List<int> isolated = vnet.getIsolatedVerteices();
            if (isolated.Count != 0)
            {
                string ss = "Error. Isolated vertices found: ";
                foreach (int i in isolated)
                    ss += i.ToString() + " ";
                return ss;
            }

            System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");
            int id_count = offset;
            StringBuilder outText = new StringBuilder("Name: ");
            outText.Append(vnet.name + "\n");
            outText.Append("Coordinates:\n");
            foreach (var n in vnet.vascular_system)
            {
                n.setId(id_count);
                Vector3 pos = n.Position;
                // pos = pos + new Vector3(-10.30949, 47.39303, 1.148586);
                outText.Append(n.getId() + " X:" + (pos.X / measurePos).ToString("F8") + " Y:" + (pos.Y / measurePos).ToString("F8") +
                    " Z:" + (pos.Z / measurePos).ToString("F8") + " R:" + (Math.Sqrt(n.getLumen_sq0() / Math.PI) / measureR).ToString("F8") +
                    " C:" + (n.curvature / measureC).ToString("F4") + (printBeta ? " B:" + (n.Beta / measureBeta).ToString("F4") : "") + 
                    "\n");
                id_count++;
            }
            outText.Append("\nBonds:\n");
            foreach (var n in vnet.vascular_system)
            {
                outText.Append(n.getId() + " ");
                foreach (var nn in n.getNeighbours())
                    outText.Append(nn.getId() + " ");
                outText.Append("\n");
            }
            System.IO.File.WriteAllText(file_path, outText.ToString());
            return "";
        }

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
                        Vector3 position = new Vector3((float.Parse(node_match.Groups[2].Value) * measurePos),
                                                       (float.Parse(node_match.Groups[3].Value) * measurePos),
                                                       (float.Parse(node_match.Groups[4].Value) * measurePos));
                        double rad = double.Parse(node_match.Groups[5].Value); // .Replace('.', ',')
                        newNode = new Node(id, position, rad * measureR);
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
                            try
                            {
                                neighbours[currrent_bond_id] = preallocated_vascular_system[list[currrent_bond_id + 1]];
                            }
                            catch
                            {
                                int a = 0;
                                a++;
                            }
                        }
                        preallocated_vascular_system[current_id].addNeighbours(neighbours);
                    }
                    vnet.preallocated_vascular_system = preallocated_vascular_system;
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

        public void PrintMurrayStats(string filename)
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");

            List<Core3Node> core3 = buildCore3NodesList();
            List<string> data = new List<string>(core3.Count + 1);
            foreach (Core3Node c in core3)
            {
                int indexMax = 0;
                for (int i = 1; i < c.neighbours.Count; i++) {
                    if (c.neighbours[i].Rad > c.neighbours[indexMax].Rad) {
                        indexMax = i;
                    }
                }
                double RP3 = Math.Pow(c.neighbours[indexMax].Rad * 1000, 3);
                double RC3S = 0;
                for (int i = 0; i < c.neighbours.Count; i++) {
                    if (i == indexMax)
                        continue;
                    RC3S += Math.Pow(c.neighbours[i].Rad * 1000, 3);
                }
                data.Add(String.Format("{0:f4}\t{1:f4}", RP3, RC3S));
            }
            File.WriteAllLines(filename, data);
        }

        public static void Fix(VascularNet vnet, double resolution, 
            int maxTerminalBranchLength, double maxTerminalBranchRadius, double maxTerminalNodeRadius,
            bool enableSimplification, bool saveTopology, bool setBifurcations,
            bool linearA0notR, bool conserveLengths)
        {
            //vnet.setResolution(resolution, saveTopology);

            vnet.setResolutionSaveLengths(resolution, saveTopology, linearA0notR, conserveLengths);

            //vnet.reindexNodes();
            //if (!saveTopology)
            //{
            //    //vnet.mergeCoupleNode(resolution);
            //}
            //if (setBifurcations)
            //    vnet.fillBifurcationNodes();

            if (enableSimplification)
                vnet.simplifyTerminalNodesKill(maxTerminalBranchLength, maxTerminalBranchRadius, maxTerminalNodeRadius);

                //vnet.simplifyTerminalNodes(maxTermBranchLength, maxTerminalRadius);
        }

        public VascularNet(string _name)
        {
            name = new string(_name.ToCharArray());
            vascular_system = new List<Node>();
            preallocated_vascular_system = null;
            //reference_lengths = null;
        }

        public void selectBranches(Queue<DeepVascularThread> threadsToProcess, double softRadBound, double hardRadBound,
            int softThreadsBound, int hardThreadsBound, double softLengthBound, double hardLengthBound)
        {
            foreach (var n in vascular_system)
            {
                n.TailSelectionFlag = false;
                n.is_processed = false;
            }
            
            //foreach (var n in commonData.Vnet.Nodes)
            //{
            //    if (n.Rad < hardRadBound)
            //        n.TailSelectionFlag = true;
            //}
            DeepVascularThread currentDVT, newDVT;
            VascularThread thread;
            Node lastNode;
            do
            {
                currentDVT = threadsToProcess.Dequeue();
                thread = currentDVT.notProcessedThread;
                bool tmp = thread.nodes[0].TailSelectionFlag;
                bool flagSet = false;

                double maxRad = thread.getMaxRadius();
                double length = thread.getLength();

                if ((maxRad < hardRadBound) && (currentDVT.threadsPassed >= softThreadsBound) &&
                    (length < softLengthBound))
                {
                    thread.setSelectionFlag(true);
                    flagSet = true;
                }

                if ((maxRad < softRadBound) && (currentDVT.threadsPassed >= hardThreadsBound) &&
                    (length < softLengthBound))
                {
                    thread.setSelectionFlag(true);
                    flagSet = true;
                }

                if ((maxRad < softRadBound) && (currentDVT.threadsPassed >= softThreadsBound) &&
                    (length < hardLengthBound))
                {
                    thread.setSelectionFlag(true);
                    flagSet = true;
                }
                thread.nodes[0].TailSelectionFlag = tmp;

                if (!flagSet)
                {
                    thread = new VascularThread();
                    thread.nodes = new Node[currentDVT.notProcessedThread.nodes.Length-1];
                    for (int i = 1; i < currentDVT.notProcessedThread.nodes.Length; i++)
                    {
                        thread.nodes[i - 1] = currentDVT.notProcessedThread.nodes[i];
                    }
                    maxRad = thread.getMaxRadius();
                    length = thread.getLength();

                    if ((maxRad < hardRadBound) && (currentDVT.threadsPassed >= softThreadsBound) &&
                        (length < softLengthBound))
                    {
                        thread.setSelectionFlag(true);
                    }

                    if ((maxRad < softRadBound) && (currentDVT.threadsPassed >= hardThreadsBound) &&
                        (length < softLengthBound))
                    {
                        thread.setSelectionFlag(true);
                    }

                    if ((maxRad < softRadBound) && (currentDVT.threadsPassed >= softThreadsBound) &&
                        (length < hardLengthBound))
                    {
                        thread.setSelectionFlag(true);
                    }
                }

                thread.setProcessed(true);
                lastNode = thread.nodes.Last();
                foreach (var n in lastNode.getNeighbours())
                {
                    if (n.is_processed)
                        continue;
                    newDVT = new DeepVascularThread(currentDVT.threadsPassed + 1, getThread(lastNode, n));
                    if (threadsToProcess.Contains(newDVT))
                        continue;
                    threadsToProcess.Enqueue(newDVT);
                }
            }
            while (threadsToProcess.Count > 0);
        }

        public int simplifyTerminalNodesKill(int maxTerminalBranchLength, double maxTerminalBranchRadius, 
            double maxTerminalNodeRadius)
        {

            List<Node> nodesPossiblyTerminal = new List<Node>();
            //List<Node> nodesTerminal = new List<Node>();
            int nodesTerminalCount = 0;
            //List<List<Node>> nodesInlets = new List<List<Node>>();
            List<VascularThread> threadsToDelete = new List<VascularThread>();
            VascularThread thread;
            Node lastNode;
            List<Node> inlets =  new List<Node>();
            bool flagInletFound = false;
            bool flagTooLongThreadFound = false;
            bool flagTooBigRadius = false;
            int countOfTerminalBranches = 0;

            // Get all nodes that can turn out overloaded terminal.
            foreach (Node n in vascular_system)
            {
                if (n.getNeighbours().Count > 2)
                    nodesPossiblyTerminal.Add(n);
            }

            // Determine terminal nodes.
            foreach (Node n in nodesPossiblyTerminal)
            {
                countOfTerminalBranches = 0;
                flagInletFound = false;
                flagTooLongThreadFound = false;
                flagTooBigRadius = false;
                inlets.Clear();
                threadsToDelete.Clear();
                //if (Math.Abs(n.Rad - 0.00068206) < 1E-7)
                //{
                //    int a = 0;
                //    a++;
                //}
                foreach (Node nn in n.getNeighbours())
                {
                    thread = getThread(n, nn);
                    lastNode = thread.nodes.Last();
                    if (lastNode.getNeighbours().Count != 1)
                    {
                        // Not a termilal branch
                        flagInletFound = true;
                        inlets.Add(nn);
                        continue;
                    }
                    if (lastNode.Rad > maxTerminalNodeRadius)
                    {
                        // Inlet thread.
                        flagInletFound = true;
                        inlets.Add(nn);
                        continue;
                    }
                    // Terminal thread.
                    if (thread.nodes.Length > maxTerminalBranchLength + 1)
                    {
                        // Too many segments in a thread.
                        // Skip the node.
                        flagTooLongThreadFound = true;
                        break;
                    }

                    // Skip the bifurcation node for radius check.
                    for (int i = 1; i < thread.nodes.Length; i++)
                    {
                        if (thread.nodes[i].Rad > maxTerminalBranchRadius)
                        {
                            flagTooBigRadius = true;
                            break;
                        }
                    }
                    // All checks passed, approved terminal branch to kill.
                    threadsToDelete.Add(thread);
                    countOfTerminalBranches++;
                }
                if ((!flagTooLongThreadFound) && flagInletFound &&
                    (!flagTooLongThreadFound) && (!flagTooBigRadius) &&
                    (countOfTerminalBranches >= 2) && (inlets.Count == 1))
                {
                    //nodesTerminal.Add(n);
                    //nodesInlets.Add(inlets);
                    
                    // Delete threads in-place:
                    foreach (var th in threadsToDelete)
                    {
                        // Skip the bifurcation node.
                        for (int i = 1; i < th.nodes.Length; i++)
                        {
                            vascular_system.Remove(th.nodes[i]);
                        }
                    }
                    // Fix connections
                    n.getNeighbours().Clear();
                    n.getNeighbours().AddRange(inlets);

                    nodesTerminalCount++;
                }
            }

            //return nodesTerminal;

            //// Process terminal nodes.
            //for (int i = 0; i < nodesTerminal.Count; i++)
            //{
            //    terminal = nodesTerminal[i];
            //    inlets = nodesInlets[i];

            //    deleteThreadsLength1Except(terminal, inlets);
            //}
            return nodesTerminalCount;
        }

        // Old version.
        public /* List<Node> */ void simplifyTerminalNodes(double maxTermBranchLength, double maxTerminalRadius)
        {
            List<Node> nodesPossiblyTerminal = new List<Node>();
            List<Node> nodesTerminal = new List<Node>();
            List<List<Node>> nodesInlets = new List<List<Node>>();
            VascularThread thread;
            Node lastNode;
            Node terminal;
            List<Node> inlets = null;
            bool flagTerminal = true;
            bool flagInletFound = false;
            int countOfTerminalBranches = 0;

            // Get all nodes that can turn out overloaded terminal.
            foreach (Node n in vascular_system)
            {
                if (n.getNeighbours().Count > MIN_TERMINAL_BRANCHES_TO_SIMPLIFY)
                    nodesPossiblyTerminal.Add(n);
            }

            // Determine terminal nodes.
            foreach (Node n in nodesPossiblyTerminal)
            {
                countOfTerminalBranches = 0;
                flagTerminal = true;
                flagInletFound = false;
                inlets = new List<Node>();
                foreach (Node nn in n.getNeighbours())
                {
                    thread = getThread(n, nn);
                    lastNode = thread.nodes.Last();
                    if (lastNode.getNeighbours().Count != 1)
                    {
                        // Not a termilal branch
                        flagInletFound = true;
                        inlets.Add(nn);
                        continue;
                    }
                    if (lastNode.Rad > maxTerminalRadius)
                    {
                        // Inlet thread.
                        flagInletFound = true;
                        inlets.Add(nn);
                        continue;
                    }
                    if (thread.getLength() > maxTermBranchLength)
                    {
                        // Not satisfies length constraint => possibly inlet?
                        //flagTerminal = false;
                        //break;

                        flagInletFound = true;
                        inlets.Add(nn);
                        continue;
                    }
                    countOfTerminalBranches++;
                }
                if (flagTerminal && flagInletFound &&
                    (countOfTerminalBranches >= MIN_TERMINAL_BRANCHES_TO_SIMPLIFY))
                {
                    nodesTerminal.Add(n);
                    nodesInlets.Add(inlets);
                }
            }

            //return nodesTerminal;

            // Process terminal nodes.
            for (int i = 0; i < nodesTerminal.Count; i++)
            {
                terminal = nodesTerminal[i];
                inlets = nodesInlets[i];

                mergeSelectedTail(terminal, inlets);
            }
        }

        public List<int> getIsolatedVerteices()
        {
            List<int> isolated = new List<int>();
            foreach (Node n in vascular_system)
            {
                if (n.getNeighbours().Count == 0)
                {
                    isolated.Add(n.getId());
                }
            }
            return isolated;
        }

        public void mergeWithNet(VascularNet vnetToBeMerged)
        {
            int thisMaxId = getMaxId();
            int thatMinId = vnetToBeMerged.getMinId();
            if (thatMinId <= thisMaxId)
                vnetToBeMerged.shiftAllIds(thisMaxId - thatMinId + 1);
            mergeNetsForced(vnetToBeMerged);
        }

        private int getMinId()
        {
            int minId = int.MaxValue;
            foreach (Node n in vascular_system)
            {
                if (n.getId() < minId)
                    minId = n.getId();
            }
            return minId;
        }

        private int getMaxId()
        {
            int maxId = int.MinValue;
            foreach (Node n in vascular_system)
            {
                if (n.getId() > maxId)
                    maxId = n.getId();
            }
            return maxId;
        }

        private void shiftAllIds(int shift)
        {
            int id;
            foreach (Node n in vascular_system)
            {
                id = n.getId();
                n.setId(id + shift);
            }
        }

        private void mergeNetsForced(VascularNet vnetToBeMerged)
        {
            vascular_system.AddRange(vnetToBeMerged.vascular_system);
        }

        public void selectTail(Node from, Node except)
        {
            if (from.getNeighbours().Contains(except))
                from.selectTail(except, 0);
        }

        public void clearTailSelection()
        {
            foreach (Node n in vascular_system)
                n.TailSelectionFlag = false;
        }

        public void selectThread(VascularThread thread)
        {
            if (thread.nodes == null)
                return;
            if (thread.nodes.Length == 0)
                return;
            thread.nodes[0].TailSelectionFlag = true;
            if (thread.nodes.Length > 1)
            {
                selectTail(thread.nodes[1], thread.nodes[0]);
            }
        }

        public void deleteSelectedTail(Node from, Node except)
        {
            foreach (Node n in from.getNeighbours())
            {
                if (n.Equals(except))
                    continue;
                if (!n.TailSelectionFlag)
                    continue;
                deleteSelectedTail(n, from);
                vascular_system.Remove(n);
            }
            from.getNeighbours().Clear();
            from.addNeighbours(new Node[] { except });
        }

        //public void deleteThreadsLength1Except(Node from, List<Node> except)
        //{
        //    foreach (Node n in from.getNeighbours())
        //    {
        //        if (except.Contains(n))
        //            continue;
        //        vascular_system.Remove(n);
        //    }
        //    from.getNeighbours().Clear();
        //    from.addNeighbours(except.ToArray());
        //}

        //public void deleteThreadsAnyLengthExcept(Node from, List<Node> except)
        //{
        //    foreach (Node n in from.getNeighbours())
        //    {
        //        if (except.Contains(n))
        //            continue;
        //        deleteTerminalThreadExceptFirst(from, n);
        //    }
        //    from.getNeighbours().Clear();
        //    from.addNeighbours(except.ToArray());
        //}

        public void deleteSelected()
        {
            //int count = 0;
            //count = 
            vascular_system.RemoveAll(x => x.TailSelectionFlag);
            foreach (var n in vascular_system)
            {
                n.getNeighbours().RemoveAll(x => x.TailSelectionFlag);
            }
            clearTailSelection();
        }

        public void deleteThreadExceptStartNodes(VascularThread thread, int nodesToSave)
        {
            if (thread.nodes.Length <= nodesToSave)
                return;
            thread.nodes[nodesToSave - 1].getNeighbours().Remove(thread.nodes[nodesToSave]);
            thread.nodes[nodesToSave].getNeighbours().Remove(thread.nodes[nodesToSave - 1]);
            for (int i = nodesToSave; i < thread.nodes.Length; i++)
            {
                vascular_system.Remove(thread.nodes[i]);
            }
        }

        public VascularThread getThread(List<Node> nodes)
        {
            VascularThread thread = new VascularThread();
            thread.nodes = nodes.ToArray();
            return thread;
        }

        public VascularThread getThread(Node from, Node towards)
        {
            VascularThread thread = new VascularThread();
            List<Node> nodes = new List<Node>();
            Node prev_node = from;
            Node curr_node = towards;
            Node next_node = null;
            if (!from.getNeighbours().Contains(towards))
                return thread;
            nodes.Add(from);
            while (true)
            {
                nodes.Add(curr_node);
                if (curr_node.getNeighbours().Count != 2)
                    break;
                next_node = curr_node.getNeighbours().Find(x => x != prev_node);
                prev_node = curr_node;
                curr_node = next_node;
            }
            thread.nodes = nodes.ToArray();

            return thread;
        }


        // GetInterNode does not accept linearA0notR parameter.
        public void mergeThreads(VascularThread thread1, VascularThread thread2)
        {
            double length1 = thread1.getLength();
            double length2 = thread2.getLength();

            VascularThread masterThread, slaveThread;

            if (length1 < length2)
            {
                masterThread = thread1;
                slaveThread = thread2;
            }
            else
            {
                masterThread = thread2;
                slaveThread = thread1;
            }
            
            // Merge
            float distance = Vector3.Distance(masterThread.nodes[1].Position, masterThread.nodes[0].Position);
            if (masterThread.nodes[0].getNeighbours().Count == 1)
            {
                masterThread.nodes[0].Position = (masterThread.nodes[0].Position + slaveThread.nodes[0].Position) / 2;
                masterThread.nodes[0].Rad = Math.Pow(
                    (Math.Pow(masterThread.nodes[0].Rad, 4) + Math.Pow(slaveThread.nodes[0].Rad, 4)), 0.25);
            }
            for (int i = 1; i < (masterThread.nodes.Length - 1); i++)
            {
                Node slaveNode = slaveThread.getInterNode(distance, false, 0);
                distance += Vector3.Distance(masterThread.nodes[i + 1].Position, masterThread.nodes[i].Position);
                masterThread.nodes[i].Position = (masterThread.nodes[i].Position + slaveNode.Position) / 2;
                masterThread.nodes[i].Rad = Math.Pow((Math.Pow(masterThread.nodes[i].Rad, 4) + Math.Pow(slaveNode.Rad, 4)), 0.25);
            }
            if (masterThread.nodes[masterThread.nodes.Length - 1].getNeighbours().Count == 1)
            {
                Node slaveNode = slaveThread.getInterNode(distance, false, 0);
                masterThread.nodes[masterThread.nodes.Length - 1].Position =
                    (masterThread.nodes[masterThread.nodes.Length - 1].Position + slaveNode.Position) / 2;
                masterThread.nodes[masterThread.nodes.Length - 1].Rad =
                    Math.Pow((Math.Pow(masterThread.nodes[masterThread.nodes.Length - 1].Rad, 4) + Math.Pow(slaveNode.Rad, 4)), 0.25);
            }

            // Delete
            if (slaveThread.nodes[0].getNeighbours().Count == 1)
            {
                vascular_system.Remove(slaveThread.nodes[0]);
            }
            else
            {
                slaveThread.nodes[0].getNeighbours().Remove(slaveThread.nodes[1]);
            }
            for (int i = 1; i < (slaveThread.nodes.Length - 1); i++)
            {
                vascular_system.Remove(slaveThread.nodes[i]);
            }
            if (slaveThread.nodes[slaveThread.nodes.Length - 1].getNeighbours().Count == 1)
            {
                vascular_system.Remove(slaveThread.nodes[slaveThread.nodes.Length - 1]);
            }
            else
            {
                slaveThread.nodes[slaveThread.nodes.Length - 1].getNeighbours()
                    .Remove(slaveThread.nodes[slaveThread.nodes.Length - 2]);
            }
        }

        public void mergeSelectedTail(Node from, List<Node> except)
        {
            List<VascularThread> threadsToShorten = new List<VascularThread>();
            List<VascularThread> threads = new List<VascularThread>();
            VascularThread thread;
            int longestThreadIndex;
            List<Node> newThreadNodes = new List<Node>();
            double Rpow4sum = 0.0;
            double newR = 0.0;
            int countLeavesToSave = MIN_TERMINAL_BRANCHES_TO_SIMPLIFY - 1;
            Rpow4sum = 0.0;

            if ((from.getNeighbours().Count - except.Count) < MIN_TERMINAL_BRANCHES_TO_SIMPLIFY)
                return;
            
            // Get new radius
            foreach (Node n in from.getNeighbours())
            {
                if (except.Contains(n))
                    continue;
                Rpow4sum += Math.Pow(n.Rad, 4);
            }
            newR = Math.Pow(Rpow4sum / countLeavesToSave, 1.0 / 4);

            // Separate threads
            foreach (Node n in from.getNeighbours())
            {
                if (except.Contains(n))
                    continue;
                thread = getThread(from, n);
                threads.Add(thread);
            }
            for (int i = 0; i < countLeavesToSave; i++)
            {
                // Get the longest thread's index
                longestThreadIndex = 0;
                for (int j = 1; j < threads.Count; j++)
                {
                    if (threads[j].getLength() > threads[longestThreadIndex].getLength())
                        longestThreadIndex = j;
                }
                // Move the longest thread to the list-to-shorten
                threadsToShorten.Add(threads[longestThreadIndex]);
                threads.RemoveAt(longestThreadIndex);
            }

            // Delete unnesessary threads
            foreach (var threadToDelete in threads)
            {
                deleteThreadExceptStartNodes(threadToDelete, 1);
            }

            // Shorten threads
            foreach (var threadToShorten in threadsToShorten)
            {
                deleteThreadExceptStartNodes(threadToShorten, 2);
            }

            // Set radius
            foreach (var n in from.getNeighbours())
            {
                if (except.Contains(n))
                    continue;
                n.Rad = newR;
            }
        }

        public bool removeNode(Node node)
        {
            if (vascular_system.Remove(node))
            {
                foreach (Node n in node.getNeighbours())
                {
                    n.getNeighbours().Remove(node);
                }
                return true;
            }
            return false;
        }

        // Not implemented
        public void fixNodeCurvature(Node node)
        {

        }

        public bool hasNeighboursConsistency()
        {
            foreach (var n in vascular_system)
            {
                foreach (var nn in n.getNeighbours())
                    if (!nn.getNeighbours().Contains(n))
                        return false;
            }
            return true;
        }

        public List<Core3Node> buildCore3NodesList()
        {
            List<Core3Node> core3 = new List<Core3Node>();
            foreach (Node n in vascular_system)
            {
                if (n.getNeighbours().Count > 2)
                    core3.Add(new Core3Node(n));
            }

            return core3;

        }

        public void ResetCore3Flags(List<Core3Node> core3)
        {
            foreach (Core3Node c3n in core3)
            {
                for (int i = 0; i < c3n.neighbours.Count; i++)
                {
                    c3n.threadsProcessed[c3n.neighbours[i].getId()] = false;
                }
            }
        }

        public SortedList<ThreadDescriptor, double> calculateReferenceLengths(List<Core3Node> core3)
        {
            ResetCore3Flags(core3);
            //reference_lengths = new List<RefLengthDescription>;
            SortedList<ThreadDescriptor, double>  reference_lengths = new SortedList<ThreadDescriptor, double>(new ThreadDescriptorComparer());
            
            VascularThread thread;
            double length;
            Node lastNode;
            Node preLastNode;
            Core3Node core3Last;

            foreach (Core3Node c3n in core3)
            {
                for (int ni = 0; ni < c3n.neighbours.Count; ni++)
                {
                    if (c3n.threadsProcessed[c3n.neighbours[ni].getId()])
                        continue;
                    thread = getThread(c3n.bifurcationNode, c3n.neighbours[ni]);
                    length = thread.getLength();
                    lastNode = thread.nodes.Last();
                    preLastNode = thread.nodes[thread.nodes.Length - 2];
                    ThreadDescriptor threadDesc = new ThreadDescriptor(thread);
                        
                    reference_lengths.Add(threadDesc, length);

                    c3n.threadsProcessed[thread.nodes[1].getId()] = true;
                    if (lastNode.getNeighbours().Count > 2)
                    {
                        core3Last = core3.Find(x => (x.bifurcationNode.Equals(lastNode)));
                        core3Last.threadsProcessed[preLastNode.getId()] = true;
                    }
                }
            }
            return reference_lengths;
        }

        public bool willMergeLeadToCycles(Core3Node from, Core3Node to, SortedList<ThreadDescriptor, double> reference_lengths,
            double _dz)
        {
            for (int iLNN = 0; iLNN < from.neighbours.Count; iLNN++)
            {
                Node LNN = from.neighbours[iLNN];
                // Obtain old reference length for a thread that will be changed.
                VascularThread LNthread = getThread(from.bifurcationNode, LNN);
                ThreadDescriptor LNthreadDesc = new ThreadDescriptor(LNthread);
                double oldLNlength = 0.0;
                reference_lengths.TryGetValue(LNthreadDesc, out oldLNlength);

                if (LNthread.nodes[LNthread.nodes.Length - 1].Equals(to.bifurcationNode))
                {
                    // This thread is parallel to thread to be eliminated.

                    if (oldLNlength < 2 * _dz)
                    {
                        // Thread is too short, it will be eliminated too.
                    }
                    else
                    {
                        // This thread is long enough, it will produce a cycle -<==>
                        return true;
                    }
                }
                else
                {
                    // This thread is ordinary (not parallel).
                }
            } // End of check moving last node neighbours for.
            return false;
        }

        public void moveAllNeighbours(Core3Node from, Core3Node to, SortedList<ThreadDescriptor, double> reference_lengths,
            double _dz, double fromToLength)
        {
            for (int iLNN = 0; iLNN < from.neighbours.Count; iLNN++)
            {
                Node LNN = from.neighbours[iLNN];
                // Obtain old reference length for a thread that will be changed.
                VascularThread LNthread = getThread(from.bifurcationNode, LNN);
                ThreadDescriptor LNthreadDesc = new ThreadDescriptor(LNthread);
                double oldLNlength = 0.0;
                reference_lengths.TryGetValue(LNthreadDesc, out oldLNlength);

                if (LNthread.nodes[LNthread.nodes.Length - 1].Equals(to.bifurcationNode))
                {
                    // This thread is parallel to thread to be eliminated.

                    if (oldLNlength < 2 * _dz)
                    {
                        // Thread is too short, eliminate it too.
                        List<Node> LNthreadNodes = LNthread.nodes.ToList();
                        LNthreadNodes.RemoveAt(0);
                        LNthreadNodes.RemoveAt(LNthreadNodes.Count - 1);
                        vascular_system.RemoveAll(x => LNthreadNodes.Contains(x));

                        // This neighbour is not processed for sure.
                        // In other case, this method would have been called firstly for such a neighbour / thread.
                        to.RemoveNeighbour(LNthread.nodes[LNthread.nodes.Length - 2]);
                        reference_lengths.Remove(LNthreadDesc);
                    }
                    else
                    {
                        // 1-thread cycles generation is not supported.
                        throw new Exception("The operation of eliminating of thread " +
                            from.bifurcationNode.getId() + " -> " + to.bifurcationNode.getId() +
                            " leads to a 1-thread cycle -<==>. Cycles of this type are not supported.");

                        //// Thread is long enough, saving it as a cycle.

                        //reference_lengths.Remove(LNthreadDesc);
                        //// Change connections.
                        //if (LNthread.nodes.Length > 3)
                        //{
                        //    // Cycle is long enough, no need in intermediate node.
                        //    LNthread.nodes[1].getNeighbours().Remove(LNthread.nodes[0]);
                        //    LNthread.nodes[1].getNeighbours().Add(to.bifurcationNode);

                        //    to.AddNeighbour(LNthread.nodes[1]);
                        //}
                        //else
                        //{
                        //    // Need new intermediate node.

                        //    //////////////////////////
                        //    // TODO FIX

                        //    //////////////////////////////////////////
                        //    // Add a node between LNthread.nodes[1] and firstNode nodes.
                        //    Vector3 newPosition = (to.bifurcationNode.Position + LNthread.nodes[1].Position) / 2.0f;
                        //    double newS0 = (to.bifurcationNode.getLumen_sq0() + LNthread.nodes[1].getLumen_sq0()) / 2.0;

                        //    Node newNode = new Node(newId, newPosition, Math.Sqrt(newS0 / Math.PI));
                        //    newId++;

                        //    vascular_system.Add(newNode);

                        //    LNthread.nodes[1].getNeighbours().Remove(LNthread.nodes[0]);
                        //    LNthread.nodes[1].getNeighbours().Add(newNode);
                        //    to.AddNeighbour(newNode);
                        //    newNode.addNeighbour(LNthread.nodes[1]);
                        //    newNode.addNeighbour(to.bifurcationNode);

                        //}
                        //// Store new reference length.
                        ///////////////////////////////
                        //// TODO save cycles lengths.
                        //ThreadDescriptor newLNthreadIO = new ThreadDescriptor(LNthread);
                        //reference_lengths.Add(newLNthreadIO, oldLNlength + fromToLength);
                    }
                }
                else
                {
                    // This thread is ordinary (not parallel).

                    reference_lengths.Remove(LNthreadDesc);
                    // Change connections.
                    LNthread.nodes[1].getNeighbours().Remove(LNthread.nodes[0]);
                    LNthread.nodes[1].getNeighbours().Add(to.bifurcationNode);

                    to.AddNeighbour(LNthread.nodes[1]);
                    LNthread.nodes[0] = to.bifurcationNode;

                    // Store new reference length.
                    ThreadDescriptor newLNthreadDesc = new ThreadDescriptor(LNthread);
                    reference_lengths.Add(newLNthreadDesc, oldLNlength + fromToLength);
                }
            } // End of moving last node neighbours for.
        }
        
        public void changeTopology(List<Core3Node> core3, SortedList<ThreadDescriptor, double> reference_lengths, double _dz)
        {
            ResetCore3Flags(core3);

            int newId = getMaxId() + 1;
            
            // Change topology: eliminate too short threads.
            for (int iC3 = 0; iC3 < core3.Count; iC3++)
            {
                Core3Node firstCore3 = core3[iC3];
                for (int iC3N = 0; iC3N < core3[iC3].neighbours.Count; iC3N++)
                {
                    // Get thread & its length.
                    Node c3nb = firstCore3.neighbours[iC3N];
                    if (firstCore3.threadsProcessed[c3nb.getId()])
                        continue;
                    VascularThread thread = getThread(firstCore3.bifurcationNode, c3nb);
                    Node firstNode = thread.nodes[0];
                    Node lastNode = thread.nodes[thread.nodes.Length - 1];
                    double desiredLength = 0.0;
                    ThreadDescriptor threadDesc = new ThreadDescriptor(thread);
                    if (!reference_lengths.TryGetValue(threadDesc, out desiredLength))
                    {
                        // Failed to get reference length.
                        throw new Exception("Failed to get stored reference length for thread " +
                            threadDesc.id0 + " -> " + threadDesc.idNdec1);
                    }

                    if (desiredLength > 2 * _dz)
                    {
                        // Thread is long enough.

                        // No topology changes needed.
                        // Just setting a "processed" mark.
                        if (lastNode.getNeighbours().Count > 2)
                        {
                            Core3Node lastCore3 = core3.Find(x => (x.bifurcationNode.Equals(lastNode)));
                            lastCore3.threadsProcessed[thread.nodes[thread.nodes.Length - 2].getId()] = true;
                        }
                        firstCore3.threadsProcessed[thread.nodes[1].getId()] = true;
                    }
                    else
                    {
                        // Too short thread.
                        if (lastNode.getNeighbours().Count == 1)
                        {
                            // Terminal branch.

                            // No topology changes needed.
                            // Just setting a "processed" mark.
                            firstCore3.threadsProcessed[thread.nodes[1].getId()] = true;
                        }
                        else
                        {
                            // Last node is a bifurcation node.

                            // Need to eliminate too short thread.

                            Core3Node lastCore3 = core3.Find(x => (x.bifurcationNode.Equals(lastNode)));
                            // lastCore3 is not visited (that is, its index in core3 list is greater than iC3),
                            // so we can remove it or change its neighbours without reqesting a new iteration of core3 loop.

                            // Check if elimination of this thread will born a cycle -<==>
                            
                            // Compare radiuses. 
                            if (lastCore3.bifurcationNode.Rad <= firstCore3.bifurcationNode.Rad)
                            {
                                if (willMergeLeadToCycles(lastCore3, firstCore3, reference_lengths, _dz))
                                {
                                    // A cycle will appear, aborting the elimination of thread.
                                    continue;
                                }
                            }
                            else
                            {
                                if (willMergeLeadToCycles(firstCore3, lastCore3, reference_lengths, _dz))
                                {
                                    // A cycle will appear, aborting the elimination of thread.
                                    continue;
                                }
                            }

                            // Detach the thread that will be eliminated from firstNode & lastNode.
                            firstCore3.RemoveNeighbour(thread.nodes[1]);

                            // A neighbour (current item of neighbours list) was removed, so the counter should be decreased.
                            iC3N--;

                            lastCore3.RemoveNeighbour(thread.nodes[thread.nodes.Length - 2]);

                            // Delete intermediate nodes of the eliminating thread from vascular_system.
                            List<Node> threadNodes = thread.nodes.ToList();
                            threadNodes.RemoveAt(0);
                            threadNodes.RemoveAt(threadNodes.Count - 1);
                            vascular_system.RemoveAll(x => threadNodes.Contains(x));
                            // Delete value of length of eliminating thread from reference_lengths.
                            reference_lengths.Remove(threadDesc);

                            // All remaining neighbours of the Core3Node with smaller radius 
                            // will be transferred to the other Core3Node.

                            // Thread reversion can be done by this line:
                            // thread.nodes = thread.nodes.Reverse().ToArray();

                            // Compare radiuses. 
                            if (lastCore3.bifurcationNode.Rad <= firstCore3.bifurcationNode.Rad)
                            {
                                moveAllNeighbours(lastCore3, firstCore3, reference_lengths, _dz, desiredLength);
                                vascular_system.Remove(lastCore3.bifurcationNode);
                                core3.Remove(lastCore3);
                            }
                            else
                            {
                                moveAllNeighbours(firstCore3, lastCore3, reference_lengths, _dz, desiredLength);
                                vascular_system.Remove(firstCore3.bifurcationNode);
                                core3.Remove(firstCore3);
                                // Current core3 item was removed, so we need to start
                                // processing of its successor immediately.
                                iC3--;
                                // Interrupt the neighbours for.
                                break;
                            }
                        } // End of last node is a bifurcation node else.
                    } // End of too short thread else.

                } // End of neighbours for.
            } // End of core3 for.
        }

        // Checks if lengths are same enough.
        public bool areLengthsSame(double currentLength, double desiredLength, double relErr, double absErr)
        {
            bool checksPassed = true;
            if (relErr > 0.0)
            {
                // Relative error check enabled.
                if (Math.Abs(currentLength - desiredLength) > desiredLength * relErr)
                {
                    checksPassed = false;
                }
            }
            if (absErr > 0.0)
            {
                // Absolute error check enabled.
                if (Math.Abs(currentLength - desiredLength) > absErr)
                {
                    checksPassed = false;
                }
            }
            return checksPassed;
        }

        // Returns count of segments removed.
        // This procedure changes new_thread_list so that length of the corresponding thread become correct.
        public int adjustLength(List<Node> new_thread_list, double desiredLength, double dz, ref int newId, 
            Core3Node lastCore3, bool linearA0notR, double relErr, double absErr)
        {
            VascularThread newThread;
            Node firstNode = new_thread_list[0];
            Node lastNode = new_thread_list[new_thread_list.Count - 1];
            newThread = getThread(new_thread_list);
            ThreadDescriptor threadDesc = new ThreadDescriptor(newThread);

            double currentLength = newThread.getLength();

            int countOfSegmentsRemoved = 0;

            if (relErr <= 0.0 && absErr <= 0.0)
                throw new ArgumentException("At least one error parameter should be positive.");

            if (areLengthsSame(currentLength, desiredLength, relErr, absErr))
            {
                return countOfSegmentsRemoved;
            }

            if (lastNode.getNeighbours().Count == 1)
            {
                // Terminal branch
                if (currentLength > desiredLength)
                {
                    // Too long thread.

                    while (true)
                    {
                        // Length adjusting cycle.
                        if (new_thread_list.Count == 2)
                        {
                            // Only 1 segment left.
                            adjustSegmentLength(new_thread_list[0], new_thread_list[1], desiredLength);

                            Node newNode = getNodeInBetween(new_thread_list[0], new_thread_list[1], linearA0notR, newId);
                            new_thread_list.Insert(1, newNode);
                            newId++;
                            // Length now is OK, interrupting the loop.
                            break;
                        }
                        else
                        {
                            // At least 2 segments left
                            double lastSegmentLength = getLastSegmentLength(new_thread_list);
                            if ((currentLength - desiredLength) >= lastSegmentLength)
                            {
                                // Excess of length of thread is greater or equal to length of last segment.
                                /////////////////////////////
                                // So this segment needs to be removed, but radius & beta values should be saved.
                                new_thread_list[new_thread_list.Count - 2].setLumen_sq0(
                                    new_thread_list[new_thread_list.Count - 1].getLumen_sq0());
                                new_thread_list[new_thread_list.Count - 2].Beta =
                                    new_thread_list[new_thread_list.Count - 1].Beta;
                                new_thread_list.RemoveAt(new_thread_list.Count - 1);
                                // Length now can still differ, continuing the loop.
                                newThread = getThread(new_thread_list);
                                currentLength = newThread.getLength();
                                countOfSegmentsRemoved++;
                                continue;
                            }
                            else
                            {
                                // Excess of length of thread is less than length of last segment.
                                adjustSegmentLength(
                                    new_thread_list[new_thread_list.Count - 2],
                                    new_thread_list[new_thread_list.Count - 1],
                                    lastSegmentLength - (currentLength - desiredLength));
                                // Length now is OK, interrupting the loop.
                                break;
                            }
                        }
                    } // End of while (true) cycle of length adjustment.
                } // End of too long thread if.
                else
                {
                    // Too short thread.

                    while (true)
                    {
                        // Length adjusting cycle.
                        if ((desiredLength - currentLength) >= dz)
                        {
                            // Shortage of length of thread is greater or equal to length of a segment.
                            // So this segment needs to be added, but radius & beta values should be saved.
                            new_thread_list.Add(getNodeBehindTheEnd(
                                new_thread_list[new_thread_list.Count - 2],
                                new_thread_list[new_thread_list.Count - 1], newId, dz));
                            newId++;
                            // Length now can still differ, continuing the loop.
                            newThread = getThread(new_thread_list);
                            currentLength = newThread.getLength();
                            continue;
                        }
                        else
                        {
                            // Excess of length of thread is less than length of last segment.
                            double lastSegmentLength = getLastSegmentLength(new_thread_list);
                            adjustSegmentLength(
                                new_thread_list[new_thread_list.Count - 2],
                                new_thread_list[new_thread_list.Count - 1],
                                lastSegmentLength + (desiredLength - currentLength));
                            // Length now is OK, interrupting the loop.
                            break;
                        }
                    } // End of while (true) cycle of length adjustment.

                    // If there are only two nodes in a thread, add a node in between.
                    if (new_thread_list.Count == 2)
                    {
                        // Only 1 segment left.
                        adjustSegmentLength(new_thread_list[0], new_thread_list[1], desiredLength);

                        Node newNode = getNodeInBetween(new_thread_list[0], new_thread_list[1], linearA0notR, newId);
                        new_thread_list.Insert(1, newNode);
                        newId++;
                        // Length now is OK, interrupting the loop.
                    }
                } // End of too short thread else.
            } // End of terminal branch if.
            else
            {
                //////////////////////////////////////////
                // Last node is a bifurcation node.
                // lastCore3 is valid.

                if (currentLength > desiredLength)
                {
                    // Thread is too long.

                    double minLength = (lastNode.Position - firstNode.Position).Length();

                    if (minLength > desiredLength)
                    {
                        throw new Exception("Thread cannot be shortened to desired length " +
                            desiredLength + " cm " +
                            threadDesc.id0 + " -> " + threadDesc.idNdec1);
                    }

                    // Iterate through the list of nodes to determine 
                    // a part of it to be shortened (some nodes in the end starting from iS).
                    int iS = newThread.nodes.Length - 2;
                    for (; iS >= 0; iS--)
                    {
                        if (
                            // Part that will be left unchanged
                            newThread.getPartialLength(0, iS) +
                            // and part that will be shortened
                            (newThread.nodes[newThread.nodes.Length - 1].Position -
                            newThread.nodes[iS].Position).Length()
                            // together should be less or equal to desired length.
                            <= desiredLength)
                        {
                            // With such a shortening the thread will be short enough 
                            // (length will be less or equal to desired one).
                            break;
                        }
                    }

                    if (iS < newThread.nodes.Length - 2)
                    {
                        // There are some segments to be shortened.
                        // Do the shortening.
                        double shorteningLength = newThread.getPartialLength(iS, newThread.nodes.Length - 1);

                        // Move all intermediate nodes to the shortest path line.
                        Vector3 newPosition;
                        Vector3 dirVector = newThread.nodes[newThread.nodes.Length - 1].Position -
                            newThread.nodes[iS].Position;
                        double cumulativeShorteningLength =
                            (newThread.nodes[iS + 1].Position - newThread.nodes[iS].Position).Length();
                        for (int iIN = iS + 1; iIN < (newThread.nodes.Length - 1); iIN++)
                        {
                            newPosition = newThread.nodes[iS].Position + dirVector *
                                (float)(cumulativeShorteningLength / shorteningLength);
                            cumulativeShorteningLength +=
                                (newThread.nodes[iIN + 1].Position - newThread.nodes[iIN].Position).Length();
                            newThread.nodes[iIN].Position = newPosition;
                        }
                    }
                } // End of too long thread if.

                // Thread is too short or of desired length (either initially or after shortening).

                if (new_thread_list.Count == 2)
                {
                    // >-< too short thread.
                    // Adding a node in between to make it >--<.
                    Node newNode = getNodeInBetween(newThread.nodes[0], newThread.nodes[1], linearA0notR, newId);
                    new_thread_list.Insert(1, newNode);
                    newId++;
                }
                newThread = getThread(new_thread_list);
                // Update current length.
                currentLength = newThread.getLength();

                if (areLengthsSame(currentLength, desiredLength, relErr, absErr))
                {
                    return countOfSegmentsRemoved;
                }

                // Now the thread has at least 2 segments and length shorter than desired one.
                // Shift prelast node so that length will be OK.

                // Place the node between its neighbours.
                Node nodeToShift = newThread.nodes[newThread.nodes.Length - 2];
                Vector3 prelastSegment = newThread.nodes[newThread.nodes.Length - 2].Position -
                    newThread.nodes[newThread.nodes.Length - 3].Position;
                Vector3 vec2segments = newThread.nodes[newThread.nodes.Length - 1].Position -
                    newThread.nodes[newThread.nodes.Length - 3].Position;
                double vec2segmentsLength = vec2segments.Length();
                nodeToShift.Position = nodeToShift.Position - prelastSegment + vec2segments / 2.0f;
                currentLength = newThread.getLength();

                // Now last 2 segments are parallel and of equal length.
                // Set shiftDir as a vector orthogonal to vec2segments.

                Vector3 shiftDir;
                shiftDir.X = -vec2segments.Y;
                shiftDir.Y = vec2segments.X;
                shiftDir.Z = vec2segments.Z;

                // Calculate required shift.
                double lengthOfStaticPart = currentLength - vec2segmentsLength;
                double desiredLengthOfLast2Segments = desiredLength - lengthOfStaticPart;

                if (desiredLengthOfLast2Segments < vec2segmentsLength)
                {
                    throw new Exception("Thread cannot be modified to get desired length " +
                        desiredLength + " cm (by shifting the prelast node) " +
                        threadDesc.id0 + " -> " + threadDesc.idNdec1);
                }

                double segmentAngle = Math.Acos(vec2segmentsLength / desiredLengthOfLast2Segments);
                Vector3 shiftVec = shiftDir *
                    (float)(Math.Sin(segmentAngle) * (desiredLengthOfLast2Segments / 2.0) / shiftDir.Length());

                // Shift the node.
                nodeToShift.Position = nodeToShift.Position + shiftVec;

            } // End of last node is a bifurcation node else.

            return countOfSegmentsRemoved;
        }

        // Returns count of segments removed.
        // This procedure adds or removes nodes to set the resolution.
        public int rearrangeNodes(List<Core3Node> core3, SortedList<ThreadDescriptor, double> reference_lengths,
            double _dz, bool linearA0notR, double absErr, double relErr, bool conserveLengths)
        {
            ResetCore3Flags(core3);

            int newId = getMaxId() + 1;

            int countOfSegmentsRemoved = 0;

            for (int c = 0; c < core3.Count; c++)
            {
                for (int i = 0; i < core3[c].neighbours.Count; i++)
                {
                    Node n = core3[c].neighbours[i];
                    if (core3[c].threadsProcessed[n.getId()])
                        continue;
                    VascularThread thread = getThread(core3[c].bifurcationNode, n);
                    double oldLength = thread.getLength();
                    double desiredLength = 0.0;
                    Node firstNode = thread.nodes[0];
                    Node lastNode = thread.nodes[thread.nodes.Length - 1];
                    ThreadDescriptor threadDesc = new ThreadDescriptor(thread);
                    List<Node> new_thread_list = new List<Node>();
                    bool new_thread_list_valid = false;
                    if (!reference_lengths.TryGetValue(threadDesc, out desiredLength))
                    {
                        throw new Exception("Failed to get stored reference length for thread " + 
                            threadDesc.id0 + " -> " + threadDesc.idNdec1);
                    }

                    if (desiredLength > 2 * _dz)
                    {
                        // Thread is long enough.
                        int N = (int)Math.Floor(desiredLength / _dz); // segments count.

                        if (N < 2)
                        {
                            throw new Exception("Thread of " + desiredLength * 100 + " cm length is to be divided "+
                                "into N = " + N + " < 2 segments: " +
                                threadDesc.id0 + " -> " + threadDesc.idNdec1);
                        }

                        //if (threadDesc.id0 == 249 && threadDesc.idNdec1 == 20667)
                        //{
                        //    int a = 0;
                        //    a++;
                        //}

                        //if (c == 361 && i == 0)
                        //{
                        //    {
                        //        int a = 0;
                        //        a++;
                        //    }
                        //}

                        // Actual size of segment.
                        double dz = desiredLength / N;

                        // Build a list of new nodes.
                        new_thread_list.Add(firstNode);
                        for (int t = 1; t < N; t++)
                        {
                            if (t * dz >= oldLength)
                                break;

                            new_thread_list.Add(thread.getInterNode(t * dz, linearA0notR, newId));

                            newId++;
                        }
                        new_thread_list.Add(lastNode);

                        Core3Node lastCore3 = null;
                        bool threadIsTerminal = (lastNode.getNeighbours().Count == 1);
                        if (lastNode.getNeighbours().Count > 2)
                        {
                            lastCore3 = core3.Find(x => x.bifurcationNode.Equals(lastNode));
                        }

                        // Adjust lengths according to reference length.

                        if (conserveLengths)
                        {
                            countOfSegmentsRemoved +=
                                adjustLength(new_thread_list, desiredLength, dz, ref newId, lastCore3, linearA0notR, relErr, absErr);
                        }
                        /////////////////
                        // Lengths are okay. Copy nodes to vascular_system

                        // Replace neighbour of source node.
                        core3[c].RemoveNeighbour(thread.nodes[1]);
                        core3[c].AddNeighbour(new_thread_list[1]);
                        // One neighour was removed, decrease the counter.
                        i--;

                        // Set neighbourhood for internal nodes of new thread.
                        for (int t = 1; t < new_thread_list.Count - 1; t++)
                        {
                            new_thread_list[t].getNeighbours().Clear();
                            new_thread_list[t].getNeighbours().Add(new_thread_list[t - 1]);
                            new_thread_list[t].getNeighbours().Add(new_thread_list[t + 1]);
                        }

                        // Replace internal nodes of thread in vascular system.

                        List<Node> threadNodes = thread.nodes.ToList();
                        threadNodes.RemoveAt(0);
                        threadNodes.RemoveAt(threadNodes.Count - 1);
                        vascular_system.RemoveAll(x => threadNodes.Contains(x));
                        // Four lines before this one replace two lines after.
                        //for (int ii = 1; ii < thread.nodes.Length - 1; ii++)
                        //    vascular_system.Remove(thread.nodes[ii]);
                        for (int ii = 1; ii < new_thread_list.Count - 1; ii++)
                            vascular_system.Add(new_thread_list[ii]);

                        // Replace neighbour of last node (or the last node itself).
                        if (!threadIsTerminal)
                        {
                            // lastCore3 is valid.
                            lastCore3.RemoveNeighbour(thread.nodes[thread.nodes.Length - 2]);
                            lastCore3.AddNeighbour(new_thread_list[new_thread_list.Count - 2]);
                            lastCore3.threadsProcessed[new_thread_list[new_thread_list.Count - 2].getId()] = true;
                        }
                        else
                        {
                            // Terminal branch
                            vascular_system.Remove(lastNode);
                            lastNode = new_thread_list[new_thread_list.Count - 1];
                            vascular_system.Add(lastNode);

                            lastNode.getNeighbours().Clear();
                            lastNode.getNeighbours().Add(new_thread_list[new_thread_list.Count - 2]);
                        }

                        // Mark a thread as processed.
                        core3[c].threadsProcessed[new_thread_list[1].getId()] = true;
                        new_thread_list_valid = true;
                    } // End of long enough thread if.
                    else
                    {
                        // Short thread: length <= 2 * _dz.
                        if (lastNode.getNeighbours().Count == 1)
                        {
                            // Terminal branch.
                            if (thread.nodes.Length > 2)
                            {
                                // Remove internal nodes of thread from vascular system.
                                for (int ii = 1; ii < thread.nodes.Length - 1; ii++)
                                    vascular_system.Remove(thread.nodes[ii]);
                            }

                            // Remove a neighbour of source node.
                            core3[c].RemoveNeighbour(thread.nodes[1]);

                            // One neighour was removed, derease a counter.
                            i--;

                            // Remove a neighbour of last node.
                            lastNode.getNeighbours().Clear();

                            // Adjust lengths according to reference length.
                            if (conserveLengths)
                                adjustSegmentLength(firstNode, lastNode, desiredLength);
                            //////////////////////////////////////////
                            // Add a node between last and first nodes.
                            Node newNode = addNodeInBetween(core3[c], lastNode, linearA0notR, newId);
                            newId++;

                            new_thread_list.Add(firstNode);
                            new_thread_list.Add(newNode);
                            new_thread_list.Add(lastNode);

                            // Mark a thread as processed.
                            core3[c].threadsProcessed[newId - 1] = true;
                            new_thread_list_valid = true;
                        }
                        else
                        {
                            // Last node is a bifurcation node.

                            // Such a thread is a result of aborted merge (this merge would have lead to a cycle -<==>,
                            // which is not supported).

                            // Build a list of new nodes.
                            new_thread_list.Add(firstNode);
                            new_thread_list.Add(getNodeInBetween(firstNode, lastNode, linearA0notR, newId));
                            newId++;
                            new_thread_list.Add(lastNode);

                            Core3Node lastCore3 = core3.Find(x => x.bifurcationNode.Equals(lastNode));

                            // Adjust lengths according to reference length.

                            if (conserveLengths)
                            {
                                countOfSegmentsRemoved +=
                                    adjustLength(new_thread_list, desiredLength, _dz, ref newId, lastCore3, linearA0notR, relErr, absErr);
                            }
                            /////////////////
                            // Lengths are okay. Copy nodes to vascular_system

                            // Replace neighbour of source node.
                            core3[c].RemoveNeighbour(thread.nodes[1]);
                            core3[c].AddNeighbour(new_thread_list[1]);
                            // One neighour was removed, decrease the counter.
                            i--;

                            // Set neighbourhood for internal nodes of new thread.
                            for (int t = 1; t < new_thread_list.Count - 1; t++)
                            {
                                new_thread_list[t].getNeighbours().Add(new_thread_list[t - 1]);
                                new_thread_list[t].getNeighbours().Add(new_thread_list[t + 1]);
                            }

                            // Replace internal nodes of thread in vascular system.

                            List<Node> threadNodes = thread.nodes.ToList();
                            threadNodes.RemoveAt(0);
                            threadNodes.RemoveAt(threadNodes.Count - 1);
                            vascular_system.RemoveAll(x => threadNodes.Contains(x));
                            // Four lines before this one replace two lines after.
                            //for (int ii = 1; ii < thread.nodes.Length - 1; ii++)
                            //    vascular_system.Remove(thread.nodes[ii]);
                            for (int ii = 1; ii < new_thread_list.Count - 1; ii++)
                                vascular_system.Add(new_thread_list[ii]);

                            // Replace neighbour of last node (or the last node itself).
                            // lastCore3 is valid.
                            lastCore3.RemoveNeighbour(thread.nodes[thread.nodes.Length - 2]);
                            lastCore3.AddNeighbour(new_thread_list[new_thread_list.Count - 2]);
                            lastCore3.threadsProcessed[new_thread_list[new_thread_list.Count - 2].getId()] = true;

                            // Mark a thread as processed.
                            core3[c].threadsProcessed[new_thread_list[1].getId()] = true;
                            new_thread_list_valid = true;
                            //new_thread_list_valid = false;
                        } // End of last node is a bifurcation node else.
                    } // End of short thread else.

                    // Update key for reference length.
                    if (new_thread_list_valid)
                    {
                        VascularThread newThread = getThread(new_thread_list);
                        reference_lengths.Remove(threadDesc);
                        reference_lengths.Add(new ThreadDescriptor(newThread), desiredLength);
                    }

                } // End of neighbours for.
            } // End of core3 for.

            return countOfSegmentsRemoved;
        }

        public double getLastSegmentLength(List<Node> nodesList)
        {
            Vector3 lastSegment = nodesList[nodesList.Count - 1].Position - nodesList[nodesList.Count - 2].Position;
            return lastSegment.Length();
        }

        public void adjustSegmentLength(Node nFrom, Node nTo, double desiredLength)
        {
            Vector3 branchVector = nTo.Position - nFrom.Position;
            Vector3 newBranchVector = branchVector * (float)(desiredLength / branchVector.Length());
            nTo.Position = nFrom.Position + newBranchVector;
        }


        public void setResolutionSaveLengths(double _dz, bool saveTopology, bool linearA0notR, bool conserveLengths)
        {
            List<Core3Node> core3 = buildCore3NodesList();
            SortedList<ThreadDescriptor, double> reference_lengths = calculateReferenceLengths(core3);

            if (!saveTopology)
                changeTopology(core3, reference_lengths, _dz);

            int nodesRemoved = rearrangeNodes(core3, reference_lengths, _dz, linearA0notR, 0.0, 1E-6 ,conserveLengths);
            //int a = 0;
            //a++;
        }

        public Node getNodeBehindTheEnd(Node n1, Node n2, int id, double desiredLength)
        {
            Vector3 branchVector = n2.Position - n1.Position;
            Vector3 newPosition = n2.Position + branchVector * (float)(desiredLength / branchVector.Length());
            double newS0 = n2.getLumen_sq0();

            Node newNode = new Node(id, newPosition, Math.Sqrt(newS0 / Math.PI));
            newNode.Beta = n2.Beta;

            return newNode;
        }

        public Node getNodeInBetween(Node n1, Node n2, bool linearA0notR, int id)
        {
            Vector3 newPosition = (n1.Position + n2.Position) / 2.0f;
            double newR = 0.0;

            if (linearA0notR)
            {
                double newS0 = (n1.getLumen_sq0() + n2.getLumen_sq0()) / 2.0;
                newR = Math.Sqrt(newS0 / Math.PI);
            }
            else
            {
                newR = (Math.Sqrt(n1.getLumen_sq0() / Math.PI) + Math.Sqrt(n2.getLumen_sq0() / Math.PI)) / 2.0;
            }

            Node newNode = new Node(id, newPosition, newR);
            newNode.Beta = (n1.Beta + n2.Beta) / 2.0;

            return newNode;
        }

        public Node addNodeInBetween(Node n1, Node n2, bool linearA0notR, int id)
        {
            Node newNode = getNodeInBetween(n1, n2, linearA0notR, id);

            vascular_system.Add(newNode);

            newNode.addNeighbour(n1);
            newNode.addNeighbour(n2);

            n1.addNeighbour(newNode);
            n2.addNeighbour(newNode);

            return newNode;
        }

        public Node addNodeInBetween(Core3Node n1, Node n2, bool linearA0notR, int id)
        {
            Node newNode = getNodeInBetween(n1.bifurcationNode, n2, linearA0notR, id);

            vascular_system.Add(newNode);

            newNode.addNeighbour(n1.bifurcationNode);
            newNode.addNeighbour(n2);

            n1.AddNeighbour(newNode);
            n2.addNeighbour(newNode);

            return newNode;
        }

        public Node addNodeInBetween(Core3Node n1, Core3Node n2, bool linearA0notR, int id)
        {
            Node newNode = getNodeInBetween(n1.bifurcationNode, n2.bifurcationNode, linearA0notR, id);

            vascular_system.Add(newNode);

            newNode.addNeighbour(n1.bifurcationNode);
            newNode.addNeighbour(n2.bifurcationNode);

            n1.AddNeighbour(newNode);
            n2.AddNeighbour(newNode);

            return newNode;
        }

        ////////////////////
        // Old version.
        // GetInterNode does not accept linearA0notR parameter.
        //public void setResolution(double _dz, bool saveTopology)
        //{
        //    foreach (var n in vascular_system)
        //    {
        //        n.is_processed = false;
        //        foreach (var nn in n.getNeighbours())
        //            if (!nn.getNeighbours().Contains(n))
        //                nn.getNeighbours().Add(n);
        //    }
            
        //    int id_count = vascular_system.Count;
        //    List<Node> core3 = vascular_system.FindAll(x => x.getNeighbours().Count > 2);
        //    //////////////////////////////////////////////
        //    // TODO: add threads with length 1 processing!
        //    //////////////////////////////////////////////
        //    //for (int c = 0; c < core3.Count; c++)
        //    //{
        //    //    core3[c].is_processed = true;
        //    //}

        //    for (int c = 0; c < core3.Count; c++)
        //    {
        //        Node prev_node, curr_node;
        //        List<Node> core_neighbours = core3[c].getNeighbours();

        //        for (int i = 0; i < core_neighbours.Count; i++)
        //        {
        //            Node n = core_neighbours[i];
        //            float lenght = 0;
        //            if (n.is_processed)
        //                continue;
        //            List<Node> protothread = new List<Node>();
        //            protothread.Add(core3[c]);
        //            protothread.Add(n);
        //            prev_node = core3[c];
        //            curr_node = n;
        //            if (n.getNeighbours().Count <= 2)
        //                n.is_processed = true;
        //            while (true)
        //            {
        //                lenght += (float)Vector3.Distance(curr_node.Position, prev_node.Position);
        //                if (curr_node.getNeighbours().Count == 2)
        //                {
        //                    protothread.Add(curr_node.getNeighbours().Find(x => x != prev_node));
        //                    prev_node = curr_node;
        //                    curr_node = protothread.Last();
        //                    curr_node.is_processed = true;
        //                }
        //                else
        //                {
        //                    break;
        //                }
        //            }

        //            double[] natural_t = new double[protothread.Count];
        //            natural_t[0] = 0;
        //            for (int t = 1; t < protothread.Count; t++)
        //                natural_t[t] = natural_t[t - 1] + Vector3.Distance(protothread[t].Position, protothread[t - 1].Position);

        //            for (int t = 1; t < protothread.Count - 1; t++)
        //            {
        //                Vector3 d2 = (protothread[t + 1].Position - protothread[t].Position) * 
        //                    (float)(1.0f / (natural_t[t + 1] - natural_t[t]));
        //                Vector3 d1 = (protothread[t].Position - protothread[t - 1].Position) * 
        //                    (float)(1.0f / (natural_t[t] - natural_t[t - 1]));
        //                protothread[t].curvature = 
        //                    ((d2 - d1) * (float)(0.5f / (natural_t[t + 1] - natural_t[t - 1]))).Length();
        //            }
        //            protothread[0].curvature = protothread[1].curvature;
        //            protothread[protothread.Count - 1].curvature = protothread[protothread.Count - 2].curvature;

        //            if (lenght < 2 * _dz)
        //            {
        //                if (protothread.Last().getNeighbours().Count > 2)
        //                {
        //                    if (saveTopology)
        //                    {
        //                        if (protothread.Count > 2)
        //                        {
        //                            protothread[0].getNeighbours().Remove(protothread[1]);
        //                            protothread[protothread.Count - 1].getNeighbours().
        //                                Remove(protothread[protothread.Count - 2]);
        //                            protothread[0].getNeighbours().Add(protothread[protothread.Count - 1]);
        //                            protothread[protothread.Count - 1].getNeighbours().Add(protothread[0]);
        //                            for (int ii = 1; ii < protothread.Count - 1; ii++)
        //                            {
        //                                vascular_system.Remove(protothread[ii]);
        //                            }
        //                            protothread[0].is_processed = true;
        //                        }
        //                    }
        //                    else
        //                    { // !saveTopology
        //                        int N = protothread.Count;
        //                        List<Node> tot_neighbours = new List<Node>();
        //                        tot_neighbours.AddRange(protothread.First().getNeighbours());
        //                        tot_neighbours.Remove(protothread[1]);
        //                        tot_neighbours.AddRange(protothread.Last().getNeighbours());
        //                        tot_neighbours.Remove(protothread[N - 2]);
        //                        //Vector3 pos = Vector3.Zero;
        //                        //foreach (var ptnode in protothread)
        //                        //    pos += ptnode.Position;
        //                        //pos /= N;
        //                        Node union_node = new Node(id_count, protothread[N / 2].Position,
        //                            Math.Sqrt(protothread[N / 2].lumen_sq_0 / Math.PI));
        //                        union_node.curvature = protothread[N / 2].curvature;
        //                        union_node.Beta = protothread[N / 2].Beta;
        //                        foreach (var nn in tot_neighbours)
        //                        {
        //                            nn.getNeighbours().Remove(protothread.First());
        //                            nn.getNeighbours().Remove(protothread.Last());
        //                            nn.getNeighbours().Add(union_node);
        //                        }
        //                        union_node.addNeighbours(tot_neighbours.ToArray());

        //                        core3.Remove(protothread.Last());
        //                        i--;
        //                        foreach (var nn in protothread)
        //                            vascular_system.Remove(nn);

        //                        core3[c] = union_node;
        //                        union_node.is_processed = true;
        //                        core_neighbours = core3[c].getNeighbours();
        //                        i = -1;
        //                        vascular_system.Add(union_node);

        //                        id_count++;
        //                    }
        //                }
        //                else
        //                { // protothread.Last().getNeighbours().Count == 1
        //                    protothread.First().getNeighbours().Remove(protothread[1]);
        //                    i--;
        //                    protothread.First().getNeighbours().Add(protothread.Last());
        //                    protothread.Last().getNeighbours().Clear();
        //                    protothread.Last().getNeighbours().Add(protothread.First());
        //                    Vector3 dir_vector = protothread.Last().Position - protothread.First().Position;
        //                    dir_vector.Normalize();
        //                    dir_vector = dir_vector * (float)_dz;
        //                    protothread.Last().Position = protothread.First().Position + dir_vector;
        //                    protothread.Last().is_processed = true;
        //                    for (int ii = 1; ii < protothread.Count - 1; ii++)
        //                        vascular_system.Remove(protothread[ii]);
        //                }
        //            }
        //            else
        //            {
        //                VascularThread thread = new VascularThread();
        //                thread.nodes = protothread.ToArray();
        //                int N = (int)Math.Floor(lenght / _dz);
        //                List<Node> new_thread_list = new List<Node>();
        //                double dz = lenght / N;
        //                for (int t = 0; t < N; t++)
        //                {
        //                    id_count++;
        //                    new_thread_list.Add(thread.getInterNode(t * dz, id_count));
        //                }

        //                new_thread_list[0] = thread.nodes[0];
        //                new_thread_list[N - 1] = thread.nodes[thread.nodes.GetLength(0) - 1];

        //                for (int t = 1; t < new_thread_list.Count - 1; t++)
        //                    new_thread_list[t].addNeighbours(new Node[] { new_thread_list[t - 1], new_thread_list[t + 1] });

        //                new_thread_list[0].addNeighbours(new Node[] { new_thread_list[1] });
        //                new_thread_list[N - 1].addNeighbours(new Node[] { new_thread_list[N - 2] });

        //                new_thread_list[0].getNeighbours().Remove(thread.nodes[1]);
        //                i--;
        //                new_thread_list[N - 1].getNeighbours().Remove(thread.nodes[thread.nodes.GetLength(0) - 2]);

        //                for (int ii = 1; ii < protothread.Count - 1; ii++)
        //                    vascular_system.Remove(protothread[ii]);
        //                for (int ii = 1; ii < new_thread_list.Count - 1; ii++)
        //                    vascular_system.Add(new_thread_list[ii]);
        //            }
        //        }

        //        core3[c].is_processed = true;
        //    }
        //}

        public void mergeCoupleNode(double dz)
        {
            int id_count = vascular_system.Count;
            List<Node> core3 = vascular_system.FindAll(x => x.getNeighbours().Count > 2);

            foreach (var n in vascular_system)
            {
                n.is_processed = false;
                n.member_of_protoknot = false;
            }

            for (int i = 0; i < core3.Count; i++)
            {
                List<Node> curr_front = new List<Node>();
                List<Node> protoknot = new List<Node>();
                List<Node> new_front = new List<Node>();
                curr_front.Add(core3[i]);
                protoknot.Add(core3[i]);
                core3[i].member_of_protoknot = true;
                bool sign = false;

                while (true)
                {
                    foreach (var nn in curr_front)
                        nn.is_processed = true;

                    foreach (var curr_node in curr_front)
                        foreach (var n in curr_node.getNeighbours())
                        {
                            if (n.getNeighbours().Count > 2 && (!n.is_processed) 
                               // &&
                               // Vector3.Distance(core3[i].Position, n.Position) < dz
                                )
                            {
                                new_front.Add(n);
                                sign = true;
                            }
                        }

                    if (sign)
                    {
                        curr_front.Clear();
                        curr_front = new List<Node>(new_front);

                        protoknot.AddRange(curr_front);
                        foreach (var cc in curr_front)
                        {
                            cc.member_of_protoknot = true;
                        }
                        new_front.Clear();
                        sign = false;
                    }
                    else
                    {
                        if (protoknot.Count == 1)
                            break;

                        Vector3 cntr_pos = new Vector3(0, 0, 0);
                        double rad_0 = 0;
                        foreach (var n in protoknot)
                        {
                            cntr_pos = cntr_pos + n.Position;
                            rad_0 += Math.Sqrt(n.lumen_sq_0 / Math.PI);
                            core3.Remove(n);
                            if (i > 0)
                                i--;
                        }
                        cntr_pos = cntr_pos * (1.0f / protoknot.Count);
                        rad_0 = rad_0 / protoknot.Count;

                        Node union_node = new Node(id_count, cntr_pos, rad_0);

                        List<Node> all_neighbours = new List<Node>();
                        foreach (var n in protoknot)
                            foreach (var nn in n.getNeighbours())
                                if (!nn.member_of_protoknot)
                                    all_neighbours.Add(nn);

                        all_neighbours = all_neighbours.Distinct().ToList();
                        union_node.addNeighbours(all_neighbours.ToArray());
                        foreach (var n in all_neighbours)
                        {
                            foreach (var nn in protoknot)
                                n.getNeighbours().Remove(nn);
                            n.getNeighbours().Add(union_node);
                        }
                        foreach (var n in protoknot)
                            vascular_system.Remove(n);
                        vascular_system.Add(union_node);
                        id_count++;
                        break;
                    }
                }

            }
        }


        public void fillBifurcationNodes()
        {
            List<Node> core3 = vascular_system.FindAll(x => x.getNeighbours().Count > 2);
            foreach (var n in core3)
            {
                float av_rad = 0;
                foreach (var nn in n.getNeighbours())
                {
                    if (Math.Sqrt(nn.getLumen_sq0() / Math.PI) > av_rad)
                        av_rad = (float)Math.Sqrt(nn.getLumen_sq0() / Math.PI);
                }
                n.setLumen_sq0(Math.PI * av_rad * av_rad);
            }
        }

        public void SortByXYZ()
        {
            vascular_system.Sort(Node.CompareNodesByXYZ);
        }

        public bool AddConnection(int idFrom, int idTo)
        {
            if (idFrom == idTo)
                return false;
            Node from = vascular_system.Find(x => x.getId() == idFrom);
            Node to = vascular_system.Find(x => x.getId() == idTo);
            return AddConnection(from, to);
        }

        public bool AddConnection(Node from, Node to)
        {
            if ((from == null) || (to == null))
                return false;
            if (from.getNeighbours().Contains(to))
                return false;
            if (to.getNeighbours().Contains(from))
                return false;
            from.addNeighbours(new Node[] { to });
            to.addNeighbours(new Node[] { from });
            return true;
        }
        
        public bool RemoveConnection(int idFrom, int idTo)
        {
            if (idFrom == idTo)
                return false;
            Node from = vascular_system.Find(x => x.getId() == idFrom);
            Node to = vascular_system.Find(x => x.getId() == idTo);
            return RemoveConnection(from, to);
        }

        public bool RemoveConnection(Node from, Node to)
        {
            if ((from == null) || (to == null))
                return false;
            if (!from.getNeighbours().Contains(to))
                return false;
            if (!to.getNeighbours().Contains(from))
                return false;
            from.getNeighbours().Remove(to);
            to.getNeighbours().Remove(from);
            return true;
        }
        /*
        public void setSubsystem(List<Node> sub_sustem)
        {
            foreach (var n in sub_sustem)
            {
                int ind = vascular_system.FindIndex(x => x.getId() == n.getId());
                List<Node> neughbours = vascular_system[ind].getNeighbours();
                n.addNeighbours(neughbours.ToArray());
                foreach (var nn in neughbours)
                {
                    nn.addNeighbours(new Node[] { n });
                    nn.getNeighbours().Remove(vascular_system[ind]);
                }

                vascular_system[ind] = n;
            }
        }
         */

        /*
        public void decomposeNet()
        {
            foreach (var n in vascular_system)
                foreach (var nn in n.getNeighbours())
                    if (!nn.getNeighbours().Contains(n))
                        nn.getNeighbours().Add(n);

            knots = new List<Knot>();
            threads = new List<VascularThread>();

            List<Node> core3 = vascular_system.FindAll(x => x.getNeighbours().Count > 2);


            foreach (var n in core3)
                n.is_processed = false;
            foreach (var node in core3)
            {
                List<Node> front = new List<Node>();
                List<Node> new_front = new List<Node>();
                List<Node> protoknot = new List<Node>();

                if (node.is_processed)
                    continue;
                front.Add(node);
                while (true)
                {
                    foreach (var n in front)
                    {
                        foreach (var nn in n.getNeighbours())
                            if (nn.getNeighbours().Count > 2)
                            {
                                if (!nn.is_processed)
                                {
                                    new_front.Add(nn);
                                    nn.is_processed = true;
                                }
                            }
                            else
                                if (nn.getNeighbours().Count <= 2)
                                    protoknot.Add(nn);
                    }
                    if (new_front.Count == 0)
                        break;
                    front = new List<Node>(new_front);
                    new_front.Clear();
                }
                protoknot = new List<Node>(protoknot.Distinct());

                Knot knot = new Knot();


                knot.nodes = protoknot.ToArray();
                knot.center_node = node;
                int L = knot.nodes.GetLength(0);
                knot.velocity = new double[L];
                knot.pressure = new double[L];
                knot.lumen_sq = new double[L];
                knot.flux = new double[L];
                knot.center_node = node;
                knot.dir_vectors = new Vector3[L];
                for (int ii = 0; ii < knot.nodes.GetLength(0); ii++)
                {
                    knot.dir_vectors[ii] = knot.nodes[ii].Position - node.Position;
                    knot.dir_vectors[ii].Normalize();
                }

                knots.Add(knot);
            }

            foreach (var n in vascular_system)
                n.is_processed = false;

            foreach (var k in knots)
                k.center_node.is_processed = true;

            if (knots.Count != 0)
                foreach (var knot in knots)
                {
                    for (int i = 0; i < knot.nodes.GetLength(0); i++)
                    {
                        List<Node> protothread = new List<Node>();
                        if (knot.nodes[i].is_processed)
                            continue;

                        protothread.Add(knot.center_node);
                        protothread.Add(knot.nodes[i]);
                        Node curr_node = knot.nodes[i];
                        Node prev_node = knot.center_node;
                        while (true)
                        {
                            Node next_node = curr_node.getNeighbours().Find(x => !(x.is_processed));
                            if (next_node == null)
                            {
                                curr_node.is_processed = true;
                                if (curr_node.getNeighbours().Count > 1)
                                    protothread.Add(curr_node.getNeighbours().Find(x => x.getNeighbours().Count > 2 && x != prev_node));

                                break;
                            }
                            protothread.Add(next_node);
                            curr_node.is_processed = true;
                            prev_node = curr_node;
                            curr_node = next_node;
                        }

                        VascularThread thread = new VascularThread();
                        protothread.Reverse();
                        thread.nodes = protothread.ToArray();
                        int L = thread.nodes.GetLength(0);
                        thread.velocity = new double[L];
                        thread.pressure = new double[L];
                        thread.lumen_sq = new double[L];
                        thread.flux = new double[L];
                        thread.dir_vector = new Vector3[L];
                        for (int j = 0; j < L; j++)
                        {
                            thread.velocity[j] = 0;
                            thread.flux[j] = 0;
                            thread.pressure[j] = Node.diastolic_pressure;
                            thread.lumen_sq[j] = thread.nodes[j].getLumen_sq0();
                            if (j < L - 1 && j > 0)
                            {
                                thread.dir_vector[j] = thread.nodes[j + 1].Position - thread.nodes[j - 1].Position;
                                thread.dir_vector[j].Normalize();
                            }
                        }

                        if (L > 1)
                        {
                            thread.dir_vector[0] = thread.nodes[1].Position - thread.nodes[0].Position;
                            thread.dir_vector[0].Normalize();

                            thread.dir_vector[L - 1] = thread.nodes[L - 1].Position - thread.nodes[L - 2].Position;
                            thread.dir_vector[L - 1].Normalize();
                        }
                        else
                            continue;

                        threads.Add(thread);
                    }
                }
            else
                foreach (var n in inlet)
                {
                    List<Node> protothread = new List<Node>();
                    protothread.Add(n);
                    protothread.Add(n.getNeighbours().Last());
                    while (protothread.Last().getNeighbours().Count == 2)
                    {
                        protothread.Add(protothread.Last().getNeighbours().Find(x => x != protothread[protothread.Count - 2]));
                        if (protothread.Last().getNeighbours().Count > 2)
                            protothread.Remove(protothread.Last());
                    }

                    VascularThread thread = new VascularThread();
                    //protothread.Reverse();
                    thread.nodes = protothread.ToArray();
                    int L = thread.nodes.GetLength(0);
                    thread.velocity = new double[L];
                    thread.pressure = new double[L];
                    thread.lumen_sq = new double[L];
                    thread.flux = new double[L];
                    thread.dir_vector = new Vector3[L];
                    for (int j = 0; j < L; j++)
                    {
                        thread.velocity[j] = 0;
                        thread.flux[j] = 0;
                        thread.pressure[j] = Node.diastolic_pressure;
                        thread.lumen_sq[j] = thread.nodes[j].getLumen_sq0();
                        if (j < L - 1 && j > 0)
                        {
                            thread.dir_vector[j] = thread.nodes[j + 1].Position - thread.nodes[j - 1].Position;
                            thread.dir_vector[j].Normalize();
                        }
                    }
                    thread.dir_vector[0] = thread.nodes[1].Position - thread.nodes[0].Position;
                    thread.dir_vector[0].Normalize();

                    thread.dir_vector[L - 1] = thread.nodes[L - 1].Position - thread.nodes[L - 2].Position;
                    thread.dir_vector[L - 1].Normalize();

                    threads.Add(thread);
                }

            foreach (var t in threads)
            {
                int N = t.nodes.GetLength(0);
                if (N > 1)
                {
                    t.nodes[N - 1].lumen_sq_0 = t.nodes[N - 2].getLumen_sq0();
                    t.nodes[0].lumen_sq_0 = t.nodes[1].getLumen_sq0();
                }
            }
        }
        */

        /*
        public void setBounds(List<Node> _inlet, List<Node> _outlet)
        {
            inlet = new List<Node>(_inlet);
            outlet = new List<Node>(_outlet);
        }
        */

        /*

        public List<VascularThread> getThreads()
        {
            return threads;
        }

         */

        public bool UpdatePreallocatedVascularSystem(int max_preallocated_size)
        {
            int max_node_id = getMaxId();
            if (max_node_id >= max_preallocated_size)
                return false;

            preallocated_vascular_system = new Node[max_node_id + 1];
            foreach (Node n in vascular_system)
            {
                preallocated_vascular_system[n.getId()] = n;
            }
            return true;
        }

        public int NodesCount
        {
            get
            {
                return vascular_system.Count;
            }
        }

        public List<Node> Nodes
        {
            get
            {
                return vascular_system;
            }
        }

        public List<Node> NodesCopy
        {
            get
            {
                return vascular_system.GetRange(0, vascular_system.Count);
            }
        }

        public Vector3 getCenter()
        {
            Vector3 pos_of_center = Vector3.Zero;
            int count = vascular_system.Count;

            foreach (Node node in vascular_system)
            {
                pos_of_center += node.Position;
            }

            pos_of_center = pos_of_center / count;
            
            return pos_of_center;
        }

        public Vector3 getSelCenter()
        {
            Vector3 pos_of_center = Vector3.Zero;
            int count = 0;

            foreach (Node node in vascular_system)
            {
                if (node.TailSelectionFlag)
                {
                    pos_of_center += node.Position;
                    count++;
                }
            }

            if (count > 0)
                pos_of_center = pos_of_center / count;

            return pos_of_center;
        }

        public void moveAllNodes(Vector3 shift)
        {
            foreach (Node node in vascular_system)
            {
                node.Position = node.Position + shift;
            }
        }
        public void moveSelectedNodes(Vector3 shift)
        {
            foreach (Node node in vascular_system)
            {
                if (node.TailSelectionFlag)
                    node.Position = node.Position + shift;
            }
        }

        public void mirrorSelectedNodesOYZ(float atX)
        {
            Vector3 newPos;
            foreach (Node node in vascular_system)
            {
                if (node.TailSelectionFlag)
                {
                    newPos.X = 2.0f * atX - node.Position.X;
                    newPos.Y = node.Position.Y;
                    newPos.Z = node.Position.Z;

                    node.Position = newPos;
                }
            }
        }

        public void reindexNodes()
        {
            for (int i = 0; i < vascular_system.Count; i++)
            {
                vascular_system[i].setId(i);
            }
        }

        public void moveNode(int id, int pos)
        {
            int nodeIndex = vascular_system.FindIndex(x => x.getId() == id);
            Node tmp = vascular_system[pos];
            vascular_system[pos] = vascular_system[nodeIndex];
            vascular_system[nodeIndex] = tmp;
        }

        public void clearMappingData()
        {

            for (int i = 0; i < vascular_system.Count; i++)
            {
                vascular_system[i].MappedVertices.Clear();
            }
        }

        public Node[] PreallocatedVascularSystem
        {
            get
            {
                return preallocated_vascular_system;
            }
            set
            {
                preallocated_vascular_system = value;
            }
        }

        private string name;
        private List<Node> vascular_system;
        Node[] preallocated_vascular_system;
        //SortedSet<RefLengthDescription> rl;
        //SortedList<int, RefLengthDescription> ll;
        //List<RefLengthDescription> reference_lengths;

        //private List<Node> inlet;
        //private List<Node> outlet;

        //private List<VascularThread> threads;
        //private List<Knot> knots;

        public void RemoveShortTerminals(float res)
        {
            List<Core3Node> core3 = buildCore3NodesList();
            List<VascularThread> threadsToRemove = new List<VascularThread>();
            foreach (Core3Node c in core3)
            {
                foreach (Node n in c.neighbours)
                {
                    VascularThread th = getThread(c.bifurcationNode, n);
                    if (th.nodes.Last().getNeighbours().Count == 1)
                    {
                        // Terminal
                        if (th.getLength() < res)
                        {
                            // short
                            threadsToRemove.Add(th);
                        }
                    }
                }
            }
            foreach (var th in threadsToRemove)
            {
                th.nodes[0].getNeighbours().Remove(th.nodes[1]);
                for (int i = 1; i < th.nodes.Length; i++)
                    vascular_system.Remove(th.nodes[i]);
            }
        }
    };

    public class ThreadDescriptor
    {
        public int id0;
        public int id1;
        public int idNdec2;
        public int idNdec1;

        public ThreadDescriptor(VascularThread thread)
        {
            int _id0 = thread.nodes[0].getId();
            int _id1 = thread.nodes[1].getId();
            int _idNdec2 = thread.nodes[thread.nodes.Length - 2].getId();
            int _idNdec1 = thread.nodes[thread.nodes.Length - 1].getId();
            bool direct = true;
            if (_id0 != _idNdec1)
            {
                direct = (_id0 < _idNdec1);
            }
            else
            {
                direct = (_id1 <= _idNdec2);
            }

            if (direct)
            {
                // Direct order of nodes.
                id0 = _id0;
                id1 = _id1;
                idNdec2 = _idNdec2;
                idNdec1 = _idNdec1;
            }
            else
            {
                // Reversed order of nodes.
                id0 = _idNdec1;
                id1 = _idNdec2;
                idNdec2 = _id1; 
                idNdec1 = _id0;
            }
        }

        public override bool Equals(object obj)
        {
            if (!(obj is ThreadDescriptor))
                return false;
            ThreadDescriptor tObj = (ThreadDescriptor)obj;
            if (tObj.id0 != id0)
                return false;
            if (tObj.id1 != id1)
                return false;
            if (tObj.idNdec2 != idNdec2)
                return false;
            if (tObj.idNdec1 != idNdec1)
                return false;
            return true;
        }

        public override int GetHashCode()
        {
            return id0.GetHashCode() ^ id1.GetHashCode() ^ idNdec2.GetHashCode() ^ idNdec1.GetHashCode();
        }
    }

    public class ThreadDescriptorComparer : Comparer<ThreadDescriptor>
    {
        public override int Compare(ThreadDescriptor x, ThreadDescriptor y)
        {
            if (x.id0 != y.id0)
            {
                return (x.id0.CompareTo(y.id0));
            }
            if (x.id1 != y.id1)
            {
                return (x.id1.CompareTo(y.id1));
            }
            if (x.idNdec2 != y.idNdec2)
            {
                return (x.idNdec2.CompareTo(y.idNdec2));
            }
            return (x.idNdec1.CompareTo(y.idNdec1));
        }
    }

    
    //public struct RefLengthDescription
    //{
    //    // Source < Target!
    //    public int sourceNodeId;
    //    public int targetNodeId;
    //    public double length;

    //    public RefLengthDescription(int _sourceNodeId, int _targetNodeId, double _length)
    //    {
    //        sourceNodeId = _sourceNodeId;
    //        targetNodeId = _targetNodeId;
    //        length = _length;
    //    }

    //    public override bool Equals(object obj)
    //    {
    //        if (!(obj is RefLengthDescription))
    //            return false;
    //        RefLengthDescription o = (RefLengthDescription)obj;
    //        if (o.sourceNodeId != sourceNodeId)
    //            return false;
    //        if (o.targetNodeId != targetNodeId)
    //            return false;
    //        return true;
    //    }

    //    public override int GetHashCode()
    //    {
    //        return targetNodeId.GetHashCode() ^ sourceNodeId.GetHashCode();
    //    }
    //}

    public class Core3Node
    {
        public Node bifurcationNode;
        public List<Node> neighbours;
        public Dictionary<int,bool> threadsProcessed;

        public Core3Node(Node _bifurcationNode)
        {
            bifurcationNode = _bifurcationNode;
            neighbours = bifurcationNode.getNeighbours();
            threadsProcessed = new Dictionary<int,bool>();
            foreach (Node n in neighbours)
            {
                threadsProcessed.Add(n.getId(), false);
            }
        }

        public void AddNeighbour(Node n)
        {
            neighbours.Add(n);
            if (!threadsProcessed.ContainsKey(n.getId()))
                threadsProcessed.Add(n.getId(), false);
        }

        public void RemoveNeighbour(Node n)
        {
            neighbours.Remove(n);
            threadsProcessed.Remove(n.getId());
        }
    }
}
