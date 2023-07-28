using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace BloodFlow
{

    public struct Clot
    {
        public VascularNode c_nd;
        public List<VascularNode> nd;
        public int thread_id;
        public double tgr_degree;
        public double curr_degree;
        public List<double> normal_lumen;
    }

    public class FFRClot       
    {
        public FFRClot(double curr_time, double _alpha)
        {
            p_pressure_acc = 0;
            d_pressure_acc = 0;
            av_flux_acc = 0;
            last_time = curr_time;
            alpha = _alpha;
        }

        public void init()
        {
            p_pressure_acc = 0;
            d_pressure_acc = 0;
            av_flux_acc = 0;
            
            length = 0;
            for (int i = 1; i < nd.Count; i++)
                length += Vector3.Length(nd[i].position - nd[i - 1].position);

            effective_visc = GlobalDefs.BLOOD_VISC;
            tg_ffr = 0.5;
        }

        public bool update_clot(double curr_time, double heart_period)
        {
            double dt = curr_time - last_time;
            last_time = curr_time;

            p_pressure_acc += proximal_nd.pressure * dt;
            d_pressure_acc +=   distal_nd.pressure * dt;
            av_flux_acc += proximal_nd.velocity * proximal_nd.lumen_area * dt;

            if (curr_time % heart_period <= dt)
            {
                proximal_pressure = p_pressure_acc / heart_period;
                p_pressure_acc = 0;
                  distal_pressure = d_pressure_acc / heart_period;
                d_pressure_acc = 0;

                av_flux = Math.Abs(av_flux_acc);                
                av_flux_acc = 0;

                  if (proximal_pressure < distal_pressure)
                  {
                      VascularNode tmp = proximal_nd;
                      double tmp_p = proximal_pressure;
                      proximal_nd = distal_nd;
                      distal_nd = tmp;
                      proximal_pressure = distal_pressure;
                      distal_pressure = tmp_p;                      
                  }

                curr_ffr = distal_pressure / proximal_pressure;
                effective_visc = effective_visc * (1 + 4 * (curr_ffr - tg_ffr));
                
                //(1 - tg_ffr) / length / alpha / Math.PI * proximal_pressure / av_flux;
                

                return true;
            }

            return false;
        }

        public double   tg_ffr;
        public double curr_ffr;
        public double effective_visc;

        public List<VascularNode> nd;
        public VascularNode proximal_nd;
        public VascularNode   distal_nd;
        public int thread_id;
        public double proximal_pressure;
        public double   distal_pressure;        
        public double av_flux;

        protected double p_pressure_acc;
        protected double d_pressure_acc;
        protected double av_flux_acc;
        protected double length;
        protected double alpha;

        protected double last_time;
    }

    public class Thread
    {
        public Thread(List<VascularNode> protothread)
        {


            int L = protothread.Count;
            nodes = (VascularNode[])protothread.ToArray().Clone();
            v_sign = new int[L];

            velocity = new double[L];
            pressure = new double[L];
            lumen_area = new double[L];
            lumen_area_old = new double[L];
            flux = new double[L];
            chrt = new double[L];
            agent_c = new double[L];
            agent_shape = new double[L];
            agent_xbias = new double[L];
            agent_ybias = new double[L];

            DefineSigns();
            current_time = 0;
            
        }

        unsafe public void nodes2state()
        {
            int L = nodes.GetLength(0);
            for (int i = 0; i < L; i++)
            {
                velocity[i] = nodes[i].velocity * v_sign[i];
                pressure[i] = nodes[i].pressure;
                lumen_area[i] = nodes[i].lumen_area;
                agent_shape[i] = nodes[i].agent_shape;
                agent_c[i] = nodes[i].agent_c;
                agent_xbias[i] = nodes[i].agent_xbias;
                agent_ybias[i] = nodes[i].agent_ybias;
            }
        }

        public virtual void updateState()
        { }

        public virtual void updateStateFFR()
        { }

        unsafe public void state2nodes()
        {
            int L = nodes.GetLength(0);
            for (int i = 0; i < L; i++)
            {
                nodes[i].velocity = velocity[i] * v_sign[i];
                nodes[i].pressure = pressure[i];
                nodes[i].lumen_area = lumen_area[i];
                nodes[i].agent_c = agent_c[i];
                nodes[i].agent_shape = agent_shape[i];
                nodes[i].agent_xbias = agent_xbias[i];
                nodes[i].agent_ybias = agent_ybias[i];
            }
        }

        public int getLength()
        {
            return nodes.GetLength(0);
        }

        public virtual void calcThread(double dt)
        {
        }

        public virtual void reset()
        {
           
        }

        protected void DefineSigns()
        {
            int L = nodes.GetLength(0);  

            Vector3[] dir_vector1 = new Vector3[L];
            Vector3[] dir_vector2 = new Vector3[L];
            v_sign = new int[L];
            for (int i = 0; i < L - 1; i++)
            {
                dir_vector1[i] = (nodes[i + 1].position - nodes[i].position);
            }
            for (int i = 0; i < L; i++)
            {
                nodes[i].defDirVector();
                dir_vector2[i] = nodes[i].dir_vector;
            }

            if (L > 1)
                dir_vector1[L - 1] = nodes[L - 1].position - nodes[L - 2].position;
            else
                dir_vector1[L - 1] = dir_vector2[L - 1];

            for (int i = 0; i < L; i++)
                v_sign[i] = Math.Sign(Vector3.Dot(dir_vector1[i], dir_vector2[i]));
        }

        public void WriteThreadToFile(string file_path)
        {
            int id_count = 0;
            string outText = "Name:";
            outText += "System_0" + "\n";
            outText += "Coordinates:\n";
            foreach (var n in nodes)
            {
                n.id = id_count;
                outText += n.id + " X:" + n.position.x.ToString("F8") + " Y:" + n.position.y.ToString("F8") + " Z:" + n.position.z.ToString("F8") + " R:" + Math.Sqrt(n.lumen_area_0 / Math.PI).ToString("F8") + "\n";
                id_count++;
            }
            outText += "\nBonds:\n";
            foreach (var n in nodes)
            {
                outText += n.id + " ";
                foreach (var nn in n.neighbours)
                    if (nodes.Contains(nn))
                    {
                        outText += nn.id + " ";
                    }
                outText += "\n";
            }
            System.IO.File.WriteAllText(file_path, outText);
        }

        public virtual bool setClot(VascularNode nd, float degree)
        { return false; }

        public virtual bool setFFRClot(VascularNode nd, float FFR_degree)
        { return false; }

        public virtual void removeClot(VascularNode nd)
        { }

     /*   public void clearClotList()
        {
            clotes.Clear();
        }*/


        public VascularNode[] nodes;
        public int[] v_sign;

        public double[] lumen_area_old;

        public double[] velocity;
        public double[] pressure;
        public double[] lumen_area;
        public double[] flux;
        public double[] agent_c;
        public double[] agent_shape;
        public double[] agent_xbias;
        public double[] agent_ybias;

        public double[] chrt;

        public double current_time;
        protected List<Clot>     clotes;
        protected List<FFRClot>  ffr_clotes;

        public double diss_func_sum_1thr;
    }


    public class ElasticThread : Thread
    {
        public ElasticThread(Thread _protothread, GetBetaFunction getElsticBeta)
            : base(_protothread.nodes.ToList())
        {
            int L = nodes.GetLength(0);
            beta_1 = new double[L];
            lumen_area_0 = new double[L];
            wall_thickhess = new double[L];
            viscosity = new double[L];
            lumen_area_old = new double[L];
            g_energy = new double[L];

            for (int i = 0; i < L; i++)
            {

                double R0 = Math.Sqrt(nodes[i].lumen_area_0 / Math.PI);
                beta = getElsticBeta(R0);

                beta_1[i] = beta / nodes[i].lumen_area_0;
                wall_thickhess[i] = GlobalDefs.getBoileauWallThickness(R0);
                lumen_area_0[i] = nodes[i].lumen_area_0;
                lumen_area[i] = nodes[i].lumen_area_0;
                pressure[i] = GlobalDefs.DIASTOLIC_PRESSURE;
                lumen_area_old[i] = lumen_area_0[i];
                g_energy[i] = -Vector3.Dot(nodes[i].position - GlobalDefs.ZERO_POINT, GlobalDefs.DOWN) * GlobalDefs.GRAVITY;
                viscosity[i] = GlobalDefs.BLOOD_VISC;
            }

            unsafe
            {
                double* dZ = (double*)Marshal.AllocHGlobal((L - 1) * sizeof(double));
                for (int i = 0; i < L - 1; i++)                
                    dZ[i] = Vector3.Distance(nodes[i + 1].position, nodes[i].position);                    
                

                thread_solver = new McCormackThread(L, dZ, GlobalDefs.BLOOD_DENSITY, GlobalDefs.BLOOD_VISC);

                for (int i = 0; i < L; i++)
                {                    
                    int I = i;                    
                    _1DFunction_del pressure_func_del = delegate(double x)
                    {
                        return calcPressure(x, I);
                    };

                    delegate1DFunc f = new delegate1DFunc(pressure_func_del);

                    thread_solver.addFunc(f, i);
                }

                fixed (double* g_ptr = &g_energy[0])
                {
                    //thread_solver.setGravity(g_ptr);
                }
            }

            clotes = new List<Clot>();
            ffr_clotes = new List<FFRClot>();   
        }

        public override void reset()
        {
            int L = nodes.GetLength(0);

            foreach (var clt in clotes)
            {
                int CL = 2; int sh = 0;
                int nd_id = clt.thread_id;
                for (int j = -CL; j < CL; j++)
                    if (nd_id + j >= 0 && nd_id + j < nodes.GetLength(0))
                    {
                        clt.nd[j + CL + sh].lumen_area_0 = clt.normal_lumen[j + CL + sh];
                        lumen_area_0[nd_id + j] = clt.nd[j + CL + sh].lumen_area_0;
                    }
                    else
                        sh--;
            }
                

            for (int i = 0; i < L; i++)
            {
                velocity[i] = 0;
                pressure[i] = 0;                
                lumen_area[i] = lumen_area_0[i];
                lumen_area_old[i] = lumen_area_0[i];
                agent_c[i] = 0;
            }

            clotes.Clear();

            state2nodes();
        }


        public override bool setClot(VascularNode nd, float degree)
        {
            int nd_id = 0;
            for (int i = 0; i < nodes.GetLength(0); i++)
                if (nodes[i] == nd)
                { nd_id = i; break; }

            if (clotes.FindIndex(x => x.c_nd == nd) == -1)
            {

                Clot clt = new Clot();
                clt.c_nd = nd;

                clt.normal_lumen = new List<double>();
                clt.nd = new List<VascularNode>();

                int L = 2;

                for (int i = -L; i < L+1; i++)
                    if (nd_id + i >= 0 && nd_id + i < nodes.GetLength(0))
                    {
                        clt.nd.Add(nodes[nd_id + i]);
                        clt.normal_lumen.Add(lumen_area_0[nd_id + i]);
                    }

                clt.tgr_degree = degree;
                clt.curr_degree = 0;
                clt.thread_id = nd_id;
                clotes.Add(clt);
                return true;
            }

            return false;
        }


        public override bool setFFRClot(VascularNode nd, float FFR_degree)
        {
            int nd_id = 0;
            for (int i = 0; i < nodes.GetLength(0); i++)
                if (nodes[i] == nd)
                { nd_id = i; break; }

            if (clotes.FindIndex(x => x.c_nd == nd) == -1)
            {

                FFRClot clt = new FFRClot(current_time, GlobalDefs.FRICTION_C);
                
                clt.nd = new List<VascularNode>();
                int L = 2;
                if (nd_id - L - 1 >= 0)
                    clt.proximal_nd = nodes[nd_id - L - 1];
                else
                    return false;

                if (nd_id + L + 1 < nodes.GetLength(0))
                    clt.distal_nd = nodes[nd_id + L + 1];
                else
                    return false;                

                for (int i = -L; i < L + 1; i++)
                    if (nd_id + i >= 0 && nd_id + i < nodes.GetLength(0))
                    {                       
                        clt.nd.Add(nodes[nd_id + i]);                       
                    }

                clt.thread_id = nd_id;
                clt.init();
                ffr_clotes.Add(clt);
                return true;
            }

            return false;
        }


        public override void removeClot(VascularNode nd)
        {
            int clt_id = clotes.FindIndex(x => x.c_nd == nd);
            if (clt_id != -1)
            {
                Clot clt = clotes[clt_id];
                clt.tgr_degree = 0.0;
                clotes[clt_id] = clt;
            }
        }

        public override void updateState()
        {
            for (int i = 0; i < clotes.Count; i++)
            {
                Clot clt = clotes[i];               
                if (Math.Abs(clt.tgr_degree - clt.curr_degree) != 0.0)
                {
                    clt.curr_degree += Math.Sign(clt.tgr_degree - clt.curr_degree)*0.001f;

                    if (Math.Abs(clt.tgr_degree - clt.curr_degree) < 0.01)
                        clt.curr_degree = clt.tgr_degree;

                    int L = 2; int sh = 0;
                    int nd_id = clt.thread_id;
                    for (int j = -L; j < L+1; j++)
                    {
                        if (nd_id + j >= 0 && nd_id + j < nodes.GetLength(0))
                        {
                            double degree = clt.curr_degree * (L - Math.Abs(j) + 1) / (L + 1);
                            clt.nd[j + L + sh].lumen_area_0 = clt.normal_lumen[j + L + sh] * (1 - degree);                            
                            lumen_area_0[nd_id + j] = clt.nd[j + L + sh].lumen_area_0;
                            beta_1[nd_id + j] = this.beta / clt.nd[j + L + sh].lumen_area_0;
                        }
                        else
                            sh--;
                    }
                    clotes[i] = clt;
                }
                else
                {                    
                    if (clt.tgr_degree == 0.0)
                    {
                        clotes.RemoveAt(i);
                        break;
                    }
                }
            }
        }
     

        unsafe public override void calcThread(double dt)
        {
            current_time += dt;
            double dz = 0;
            curr_dt = dt;

            int L = nodes.GetLength(0);          

            this.nodes2state();

            /*
            unsafe
            {
                fixed (double* v_ptr = &velocity[0])
                {
                    fixed (double* lum_ptr = &lumen_area[0])
                    {
                        fixed (double* p_ptr = &pressure[0])
                        {
                            thread_solver.calc(v_ptr, lum_ptr, p_ptr, dt);
                        }
                    }
                }
            }
            */
       
            lumen_area_old = (double[])lumen_area.Clone();
            
            double[] velocity_pred = (double[])velocity.Clone();
            double[] lumen_area_pred = (double[])lumen_area.Clone();
            double[] pressure_pred = (double[])pressure.Clone();

            double diss_func_el;

            for (int i = 1; i < L-1; i++)
            {
                dz = Vector3.Distance(nodes[i].position, nodes[i - 1].position);
                velocity_pred[i] = velocity[i] - dt / dz * ((velocity[i] * velocity[i] / 2 + pressure[i] / GlobalDefs.BLOOD_DENSITY + g_energy[i]) - (velocity[i - 1] * velocity[i - 1] / 2 + pressure[i - 1] / GlobalDefs.BLOOD_DENSITY + g_energy[i-1]));
                velocity_pred[i] = velocity_pred[i] - 1.0 / GlobalDefs.BLOOD_DENSITY / lumen_area[i] * (GlobalDefs.FRICTION_C * viscosity[i] * Math.PI * velocity_pred[i]) * dt;
                lumen_area_pred[i] = lumen_area[i] - dt / dz * (lumen_area[i] * velocity[i] - lumen_area[i-1] * velocity[i-1]);
                pressure_pred[i] = calcPressure(lumen_area_pred[i], i);
            }           

            for (int i = 1; i < L-1; i++)
            {
                dz = Vector3.Distance(nodes[i].position, nodes[i + 1].position);
                velocity[i] = (velocity[i] + velocity_pred[i]) / 2 - dt / dz / 2 * ((velocity_pred[i + 1] * velocity_pred[i + 1] / 2 + pressure_pred[i + 1] / GlobalDefs.BLOOD_DENSITY + g_energy[i + 1]) - (velocity_pred[i] * velocity_pred[i] / 2 + pressure_pred[i] / GlobalDefs.BLOOD_DENSITY + g_energy[i]));
                velocity[i] = velocity[i] - 1.0 / 2.0 / GlobalDefs.BLOOD_DENSITY / lumen_area[i] * (GlobalDefs.FRICTION_C * viscosity[i] * Math.PI * velocity[i]) * dt;
                lumen_area[i] = (lumen_area[i] + lumen_area_pred[i]) / 2 - dt / dz / 2 * (lumen_area_pred[i + 1] * velocity_pred[i + 1] - lumen_area_pred[i] * velocity_pred[i]);
                pressure[i] = calcPressure(lumen_area[i], i);
            }

            if (L == 2)
            {
                velocity[0] = (velocity[0] + velocity[1]) / 2;
                velocity[1] = velocity[0];

                pressure[0] = (pressure[0] + pressure[1]) / 2;
                pressure[1] = pressure[0];

                lumen_area[0] = (lumen_area[0] + lumen_area[1]) / 2;
                lumen_area[1] = lumen_area[0];
            }

            if (L > 2)
            {

                velocity[L - 1] = velocity[L - 2];
                velocity[0] = velocity[1];

                pressure[L - 1] = pressure[L - 2];
                pressure[0] = pressure[1];

                lumen_area[L - 1] = lumen_area[L - 2];
                lumen_area[0] = lumen_area[1];
            }

            switch (Program.diss_mode)
            {
                case 0:
                    int l;
                    l = L - 2;
                    diss_func_sum_1thr = 0;
                    for (int i = 1; i < L - 1; i++)
                    {
                        if (velocity[i] > 0)
                        {
                            diss_func_el = -(pressure[i + 1] - pressure[i]) * (velocity[i] * lumen_area[i] * 1e6);
                        }
                        else
                        {
                            diss_func_el = -(pressure[i - 1] - pressure[i]) * ((-velocity[i]) * lumen_area[i] * 1e6);
                        }
                        diss_func_sum_1thr = diss_func_sum_1thr + diss_func_el;
                    }
                    if (velocity[0] > 0)
                    {
                        diss_func_el = -(pressure[1] - pressure[0]) * (velocity[0] * lumen_area[0] * 1e6);
                        diss_func_sum_1thr = diss_func_sum_1thr + diss_func_el;
                        l++;
                    }
                    if (velocity[L - 1] < 0)
                    {
                        diss_func_el = -(pressure[L - 2] - pressure[L - 1]) * ((-velocity[L - 1]) * lumen_area[L - 1] * 1e6);
                        diss_func_sum_1thr = diss_func_sum_1thr + diss_func_el;
                        l++;
                    }
                    break;
                case 1:
                    diss_func_sum_1thr = 0;
                    if (L > 2)
                    {
                        for (int i = 1; i < L - 1; i++)
                        {
                            diss_func_el = (Math.Abs(pressure[i + 1] - pressure[i - 1]) / 2) * (Math.Abs(velocity[i] * lumen_area[i] * 1e6 + velocity[i - 1] * lumen_area[i - 1] * 1e6 / 2 + velocity[i + 1] * lumen_area[i + 1] * 1e6 / 2) / 2);
                            diss_func_sum_1thr = diss_func_sum_1thr + diss_func_el;
                        }
                    }
                    diss_func_el = (Math.Abs(pressure[1] - pressure[0]) / 2) * (Math.Abs(velocity[0] * lumen_area[0] * 1e6 + velocity[1] * lumen_area[1] * 1e6) / 2);
                    diss_func_sum_1thr = diss_func_sum_1thr + diss_func_el;
                    diss_func_el = (Math.Abs(pressure[L - 1] - pressure[L - 2]) / 2) * (Math.Abs(velocity[L - 2] * lumen_area[L - 2] * 1e6 + velocity[L - 1] * lumen_area[L - 1] * 1e6) / 2);
                    diss_func_sum_1thr = diss_func_sum_1thr + diss_func_el;
                    break;
                default:
                    Console.WriteLine("Dissipation calculation mode is not selected");
                    break;
            }

            double[] rad = new double[L];
            double[] h = new double[L];
            for (int i = 0; i < L; ++i)
            {
                // действительно ли это так?
                rad[i] = nodes[i].radius;
                if (i > 0)
                {
                    h[i] = Vector3.Distance(nodes[i - 1].position, nodes[i].position);
                }
            }
            h[0] = h[1];
            calculatePropagation(ref agent_c, ref agent_shape, rad, velocity, L, h, dt);
            this.state2nodes();
        }

// agent - avg concentration; shape - parameter of the parabolic concentration profile
public static void calculatePropagation(ref double[] agent, ref double[] shape, double[] rad, double[] velocity, int node_count, double[] h, double dt)
{
    // shape - aplha from eq.(8) (doi:10.1016/j.procs.2018.08.272)
    // agent - S from eq.(8)
    const double U_POW = 2; // velocity profile power, ζ
    const double DIFF = 1e-4; // diffusion coeff, D

    double[] agent_predictor = (double[])agent.Clone();
    double[] agent_corrector = (double[])agent.Clone();
    double[] shape_agent = new double[node_count];
    double[] shape_agent_predictor = new double[node_count];
    double[] shape_agent_corrector = new double[node_count];
    double[] prop_coeff = new double[node_count];
    double[] u_av_coeff = new double[node_count];

    for (int i = 0; i < node_count; ++i)
    {
        prop_coeff[i] = 2 * Math.Pow(rad[i], 2) / (U_POW + 4);
        u_av_coeff[i] = (U_POW + 2) / U_POW;
        shape_agent[i] = shape[i] * agent[i];
    }

    for (int i = 0; i < node_count - 1; ++i)
    {
        // [0: N - 1]
        agent_predictor[i] = agent[i] - velocity[i] * dt / h[i] * (
            agent[i + 1] - agent[i] + prop_coeff[i] * (shape[i + 1] * agent[i + 1] - shape[i] * agent[i])
        ); //S(t+1) = S(t) + U*dt*(d(S) + prop_coeff*(d(alpha*S)))/dx  from system (8) eq.1 
        // [0: N - 1]
        shape_agent_predictor[i] = shape_agent[i] - u_av_coeff[i] * velocity[i] * dt / h[i] * (
            shape_agent[i + 1] - shape_agent[i] + 1.0 / (rad[i] * rad[i]) * (agent[i + 1] - agent[i])
            //U0 - average velocity
            //S(t+1) = alpha*S(t) + U0*R^2*dt*(d(alpha*S) + U0/R^2*(d(S)))/dx  from system (8) eq.2
        );
    }
    for (int i = 0; i < node_count - 1; ++i)
    {
        shape[i] = agent[i] > 1e-4 ? shape_agent_predictor[i] / agent[i] : 0;
    }

    for (int i = 1; i < node_count; ++i)
    {
        // [1: N]
        agent_corrector[i] = 0.5 * (agent[i] + agent_predictor[i] - velocity[i] * dt / h[i] * (
            agent_predictor[i] - agent_predictor[i - 1] + prop_coeff[i] * (
                shape[i] * agent_predictor[i] - shape[i - 1] * agent_predictor[i - 1]
            )
        ));
    }

    for (int i = 1; i < node_count - 1; ++i)
    {
        // [1: N - 1] //adding diffusion term for S
        agent_corrector[i] = agent_corrector[i] + dt * DIFF * (
                agent_corrector[i + 1] + agent_corrector[i - 1] - 2 * agent_corrector[i]
            ) / (h[i] * h[i]);
    }

    for (int i = 0; i < node_count; ++i)
    {
        agent_corrector[i] = agent_corrector[i] > 0 ? agent_corrector[i] : 0;
    }

    for (int i = 1; i < node_count; ++i)
    {
        double agent_dt = (agent_corrector[i] - agent[i]) / dt;

        // [1: N]
        shape_agent_corrector[i] = 0.5 * (
            shape_agent[i] + shape_agent_predictor[i] - u_av_coeff[i] * velocity[i] * dt / h[i] * (
                shape_agent_predictor[i] - shape_agent_predictor[i - 1] + 1.0 / Math.Pow(rad[i], 2) * (
                    agent_corrector[i] - agent_corrector[i - 1]
                )
            )
        );
    }

    for (int i = 0; i < node_count; ++i)
    {// [0: N]
        double agent_dt = (agent_corrector[i] - agent[i]) / dt;
        
        shape_agent_corrector[i] = shape_agent_corrector[i] -
            DIFF / Math.Pow(rad[i], 2) * shape_agent[i] * dt - // difusion for alpha*S field, eq 2. from system (8)
            1.0 / Math.Pow(rad[i], 2) * agent_dt * dt; // time-derivative of S 1/R^2*dS/dt, fourth term in eq.2 sys.(8)

        shape[i] = agent_corrector[i] > 1e-4 ? shape_agent_corrector[i] / agent_corrector[i] : 0; // getting alpha from alpha*S
        agent[i] = agent_corrector[i];
    }
    shape[node_count - 1] = shape[node_count - 2];
    shape[0] = shape[1];
    agent[0] = agent[1];
    agent[node_count - 1] = agent[node_count - 2];
}

        protected double calcPressure(double _lumen_area, int i)
        {
            return GlobalDefs.DIASTOLIC_PRESSURE + (beta_1[i] * (Math.Sqrt(_lumen_area) - Math.Sqrt(lumen_area_0[i])));
        } 
       
        protected double calcPressure(int i)
        {
            return GlobalDefs.DIASTOLIC_PRESSURE + (beta_1[i] * (Math.Sqrt(lumen_area[i]) - Math.Sqrt(lumen_area_0[i])));
        }
        
        protected double calclumen_area(int i)
        {
            return (double)Math.Pow((pressure[i] - GlobalDefs.DIASTOLIC_PRESSURE) / beta_1[i] + Math.Sqrt(lumen_area_0[i]), 2);
        }


        protected double young_modulus;
        protected double[] wall_thickhess;
        protected double[]      viscosity;
        protected double   beta;
        protected double[] beta_1;
        protected double[] lumen_area_0;
        protected double[] g_energy;

        protected double curr_dt;


        protected McCormackThread thread_solver;

        const double h_a = 0.2802;
        const double h_b = -505.3; //m^-1
        const double h_c = 0.1324;
        const double h_d = -11.14; //m^-1 
     
    }
}    