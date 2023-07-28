using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BloodFlow
{    
    public delegate double getFloatValueDelegate(MetricPoint node);
    public delegate void setFloatValueDelegate(MetricPoint node, double value);

    public delegate void setBoolValueDelegate(MetricPoint node, bool value);
    public delegate bool getBoolValueDelegate(MetricPoint node);

    public class MetricPoint //General calss for objects with common metrics, used for wide-width search (for keeping distance from origin point)
    {
        private Dictionary<int, double> f_id2value;
        private Dictionary<int, bool>   b_id2value;
        private static Dictionary<getFloatValueDelegate, int> f_dlg2id = new Dictionary<getFloatValueDelegate, int>();
        private static Dictionary<getBoolValueDelegate, int> b_dlg2id = new Dictionary<getBoolValueDelegate, int>();
        private static Dictionary<int, List<MetricPoint>> id2nodelist = new Dictionary<int, List<MetricPoint>>();


        public MetricPoint()
        {
            f_id2value = new Dictionary<int, double>();
            b_id2value = new Dictionary<int, bool>();
        }

        public static void newFloatValueLayer(out getFloatValueDelegate get_del, out setFloatValueDelegate set_del)
        {
            Random r = new Random();

            int c = r.Next(int.MaxValue); ;
            while (f_dlg2id.ContainsValue(c))
                c = r.Next(int.MaxValue);

            get_del = delegate(MetricPoint node)
            {
                MetricPoint n = (MetricPoint)node;
                if (n.f_id2value.ContainsKey(c))
                    return n.f_id2value[c];
                else
                {
                    n.f_id2value.Add(c, double.MaxValue);
                    MetricPoint.id2nodelist[c].Add(node);
                }
                return double.MaxValue;
            };

            f_dlg2id.Add(get_del, c);

            set_del = delegate(MetricPoint node, double val)
            {
                MetricPoint n = (MetricPoint)node;
                if (n.f_id2value.ContainsKey(c))
                    n.f_id2value[c] = val;
                else
                {
                    n.f_id2value.Add(c, val);
                    MetricPoint.id2nodelist[c].Add(node);
                }
            };

            id2nodelist.Add(c, new List<MetricPoint>());
        }

        public static void newBoolValueLayer(out getBoolValueDelegate get_del, out setBoolValueDelegate set_del)
        {
            int c = 0;
            while (true)
            {
                Random r = new Random();

                c = r.Next(int.MaxValue); ;
                while (b_dlg2id.ContainsValue(c))
                    c = r.Next(int.MaxValue);
                if (!id2nodelist.ContainsKey(c))
                    break;
            }



            get_del = delegate(MetricPoint node)
            {
                MetricPoint n = (MetricPoint)node;
                if (n.b_id2value.ContainsKey(c))
                    return n.b_id2value[c];
                else
                {
                    n.b_id2value.Add(c, false);
                    MetricPoint.id2nodelist[c].Add(node);
                }
                return false;
            };

            b_dlg2id.Add(get_del, c);

            set_del = delegate(MetricPoint node, bool val)
            {
                MetricPoint n = (MetricPoint)node;
                if (n.b_id2value.ContainsKey(c))
                    n.b_id2value[c] = val;
                else
                {
                    n.b_id2value.Add(c, val);
                    MetricPoint.id2nodelist[c].Add(node);
                }
            };

            id2nodelist.Add(c, new List<MetricPoint>());
        }

        public static void terminateFloatValueLayer(ref getFloatValueDelegate get_del)
        {
            int c = f_dlg2id[get_del];
            foreach (MetricPoint n in id2nodelist[c])
                n.f_id2value.Remove(c);
            id2nodelist.Remove(c);
            f_dlg2id.Remove(get_del);
            get_del = null;
        }

        public static void terminateBoolValueLayer(ref getBoolValueDelegate get_del)
        {
            int c = b_dlg2id[get_del];
            foreach (MetricPoint n in id2nodelist[c])
                n.b_id2value.Remove(c);
            id2nodelist.Remove(c);
            b_dlg2id.Remove(get_del);
            get_del = null;
        }
    }


	public class VascularNode: MetricPoint
    {   

        public VascularNode(int _id, Vector3 _position, double _rad)
        {
            id = _id;
            position = _position;            
            neighbours = new List<VascularNode>();
            lumen_area_0 = (double)Math.PI * _rad * _rad;
            radius = _rad;

            double beta = GlobalDefs.getBoileauBeta(_rad);           
           
            lumen_area = lumen_area_0;
            pressure = GlobalDefs.DIASTOLIC_PRESSURE;
            velocity = 0;
        }

        public void setRad(float _rad)
        {
            lumen_area_0 = (double)Math.PI * _rad * _rad;
            radius = _rad;
            double beta = GlobalDefs.getBoileauBeta(_rad);
            lumen_area = lumen_area_0;
        }

        public virtual void defDirVector()
        {
            if (neighbours.Count < 3)
                dir_vector = neighbours.First().position - position;
            else
                dir_vector = new Vector3(0, 0, 0);
        }

        public int addNeighbours(VascularNode[] _neighbours)
        {
            int L1 = neighbours.Count();
            neighbours.AddRange(_neighbours.ToList());
            neighbours = neighbours.Distinct().ToList<VascularNode>();
            neighbours.RemoveAll(x => x == this);

            return neighbours.Count - L1;
        }

        public int excludeNeighbours(VascularNode[] _neighbours)
        {
            int L1 = neighbours.Count;
            foreach (var n in _neighbours)
                excludeNeighbour(n);

            return L1 - neighbours.Count;
        }

        public void addNeighbour(VascularNode neighbour)
        {
            neighbours.Remove(neighbour);
            neighbours.Add(neighbour);
        }

        public bool excludeNeighbour(VascularNode neighbour)
        {
            return neighbours.Remove(neighbour);
        } 
        
        public double calcVelocityInSectionPoint (VascularNode node, double x_sec, double y_sec) // x_sec is the x coordinate in the inflow section, y_sec is the y coordinate in the inflow section, [x_sec == 0; y_sec == 0] is the center of the section. See (3) of 10.1016/j.procs.2018.08.272  
        {
            double u, r, g_r, zeta, R0;
            R0 = Math.Sqrt(node.lumen_area / Math.PI);
            zeta = (GlobalDefs.FRICTION_C / 2) - 2;
            r = Math.Sqrt(Math.Pow(x_sec, 2) + Math.Pow(y_sec, 2));
            g_r = ((zeta + 2) / 2) * (1 - Math.Pow(r / R0, zeta));
            u = node.velocity * g_r;
            return u;
        }

        public double calcAgent_cInSectionPoint(VascularNode node, double x_sec, double y_sec) // x_sec is the x coordinate in the inflow section, y_sec is the y coordinate in the inflow section, [x_sec == 0; y_sec == 0] is the center of the section. See (6) of 10.1016/j.procs.2018.08.272
        {
            double c, r, R0;
            R0 = Math.Sqrt(node.lumen_area / Math.PI);
            r = Math.Sqrt(Math.Pow((x_sec - node.agent_xbias), 2) + Math.Pow((y_sec - node.agent_ybias), 2));
            c = node.agent_c * (1 + node.agent_shape * (Math.Pow(R0, 2) - 2 * Math.Pow(r, 2)));
            return c;
        }

        public double velocity { get; set; }
        public double lumen_area { get; set; }
        public double pressure { get; set; }
        public double agent_c { get; set; } // Concentration as the ratio of volumes
        public double agent_shape { get; set; }
        public double agent_xbias { get; set; }
        public double agent_ybias { get; set; }

        public int id;
        public Vector3 dir_vector;
        public List<VascularNode> neighbours;
        public Vector3 position;
        public double lumen_area_0; //lumen area
        public double radius;
    };
}