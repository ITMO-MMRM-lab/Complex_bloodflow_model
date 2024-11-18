#define FAST

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;



namespace BloodFlow
{
    public class Knot
    {
        public Knot(VascularNode _core_node, double start_time)
        {
            core_node = _core_node;
            nodes = (VascularNode[])core_node.neighbours.ToArray().Clone();

            int L = nodes.GetLength(0);
            
            velocity = new double[L];
            pressure = new double[L];
            lumen_area = new double[L];
            agent_c = new double[L];

            for (int i = 0; i < core_node.neighbours.Count; i++)
            {               
                lumen_area[i] = core_node.neighbours[i].lumen_area_0;
            }

            DefineSigns();

            current_time = start_time;
            previous_time = current_time - 1e-3;
        }

        public void DefineSigns()
        {
            int L = nodes.GetLength(0);
            Vector3[] dir_vector1 = new Vector3[L];
            Vector3[] dir_vector2 = new Vector3[L];
            Vector3[] dir_vector3 = new Vector3[L];
            Vector3[] dir_vector4 = new Vector3[L];
            v_sign = new int[L];
            v_sign_1 = new int[L];

            for (int i = 0; i < L; i++)
            {
                dir_vector1[i] = core_node.position - nodes[i].position;
                dir_vector2[i] = nodes[i].dir_vector;
                dir_vector3[i] = nodes[i].position - nodes[i].neighbours.Last().position;
                dir_vector4[i] = nodes[i].neighbours.Last().dir_vector;
            }

            for (int i = 0; i < L; i++)
            {
                v_sign[i] = Math.Sign(Vector3.Dot(dir_vector1[i], dir_vector2[i]));
                v_sign_1[i] = Math.Sign(Vector3.Dot(dir_vector3[i], dir_vector4[i]))*v_sign[i];
            }
        }

        public int NaNControl()
        {
            foreach (var n in nodes)
                if (double.IsNaN(n.pressure) || double.IsNaN(n.velocity))
                    return core_node.id;
            return -1;
        }

        public virtual void reset()
        {
            int L = nodes.GetLength(0);
            for (int i = 0; i < L; i++)
            {
                velocity[i] = 0;
                pressure[i] = 0;
                lumen_area[i] = nodes[i].lumen_area_0;
                nodes[i].velocity = velocity[i];
                nodes[i].pressure = pressure[i];
                nodes[i].lumen_area = nodes[i].lumen_area_0;
                nodes[i].agent_c = agent_c[i];
                nodes[i].agent_shape = agent_shape[i];
                nodes[i].agent_xbias = agent_xbias[i];
                nodes[i].agent_ybias = agent_ybias[i];
            } 
        }

        public virtual void doCoupling(double dt) { }

        public virtual void holdChtr(double dt) { }

        public VascularNode[] nodes;
        public VascularNode core_node;


        public int[] v_sign;
        public int[] v_sign_1;
        public double[] velocity;
        public double[] pressure;
        public double[] lumen_area;
        public double[] lumen_area_0;
        public double[] agent_c;
        public double[] agent_shape;
        public double[] agent_xbias;
        public double[] agent_ybias;

        public double current_time;
        public double previous_time;
    }

    public class
           StandartKnot : Knot
    {
        public StandartKnot(Knot _knot, GetBetaFunction getElasticBeta)
            : base(_knot.core_node, _knot.current_time)
        {
            int L = nodes.GetLength(0);
            chrt_func = new MDFunction[L];
            energy_conservation_func = new MDFunction[L - 1];
            funcs = new MDFunction[2 * L];

            wall_thickhess = new double[L];
            lumen_area_0 = new double[L];

            beta_1 = new double[L];
            chrt_b = new double[L];
            chrt_f = new double[L];
            c_dst  = new double[L];
            dep_matrix = new bool[2 * L, 2 * L];
            prev_velocity = new double[L];
            g_energy = new double[L];

            nl_system = new NewtonSolver(2 * L);


            for (int i = 0; i < L; i++)
                for (int j = 0; j < L; j++)
                    dep_matrix[i, j] = false;

            for (int i = 0; i < L; i++)
            {
                double R0 = Math.Sqrt(nodes[i].lumen_area_0 / Math.PI);
                beta_1[i] = getElasticBeta(R0) / nodes[i].lumen_area_0;
                wall_thickhess[i] = GlobalDefs.getBoileauWallThickness(R0);
                lumen_area_0[i] = nodes[i].lumen_area_0;
                prev_velocity[i] = nodes[i].velocity;
                chrt_b[i] = 0;//-(4 * Math.Pow(nodes[i].lumen_area_0, 0.25f) * Math.Sqrt(beta_1[i] / 2.0f /  GlobalDefs.BLOOD_DENSITY));
                chrt_f[i] = 0;// (4 * Math.Pow(nodes[i].lumen_area_0, 0.25f) * Math.Sqrt(beta_1[i] / 2.0f /  GlobalDefs.BLOOD_DENSITY));               
                c_dst[i] = Math.Pow(nodes[i].lumen_area_0, 0.25f) * Math.Sqrt(beta_1[i] / 2.0f / GlobalDefs.BLOOD_DENSITY);
                g_energy[i] = 0;//GlobalDefs.BLOOD_DENSITY * Vector3.Dot(GlobalDefs.DOWN, nodes[i].position - GlobalDefs.ZERO_POINT) * GlobalDefs.GRAVITY;
            }

            for (int i = 0; i < L; i++)
                pressure[i] = GlobalDefs.DIASTOLIC_PRESSURE;



            int count = 0;
            unsafe
            {
                for (int i = 0; i < L; i++)
                {
                    int I = i;
                    MDFunction_del f1_del = delegate(double* args)
                    {
                        double v = args[0 + I * 2];
                        double l = args[1 + I * 2];

                        if (v > 0)
                            return Math.Abs(v) + 4 * (Math.Sqrt(Math.Sqrt(l)) * Math.Sqrt(beta_1[I] / 2.0f / GlobalDefs.BLOOD_DENSITY) - c_dst[I]) - chrt_f[I];
                        else
                            return Math.Abs(v) - 4 * (Math.Sqrt(Math.Sqrt(l)) * Math.Sqrt(beta_1[I] / 2.0f / GlobalDefs.BLOOD_DENSITY) - c_dst[I]) - chrt_b[I];
                    };
                    baseMDFunction f1 = new delegateMDFunc(f1_del);

                    chrt_func[i] = delegate(double[] args) //v1,l1; v2,l2 ...
                    {
                        double v = args[0 + I * 2];
                        double l = args[1 + I * 2];

                        if (v > 0)
                            return Math.Abs(v) + 4 * (Math.Pow(l, 0.25f) * Math.Sqrt(beta_1[I] / 2.0f / GlobalDefs.BLOOD_DENSITY) - c_dst[I]) - chrt_f[I];
                        else
                            return Math.Abs(v) - 4 * (Math.Pow(l, 0.25f) * Math.Sqrt(beta_1[I] / 2.0f / GlobalDefs.BLOOD_DENSITY) - c_dst[I]) - chrt_b[I];
                    };

                    nl_system.addFunc(f1);
                    funcs[count] = chrt_func[i];

                    dep_matrix[count, 2 * I] = true;
                    dep_matrix[count, 2 * I + 1] = true;
                    nl_system.setDetMatrixEl(count, 2 * I, true);
                    nl_system.setDetMatrixEl(count, 2 * I + 1, true);

                    count++;
                }
            }

            unsafe
            {
                MDFunction_del f1_del = delegate(double* args)
                {
                    double summ_flux = 0;
                    for (int i = 0; i < L; i++)
                        summ_flux += args[0 + i * 2] * args[1 + i * 2];

                    return summ_flux;
                };
                baseMDFunction f1 = new delegateMDFunc(f1_del);


                mass_conservation_func = delegate(double[] args)
                {
                    double summ_flux = 0;
                    for (int i = 0; i < L; i++)
                    {
                        double v = args[0 + i * 2];
                        double l = args[1 + i * 2];

                        summ_flux += v * l;
                    }
                    return summ_flux;
                };


                funcs[count] = mass_conservation_func;
                for (int i = 0; i < 2 * L; i++)
                {
                    dep_matrix[count, i] = true;
                    nl_system.setDetMatrixEl(count, i, true);
                }

                nl_system.addFunc(f1);
            };

            count++;

            unsafe
            {
                for (int i = 1; i < L; i++)
                {
                    int I = i;
                    MDFunction_del f1_del = delegate(double* args)
                    {
                        double v0 = args[0];
                        double p0 = beta_1[0] * (Math.Sqrt(args[1]) - Math.Sqrt(nodes[0].lumen_area_0)) + GlobalDefs.DIASTOLIC_PRESSURE;

                        double v = args[0 + I * 2];
                        double p = beta_1[I] * (Math.Sqrt(args[1 + 2 * I]) - Math.Sqrt(nodes[I].lumen_area_0)) + GlobalDefs.DIASTOLIC_PRESSURE;
                        return GlobalDefs.BLOOD_DENSITY * (v0 * v0 - v * v) / 2 + p0 - p + g_energy[0] - g_energy[I];
                    };
                    baseMDFunction f1 = new delegateMDFunc(f1_del);

                    energy_conservation_func[i - 1] = delegate(double[] args)
                    {
                        double v0 = args[0];
                        double p0 = beta_1[0] * (Math.Sqrt(args[1]) - Math.Sqrt(nodes[0].lumen_area_0)) + GlobalDefs.DIASTOLIC_PRESSURE;

                        double v = args[0 + I * 2];
                        double p = beta_1[I] * (Math.Sqrt(args[1 + 2 * I]) - Math.Sqrt(nodes[I].lumen_area_0)) + GlobalDefs.DIASTOLIC_PRESSURE;
                        return GlobalDefs.BLOOD_DENSITY * (v0 * v0 - v * v) / 2 + p0 - p + g_energy[I];
                    };

                    nl_system.addFunc(f1);
                    funcs[count] = energy_conservation_func[I - 1];

                    dep_matrix[count, 0] = true;
                    dep_matrix[count, 1] = true;
                    nl_system.setDetMatrixEl(count, 0, true);
                    nl_system.setDetMatrixEl(count, 1, true);

                    dep_matrix[count, 2 * I] = true;
                    dep_matrix[count, 2 * I + 1] = true;

                    nl_system.setDetMatrixEl(count, 2 * I, true);
                    nl_system.setDetMatrixEl(count, 2 * I + 1, true);


                    count++;
                }
            }

            unsafe
            {
                us_init_X   = (double*)Marshal.AllocHGlobal(2 * L * sizeof(double));
                us_solution = (double*)Marshal.AllocHGlobal(2 * L * sizeof(double));

                for (int i = 0; i < 2 * L; i += 2)
                {
                    us_init_X[i] = nodes[i / 2].velocity * v_sign[i / 2];
                    us_init_X[i + 1] = nodes[i / 2].lumen_area;
                }
            }

            dX = new double[2 * L];
            for (int i = 0; i < 2 * L; i += 2)
            {
                dX[i] = 1e-12f;
                dX[i + 1] = 1e-12f;
                nl_system.setDxVectorEl(i, 1e-12f);
                nl_system.setDxVectorEl(i + 1, 1e-12f);
            }
        }

        public override void reset()
        {
            int L = nodes.GetLength(0);
            for (int i = 0; i < L; i++)
            {
                velocity[i] = 0;
                pressure[i] = GlobalDefs.DIASTOLIC_PRESSURE;
                lumen_area[i] = nodes[i].lumen_area_0;
                nodes[i].velocity = velocity[i];
                nodes[i].pressure = pressure[i];
                nodes[i].lumen_area = nodes[i].lumen_area_0;
                nodes[i].agent_c = agent_c[i];
                nodes[i].agent_shape = agent_shape[i];
                nodes[i].agent_xbias = agent_xbias[i];
                nodes[i].agent_ybias = agent_ybias[i];
            }

            unsafe
            {
                for (int i = 0; i < 2 * L; i += 2)
                {
                    us_init_X[i] = nodes[i / 2].velocity * v_sign[i / 2];
                    us_init_X[i + 1] = nodes[i / 2].lumen_area;
                }
            }
        }


        unsafe public override void doCoupling(double dt)
        {
            current_time = current_time + dt;
            previous_time = current_time;

            int L = nodes.GetLength(0);

            if (core_node.id == 21)
                L = nodes.GetLength(0);
#if SAFE            
            double[] solution = new double[2 * L];
            double[] init_X   = new double[2 * L];     
            for(int i=0; i<2*L; i+=2)
            {
                init_X[i  ] = nodes[i/2].velocity*v_sign[i/2];
                init_X[i+1] = nodes[i/2].lumen_area;
                double wave_speed = 4 * (Math.Pow(nodes[i / 2].lumen_area, 0.25f) * Math.Sqrt(beta_1[i / 2] / 2.0f /  GlobalDefs.BLOOD_DENSITY) - c_dst[i/2]);
                // Comment of nex two rows gives new algo for knot couplong
                chrt_f[i / 2] = Math.Abs(nodes[i / 2].velocity) + 4 * (Math.Pow(nodes[i / 2].lumen_area, 0.25f) * Math.Sqrt(beta_1[i / 2] / 2.0f /  GlobalDefs.BLOOD_DENSITY) - c_dst[i/2]);
                chrt_b[i / 2] = Math.Abs(nodes[i / 2].velocity) - 4 * (Math.Pow(nodes[i / 2].lumen_area, 0.25f) * Math.Sqrt(beta_1[i / 2] / 2.0f /  GlobalDefs.BLOOD_DENSITY) - c_dst[i/2]); 
            }
            
            solution = myMath.MDNewtonSolver(funcs, dep_matrix, init_X, 1e-6, dX);

            for (int i = 0; i < 2 * L; i += 2)
            {
                nodes[i / 2].velocity = solution[i] * v_sign[i / 2];
                nodes[i / 2].lumen_area = solution[i + 1];
                nodes[i / 2].pressure = beta_1[i / 2] * (Math.Sqrt(nodes[i / 2].lumen_area) - Math.Sqrt(nodes[i / 2].lumen_area_0)) + GlobalDefs.DIASTOLIC_PRESSURE;
            }
#endif


#if FAST
            unsafe
            {
                //double* us_init_X   = (double*)Marshal.AllocHGlobal(2* L * sizeof(double));
                //double* us_solution = (double*)Marshal.AllocHGlobal(2* L * sizeof(double));   

                for (int i = 0; i < 2 * L; i += 2)
                {
                    //My initial guess for nonlinear solver, a bit faster

                    us_init_X[i] = 1.5 * nodes[i / 2].velocity * v_sign[i / 2] - 0.5 * us_init_X[i];
                    us_init_X[i + 1] = 1.5 * nodes[i / 2].lumen_area - 0.5 * us_init_X[i + 1];

                    //Common initial guess for nonlinear solver
                    //  us_init_X[i] = nodes[i / 2].velocity * v_sign[i / 2];
                    //  us_init_X[i + 1] = nodes[i / 2].lumen_area;
                    double wave_speed = 4 * (Math.Sqrt(Math.Sqrt(nodes[i / 2].lumen_area)) * Math.Sqrt(beta_1[i / 2] / 2.0f / GlobalDefs.BLOOD_DENSITY) - c_dst[i / 2]);
                    chrt_f[i / 2] = Math.Abs(nodes[i / 2].velocity) + wave_speed;
                    chrt_b[i / 2] = Math.Abs(nodes[i / 2].velocity) - wave_speed;
                }

                nl_system.solve(us_init_X, 1e-7, us_solution);

                double av_pressure = 0;
                double av_flux_in = 0;
                double av_flux_out = 0;
                double av_lumen_in = 0;
                double av_lumen_out = 0;
                int in_Num = 0;
                int out_Num = 0;
                List<int> in_i = new List<int>();
                List<int> out_i = new List<int>();

                for (int i = 0; i < 2 * L; i += 2)
                {

                    nodes[i / 2].velocity = us_solution[i] * v_sign[i / 2];
                    nodes[i / 2].lumen_area = us_solution[i + 1];
                    nodes[i / 2].pressure = beta_1[i / 2] * (Math.Sqrt(nodes[i / 2].lumen_area) - Math.Sqrt(nodes[i / 2].lumen_area_0)) + GlobalDefs.DIASTOLIC_PRESSURE;

                    av_pressure += nodes[i / 2].pressure;
                    if (us_solution[i] >= 0)
                    {
                        av_flux_in += Math.Abs(us_solution[i + 1] * us_solution[i]);
                        av_lumen_in += us_solution[i + 1];
                        in_Num++;
                        in_i.Add(i);
                    }
                    else
                    {
                        av_flux_out += Math.Abs(us_solution[i + 1] * us_solution[i]);
                        av_lumen_out += us_solution[i + 1];
                        out_Num++;
                        out_i.Add(i);
                    }
                }

                //    if ((in_i.Count == 1) && (out_i.Count == 2))
                //  {

                //}

                core_node.pressure = av_pressure / L;
                core_node.lumen_area = av_lumen_in;
                core_node.velocity = av_flux_in / av_lumen_in;
            }

            core_node.velocity = core_node.neighbours.Last().velocity;
            core_node.lumen_area = core_node.neighbours.Last().lumen_area;

#endif
            if (Program.knot_agent_mode == 1)
            // 1D recalculation of agent_c.
            {
                double agent_c_av = 0;  // = (sum volume of agent for delta t)/(sum volume of the mixture for delta t)
                double volume_ag_sum = 0; // Divided on delta t
                double volume_sum = 0; // Divided on delta t
                for (int i = 0; i < L; i++)
                {
                    agent_c[i] = nodes[i].agent_c;
                    volume_ag_sum += agent_c[i] * lumen_area[i] * Math.Abs(nodes[i].velocity);
                    volume_sum += lumen_area[i] * Math.Abs(nodes[i].velocity);
                }
                if (volume_sum == 0)
                {
                    for (int i = 0; i < L; i++)
                    {
                        nodes[i].agent_c = agent_c[i];
                    }
                    core_node.agent_c = 0;
                }
                else
                {
                    agent_c_av = volume_ag_sum / volume_sum;
                    for (int i = 0; i < L; i++)
                    {
                        agent_c[i] = agent_c_av;
                        nodes[i].agent_c = agent_c[i];
                    }
                    core_node.agent_c = agent_c_av;
                }
            }

            if (Program.knot_agent_mode == 3)
            // 3D recalculation of agent_c. ONLY for "one in, two out".
            {
                // Finding inflows and outflows. 
                List<double> area_in = new List<double>();
                List<double> area_out = new List<double>();
                List<double> flux_in = new List<double>();
                List<double> flux_out = new List<double>();
                List<double> flux_out_min = new List<double>();
                List<VascularNode> nodes_in = new List<VascularNode>();
                List<VascularNode> nodes_out = new List<VascularNode>();
                List<VascularNode> nodes_out_min = new List<VascularNode>();
                List<int> index_in = new List<int>();
                List<int> index_out = new List<int>();
                double flux_out_min_val;
                double area_min, area_max, area, area_step, angle, angle_step, delta_area, area_prev, angle_prev, AngleofSpl, phi_1, phi_2, x_1, y_1, x_2, y_2;
                double flux_min, flux_sum, flux_step, velocity_min, velocity_sum, velocity_sum_step, velocity_sum_max, velocity_in_min_part, velocity_in_max_part;
                double velocity_dir;
                double dx, dy;
                double velocity_in_p;  // Velocity in the point of the section.
                for (int i = 0; i < L; i++)
                {
                    velocity_dir = nodes[i].velocity / v_sign[i];
                    if (velocity_dir > 0)
                    {
                        area_in.Add(nodes[i].lumen_area);
                        flux_in.Add(Math.Abs(nodes[i].velocity * nodes[i].lumen_area));
                        nodes_in.Add(nodes[i]);
                        index_in.Add(i);
                    }
                    if (velocity_dir < 0)
                    {
                        area_out.Add(nodes[i].lumen_area);
                        flux_out.Add(Math.Abs(nodes[i].velocity * nodes[i].lumen_area));
                        nodes_out.Add(nodes[i]);
                        index_out.Add(i);
                    }
                }
                // Calculating ONLY for "one in, two out".
                if ((flux_in.Count == 1) && (flux_out.Count == 2))
                {
                    // Finding points of intersection coordinates. Solutions of the system of three equations.
                    double x0, y0, z0, x1, y1, z1, x2, y2, z2, A0, B0, C0, R0;
                    List<double> PoICoords1 = new List<double>(); // [0] - x, [1] - y, [2] - z.
                    List<double> PoICoords2 = new List<double>(); // [0] - x, [1] - y, [2] - z. 
                    x0 = nodes_in[0].position.x;
                    y0 = nodes_in[0].position.y;
                    z0 = nodes_in[0].position.z;
                    x1 = nodes_out[0].position.x;
                    y1 = nodes_out[0].position.y;
                    z1 = nodes_out[0].position.z;
                    x2 = nodes_out[1].position.x;
                    y2 = nodes_out[1].position.y;
                    z2 = nodes_out[1].position.z;
                    A0 = nodes_in[0].dir_vector.x;
                    B0 = nodes_in[0].dir_vector.y;
                    C0 = nodes_in[0].dir_vector.z;
                    R0 = Math.Sqrt(nodes_in[0].lumen_area / Math.PI);
                    // Formulas for coordinates are from MATLAB.
                    PoICoords1.Add(x0 - R0 * Math.Pow(1 / (Math.Pow(A0, 2) * Math.Pow(x0, 2) * Math.Pow(y1, 2) - 2 * Math.Pow(A0, 2) * Math.Pow(x0, 2) * y1 * y2 + Math.Pow(A0, 2) * Math.Pow(x0, 2) * Math.Pow(y2, 2) + Math.Pow(A0, 2) * Math.Pow(x0, 2) * Math.Pow(z1, 2) - 2 * Math.Pow(A0, 2) * Math.Pow(x0, 2) * z1 * z2 + Math.Pow(A0, 2) * Math.Pow(x0, 2) * Math.Pow(z2, 2) - 2 * Math.Pow(A0, 2) * x0 * x1 * y0 * y1 + 2 * Math.Pow(A0, 2) * x0 * x1 * y0 * y2 + 2 * Math.Pow(A0, 2) * x0 * x1 * y1 * y2 - 2 * Math.Pow(A0, 2) * x0 * x1 * Math.Pow(y2, 2) - 2 * Math.Pow(A0, 2) * x0 * x1 * z0 * z1 + 2 * Math.Pow(A0, 2) * x0 * x1 * z0 * z2 + 2 * Math.Pow(A0, 2) * x0 * x1 * z1 * z2 - 2 * Math.Pow(A0, 2) * x0 * x1 * Math.Pow(z2, 2) + 2 * Math.Pow(A0, 2) * x0 * x2 * y0 * y1 - 2 * Math.Pow(A0, 2) * x0 * x2 * y0 * y2 - 2 * Math.Pow(A0, 2) * x0 * x2 * Math.Pow(y1, 2) + 2 * Math.Pow(A0, 2) * x0 * x2 * y1 * y2 + 2 * Math.Pow(A0, 2) * x0 * x2 * z0 * z1 - 2 * Math.Pow(A0, 2) * x0 * x2 * z0 * z2 - 2 * Math.Pow(A0, 2) * x0 * x2 * Math.Pow(z1, 2) + 2 * Math.Pow(A0, 2) * x0 * x2 * z1 * z2 + Math.Pow(A0, 2) * Math.Pow(x1, 2) * Math.Pow(y0, 2) - 2 * Math.Pow(A0, 2) * Math.Pow(x1, 2) * y0 * y2 + Math.Pow(A0, 2) * Math.Pow(x1, 2) * Math.Pow(y2, 2) + Math.Pow(A0, 2) * Math.Pow(x1, 2) * Math.Pow(z0, 2) - 2 * Math.Pow(A0, 2) * Math.Pow(x1, 2) * z0 * z2 + Math.Pow(A0, 2) * Math.Pow(x1, 2) * Math.Pow(z2, 2) - 2 * Math.Pow(A0, 2) * x1 * x2 * Math.Pow(y0, 2) + 2 * Math.Pow(A0, 2) * x1 * x2 * y0 * y1 + 2 * Math.Pow(A0, 2) * x1 * x2 * y0 * y2 - 2 * Math.Pow(A0, 2) * x1 * x2 * y1 * y2 - 2 * Math.Pow(A0, 2) * x1 * x2 * Math.Pow(z0, 2) + 2 * Math.Pow(A0, 2) * x1 * x2 * z0 * z1 + 2 * Math.Pow(A0, 2) * x1 * x2 * z0 * z2 - 2 * Math.Pow(A0, 2) * x1 * x2 * z1 * z2 + Math.Pow(A0, 2) * Math.Pow(x2, 2) * Math.Pow(y0, 2) - 2 * Math.Pow(A0, 2) * Math.Pow(x2, 2) * y0 * y1 + Math.Pow(A0, 2) * Math.Pow(x2, 2) * Math.Pow(y1, 2) + Math.Pow(A0, 2) * Math.Pow(x2, 2) * Math.Pow(z0, 2) - 2 * Math.Pow(A0, 2) * Math.Pow(x2, 2) * z0 * z1 + Math.Pow(A0, 2) * Math.Pow(x2, 2) * Math.Pow(z1, 2) + 2 * A0 * B0 * x0 * y0 * Math.Pow(z1, 2) - 4 * A0 * B0 * x0 * y0 * z1 * z2 + 2 * A0 * B0 * x0 * y0 * Math.Pow(z2, 2) - 2 * A0 * B0 * x0 * y1 * z0 * z1 + 2 * A0 * B0 * x0 * y1 * z0 * z2 + 2 * A0 * B0 * x0 * y1 * z1 * z2 - 2 * A0 * B0 * x0 * y1 * Math.Pow(z2, 2) + 2 * A0 * B0 * x0 * y2 * z0 * z1 - 2 * A0 * B0 * x0 * y2 * z0 * z2 - 2 * A0 * B0 * x0 * y2 * Math.Pow(z1, 2) + 2 * A0 * B0 * x0 * y2 * z1 * z2 - 2 * A0 * B0 * x1 * y0 * z0 * z1 + 2 * A0 * B0 * x1 * y0 * z0 * z2 + 2 * A0 * B0 * x1 * y0 * z1 * z2 - 2 * A0 * B0 * x1 * y0 * Math.Pow(z2, 2) + 2 * A0 * B0 * x1 * y1 * Math.Pow(z0, 2) - 4 * A0 * B0 * x1 * y1 * z0 * z2 + 2 * A0 * B0 * x1 * y1 * Math.Pow(z2, 2) - 2 * A0 * B0 * x1 * y2 * Math.Pow(z0, 2) + 2 * A0 * B0 * x1 * y2 * z0 * z1 + 2 * A0 * B0 * x1 * y2 * z0 * z2 - 2 * A0 * B0 * x1 * y2 * z1 * z2 + 2 * A0 * B0 * x2 * y0 * z0 * z1 - 2 * A0 * B0 * x2 * y0 * z0 * z2 - 2 * A0 * B0 * x2 * y0 * Math.Pow(z1, 2) + 2 * A0 * B0 * x2 * y0 * z1 * z2 - 2 * A0 * B0 * x2 * y1 * Math.Pow(z0, 2) + 2 * A0 * B0 * x2 * y1 * z0 * z1 + 2 * A0 * B0 * x2 * y1 * z0 * z2 - 2 * A0 * B0 * x2 * y1 * z1 * z2 + 2 * A0 * B0 * x2 * y2 * Math.Pow(z0, 2) - 4 * A0 * B0 * x2 * y2 * z0 * z1 + 2 * A0 * B0 * x2 * y2 * Math.Pow(z1, 2) - 2 * A0 * C0 * x0 * y0 * y1 * z1 + 2 * A0 * C0 * x0 * y0 * y1 * z2 + 2 * A0 * C0 * x0 * y0 * y2 * z1 - 2 * A0 * C0 * x0 * y0 * y2 * z2 + 2 * A0 * C0 * x0 * Math.Pow(y1, 2) * z0 - 2 * A0 * C0 * x0 * Math.Pow(y1, 2) * z2 - 4 * A0 * C0 * x0 * y1 * y2 * z0 + 2 * A0 * C0 * x0 * y1 * y2 * z1 + 2 * A0 * C0 * x0 * y1 * y2 * z2 + 2 * A0 * C0 * x0 * Math.Pow(y2, 2) * z0 - 2 * A0 * C0 * x0 * Math.Pow(y2, 2) * z1 + 2 * A0 * C0 * x1 * Math.Pow(y0, 2) * z1 - 2 * A0 * C0 * x1 * Math.Pow(y0, 2) * z2 - 2 * A0 * C0 * x1 * y0 * y1 * z0 + 2 * A0 * C0 * x1 * y0 * y1 * z2 + 2 * A0 * C0 * x1 * y0 * y2 * z0 - 4 * A0 * C0 * x1 * y0 * y2 * z1 + 2 * A0 * C0 * x1 * y0 * y2 * z2 + 2 * A0 * C0 * x1 * y1 * y2 * z0 - 2 * A0 * C0 * x1 * y1 * y2 * z2 - 2 * A0 * C0 * x1 * Math.Pow(y2, 2) * z0 + 2 * A0 * C0 * x1 * Math.Pow(y2, 2) * z1 - 2 * A0 * C0 * x2 * Math.Pow(y0, 2) * z1 + 2 * A0 * C0 * x2 * Math.Pow(y0, 2) * z2 + 2 * A0 * C0 * x2 * y0 * y1 * z0 + 2 * A0 * C0 * x2 * y0 * y1 * z1 - 4 * A0 * C0 * x2 * y0 * y1 * z2 - 2 * A0 * C0 * x2 * y0 * y2 * z0 + 2 * A0 * C0 * x2 * y0 * y2 * z1 - 2 * A0 * C0 * x2 * Math.Pow(y1, 2) * z0 + 2 * A0 * C0 * x2 * Math.Pow(y1, 2) * z2 + 2 * A0 * C0 * x2 * y1 * y2 * z0 - 2 * A0 * C0 * x2 * y1 * y2 * z1 + Math.Pow(B0, 2) * Math.Pow(x0, 2) * Math.Pow(y1, 2) - 2 * Math.Pow(B0, 2) * Math.Pow(x0, 2) * y1 * y2 + Math.Pow(B0, 2) * Math.Pow(x0, 2) * Math.Pow(y2, 2) - 2 * Math.Pow(B0, 2) * x0 * x1 * y0 * y1 + 2 * Math.Pow(B0, 2) * x0 * x1 * y0 * y2 + 2 * Math.Pow(B0, 2) * x0 * x1 * y1 * y2 - 2 * Math.Pow(B0, 2) * x0 * x1 * Math.Pow(y2, 2) + 2 * Math.Pow(B0, 2) * x0 * x2 * y0 * y1 - 2 * Math.Pow(B0, 2) * x0 * x2 * y0 * y2 - 2 * Math.Pow(B0, 2) * x0 * x2 * Math.Pow(y1, 2) + 2 * Math.Pow(B0, 2) * x0 * x2 * y1 * y2 + Math.Pow(B0, 2) * Math.Pow(x1, 2) * Math.Pow(y0, 2) - 2 * Math.Pow(B0, 2) * Math.Pow(x1, 2) * y0 * y2 + Math.Pow(B0, 2) * Math.Pow(x1, 2) * Math.Pow(y2, 2) - 2 * Math.Pow(B0, 2) * x1 * x2 * Math.Pow(y0, 2) + 2 * Math.Pow(B0, 2) * x1 * x2 * y0 * y1 + 2 * Math.Pow(B0, 2) * x1 * x2 * y0 * y2 - 2 * Math.Pow(B0, 2) * x1 * x2 * y1 * y2 + Math.Pow(B0, 2) * Math.Pow(x2, 2) * Math.Pow(y0, 2) - 2 * Math.Pow(B0, 2) * Math.Pow(x2, 2) * y0 * y1 + Math.Pow(B0, 2) * Math.Pow(x2, 2) * Math.Pow(y1, 2) + Math.Pow(B0, 2) * Math.Pow(y0, 2) * Math.Pow(z1, 2) - 2 * Math.Pow(B0, 2) * Math.Pow(y0, 2) * z1 * z2 + Math.Pow(B0, 2) * Math.Pow(y0, 2) * Math.Pow(z2, 2) - 2 * Math.Pow(B0, 2) * y0 * y1 * z0 * z1 + 2 * Math.Pow(B0, 2) * y0 * y1 * z0 * z2 + 2 * Math.Pow(B0, 2) * y0 * y1 * z1 * z2 - 2 * Math.Pow(B0, 2) * y0 * y1 * Math.Pow(z2, 2) + 2 * Math.Pow(B0, 2) * y0 * y2 * z0 * z1 - 2 * Math.Pow(B0, 2) * y0 * y2 * z0 * z2 - 2 * Math.Pow(B0, 2) * y0 * y2 * Math.Pow(z1, 2) + 2 * Math.Pow(B0, 2) * y0 * y2 * z1 * z2 + Math.Pow(B0, 2) * Math.Pow(y1, 2) * Math.Pow(z0, 2) - 2 * Math.Pow(B0, 2) * Math.Pow(y1, 2) * z0 * z2 + Math.Pow(B0, 2) * Math.Pow(y1, 2) * Math.Pow(z2, 2) - 2 * Math.Pow(B0, 2) * y1 * y2 * Math.Pow(z0, 2) + 2 * Math.Pow(B0, 2) * y1 * y2 * z0 * z1 + 2 * Math.Pow(B0, 2) * y1 * y2 * z0 * z2 - 2 * Math.Pow(B0, 2) * y1 * y2 * z1 * z2 + Math.Pow(B0, 2) * Math.Pow(y2, 2) * Math.Pow(z0, 2) - 2 * Math.Pow(B0, 2) * Math.Pow(y2, 2) * z0 * z1 + Math.Pow(B0, 2) * Math.Pow(y2, 2) * Math.Pow(z1, 2) + 2 * B0 * C0 * Math.Pow(x0, 2) * y1 * z1 - 2 * B0 * C0 * Math.Pow(x0, 2) * y1 * z2 - 2 * B0 * C0 * Math.Pow(x0, 2) * y2 * z1 + 2 * B0 * C0 * Math.Pow(x0, 2) * y2 * z2 - 2 * B0 * C0 * x0 * x1 * y0 * z1 + 2 * B0 * C0 * x0 * x1 * y0 * z2 - 2 * B0 * C0 * x0 * x1 * y1 * z0 + 2 * B0 * C0 * x0 * x1 * y1 * z2 + 2 * B0 * C0 * x0 * x1 * y2 * z0 + 2 * B0 * C0 * x0 * x1 * y2 * z1 - 4 * B0 * C0 * x0 * x1 * y2 * z2 + 2 * B0 * C0 * x0 * x2 * y0 * z1 - 2 * B0 * C0 * x0 * x2 * y0 * z2 + 2 * B0 * C0 * x0 * x2 * y1 * z0 - 4 * B0 * C0 * x0 * x2 * y1 * z1 + 2 * B0 * C0 * x0 * x2 * y1 * z2 - 2 * B0 * C0 * x0 * x2 * y2 * z0 + 2 * B0 * C0 * x0 * x2 * y2 * z1 + 2 * B0 * C0 * Math.Pow(x1, 2) * y0 * z0 - 2 * B0 * C0 * Math.Pow(x1, 2) * y0 * z2 - 2 * B0 * C0 * Math.Pow(x1, 2) * y2 * z0 + 2 * B0 * C0 * Math.Pow(x1, 2) * y2 * z2 - 4 * B0 * C0 * x1 * x2 * y0 * z0 + 2 * B0 * C0 * x1 * x2 * y0 * z1 + 2 * B0 * C0 * x1 * x2 * y0 * z2 + 2 * B0 * C0 * x1 * x2 * y1 * z0 - 2 * B0 * C0 * x1 * x2 * y1 * z2 + 2 * B0 * C0 * x1 * x2 * y2 * z0 - 2 * B0 * C0 * x1 * x2 * y2 * z1 + 2 * B0 * C0 * Math.Pow(x2, 2) * y0 * z0 - 2 * B0 * C0 * Math.Pow(x2, 2) * y0 * z1 - 2 * B0 * C0 * Math.Pow(x2, 2) * y1 * z0 + 2 * B0 * C0 * Math.Pow(x2, 2) * y1 * z1 + Math.Pow(C0, 2) * Math.Pow(x0, 2) * Math.Pow(z1, 2) - 2 * Math.Pow(C0, 2) * Math.Pow(x0, 2) * z1 * z2 + Math.Pow(C0, 2) * Math.Pow(x0, 2) * Math.Pow(z2, 2) - 2 * Math.Pow(C0, 2) * x0 * x1 * z0 * z1 + 2 * Math.Pow(C0, 2) * x0 * x1 * z0 * z2 + 2 * Math.Pow(C0, 2) * x0 * x1 * z1 * z2 - 2 * Math.Pow(C0, 2) * x0 * x1 * Math.Pow(z2, 2) + 2 * Math.Pow(C0, 2) * x0 * x2 * z0 * z1 - 2 * Math.Pow(C0, 2) * x0 * x2 * z0 * z2 - 2 * Math.Pow(C0, 2) * x0 * x2 * Math.Pow(z1, 2) + 2 * Math.Pow(C0, 2) * x0 * x2 * z1 * z2 + Math.Pow(C0, 2) * Math.Pow(x1, 2) * Math.Pow(z0, 2) - 2 * Math.Pow(C0, 2) * Math.Pow(x1, 2) * z0 * z2 + Math.Pow(C0, 2) * Math.Pow(x1, 2) * Math.Pow(z2, 2) - 2 * Math.Pow(C0, 2) * x1 * x2 * Math.Pow(z0, 2) + 2 * Math.Pow(C0, 2) * x1 * x2 * z0 * z1 + 2 * Math.Pow(C0, 2) * x1 * x2 * z0 * z2 - 2 * Math.Pow(C0, 2) * x1 * x2 * z1 * z2 + Math.Pow(C0, 2) * Math.Pow(x2, 2) * Math.Pow(z0, 2) - 2 * Math.Pow(C0, 2) * Math.Pow(x2, 2) * z0 * z1 + Math.Pow(C0, 2) * Math.Pow(x2, 2) * Math.Pow(z1, 2) + Math.Pow(C0, 2) * Math.Pow(y0, 2) * Math.Pow(z1, 2) - 2 * Math.Pow(C0, 2) * Math.Pow(y0, 2) * z1 * z2 + Math.Pow(C0, 2) * Math.Pow(y0, 2) * Math.Pow(z2, 2) - 2 * Math.Pow(C0, 2) * y0 * y1 * z0 * z1 + 2 * Math.Pow(C0, 2) * y0 * y1 * z0 * z2 + 2 * Math.Pow(C0, 2) * y0 * y1 * z1 * z2 - 2 * Math.Pow(C0, 2) * y0 * y1 * Math.Pow(z2, 2) + 2 * Math.Pow(C0, 2) * y0 * y2 * z0 * z1 - 2 * Math.Pow(C0, 2) * y0 * y2 * z0 * z2 - 2 * Math.Pow(C0, 2) * y0 * y2 * Math.Pow(z1, 2) + 2 * Math.Pow(C0, 2) * y0 * y2 * z1 * z2 + Math.Pow(C0, 2) * Math.Pow(y1, 2) * Math.Pow(z0, 2) - 2 * Math.Pow(C0, 2) * Math.Pow(y1, 2) * z0 * z2 + Math.Pow(C0, 2) * Math.Pow(y1, 2) * Math.Pow(z2, 2) - 2 * Math.Pow(C0, 2) * y1 * y2 * Math.Pow(z0, 2) + 2 * Math.Pow(C0, 2) * y1 * y2 * z0 * z1 + 2 * Math.Pow(C0, 2) * y1 * y2 * z0 * z2 - 2 * Math.Pow(C0, 2) * y1 * y2 * z1 * z2 + Math.Pow(C0, 2) * Math.Pow(y2, 2) * Math.Pow(z0, 2) - 2 * Math.Pow(C0, 2) * Math.Pow(y2, 2) * z0 * z1 + Math.Pow(C0, 2) * Math.Pow(y2, 2) * Math.Pow(z1, 2)), 1 / 2) * (B0 * x0 * y1 - B0 * x1 * y0 - B0 * x0 * y2 + B0 * x2 * y0 + B0 * x1 * y2 - B0 * x2 * y1 + C0 * x0 * z1 - C0 * x1 * z0 - C0 * x0 * z2 + C0 * x2 * z0 + C0 * x1 * z2 - C0 * x2 * z1));
                    PoICoords1.Add(y0 + R0 * Math.Pow(1 / (Math.Pow(A0, 2) * Math.Pow(x0, 2) * Math.Pow(y1, 2) - 2 * Math.Pow(A0, 2) * Math.Pow(x0, 2) * y1 * y2 + Math.Pow(A0, 2) * Math.Pow(x0, 2) * Math.Pow(y2, 2) + Math.Pow(A0, 2) * Math.Pow(x0, 2) * Math.Pow(z1, 2) - 2 * Math.Pow(A0, 2) * Math.Pow(x0, 2) * z1 * z2 + Math.Pow(A0, 2) * Math.Pow(x0, 2) * Math.Pow(z2, 2) - 2 * Math.Pow(A0, 2) * x0 * x1 * y0 * y1 + 2 * Math.Pow(A0, 2) * x0 * x1 * y0 * y2 + 2 * Math.Pow(A0, 2) * x0 * x1 * y1 * y2 - 2 * Math.Pow(A0, 2) * x0 * x1 * Math.Pow(y2, 2) - 2 * Math.Pow(A0, 2) * x0 * x1 * z0 * z1 + 2 * Math.Pow(A0, 2) * x0 * x1 * z0 * z2 + 2 * Math.Pow(A0, 2) * x0 * x1 * z1 * z2 - 2 * Math.Pow(A0, 2) * x0 * x1 * Math.Pow(z2, 2) + 2 * Math.Pow(A0, 2) * x0 * x2 * y0 * y1 - 2 * Math.Pow(A0, 2) * x0 * x2 * y0 * y2 - 2 * Math.Pow(A0, 2) * x0 * x2 * Math.Pow(y1, 2) + 2 * Math.Pow(A0, 2) * x0 * x2 * y1 * y2 + 2 * Math.Pow(A0, 2) * x0 * x2 * z0 * z1 - 2 * Math.Pow(A0, 2) * x0 * x2 * z0 * z2 - 2 * Math.Pow(A0, 2) * x0 * x2 * Math.Pow(z1, 2) + 2 * Math.Pow(A0, 2) * x0 * x2 * z1 * z2 + Math.Pow(A0, 2) * Math.Pow(x1, 2) * Math.Pow(y0, 2) - 2 * Math.Pow(A0, 2) * Math.Pow(x1, 2) * y0 * y2 + Math.Pow(A0, 2) * Math.Pow(x1, 2) * Math.Pow(y2, 2) + Math.Pow(A0, 2) * Math.Pow(x1, 2) * Math.Pow(z0, 2) - 2 * Math.Pow(A0, 2) * Math.Pow(x1, 2) * z0 * z2 + Math.Pow(A0, 2) * Math.Pow(x1, 2) * Math.Pow(z2, 2) - 2 * Math.Pow(A0, 2) * x1 * x2 * Math.Pow(y0, 2) + 2 * Math.Pow(A0, 2) * x1 * x2 * y0 * y1 + 2 * Math.Pow(A0, 2) * x1 * x2 * y0 * y2 - 2 * Math.Pow(A0, 2) * x1 * x2 * y1 * y2 - 2 * Math.Pow(A0, 2) * x1 * x2 * Math.Pow(z0, 2) + 2 * Math.Pow(A0, 2) * x1 * x2 * z0 * z1 + 2 * Math.Pow(A0, 2) * x1 * x2 * z0 * z2 - 2 * Math.Pow(A0, 2) * x1 * x2 * z1 * z2 + Math.Pow(A0, 2) * Math.Pow(x2, 2) * Math.Pow(y0, 2) - 2 * Math.Pow(A0, 2) * Math.Pow(x2, 2) * y0 * y1 + Math.Pow(A0, 2) * Math.Pow(x2, 2) * Math.Pow(y1, 2) + Math.Pow(A0, 2) * Math.Pow(x2, 2) * Math.Pow(z0, 2) - 2 * Math.Pow(A0, 2) * Math.Pow(x2, 2) * z0 * z1 + Math.Pow(A0, 2) * Math.Pow(x2, 2) * Math.Pow(z1, 2) + 2 * A0 * B0 * x0 * y0 * Math.Pow(z1, 2) - 4 * A0 * B0 * x0 * y0 * z1 * z2 + 2 * A0 * B0 * x0 * y0 * Math.Pow(z2, 2) - 2 * A0 * B0 * x0 * y1 * z0 * z1 + 2 * A0 * B0 * x0 * y1 * z0 * z2 + 2 * A0 * B0 * x0 * y1 * z1 * z2 - 2 * A0 * B0 * x0 * y1 * Math.Pow(z2, 2) + 2 * A0 * B0 * x0 * y2 * z0 * z1 - 2 * A0 * B0 * x0 * y2 * z0 * z2 - 2 * A0 * B0 * x0 * y2 * Math.Pow(z1, 2) + 2 * A0 * B0 * x0 * y2 * z1 * z2 - 2 * A0 * B0 * x1 * y0 * z0 * z1 + 2 * A0 * B0 * x1 * y0 * z0 * z2 + 2 * A0 * B0 * x1 * y0 * z1 * z2 - 2 * A0 * B0 * x1 * y0 * Math.Pow(z2, 2) + 2 * A0 * B0 * x1 * y1 * Math.Pow(z0, 2) - 4 * A0 * B0 * x1 * y1 * z0 * z2 + 2 * A0 * B0 * x1 * y1 * Math.Pow(z2, 2) - 2 * A0 * B0 * x1 * y2 * Math.Pow(z0, 2) + 2 * A0 * B0 * x1 * y2 * z0 * z1 + 2 * A0 * B0 * x1 * y2 * z0 * z2 - 2 * A0 * B0 * x1 * y2 * z1 * z2 + 2 * A0 * B0 * x2 * y0 * z0 * z1 - 2 * A0 * B0 * x2 * y0 * z0 * z2 - 2 * A0 * B0 * x2 * y0 * Math.Pow(z1, 2) + 2 * A0 * B0 * x2 * y0 * z1 * z2 - 2 * A0 * B0 * x2 * y1 * Math.Pow(z0, 2) + 2 * A0 * B0 * x2 * y1 * z0 * z1 + 2 * A0 * B0 * x2 * y1 * z0 * z2 - 2 * A0 * B0 * x2 * y1 * z1 * z2 + 2 * A0 * B0 * x2 * y2 * Math.Pow(z0, 2) - 4 * A0 * B0 * x2 * y2 * z0 * z1 + 2 * A0 * B0 * x2 * y2 * Math.Pow(z1, 2) - 2 * A0 * C0 * x0 * y0 * y1 * z1 + 2 * A0 * C0 * x0 * y0 * y1 * z2 + 2 * A0 * C0 * x0 * y0 * y2 * z1 - 2 * A0 * C0 * x0 * y0 * y2 * z2 + 2 * A0 * C0 * x0 * Math.Pow(y1, 2) * z0 - 2 * A0 * C0 * x0 * Math.Pow(y1, 2) * z2 - 4 * A0 * C0 * x0 * y1 * y2 * z0 + 2 * A0 * C0 * x0 * y1 * y2 * z1 + 2 * A0 * C0 * x0 * y1 * y2 * z2 + 2 * A0 * C0 * x0 * Math.Pow(y2, 2) * z0 - 2 * A0 * C0 * x0 * Math.Pow(y2, 2) * z1 + 2 * A0 * C0 * x1 * Math.Pow(y0, 2) * z1 - 2 * A0 * C0 * x1 * Math.Pow(y0, 2) * z2 - 2 * A0 * C0 * x1 * y0 * y1 * z0 + 2 * A0 * C0 * x1 * y0 * y1 * z2 + 2 * A0 * C0 * x1 * y0 * y2 * z0 - 4 * A0 * C0 * x1 * y0 * y2 * z1 + 2 * A0 * C0 * x1 * y0 * y2 * z2 + 2 * A0 * C0 * x1 * y1 * y2 * z0 - 2 * A0 * C0 * x1 * y1 * y2 * z2 - 2 * A0 * C0 * x1 * Math.Pow(y2, 2) * z0 + 2 * A0 * C0 * x1 * Math.Pow(y2, 2) * z1 - 2 * A0 * C0 * x2 * Math.Pow(y0, 2) * z1 + 2 * A0 * C0 * x2 * Math.Pow(y0, 2) * z2 + 2 * A0 * C0 * x2 * y0 * y1 * z0 + 2 * A0 * C0 * x2 * y0 * y1 * z1 - 4 * A0 * C0 * x2 * y0 * y1 * z2 - 2 * A0 * C0 * x2 * y0 * y2 * z0 + 2 * A0 * C0 * x2 * y0 * y2 * z1 - 2 * A0 * C0 * x2 * Math.Pow(y1, 2) * z0 + 2 * A0 * C0 * x2 * Math.Pow(y1, 2) * z2 + 2 * A0 * C0 * x2 * y1 * y2 * z0 - 2 * A0 * C0 * x2 * y1 * y2 * z1 + Math.Pow(B0, 2) * Math.Pow(x0, 2) * Math.Pow(y1, 2) - 2 * Math.Pow(B0, 2) * Math.Pow(x0, 2) * y1 * y2 + Math.Pow(B0, 2) * Math.Pow(x0, 2) * Math.Pow(y2, 2) - 2 * Math.Pow(B0, 2) * x0 * x1 * y0 * y1 + 2 * Math.Pow(B0, 2) * x0 * x1 * y0 * y2 + 2 * Math.Pow(B0, 2) * x0 * x1 * y1 * y2 - 2 * Math.Pow(B0, 2) * x0 * x1 * Math.Pow(y2, 2) + 2 * Math.Pow(B0, 2) * x0 * x2 * y0 * y1 - 2 * Math.Pow(B0, 2) * x0 * x2 * y0 * y2 - 2 * Math.Pow(B0, 2) * x0 * x2 * Math.Pow(y1, 2) + 2 * Math.Pow(B0, 2) * x0 * x2 * y1 * y2 + Math.Pow(B0, 2) * Math.Pow(x1, 2) * Math.Pow(y0, 2) - 2 * Math.Pow(B0, 2) * Math.Pow(x1, 2) * y0 * y2 + Math.Pow(B0, 2) * Math.Pow(x1, 2) * Math.Pow(y2, 2) - 2 * Math.Pow(B0, 2) * x1 * x2 * Math.Pow(y0, 2) + 2 * Math.Pow(B0, 2) * x1 * x2 * y0 * y1 + 2 * Math.Pow(B0, 2) * x1 * x2 * y0 * y2 - 2 * Math.Pow(B0, 2) * x1 * x2 * y1 * y2 + Math.Pow(B0, 2) * Math.Pow(x2, 2) * Math.Pow(y0, 2) - 2 * Math.Pow(B0, 2) * Math.Pow(x2, 2) * y0 * y1 + Math.Pow(B0, 2) * Math.Pow(x2, 2) * Math.Pow(y1, 2) + Math.Pow(B0, 2) * Math.Pow(y0, 2) * Math.Pow(z1, 2) - 2 * Math.Pow(B0, 2) * Math.Pow(y0, 2) * z1 * z2 + Math.Pow(B0, 2) * Math.Pow(y0, 2) * Math.Pow(z2, 2) - 2 * Math.Pow(B0, 2) * y0 * y1 * z0 * z1 + 2 * Math.Pow(B0, 2) * y0 * y1 * z0 * z2 + 2 * Math.Pow(B0, 2) * y0 * y1 * z1 * z2 - 2 * Math.Pow(B0, 2) * y0 * y1 * Math.Pow(z2, 2) + 2 * Math.Pow(B0, 2) * y0 * y2 * z0 * z1 - 2 * Math.Pow(B0, 2) * y0 * y2 * z0 * z2 - 2 * Math.Pow(B0, 2) * y0 * y2 * Math.Pow(z1, 2) + 2 * Math.Pow(B0, 2) * y0 * y2 * z1 * z2 + Math.Pow(B0, 2) * Math.Pow(y1, 2) * Math.Pow(z0, 2) - 2 * Math.Pow(B0, 2) * Math.Pow(y1, 2) * z0 * z2 + Math.Pow(B0, 2) * Math.Pow(y1, 2) * Math.Pow(z2, 2) - 2 * Math.Pow(B0, 2) * y1 * y2 * Math.Pow(z0, 2) + 2 * Math.Pow(B0, 2) * y1 * y2 * z0 * z1 + 2 * Math.Pow(B0, 2) * y1 * y2 * z0 * z2 - 2 * Math.Pow(B0, 2) * y1 * y2 * z1 * z2 + Math.Pow(B0, 2) * Math.Pow(y2, 2) * Math.Pow(z0, 2) - 2 * Math.Pow(B0, 2) * Math.Pow(y2, 2) * z0 * z1 + Math.Pow(B0, 2) * Math.Pow(y2, 2) * Math.Pow(z1, 2) + 2 * B0 * C0 * Math.Pow(x0, 2) * y1 * z1 - 2 * B0 * C0 * Math.Pow(x0, 2) * y1 * z2 - 2 * B0 * C0 * Math.Pow(x0, 2) * y2 * z1 + 2 * B0 * C0 * Math.Pow(x0, 2) * y2 * z2 - 2 * B0 * C0 * x0 * x1 * y0 * z1 + 2 * B0 * C0 * x0 * x1 * y0 * z2 - 2 * B0 * C0 * x0 * x1 * y1 * z0 + 2 * B0 * C0 * x0 * x1 * y1 * z2 + 2 * B0 * C0 * x0 * x1 * y2 * z0 + 2 * B0 * C0 * x0 * x1 * y2 * z1 - 4 * B0 * C0 * x0 * x1 * y2 * z2 + 2 * B0 * C0 * x0 * x2 * y0 * z1 - 2 * B0 * C0 * x0 * x2 * y0 * z2 + 2 * B0 * C0 * x0 * x2 * y1 * z0 - 4 * B0 * C0 * x0 * x2 * y1 * z1 + 2 * B0 * C0 * x0 * x2 * y1 * z2 - 2 * B0 * C0 * x0 * x2 * y2 * z0 + 2 * B0 * C0 * x0 * x2 * y2 * z1 + 2 * B0 * C0 * Math.Pow(x1, 2) * y0 * z0 - 2 * B0 * C0 * Math.Pow(x1, 2) * y0 * z2 - 2 * B0 * C0 * Math.Pow(x1, 2) * y2 * z0 + 2 * B0 * C0 * Math.Pow(x1, 2) * y2 * z2 - 4 * B0 * C0 * x1 * x2 * y0 * z0 + 2 * B0 * C0 * x1 * x2 * y0 * z1 + 2 * B0 * C0 * x1 * x2 * y0 * z2 + 2 * B0 * C0 * x1 * x2 * y1 * z0 - 2 * B0 * C0 * x1 * x2 * y1 * z2 + 2 * B0 * C0 * x1 * x2 * y2 * z0 - 2 * B0 * C0 * x1 * x2 * y2 * z1 + 2 * B0 * C0 * Math.Pow(x2, 2) * y0 * z0 - 2 * B0 * C0 * Math.Pow(x2, 2) * y0 * z1 - 2 * B0 * C0 * Math.Pow(x2, 2) * y1 * z0 + 2 * B0 * C0 * Math.Pow(x2, 2) * y1 * z1 + Math.Pow(C0, 2) * Math.Pow(x0, 2) * Math.Pow(z1, 2) - 2 * Math.Pow(C0, 2) * Math.Pow(x0, 2) * z1 * z2 + Math.Pow(C0, 2) * Math.Pow(x0, 2) * Math.Pow(z2, 2) - 2 * Math.Pow(C0, 2) * x0 * x1 * z0 * z1 + 2 * Math.Pow(C0, 2) * x0 * x1 * z0 * z2 + 2 * Math.Pow(C0, 2) * x0 * x1 * z1 * z2 - 2 * Math.Pow(C0, 2) * x0 * x1 * Math.Pow(z2, 2) + 2 * Math.Pow(C0, 2) * x0 * x2 * z0 * z1 - 2 * Math.Pow(C0, 2) * x0 * x2 * z0 * z2 - 2 * Math.Pow(C0, 2) * x0 * x2 * Math.Pow(z1, 2) + 2 * Math.Pow(C0, 2) * x0 * x2 * z1 * z2 + Math.Pow(C0, 2) * Math.Pow(x1, 2) * Math.Pow(z0, 2) - 2 * Math.Pow(C0, 2) * Math.Pow(x1, 2) * z0 * z2 + Math.Pow(C0, 2) * Math.Pow(x1, 2) * Math.Pow(z2, 2) - 2 * Math.Pow(C0, 2) * x1 * x2 * Math.Pow(z0, 2) + 2 * Math.Pow(C0, 2) * x1 * x2 * z0 * z1 + 2 * Math.Pow(C0, 2) * x1 * x2 * z0 * z2 - 2 * Math.Pow(C0, 2) * x1 * x2 * z1 * z2 + Math.Pow(C0, 2) * Math.Pow(x2, 2) * Math.Pow(z0, 2) - 2 * Math.Pow(C0, 2) * Math.Pow(x2, 2) * z0 * z1 + Math.Pow(C0, 2) * Math.Pow(x2, 2) * Math.Pow(z1, 2) + Math.Pow(C0, 2) * Math.Pow(y0, 2) * Math.Pow(z1, 2) - 2 * Math.Pow(C0, 2) * Math.Pow(y0, 2) * z1 * z2 + Math.Pow(C0, 2) * Math.Pow(y0, 2) * Math.Pow(z2, 2) - 2 * Math.Pow(C0, 2) * y0 * y1 * z0 * z1 + 2 * Math.Pow(C0, 2) * y0 * y1 * z0 * z2 + 2 * Math.Pow(C0, 2) * y0 * y1 * z1 * z2 - 2 * Math.Pow(C0, 2) * y0 * y1 * Math.Pow(z2, 2) + 2 * Math.Pow(C0, 2) * y0 * y2 * z0 * z1 - 2 * Math.Pow(C0, 2) * y0 * y2 * z0 * z2 - 2 * Math.Pow(C0, 2) * y0 * y2 * Math.Pow(z1, 2) + 2 * Math.Pow(C0, 2) * y0 * y2 * z1 * z2 + Math.Pow(C0, 2) * Math.Pow(y1, 2) * Math.Pow(z0, 2) - 2 * Math.Pow(C0, 2) * Math.Pow(y1, 2) * z0 * z2 + Math.Pow(C0, 2) * Math.Pow(y1, 2) * Math.Pow(z2, 2) - 2 * Math.Pow(C0, 2) * y1 * y2 * Math.Pow(z0, 2) + 2 * Math.Pow(C0, 2) * y1 * y2 * z0 * z1 + 2 * Math.Pow(C0, 2) * y1 * y2 * z0 * z2 - 2 * Math.Pow(C0, 2) * y1 * y2 * z1 * z2 + Math.Pow(C0, 2) * Math.Pow(y2, 2) * Math.Pow(z0, 2) - 2 * Math.Pow(C0, 2) * Math.Pow(y2, 2) * z0 * z1 + Math.Pow(C0, 2) * Math.Pow(y2, 2) * Math.Pow(z1, 2)), 1 / 2) * (A0 * x0 * y1 - A0 * x1 * y0 - A0 * x0 * y2 + A0 * x2 * y0 + A0 * x1 * y2 - A0 * x2 * y1 - C0 * y0 * z1 + C0 * y1 * z0 + C0 * y0 * z2 - C0 * y2 * z0 - C0 * y1 * z2 + C0 * y2 * z1));
                    PoICoords1.Add(z0 + R0 * Math.Pow(1 / (Math.Pow(A0, 2) * Math.Pow(x0, 2) * Math.Pow(y1, 2) - 2 * Math.Pow(A0, 2) * Math.Pow(x0, 2) * y1 * y2 + Math.Pow(A0, 2) * Math.Pow(x0, 2) * Math.Pow(y2, 2) + Math.Pow(A0, 2) * Math.Pow(x0, 2) * Math.Pow(z1, 2) - 2 * Math.Pow(A0, 2) * Math.Pow(x0, 2) * z1 * z2 + Math.Pow(A0, 2) * Math.Pow(x0, 2) * Math.Pow(z2, 2) - 2 * Math.Pow(A0, 2) * x0 * x1 * y0 * y1 + 2 * Math.Pow(A0, 2) * x0 * x1 * y0 * y2 + 2 * Math.Pow(A0, 2) * x0 * x1 * y1 * y2 - 2 * Math.Pow(A0, 2) * x0 * x1 * Math.Pow(y2, 2) - 2 * Math.Pow(A0, 2) * x0 * x1 * z0 * z1 + 2 * Math.Pow(A0, 2) * x0 * x1 * z0 * z2 + 2 * Math.Pow(A0, 2) * x0 * x1 * z1 * z2 - 2 * Math.Pow(A0, 2) * x0 * x1 * Math.Pow(z2, 2) + 2 * Math.Pow(A0, 2) * x0 * x2 * y0 * y1 - 2 * Math.Pow(A0, 2) * x0 * x2 * y0 * y2 - 2 * Math.Pow(A0, 2) * x0 * x2 * Math.Pow(y1, 2) + 2 * Math.Pow(A0, 2) * x0 * x2 * y1 * y2 + 2 * Math.Pow(A0, 2) * x0 * x2 * z0 * z1 - 2 * Math.Pow(A0, 2) * x0 * x2 * z0 * z2 - 2 * Math.Pow(A0, 2) * x0 * x2 * Math.Pow(z1, 2) + 2 * Math.Pow(A0, 2) * x0 * x2 * z1 * z2 + Math.Pow(A0, 2) * Math.Pow(x1, 2) * Math.Pow(y0, 2) - 2 * Math.Pow(A0, 2) * Math.Pow(x1, 2) * y0 * y2 + Math.Pow(A0, 2) * Math.Pow(x1, 2) * Math.Pow(y2, 2) + Math.Pow(A0, 2) * Math.Pow(x1, 2) * Math.Pow(z0, 2) - 2 * Math.Pow(A0, 2) * Math.Pow(x1, 2) * z0 * z2 + Math.Pow(A0, 2) * Math.Pow(x1, 2) * Math.Pow(z2, 2) - 2 * Math.Pow(A0, 2) * x1 * x2 * Math.Pow(y0, 2) + 2 * Math.Pow(A0, 2) * x1 * x2 * y0 * y1 + 2 * Math.Pow(A0, 2) * x1 * x2 * y0 * y2 - 2 * Math.Pow(A0, 2) * x1 * x2 * y1 * y2 - 2 * Math.Pow(A0, 2) * x1 * x2 * Math.Pow(z0, 2) + 2 * Math.Pow(A0, 2) * x1 * x2 * z0 * z1 + 2 * Math.Pow(A0, 2) * x1 * x2 * z0 * z2 - 2 * Math.Pow(A0, 2) * x1 * x2 * z1 * z2 + Math.Pow(A0, 2) * Math.Pow(x2, 2) * Math.Pow(y0, 2) - 2 * Math.Pow(A0, 2) * Math.Pow(x2, 2) * y0 * y1 + Math.Pow(A0, 2) * Math.Pow(x2, 2) * Math.Pow(y1, 2) + Math.Pow(A0, 2) * Math.Pow(x2, 2) * Math.Pow(z0, 2) - 2 * Math.Pow(A0, 2) * Math.Pow(x2, 2) * z0 * z1 + Math.Pow(A0, 2) * Math.Pow(x2, 2) * Math.Pow(z1, 2) + 2 * A0 * B0 * x0 * y0 * Math.Pow(z1, 2) - 4 * A0 * B0 * x0 * y0 * z1 * z2 + 2 * A0 * B0 * x0 * y0 * Math.Pow(z2, 2) - 2 * A0 * B0 * x0 * y1 * z0 * z1 + 2 * A0 * B0 * x0 * y1 * z0 * z2 + 2 * A0 * B0 * x0 * y1 * z1 * z2 - 2 * A0 * B0 * x0 * y1 * Math.Pow(z2, 2) + 2 * A0 * B0 * x0 * y2 * z0 * z1 - 2 * A0 * B0 * x0 * y2 * z0 * z2 - 2 * A0 * B0 * x0 * y2 * Math.Pow(z1, 2) + 2 * A0 * B0 * x0 * y2 * z1 * z2 - 2 * A0 * B0 * x1 * y0 * z0 * z1 + 2 * A0 * B0 * x1 * y0 * z0 * z2 + 2 * A0 * B0 * x1 * y0 * z1 * z2 - 2 * A0 * B0 * x1 * y0 * Math.Pow(z2, 2) + 2 * A0 * B0 * x1 * y1 * Math.Pow(z0, 2) - 4 * A0 * B0 * x1 * y1 * z0 * z2 + 2 * A0 * B0 * x1 * y1 * Math.Pow(z2, 2) - 2 * A0 * B0 * x1 * y2 * Math.Pow(z0, 2) + 2 * A0 * B0 * x1 * y2 * z0 * z1 + 2 * A0 * B0 * x1 * y2 * z0 * z2 - 2 * A0 * B0 * x1 * y2 * z1 * z2 + 2 * A0 * B0 * x2 * y0 * z0 * z1 - 2 * A0 * B0 * x2 * y0 * z0 * z2 - 2 * A0 * B0 * x2 * y0 * Math.Pow(z1, 2) + 2 * A0 * B0 * x2 * y0 * z1 * z2 - 2 * A0 * B0 * x2 * y1 * Math.Pow(z0, 2) + 2 * A0 * B0 * x2 * y1 * z0 * z1 + 2 * A0 * B0 * x2 * y1 * z0 * z2 - 2 * A0 * B0 * x2 * y1 * z1 * z2 + 2 * A0 * B0 * x2 * y2 * Math.Pow(z0, 2) - 4 * A0 * B0 * x2 * y2 * z0 * z1 + 2 * A0 * B0 * x2 * y2 * Math.Pow(z1, 2) - 2 * A0 * C0 * x0 * y0 * y1 * z1 + 2 * A0 * C0 * x0 * y0 * y1 * z2 + 2 * A0 * C0 * x0 * y0 * y2 * z1 - 2 * A0 * C0 * x0 * y0 * y2 * z2 + 2 * A0 * C0 * x0 * Math.Pow(y1, 2) * z0 - 2 * A0 * C0 * x0 * Math.Pow(y1, 2) * z2 - 4 * A0 * C0 * x0 * y1 * y2 * z0 + 2 * A0 * C0 * x0 * y1 * y2 * z1 + 2 * A0 * C0 * x0 * y1 * y2 * z2 + 2 * A0 * C0 * x0 * Math.Pow(y2, 2) * z0 - 2 * A0 * C0 * x0 * Math.Pow(y2, 2) * z1 + 2 * A0 * C0 * x1 * Math.Pow(y0, 2) * z1 - 2 * A0 * C0 * x1 * Math.Pow(y0, 2) * z2 - 2 * A0 * C0 * x1 * y0 * y1 * z0 + 2 * A0 * C0 * x1 * y0 * y1 * z2 + 2 * A0 * C0 * x1 * y0 * y2 * z0 - 4 * A0 * C0 * x1 * y0 * y2 * z1 + 2 * A0 * C0 * x1 * y0 * y2 * z2 + 2 * A0 * C0 * x1 * y1 * y2 * z0 - 2 * A0 * C0 * x1 * y1 * y2 * z2 - 2 * A0 * C0 * x1 * Math.Pow(y2, 2) * z0 + 2 * A0 * C0 * x1 * Math.Pow(y2, 2) * z1 - 2 * A0 * C0 * x2 * Math.Pow(y0, 2) * z1 + 2 * A0 * C0 * x2 * Math.Pow(y0, 2) * z2 + 2 * A0 * C0 * x2 * y0 * y1 * z0 + 2 * A0 * C0 * x2 * y0 * y1 * z1 - 4 * A0 * C0 * x2 * y0 * y1 * z2 - 2 * A0 * C0 * x2 * y0 * y2 * z0 + 2 * A0 * C0 * x2 * y0 * y2 * z1 - 2 * A0 * C0 * x2 * Math.Pow(y1, 2) * z0 + 2 * A0 * C0 * x2 * Math.Pow(y1, 2) * z2 + 2 * A0 * C0 * x2 * y1 * y2 * z0 - 2 * A0 * C0 * x2 * y1 * y2 * z1 + Math.Pow(B0, 2) * Math.Pow(x0, 2) * Math.Pow(y1, 2) - 2 * Math.Pow(B0, 2) * Math.Pow(x0, 2) * y1 * y2 + Math.Pow(B0, 2) * Math.Pow(x0, 2) * Math.Pow(y2, 2) - 2 * Math.Pow(B0, 2) * x0 * x1 * y0 * y1 + 2 * Math.Pow(B0, 2) * x0 * x1 * y0 * y2 + 2 * Math.Pow(B0, 2) * x0 * x1 * y1 * y2 - 2 * Math.Pow(B0, 2) * x0 * x1 * Math.Pow(y2, 2) + 2 * Math.Pow(B0, 2) * x0 * x2 * y0 * y1 - 2 * Math.Pow(B0, 2) * x0 * x2 * y0 * y2 - 2 * Math.Pow(B0, 2) * x0 * x2 * Math.Pow(y1, 2) + 2 * Math.Pow(B0, 2) * x0 * x2 * y1 * y2 + Math.Pow(B0, 2) * Math.Pow(x1, 2) * Math.Pow(y0, 2) - 2 * Math.Pow(B0, 2) * Math.Pow(x1, 2) * y0 * y2 + Math.Pow(B0, 2) * Math.Pow(x1, 2) * Math.Pow(y2, 2) - 2 * Math.Pow(B0, 2) * x1 * x2 * Math.Pow(y0, 2) + 2 * Math.Pow(B0, 2) * x1 * x2 * y0 * y1 + 2 * Math.Pow(B0, 2) * x1 * x2 * y0 * y2 - 2 * Math.Pow(B0, 2) * x1 * x2 * y1 * y2 + Math.Pow(B0, 2) * Math.Pow(x2, 2) * Math.Pow(y0, 2) - 2 * Math.Pow(B0, 2) * Math.Pow(x2, 2) * y0 * y1 + Math.Pow(B0, 2) * Math.Pow(x2, 2) * Math.Pow(y1, 2) + Math.Pow(B0, 2) * Math.Pow(y0, 2) * Math.Pow(z1, 2) - 2 * Math.Pow(B0, 2) * Math.Pow(y0, 2) * z1 * z2 + Math.Pow(B0, 2) * Math.Pow(y0, 2) * Math.Pow(z2, 2) - 2 * Math.Pow(B0, 2) * y0 * y1 * z0 * z1 + 2 * Math.Pow(B0, 2) * y0 * y1 * z0 * z2 + 2 * Math.Pow(B0, 2) * y0 * y1 * z1 * z2 - 2 * Math.Pow(B0, 2) * y0 * y1 * Math.Pow(z2, 2) + 2 * Math.Pow(B0, 2) * y0 * y2 * z0 * z1 - 2 * Math.Pow(B0, 2) * y0 * y2 * z0 * z2 - 2 * Math.Pow(B0, 2) * y0 * y2 * Math.Pow(z1, 2) + 2 * Math.Pow(B0, 2) * y0 * y2 * z1 * z2 + Math.Pow(B0, 2) * Math.Pow(y1, 2) * Math.Pow(z0, 2) - 2 * Math.Pow(B0, 2) * Math.Pow(y1, 2) * z0 * z2 + Math.Pow(B0, 2) * Math.Pow(y1, 2) * Math.Pow(z2, 2) - 2 * Math.Pow(B0, 2) * y1 * y2 * Math.Pow(z0, 2) + 2 * Math.Pow(B0, 2) * y1 * y2 * z0 * z1 + 2 * Math.Pow(B0, 2) * y1 * y2 * z0 * z2 - 2 * Math.Pow(B0, 2) * y1 * y2 * z1 * z2 + Math.Pow(B0, 2) * Math.Pow(y2, 2) * Math.Pow(z0, 2) - 2 * Math.Pow(B0, 2) * Math.Pow(y2, 2) * z0 * z1 + Math.Pow(B0, 2) * Math.Pow(y2, 2) * Math.Pow(z1, 2) + 2 * B0 * C0 * Math.Pow(x0, 2) * y1 * z1 - 2 * B0 * C0 * Math.Pow(x0, 2) * y1 * z2 - 2 * B0 * C0 * Math.Pow(x0, 2) * y2 * z1 + 2 * B0 * C0 * Math.Pow(x0, 2) * y2 * z2 - 2 * B0 * C0 * x0 * x1 * y0 * z1 + 2 * B0 * C0 * x0 * x1 * y0 * z2 - 2 * B0 * C0 * x0 * x1 * y1 * z0 + 2 * B0 * C0 * x0 * x1 * y1 * z2 + 2 * B0 * C0 * x0 * x1 * y2 * z0 + 2 * B0 * C0 * x0 * x1 * y2 * z1 - 4 * B0 * C0 * x0 * x1 * y2 * z2 + 2 * B0 * C0 * x0 * x2 * y0 * z1 - 2 * B0 * C0 * x0 * x2 * y0 * z2 + 2 * B0 * C0 * x0 * x2 * y1 * z0 - 4 * B0 * C0 * x0 * x2 * y1 * z1 + 2 * B0 * C0 * x0 * x2 * y1 * z2 - 2 * B0 * C0 * x0 * x2 * y2 * z0 + 2 * B0 * C0 * x0 * x2 * y2 * z1 + 2 * B0 * C0 * Math.Pow(x1, 2) * y0 * z0 - 2 * B0 * C0 * Math.Pow(x1, 2) * y0 * z2 - 2 * B0 * C0 * Math.Pow(x1, 2) * y2 * z0 + 2 * B0 * C0 * Math.Pow(x1, 2) * y2 * z2 - 4 * B0 * C0 * x1 * x2 * y0 * z0 + 2 * B0 * C0 * x1 * x2 * y0 * z1 + 2 * B0 * C0 * x1 * x2 * y0 * z2 + 2 * B0 * C0 * x1 * x2 * y1 * z0 - 2 * B0 * C0 * x1 * x2 * y1 * z2 + 2 * B0 * C0 * x1 * x2 * y2 * z0 - 2 * B0 * C0 * x1 * x2 * y2 * z1 + 2 * B0 * C0 * Math.Pow(x2, 2) * y0 * z0 - 2 * B0 * C0 * Math.Pow(x2, 2) * y0 * z1 - 2 * B0 * C0 * Math.Pow(x2, 2) * y1 * z0 + 2 * B0 * C0 * Math.Pow(x2, 2) * y1 * z1 + Math.Pow(C0, 2) * Math.Pow(x0, 2) * Math.Pow(z1, 2) - 2 * Math.Pow(C0, 2) * Math.Pow(x0, 2) * z1 * z2 + Math.Pow(C0, 2) * Math.Pow(x0, 2) * Math.Pow(z2, 2) - 2 * Math.Pow(C0, 2) * x0 * x1 * z0 * z1 + 2 * Math.Pow(C0, 2) * x0 * x1 * z0 * z2 + 2 * Math.Pow(C0, 2) * x0 * x1 * z1 * z2 - 2 * Math.Pow(C0, 2) * x0 * x1 * Math.Pow(z2, 2) + 2 * Math.Pow(C0, 2) * x0 * x2 * z0 * z1 - 2 * Math.Pow(C0, 2) * x0 * x2 * z0 * z2 - 2 * Math.Pow(C0, 2) * x0 * x2 * Math.Pow(z1, 2) + 2 * Math.Pow(C0, 2) * x0 * x2 * z1 * z2 + Math.Pow(C0, 2) * Math.Pow(x1, 2) * Math.Pow(z0, 2) - 2 * Math.Pow(C0, 2) * Math.Pow(x1, 2) * z0 * z2 + Math.Pow(C0, 2) * Math.Pow(x1, 2) * Math.Pow(z2, 2) - 2 * Math.Pow(C0, 2) * x1 * x2 * Math.Pow(z0, 2) + 2 * Math.Pow(C0, 2) * x1 * x2 * z0 * z1 + 2 * Math.Pow(C0, 2) * x1 * x2 * z0 * z2 - 2 * Math.Pow(C0, 2) * x1 * x2 * z1 * z2 + Math.Pow(C0, 2) * Math.Pow(x2, 2) * Math.Pow(z0, 2) - 2 * Math.Pow(C0, 2) * Math.Pow(x2, 2) * z0 * z1 + Math.Pow(C0, 2) * Math.Pow(x2, 2) * Math.Pow(z1, 2) + Math.Pow(C0, 2) * Math.Pow(y0, 2) * Math.Pow(z1, 2) - 2 * Math.Pow(C0, 2) * Math.Pow(y0, 2) * z1 * z2 + Math.Pow(C0, 2) * Math.Pow(y0, 2) * Math.Pow(z2, 2) - 2 * Math.Pow(C0, 2) * y0 * y1 * z0 * z1 + 2 * Math.Pow(C0, 2) * y0 * y1 * z0 * z2 + 2 * Math.Pow(C0, 2) * y0 * y1 * z1 * z2 - 2 * Math.Pow(C0, 2) * y0 * y1 * Math.Pow(z2, 2) + 2 * Math.Pow(C0, 2) * y0 * y2 * z0 * z1 - 2 * Math.Pow(C0, 2) * y0 * y2 * z0 * z2 - 2 * Math.Pow(C0, 2) * y0 * y2 * Math.Pow(z1, 2) + 2 * Math.Pow(C0, 2) * y0 * y2 * z1 * z2 + Math.Pow(C0, 2) * Math.Pow(y1, 2) * Math.Pow(z0, 2) - 2 * Math.Pow(C0, 2) * Math.Pow(y1, 2) * z0 * z2 + Math.Pow(C0, 2) * Math.Pow(y1, 2) * Math.Pow(z2, 2) - 2 * Math.Pow(C0, 2) * y1 * y2 * Math.Pow(z0, 2) + 2 * Math.Pow(C0, 2) * y1 * y2 * z0 * z1 + 2 * Math.Pow(C0, 2) * y1 * y2 * z0 * z2 - 2 * Math.Pow(C0, 2) * y1 * y2 * z1 * z2 + Math.Pow(C0, 2) * Math.Pow(y2, 2) * Math.Pow(z0, 2) - 2 * Math.Pow(C0, 2) * Math.Pow(y2, 2) * z0 * z1 + Math.Pow(C0, 2) * Math.Pow(y2, 2) * Math.Pow(z1, 2)), 1 / 2) * (A0 * x0 * z1 - A0 * x1 * z0 - A0 * x0 * z2 + A0 * x2 * z0 + A0 * x1 * z2 - A0 * x2 * z1 + B0 * y0 * z1 - B0 * y1 * z0 - B0 * y0 * z2 + B0 * y2 * z0 + B0 * y1 * z2 - B0 * y2 * z1));
                    PoICoords2.Add(x0 + R0 * Math.Pow(1 / (Math.Pow(A0, 2) * Math.Pow(x0, 2) * Math.Pow(y1, 2) - 2 * Math.Pow(A0, 2) * Math.Pow(x0, 2) * y1 * y2 + Math.Pow(A0, 2) * Math.Pow(x0, 2) * Math.Pow(y2, 2) + Math.Pow(A0, 2) * Math.Pow(x0, 2) * Math.Pow(z1, 2) - 2 * Math.Pow(A0, 2) * Math.Pow(x0, 2) * z1 * z2 + Math.Pow(A0, 2) * Math.Pow(x0, 2) * Math.Pow(z2, 2) - 2 * Math.Pow(A0, 2) * x0 * x1 * y0 * y1 + 2 * Math.Pow(A0, 2) * x0 * x1 * y0 * y2 + 2 * Math.Pow(A0, 2) * x0 * x1 * y1 * y2 - 2 * Math.Pow(A0, 2) * x0 * x1 * Math.Pow(y2, 2) - 2 * Math.Pow(A0, 2) * x0 * x1 * z0 * z1 + 2 * Math.Pow(A0, 2) * x0 * x1 * z0 * z2 + 2 * Math.Pow(A0, 2) * x0 * x1 * z1 * z2 - 2 * Math.Pow(A0, 2) * x0 * x1 * Math.Pow(z2, 2) + 2 * Math.Pow(A0, 2) * x0 * x2 * y0 * y1 - 2 * Math.Pow(A0, 2) * x0 * x2 * y0 * y2 - 2 * Math.Pow(A0, 2) * x0 * x2 * Math.Pow(y1, 2) + 2 * Math.Pow(A0, 2) * x0 * x2 * y1 * y2 + 2 * Math.Pow(A0, 2) * x0 * x2 * z0 * z1 - 2 * Math.Pow(A0, 2) * x0 * x2 * z0 * z2 - 2 * Math.Pow(A0, 2) * x0 * x2 * Math.Pow(z1, 2) + 2 * Math.Pow(A0, 2) * x0 * x2 * z1 * z2 + Math.Pow(A0, 2) * Math.Pow(x1, 2) * Math.Pow(y0, 2) - 2 * Math.Pow(A0, 2) * Math.Pow(x1, 2) * y0 * y2 + Math.Pow(A0, 2) * Math.Pow(x1, 2) * Math.Pow(y2, 2) + Math.Pow(A0, 2) * Math.Pow(x1, 2) * Math.Pow(z0, 2) - 2 * Math.Pow(A0, 2) * Math.Pow(x1, 2) * z0 * z2 + Math.Pow(A0, 2) * Math.Pow(x1, 2) * Math.Pow(z2, 2) - 2 * Math.Pow(A0, 2) * x1 * x2 * Math.Pow(y0, 2) + 2 * Math.Pow(A0, 2) * x1 * x2 * y0 * y1 + 2 * Math.Pow(A0, 2) * x1 * x2 * y0 * y2 - 2 * Math.Pow(A0, 2) * x1 * x2 * y1 * y2 - 2 * Math.Pow(A0, 2) * x1 * x2 * Math.Pow(z0, 2) + 2 * Math.Pow(A0, 2) * x1 * x2 * z0 * z1 + 2 * Math.Pow(A0, 2) * x1 * x2 * z0 * z2 - 2 * Math.Pow(A0, 2) * x1 * x2 * z1 * z2 + Math.Pow(A0, 2) * Math.Pow(x2, 2) * Math.Pow(y0, 2) - 2 * Math.Pow(A0, 2) * Math.Pow(x2, 2) * y0 * y1 + Math.Pow(A0, 2) * Math.Pow(x2, 2) * Math.Pow(y1, 2) + Math.Pow(A0, 2) * Math.Pow(x2, 2) * Math.Pow(z0, 2) - 2 * Math.Pow(A0, 2) * Math.Pow(x2, 2) * z0 * z1 + Math.Pow(A0, 2) * Math.Pow(x2, 2) * Math.Pow(z1, 2) + 2 * A0 * B0 * x0 * y0 * Math.Pow(z1, 2) - 4 * A0 * B0 * x0 * y0 * z1 * z2 + 2 * A0 * B0 * x0 * y0 * Math.Pow(z2, 2) - 2 * A0 * B0 * x0 * y1 * z0 * z1 + 2 * A0 * B0 * x0 * y1 * z0 * z2 + 2 * A0 * B0 * x0 * y1 * z1 * z2 - 2 * A0 * B0 * x0 * y1 * Math.Pow(z2, 2) + 2 * A0 * B0 * x0 * y2 * z0 * z1 - 2 * A0 * B0 * x0 * y2 * z0 * z2 - 2 * A0 * B0 * x0 * y2 * Math.Pow(z1, 2) + 2 * A0 * B0 * x0 * y2 * z1 * z2 - 2 * A0 * B0 * x1 * y0 * z0 * z1 + 2 * A0 * B0 * x1 * y0 * z0 * z2 + 2 * A0 * B0 * x1 * y0 * z1 * z2 - 2 * A0 * B0 * x1 * y0 * Math.Pow(z2, 2) + 2 * A0 * B0 * x1 * y1 * Math.Pow(z0, 2) - 4 * A0 * B0 * x1 * y1 * z0 * z2 + 2 * A0 * B0 * x1 * y1 * Math.Pow(z2, 2) - 2 * A0 * B0 * x1 * y2 * Math.Pow(z0, 2) + 2 * A0 * B0 * x1 * y2 * z0 * z1 + 2 * A0 * B0 * x1 * y2 * z0 * z2 - 2 * A0 * B0 * x1 * y2 * z1 * z2 + 2 * A0 * B0 * x2 * y0 * z0 * z1 - 2 * A0 * B0 * x2 * y0 * z0 * z2 - 2 * A0 * B0 * x2 * y0 * Math.Pow(z1, 2) + 2 * A0 * B0 * x2 * y0 * z1 * z2 - 2 * A0 * B0 * x2 * y1 * Math.Pow(z0, 2) + 2 * A0 * B0 * x2 * y1 * z0 * z1 + 2 * A0 * B0 * x2 * y1 * z0 * z2 - 2 * A0 * B0 * x2 * y1 * z1 * z2 + 2 * A0 * B0 * x2 * y2 * Math.Pow(z0, 2) - 4 * A0 * B0 * x2 * y2 * z0 * z1 + 2 * A0 * B0 * x2 * y2 * Math.Pow(z1, 2) - 2 * A0 * C0 * x0 * y0 * y1 * z1 + 2 * A0 * C0 * x0 * y0 * y1 * z2 + 2 * A0 * C0 * x0 * y0 * y2 * z1 - 2 * A0 * C0 * x0 * y0 * y2 * z2 + 2 * A0 * C0 * x0 * Math.Pow(y1, 2) * z0 - 2 * A0 * C0 * x0 * Math.Pow(y1, 2) * z2 - 4 * A0 * C0 * x0 * y1 * y2 * z0 + 2 * A0 * C0 * x0 * y1 * y2 * z1 + 2 * A0 * C0 * x0 * y1 * y2 * z2 + 2 * A0 * C0 * x0 * Math.Pow(y2, 2) * z0 - 2 * A0 * C0 * x0 * Math.Pow(y2, 2) * z1 + 2 * A0 * C0 * x1 * Math.Pow(y0, 2) * z1 - 2 * A0 * C0 * x1 * Math.Pow(y0, 2) * z2 - 2 * A0 * C0 * x1 * y0 * y1 * z0 + 2 * A0 * C0 * x1 * y0 * y1 * z2 + 2 * A0 * C0 * x1 * y0 * y2 * z0 - 4 * A0 * C0 * x1 * y0 * y2 * z1 + 2 * A0 * C0 * x1 * y0 * y2 * z2 + 2 * A0 * C0 * x1 * y1 * y2 * z0 - 2 * A0 * C0 * x1 * y1 * y2 * z2 - 2 * A0 * C0 * x1 * Math.Pow(y2, 2) * z0 + 2 * A0 * C0 * x1 * Math.Pow(y2, 2) * z1 - 2 * A0 * C0 * x2 * Math.Pow(y0, 2) * z1 + 2 * A0 * C0 * x2 * Math.Pow(y0, 2) * z2 + 2 * A0 * C0 * x2 * y0 * y1 * z0 + 2 * A0 * C0 * x2 * y0 * y1 * z1 - 4 * A0 * C0 * x2 * y0 * y1 * z2 - 2 * A0 * C0 * x2 * y0 * y2 * z0 + 2 * A0 * C0 * x2 * y0 * y2 * z1 - 2 * A0 * C0 * x2 * Math.Pow(y1, 2) * z0 + 2 * A0 * C0 * x2 * Math.Pow(y1, 2) * z2 + 2 * A0 * C0 * x2 * y1 * y2 * z0 - 2 * A0 * C0 * x2 * y1 * y2 * z1 + Math.Pow(B0, 2) * Math.Pow(x0, 2) * Math.Pow(y1, 2) - 2 * Math.Pow(B0, 2) * Math.Pow(x0, 2) * y1 * y2 + Math.Pow(B0, 2) * Math.Pow(x0, 2) * Math.Pow(y2, 2) - 2 * Math.Pow(B0, 2) * x0 * x1 * y0 * y1 + 2 * Math.Pow(B0, 2) * x0 * x1 * y0 * y2 + 2 * Math.Pow(B0, 2) * x0 * x1 * y1 * y2 - 2 * Math.Pow(B0, 2) * x0 * x1 * Math.Pow(y2, 2) + 2 * Math.Pow(B0, 2) * x0 * x2 * y0 * y1 - 2 * Math.Pow(B0, 2) * x0 * x2 * y0 * y2 - 2 * Math.Pow(B0, 2) * x0 * x2 * Math.Pow(y1, 2) + 2 * Math.Pow(B0, 2) * x0 * x2 * y1 * y2 + Math.Pow(B0, 2) * Math.Pow(x1, 2) * Math.Pow(y0, 2) - 2 * Math.Pow(B0, 2) * Math.Pow(x1, 2) * y0 * y2 + Math.Pow(B0, 2) * Math.Pow(x1, 2) * Math.Pow(y2, 2) - 2 * Math.Pow(B0, 2) * x1 * x2 * Math.Pow(y0, 2) + 2 * Math.Pow(B0, 2) * x1 * x2 * y0 * y1 + 2 * Math.Pow(B0, 2) * x1 * x2 * y0 * y2 - 2 * Math.Pow(B0, 2) * x1 * x2 * y1 * y2 + Math.Pow(B0, 2) * Math.Pow(x2, 2) * Math.Pow(y0, 2) - 2 * Math.Pow(B0, 2) * Math.Pow(x2, 2) * y0 * y1 + Math.Pow(B0, 2) * Math.Pow(x2, 2) * Math.Pow(y1, 2) + Math.Pow(B0, 2) * Math.Pow(y0, 2) * Math.Pow(z1, 2) - 2 * Math.Pow(B0, 2) * Math.Pow(y0, 2) * z1 * z2 + Math.Pow(B0, 2) * Math.Pow(y0, 2) * Math.Pow(z2, 2) - 2 * Math.Pow(B0, 2) * y0 * y1 * z0 * z1 + 2 * Math.Pow(B0, 2) * y0 * y1 * z0 * z2 + 2 * Math.Pow(B0, 2) * y0 * y1 * z1 * z2 - 2 * Math.Pow(B0, 2) * y0 * y1 * Math.Pow(z2, 2) + 2 * Math.Pow(B0, 2) * y0 * y2 * z0 * z1 - 2 * Math.Pow(B0, 2) * y0 * y2 * z0 * z2 - 2 * Math.Pow(B0, 2) * y0 * y2 * Math.Pow(z1, 2) + 2 * Math.Pow(B0, 2) * y0 * y2 * z1 * z2 + Math.Pow(B0, 2) * Math.Pow(y1, 2) * Math.Pow(z0, 2) - 2 * Math.Pow(B0, 2) * Math.Pow(y1, 2) * z0 * z2 + Math.Pow(B0, 2) * Math.Pow(y1, 2) * Math.Pow(z2, 2) - 2 * Math.Pow(B0, 2) * y1 * y2 * Math.Pow(z0, 2) + 2 * Math.Pow(B0, 2) * y1 * y2 * z0 * z1 + 2 * Math.Pow(B0, 2) * y1 * y2 * z0 * z2 - 2 * Math.Pow(B0, 2) * y1 * y2 * z1 * z2 + Math.Pow(B0, 2) * Math.Pow(y2, 2) * Math.Pow(z0, 2) - 2 * Math.Pow(B0, 2) * Math.Pow(y2, 2) * z0 * z1 + Math.Pow(B0, 2) * Math.Pow(y2, 2) * Math.Pow(z1, 2) + 2 * B0 * C0 * Math.Pow(x0, 2) * y1 * z1 - 2 * B0 * C0 * Math.Pow(x0, 2) * y1 * z2 - 2 * B0 * C0 * Math.Pow(x0, 2) * y2 * z1 + 2 * B0 * C0 * Math.Pow(x0, 2) * y2 * z2 - 2 * B0 * C0 * x0 * x1 * y0 * z1 + 2 * B0 * C0 * x0 * x1 * y0 * z2 - 2 * B0 * C0 * x0 * x1 * y1 * z0 + 2 * B0 * C0 * x0 * x1 * y1 * z2 + 2 * B0 * C0 * x0 * x1 * y2 * z0 + 2 * B0 * C0 * x0 * x1 * y2 * z1 - 4 * B0 * C0 * x0 * x1 * y2 * z2 + 2 * B0 * C0 * x0 * x2 * y0 * z1 - 2 * B0 * C0 * x0 * x2 * y0 * z2 + 2 * B0 * C0 * x0 * x2 * y1 * z0 - 4 * B0 * C0 * x0 * x2 * y1 * z1 + 2 * B0 * C0 * x0 * x2 * y1 * z2 - 2 * B0 * C0 * x0 * x2 * y2 * z0 + 2 * B0 * C0 * x0 * x2 * y2 * z1 + 2 * B0 * C0 * Math.Pow(x1, 2) * y0 * z0 - 2 * B0 * C0 * Math.Pow(x1, 2) * y0 * z2 - 2 * B0 * C0 * Math.Pow(x1, 2) * y2 * z0 + 2 * B0 * C0 * Math.Pow(x1, 2) * y2 * z2 - 4 * B0 * C0 * x1 * x2 * y0 * z0 + 2 * B0 * C0 * x1 * x2 * y0 * z1 + 2 * B0 * C0 * x1 * x2 * y0 * z2 + 2 * B0 * C0 * x1 * x2 * y1 * z0 - 2 * B0 * C0 * x1 * x2 * y1 * z2 + 2 * B0 * C0 * x1 * x2 * y2 * z0 - 2 * B0 * C0 * x1 * x2 * y2 * z1 + 2 * B0 * C0 * Math.Pow(x2, 2) * y0 * z0 - 2 * B0 * C0 * Math.Pow(x2, 2) * y0 * z1 - 2 * B0 * C0 * Math.Pow(x2, 2) * y1 * z0 + 2 * B0 * C0 * Math.Pow(x2, 2) * y1 * z1 + Math.Pow(C0, 2) * Math.Pow(x0, 2) * Math.Pow(z1, 2) - 2 * Math.Pow(C0, 2) * Math.Pow(x0, 2) * z1 * z2 + Math.Pow(C0, 2) * Math.Pow(x0, 2) * Math.Pow(z2, 2) - 2 * Math.Pow(C0, 2) * x0 * x1 * z0 * z1 + 2 * Math.Pow(C0, 2) * x0 * x1 * z0 * z2 + 2 * Math.Pow(C0, 2) * x0 * x1 * z1 * z2 - 2 * Math.Pow(C0, 2) * x0 * x1 * Math.Pow(z2, 2) + 2 * Math.Pow(C0, 2) * x0 * x2 * z0 * z1 - 2 * Math.Pow(C0, 2) * x0 * x2 * z0 * z2 - 2 * Math.Pow(C0, 2) * x0 * x2 * Math.Pow(z1, 2) + 2 * Math.Pow(C0, 2) * x0 * x2 * z1 * z2 + Math.Pow(C0, 2) * Math.Pow(x1, 2) * Math.Pow(z0, 2) - 2 * Math.Pow(C0, 2) * Math.Pow(x1, 2) * z0 * z2 + Math.Pow(C0, 2) * Math.Pow(x1, 2) * Math.Pow(z2, 2) - 2 * Math.Pow(C0, 2) * x1 * x2 * Math.Pow(z0, 2) + 2 * Math.Pow(C0, 2) * x1 * x2 * z0 * z1 + 2 * Math.Pow(C0, 2) * x1 * x2 * z0 * z2 - 2 * Math.Pow(C0, 2) * x1 * x2 * z1 * z2 + Math.Pow(C0, 2) * Math.Pow(x2, 2) * Math.Pow(z0, 2) - 2 * Math.Pow(C0, 2) * Math.Pow(x2, 2) * z0 * z1 + Math.Pow(C0, 2) * Math.Pow(x2, 2) * Math.Pow(z1, 2) + Math.Pow(C0, 2) * Math.Pow(y0, 2) * Math.Pow(z1, 2) - 2 * Math.Pow(C0, 2) * Math.Pow(y0, 2) * z1 * z2 + Math.Pow(C0, 2) * Math.Pow(y0, 2) * Math.Pow(z2, 2) - 2 * Math.Pow(C0, 2) * y0 * y1 * z0 * z1 + 2 * Math.Pow(C0, 2) * y0 * y1 * z0 * z2 + 2 * Math.Pow(C0, 2) * y0 * y1 * z1 * z2 - 2 * Math.Pow(C0, 2) * y0 * y1 * Math.Pow(z2, 2) + 2 * Math.Pow(C0, 2) * y0 * y2 * z0 * z1 - 2 * Math.Pow(C0, 2) * y0 * y2 * z0 * z2 - 2 * Math.Pow(C0, 2) * y0 * y2 * Math.Pow(z1, 2) + 2 * Math.Pow(C0, 2) * y0 * y2 * z1 * z2 + Math.Pow(C0, 2) * Math.Pow(y1, 2) * Math.Pow(z0, 2) - 2 * Math.Pow(C0, 2) * Math.Pow(y1, 2) * z0 * z2 + Math.Pow(C0, 2) * Math.Pow(y1, 2) * Math.Pow(z2, 2) - 2 * Math.Pow(C0, 2) * y1 * y2 * Math.Pow(z0, 2) + 2 * Math.Pow(C0, 2) * y1 * y2 * z0 * z1 + 2 * Math.Pow(C0, 2) * y1 * y2 * z0 * z2 - 2 * Math.Pow(C0, 2) * y1 * y2 * z1 * z2 + Math.Pow(C0, 2) * Math.Pow(y2, 2) * Math.Pow(z0, 2) - 2 * Math.Pow(C0, 2) * Math.Pow(y2, 2) * z0 * z1 + Math.Pow(C0, 2) * Math.Pow(y2, 2) * Math.Pow(z1, 2)), 1 / 2) * (B0 * x0 * y1 - B0 * x1 * y0 - B0 * x0 * y2 + B0 * x2 * y0 + B0 * x1 * y2 - B0 * x2 * y1 + C0 * x0 * z1 - C0 * x1 * z0 - C0 * x0 * z2 + C0 * x2 * z0 + C0 * x1 * z2 - C0 * x2 * z1));
                    PoICoords2.Add(y0 - R0 * Math.Pow(1 / (Math.Pow(A0, 2) * Math.Pow(x0, 2) * Math.Pow(y1, 2) - 2 * Math.Pow(A0, 2) * Math.Pow(x0, 2) * y1 * y2 + Math.Pow(A0, 2) * Math.Pow(x0, 2) * Math.Pow(y2, 2) + Math.Pow(A0, 2) * Math.Pow(x0, 2) * Math.Pow(z1, 2) - 2 * Math.Pow(A0, 2) * Math.Pow(x0, 2) * z1 * z2 + Math.Pow(A0, 2) * Math.Pow(x0, 2) * Math.Pow(z2, 2) - 2 * Math.Pow(A0, 2) * x0 * x1 * y0 * y1 + 2 * Math.Pow(A0, 2) * x0 * x1 * y0 * y2 + 2 * Math.Pow(A0, 2) * x0 * x1 * y1 * y2 - 2 * Math.Pow(A0, 2) * x0 * x1 * Math.Pow(y2, 2) - 2 * Math.Pow(A0, 2) * x0 * x1 * z0 * z1 + 2 * Math.Pow(A0, 2) * x0 * x1 * z0 * z2 + 2 * Math.Pow(A0, 2) * x0 * x1 * z1 * z2 - 2 * Math.Pow(A0, 2) * x0 * x1 * Math.Pow(z2, 2) + 2 * Math.Pow(A0, 2) * x0 * x2 * y0 * y1 - 2 * Math.Pow(A0, 2) * x0 * x2 * y0 * y2 - 2 * Math.Pow(A0, 2) * x0 * x2 * Math.Pow(y1, 2) + 2 * Math.Pow(A0, 2) * x0 * x2 * y1 * y2 + 2 * Math.Pow(A0, 2) * x0 * x2 * z0 * z1 - 2 * Math.Pow(A0, 2) * x0 * x2 * z0 * z2 - 2 * Math.Pow(A0, 2) * x0 * x2 * Math.Pow(z1, 2) + 2 * Math.Pow(A0, 2) * x0 * x2 * z1 * z2 + Math.Pow(A0, 2) * Math.Pow(x1, 2) * Math.Pow(y0, 2) - 2 * Math.Pow(A0, 2) * Math.Pow(x1, 2) * y0 * y2 + Math.Pow(A0, 2) * Math.Pow(x1, 2) * Math.Pow(y2, 2) + Math.Pow(A0, 2) * Math.Pow(x1, 2) * Math.Pow(z0, 2) - 2 * Math.Pow(A0, 2) * Math.Pow(x1, 2) * z0 * z2 + Math.Pow(A0, 2) * Math.Pow(x1, 2) * Math.Pow(z2, 2) - 2 * Math.Pow(A0, 2) * x1 * x2 * Math.Pow(y0, 2) + 2 * Math.Pow(A0, 2) * x1 * x2 * y0 * y1 + 2 * Math.Pow(A0, 2) * x1 * x2 * y0 * y2 - 2 * Math.Pow(A0, 2) * x1 * x2 * y1 * y2 - 2 * Math.Pow(A0, 2) * x1 * x2 * Math.Pow(z0, 2) + 2 * Math.Pow(A0, 2) * x1 * x2 * z0 * z1 + 2 * Math.Pow(A0, 2) * x1 * x2 * z0 * z2 - 2 * Math.Pow(A0, 2) * x1 * x2 * z1 * z2 + Math.Pow(A0, 2) * Math.Pow(x2, 2) * Math.Pow(y0, 2) - 2 * Math.Pow(A0, 2) * Math.Pow(x2, 2) * y0 * y1 + Math.Pow(A0, 2) * Math.Pow(x2, 2) * Math.Pow(y1, 2) + Math.Pow(A0, 2) * Math.Pow(x2, 2) * Math.Pow(z0, 2) - 2 * Math.Pow(A0, 2) * Math.Pow(x2, 2) * z0 * z1 + Math.Pow(A0, 2) * Math.Pow(x2, 2) * Math.Pow(z1, 2) + 2 * A0 * B0 * x0 * y0 * Math.Pow(z1, 2) - 4 * A0 * B0 * x0 * y0 * z1 * z2 + 2 * A0 * B0 * x0 * y0 * Math.Pow(z2, 2) - 2 * A0 * B0 * x0 * y1 * z0 * z1 + 2 * A0 * B0 * x0 * y1 * z0 * z2 + 2 * A0 * B0 * x0 * y1 * z1 * z2 - 2 * A0 * B0 * x0 * y1 * Math.Pow(z2, 2) + 2 * A0 * B0 * x0 * y2 * z0 * z1 - 2 * A0 * B0 * x0 * y2 * z0 * z2 - 2 * A0 * B0 * x0 * y2 * Math.Pow(z1, 2) + 2 * A0 * B0 * x0 * y2 * z1 * z2 - 2 * A0 * B0 * x1 * y0 * z0 * z1 + 2 * A0 * B0 * x1 * y0 * z0 * z2 + 2 * A0 * B0 * x1 * y0 * z1 * z2 - 2 * A0 * B0 * x1 * y0 * Math.Pow(z2, 2) + 2 * A0 * B0 * x1 * y1 * Math.Pow(z0, 2) - 4 * A0 * B0 * x1 * y1 * z0 * z2 + 2 * A0 * B0 * x1 * y1 * Math.Pow(z2, 2) - 2 * A0 * B0 * x1 * y2 * Math.Pow(z0, 2) + 2 * A0 * B0 * x1 * y2 * z0 * z1 + 2 * A0 * B0 * x1 * y2 * z0 * z2 - 2 * A0 * B0 * x1 * y2 * z1 * z2 + 2 * A0 * B0 * x2 * y0 * z0 * z1 - 2 * A0 * B0 * x2 * y0 * z0 * z2 - 2 * A0 * B0 * x2 * y0 * Math.Pow(z1, 2) + 2 * A0 * B0 * x2 * y0 * z1 * z2 - 2 * A0 * B0 * x2 * y1 * Math.Pow(z0, 2) + 2 * A0 * B0 * x2 * y1 * z0 * z1 + 2 * A0 * B0 * x2 * y1 * z0 * z2 - 2 * A0 * B0 * x2 * y1 * z1 * z2 + 2 * A0 * B0 * x2 * y2 * Math.Pow(z0, 2) - 4 * A0 * B0 * x2 * y2 * z0 * z1 + 2 * A0 * B0 * x2 * y2 * Math.Pow(z1, 2) - 2 * A0 * C0 * x0 * y0 * y1 * z1 + 2 * A0 * C0 * x0 * y0 * y1 * z2 + 2 * A0 * C0 * x0 * y0 * y2 * z1 - 2 * A0 * C0 * x0 * y0 * y2 * z2 + 2 * A0 * C0 * x0 * Math.Pow(y1, 2) * z0 - 2 * A0 * C0 * x0 * Math.Pow(y1, 2) * z2 - 4 * A0 * C0 * x0 * y1 * y2 * z0 + 2 * A0 * C0 * x0 * y1 * y2 * z1 + 2 * A0 * C0 * x0 * y1 * y2 * z2 + 2 * A0 * C0 * x0 * Math.Pow(y2, 2) * z0 - 2 * A0 * C0 * x0 * Math.Pow(y2, 2) * z1 + 2 * A0 * C0 * x1 * Math.Pow(y0, 2) * z1 - 2 * A0 * C0 * x1 * Math.Pow(y0, 2) * z2 - 2 * A0 * C0 * x1 * y0 * y1 * z0 + 2 * A0 * C0 * x1 * y0 * y1 * z2 + 2 * A0 * C0 * x1 * y0 * y2 * z0 - 4 * A0 * C0 * x1 * y0 * y2 * z1 + 2 * A0 * C0 * x1 * y0 * y2 * z2 + 2 * A0 * C0 * x1 * y1 * y2 * z0 - 2 * A0 * C0 * x1 * y1 * y2 * z2 - 2 * A0 * C0 * x1 * Math.Pow(y2, 2) * z0 + 2 * A0 * C0 * x1 * Math.Pow(y2, 2) * z1 - 2 * A0 * C0 * x2 * Math.Pow(y0, 2) * z1 + 2 * A0 * C0 * x2 * Math.Pow(y0, 2) * z2 + 2 * A0 * C0 * x2 * y0 * y1 * z0 + 2 * A0 * C0 * x2 * y0 * y1 * z1 - 4 * A0 * C0 * x2 * y0 * y1 * z2 - 2 * A0 * C0 * x2 * y0 * y2 * z0 + 2 * A0 * C0 * x2 * y0 * y2 * z1 - 2 * A0 * C0 * x2 * Math.Pow(y1, 2) * z0 + 2 * A0 * C0 * x2 * Math.Pow(y1, 2) * z2 + 2 * A0 * C0 * x2 * y1 * y2 * z0 - 2 * A0 * C0 * x2 * y1 * y2 * z1 + Math.Pow(B0, 2) * Math.Pow(x0, 2) * Math.Pow(y1, 2) - 2 * Math.Pow(B0, 2) * Math.Pow(x0, 2) * y1 * y2 + Math.Pow(B0, 2) * Math.Pow(x0, 2) * Math.Pow(y2, 2) - 2 * Math.Pow(B0, 2) * x0 * x1 * y0 * y1 + 2 * Math.Pow(B0, 2) * x0 * x1 * y0 * y2 + 2 * Math.Pow(B0, 2) * x0 * x1 * y1 * y2 - 2 * Math.Pow(B0, 2) * x0 * x1 * Math.Pow(y2, 2) + 2 * Math.Pow(B0, 2) * x0 * x2 * y0 * y1 - 2 * Math.Pow(B0, 2) * x0 * x2 * y0 * y2 - 2 * Math.Pow(B0, 2) * x0 * x2 * Math.Pow(y1, 2) + 2 * Math.Pow(B0, 2) * x0 * x2 * y1 * y2 + Math.Pow(B0, 2) * Math.Pow(x1, 2) * Math.Pow(y0, 2) - 2 * Math.Pow(B0, 2) * Math.Pow(x1, 2) * y0 * y2 + Math.Pow(B0, 2) * Math.Pow(x1, 2) * Math.Pow(y2, 2) - 2 * Math.Pow(B0, 2) * x1 * x2 * Math.Pow(y0, 2) + 2 * Math.Pow(B0, 2) * x1 * x2 * y0 * y1 + 2 * Math.Pow(B0, 2) * x1 * x2 * y0 * y2 - 2 * Math.Pow(B0, 2) * x1 * x2 * y1 * y2 + Math.Pow(B0, 2) * Math.Pow(x2, 2) * Math.Pow(y0, 2) - 2 * Math.Pow(B0, 2) * Math.Pow(x2, 2) * y0 * y1 + Math.Pow(B0, 2) * Math.Pow(x2, 2) * Math.Pow(y1, 2) + Math.Pow(B0, 2) * Math.Pow(y0, 2) * Math.Pow(z1, 2) - 2 * Math.Pow(B0, 2) * Math.Pow(y0, 2) * z1 * z2 + Math.Pow(B0, 2) * Math.Pow(y0, 2) * Math.Pow(z2, 2) - 2 * Math.Pow(B0, 2) * y0 * y1 * z0 * z1 + 2 * Math.Pow(B0, 2) * y0 * y1 * z0 * z2 + 2 * Math.Pow(B0, 2) * y0 * y1 * z1 * z2 - 2 * Math.Pow(B0, 2) * y0 * y1 * Math.Pow(z2, 2) + 2 * Math.Pow(B0, 2) * y0 * y2 * z0 * z1 - 2 * Math.Pow(B0, 2) * y0 * y2 * z0 * z2 - 2 * Math.Pow(B0, 2) * y0 * y2 * Math.Pow(z1, 2) + 2 * Math.Pow(B0, 2) * y0 * y2 * z1 * z2 + Math.Pow(B0, 2) * Math.Pow(y1, 2) * Math.Pow(z0, 2) - 2 * Math.Pow(B0, 2) * Math.Pow(y1, 2) * z0 * z2 + Math.Pow(B0, 2) * Math.Pow(y1, 2) * Math.Pow(z2, 2) - 2 * Math.Pow(B0, 2) * y1 * y2 * Math.Pow(z0, 2) + 2 * Math.Pow(B0, 2) * y1 * y2 * z0 * z1 + 2 * Math.Pow(B0, 2) * y1 * y2 * z0 * z2 - 2 * Math.Pow(B0, 2) * y1 * y2 * z1 * z2 + Math.Pow(B0, 2) * Math.Pow(y2, 2) * Math.Pow(z0, 2) - 2 * Math.Pow(B0, 2) * Math.Pow(y2, 2) * z0 * z1 + Math.Pow(B0, 2) * Math.Pow(y2, 2) * Math.Pow(z1, 2) + 2 * B0 * C0 * Math.Pow(x0, 2) * y1 * z1 - 2 * B0 * C0 * Math.Pow(x0, 2) * y1 * z2 - 2 * B0 * C0 * Math.Pow(x0, 2) * y2 * z1 + 2 * B0 * C0 * Math.Pow(x0, 2) * y2 * z2 - 2 * B0 * C0 * x0 * x1 * y0 * z1 + 2 * B0 * C0 * x0 * x1 * y0 * z2 - 2 * B0 * C0 * x0 * x1 * y1 * z0 + 2 * B0 * C0 * x0 * x1 * y1 * z2 + 2 * B0 * C0 * x0 * x1 * y2 * z0 + 2 * B0 * C0 * x0 * x1 * y2 * z1 - 4 * B0 * C0 * x0 * x1 * y2 * z2 + 2 * B0 * C0 * x0 * x2 * y0 * z1 - 2 * B0 * C0 * x0 * x2 * y0 * z2 + 2 * B0 * C0 * x0 * x2 * y1 * z0 - 4 * B0 * C0 * x0 * x2 * y1 * z1 + 2 * B0 * C0 * x0 * x2 * y1 * z2 - 2 * B0 * C0 * x0 * x2 * y2 * z0 + 2 * B0 * C0 * x0 * x2 * y2 * z1 + 2 * B0 * C0 * Math.Pow(x1, 2) * y0 * z0 - 2 * B0 * C0 * Math.Pow(x1, 2) * y0 * z2 - 2 * B0 * C0 * Math.Pow(x1, 2) * y2 * z0 + 2 * B0 * C0 * Math.Pow(x1, 2) * y2 * z2 - 4 * B0 * C0 * x1 * x2 * y0 * z0 + 2 * B0 * C0 * x1 * x2 * y0 * z1 + 2 * B0 * C0 * x1 * x2 * y0 * z2 + 2 * B0 * C0 * x1 * x2 * y1 * z0 - 2 * B0 * C0 * x1 * x2 * y1 * z2 + 2 * B0 * C0 * x1 * x2 * y2 * z0 - 2 * B0 * C0 * x1 * x2 * y2 * z1 + 2 * B0 * C0 * Math.Pow(x2, 2) * y0 * z0 - 2 * B0 * C0 * Math.Pow(x2, 2) * y0 * z1 - 2 * B0 * C0 * Math.Pow(x2, 2) * y1 * z0 + 2 * B0 * C0 * Math.Pow(x2, 2) * y1 * z1 + Math.Pow(C0, 2) * Math.Pow(x0, 2) * Math.Pow(z1, 2) - 2 * Math.Pow(C0, 2) * Math.Pow(x0, 2) * z1 * z2 + Math.Pow(C0, 2) * Math.Pow(x0, 2) * Math.Pow(z2, 2) - 2 * Math.Pow(C0, 2) * x0 * x1 * z0 * z1 + 2 * Math.Pow(C0, 2) * x0 * x1 * z0 * z2 + 2 * Math.Pow(C0, 2) * x0 * x1 * z1 * z2 - 2 * Math.Pow(C0, 2) * x0 * x1 * Math.Pow(z2, 2) + 2 * Math.Pow(C0, 2) * x0 * x2 * z0 * z1 - 2 * Math.Pow(C0, 2) * x0 * x2 * z0 * z2 - 2 * Math.Pow(C0, 2) * x0 * x2 * Math.Pow(z1, 2) + 2 * Math.Pow(C0, 2) * x0 * x2 * z1 * z2 + Math.Pow(C0, 2) * Math.Pow(x1, 2) * Math.Pow(z0, 2) - 2 * Math.Pow(C0, 2) * Math.Pow(x1, 2) * z0 * z2 + Math.Pow(C0, 2) * Math.Pow(x1, 2) * Math.Pow(z2, 2) - 2 * Math.Pow(C0, 2) * x1 * x2 * Math.Pow(z0, 2) + 2 * Math.Pow(C0, 2) * x1 * x2 * z0 * z1 + 2 * Math.Pow(C0, 2) * x1 * x2 * z0 * z2 - 2 * Math.Pow(C0, 2) * x1 * x2 * z1 * z2 + Math.Pow(C0, 2) * Math.Pow(x2, 2) * Math.Pow(z0, 2) - 2 * Math.Pow(C0, 2) * Math.Pow(x2, 2) * z0 * z1 + Math.Pow(C0, 2) * Math.Pow(x2, 2) * Math.Pow(z1, 2) + Math.Pow(C0, 2) * Math.Pow(y0, 2) * Math.Pow(z1, 2) - 2 * Math.Pow(C0, 2) * Math.Pow(y0, 2) * z1 * z2 + Math.Pow(C0, 2) * Math.Pow(y0, 2) * Math.Pow(z2, 2) - 2 * Math.Pow(C0, 2) * y0 * y1 * z0 * z1 + 2 * Math.Pow(C0, 2) * y0 * y1 * z0 * z2 + 2 * Math.Pow(C0, 2) * y0 * y1 * z1 * z2 - 2 * Math.Pow(C0, 2) * y0 * y1 * Math.Pow(z2, 2) + 2 * Math.Pow(C0, 2) * y0 * y2 * z0 * z1 - 2 * Math.Pow(C0, 2) * y0 * y2 * z0 * z2 - 2 * Math.Pow(C0, 2) * y0 * y2 * Math.Pow(z1, 2) + 2 * Math.Pow(C0, 2) * y0 * y2 * z1 * z2 + Math.Pow(C0, 2) * Math.Pow(y1, 2) * Math.Pow(z0, 2) - 2 * Math.Pow(C0, 2) * Math.Pow(y1, 2) * z0 * z2 + Math.Pow(C0, 2) * Math.Pow(y1, 2) * Math.Pow(z2, 2) - 2 * Math.Pow(C0, 2) * y1 * y2 * Math.Pow(z0, 2) + 2 * Math.Pow(C0, 2) * y1 * y2 * z0 * z1 + 2 * Math.Pow(C0, 2) * y1 * y2 * z0 * z2 - 2 * Math.Pow(C0, 2) * y1 * y2 * z1 * z2 + Math.Pow(C0, 2) * Math.Pow(y2, 2) * Math.Pow(z0, 2) - 2 * Math.Pow(C0, 2) * Math.Pow(y2, 2) * z0 * z1 + Math.Pow(C0, 2) * Math.Pow(y2, 2) * Math.Pow(z1, 2)), 1 / 2) * (A0 * x0 * y1 - A0 * x1 * y0 - A0 * x0 * y2 + A0 * x2 * y0 + A0 * x1 * y2 - A0 * x2 * y1 - C0 * y0 * z1 + C0 * y1 * z0 + C0 * y0 * z2 - C0 * y2 * z0 - C0 * y1 * z2 + C0 * y2 * z1));
                    PoICoords2.Add(z0 - R0 * Math.Pow(1 / (Math.Pow(A0, 2) * Math.Pow(x0, 2) * Math.Pow(y1, 2) - 2 * Math.Pow(A0, 2) * Math.Pow(x0, 2) * y1 * y2 + Math.Pow(A0, 2) * Math.Pow(x0, 2) * Math.Pow(y2, 2) + Math.Pow(A0, 2) * Math.Pow(x0, 2) * Math.Pow(z1, 2) - 2 * Math.Pow(A0, 2) * Math.Pow(x0, 2) * z1 * z2 + Math.Pow(A0, 2) * Math.Pow(x0, 2) * Math.Pow(z2, 2) - 2 * Math.Pow(A0, 2) * x0 * x1 * y0 * y1 + 2 * Math.Pow(A0, 2) * x0 * x1 * y0 * y2 + 2 * Math.Pow(A0, 2) * x0 * x1 * y1 * y2 - 2 * Math.Pow(A0, 2) * x0 * x1 * Math.Pow(y2, 2) - 2 * Math.Pow(A0, 2) * x0 * x1 * z0 * z1 + 2 * Math.Pow(A0, 2) * x0 * x1 * z0 * z2 + 2 * Math.Pow(A0, 2) * x0 * x1 * z1 * z2 - 2 * Math.Pow(A0, 2) * x0 * x1 * Math.Pow(z2, 2) + 2 * Math.Pow(A0, 2) * x0 * x2 * y0 * y1 - 2 * Math.Pow(A0, 2) * x0 * x2 * y0 * y2 - 2 * Math.Pow(A0, 2) * x0 * x2 * Math.Pow(y1, 2) + 2 * Math.Pow(A0, 2) * x0 * x2 * y1 * y2 + 2 * Math.Pow(A0, 2) * x0 * x2 * z0 * z1 - 2 * Math.Pow(A0, 2) * x0 * x2 * z0 * z2 - 2 * Math.Pow(A0, 2) * x0 * x2 * Math.Pow(z1, 2) + 2 * Math.Pow(A0, 2) * x0 * x2 * z1 * z2 + Math.Pow(A0, 2) * Math.Pow(x1, 2) * Math.Pow(y0, 2) - 2 * Math.Pow(A0, 2) * Math.Pow(x1, 2) * y0 * y2 + Math.Pow(A0, 2) * Math.Pow(x1, 2) * Math.Pow(y2, 2) + Math.Pow(A0, 2) * Math.Pow(x1, 2) * Math.Pow(z0, 2) - 2 * Math.Pow(A0, 2) * Math.Pow(x1, 2) * z0 * z2 + Math.Pow(A0, 2) * Math.Pow(x1, 2) * Math.Pow(z2, 2) - 2 * Math.Pow(A0, 2) * x1 * x2 * Math.Pow(y0, 2) + 2 * Math.Pow(A0, 2) * x1 * x2 * y0 * y1 + 2 * Math.Pow(A0, 2) * x1 * x2 * y0 * y2 - 2 * Math.Pow(A0, 2) * x1 * x2 * y1 * y2 - 2 * Math.Pow(A0, 2) * x1 * x2 * Math.Pow(z0, 2) + 2 * Math.Pow(A0, 2) * x1 * x2 * z0 * z1 + 2 * Math.Pow(A0, 2) * x1 * x2 * z0 * z2 - 2 * Math.Pow(A0, 2) * x1 * x2 * z1 * z2 + Math.Pow(A0, 2) * Math.Pow(x2, 2) * Math.Pow(y0, 2) - 2 * Math.Pow(A0, 2) * Math.Pow(x2, 2) * y0 * y1 + Math.Pow(A0, 2) * Math.Pow(x2, 2) * Math.Pow(y1, 2) + Math.Pow(A0, 2) * Math.Pow(x2, 2) * Math.Pow(z0, 2) - 2 * Math.Pow(A0, 2) * Math.Pow(x2, 2) * z0 * z1 + Math.Pow(A0, 2) * Math.Pow(x2, 2) * Math.Pow(z1, 2) + 2 * A0 * B0 * x0 * y0 * Math.Pow(z1, 2) - 4 * A0 * B0 * x0 * y0 * z1 * z2 + 2 * A0 * B0 * x0 * y0 * Math.Pow(z2, 2) - 2 * A0 * B0 * x0 * y1 * z0 * z1 + 2 * A0 * B0 * x0 * y1 * z0 * z2 + 2 * A0 * B0 * x0 * y1 * z1 * z2 - 2 * A0 * B0 * x0 * y1 * Math.Pow(z2, 2) + 2 * A0 * B0 * x0 * y2 * z0 * z1 - 2 * A0 * B0 * x0 * y2 * z0 * z2 - 2 * A0 * B0 * x0 * y2 * Math.Pow(z1, 2) + 2 * A0 * B0 * x0 * y2 * z1 * z2 - 2 * A0 * B0 * x1 * y0 * z0 * z1 + 2 * A0 * B0 * x1 * y0 * z0 * z2 + 2 * A0 * B0 * x1 * y0 * z1 * z2 - 2 * A0 * B0 * x1 * y0 * Math.Pow(z2, 2) + 2 * A0 * B0 * x1 * y1 * Math.Pow(z0, 2) - 4 * A0 * B0 * x1 * y1 * z0 * z2 + 2 * A0 * B0 * x1 * y1 * Math.Pow(z2, 2) - 2 * A0 * B0 * x1 * y2 * Math.Pow(z0, 2) + 2 * A0 * B0 * x1 * y2 * z0 * z1 + 2 * A0 * B0 * x1 * y2 * z0 * z2 - 2 * A0 * B0 * x1 * y2 * z1 * z2 + 2 * A0 * B0 * x2 * y0 * z0 * z1 - 2 * A0 * B0 * x2 * y0 * z0 * z2 - 2 * A0 * B0 * x2 * y0 * Math.Pow(z1, 2) + 2 * A0 * B0 * x2 * y0 * z1 * z2 - 2 * A0 * B0 * x2 * y1 * Math.Pow(z0, 2) + 2 * A0 * B0 * x2 * y1 * z0 * z1 + 2 * A0 * B0 * x2 * y1 * z0 * z2 - 2 * A0 * B0 * x2 * y1 * z1 * z2 + 2 * A0 * B0 * x2 * y2 * Math.Pow(z0, 2) - 4 * A0 * B0 * x2 * y2 * z0 * z1 + 2 * A0 * B0 * x2 * y2 * Math.Pow(z1, 2) - 2 * A0 * C0 * x0 * y0 * y1 * z1 + 2 * A0 * C0 * x0 * y0 * y1 * z2 + 2 * A0 * C0 * x0 * y0 * y2 * z1 - 2 * A0 * C0 * x0 * y0 * y2 * z2 + 2 * A0 * C0 * x0 * Math.Pow(y1, 2) * z0 - 2 * A0 * C0 * x0 * Math.Pow(y1, 2) * z2 - 4 * A0 * C0 * x0 * y1 * y2 * z0 + 2 * A0 * C0 * x0 * y1 * y2 * z1 + 2 * A0 * C0 * x0 * y1 * y2 * z2 + 2 * A0 * C0 * x0 * Math.Pow(y2, 2) * z0 - 2 * A0 * C0 * x0 * Math.Pow(y2, 2) * z1 + 2 * A0 * C0 * x1 * Math.Pow(y0, 2) * z1 - 2 * A0 * C0 * x1 * Math.Pow(y0, 2) * z2 - 2 * A0 * C0 * x1 * y0 * y1 * z0 + 2 * A0 * C0 * x1 * y0 * y1 * z2 + 2 * A0 * C0 * x1 * y0 * y2 * z0 - 4 * A0 * C0 * x1 * y0 * y2 * z1 + 2 * A0 * C0 * x1 * y0 * y2 * z2 + 2 * A0 * C0 * x1 * y1 * y2 * z0 - 2 * A0 * C0 * x1 * y1 * y2 * z2 - 2 * A0 * C0 * x1 * Math.Pow(y2, 2) * z0 + 2 * A0 * C0 * x1 * Math.Pow(y2, 2) * z1 - 2 * A0 * C0 * x2 * Math.Pow(y0, 2) * z1 + 2 * A0 * C0 * x2 * Math.Pow(y0, 2) * z2 + 2 * A0 * C0 * x2 * y0 * y1 * z0 + 2 * A0 * C0 * x2 * y0 * y1 * z1 - 4 * A0 * C0 * x2 * y0 * y1 * z2 - 2 * A0 * C0 * x2 * y0 * y2 * z0 + 2 * A0 * C0 * x2 * y0 * y2 * z1 - 2 * A0 * C0 * x2 * Math.Pow(y1, 2) * z0 + 2 * A0 * C0 * x2 * Math.Pow(y1, 2) * z2 + 2 * A0 * C0 * x2 * y1 * y2 * z0 - 2 * A0 * C0 * x2 * y1 * y2 * z1 + Math.Pow(B0, 2) * Math.Pow(x0, 2) * Math.Pow(y1, 2) - 2 * Math.Pow(B0, 2) * Math.Pow(x0, 2) * y1 * y2 + Math.Pow(B0, 2) * Math.Pow(x0, 2) * Math.Pow(y2, 2) - 2 * Math.Pow(B0, 2) * x0 * x1 * y0 * y1 + 2 * Math.Pow(B0, 2) * x0 * x1 * y0 * y2 + 2 * Math.Pow(B0, 2) * x0 * x1 * y1 * y2 - 2 * Math.Pow(B0, 2) * x0 * x1 * Math.Pow(y2, 2) + 2 * Math.Pow(B0, 2) * x0 * x2 * y0 * y1 - 2 * Math.Pow(B0, 2) * x0 * x2 * y0 * y2 - 2 * Math.Pow(B0, 2) * x0 * x2 * Math.Pow(y1, 2) + 2 * Math.Pow(B0, 2) * x0 * x2 * y1 * y2 + Math.Pow(B0, 2) * Math.Pow(x1, 2) * Math.Pow(y0, 2) - 2 * Math.Pow(B0, 2) * Math.Pow(x1, 2) * y0 * y2 + Math.Pow(B0, 2) * Math.Pow(x1, 2) * Math.Pow(y2, 2) - 2 * Math.Pow(B0, 2) * x1 * x2 * Math.Pow(y0, 2) + 2 * Math.Pow(B0, 2) * x1 * x2 * y0 * y1 + 2 * Math.Pow(B0, 2) * x1 * x2 * y0 * y2 - 2 * Math.Pow(B0, 2) * x1 * x2 * y1 * y2 + Math.Pow(B0, 2) * Math.Pow(x2, 2) * Math.Pow(y0, 2) - 2 * Math.Pow(B0, 2) * Math.Pow(x2, 2) * y0 * y1 + Math.Pow(B0, 2) * Math.Pow(x2, 2) * Math.Pow(y1, 2) + Math.Pow(B0, 2) * Math.Pow(y0, 2) * Math.Pow(z1, 2) - 2 * Math.Pow(B0, 2) * Math.Pow(y0, 2) * z1 * z2 + Math.Pow(B0, 2) * Math.Pow(y0, 2) * Math.Pow(z2, 2) - 2 * Math.Pow(B0, 2) * y0 * y1 * z0 * z1 + 2 * Math.Pow(B0, 2) * y0 * y1 * z0 * z2 + 2 * Math.Pow(B0, 2) * y0 * y1 * z1 * z2 - 2 * Math.Pow(B0, 2) * y0 * y1 * Math.Pow(z2, 2) + 2 * Math.Pow(B0, 2) * y0 * y2 * z0 * z1 - 2 * Math.Pow(B0, 2) * y0 * y2 * z0 * z2 - 2 * Math.Pow(B0, 2) * y0 * y2 * Math.Pow(z1, 2) + 2 * Math.Pow(B0, 2) * y0 * y2 * z1 * z2 + Math.Pow(B0, 2) * Math.Pow(y1, 2) * Math.Pow(z0, 2) - 2 * Math.Pow(B0, 2) * Math.Pow(y1, 2) * z0 * z2 + Math.Pow(B0, 2) * Math.Pow(y1, 2) * Math.Pow(z2, 2) - 2 * Math.Pow(B0, 2) * y1 * y2 * Math.Pow(z0, 2) + 2 * Math.Pow(B0, 2) * y1 * y2 * z0 * z1 + 2 * Math.Pow(B0, 2) * y1 * y2 * z0 * z2 - 2 * Math.Pow(B0, 2) * y1 * y2 * z1 * z2 + Math.Pow(B0, 2) * Math.Pow(y2, 2) * Math.Pow(z0, 2) - 2 * Math.Pow(B0, 2) * Math.Pow(y2, 2) * z0 * z1 + Math.Pow(B0, 2) * Math.Pow(y2, 2) * Math.Pow(z1, 2) + 2 * B0 * C0 * Math.Pow(x0, 2) * y1 * z1 - 2 * B0 * C0 * Math.Pow(x0, 2) * y1 * z2 - 2 * B0 * C0 * Math.Pow(x0, 2) * y2 * z1 + 2 * B0 * C0 * Math.Pow(x0, 2) * y2 * z2 - 2 * B0 * C0 * x0 * x1 * y0 * z1 + 2 * B0 * C0 * x0 * x1 * y0 * z2 - 2 * B0 * C0 * x0 * x1 * y1 * z0 + 2 * B0 * C0 * x0 * x1 * y1 * z2 + 2 * B0 * C0 * x0 * x1 * y2 * z0 + 2 * B0 * C0 * x0 * x1 * y2 * z1 - 4 * B0 * C0 * x0 * x1 * y2 * z2 + 2 * B0 * C0 * x0 * x2 * y0 * z1 - 2 * B0 * C0 * x0 * x2 * y0 * z2 + 2 * B0 * C0 * x0 * x2 * y1 * z0 - 4 * B0 * C0 * x0 * x2 * y1 * z1 + 2 * B0 * C0 * x0 * x2 * y1 * z2 - 2 * B0 * C0 * x0 * x2 * y2 * z0 + 2 * B0 * C0 * x0 * x2 * y2 * z1 + 2 * B0 * C0 * Math.Pow(x1, 2) * y0 * z0 - 2 * B0 * C0 * Math.Pow(x1, 2) * y0 * z2 - 2 * B0 * C0 * Math.Pow(x1, 2) * y2 * z0 + 2 * B0 * C0 * Math.Pow(x1, 2) * y2 * z2 - 4 * B0 * C0 * x1 * x2 * y0 * z0 + 2 * B0 * C0 * x1 * x2 * y0 * z1 + 2 * B0 * C0 * x1 * x2 * y0 * z2 + 2 * B0 * C0 * x1 * x2 * y1 * z0 - 2 * B0 * C0 * x1 * x2 * y1 * z2 + 2 * B0 * C0 * x1 * x2 * y2 * z0 - 2 * B0 * C0 * x1 * x2 * y2 * z1 + 2 * B0 * C0 * Math.Pow(x2, 2) * y0 * z0 - 2 * B0 * C0 * Math.Pow(x2, 2) * y0 * z1 - 2 * B0 * C0 * Math.Pow(x2, 2) * y1 * z0 + 2 * B0 * C0 * Math.Pow(x2, 2) * y1 * z1 + Math.Pow(C0, 2) * Math.Pow(x0, 2) * Math.Pow(z1, 2) - 2 * Math.Pow(C0, 2) * Math.Pow(x0, 2) * z1 * z2 + Math.Pow(C0, 2) * Math.Pow(x0, 2) * Math.Pow(z2, 2) - 2 * Math.Pow(C0, 2) * x0 * x1 * z0 * z1 + 2 * Math.Pow(C0, 2) * x0 * x1 * z0 * z2 + 2 * Math.Pow(C0, 2) * x0 * x1 * z1 * z2 - 2 * Math.Pow(C0, 2) * x0 * x1 * Math.Pow(z2, 2) + 2 * Math.Pow(C0, 2) * x0 * x2 * z0 * z1 - 2 * Math.Pow(C0, 2) * x0 * x2 * z0 * z2 - 2 * Math.Pow(C0, 2) * x0 * x2 * Math.Pow(z1, 2) + 2 * Math.Pow(C0, 2) * x0 * x2 * z1 * z2 + Math.Pow(C0, 2) * Math.Pow(x1, 2) * Math.Pow(z0, 2) - 2 * Math.Pow(C0, 2) * Math.Pow(x1, 2) * z0 * z2 + Math.Pow(C0, 2) * Math.Pow(x1, 2) * Math.Pow(z2, 2) - 2 * Math.Pow(C0, 2) * x1 * x2 * Math.Pow(z0, 2) + 2 * Math.Pow(C0, 2) * x1 * x2 * z0 * z1 + 2 * Math.Pow(C0, 2) * x1 * x2 * z0 * z2 - 2 * Math.Pow(C0, 2) * x1 * x2 * z1 * z2 + Math.Pow(C0, 2) * Math.Pow(x2, 2) * Math.Pow(z0, 2) - 2 * Math.Pow(C0, 2) * Math.Pow(x2, 2) * z0 * z1 + Math.Pow(C0, 2) * Math.Pow(x2, 2) * Math.Pow(z1, 2) + Math.Pow(C0, 2) * Math.Pow(y0, 2) * Math.Pow(z1, 2) - 2 * Math.Pow(C0, 2) * Math.Pow(y0, 2) * z1 * z2 + Math.Pow(C0, 2) * Math.Pow(y0, 2) * Math.Pow(z2, 2) - 2 * Math.Pow(C0, 2) * y0 * y1 * z0 * z1 + 2 * Math.Pow(C0, 2) * y0 * y1 * z0 * z2 + 2 * Math.Pow(C0, 2) * y0 * y1 * z1 * z2 - 2 * Math.Pow(C0, 2) * y0 * y1 * Math.Pow(z2, 2) + 2 * Math.Pow(C0, 2) * y0 * y2 * z0 * z1 - 2 * Math.Pow(C0, 2) * y0 * y2 * z0 * z2 - 2 * Math.Pow(C0, 2) * y0 * y2 * Math.Pow(z1, 2) + 2 * Math.Pow(C0, 2) * y0 * y2 * z1 * z2 + Math.Pow(C0, 2) * Math.Pow(y1, 2) * Math.Pow(z0, 2) - 2 * Math.Pow(C0, 2) * Math.Pow(y1, 2) * z0 * z2 + Math.Pow(C0, 2) * Math.Pow(y1, 2) * Math.Pow(z2, 2) - 2 * Math.Pow(C0, 2) * y1 * y2 * Math.Pow(z0, 2) + 2 * Math.Pow(C0, 2) * y1 * y2 * z0 * z1 + 2 * Math.Pow(C0, 2) * y1 * y2 * z0 * z2 - 2 * Math.Pow(C0, 2) * y1 * y2 * z1 * z2 + Math.Pow(C0, 2) * Math.Pow(y2, 2) * Math.Pow(z0, 2) - 2 * Math.Pow(C0, 2) * Math.Pow(y2, 2) * z0 * z1 + Math.Pow(C0, 2) * Math.Pow(y2, 2) * Math.Pow(z1, 2)), 1 / 2) * (A0 * x0 * z1 - A0 * x1 * z0 - A0 * x0 * z2 + A0 * x2 * z0 + A0 * x1 * z2 - A0 * x2 * z1 + B0 * y0 * z1 - B0 * y1 * z0 - B0 * y0 * z2 + B0 * y2 * z0 + B0 * y1 * z2 - B0 * y2 * z1));
                    // Creating vectors corresponding to the coordinates.
                    Vector3 Vector_1 = new Vector3(PoICoords1[0], PoICoords1[1], PoICoords1[2]); // [0] - x, [1] - y, [2] - z.
                    Vector3 Vector_2 = new Vector3(PoICoords2[0], PoICoords2[1], PoICoords2[2]); // [0] - x, [1] - y, [2] - z.
                    Vector3 Vector_min = new Vector3(0, 0, 0);
                    Vector3 Vector_max = new Vector3(0, 0, 0);
                    // Finding which point corresponds to min outflow. ONLY for "one in, two out".
                    List<double> PoICoordsmin = new List<double>(); // [0] - x, [1] - y, [2] - z. 
                    flux_out_min_val = flux_out.Min();
                    int i_min = 0;
                    int i_max = 0;
                    for (int i = 0; i < flux_out.Count; i++)
                    {
                        if (flux_out[i] == flux_out_min_val)
                        {
                            nodes_out_min.Add(nodes_out[i]);
                            i_min = i;
                        }
                    }
                    if (i_min == 0)
                    {
                        PoICoordsmin = PoICoords1;
                        Vector_min = Vector_1;
                        Vector_max = Vector_2;
                        i_max = 1;
                    }
                    if (i_min == 1)
                    {
                        PoICoordsmin = PoICoords2;
                        Vector_min = Vector_2;
                        Vector_max = Vector_1;
                        i_max = 0;
                    }
                    // Calculating velocity_sum_max.
                    velocity_sum_max = nodes_in[0].velocity * nodes_in[0].lumen_area;
                    // Finding the angle of splitting of the inflow section.
                    double zeta_double;
                    int zeta, zeta_int, zeta_first, k;
                    double I, I0, I1, Iprev;
                    double x_sec_min, x_sec_max, y_sec_min, y_sec_max, phi_min;  // Coordinates of points of min and max on the circumference in section.
                    double h;
                    angle_step = Math.PI / 180;
                    velocity_in_p = 0;
                    Section Section_in = new Section(nodes_in[0].position, R0, nodes_in[0].dir_vector);
                    Section_in.x_axis = new Vector3(1, 0, 0);
                    Section_in.y_axis = new Vector3(0, 1, 0);
                    Vector3 Vector_sec_1 = Vector_1 - nodes_in[0].position;
                    Vector3 Vector_sec_2 = Vector_2 - nodes_in[0].position;
                    Vector3 Vector_sec_min = Vector_min - nodes_in[0].position;
                    Vector3 Vector_sec_max = Vector_max - nodes_in[0].position;
                    Vector3 x_sec_axis = new Vector3(1, 0, 0);
                    Vector3 y_sec_axis = new Vector3(0, 1, 0);
                    x_sec_min = Vector3.Dot(Vector_sec_min, x_sec_axis);
                    y_sec_min = Vector3.Dot(Vector_sec_min, y_sec_axis);
                    AngleofSpl = 0;
                    angle = 0;
                    area = 0;
                    velocity_sum = 0;
                    zeta_double = (GlobalDefs.FRICTION_C / 2) - 2;
                    zeta_int = (Int32)zeta_double;
                    while ((velocity_sum / (velocity_sum_max - velocity_sum) < (flux_out.Min() / flux_out.Max())))
                    {
                        area = (1.0 / 2.0) * Math.Pow(R0, 2) * (angle - Math.Sin(angle));
                        AngleofSpl = angle;
                        I0 = Math.Tan(angle / 2) - Math.Tan(-(angle / 2));
                        I1 = Math.Sin(angle / 2) / Math.Pow(Math.Cos(angle / 2), 2) / 2 + Math.Log(Math.Abs(Math.Tan((angle / 2) / 2 + Math.PI / 4))) / 2 - Math.Sin(-(angle / 2)) / Math.Pow(Math.Cos(-(angle / 2)), 2) / 2 - Math.Log(Math.Abs(Math.Tan(-(angle / 2) / 2 + Math.PI / 4))) / 2;
                        zeta_first = 0;
                        I = 0;
                        if (zeta_int == 0)
                        {
                            I = I0;
                        }
                        if (zeta_int == 1)
                        {
                            I = I1;
                        }
                        if ((zeta_int % 2 == 0) && (zeta_int != 0))
                        {
                            I = I0;
                            zeta_first = 2;
                        }
                        if ((zeta_int % 2 != 0) && (zeta_int != 1))
                        {
                            I = I1;
                            zeta_first = 3;
                        }
                        if ((zeta_first == 2) || (zeta_first == 3))
                        {
                            for (zeta = zeta_first; zeta != zeta_int; zeta = zeta + 2)
                            {
                                Iprev = I;
                                I = Math.Sin(angle / 2) * Math.Pow(Math.Cos(angle / 2), 1 - (zeta + 2)) / ((zeta + 2) - 1) - Math.Sin(-(angle / 2)) * Math.Pow(Math.Cos(-(angle / 2)), 1 - (zeta + 2)) / ((zeta + 2) - 1) + ((zeta + 2) - 2) / ((zeta + 2) - 1) * Iprev;
                            }
                        }  
                        velocity_sum = Math.Pow(R0, 2) * nodes_in[0].velocity * Math.Pow(Math.Cos(AngleofSpl / 2), zeta_double + 2) / zeta_double * I - Math.Pow(R0, 2) * nodes_in[0].velocity * (zeta_double + 2) / (2 * zeta_double) * Math.Sin(AngleofSpl) + Math.Pow(R0, 2) * nodes_in[0].velocity / 2 * AngleofSpl;
                        angle = angle + angle_step;
                    }
                    area_min = area;
                    area_max = nodes_in[0].lumen_area - area_min;
                    h = R0 * (1 - Math.Cos(AngleofSpl / 2));
                    // Finding phi_1 and phi_2.
                    phi_min = Math.Atan(y_sec_min / x_sec_min);
                    phi_1 = phi_min - AngleofSpl / 2;
                    phi_2 = phi_min + AngleofSpl / 2;
                    // Finding x_1, y_1 and x_2, y_2.
                    x_1 = R0 * Math.Cos(phi_1);
                    y_1 = R0 * Math.Sin(phi_1);
                    x_2 = R0 * Math.Cos(phi_2);
                    y_2 = R0 * Math.Sin(phi_2);
                    // Calculating substance volume going out. ONLY for some case.
                    /*             double agent_sum_min_part, agent_sum_max_part;
                    double S, alpha, x_bias, y_bias;
                    S = nodes_in[0].agent_c;
                    alpha = nodes_in[0].agent_shape;
                    x_bias = nodes_in[0].agent_xbias;
                    y_bias = nodes_in[0].agent_ybias;
                    if ((y_1 >= 0) && (y_2 >= 0))
                    {
 // Wrong:                       agent_sum_min_part = (S * (6 * b * x_1 - 6 * b * x_2 - 3 * x_1 * Math.Pow(Math.Pow(R0, 2) - Math.Pow(x_1, 2), 1 / 2) + 3 * x_2 * Math.Pow(Math.Pow(R0, 2) - Math.Pow(x_2, 2), 1 / 2) + 3 * a * Math.Pow(x_1, 2) - 3 * a * Math.Pow(x_2, 2) - 3 * Math.Pow(R0, 2) * Math.Asin(x_1 / R0) + 3 * Math.Pow(R0, 2) * Math.Asin(x_2 / R0) - 4 * alpha * Math.Pow(x_1, 3) * y_bias + 4 * alpha * Math.Pow(x_2, 3) * y_bias - 2 * alpha * Math.Pow(x_1, 3) * Math.Pow(Math.Pow(R0, 2) - Math.Pow(x_1, 2), 1 / 2) + 2 * alpha * Math.Pow(x_2, 3) * Math.Pow(Math.Pow(R0, 2) - Math.Pow(x_2, 2), 1 / 2) + Math.Pow(a, 3) * alpha * Math.Pow(x_1, 4) - Math.Pow(a, 3) * alpha * Math.Pow(x_2, 4) + 3 * a * alpha * Math.Pow(x_1, 4) - 3 * a * alpha * Math.Pow(x_2, 4) + 4 * alpha * b * Math.Pow(x_1, 3) + 4 * alpha * Math.Pow(b, 3) * x_1 - 4 * alpha * b * Math.Pow(x_2, 3) - 4 * alpha * Math.Pow(b, 3) * x_2 - 6 * Math.Pow(R0, 2) * alpha * Math.Pow(x_bias, 2) * Math.Asin(x_1 / R0) + 6 * Math.Pow(R0, 2) * alpha * Math.Pow(x_bias, 2) * Math.Asin(x_2 / R0) - 6 * Math.Pow(R0, 2) * alpha * Math.Pow(y_bias, 2) * Math.Asin(x_1 / R0) + 6 * Math.Pow(R0, 2) * alpha * Math.Pow(y_bias, 2) * Math.Asin(x_2 / R0) - 6 * Math.Pow(R0, 2) * alpha * b * x_1 + 6 * Math.Pow(R0, 2) * alpha * b * x_2 + 12 * Math.Pow(R0, 2) * alpha * x_1 * y_bias - 12 * Math.Pow(R0, 2) * alpha * x_2 * y_bias - 8 * a * alpha * Math.Pow(x_1, 3) * x_bias + 8 * a * alpha * Math.Pow(x_2, 3) * x_bias + 12 * alpha * b * x_1 * Math.Pow(x_bias, 2) - 12 * alpha * b * Math.Pow(x_1, 2) * x_bias - 12 * alpha * b * x_2 * Math.Pow(x_bias, 2) + 12 * alpha * b * Math.Pow(x_2, 2) * x_bias + 12 * alpha * b * x_1 * Math.Pow(y_bias, 2) - 12 * alpha * Math.Pow(b, 2) * x_1 * y_bias - 12 * alpha * b * x_2 * Math.Pow(y_bias, 2) + 12 * alpha * Math.Pow(b, 2) * x_2 * y_bias + 2 * Math.Pow(R0, 2) * alpha * x_1 * Math.Pow(Math.Pow(R0, 2) - Math.Pow(x_1, 2), 1 / 2) - 2 * Math.Pow(R0, 2) * alpha * x_2 * Math.Pow(Math.Pow(R0, 2) - Math.Pow(x_2, 2), 1 / 2) - 8 * Math.Pow(R0, 2) * alpha * x_bias * Math.Pow(Math.Pow(R0, 2) - Math.Pow(x_1, 2), 1 / 2) + 8 * Math.Pow(R0, 2) * alpha * x_bias * Math.Pow(Math.Pow(R0, 2) - Math.Pow(x_2, 2), 1 / 2) - 3 * Math.Pow(R0, 2) * a * alpha * Math.Pow(x_1, 2) + 3 * Math.Pow(R0, 2) * a * alpha * Math.Pow(x_2, 2) - 6 * alpha * x_1 * Math.Pow(x_bias, 2) * Math.Pow(Math.Pow(R0, 2) - Math.Pow(x_1, 2), 1 / 2) + 8 * alpha * Math.Pow(x_1, 2) * x_bias * Math.Pow(Math.Pow(R0, 2) - Math.Pow(x_1, 2), 1 / 2) + 6 * alpha * x_2 * Math.Pow(x_bias, 2) * Math.Pow(Math.Pow(R0, 2) - Math.Pow(x_2, 2), 1 / 2) - 8 * alpha * Math.Pow(x_2, 2) * x_bias * Math.Pow(Math.Pow(R0, 2) - Math.Pow(x_2, 2), 1 / 2) - 6 * alpha * x_1 * Math.Pow(y_bias, 2) * Math.Pow(Math.Pow(R0, 2) - Math.Pow(x_1, 2), 1 / 2) + 6 * alpha * x_2 * Math.Pow(y_bias, 2) * Math.Pow(Math.Pow(R0, 2) - Math.Pow(x_2, 2), 1 / 2) + 6 * a * alpha * Math.Pow(b, 2) * Math.Pow(x_1, 2) - 6 * a * alpha * Math.Pow(b, 2) * Math.Pow(x_2, 2) + 4 * Math.Pow(a, 2) * alpha * b * Math.Pow(x_1, 3) - 4 * Math.Pow(a, 2) * alpha * b * Math.Pow(x_2, 3) + 6 * a * alpha * Math.Pow(x_1, 2) * Math.Pow(x_bias, 2) - 6 * a * alpha * Math.Pow(x_2, 2) * Math.Pow(x_bias, 2) + 6 * a * alpha * Math.Pow(x_1, 2) * Math.Pow(y_bias, 2) - 6 * a * alpha * Math.Pow(x_2, 2) * Math.Pow(y_bias, 2) - 4 * Math.Pow(a, 2) * alpha * Math.Pow(x_1, 3) * y_bias + 4 * Math.Pow(a, 2) * alpha * Math.Pow(x_2, 3) * y_bias - 12 * a * alpha * b * Math.Pow(x_1, 2) * y_bias + 12 * a * alpha * b * Math.Pow(x_2, 2) * y_bias)) / 6;
                    }
                    if ((y_1 < 0) && (y_2 < 0))
                    {
                    }   */
                    double step; // The step of integration - the distance between two points of the section. All section is divided on squares. The points are the centers of the squares.
                    double x_sec, y_sec, r_sec, phi_sec;
                    double agent_c_sum_in_min, agent_c_sum_in_max;
                    double volume_ag_in_min = 0; 
                    double volume_ag_in_max = 0; 
                    double volume_ag_out_min = 0; 
                    double volume_ag_out_max = 0; 
                    double volume_out_min = 0; 
                    double volume_out_max = 0; 
                    bool check_in = true; // The variable for checking if the point is into the circumference (and not on the boundary).
                    step = R0 / 10;
                    x_sec = -step / 2;
                    y_sec = -R0 + step / 2;
                    agent_c_sum_in_min = 0;
                    agent_c_sum_in_max = 0;
                    while (y_sec <= R0)
                    {
                        check_in = true;
                        while (check_in == true)
                        {
                            x_sec = x_sec - step;
                            if (Math.Pow(x_sec, 2) + Math.Pow(y_sec, 2) >= Math.Pow(R0, 2))
                            {
                                check_in = false;
                            }
                        }
                        x_sec = x_sec + step;
                        check_in = true;
                        r_sec = Math.Sqrt(Math.Pow(x_sec, 2) + Math.Pow(y_sec, 2));
                        phi_sec = Math.Atan(y_sec / x_sec) + Math.PI;
                        while (check_in == true)
                        {
                            if ((phi_sec > phi_1) && (phi_sec < phi_2) && (r_sec < R0) && (r_sec > (R0 - h) / Math.Cos((phi_sec - phi_min))))
                            {
                                agent_c_sum_in_min = agent_c_sum_in_min + nodes_in[0].calcAgent_cInSectionPoint(nodes_in[0], x_sec, y_sec) * Math.Pow(step, 2);
                            }
                            else
                            {
                                agent_c_sum_in_max = agent_c_sum_in_max + nodes_in[0].calcAgent_cInSectionPoint(nodes_in[0], x_sec, y_sec) * Math.Pow(step, 2);
                            }
                            x_sec = x_sec + step;
                            r_sec = Math.Sqrt(Math.Pow(x_sec, 2) + Math.Pow(y_sec, 2));
                            if (x_sec < 0)
                            {
                                phi_sec = Math.Atan(y_sec / x_sec) + Math.PI;
                            }
                            if (x_sec >= 0)
                            {
                                phi_sec = Math.Atan(y_sec / x_sec);
                            }
                            if (Math.Pow(x_sec, 2) + Math.Pow(y_sec, 2) >= Math.Pow(R0, 2))
                            {
                                check_in = false;
                            }
                        }
                        x_sec = -step / 2;
                        y_sec = y_sec + step;
                    }
                    volume_ag_in_min = agent_c_sum_in_min * velocity_sum / area_min * Program.TIMESTEP;  
                    volume_ag_in_max = agent_c_sum_in_max * (velocity_sum_max - velocity_sum) / area_max * Program.TIMESTEP;   
                    volume_ag_out_min = volume_ag_in_min;
                    volume_ag_out_max = volume_ag_in_max;
                    volume_out_min = Math.Abs((nodes_out[i_min].velocity / v_sign[i_min])) * nodes_out[i_min].lumen_area * Program.TIMESTEP; // ONLY for flow going out
                    volume_out_max = Math.Abs((nodes_out[i_max].velocity / v_sign[i_max])) * nodes_out[i_max].lumen_area * Program.TIMESTEP; // ONLY for flow going out
                    nodes_out[i_min].agent_c = volume_ag_in_min / volume_out_min;
                    nodes_out[i_max].agent_c = volume_ag_in_max / volume_out_max;
                    nodes_out[i_min].agent_shape = 0;
                    nodes_out[i_max].agent_shape = 0;
                    nodes_out[i_min].agent_xbias = 0;
                    nodes_out[i_max].agent_xbias = 0;
                    nodes_out[i_min].agent_ybias = 0;
                    nodes_out[i_max].agent_ybias = 0;   
                    // Writing calculated values to the vnet nodes. ONLY for some case.
                    int j;
                    j = index_in[0];
                    nodes[j].agent_c = nodes_in[0].agent_c;
                    nodes[j].agent_shape = nodes_in[0].agent_shape;
                    nodes[j].agent_xbias = nodes_in[0].agent_xbias;
                    nodes[j].agent_ybias = nodes_in[0].agent_ybias;
                    j = index_out[0];
                    nodes[j].agent_c = nodes_out[0].agent_c;
                    nodes[j].agent_shape = nodes_out[0].agent_shape;
                    nodes[j].agent_xbias = nodes_out[0].agent_xbias;
                    nodes[j].agent_ybias = nodes_out[0].agent_ybias;
                    j = index_out[1];
                    nodes[j].agent_c = nodes_out[1].agent_c;
                    nodes[j].agent_shape = nodes_out[1].agent_shape;
                    nodes[j].agent_xbias = nodes_out[1].agent_xbias;
                    nodes[j].agent_ybias = nodes_out[1].agent_ybias;
                    // Calculating agent_c in core_node.
                    double volume_sum = 0; 
                    double volume_ag_sum = 0; 
                    double agent_c_av;
                    for (int i = 0; i < L; i++)
                    {
                        agent_c[i] = nodes[i].agent_c;
                        volume_ag_sum += agent_c[i] * lumen_area[i] * Math.Abs(nodes[i].velocity);
                        volume_sum += lumen_area[i] * Math.Abs(nodes[i].velocity);
                    }
                    if (volume_sum == 0)
                    {
                        core_node.agent_c = 0;
                    }
                    else
                    {
                        agent_c_av = volume_ag_sum / volume_sum;
                        core_node.agent_c = agent_c_av;
                    }
                }
                // Clearing the temporary lists. 
                area_in.Clear();
                flux_in.Clear();
                nodes_in.Clear();
                index_in.Clear();
                area_out.Clear();
                flux_out.Clear();
                nodes_out.Clear();
                index_out.Clear();
                nodes_out_min.Clear();
            }  
        }     

        protected MDFunction[] chrt_func;
        protected MDFunction mass_conservation_func;
        protected MDFunction[] energy_conservation_func;

        protected MDFunction[] funcs;

        protected double[] next_neighbours_pressure;
        protected VascularNode[] next_neighbours;
        protected int[] next_neighbours_v_sign;

        protected double[] prev_velocity;
        protected double[] wall_thickhess;
        protected double dt;

        protected double[] beta_1;
        protected double[] c_dst;
        protected double[] dX;
        protected double[] g_energy;

        protected double[] chrt_b;
        protected double[] chrt_f;

        protected bool[,] dep_matrix;

        protected NewtonSolver nl_system;

        unsafe protected double* us_init_X; 
        unsafe protected double* us_solution;
    }

    public class
        ViscoElasticKnot : Knot
    {
        public ViscoElasticKnot(Knot _knot, GetBetaFunction getElasticBeta)
            : base(_knot.core_node, _knot.current_time)
        {
            int L = nodes.GetLength(0);
            lumen_area_old = new double[L];
            chrt_func = new MDFunction[L];
            energy_conservation_func = new MDFunction[L - 1];
            funcs = new MDFunction[2 * L];

            wall_thickhess = new double[L];
            lumen_area_0 = new double[L];

            beta_1 = new double[L];
            chrt_b = new double[L];
            chrt_f = new double[L];
            c_dst = new double[L];
            dep_matrix = new bool[2 * L, 2 * L];
            prev_velocity = new double[L];

            nl_system = new NewtonSolver(2 * L);


            for (int i = 0; i < L; i++)
                for (int j = 0; j < L; j++)
                    dep_matrix[i, j] = false;

            for (int i = 0; i < L; i++)
            {
                double R0 = Math.Sqrt(nodes[i].lumen_area_0 / Math.PI);
                beta_1[i] = getElasticBeta(R0) / nodes[i].lumen_area_0;
                wall_thickhess[i] = GlobalDefs.getBoileauWallThickness(R0);
                lumen_area_0[i] = nodes[i].lumen_area_0;
                prev_velocity[i] = nodes[i].velocity;
                chrt_b[i] = 0;
                chrt_f[i] = 0;               
                c_dst[i] = Math.Pow(nodes[i].lumen_area_0, 0.25f) * Math.Sqrt(beta_1[i] / 2.0f /  GlobalDefs.BLOOD_DENSITY);
                lumen_area_old[i] = lumen_area_0[i];
            }

            for (int i = 0; i < L; i++)            
                pressure[i] = GlobalDefs.DIASTOLIC_PRESSURE;

            

            int count = 0;
            unsafe
            {
                for (int i = 0; i < L; i++)
                {
                    int I = i;
                    MDFunction_del f1_del = delegate(double* args)
                    {
                        double v = args[0 + I * 2];
                        double l = args[1 + I * 2];

                        if (v > 0)
                            return Math.Abs(v) + 4 * (Math.Sqrt(Math.Sqrt(l)) * Math.Sqrt(beta_1[I] / 2.0f /  GlobalDefs.BLOOD_DENSITY) - c_dst[I]) - chrt_f[I];
                        else
                            return Math.Abs(v) - 4 * (Math.Sqrt(Math.Sqrt(l)) * Math.Sqrt(beta_1[I] / 2.0f /  GlobalDefs.BLOOD_DENSITY) - c_dst[I]) - chrt_b[I];
                    };
                    baseMDFunction f1 = new delegateMDFunc(f1_del);

                    chrt_func[i] = delegate(double[] args) //v1,l1; v2,l2 ...
                    {
                        double v = args[0 + I * 2];
                        double l = args[1 + I * 2];

                        if (v > 0)
                            return Math.Abs(v) + 4 * (Math.Pow(l, 0.25f) * Math.Sqrt(beta_1[I] / 2.0f /  GlobalDefs.BLOOD_DENSITY) - c_dst[I]) - chrt_f[I];
                        else
                            return Math.Abs(v) - 4 * (Math.Pow(l, 0.25f) * Math.Sqrt(beta_1[I] / 2.0f /  GlobalDefs.BLOOD_DENSITY) - c_dst[I]) - chrt_b[I];
                    };

                    nl_system.addFunc(f1);
                    funcs[count] = chrt_func[i];

                    dep_matrix[count, 2 * I] = true;
                    dep_matrix[count, 2 * I + 1] = true;
                    nl_system.setDetMatrixEl(count, 2 * I, true);
                    nl_system.setDetMatrixEl(count, 2 * I + 1, true);

                    count++;
                }
            }

            unsafe
            {
                MDFunction_del f1_del = delegate(double* args)
                {
                    double summ_flux = 0;
                    for (int i = 0; i < L; i++)
                        summ_flux += args[0 + i * 2] * args[1 + i * 2];

                    return summ_flux;
                };
                baseMDFunction f1 = new delegateMDFunc(f1_del);


                mass_conservation_func = delegate(double[] args)
                {
                    double summ_flux = 0;
                    for (int i = 0; i < L; i++)
                    {
                        double v = args[0 + i * 2];
                        double l = args[1 + i * 2];

                        summ_flux += v * l;
                    }
                    return summ_flux;
                };


                funcs[count] = mass_conservation_func;
                for (int i = 0; i < 2 * L; i++)
                {
                    dep_matrix[count, i] = true;
                    nl_system.setDetMatrixEl(count, i, true);
                }

                nl_system.addFunc(f1);
            };

            count++;

            unsafe
            {
                for (int i = 1; i < L; i++)
                {
                    int I = i;
                    MDFunction_del f1_del = delegate(double* args)
                    {
                        double v0 = args[0];
                        double p0 = calcPressureV(0, args[1]);

                        double v = args[0 + I * 2];
                        double p = calcPressureV(I, args[1 + 2 * I]); 

                        return  GlobalDefs.BLOOD_DENSITY * (v0 * v0 - v * v) / 2 + p0 - p;
                    };
                    baseMDFunction f1 = new delegateMDFunc(f1_del);

                    energy_conservation_func[i - 1] = delegate(double[] args)
                    {
                        double v0 = args[0];
                        double p0 = calcPressureV(0, args[1]);

                        double v = args[0 + I * 2];
                        double p = calcPressureV(I, args[1 + 2 * I]);
                            
                        return  GlobalDefs.BLOOD_DENSITY * (v0 * v0 - v * v) / 2 + p0 - p;
                    };

                    nl_system.addFunc(f1);
                    funcs[count] = energy_conservation_func[I - 1];

                    dep_matrix[count, 0] = true;
                    dep_matrix[count, 1] = true;
                    nl_system.setDetMatrixEl(count, 0, true);
                    nl_system.setDetMatrixEl(count, 1, true);

                    dep_matrix[count, 2 * I] = true;
                    dep_matrix[count, 2 * I + 1] = true;

                    nl_system.setDetMatrixEl(count, 2 * I, true);
                    nl_system.setDetMatrixEl(count, 2 * I + 1, true);

                    count++;
                }

                us_init_X = (double*)Marshal.AllocHGlobal(2 * L * sizeof(double));
                us_solution = (double*)Marshal.AllocHGlobal(2 * L * sizeof(double));

                for (int i = 0; i < 2 * L; i += 2)
                {
                    us_init_X[i] = nodes[i / 2].velocity * v_sign[i / 2];
                    us_init_X[i + 1] = nodes[i / 2].lumen_area;
                }
            }

            dX = new double[2 * L];
            for (int i = 0; i < 2 * L; i += 2)
            {
                dX[i] = 1e-12f;
                dX[i + 1] = 1e-12f;
                nl_system.setDxVectorEl(i, 1e-12f);
                nl_system.setDxVectorEl(i + 1, 1e-12f);
            }
            
        }
        

        unsafe public override void doCoupling(double dt)
        {
            curr_dt = dt;
            current_time = current_time + dt;
            previous_time = current_time;
            int L = nodes.GetLength(0);

            if (core_node.id == 0)
                L = nodes.GetLength(0);
                        
            unsafe
            {
                for (int i = 0; i < 2 * L; i += 2)
                {
                    us_init_X[i    ] = 1.5*nodes[i / 2].velocity * v_sign[i / 2] - 0.5*us_init_X[i];
                    us_init_X[i + 1] = 1.5*nodes[i / 2].lumen_area - 0.5*us_init_X[i + 1];
                    
                    double wave_speed = 4 * (Math.Sqrt(Math.Sqrt(nodes[i / 2].lumen_area)) * Math.Sqrt(beta_1[i / 2] / 2.0f / GlobalDefs.BLOOD_DENSITY) - c_dst[i / 2]);
                    chrt_f[i / 2] = Math.Abs(nodes[i / 2].velocity) + wave_speed;
                    chrt_b[i / 2] = Math.Abs(nodes[i / 2].velocity) - wave_speed;
                }

                nl_system.solve(us_init_X, 1e-6, us_solution);

                double av_pressure = 0;
                double av_flux_in = 0;
                double av_flux_out = 0;
                double av_lumen_in = 0;
                double av_lumen_out = 0;

                for (int i = 0; i < 2 * L; i += 2)
                {
                    nodes[i / 2].velocity = us_solution[i] * v_sign[i / 2];
                    lumen_area_old[i / 2] = nodes[i / 2].lumen_area;
                    nodes[i / 2].lumen_area = us_solution[i + 1];
                    nodes[i / 2].pressure = calcPressureV(i / 2, nodes[i / 2].lumen_area);

                    av_pressure += nodes[i / 2].pressure;
                    if (us_solution[i] >= 0)
                    {
                        av_flux_in += Math.Abs(us_solution[i + 1] * us_solution[i]);
                        av_lumen_in += us_solution[i + 1];
                    }
                    else
                    {
                        av_flux_out += Math.Abs(us_solution[i + 1] * us_solution[i]);
                        av_lumen_out += us_solution[i + 1];
                    }
                }

                core_node.pressure = av_pressure / L;
                core_node.lumen_area = av_lumen_in;
                core_node.velocity = av_flux_in / av_lumen_in;
            }


            core_node.velocity = core_node.neighbours.Last().velocity;
            core_node.lumen_area = core_node.neighbours.Last().lumen_area;
        }

        public override void reset()
        {
            int L = nodes.GetLength(0);
            for (int i = 0; i < L; i++)
            {
                velocity[i] = 0;
                pressure[i] = GlobalDefs.DIASTOLIC_PRESSURE;                
                lumen_area[i] = nodes[i].lumen_area_0;
                lumen_area_old[i] = nodes[i].lumen_area_0;
                nodes[i].velocity = velocity[i];
                nodes[i].pressure = pressure[i];
                nodes[i].lumen_area = nodes[i].lumen_area_0;
                nodes[i].agent_c = agent_c[i];
            }

            unsafe
            {
                for (int i = 0; i < 2 * L; i += 2)
                {
                    us_init_X[i] = nodes[i / 2].velocity * v_sign[i / 2];
                    us_init_X[i + 1] = nodes[i / 2].lumen_area;
                }
            }
        }

        protected double calcPressureV(int i, double _lumen_area)
        {
            double Gamma = 2.0 / 3.0 * Math.Sqrt(Math.PI) * wall_thickhess[i] * GlobalDefs.phi;
            return beta_1[i] * (Math.Sqrt(_lumen_area) - Math.Sqrt(lumen_area_0[i])) + GlobalDefs.DIASTOLIC_PRESSURE + Gamma / lumen_area_0[i] / Math.Sqrt(_lumen_area) * (_lumen_area - lumen_area_old[i]) / curr_dt;
        }

        protected double[] lumen_area_old;
        double curr_dt;

        protected MDFunction[] chrt_func;
        protected MDFunction mass_conservation_func;
        protected MDFunction[] energy_conservation_func;

        protected MDFunction[] funcs;

        protected double[] next_neighbours_pressure;
        protected VascularNode[] next_neighbours;
        protected int[] next_neighbours_v_sign;

        protected double[] prev_velocity;
        protected double[] wall_thickhess;
        protected double dt;

        protected double[] beta_1;
        protected double[] c_dst;
        protected double[] dX;

        protected double[] chrt_b;
        protected double[] chrt_f;

        protected bool[,] dep_matrix;

        protected NewtonSolver nl_system;

        unsafe protected double* us_init_X  ; 
        unsafe protected double* us_solution; 
    }

   

}