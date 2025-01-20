using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;

namespace BloodFlow
{
    public enum adjustMode { AbsInit, AllInit, All, Personal, None };
    public enum CalculationMode { None, Stabilisation, WriteState, ReferenceAveraging, GetReferenceFlux, ClotAveraging, GetClotFlux, ClotStabilisations, RemoveClot, AddClot, ResetAveraging, FullResetAvergaing };

    class Program
    {
        public static float TIMESTEP = 0.5e-4f;
        public static float AV_TIME = 1.0f;
        public static float END_TIME = 12.0f; // end time of the simulation, redefined for stenosis case
        public static float WRITE_TIME = 2.0f; // time to start writing output dynamics file

        public static float clot_set_time = 0.0f;
       // public static bool SNAPSHOT_AVAILABLE = File.Exists("snapshot.txt");
        public static bool SNAPSHOT_AVAILABLE = false;   
        public static float STABILISATION_TIME = SNAPSHOT_AVAILABLE ? 0.0f : 10.0f;

        public static float CLOT_RELAXATION_PERIOD = 1.0f;
        public static float CLOT_REMOVE_RELAXATION_PERIOD = 1.0f;
        public static float OUTPUT_PERIOD = 0.01f;
        public static BFSimulator bf_simulation;

        
        public static string filename;
        public static int diss_mode = 1; // "0" stands for the non-modulus mode, "1" stands for the modulus mode
        public static int knot_agent_mode = 1; // "1" stands for 1D recalculation of agent_c in knots, "3" stands for 3D recalculation of agent_c in knots


        static void Main(string[] args)
        {
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            //BFSimulator bf_simulation = new BFSimulator("", @"verefication\full_body_Boileau_LR_2.5mm.top", GlobalDefs.getBoileauBeta);
            IO_Module.localization = "en-US";
            string top_filename = "";
            string parameters_filename = "";
            string cll_parameters_filename = "";
            string out_filename = "";
            string stream_out_filename = "";
            string insertion_type = "IV"; // IV = intravenous, O = oral

            bool agent_inj = false;
            int agent_in_id = 0;

            List<ClotDscr> stenosis_task = new List<ClotDscr>();
            List<Tuple<int, string>> inlet_data = new List<Tuple<int, string>>();
            List<double> agent_mass_average = new List<double>();

            filename = args[0];

            string base_path = IO_Module.readTaskFile(args[0], ref top_filename, ref parameters_filename, ref cll_parameters_filename, ref inlet_data, ref stenosis_task, ref out_filename, ref agent_in_id, ref agent_inj);

            string dyn_out_filename = "temp.dyn";
            string diss_filename = "dissipation.txt";
            string heart_period_filename = "heart_period.txt";
            string timestep_filename = "timestep.txt";
            string output_period_filename = "output_period.txt";
            string out_summary_filename = "test_summary.txt";

            if (args.GetLength(0) > 1)
            {
                out_filename = base_path + out_filename;
                dyn_out_filename = base_path + dyn_out_filename;
                diss_filename = base_path + diss_filename;
                heart_period_filename = base_path + heart_period_filename;
                timestep_filename = base_path + timestep_filename;
                output_period_filename = base_path + output_period_filename;
                out_summary_filename = base_path + out_summary_filename;
            }
            System.IO.StreamWriter out_dynamics_file = new System.IO.StreamWriter(dyn_out_filename);
            System.IO.StreamWriter propagation_data_file = new System.IO.StreamWriter("propagation.txt");
            System.IO.StreamWriter concentration_data_file = new System.IO.StreamWriter("concentration.txt");
            System.IO.StreamWriter mass_conservation_data_file = new System.IO.StreamWriter("mass_conservation.txt");
            stream_out_filename = out_filename + "s";
            
            bf_simulation = new BFSimulator("", base_path + top_filename, insertion_type, inlet_data.Select(t => t.Item1).ToArray()); // reading of topology and creating of VascularNet
            foreach (var inlt in inlet_data)
            {
                bf_simulation.setInletBC(File.ReadAllText(base_path + inlt.Item2), inlt.Item1, InletType.FLUX);
                if ((agent_inj == true) && (inlt.Item1 == agent_in_id))
                {
                    bf_simulation.setAgentInletId(agent_in_id);
                }
            }
			
            var body_part_lst = new List<BodyPartInlet>();

            if (parameters_filename != "")
                bf_simulation.setOutletBC(File.ReadAllText(base_path + parameters_filename)); // define OutletBC

            if (cll_parameters_filename != "")
                bf_simulation.setCollaterals(File.ReadAllText(base_path + cll_parameters_filename));

            //    var bc_list = bf_simulation.setOutletBC(33,  body_part_lst, 30.0e-3f, 1000*1e-12f, 7.851e9f);

            // IO_Module.WriteRCR("new_lca_par.par", bc_list);

            List<NodeSummary> control_points = new List<NodeSummary>(stenosis_task.Count);
            for (int i = 0; i < stenosis_task.Count; i++)
            {
                //  if (control_points.FindIndex(x => x.node.id == stenosis_task[i].node_id)==-1)
                bf_simulation.addControlPoint(stenosis_task[i].node_id);
            }

            ClotDscr curr_clot = null;
            float clot_timer = 0;
            float clot_period = 10; //time for one stenosis value simulation, 1st one is placed "clot_period" seconds after the start
            float relaxation_time = 1.0f; //time before inserting a new stenosis value
            int curr_stenosis_id = 0;

            if (stenosis_task.Count > 0)
            {
                END_TIME = Math.Max(END_TIME, (stenosis_task.Count + 1) * (clot_period + relaxation_time) + 1);
            }

            bf_simulation.setTimeParameters(TIMESTEP, END_TIME, AV_TIME);

            double diss_func_sum = 0;
            double diss_func_sum_norm = 0;
            while (true)
            {
                if (bf_simulation.current_time % AV_TIME < bf_simulation.delta_tau)
                {
                    if (bf_simulation.current_time > relaxation_time + 1.0)
                    {
                        if (clot_timer >= clot_period - AV_TIME && curr_clot != null && bf_simulation.isClotSet())
                        {
                            NodeSummary summary = bf_simulation.getContorlPoint(curr_clot.node_id);
                            curr_clot.depressed_flux = (float)summary.get_av_flux();
                            curr_clot.rad = (float)summary.getRad();
                            curr_clot.rad_real = (float)summary.getRadReal();

                            double av_prs;
                            double prs_drop;
                            double p_distal;
                            double p_proximal;
                            av_prs = summary.get_av_prs(out prs_drop, out p_proximal, out p_distal);
                            curr_clot.p_av = (float)av_prs;
                            curr_clot.pressure_drop = (float)prs_drop;
                            curr_clot.pressure_distal = (float)p_distal;
                            curr_clot.pressure_proximal = (float)p_proximal;
                            curr_clot.is_ready = true;
                            bf_simulation.removeLastClot();
                            curr_clot = null;
                            curr_stenosis_id++;

                            string out_text = "";
                            for (int i = 0; i < stenosis_task.Count; i++)
                                out_text += stenosis_task[i].node_id + " R0: " + (stenosis_task[i].rad_0 * 1e3).ToString("F3") + " R: " + (stenosis_task[i].rad * 1e3).ToString("F3") + " R real: " + (stenosis_task[i].rad_real * 1e3).ToString("F5") + " ref flux: " +
                                    (stenosis_task[i].reference_flux * 1e6).ToString("F4") + " degree: " + stenosis_task[i].degree.ToString("F2") + " depressed flux: " +
                                    (stenosis_task[i].depressed_flux * 1e6).ToString("F4") + " Pp: " + (stenosis_task[i].pressure_proximal).ToString("F2") +
                                    " Pd: " + (stenosis_task[i].pressure_distal).ToString("F2") + " delta P: " + (stenosis_task[i].pressure_drop).ToString("F4") + " Pav: " + (stenosis_task[i].p_av).ToString("F2") + "\n";

                            File.WriteAllText(out_filename, out_text);
                        }

                        if (clot_timer >= clot_period && stenosis_task.Count != 0 && !bf_simulation.isClotSet())
                        {
                            curr_clot = stenosis_task[curr_stenosis_id];
                            bf_simulation.addClot(curr_clot.node_id, curr_clot.degree);
                            // bf_simulation.addFFRClot(curr_clot.node_id, curr_clot.degree);
                            //bf_simulation.addContorlPoint(curr_clot.node_id);
                            clot_timer = 0;
                        }

                        if ((stenosis_task.Count != 0) && (curr_stenosis_id == stenosis_task.Count))
                        {
                            END_TIME = (float)bf_simulation.current_time;  
                        }
                    }

                    // calculate the value of flow with no stenosis
                    if (Math.Abs((clot_period - relaxation_time) - bf_simulation.current_time) < bf_simulation.delta_tau)
                    {
                        for (int i = 0; i < stenosis_task.Count; i++)
                        {
                            stenosis_task[i].reference_flux = (float)bf_simulation.control_point_list[i].get_av_flux();
                            stenosis_task[i].rad_0 = (float)bf_simulation.control_point_list[i].getRad();
                        }
                    }
                }

                if (bf_simulation.current_time % OUTPUT_PERIOD < bf_simulation.delta_tau)
                {
                    if (bf_simulation.current_time >= END_TIME && curr_stenosis_id == stenosis_task.Count)
                    {
                        string out_text = "";
                        for (int i = 0; i < stenosis_task.Count; i++)
                            out_text += stenosis_task[i].node_id + " R0: " + (stenosis_task[i].rad_0 * 1e3).ToString("F3") + " R: " + (stenosis_task[i].rad * 1e3).ToString("F3") + " R real: " + (stenosis_task[i].rad_real * 1e3).ToString("F5") + " ref flux: " + (stenosis_task[i].reference_flux * 1e6).ToString("F4") + " degree: " +
                                stenosis_task[i].degree.ToString("F2") + " depressed flux: " + (stenosis_task[i].depressed_flux * 1e6).ToString("F4") + " Pp: " + (stenosis_task[i].pressure_proximal).ToString("F2") +
                                " Pd: " + (stenosis_task[i].pressure_distal).ToString("F2") + " delta P: " + (stenosis_task[i].pressure_drop).ToString("F4") + " Pav: " + (stenosis_task[i].p_av).ToString("F2") + "\n";

                        File.WriteAllText(out_filename, out_text);
                        break;
                    }

                    if (bf_simulation.isClotSet())
                        foreach (var id in bf_simulation.getClotesId())
                            Console.WriteLine("Clot id: " + id);

                    // State output, disabled if WRITE_TIME==0 
                    if (WRITE_TIME != 0 && bf_simulation.current_time >= WRITE_TIME)
                    {
                        IO_Module.WriteState(bf_simulation.current_time, bf_simulation.getVNet(), out_dynamics_file, propagation_data_file);
                    }

                    if (bf_simulation.solution_state == SolutionState.ERROR)
                    {
                        Console.WriteLine("Error; physical time: " + bf_simulation.current_time);
                        break;
                    }

                    if (bf_simulation.solution_state == SolutionState.FINISHED)
                    {
                        Console.WriteLine("Physical time: " + bf_simulation.current_time);
                        Console.WriteLine("End");
                        break;
                    }
                }

                bf_simulation.Control();
                bf_simulation.Update();
                clot_timer += bf_simulation.delta_tau;

                if (bf_simulation.current_time % OUTPUT_PERIOD < bf_simulation.delta_tau)
                {
                    double agent_mass_conservation = bf_simulation.getAgentMass() + bf_simulation.heart_amount_agent;
                    agent_mass_average.Add(agent_mass_conservation);

                    Console.WriteLine("Time: " + bf_simulation.current_time);
                    Console.WriteLine("Mass conservation check, total agent mass: " + agent_mass_conservation);
                    Console.WriteLine("Agent Concentration: " + bf_simulation.getCurrentConcentration());
                   
                    IO_Module.WriteConcentration(bf_simulation.current_time, bf_simulation.getCurrentConcentration(), concentration_data_file);
                    IO_Module.WriteConservation(bf_simulation.current_time, agent_mass_conservation, mass_conservation_data_file);

                    if (bf_simulation.current_time % 1.0f < 0.01f)   // each second
                    {
                        double sum_mass_conservation = 0.0f;
                        foreach (double mass_conservation in agent_mass_average)
                            sum_mass_conservation += mass_conservation;
                        Console.WriteLine("Mass of the agent average over the last second: " + (sum_mass_conservation / agent_mass_average.Count));
                        agent_mass_average.Clear();
                    }

                    /*               string text = File.ReadAllText(filename);
                                   string[] lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                                   string[] path_tmp = filename.Split(new[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);

                                   Regex regex = new Regex(@"^InletFlux:\s+(.+)$", RegexOptions.IgnoreCase);
                                   for (int i = 0; i < lines.GetLength(0); i++)
                                   {
                                       Match inlet_match = regex.Match(lines[i]);
                                       if (inlet_match.Groups.Count != 2)
                                           continue;

                                       string[] inlets = inlet_match.Groups[1].Value.Split(',');
                                       foreach (var inlt in inlets)
                                       {
                                           string[] s = inlt.Trim().Split(' ');
                                           inlet_num = int.Parse(s[0]);
                                       }
                                   }  */

                }
                diss_func_sum = diss_func_sum + BFSimulator.diss_func_sum_threads;
            }
            diss_func_sum_norm = diss_func_sum / BFSimulator.last_period_inlet_flow_av;            
            List<string> lines = new List<string>();
            lines.Add(Convert.ToString(diss_func_sum_norm));
            System.IO.File.WriteAllLines(diss_filename, lines);

            File.WriteAllText(heart_period_filename, Convert.ToString(GlobalDefs.HEART_PERIOD));
            File.WriteAllText(timestep_filename, Convert.ToString(TIMESTEP));
            File.WriteAllText(output_period_filename, Convert.ToString(OUTPUT_PERIOD));

      


    //        double R1;
     //       R1 = GlobalDefs.getR1(0.15500003 * 1e-3) * 1e-9;
     //       Console.WriteLine("{0}", Convert.ToString(R1));
        }
    }
}