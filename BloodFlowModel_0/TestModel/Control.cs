using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BloodFlow
{
    public struct FlowSolution
    {
        int[] ids;
        float[] velocity;
        float[] pressure;
        Vector3 dirVectors;
    }

    public struct PressureCht
    {
        public PressureCht(double s, double d)
        {
            sistolic = s;
            diastolic = d;
            p_mean = sistolic + 1 / 3.0f * (sistolic - diastolic);
        }
        public double sistolic;
        public double diastolic;
        public double p_mean;
    }


    public struct PulseState
    {
        public FlowSolution solution;
        public double Q_max, Q_min;
        public double Ps, Pd;
        public double t_q_max, t_q_min;
        public double td, ts;
        public double Q_tot, Q_average;
        public double time_begin, time_end;
        public double period;
    }

    public class BC_spec
    {
        

        public BC_spec(BoundaryCondition bc_node)
        {
            w_node = bc_node;
            Q_on_t = new List<double>();
            P_on_t = new List<double>();
            time = new List<double>();
            state_history = new List<PulseState>();
            CalcSpec();
        }

        public void Reset()
        {
            Q_on_t.Clear();
            P_on_t.Clear();
            time.Clear();
            period = 0;
        }

        public void Record(double _t)
        {
            if (w_node.core_node.id == 1257)
                w_node.core_node.id = 1257;
            Q_on_t.Add(-w_node.core_node.velocity * w_node.core_node.lumen_area);
            P_on_t.Add(w_node.core_node.pressure);
            time.Add(_t);
        }

        public void CalcSpec()
        {
            PulseState new_state = new PulseState();
            try
            {
                new_state.Q_max = Q_on_t.Max();
                new_state.Q_min = Q_on_t.Min();

                new_state.Ps = P_on_t.Max();
                new_state.Pd = P_on_t.Min();
                int id = Q_on_t.FindIndex(x => x == new_state.Q_min);
                new_state.t_q_min = time[id];
                id = Q_on_t.FindIndex(x => x == new_state.Q_max);
                new_state.t_q_max = time[id];

                new_state.td = time[P_on_t.FindIndex(x => x == new_state.Ps)];
                new_state.ts = time[P_on_t.FindIndex(x => x == new_state.Pd)];

                Q_tot = 0;
                period = 0;
                for (int i = 0; i < Q_on_t.Count() - 1; i++)
                {
                    Q_tot += (Q_on_t[i] + Q_on_t[i + 1]) / 2 * (time[i + 1] - time[i]);
                    period += (time[i + 1] - time[i]);
                }
                new_state.period = period;
                new_state.time_begin = time[0];
                new_state.time_end = time[Q_on_t.Count() - 1];
                new_state.Q_average = (Q_tot / period);
                new_state.Q_tot = Q_tot;

                this.state_history.Add(new_state);
            }
            catch (Exception e)
            {
                this.state_history.Add(new_state);
            }

            update();
        }

        public PulseState getState(int seq)
        {
            return state_history[state_history.Count - 1 - seq];
        }

        public void update()
        {
            Q_max = state_history.Last().Q_max;
            Q_min = state_history.Last().Q_min;
            Pd = state_history.Last().Pd;
            Ps = state_history.Last().Ps;

            t_q_max = state_history.Last().t_q_max;
            t_q_min = state_history.Last().t_q_min;

            td = state_history.Last().td;
            ts = state_history.Last().ts;
            Q_tot = state_history.Last().Q_tot;
            Q_averge = state_history.Last().Q_average;
            period = state_history.Last().period;
        }


        private BoundaryCondition w_node;

        public List<double> Q_on_t;
        public List<double> P_on_t;
        public List<double> time;

        private List<PulseState> state_history;

        public double Q_max, Q_min, Pd, Ps;
        public double t_q_max, t_q_min, td, ts;
        public double Q_tot, Q_averge, period;
    }   

    public class RCRBCController
    {
        public RCRBCController(double _P_trg_dst, double _P_trg_sst, double _pulse_period)
        {
            P_trg_dst = _P_trg_dst;
            P_trg_sst = _P_trg_sst;
            P_mean = P_trg_dst + 1 / 3.0f * (P_trg_sst - P_trg_dst);

            pulse_period = _pulse_period;

            start_time = 0;
            stop_time = 0;
            record_time = 0;

            terminal_pressure = null;
        }

        public RCRBCController(string term_pressure_filename, double _P_trg_dst, double _P_trg_sst, double _pulse_period)
        {            
            IO_Module.LoadTerminalPressure(term_pressure_filename,out  terminal_pressure);

            pulse_period = _pulse_period;
            start_time = 0;
            stop_time = 0;
            record_time = 0;         

            BP_trg_dst = _P_trg_dst;
            BP_trg_sst = _P_trg_sst;
            BP_mean = P_trg_dst + 1 / 3.0f * (P_trg_sst - P_trg_dst);
        }

        

        public void setPulsePeriod(double _pulse_period)
        {
            pulse_period = _pulse_period;
        }

        public void setBCset(List<PressureOutletRCR> _BC_set)
        {
            BC_set = _BC_set;
            BC_set_spec = new List<BC_spec>();
            BC_spec_dict = new Dictionary<PressureOutletRCR, BC_spec>();
            foreach (var bc in _BC_set)
            {
                BC_set_spec.Add(new BC_spec(bc));
                BC_spec_dict.Add(bc, BC_set_spec.Last());
            }
        }

        public void reset()
        {
            foreach (var bc in BC_set_spec)
                bc.Reset();

            record_time = 0;
            start_time = 0;
            stop_time = 0;
        }

        public bool record(double curr_time)
        {
            if (start_time == 0)
                start_time = curr_time;

            if (record_time >= pulse_period - 1e-5)
                return false;

            foreach (var bc in BC_set_spec)
                bc.Record(curr_time);

            record_time = curr_time - start_time;

            if (record_time >= pulse_period)
                stop_time = curr_time;

            return true;
        }

        public bool getPressureConvergence(float relTol, out double av_SystPressure, out double av_DstPressure)
        {
            double max_pressure_diff = 0;
            double max_pressure = 0;
            av_SystPressure = 0;
            av_DstPressure = 0;
            foreach (var bc in BC_set)
            {
                BC_spec bc_spec = BC_spec_dict[bc];
                bc_spec.CalcSpec();             

                double prev_Pd = bc_spec.getState(1).Pd;
                double prev_Ps = bc_spec.getState(1).Ps;

                Console.Write(bc.core_node.id.ToString()+": " + bc_spec.Ps.ToString() + " " + bc_spec.Pd.ToString() + "\n");

                av_DstPressure += bc_spec.Pd;
                av_SystPressure += bc_spec.Ps;


                if (Math.Abs(prev_Pd - bc_spec.Pd) > max_pressure_diff)
                {
                    max_pressure_diff = Math.Abs(prev_Pd - bc_spec.Pd);
                    max_pressure = bc_spec.Ps;
                }
                if (Math.Abs(prev_Ps - bc_spec.Ps) > max_pressure_diff)
                {
                    max_pressure_diff = Math.Abs(prev_Ps - bc_spec.Ps);
                    max_pressure = bc_spec.Ps;
                }
            }
            av_SystPressure = av_SystPressure / BC_set.Count;
            av_DstPressure = av_DstPressure / BC_set.Count;
            return (max_pressure_diff < max_pressure * relTol);
        }

        public bool getFluxConvergence(float relTol)
        {
            double max_flux_diff = 0;
            double max_flux = 0;

            foreach (var bc in BC_set)
            {
                BC_spec bc_spec = BC_spec_dict[bc];
                bc_spec.CalcSpec();                

                double prev_Q_max = bc_spec.getState(1).Q_max;
                double prev_Q_min = bc_spec.getState(1).Q_min;

                if (Math.Abs(prev_Q_min - bc_spec.Q_min) > max_flux_diff)
                {
                    max_flux_diff = Math.Abs(prev_Q_min - bc_spec.Q_min);
                    max_flux = Math.Abs(bc_spec.Q_max);
                }
                if (Math.Abs(prev_Q_max - bc_spec.Q_max) > max_flux_diff)
                {
                    max_flux_diff = Math.Abs(prev_Q_max - bc_spec.Q_max);
                    max_flux = Math.Abs(bc_spec.Q_max);
                }
            }
            return (max_flux_diff < relTol * max_flux);
        }

        public void adjustRCRbyFlux(adjustMode adj_mode)
        {
            if (adj_mode == adjustMode.None)
                return;
            double total_outlet_cube = 0;

            P_mean = P_trg_dst + 1 / 3.0f * (P_trg_sst - P_trg_dst);

            double Q_averge = 113.2e-6;
            double C_total = 1800.0e-12;
            double RT = 0.051e9;//P_mean / Q_averge;

            foreach (var bc in BC_set)
            {
                total_outlet_cube += Math.Pow(bc.core_node.radius, 3);                
            }

            foreach (var bc in BC_set)
            {
                double Rt = RT * total_outlet_cube / Math.Pow(bc.core_node.radius, 3);
                double lument_dst = bc.calcLumenArea(P_trg_dst);
                double c_dst = Math.Sqrt(bc.beta_1 / 2.0f / GlobalDefs.BLOOD_DENSITY) * Math.Pow(lument_dst, 0.25f);
                double R1 = GlobalDefs.BLOOD_DENSITY * c_dst / bc.calcLumenArea(P_trg_dst);
                double R2 = Rt - R1;
                if (R2 < 0)
                {
                    R2 = R1 - Rt;
                    R1 = R1 - R2;
                }   
                double C = C_total * RT / Rt;
                bc.C = C;
                bc.R2 = R2;
                bc.R1 = R1;
            }
            
        }

        public void adjustRCR(adjustMode adj_mode)
        {
            if (adj_mode == adjustMode.None)
            {
                IO_Module.WriteRCR("params.par", BC_set);
                return;
            }

            foreach (var bc in BC_set)
            {
                PressureCht p_chrt;
                if (terminal_pressure != null&&terminal_pressure.TryGetValue(bc.core_node.id, out p_chrt))
                {
                    P_trg_dst = p_chrt.diastolic;
                    P_trg_sst = p_chrt.sistolic;
                    P_mean = p_chrt.p_mean;                    
                }
                else
                {
                    if (adj_mode == adjustMode.Personal)
                        continue;
                    P_trg_dst = BP_trg_dst;
                    P_trg_sst = BP_trg_sst;
                    P_mean = P_trg_dst + 1 / 3.0f * (P_trg_sst - P_trg_dst);
                }

                BC_spec bc_spec = BC_spec_dict[bc];
                bc_spec.CalcSpec();

                double lument_dst = bc.calcLumenArea(P_trg_dst);
                double c_dst = Math.Sqrt(bc.beta_1 / 2.0f / GlobalDefs.BLOOD_DENSITY) * Math.Pow(lument_dst, 0.25f);


                

                double RT = P_mean / bc_spec.Q_averge;
                double R2 = 0;
                double R1 = 0;
                double C = 0;

                R1 = GlobalDefs.BLOOD_DENSITY * c_dst / bc.calcLumenArea(P_trg_dst);

                if (adj_mode == adjustMode.AllInit)
                {
                    

                    C = (bc_spec.Q_max - bc_spec.Q_min) / (P_trg_sst - P_trg_dst) * Math.Abs(bc_spec.t_q_max - bc_spec.t_q_min);                 
                    R1 = GlobalDefs.BLOOD_DENSITY * c_dst / bc.calcLumenArea(P_trg_dst);
                    R2 = RT - R1;

                    if (R2 < 0)
                    {
                        R1 = 1e3;
                        R2 = 1e3;
                        C = 0.1e-13;
                        continue;
                    }
                }
                else
                /*
                                {
                                       C  = bc.C ; 
                                       R1 = bc.R1;
                                       R2 = bc.R2;
                                }

                                simPd = bc_spec.Pd;
                                simPs = bc_spec.Ps;

                                double P_pulse = (bc_spec.Ps - bc_spec.Pd);
                                double P_pulse_trg = (P_trg_sst - P_trg_dst);

                                RT = RT + (P_trg_dst - simPd) / (bc_spec.Q_averge) * 0.1;
                                R2 = RT - R1;

                                C = C + (bc_spec.Q_max - bc_spec.Q_min) / (P_pulse * P_pulse) * Math.Abs(bc_spec.t_q_max - bc_spec.t_q_min) * (P_pulse - P_pulse_trg) * 0.1;

                                if (C < 0 || R2 < 0)
                                {
                                    P_mean = P_trg_dst + 1 / 3.0f * (P_trg_sst - P_trg_dst);
                                    RT = P_mean / bc_spec.Q_averge;

                                    C = (bc_spec.Q_max - bc_spec.Q_min) / (P_trg_sst - P_trg_dst) * Math.Abs(bc_spec.t_q_max - bc_spec.t_q_min);
                                    R1 = GlobalDefs.BLOOD_DENSITY * c_d / bc.core_node.lumen_sq_d;                   
                                    R2 = RT - R1;

                                    if (R2 < 0)
                                    {
                                        R1 = RT;
                                        R2 = 1e3;
                                    }
                                }
                 */
                    
                {
                    C  = bc.C;
                    R1 = bc.R1;
                    R2 = bc.R2;

                    int num_of_periods = 10;
                    int dec = 3;
                    int max_adj_cycles = 100;

                    for (int N = 0; N < max_adj_cycles; N++)
                    {
                        this.PressureRCRSimularot(num_of_periods, dec, bc_spec.Q_on_t, bc_spec.time, R1, R2, C);
                        double P_pulse = (simPs - simPd);
                        double P_pulse_trg = (P_trg_sst - P_trg_dst);

                     /*   if (Math.Abs(simPs - P_trg_sst) > P_trg_sst || Math.Abs(simPd - P_trg_dst) > P_trg_sst)
                        {
                            R1 = GlobalDefs.BLOOD_DENSITY * c_dst / bc.calcLumenSq(P_trg_dst);
                            R2 = R1 - P_mean / bc_spec.Q_averge;
                            C = (bc_spec.Q_max - bc_spec.Q_min) / (P_trg_sst - P_trg_dst) * Math.Abs(bc_spec.t_q_max - bc_spec.t_q_min);
                            if (R2 < 0)
                                R2 = 1.0e6;                           
                        }*/


                        if (Math.Abs(P_pulse - P_pulse_trg) < 100 && Math.Abs(simPd - P_trg_dst) < 100)
                            break;

                        RT = RT + (P_trg_dst - simPd) / (bc_spec.Q_averge) * 0.01;// *bc_spec.Q_averge / 10e-4;
                        R2 = RT - R1;
                        if (R2 < 0 || R1 < 0)
                        {
                            R1 = RT;//GlobalDefs.BLOOD_DENSITY * c_dst / bc.calcLumenSq(P_trg_dst);
                            C = (bc_spec.Q_max - bc_spec.Q_min) / (P_trg_sst - P_trg_dst) * Math.Abs(bc_spec.t_q_max - bc_spec.t_q_min); 
                            R2 = 1e3;                 
                        }

                        C = C + (bc_spec.Q_max - bc_spec.Q_min) / (P_pulse * P_pulse) * Math.Abs(bc_spec.t_q_max - bc_spec.t_q_min) * (P_pulse - P_pulse_trg) * 0.01;
                        if (C < 0)                       
                            C = (bc_spec.Q_max - bc_spec.Q_min) / (P_trg_sst - P_trg_dst) * Math.Abs(bc_spec.t_q_max - bc_spec.t_q_min);                             
                        

                        if(double.IsNaN(C))
                            C = 1e-15;

                    }
                    
                }

                bc.C = C;
                bc.R1 = R1;
                bc.R2 = R2;                     
            }

            IO_Module.WriteRCR("params.par", BC_set);
        }

        public void WriteRCRParams(string filename)
        {
            IO_Module.WriteRCR(filename, BC_set);
        }

        public void PressureRCRSimularot(int num_of_periods, int decimation, List<double> Q_on_t, List<double> time,
                                         double R1, double R2, double C)
        {
            double P_next = P_trg_dst; double P_curr = P_trg_dst;

            simPd = double.MaxValue;
            simPs = 0;

            for (int N = 0; N < num_of_periods; N++)
            {
                for (int i = decimation; i < Q_on_t.Count - decimation; i += decimation)
                {
                    P_curr = P_next;
                    double dQdt = (Q_on_t[i + decimation] - Q_on_t[i - decimation]) / (time[i + decimation] - time[i - decimation]);
                    double dt = (time[i + decimation] - time[i]);
                    P_next = ((Q_on_t[i] * (R2 + R1) + C * R1 * R2 * dQdt) * dt + C * R2 * P_curr) / (dt + C * R2);                  

                    if (N == num_of_periods - 1)
                    {
                        if (P_curr > simPs)
                        {
                            simPs = P_curr;
                            sim_ts = time[i];
                        }

                        if (P_curr < simPd)
                        {
                            simPd = P_curr;
                            sim_td = time[i];
                        }
                    }
                }
            }
        }



        public double record_time;
        public double start_time;
        public double stop_time;


        private List<PressureOutletRCR> BC_set;
        private List<BC_spec> BC_set_spec;
        private Dictionary<PressureOutletRCR, BC_spec> BC_spec_dict;

        private Dictionary<int, PressureCht> terminal_pressure;

        private double P_trg_dst, P_trg_sst, P_mean, pulse_period;
        private double BP_trg_dst, BP_trg_sst, BP_mean;
        private double simPd, simPs, sim_td, sim_ts;
    }

    public delegate int intSystemMask(VascularNode n);

    public struct BC_params
    {
        public int id;
        public double R1, R2, C;

        public BC_params(int id, double R1, double R2, double C)
        {
            this.id = id;
            this.R1 = R1;
            this.R2 = R2;
            this.C = C;
        }
    }
}
