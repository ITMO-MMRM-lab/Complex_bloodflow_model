using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Text;

namespace BloodFlow
{

    public enum InletType { FLUX, PRESSURE }
    public enum SolutionState { CONTINUE, ERROR, FINISHED }

    class GlobalDefs
    {
        public static float YOUNG_MODULUS = 225.0e+3f;//Pa        
        public static double BLOOD_DENSITY = 1040f; //kg/m^3 
        public static double BLOOD_VISC = 3.5e-3f; //Pa*s
        public static double DIASTOLIC_PRESSURE = 10.0e+3f; //Pa
        public static double OUT_PRESSURE = 0.0f;
        public static double DIASTOLIC_PRESSURE_1 = 0; //Pa
        public static double SISTOLIC_PRESSURE = 15.9e+3f; //Pa
        public static double GRAVITY = 9.8f;// m/s^2
        public static double FRICTION_C = 8.0f;
        public static double HEART_PERIOD = 1.0;

        public static double phi = 0e3;
        public static Vector3 ZERO_POINT = new Vector3(0.0f, 0.0f, 0.0f);
        public static Vector3 DOWN;
        public static int av_iter_num = 0;

        public static double AGENT_DENSITY = 1283.0; // kg/m^3, epinephrine

        static public TableFunction agent_inlet_flux = delegate (double a)
        {
            return 0.0;
        };

        static public double getBoileauBeta(double R0)
        {
            double h_a = 0.2802;
            double h_b = -505.3; //m^-1
            double h_c = 0.1324;
            double h_d = -11.14; //m^-1 
            double y_m = YOUNG_MODULUS;

            double w_t = R0 * (h_a * Math.Exp(h_b * R0) + h_c * Math.Exp(h_d * R0));
            return 4.0 / 3.0 * Math.Sqrt(Math.PI) * y_m * w_t;
        }

        static public double getFixedHBeta(double R0)
        {
            double w_t = getFixedWallThickness(R0);
            return 4.0 / 3.0 * Math.Sqrt(Math.PI) * YOUNG_MODULUS * w_t;
        }

        static public double getFixedWallThickness(double R0)
        {
            return 1.5e-3;
        }

        static public double getBoileauWallThickness(double R0)
        {
            double h_a = 0.2802;
            double h_b = -505.3; //m^-1
            double h_c = 0.1324;
            double h_d = -11.14; //m^-1 
            double y_m = YOUNG_MODULUS;

            return R0 * (h_a * Math.Exp(h_b * R0) + h_c * Math.Exp(h_d * R0));
        }

        static public double getSoundSpeed(double R0)
        {
            double beta = getBoileauBeta(R0);
            double A0 = Math.PI * R0 * R0;
            return Math.Sqrt(beta / (2.0 * BLOOD_DENSITY)) * Math.Pow(A0, -0.25);
        }

        static public double getR1(double R0)
        {
            double A0 = Math.PI * R0 * R0;
            return (BLOOD_DENSITY * getSoundSpeed(R0)) / A0;
        }

        static public double sin_flux(double t)
        {
            return (double)Math.Sin(Math.PI * t * 10) * 0.25f + 10f; // ml/s
        }

        static public double single_pusle(double t)
        {
            return (double)1.0e-7 * Math.Exp(-1.0e4 * (t - 0.05) * (t - 0.05)); // ml/s
        }
    }

    public struct BodyPartInlet
    {
        public BodyPartInlet(string _name, List<VascularNode> _inlet, List<double> _inlet_flux)
        {
            name = _name;
            inlet = new List<VascularNode>(_inlet);
            inlet_flux = new List<double>(_inlet_flux);
            outlet = new List<BoundaryCondition>();
        }
        public string name;
        public List<VascularNode> inlet;
        public List<BoundaryCondition> outlet;
        public List<double> inlet_flux;
    }

    public class AASimulator
    {
        /// FOR IBUPROFEN
        // ini_amount = 400 mg;
        // k_el = 0.32;
        // t_n = 60 min;
        // ∫ C from 0 to n = 109.3;

        // ini_amount = 800 mg;
        // k_el = 0.29;
        // t_n = 60 min;
        // ∫ C from 0 to n = 192.8;

        public static double agent_k_el = 0.32f / 3600f; //Ibuprofen k_el (1/h)

        public double ini_concentration;
        public double ini_time;
        public double ini_auc_time;
        public double acum_auc;

        public void initialize(double c_initial, double t_initial)
        {
            ini_concentration = c_initial;
            ini_time = t_initial;
            ini_auc_time = t_initial;
            acum_auc = 0.0f;
        }

        public double getKel(double i_concentration, double c_concentration, double ini_time, double cur_time) /// Elimination rate constant
        {
            // NOT USED
            return Math.Log(i_concentration / c_concentration) / (cur_time - ini_time); // Kel = ln(Cinitial / Ccurrent) / △t; △t = cur_time - ini_time
        }

        public double getCurrConcentration(double cur_time) /// kg/l
        {
            return Math.Exp(Math.Log(ini_concentration) - agent_k_el * (cur_time - ini_time)); // Ccurrent = e ^ (ln(Cinitial) - K_el * △t); △t = cur_time - ini_time
        }

        public double trapzRule(double t1, double t2, int n)
        {
            double area = 0.0f;
            double w = (t2 - t1) / n;
            //area += (ini_concentration + getCurrConcentration(ini_concentration, k_el, t1, t2)) / 2;
            area += (getCurrConcentration(t1) + getCurrConcentration(t2)) / 2;
            for (int i = 1; i < n; ++i)
            {
                //area += getCurrConcentration(ini_concentration, k_el, t1, t1 + w * i);
                area += getCurrConcentration(t1 + w * i);
            }
            return w * area;
        }
        public double WagnerNelsonAbsorption(double current_time)
        {
            double current_auc = trapzRule(ini_auc_time, current_time, 10);
            double WN_absorption = getCurrConcentration(current_time) + (agent_k_el * (acum_auc + current_auc)); /// A/Vd = Ccurrent + Kel * ∫ C from 0 to cur_time

            acum_auc = acum_auc + current_auc;
            ini_auc_time = current_time;
            return WN_absorption;
        }
    }

    public class BFSimulator
    {
        public BFSimulator(string top_text, string insertion_type, int[] inlet_nodes)
        {
            timestep_N = 0;
            init = true;

            end_time = -1.0f;
            current_time = 0.0f;

            insert_type = insertion_type;

            IO_Module.localization = "en-US";
            List<BC_params> rcr_params = new List<BC_params>();
            try
            {
                IO_Module.LoadTopologyFromString(top_text, out v_net);
            }
            catch { }

            GlobalDefs.ZERO_POINT = v_net.vascular_system[0].position;

            getFloatValueDelegate getProximaDst; // special delegates for wide-width search on arterial network
            setFloatValueDelegate setProximaDst; // -"-

            v_net.defineNodeDirVectors(inlet_nodes, out getProximaDst, out setProximaDst);
            v_net.defineNet(getProximaDst, setProximaDst);

            for (int i = 0; i < v_net.threads.Count; i++)
                v_net.specifyThreadType(i, new ElasticThread(v_net.threads[i], GlobalDefs.getBoileauBeta));

            // WaveTransmissive BCs are default, just pass pressure wave outside (have very small reflection coeff.)
            // No inlet BC are by def. It should be defined separatelly.
            for (int i = 0; i < v_net.bounds.Count; i++)
                // v_net.bounds[i] = new WaveTransmissive(v_net.bounds[i], GlobalDefs.getBoileauBeta);//new PressureOutletRCR(v_net.bounds[i], GlobalDefs.getBoileauBeta, 0.1e+9, 0.1e+9, 1.0e-12); // //
                v_net.bounds[i] = new PressureOutletRCR(v_net.bounds[i], GlobalDefs.getBoileauBeta, 2.0e+9, 100.0e+9, 0.1e-12); // //

            for (int i = 0; i < v_net.knots.Count; i++)
                v_net.knots[i] = new StandartKnot(v_net.knots[i], GlobalDefs.getBoileauBeta);//ViscoElasticKnot(v_net.knots[i], GlobalDefs.getBoileauBeta);


            control_point_list = new List<NodeSummary>();

            solution_state = SolutionState.CONTINUE;
        }

        public double getAgentVolume()
        {
            double agent_volume_total = 0;
            double agent_volume_thr = 0;
            double agent_volume_kn = 0;
            double agent_volume_el = 0;
            double c1, c2, r1, r2, dist_el;
            foreach (var thread in v_net.threads)
            {
                agent_volume_thr = 0;
                for (int i = 0; i < (thread.nodes.Length - 1); i++)
                {
                    c1 = thread.nodes[i].agent_c;
                    c2 = thread.nodes[i + 1].agent_c;
                    r1 = Math.Sqrt(thread.nodes[i].lumen_area / Math.PI);
                    r2 = Math.Sqrt(thread.nodes[i + 1].lumen_area / Math.PI);
                    dist_el = Vector3.Distance(thread.nodes[i + 1].position, thread.nodes[i].position);
                    agent_volume_el = Math.PI * dist_el * (3 * c1 * Math.Pow(r1, 2) + c1 * Math.Pow(r2, 2) + c2 * Math.Pow(r1, 2) + 3 * c2 * Math.Pow(r2, 2) + 2 * c1 * r1 * r2 + 2 * c2 * r1 * r2) / 12;
                    agent_volume_thr = agent_volume_thr + agent_volume_el;
                }
                agent_volume_total = agent_volume_total + agent_volume_thr;
            }
            foreach (var knot in v_net.knots)
            {
                agent_volume_kn = 0;
                for (int i = 0; i < (knot.nodes.Length); i++)
                {
                    c1 = knot.core_node.agent_c;
                    c2 = knot.nodes[i].agent_c;
                    r1 = Math.Sqrt(knot.core_node.lumen_area / Math.PI);
                    r2 = Math.Sqrt(knot.nodes[i].lumen_area / Math.PI);
                    dist_el = Vector3.Distance(knot.nodes[i].position, knot.core_node.position);
                    agent_volume_el = Math.PI * dist_el * (3 * c1 * Math.Pow(r1, 2) + c1 * Math.Pow(r2, 2) + c2 * Math.Pow(r1, 2) + 3 * c2 * Math.Pow(r2, 2) + 2 * c1 * r1 * r2 + 2 * c2 * r1 * r2) / 12;
                    agent_volume_kn = agent_volume_kn + agent_volume_el;
                }
                agent_volume_total = agent_volume_total + agent_volume_kn;
            }
            return agent_volume_total;
        }

        public double getAgentMass()
        {
            double agent_mass;
            agent_mass = getAgentVolume() * GlobalDefs.AGENT_DENSITY;
            return agent_mass;
        }
        
        public void setTimeParameters(float timestep, float end_time, float av_pariod)
        {
            this.delta_tau = timestep;
            this.end_time = end_time;
            this.stenosis_av_period = av_pariod;
        }

        public BodyPartInlet createBodyPartInlet(string _name, int[] _inlet, double[] _fluxes)
        {
            List<VascularNode> _inlet_nodes = new List<VascularNode>();
            for (int i = 0; i < _inlet.Length; i++)
                _inlet_nodes.Add(v_net.vascular_system.Find(x => x.id == _inlet[i]));

            return new BodyPartInlet(_name, _inlet_nodes, _fluxes.ToList());

        }

        public BFSimulator(string path, string top_filename, string insertion_type, int[] inlet_nodes)
            : this(
            String.Join("\n", File.ReadAllLines(path + top_filename)), insertion_type, inlet_nodes
           )
        {

        }

        public void setInletBC(string inlet_data, int node_number, InletType type)
        {
            TableFunction inlet_function = null;
            IO_Module.LoadTableFunctionFromString(inlet_data, 1.0, out inlet_function);
            InletId = v_net.bounds.FindIndex(x => x.core_node.id == node_number);

            if (type == InletType.FLUX)
                v_net.bounds[InletId] = new InletFlux(v_net.bounds[InletId], inlet_function, GlobalDefs.getBoileauBeta);

            if (type == InletType.PRESSURE)
                v_net.bounds[InletId] = new InletPressure(v_net.bounds[InletId], inlet_function, GlobalDefs.getBoileauBeta);
        }

        public void setCollaterals(string parameters)
        {
            if (parameters != "")
            {
                List<Cll_params> cll_params = new List<Cll_params>();
                IO_Module.LoadCollateralParamsFromString(parameters, out cll_params);

                List<CollateralBC> cll_tmp = new List<CollateralBC>();
                List<int> cll_ids1 = new List<int>();
                List<int> cll_ids2 = new List<int>();

                foreach (var p in cll_params)
                {
                    try
                    {
                        int ind1 = v_net.bounds.FindIndex(x => x.core_node.id == p.id1);
                        int ind2 = v_net.bounds.FindIndex(x => x.core_node.id == p.id2);

                        cll_tmp.Add(new CollateralBC((PressureOutletRCR)v_net.bounds[ind1], (PressureOutletRCR)v_net.bounds[ind2], p.R1, p.R2, p.C));
                        cll_ids2.Add(ind2);
                        cll_ids1.Add(ind1);
                        //v_net.bounds[ind2] = null;                        
                    }
                    catch
                    {
                        Console.WriteLine("ERROR: Nodes: " + p.id1 + " or " + p.id2 + " are not termainal or don't exist!\n");
                    }
                }

                for (int cll_i = 0; cll_i < cll_tmp.Count; cll_i++)
                {
                    try
                    {
                        v_net.bounds[cll_ids1[cll_i]] = cll_tmp[cll_i];
                        v_net.bounds[cll_ids2[cll_i]] = null;
                    }
                    catch
                    { }
                }
            }
        }

        public void setOutletBC(string parameters)
        {
            outlet_bc_set = new List<PressureOutletRCR>();
            re_entering_delta = new List<double>();
            if (parameters != "")
            {
                List<BC_params> rcr_params = new List<BC_params>();
                IO_Module.LoadBC_RCR_paramsFromString(parameters, out rcr_params);
                outlets_agent_amount = new Queue<double>[rcr_params.Count];
                outlets_agent_avg = new double[rcr_params.Count];

                int it = 0;
                foreach (var bc_params in rcr_params)
                {
                    try
                    {
                        int ind = v_net.bounds.FindIndex(x => x.core_node.id == bc_params.id);
                        v_net.bounds[ind] = new PressureOutletRCR(v_net.bounds[ind], GlobalDefs.getBoileauBeta, bc_params.R1, bc_params.R2, bc_params.C);
                        outlet_bc_set.Add((PressureOutletRCR)v_net.bounds[ind]);
                        
                        double outlet2inlet_cm_distance = Vector3.Distance(outlet_bc_set[it].core_node.position, v_net.bounds[InletId].core_node.position) * 100.0f;    // Calculate distance from outlet[i] to inlet
                        double approximate_vein_blood_velocity = 1.0f;   // 1.0 cm/s according Biofluid Mechanics - Mazumdar 1992
                        re_entering_delta.Add(outlet2inlet_cm_distance / approximate_vein_blood_velocity);    /// Calculate Δt based on velocity and distance
                        //re_entering_delta.Add(1.0f); // Add random Δt or 1 as here

                        outlets_agent_amount[it] = new Queue<double>();
                        outlets_agent_avg[it] = 0.0f;
                        ++it;
                    }
                    catch {
                        Console.WriteLine("WARNING: One or more outlet BCs may be not set");
                    }
                }
                return;
            }

            for (int i = 0; i < v_net.bounds.Count; i++)
            {
                BoundaryCondition bn = v_net.bounds[i];
                bn = new PressureOutletRCR(bn, GlobalDefs.getBoileauBeta, 0.1, 0.1, 0.1);
                outlet_bc_set.Add((PressureOutletRCR)bn);
            }
        }

        public List<PressureOutletRCR> setOutletBC(int start_point_id, List<BodyPartInlet> parts, float Q_total, float C_total, float R_total)
        {
            Dictionary<int, List<VascularNode>> outlet_groups = new Dictionary<int, List<VascularNode>>();

            VascularNode start_point = v_net.vascular_system.Find(x => x.id == start_point_id);
            BodyPartInlet base_part = new BodyPartInlet("Whole", new List<VascularNode> { start_point }, new List<double> { 150.0e-3f });
            if (parts.Count == 0)
                parts.Add(base_part);

            List<VascularNode> reference_points = new List<VascularNode>();
            foreach (var p in parts)
                reference_points.AddRange(p.inlet);

            List<VascularNode> front_0 = new List<VascularNode>();
            List<VascularNode> front_1 = new List<VascularNode>();

            getFloatValueDelegate get_dist;
            setFloatValueDelegate set_dist;

            getFloatValueDelegate get_group;
            setFloatValueDelegate set_group;

            VascularNode.newFloatValueLayer(out get_dist, out set_dist);
            VascularNode.newFloatValueLayer(out get_group, out set_group);

            front_0.Add(start_point);
            set_dist(start_point, 0);


            /*  Q_total = 0;
              for (int i = 0; i < parts.Count; i++)
                  Q_total += (float)parts[i].inlet_flux.Sum();*/

            while (true)
            {
                foreach (var n in front_0)
                {
                    if (reference_points.Contains(n) && (!outlet_groups.ContainsKey(n.id)))
                    {
                        outlet_groups.Add(n.id, new List<VascularNode>());
                        set_group(n, n.id);
                    }

                    foreach (var nn in n.neighbours)
                        if (get_dist(nn) > get_dist(n))
                        {
                            set_dist(nn, get_dist(n) + 1);
                            if (!front_1.Contains(nn))
                            {
                                front_1.Add(nn);
                                set_group(nn, get_group(n));
                                if (outlet_groups.ContainsKey((int)get_group(n)))
                                    outlet_groups[(int)get_group(n)].Add(nn);
                            }
                        }
                }

                front_1 = front_1.Distinct().ToList();
                front_0 = new List<VascularNode>(front_1);
                front_1.Clear();

                if (front_0.Count == 0)
                    break;
            }

            foreach (var part in parts)
            {
                foreach (var root in part.inlet)
                {
                    if (outlet_groups.ContainsKey(root.id))
                        foreach (var n in outlet_groups[root.id])
                            if (n.neighbours.Count == 1)
                            {
                                var bnd = v_net.bounds.Find(x => x.core_node == n);
                                if (bnd.core_node != start_point)
                                    part.outlet.Add(bnd);
                            }
                }
            }

            for (int i = 0; i < parts.Count; i++)
            {
                double outlet_cube_summ = 0;
                double inlet_cube_summ = 0;
                foreach (var on in parts[i].outlet)
                {
                    outlet_cube_summ += Math.Pow(on.core_node.radius, 3);
                }

                foreach (var inp_n in parts[i].inlet)
                {
                    inlet_cube_summ += Math.Pow(inp_n.radius, 3);
                }

                Console.WriteLine(parts[i].name + " inlet/outlet: " + inlet_cube_summ / outlet_cube_summ);
            }

            for (int i = 0; i < parts.Count; i++)
            {
                double a_cube_outlet = 0;
                double tot_inlet_flux = 0;

                for (int j = 0; j < parts[i].outlet.Count; j++)
                    a_cube_outlet += Math.Pow(parts[i].outlet[j].core_node.radius, 3);


                for (int j = 0; j < parts[i].inlet_flux.Count; j++)
                    tot_inlet_flux += parts[i].inlet_flux[j];

                double R_part = (Q_total / tot_inlet_flux) * R_total;

                foreach (PressureOutletRCR bc in parts[i].outlet)
                {
                    double c_d = Math.Sqrt(GlobalDefs.getBoileauBeta(bc.core_node.radius) / 2.0 / GlobalDefs.BLOOD_DENSITY / Math.Sqrt(bc.calcLumenArea(GlobalDefs.DIASTOLIC_PRESSURE)));
                    double Rt = a_cube_outlet / Math.Pow(bc.core_node.radius, 3) * R_part;
                    // inv_tot_R += 1.0 / Rt;
                    bc.R1 = GlobalDefs.BLOOD_DENSITY * c_d / bc.calcLumenArea(GlobalDefs.DIASTOLIC_PRESSURE);
                    double R2 = Rt - bc.R1;
                    if (R2 < 0)
                    {
                        R2 = 0.1;
                        bc.R1 = Rt - R2;
                    }

                    bc.R2 = R2;
                    bc.C = C_total * R_total / Rt;
                }
            }

            List<PressureOutletRCR> bc_outlet_List = new List<PressureOutletRCR>();
            for (int i = 0; i < parts.Count; i++)
            {
                foreach (PressureOutletRCR bc in parts[i].outlet)
                    bc_outlet_List.Add(bc);
            }

            return bc_outlet_List;
        }

        public void setOutletBC(List<BC_params> rcr_params)
        {
            List<PressureOutletRCR> oultlet_bc_set = new List<PressureOutletRCR>();

            foreach (var bc_params in rcr_params)
            {
                try
                {
                    int ind = v_net.bounds.FindIndex(x => x.core_node.id == bc_params.id);
                    v_net.bounds[ind] = new PressureOutletRCR(v_net.bounds[ind], GlobalDefs.getBoileauBeta, bc_params.R1, bc_params.R2, bc_params.C);
                    oultlet_bc_set.Add((PressureOutletRCR)v_net.bounds[ind]);
                }
                catch { }
            }
        }

        public double getArterialTreeVolume()
        {
            double arterial_tree_volume = 0.0f;
            foreach (var thread in v_net.threads)
            {
                double inter_node_distance = 0.0f;
                double thread_volume = 0.0f;
                for (int i = 1; i < (thread.nodes.Length); ++i)
                {
                    inter_node_distance = Vector3.Distance(thread.nodes[i].position, thread.nodes[i - 1].position);
                    thread_volume += inter_node_distance * thread.nodes[i].lumen_area;
                }
                arterial_tree_volume += thread_volume;
            }

            return arterial_tree_volume;
        }

        public double getCurrentConcentration()
        {
            double agentAmount_kg = getAgentVolume() * 1030.0f; /// Ibuprofen density = 1.03 g/cm^3, mass = volume * density => kg
            double arterialTreeVolume_l = getArterialTreeVolume() * 1000.0f; // m^3 * 1000 => liters
            return agentAmount_kg / arterialTreeVolume_l;
            //return absorption_simulator.getCurrConcentration(current_time);
        }
            
        public void Update()
        {
            Update(delta_tau);
        }

        public void Update(float timestep)
        {
            double sum_agent_OutletBC = outlet_bc_set.Aggregate(0.0, (total, next) => total + next.core_node.agent_c);
            			
			int inlet_num, inlet_index;
            diss_func_sum_threads = 0;
                                        
            this.delta_tau = timestep;

            current_time += delta_tau;
            timestep_N++;

            Parallel.ForEach(v_net.threads,
                () =>
                {
                    return 0D;
                },
                (tr, loop, subtotal) =>
                {
                    tr.calcThread(delta_tau);
                    if (current_time >= Program.END_TIME - GlobalDefs.HEART_PERIOD)
                        subtotal += tr.diss_func_sum_1thr;
                    return subtotal;
                },
                (subtotal) =>
                {
                    lock (v_net.threads)
                    {
                        if (current_time >= Program.END_TIME - GlobalDefs.HEART_PERIOD)
                            diss_func_sum_threads += subtotal * delta_tau;
                    }
                });

            Parallel.ForEach(v_net.bounds, (bc) =>
            {
                if (bc != null)
                    bc.doBC(delta_tau, insert_type);
            });                    //

            //  foreach(var bc in v_net.bounds)
            //      bc.doBC(delta_tau);//*/

            Parallel.ForEach(v_net.knots, (kn) =>
            {
                kn.doCoupling(delta_tau);
            });      //*/

            //     foreach (var kn in v_net.knots)
            //       kn.doCoupling(delta_tau);

            Parallel.ForEach(v_net.threads, (tr) =>
            {
                tr.updateState();
                // tr.updateStateFFR();
            });
			
			if (getAgentVolume() > 0.0f) /// Agent enters the system, begins absorption process
            {
                double agentAmount_kg = getAgentVolume() * 1030.0f; /// Ibuprofen density = 1.03 g/cm^3, mass = volume * density => kg
                double arterialTreeVolume_l = getArterialTreeVolume() * 1000.0f; // m^3 * 1000 => liters
                if (absorption_simulator.ini_concentration == 0.0f)
                    absorption_simulator.initialize(agentAmount_kg / arterialTreeVolume_l, current_time);
                
                //Console.WriteLine("Absorption : " + (absorption_simulator.WagnerNelsonAbsorption(current_time)));
                //Console.WriteLine("Concentration : " + (absorption_simulator.getCurrConcentration(current_time) - absorption_simulator.WagnerNelsonAbsorption(current_time)));
            }

            if (current_time > 10.0f && !File.Exists(@"snapshot.txt"))
            {
                System.IO.StreamWriter snapshot_data_file = new System.IO.StreamWriter(@"snapshot.txt");
                IO_Module.WriteSnapshot(v_net.vascular_system, snapshot_data_file);
            }

            /// Re entering the substance into the system according the dt of each outlet node
            double total_amount = 0.0f;
            double heart_amount = heart_amount_agent;

            for (int i = 0; i < re_entering_delta.Count; ++i)   // 2 outlets
            {
                // the segment length for the last node
                double dz = Vector3.Distance(outlet_bc_set[i].core_node.position, outlet_bc_set[i].core_node.neighbours[0].position);
                // calculate the agent outflow from agent_c, lumen area and velocity; note this formula ignores the diffusion term
                double agent_flow = outlet_bc_set[i].core_node.neighbours[0].agent_c *
                                    outlet_bc_set[i].core_node.neighbours[0].lumen_area *
                                    outlet_bc_set[i].core_node.neighbours[0].velocity;
                
                outlets_agent_amount[i].Enqueue(agent_flow);

                double agent_sum = 0.0f;
                if (current_time >= re_entering_delta[i])   // 18.477 seconds aprox for Y_test
                {
                    if (i == 0) // Gastric system compartment
                    {
                        if (insert_type == "O")
                        {
                            if ((current_time > (Program.STABILISATION_TIME + 0.5)) && (agent_in == true))
                            {
                                double const_init_amount = 0.00886226925452758;
                                total_amount += Math.Pow((1.0f - Convert.ToDouble(k_el / 10000.0f) - Convert.ToDouble((k_el * 10.0f) / 10000.0f)), (re_entering_delta[i] * 10000f)) * const_init_amount;
                                agent_in = false;
                            }
                            else
                            {
                                total_amount += Math.Pow((1.0f - Convert.ToDouble(k_el / 10000.0f) - Convert.ToDouble((k_el * 10.0f) / 10000.0f)), (re_entering_delta[i] * 10000f)) * outlets_agent_amount[i].Dequeue();
                            }
                        }
                        else
                            total_amount += Math.Pow((1.0f - Convert.ToDouble(k_el / 10000.0f) - Convert.ToDouble((k_el * 10.0f) / 10000.0f)), (re_entering_delta[i] * 10000f)) * outlets_agent_amount[i].Dequeue();
                    }
                    else // Renal system compartment
                    {
                        total_amount += Math.Pow((1.0f - Convert.ToDouble(k_el / 10000.0f) - Convert.ToDouble((k_el * 20.0f) / 10000.0f)), (re_entering_delta[i] * 10000f)) * outlets_agent_amount[i].Dequeue();
                    }
                    // total_amount += Math.Pow((1.0f - Convert.ToDouble(k_el / 10000.0f)), (re_entering_delta[i] * 10000f)) * outlets_agent_amount[i].Dequeue();
                }
            }
            ///TODO: add time averaging to the queues
            total_amount = total_amount + heart_amount;
            double dz_inlet = Vector3.Distance(v_net.bounds[InletId].core_node.position, v_net.bounds[InletId].core_node.neighbours[0].position);
            if (v_net.bounds[InletId].core_node.neighbours[0].velocity <= 0)
            {
                heart_amount = total_amount;
                v_net.bounds[InletId].core_node.agent_c = 0;
            }
            else
            {
                v_net.bounds[InletId].core_node.agent_c = total_amount / v_net.bounds[InletId].core_node.neighbours[0].velocity / v_net.bounds[InletId].core_node.neighbours[0].lumen_area;
                heart_amount = 0;
            }
            //v_net.bounds[InletId].core_node.agent_c = v_net.bounds[InletId].core_node.neighbours[0].agent_c;
            //Console.WriteLine("inlet dz " + dz_inlet + " inlet lumen " + v_net.bounds[InletId].core_node.neighbours[0].lumen_area_0);
            heart_amount_agent = heart_amount;

            foreach (var tr in v_net.threads)
            {
                tr.updateState();
                tr.updateStateFFR();
            }

            if (current_time >= Program.END_TIME - GlobalDefs.HEART_PERIOD)
            {
                inlet_num = IO_Module.getInletNum(Program.filename);
                inlet_index = -1;
                int bounds_Num = 0;
                foreach (var bound in v_net.bounds)
                {
                    bounds_Num++;
                }
                for (int i = 0; i < bounds_Num; i++)
                {
                    if (v_net.bounds[i] != null)
                    {
                        if (v_net.bounds[i].core_node.id == inlet_num)
                        {
                            inlet_index = i;
                        }
                    }
                }
                last_period_inlet_flow_sum = last_period_inlet_flow_sum + v_net.bounds[inlet_index].core_node.velocity * v_net.bounds[inlet_index].core_node.lumen_area * 1e6;
                last_period_counts_Num++;
                last_period_inlet_flow_av = last_period_inlet_flow_sum / last_period_counts_Num;
            }

            foreach (var cp in control_point_list)
            {
                cp.addFluxVal();
                cp.addPrsVal();
                cp.addLumenVal();
            }
            
        }

        public SolutionState Control()
        {
            solution_state = SolutionState.CONTINUE;

            if (end_time > 0)
                if (current_time >= end_time)
                    solution_state = SolutionState.FINISHED;

            if (current_time % stenosis_av_period < delta_tau)
                foreach (var cp in control_point_list)
                    cp.reset();

            foreach (var bc in v_net.bounds)
            {
                if (bc != null && bc.ValueControl() >= 0)
                    solution_state = SolutionState.ERROR;
            }

            foreach (var kn in v_net.knots)
            {
                if (kn.NaNControl() >= 0)
                    solution_state = SolutionState.ERROR;
            }
            return solution_state;
        }

        public void addControlPoint(int node_id)
        {
            //  if (!control_point_list.Exists(x => x.node.id == node_id))
            {
                NodeSummary nd_summary = new NodeSummary();
                nd_summary.setNode(v_net.vascular_system.Find(x => x.id == node_id), 2);
                control_point_list.Add(nd_summary);
            }
        }

        public NodeSummary getContorlPoint(int node_id)
        {
            try
            {
                return control_point_list.Find(x => x.node.id == node_id);
            }
            catch
            {
                return null;
            }
        }

        public void FullReset()
        {
            v_net.fullReset();
            clotes_id.Clear();
            current_time = 0;

            foreach (var cp in control_point_list)
                cp.reset();

            solution_state = SolutionState.CONTINUE;
        }

        public bool removeLastClot()
        {
            if (!clotes_id.Any())
                return false;

            v_net.removeClot(clotes_id.Pop());
            return true;
        }

        public List<int> getClotesId()
        {
            return clotes_id.ToList();
        }

        public void removeAllClots()
        {
            while (removeLastClot())
            { }
        }

        public void addClot(int node_id, float degree)
        {
            if (degree > 0.99)
                degree = 0.98f;

            if (v_net.setCloth(node_id, degree))
                clotes_id.Push(node_id);
        }

        public void addFFRClot(int node_id, float degree)
        {
            if (degree > 0.99)
                degree = 0.98f;

            if (v_net.setFFRCloth(node_id, degree))
                clotes_id.Push(node_id);
        }

        public bool isClotSet()
        {
            if (clotes_id.Count == 0)
                return false;

            return true;
        }

        public IWriteVascularNet getVNet()
        {
            return v_net;
        }


        public List<int> getCenterNodesID(int min_len, float max_R)
        {
            List<int> center_nodes_id = new List<int>();
            foreach (var tr in this.v_net.threads)
                if (tr.nodes.GetLength(0) >= min_len && tr.nodes[tr.nodes.GetLength(0) / 2].radius < max_R * 1e-3)
                    center_nodes_id.Add(tr.nodes[tr.nodes.GetLength(0) / 2].id);
            return center_nodes_id;
        }
		
		public void setAgentInletId (int ag_inl_id)
        {
            int id = v_net.bounds.FindIndex(x => x.core_node.id == ag_inl_id);
            v_net.bounds[id].agent_in = true;
            agent_inlet_id = ag_inl_id;
        }
		
		public int agent_inlet_id;

        public List<NodeSummary> control_point_list;
        public SolutionState solution_state;
        protected VascularNet v_net;
        protected AASimulator absorption_simulator = new AASimulator();
        public int InletId;
		public List<PressureOutletRCR> outlet_bc_set;
        public List<double> re_entering_delta;
        public Queue<double>[] outlets_agent_amount;
        public double[] outlets_agent_avg;
        public double k_el = 0.32f / 3600.0f; // 0.2f / 60.0f; // 0.32f / 3600.0f;   // 0.2, 1/min epinephrine | 0.32, 1/hour ibuprofen
        public double heart_amount_agent = 0;
        public string insert_type;
        public bool agent_in = true;

        //  public NodeSummary[] thread_summary;

        protected bool init;

        public float delta_tau { get; protected set; }
        public int timestep_N { get; protected set; }
        public double current_time { get; protected set; }
        public float stenosis_av_period { get; protected set; }
        //       public float stabilisation_time { get; protected set; }       

        public float output_step { get; set; }
        public float end_time { get; set; }

        protected Stack<int> clotes_id = new Stack<int>();
          
        public static double diss_func_sum_threads;
        public static int last_period_counts_Num = 0;
        public static double last_period_inlet_flow_sum = 0;
        public static double last_period_inlet_flow_av;
    }
}