using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace BloodFlow
{
    
    public struct Cll_params
    {
        public int id1, id2;
        public double R1, R2, C;

        public Cll_params(int id1, int id2, double R1, double R2, double C)
        {
            this.id1 = id1;
            this.id2 = id2;
            this.R1 = R1;
            this.R2 = R2;
            this.C = C;
        }
    }

    public class BoundaryCondition
    {
        protected static float TIMEGAP = 1e-3f;
        protected static float DEF_BASEPERIOD = 1.0f;

        public BoundaryCondition(VascularNode _core_node, double start_time)
        {
            core_node = _core_node;
            //Only 1 neughbour is possible for terminal node
            neighbour_node = _core_node.neighbours[0];

            current_time = start_time;
            previous_time = current_time - TIMEGAP;
            v_sign = new int[2];
            DefineSign();
        }

        public virtual void reset()
        {
            current_time = 0;
            previous_time = current_time - TIMEGAP;
            core_node.velocity = 0;
            core_node.pressure = 0;
            core_node.lumen_area = core_node.lumen_area_0;
        }
		
		public virtual void addingSubstance()
        {

        }

        public virtual void doBC(double dt)
        {
            previous_time = current_time;
            current_time += dt;
        }

        public virtual void doBC(double dt, string insertion_type)
        {
            insert_type = insertion_type;
            previous_time = current_time;
            current_time += dt;
        }

        public virtual double getTime()
        { return current_time; }

        public virtual int ValueControl()
        {
            if (double.IsNaN(core_node.pressure))
                return core_node.id;
            if (double.IsNaN(core_node.velocity))
                return core_node.id;
            return -1;
        }

        protected virtual void DefineSign()
        {
            Vector3 dir_vector1 = new Vector3();
            Vector3 dir_vector2 = new Vector3();

            // Positive direction is outflow from termianl node
            dir_vector1 = neighbour_node.position - core_node.position;
            dir_vector2 = core_node.dir_vector;

            v_sign[0] = Math.Sign(Vector3.Dot(dir_vector1, dir_vector2));

            dir_vector2 = neighbour_node.dir_vector;
            v_sign[1] = Math.Sign(Vector3.Dot(dir_vector1, dir_vector2));
        }

        public double current_time;
        public double previous_time;
        public double base_period;
        public string insert_type;

        public int[] v_sign;
        public VascularNode core_node;
        public VascularNode neighbour_node;
		
		public bool agent_in;
    };


    public class InletFlux : BoundaryCondition
    {
        private List<VascularNode> nodes;
        public InletFlux(BoundaryCondition BC, TableFunction _flux, GetBetaFunction getElasticBeta)
            : base(BC.core_node, BC.current_time)
        {
            this.nodes = getListOfNextNodes(core_node, 200);
            this.previous_velocity = 0;

            base_flux_on_time = _flux;
            flux_on_time = _flux;


            // Arterial lumen of terminal and previous nodes are set the same
            core_node.lumen_area_0 = neighbour_node.lumen_area_0;
            core_node.lumen_area = core_node.lumen_area_0;

            double R0 = Math.Sqrt(core_node.lumen_area_0 / Math.PI);

            beta_1 = getElasticBeta(R0) / core_node.lumen_area_0;
            flux_function = delegate (double A)
            {
                return A * chrt_back + 4 * Math.Pow(A, 1.25f) * Math.Sqrt(beta_1 / 2.0 / GlobalDefs.BLOOD_DENSITY) - flux_on_time(current_time);
            };

            this.chrt_back = -4 * Math.Pow(core_node.lumen_area_0, 0.25f) * Math.Sqrt(beta_1 / 2.0f / GlobalDefs.BLOOD_DENSITY);
        }
		
		private bool ok = false;
        public override void addingSubstance()
        {
            double center = nodes.Count / 2;
            double h = 2;
            ok = true;
            double acum_agent = 0.0f;
            for (int i = 0; i < nodes.Count; ++i)
            {
                VascularNode node = nodes[i];
                double tmp = Math.Exp(-(i - center) * (i - center) / (h * h));
                // node.agent_c = Math.Exp(-(i - center) * (i - center) / (h * h));
                acum_agent += tmp;
                node.agent_c = tmp;
            }
            //Console.WriteLine("Total initial agent inserted: " + (acum_agent / (0.2f / 60.0f)));
        }

        public override void doBC(double dt, string insertion_type)
        {
            if (true)
            {
                insert_type = insertion_type;
                current_time = current_time + dt;
                previous_time = current_time;
                previous_velocity = current_velocity;

                double flux = flux_on_time(current_time);

                double inlet_lumen = myMath.NewtonSolver(flux_function, core_node.lumen_area, core_node.lumen_area_0 * 1e-4f, core_node.lumen_area_0 * 1e-6);
                double U = flux / inlet_lumen;


                double dZ = Vector3.Distance(core_node.neighbours.Last().position, core_node.position);
                double dUdt = (U - previous_velocity) / dt;
                double dUdZ = (core_node.neighbours.Last().velocity * v_sign[1] - U) / dZ;
                double visc_term = (double)(GlobalDefs.FRICTION_C * GlobalDefs.BLOOD_VISC * Math.PI * U / GlobalDefs.BLOOD_DENSITY / inlet_lumen);
                double P = core_node.neighbours.Last().pressure + (dUdt + U * dUdZ + visc_term) * dZ * GlobalDefs.BLOOD_DENSITY;


                core_node.velocity = U * v_sign[0];
                core_node.pressure = P;
                core_node.lumen_area = inlet_lumen;
                chrt_back = core_node.velocity - 4 * Math.Pow(core_node.neighbours.Last().lumen_area, 0.25f) * Math.Sqrt(beta_1 / 2.0f / GlobalDefs.BLOOD_DENSITY);
                current_velocity = U;

                // temporary solution for adding agent to the artery
                if (insert_type == "IV" && (current_time > (Program.STABILISATION_TIME)) && (current_time < (Program.STABILISATION_TIME + 0.001)) && (agent_in == true))
                {
                    /*double center = nodes.Count / 2;
                    double h = 2;
                    ok = true;
                    for (int i = 0; i < nodes.Count; ++i)
                    {
                        VascularNode node = nodes[i];
                        node.agent_c = Math.Exp(-(i - center) * (i - center) / (h * h));
                    }*/
                    this.addingSubstance();
                    agent_in = false;
                }
            }
        }

        private List<VascularNode> getListOfNextNodes(VascularNode core_node, int max_length)
        {
            List<VascularNode> nodes = new List<VascularNode>();
            nodes.Add(core_node);
            for (int i = 0; i < max_length; ++i)
            {
                VascularNode currentNode = nodes.Last();
                if (currentNode.neighbours.Count > 2)
                {
                    break;
                }
                foreach (VascularNode item in currentNode.neighbours)
                {
                    if (!nodes.Contains(item))
                    {
                        nodes.Add(item);
                    }
                }
            }
            return nodes;
        }

        public override void reset()
        {
            base.reset();

            chrt_back = 4 * Math.Pow(core_node.lumen_area_0, 0.25f) * Math.Sqrt(beta_1 / 2.0f / GlobalDefs.BLOOD_DENSITY);
            current_velocity = 0;
            previous_velocity = 0;
        }

        protected SimpleFunction flux_function;
        protected double beta_1;


        protected double previous_velocity;
        protected double current_velocity;
        protected double chrt_back;

        // Need for change heart rate////////////        
        public TableFunction flux_on_time;
        public readonly TableFunction base_flux_on_time;
        public readonly float base_period;
        /////////////////////////////////////////

        // Not used /////////////
        protected double Q_min;
        protected double Q_max;
        protected double diastolic_pressure;
        protected double sistolic_pressure;

        protected Queue<double> pressure_hist;

        protected double pulse_time_interval;
        protected double flux_minmax_time_interval;

        private float sample_dt;
        //////////////////////////
    };

    public class CollateralBC : BoundaryCondition
    {
        public CollateralBC(PressureOutletRCR BC1, PressureOutletRCR BC2, double _R1C, double _R2C, double _CC)
            : base(BC1.core_node, BC1.current_time)
        {
            core_node_1 = BC1.core_node;
            core_node_2 = BC2.core_node;

            v_sign_1 = (int[])BC1.v_sign.Clone();
            v_sign_2 = (int[])BC1.v_sign.Clone();

            R1C = _R1C;
            R2C = _R2C;
            CC = _CC;

            parent_BC1 = BC1;
            parent_BC2 = BC2;

            Q_t_0 = 0.0;
            Q_t_1 = 0.0;
            dlt_P_t_0 = 0.0;
            dlt_P_t_1 = 0.0;
            /*
            mass_conservation_left = delegate(double[] args)
            {
                return args[0] + args[1] + core_node_1.lumen_area * core_node_1.velocity * v_sign_1[0];
            };

            mass_conservation_right = delegate(double[] args)
            {
                return args[2] + args[3] + core_node_2.lumen_area * core_node_2.velocity * v_sign_2[0];
            };

            c_flux_pressure_left = delegate(double[] args)
            {
                return args[1] * R1C - (args[4] - args[5]);
            };

            out_flux_pressure_left = delegate(double[] args)
            {
                return args[0] * R1_left - (args[4] - GlobalDefs.OUT_PRESSURE);
            };

            c_flux_pressure_right = delegate(double[] args)
            {
                return args[3] * R1C - (args[5] - args[4]);
            };

            out_flux_pressure_right = delegate(double[] args)
            {
                return args[3] * R1C - (args[5] - args[4]);
            };
            */
            /* funcs = new MDFunction[5];
             funcs[0] = mass_conservation_left;
             funcs[1] = mass_conservation_right;
             funcs[2] = c_flux_pressure_left;
             funcs[3] = out_flux_pressure_left;
             funcs[4] = c_flux_pressure_right;
             funcs[5] = out_flux_pressure_right;*/

        }

        //TODO: rewrite by Newon-based opt. method instead of Picard iterations (use repeatBC as basic function)
        public override void doBC(double dt)
        {
            double v1 = Q_t_0 / parent_BC1.A_tx_01;
            double v2 = -Q_t_0 / parent_BC2.A_tx_01;

            for (int p_i = 0; p_i < 100; p_i++)
            {
                parent_BC1.repeatBC(dt, -v1); // v1 and v2 - correction of veloity due to collateral flow
                parent_BC2.repeatBC(dt, -v2); // repeatBC called with v1=0.0 just repeat calculation of BCs for current timestep (doBC moves forward by time)

                double p1 = parent_BC1.P_tx_11;
                double p2 = parent_BC2.P_tx_11;

                dlt_P_t_1 = p2 - p1;

                double R_fct = R1C / R2C + 1.0;

                Q_t_1 = Q_t_0 + ((dlt_P_t_1 / R2C + CC * (dlt_P_t_1 - dlt_P_t_0) / dt + Q_t_0 * CC * R1C / dt) / (R_fct + CC * R1C / dt)) * 1.0e-3; //dempher

                double v1_1 = Q_t_1 / parent_BC1.A_tx_11;
                double v2_1 = -Q_t_1 / parent_BC2.A_tx_11;

                if (Math.Abs(v1_1 - v1) + Math.Abs(v2_1 - v2) < 1e-6)
                    break;

                v1 = v1_1;
                v2 = v2_1;
            }
            parent_BC1.finalizBC(dt); // moving forward by time with current values on boundary 
            parent_BC2.finalizBC(dt); // composition of repeatBC + finalizeBC = doBC
            dlt_P_t_0 = dlt_P_t_1;
            Q_t_0 = Q_t_1;
        }

        /*
        public override void doBC(double dt)
        {            
            for (int p_i = 0; p_i < 100; p_i++)
            {
                parent_BC1.repeatBC(dt);
                parent_BC2.repeatBC(dt);

                double p1 = parent_BC1.P_tx_11;
                double p2 = parent_BC2.P_tx_11;

                double q1 = (p2 - p1) / (R1C + R2C);
                double q2 = -q1;

                double v1 = q1 / parent_BC1.core_node.lumen_area;
                double v2 = q2 / parent_BC2.core_node.lumen_area;

                parent_BC1.core_node.velocity = parent_BC1.core_node.velocity + v1 * parent_BC1.v_sign[0]*1e-2;
                parent_BC2.core_node.velocity = parent_BC2.core_node.velocity + v2 * parent_BC2.v_sign[0]*1e-2;

                if (Math.Abs(v1) + Math.Abs(v2) < 1e-6)
                    break;
            }
            parent_BC1.finalizBC(dt);
            parent_BC2.finalizBC(dt);
        }*/

        public VascularNode core_node_1;
        public VascularNode core_node_2;
        public VascularNode neighbour_node_1;
        public VascularNode neighbour_node_2;

        public int[] v_sign_1;
        public int[] v_sign_2;

        public double R1C, R2C, CC;

        protected MDFunction mass_conservation_left;
        protected MDFunction mass_conservation_right;
        protected MDFunction c_flux_pressure_left;
        protected MDFunction out_flux_pressure_left;
        protected MDFunction c_flux_pressure_right;
        protected MDFunction out_flux_pressure_right;

        PressureOutletRCR parent_BC1;
        PressureOutletRCR parent_BC2;

        protected MDFunction[] funcs;
        protected bool[] depmatrix;

        protected double Q_t_0, Q_t_1, dlt_P_t_0, dlt_P_t_1;
    }

    public class PressureOutletRCR : BoundaryCondition
    {
        public PressureOutletRCR(BoundaryCondition BC, GetBetaFunction getElsticBeta, double _R1, double _R2, double _C)
            : base(BC.core_node, BC.current_time)
        {

            // Windkessel params, R2 is parallel to C //
            R1 = _R1;
            R2 = _R2;
            C = _C;
            ////////////////////////////////////////////

            //tx_ij - i - time moment, j - space point, 1 - current moment (core node), 0 -previous moment (previus point)/
            U_tx_10 = 0;
            U_tx_01 = 0;
            P_tx_10 = GlobalDefs.DIASTOLIC_PRESSURE;
            P_tx_01 = GlobalDefs.DIASTOLIC_PRESSURE;
            P_tx_11 = GlobalDefs.DIASTOLIC_PRESSURE;
            A_tx_01 = core_node.neighbours.Last().lumen_area_0;
            A_tx_10 = core_node.lumen_area_0;
            A_tx_11 = core_node.lumen_area_0;
            Q_t_0 = 0;
            Q_t_1 = 0;
            ///////////////////////////////////////////

            beta_1 = GlobalDefs.getBoileauBeta(core_node.radius) / core_node.lumen_area_0;
            chrt_function = delegate (double a_tx_11)
            {
                double chrt_frw_right = Q_t_1 / a_tx_11 + 4 * Math.Pow(a_tx_11, 0.25f) * Math.Sqrt(beta_1 / 2.0f / GlobalDefs.BLOOD_DENSITY);
                return chrt_frw_left - chrt_frw_right;
            };

            DefineSign();
        }

        protected override void DefineSign()
        {
            Vector3 dir_vector1 = new Vector3();
            Vector3 dir_vector2 = new Vector3();


            dir_vector1 = core_node.position - core_node.neighbours.First().position;
            dir_vector2 = core_node.dir_vector;

            v_sign[0] = Math.Sign(Vector3.Dot(dir_vector1, dir_vector2));

            dir_vector2 = core_node.neighbours.Last().dir_vector;
            v_sign[1] = Math.Sign(Vector3.Dot(dir_vector1, dir_vector2));
        }

        public override int ValueControl()
        {
            if (double.IsNaN(core_node.pressure))
                return core_node.id;
            return -1;
        }

        public void repeatBC(double dt, double v_corr)
        {
            interfunc_dx = Vector3.Distance(core_node.position, core_node.neighbours.Last().position);
            interfunc_dt = dt;

            if (core_node.id == 1776)
                core_node.id = 1776;

            Q_t_1 = core_node.lumen_area * (core_node.velocity + v_corr) * v_sign[0];

            // Euler scheme for ODE integration///

            double dQdt = (Q_t_1 - Q_t_0) / dt;
            P_tx_11 = (Q_t_0 * (1 + R1 / R2) + C * R1 * dQdt + GlobalDefs.OUT_PRESSURE / R2 + P_tx_01 * C / dt) / (C / dt + 1 / R2);
            ////////////////////////////////////

            P_tx_10 = neighbour_node.pressure;
            A_tx_10 = neighbour_node.lumen_area;

            chrt_frw_left = U_tx_01 + 4 * Math.Pow(A_tx_10, 0.25f) * Math.Sqrt(beta_1 / 2.0f / GlobalDefs.BLOOD_DENSITY);

            A_tx_11 = myMath.NewtonSolver(chrt_function, A_tx_11, 1e-9, 1e-10);
            U_tx_11 = Q_t_1 / A_tx_11;

            /*
            core_node.velocity = U_tx_11 * v_sign[0];
            core_node.lumen_area = A_tx_11;
            core_node.pressure = P_tx_11;            

            P_tx_01 = P_tx_11;
            A_tx_01 = A_tx_11;
            U_tx_01 = U_tx_11;
            Q_t_0 = Q_t_1;     
             */
        }

        public void finalizBC(double dt)
        {
            current_time = current_time + dt;
            previous_time = current_time;

            core_node.velocity = U_tx_11 * v_sign[0];
            core_node.lumen_area = A_tx_11;
            core_node.pressure = P_tx_11;

            P_tx_01 = P_tx_11;
            A_tx_01 = A_tx_11;
            U_tx_01 = U_tx_11;
            Q_t_0 = Q_t_1;
        }

        public override void doBC(double dt)
        {
            interfunc_dx = Vector3.Distance(core_node.position, core_node.neighbours.Last().position);
            interfunc_dt = dt;
            current_time = current_time + dt;
            previous_time = current_time;

            Q_t_1 = core_node.lumen_area * core_node.velocity * v_sign[0];

            // Euler scheme for ODE integration///
            double dQdt = (Q_t_1 - Q_t_0) / dt;
            P_tx_11 = (Q_t_0 * (1 + R1 / R2) + C * R1 * dQdt + GlobalDefs.OUT_PRESSURE / R2 + P_tx_01 * C / dt) / (C / dt + 1 / R2);
            ////////////////////////////////////

            P_tx_10 = neighbour_node.pressure;
         //   U_tx_10 = neighbour_node.velocity * v_sign[1];
            A_tx_10 = neighbour_node.lumen_area;//core_node.lumen_area_0;//

            chrt_frw_left = U_tx_01 + 4 * Math.Pow(A_tx_10, 0.25f) * Math.Sqrt(beta_1 / 2.0f / GlobalDefs.BLOOD_DENSITY);

            A_tx_11 = myMath.NewtonSolver(chrt_function, A_tx_11, 1e-9, 1e-10);
            U_tx_11 = Q_t_1 / A_tx_11;

            core_node.velocity = U_tx_11 * v_sign[0];
            core_node.lumen_area = A_tx_11;
            core_node.pressure = P_tx_11;
            /*  core_node.neighbours.Last().pressure = core_node.pressure;
              core_node.neighbours.Last().velocity = core_node.velocity;
              core_node.neighbours.Last().lumen_area = core_node.lumen_area;*/

            P_tx_01 = P_tx_11;
            A_tx_01 = A_tx_11;
            U_tx_01 = U_tx_11;
            Q_t_0 = Q_t_1;
        }

        public override void doBC(double dt, string insertion_type)
        {
            interfunc_dx = Vector3.Distance(core_node.position, core_node.neighbours.Last().position);
            interfunc_dt = dt;
            insert_type = insertion_type;
            current_time = current_time + dt;
            previous_time = current_time;

            Q_t_1 = core_node.lumen_area * core_node.velocity * v_sign[0];

            // Euler scheme for ODE integration///
            double dQdt = (Q_t_1 - Q_t_0) / dt;
            P_tx_11 = (Q_t_0 * (1 + R1 / R2) + C * R1 * dQdt + GlobalDefs.OUT_PRESSURE / R2 + P_tx_01 * C / dt) / (C / dt + 1 / R2);
            ////////////////////////////////////

            P_tx_10 = neighbour_node.pressure;
            U_tx_10 = neighbour_node.velocity * v_sign[1];
            A_tx_10 = neighbour_node.lumen_area;

            chrt_frw_left = U_tx_01 + 4 * Math.Pow(A_tx_10, 0.25f) * Math.Sqrt(beta_1 / 2.0f / GlobalDefs.BLOOD_DENSITY);

            A_tx_11 = myMath.NewtonSolver(chrt_function, A_tx_11, 1e-9, 1e-10);            
            U_tx_11 = Q_t_1 / A_tx_11;

            core_node.velocity = U_tx_11 * v_sign[0];
            core_node.lumen_area = A_tx_11;
            core_node.pressure = P_tx_11;

            P_tx_01 = P_tx_11;
            A_tx_01 = A_tx_11;
            U_tx_01 = U_tx_11;
            Q_t_0 = Q_t_1;     
        }

        public double calcLumenArea(double pressure)
        {
            return (double)Math.Pow((pressure - GlobalDefs.DIASTOLIC_PRESSURE) / beta_1 + Math.Sqrt(core_node.lumen_area_0), 2);
        }

        public double calcPressure(double lumen)
        {
            return GlobalDefs.DIASTOLIC_PRESSURE + beta_1 * (Math.Sqrt(core_node.lumen_area) - Math.Sqrt(core_node.lumen_area_0));
        }

        protected SimpleFunction chrt_function;

        public double U_tx_01, U_tx_10, U_tx_11, P_tx_10, P_tx_11, P_tx_01;
        public double A_tx_10, A_tx_01, A_tx_11, Q_t_0, Q_t_1, chrt_frw_left;
        protected double interfunc_dx, interfunc_dt;


        public double R1, R2, C, beta_1;

    }



    public class InletPressure : BoundaryCondition
    {
        public InletPressure(BoundaryCondition BC, TableFunction _pressure, GetBetaFunction getElasticBeta)
            : base(BC.core_node, BC.current_time)
        {
            pressure_on_time = _pressure;

            double R0 = Math.Sqrt(core_node.lumen_area_0 / Math.PI);
            beta_1 = getElasticBeta(R0) / core_node.lumen_area_0;


            core_node.lumen_area_0 = neighbour_node.lumen_area_0;
            core_node.lumen_area = neighbour_node.lumen_area_0;

            chrt = 4 * Math.Pow(core_node.lumen_area_0, 0.25f) * Math.Sqrt(beta_1 / 2.0f / GlobalDefs.BLOOD_DENSITY);


        }

        public override void doBC(double dt)
        {
            current_time = current_time + dt;
            previous_time = current_time;

            double pressure = pressure_on_time(current_time);
            double inlet_lumen = Math.Pow((pressure - GlobalDefs.DIASTOLIC_PRESSURE) / beta_1 + Math.Sqrt(core_node.lumen_area_0), 2);
            double neighbour_chrt = neighbour_node.velocity - 4 * Math.Pow(core_node.neighbours.Last().lumen_area, 0.25) * Math.Sqrt(beta_1 / GlobalDefs.BLOOD_DENSITY / 2.0f);
            double U = neighbour_chrt + 4 * Math.Pow(inlet_lumen, 0.25) * Math.Sqrt(beta_1 / GlobalDefs.BLOOD_DENSITY / 2.0f);

            core_node.velocity = U * v_sign[0];
            core_node.pressure = pressure;
            core_node.lumen_area = inlet_lumen;
            chrt = core_node.velocity + 4 * Math.Pow(core_node.neighbours.Last().lumen_area, 0.25f) * Math.Sqrt(beta_1 / 2.0f / GlobalDefs.BLOOD_DENSITY);

            current_velocity = U;
        }

        public override void doBC(double dt, string insertion_type)
        {
            insert_type = insertion_type;
            current_time = current_time + dt;
            previous_time = current_time;

            double pressure = pressure_on_time(current_time);
            double inlet_lumen = Math.Pow((pressure - GlobalDefs.DIASTOLIC_PRESSURE) / beta_1 + Math.Sqrt(core_node.lumen_area_0), 2);
            double neighbour_chrt = neighbour_node.velocity - 4 * Math.Pow(core_node.neighbours.Last().lumen_area, 0.25) * Math.Sqrt(beta_1 / GlobalDefs.BLOOD_DENSITY / 2.0f);
            double U = neighbour_chrt + 4 * Math.Pow(inlet_lumen, 0.25) * Math.Sqrt(beta_1 / GlobalDefs.BLOOD_DENSITY / 2.0f);

            core_node.velocity = U * v_sign[0];
            core_node.pressure = pressure;
            core_node.lumen_area = inlet_lumen;
            chrt = core_node.velocity + 4 * Math.Pow(core_node.neighbours.Last().lumen_area, 0.25f) * Math.Sqrt(beta_1 / 2.0f / GlobalDefs.BLOOD_DENSITY);

            current_velocity = U;
        }

        protected TableFunction pressure_on_time;

        protected double beta_1;
        protected double Q_min;
        protected double Q_max;

        protected double previous_velocity;
        protected double current_velocity;
        protected double chrt;

        protected double diastolic_pressure;
        protected double sistolic_pressure;

        protected Queue<double> pressure_hist;

        protected double pulse_time_interval;
        protected double flux_minmax_time_interval;

        private float sample_dt;
    };

    public class WaveTransmissive : BoundaryCondition
    {
        public WaveTransmissive(BoundaryCondition BC, GetBetaFunction getElsticBeta)
            : base(BC.core_node, BC.current_time)
        {

            //tx_ij - i - time moment, j - space point, 1 - current moment (core node), 0 -previous moment (previus point)/
            Q_tx_10 = 0;
            Q_tx_01 = 0;
            P_tx_10 = GlobalDefs.DIASTOLIC_PRESSURE;
            P_tx_01 = GlobalDefs.DIASTOLIC_PRESSURE;
            P_tx_11 = GlobalDefs.DIASTOLIC_PRESSURE;
            Q_t_0 = 0;
            Q_t_1 = 0;
            ///////////////////////////////////////////

            beta_1 = GlobalDefs.getBoileauBeta(core_node.radius) / core_node.lumen_area_0;

            chrt_function = delegate (double A_tx_11)
            {
                double chrt_frw_right = Q_t_1 / A_tx_11 + 4 * Math.Pow(A_tx_11, 0.25f) * Math.Sqrt(beta_1 / 2.0f / GlobalDefs.BLOOD_DENSITY);
                return chrt_frw_left - chrt_frw_right;
            };


            DefineSign();
        }

        protected override void DefineSign()
        {
            Vector3 dir_vector1 = new Vector3();
            Vector3 dir_vector2 = new Vector3();


            dir_vector1 = core_node.position - core_node.neighbours.First().position;
            dir_vector2 = core_node.dir_vector;

            v_sign[0] = Math.Sign(Vector3.Dot(dir_vector1, dir_vector2));

            dir_vector2 = core_node.neighbours.Last().dir_vector;
            v_sign[1] = Math.Sign(Vector3.Dot(dir_vector1, dir_vector2));
        }

        public override int ValueControl()
        {
            if (double.IsNaN(core_node.pressure))
                return core_node.id;
            return -1;
        }

        public override void doBC(double dt)
        {

            interfunc_dx = Vector3.Distance(core_node.position, core_node.neighbours.Last().position);
            interfunc_dt = dt;
            current_time = current_time + dt;
            previous_time = current_time;

            Q_tx_01 = core_node.lumen_area * core_node.velocity * v_sign[0];
            Q_tx_10 = neighbour_node.lumen_area * neighbour_node.velocity * v_sign[1];
            U_tx_10 = neighbour_node.velocity * v_sign[1];

            P_tx_01 = core_node.pressure;
            P_tx_10 = neighbour_node.pressure;

            double sound_speed = Math.Pow(core_node.lumen_area, 0.25f) * Math.Sqrt(beta_1 / 2.0f / GlobalDefs.BLOOD_DENSITY);

            P_tx_11 = (interfunc_dx / sound_speed / dt * P_tx_01 - P_tx_10) / (1 + interfunc_dx / dt / sound_speed);
            Q_tx_11 = (interfunc_dx / sound_speed / dt * Q_tx_01 - Q_tx_10) / (1 + interfunc_dx / dt / sound_speed);

            A_tx_10 = neighbour_node.lumen_area;
            chrt_frw_left = U_tx_10 + 4 * Math.Pow(A_tx_10, 0.25f) * Math.Sqrt(beta_1 / 2.0f / GlobalDefs.BLOOD_DENSITY);
            //  A_tx_11 = myMath.NewtonSolver(chrt_function, A_tx_10, 1e-9, 1e-10);
            A_tx_11 = calcLumenArea(P_tx_11);


            core_node.velocity = Q_tx_11 / A_tx_11 * v_sign[0];
            core_node.lumen_area = A_tx_11;
            core_node.pressure = P_tx_11;

            //A_tx_11 = calcLumenArea(P_tx_11);            

            Q_t_0 = Q_t_1;

        }

        public override void doBC(double dt, string insertion_type)
        {

            interfunc_dx = Vector3.Distance(core_node.position, core_node.neighbours.Last().position);
            interfunc_dt = dt;
            insert_type = insertion_type;
            current_time = current_time + dt;
            previous_time = current_time;

            Q_tx_01 = core_node.lumen_area * core_node.velocity * v_sign[0];
            Q_tx_10 = neighbour_node.lumen_area * neighbour_node.velocity * v_sign[1];
            U_tx_10 = neighbour_node.velocity * v_sign[1];

            P_tx_01 = core_node.pressure;
            P_tx_10 = neighbour_node.pressure;

            double sound_speed = Math.Pow(core_node.lumen_area, 0.25f) * Math.Sqrt(beta_1 / 2.0f / GlobalDefs.BLOOD_DENSITY);

            P_tx_11 = (interfunc_dx / sound_speed / dt * P_tx_01 - P_tx_10) / (1 + interfunc_dx / dt / sound_speed);
            Q_tx_11 = (interfunc_dx / sound_speed / dt * Q_tx_01 - Q_tx_10) / (1 + interfunc_dx / dt / sound_speed);

            A_tx_10 = neighbour_node.lumen_area;
            chrt_frw_left = U_tx_10 + 4 * Math.Pow(A_tx_10, 0.25f) * Math.Sqrt(beta_1 / 2.0f / GlobalDefs.BLOOD_DENSITY);
            //  A_tx_11 = myMath.NewtonSolver(chrt_function, A_tx_10, 1e-9, 1e-10);
            A_tx_11 = calcLumenArea(P_tx_11);


            core_node.velocity = Q_tx_11 / A_tx_11 * v_sign[0];
            core_node.lumen_area = A_tx_11;
            core_node.pressure = P_tx_11;

            //A_tx_11 = calcLumenSq(P_tx_11);            

            Q_t_0 = Q_t_1;

        }

        public double calcLumenArea(double pressure)
        {
            return (double)Math.Pow((pressure - GlobalDefs.DIASTOLIC_PRESSURE) / beta_1 + Math.Sqrt(core_node.lumen_area_0), 2);
        }

        public double calcPressure(double lumen)
        {
            return GlobalDefs.DIASTOLIC_PRESSURE + beta_1 * (Math.Sqrt(core_node.lumen_area) - Math.Sqrt(core_node.lumen_area_0));
        }

        protected SimpleFunction chrt_function;

        protected double U_tx_10, Q_tx_01, Q_tx_10, Q_tx_11, P_tx_10, P_tx_11, P_tx_01;
        protected double A_tx_11, A_tx_10, Q_t_0, Q_t_1, chrt_frw_left, chrt_frw_right;
        protected double interfunc_dx, interfunc_dt;

        public double R1, R2, C, beta_1;

    }
}