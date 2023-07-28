using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Threading;

namespace correction_loop
{
    public static class GlobalDefs
    {
        public static int mode = 1; // "0" stands for the difference mode, "1" stands for the dissipation mode
        public static string path = @"..\..\input\heart\lca_2\";
        public static string path_d = path + "temp.dyn";
        public static string path_r = path + "run_lca.txt";
        public static string path_bin = @"..\..\..\bin\";
        public static string path_m = path_bin + "TestModel.exe";
        public static string model_args = @"..\..\input\heart\lca_2\run_lca.txt " + @"..\..\input\heart\lca_2";
        public static string path_diss = path + "dissipation.txt";
        public static string path_h_p = path + "heart_period.txt";
        public static string path_timestep = path + "timestep.txt";
        public static string path_o_p = path + "output_period.txt";
        public static string line;
        public static string processName;
        public static string pattern_run_t = @"^(Topology:)\s(.*)\n?";
        public static string pattern_run_p = @"^(OutletParams:)\s(.*)\n?";
        public static string pattern_in_bond = @"^(InletFlux:)\s(\d+)\s(.*)\n?";
        public static string pattern_top = @"^(\d+)\s(\d+)\s?(\d+)?\s?(\d+)?\s?(\d+)?";
        public static string pattern_par = @"^(\d+)\sR1:(\d+\.\d+)\sR2:(\d+\.\d+)\sC:(\d+\.\d+)";
        public static string pattern_mask = @"^(\d+)\sR1:(TRUE|FALSE)\sR2:(TRUE|FALSE)\sC:(TRUE|FALSE)";
        public static string pattern_args = @"^(\d+)\s(-?\d+\.?\d*)\s";
        public static string pattern_coord = @"^(\d+)\sX:(-?\d+\.\d+)\sY:(-?\d+\.\d+)\sZ:(-?\d+.\d+)\s";
        public static string pattern_counts = @"^WT:\s(\d+\.\d+)";
        public static string path_summary = GlobalDefs.path + @"summary.txt";
        public static string path_args = GlobalDefs.path + @"out_args.txt";
        public static string path_top = null;
        public static string path_par = null;
        public static string path_mask = GlobalDefs.path + @"par_mask.par";
        public static int in_bond_num;
        public static double diff_step = 0.1;
        public static int[] term_bonds;
        public static double it_num = 0;
        public static bool[] term_mask;
        public static double[] Q_t_b_av = { 0.1, 0.1, 0.1 };
        public static double scaling_koeff_t_b = 0;
        public static string path_test_bonds = GlobalDefs.path + @"test_bonds.par";
        public static int[] test_bonds;
    }
    static class Program
    {
        /// <summary>
             /// The main entry point for the application.
        /// </summary>
        [STAThread]
        
        public static void blood_flow_grad(double[] x, ref double func, double[] grad, object obj)
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");
            string line_grad;
            string line_args;
            
            // this callback calculates function
            for (int i = 0; i < x.Length; i++)
                if (x[i] < 0)
                    x[i] = -x[i];
            func = TestModel(x);
            for (int i = 0; i < x.Length; i++)
            {
                if (!GlobalDefs.term_mask[i])
                {
                    grad[i] = 0;
                    continue;
                }
                x[i] = x[i] + GlobalDefs.diff_step;
                grad[i] = (TestModel(x) - func) / GlobalDefs.diff_step;
                x[i] = x[i] - GlobalDefs.diff_step;
            }
            var gr = string.Join(" ", grad);
            line_grad = Convert.ToString(GlobalDefs.it_num) + " " + Convert.ToString(func) + " " + gr + "\n";
            var args = string.Join(" ", x);
            line_args = Convert.ToString(GlobalDefs.it_num) + " " + Convert.ToString(func) + " " + args + "\n";
            File.AppendAllText(GlobalDefs.path_summary, line_grad);
            File.AppendAllText(GlobalDefs.path_args   , line_args);
            GlobalDefs.it_num++;
            return;
            //Console.WriteLine("Objective function: " + func);
        }

        public delegate void getReport();
        
        public static int Main(string[] args)
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");
            double[] Pav = new double[1];
            double[][] P = new double[1][];
            P[0] = new double[3];
            double[][] Q = new double[1][];
            Q[0] = new double[1];
            double[] dP_w_av = new double[1];
            int i, pos;
            string line;
            int[] values = new int[2];
            double R1, R2, C;
            int term_num;           
            double[] term_args;
            string[] lines_args;
            double[] values_args;
            double value;
            double values_args_min;
            int it_num_args;
            Regex r_run_t = new Regex(GlobalDefs.pattern_run_t);
            Regex r_run_p = new Regex(GlobalDefs.pattern_run_p);
            Regex r_run_in_bond = new Regex(GlobalDefs.pattern_in_bond);
            Regex r_top = new Regex(GlobalDefs.pattern_top);
            Regex r_par = new Regex(GlobalDefs.pattern_par);
            Regex r_mask = new Regex(GlobalDefs.pattern_mask, RegexOptions.IgnoreCase);
            Regex r_args = new Regex(GlobalDefs.pattern_args);
                                    
            System.IO.StreamReader file_run = new System.IO.StreamReader(GlobalDefs.path_r);
            while ((line = file_run.ReadLine()) != null)
            {
                Match m_run_t = r_run_t.Match(line);
                if (m_run_t.Success)
                {
                    GlobalDefs.path_top = GlobalDefs.path + m_run_t.Groups[2];
                }
                Match m_run_p = r_run_p.Match(line);
                if (m_run_p.Success)
                {
                    GlobalDefs.path_par = GlobalDefs.path + m_run_p.Groups[2];
                }
                Match m_run_in_bond = r_run_in_bond.Match(line);
                if (m_run_in_bond.Success)
                {
                    GlobalDefs.in_bond_num = Convert.ToInt32(m_run_in_bond.Groups[2].Value);
                }
            }
            file_run.Close();
            List<int> test_bonds_list = new List<int>();
            if (GlobalDefs.path_test_bonds != null)
            {
                System.IO.StreamReader file_test_bonds = new System.IO.StreamReader(GlobalDefs.path_test_bonds);
                while ((line = file_test_bonds.ReadLine()) != null)
                {
                    Regex regex = new Regex(" ");
                    string[] values_test_bonds = regex.Split(line);
                    foreach (string match in values_test_bonds)
                    {
                        test_bonds_list.Add(Convert.ToInt32(match));
                 //       Console.WriteLine("{0} ", match);
                    }
                }
                file_test_bonds.Close();
            }
            GlobalDefs.test_bonds = test_bonds_list.ToArray();
            var term_bonds_list = new List<int>();
            List<double> term_args_list = new List<double>();
            if (GlobalDefs.path_par != null)
            {
                pos = 0;
                System.IO.StreamReader file_par = new System.IO.StreamReader(GlobalDefs.path_par);
                while ((line = file_par.ReadLine()) != null)
                {
                    Match m_par = r_par.Match(line);
                    if (m_par.Value != "")
                    {
                        Group group1 = m_par.Groups[1];
                        Group group2 = m_par.Groups[2];
                        Group group3 = m_par.Groups[3];
                        Group group4 = m_par.Groups[4];
                        term_num = Convert.ToInt32(group1.Value);
                        term_bonds_list.Add(term_num);
                        R1 = Convert.ToDouble(group2.Value);
                        term_args_list.Add(R1);
                        pos++;
                        R2 = Convert.ToDouble(group3.Value);
                        term_args_list.Add(R2);
                        pos++;
                        C = Convert.ToDouble(group4.Value);
                        term_args_list.Add(C);
                        pos++;
                    }
                }
                GlobalDefs.term_bonds = term_bonds_list.ToArray();
                file_par.Close();
              //  foreach (int term_bond in GlobalDefs.term_bonds)
             //   {
               //     Console.WriteLine(term_bond);
             //   }
            }
            List<bool> term_mask_list = new List<bool>();
            if (GlobalDefs.path_mask != null)
            {
                pos = 0;
                System.IO.StreamReader file_mask = new System.IO.StreamReader(GlobalDefs.path_mask);
                while ((line = file_mask.ReadLine()) != null)
                {
                    Match m_par = r_mask.Match(line);
                    for (int ii = 2; ii < 5; ii++)
                    {
                        bool sign = false;
                        if (string.Equals(m_par.Groups[ii].Value, "true", StringComparison.OrdinalIgnoreCase))                        
                            sign = true;                        
                        term_mask_list.Add(sign);
                    }                 
                }
            }
            term_args = term_args_list.ToArray();
            GlobalDefs.term_mask = term_mask_list.ToArray();
            File.WriteAllText(GlobalDefs.path_summary, "");
            File.WriteAllText(GlobalDefs.path_args, "");
                       
            // alglib            
            int par_length = term_args.Length;
            double[] y = (double[])term_args.Clone();
            double[] scale_arr = new double[par_length];
            for (int ii = 0; ii < par_length; ii++)
                scale_arr[ii] = 1.0;
            double epsg   = 0;
            double epsf   = 10;
            double epsx   = 0.01;
            int maxits = 0;
            alglib.mincgstate state;            
            alglib.mincgcreate(y, out state);
            alglib.mincgsetcond(state, epsg, epsf, epsx, maxits);
            alglib.mincgsetscale(state, scale_arr);            
            alglib.mincgoptguardsmoothness(state);
            alglib.mincgreport rep;
            GlobalDefs.it_num = 0;
            try
            {
                alglib.mincgoptimize(state, blood_flow_grad, null, null);
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception is catched");
            }
            alglib.mincgresults (state, out y, out rep);
            System.Console.WriteLine("{0}", alglib.ap.format(y, 2));
            alglib.optguardreport ogrep;
            alglib.mincgoptguardresults(state, out ogrep);
            System.Console.WriteLine("{0}", ogrep.nonc0suspected);
            System.Console.WriteLine("{0}", ogrep.nonc1suspected);
            System.Console.ReadLine();

            // Result check
            if (GlobalDefs.path_args != null)
            {
                List<string> lines_args_list = new List<string>();
                List<double> values_args_list = new List<double>();
                System.IO.StreamReader file_args = new System.IO.StreamReader(GlobalDefs.path_args);
                while ((line = file_args.ReadLine()) != null)
                {
                    Match m_args = r_args.Match(line);
                    Group group1 = m_args.Groups[1];
                    Group group2 = m_args.Groups[2];
                    value = Convert.ToDouble(group2.Value);
                    values_args_list.Add(value);
                    lines_args_list.Add(line);
                }
                file_args.Close();
                lines_args = lines_args_list.ToArray();
                values_args = values_args_list.ToArray();
                values_args_min = values_args[0];
                it_num_args = 0;
                for (i = 0; i < values_args.Length - 2; i++)  // "values_args.Length - 2" is because agent_c column was added in .dyn file.
                {
                    if (values_args[i + 1] < values_args_min)
                    {
                        values_args_min = values_args[i + 1];
                        it_num_args = i + 1;
                    }
                }
                Console.WriteLine("Checked result: " + lines_args[it_num_args]); 
            }
            else
            {
                Console.WriteLine("ERROR! No output file!");
            }
            return 0;
        }      
        
        public static double TestModel(double[] term_args)
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");
            double function_0, function_1;
            int i, counts_Num, nodes_Num;                
            string line;
            double[] WT;
            double heart_period, timestep, delta_t;
            double Pi_sum, Pi_av, Q_in_sum, Q_in_av;                  
            double[][] P = new double[1][];
            P[0] = new double[3];
            double[][] Q = new double[1][];
            Q[0] = new double[1];
            double[] Pav = new double[1];
            double[] function = new double[1];
            int count_pars, node_pars, pos_in_line, counter;
            int node_num, count_an, counts_Num_an, nodes_Num_an;
            double[] values_dyn = new double [4];
            List<string> pars = new List<string>();
            string N_str, R1_str, R2_str, C_str;
            string path_top = null;
            string path_par = null;
            int[] values = new int[2];
            int pos;
            Regex r_run_t = new Regex(GlobalDefs.pattern_run_t);
            Regex r_run_p = new Regex(GlobalDefs.pattern_run_p);
            Regex r_top = new Regex(GlobalDefs.pattern_top);
            Regex r_par = new Regex(GlobalDefs.pattern_par);
            Regex r_coord = new Regex(GlobalDefs.pattern_coord);
            Regex r_counts = new Regex(GlobalDefs.pattern_counts);
            double dQ_t_b;
            double[] Q_t_b_sum = new double[GlobalDefs.test_bonds.Length];
            double[] Q_t_b_sum_calc = new double[GlobalDefs.test_bonds.Length];
            double[] Q_t_b_av_calc = new double[GlobalDefs.test_bonds.Length];

            double function_diss, function_el, function_sum, function_norm, function_norm_sum, L;
            int count_diss, bond_diss_prev;
       
            pos = 0;
            string[] file_strings = File.ReadAllLines(GlobalDefs.path_par);
            foreach(var l in file_strings)
            {
                Match m_par = r_par.Match(l);
                Group group1 = m_par.Groups[1];
                Group group2 = m_par.Groups[2];
                Group group3 = m_par.Groups[3];
                Group group4 = m_par.Groups[4];
                N_str = Convert.ToString(group1, CultureInfo.InvariantCulture);
                pars.Add(N_str);
                R1_str = Convert.ToString(group2, CultureInfo.InvariantCulture);
                pars.Add(R1_str);
                R2_str = Convert.ToString(group3, CultureInfo.InvariantCulture);
                pars.Add(R2_str);
                C_str = Convert.ToString(group4, CultureInfo.InvariantCulture);
                pars.Add(C_str);
            }
            pos = 1;           
            for (counter = 0; counter < term_args.Length; counter = counter + 3)
            {
                pars[pos    ] = Math.Abs(term_args[counter    ]).ToString("F");
                pars[pos + 1] = Math.Abs(term_args[counter + 1]).ToString("F");
                pars[pos + 2] = Math.Abs(term_args[counter + 2]).ToString("F");
                pos += 4;
            }
          //      for (i = 0; i < (term_Num * 4); i = i + 4)
          //      {
          //          Console.WriteLine(pars[i]);
         //           Console.WriteLine(pars[i + 1]);
         //           Console.WriteLine(pars[i + 2]);
          //          Console.WriteLine(pars[i + 3]);
          //      }
                      
                      // File deletion
            if (System.IO.File.Exists(GlobalDefs.path_par))
            {
                try
                {
                    System.IO.File.Delete(GlobalDefs.path_par);
                }
                catch (System.IO.IOException e)
                {
                    Console.WriteLine(e.Message);
                    return -1.0;
                }
            }
                
                //File creating
            pos = 0;
            List <string> lines = new List <string> ();
            while (pos < pars.Count)
            {
                lines.Add(pars[pos] + " R1:" + pars[pos + 1] + " R2:" + pars[pos + 2] + " C:" + pars[pos + 3]);
                pos = pos + 4;
          //          Console.WriteLine(lines[i]);
            }
            System.IO.File.WriteAllLines(GlobalDefs.path_par, lines);            
            
            Process TestModel = new Process();
            TestModel.StartInfo.FileName = GlobalDefs.path_m;
            TestModel.StartInfo.Arguments = GlobalDefs.model_args;
            TestModel.Start();
            TestModel.WaitForExit();

            // Reading heart_period, timestep, delta_t;
            System.IO.StreamReader file_h_p = new System.IO.StreamReader(GlobalDefs.path_h_p);
            if ((line = file_h_p.ReadLine()) != null)
            {
                heart_period = Convert.ToDouble(line);
            }
            else
            {
                Console.WriteLine("ERROR! There is no heart_period value in the file.");
                return -2;
            }
            file_h_p.Close();
            System.IO.StreamReader file_timestep = new System.IO.StreamReader(GlobalDefs.path_timestep);
            if ((line = file_timestep.ReadLine()) != null)
            {
                timestep = Convert.ToDouble(line);
            }
            else
            {
                Console.WriteLine("ERROR! There is no timestep value in the file.");
                return -3;
            }
            file_timestep.Close();
            System.IO.StreamReader file_o_p = new System.IO.StreamReader(GlobalDefs.path_o_p);
            if ((line = file_o_p.ReadLine()) != null)
            {
                delta_t = Convert.ToDouble(line);
            }
            else
            {
                Console.WriteLine("ERROR! There is no output_period value in the file.");
                return -4;
            }
            file_o_p.Close();

                        //Resize of arrays
            nodes_Num = 0;
            counts_Num = 0;
            int count = -1;
            List <double> WT_list = new List <double> (); 
            System.IO.StreamReader file_WT = new System.IO.StreamReader(GlobalDefs.path_d);
            while ((line = file_WT.ReadLine()) != null)
            {
                Match m_counts = r_counts.Match(line);
                if (m_counts.Value != "")
                {
                    Group group1 = m_counts.Groups[1];
                    WT_list.Add(Convert.ToDouble(group1.Value));
                    count++;
                }
                if ((count == 0) && (m_counts.Value == ""))
                {
                    nodes_Num++;
                }
            }
            file_WT.Close();
            WT = WT_list.ToArray();
            foreach (var wt in WT)
            {
                if (wt > (WT[WT.Length - 1] - heart_period + timestep))
                {
                    counts_Num++;
                }
            }
            
            //     ArrayResize(counts_Num, nodes_Num, ref P); -->
            var arr1 = new double[counts_Num][];
            for (int k = 0; k < counts_Num; k++)
            {
                arr1[k] = new double[nodes_Num];
            }
            for (int k = 0; k < P.Length; k++)
            {
                for (int l = 0; l < P[k].Length; l++)
                {
                    arr1[k][l] = P[k][l];
                }
            }
            P = arr1;
            //    ArrayResize(counts_Num, nodes_Num, ref Q);  -->
            var arr2 = new double[counts_Num][];
            for (int k = 0; k < counts_Num; k++)
            {
                arr2[k] = new double[nodes_Num];
            }
            for (int k = 0; k < Q.Length; k++)
            {
                for (int l = 0; l < Q[k].Length; l++)
                {
                    arr2[k][l] = Q[k][l];
                }
            }
            Q = arr2;
            Array.Resize(ref Pav, nodes_Num);
            Array.Resize(ref function, counts_Num);
            // Checking arrays resize 
            //    Console.WriteLine("counts_Num {0}, nodes_Num {1}", counts_Num, nodes_Num);   
            //    Console.WriteLine("P: counts - {0}, nodes - first {1}, last {2}", P.Length, P[0].Length, P[counts_Num - 1].Length);
            //    Console.WriteLine("Q: counts - {0}, nodes - first {1}, last {2}", Q.Length, Q[0].Length, Q[counts_Num - 1].Length);
            //    Console.WriteLine("Length Pav: {0}", Pav.Length);
            //    Console.WriteLine("Length dP_w_av: {0}", dP_w_av.Length);

            counts_Num_an = Q.Length;
            nodes_Num_an = Q[0].Length;
            
            //    FileParsing(path_d, Q, P); -->
            System.IO.StreamReader file_dyn = new System.IO.StreamReader(GlobalDefs.path_d);
            count_pars = -1;
            node_pars = 0;
            int trig = 0;
            while ((line = file_dyn.ReadLine()) != null)
            {
                Match m_counts = r_counts.Match(line);
                if (m_counts.Value != "")
                {
                    Group group1 = m_counts.Groups[1];
                    if (Convert.ToDouble(group1.Value) > (WT[WT.Length - 1] - heart_period + timestep))
                    {
                        trig++;
                    }
                }
                if (trig > 0)
                {
                    if (line[0] != 'W')
                    {
                        pos_in_line = 0;
                        string[] strings = line.Split('\t');
                        foreach (string match in strings)
                        {
                            if (pos_in_line != 4) // This condition is added because agent_c column was added in .dyn file.
                            {
                                values_dyn[pos_in_line] = double.Parse(match, CultureInfo.InvariantCulture);
                                pos_in_line++;
                            }
                        }
                        Q[count_pars][node_pars] = values_dyn[1];
                        P[count_pars][node_pars] = values_dyn[2];
                        node_pars++;
                    }
                    else
                    {
                        count_pars++;
                        node_pars = 0;
                    }
                }
            }
            file_dyn.Close();
                            
                // Pav_array                  
            for (node_num = 0; node_num < nodes_Num_an; node_num++)
            {
                Pi_sum = 0;
                for (count_an = 0; count_an < counts_Num_an; count_an++)
                {
                    Pi_sum = Pi_sum + P[count_an][node_num];
                }
                Pi_av = Pi_sum / counts_Num_an;
                Pav[node_num] = Pi_av;
            }
            
            // Function
            switch (GlobalDefs.mode)
            {
                case 0:
                    function_sum = 0;
                    dQ_t_b = 0;
                    Q_in_sum = 0;
                    for (i = 0; i < Q_t_b_av_calc.Length; i++)
                    {
                        for (count_an = 0; count_an < counts_Num_an; count_an++)
                        {
                            Q_t_b_sum_calc[i] = Q_t_b_sum_calc[i] + Math.Abs(Q[count_an][GlobalDefs.test_bonds[i]]);
                        }
                        Q_t_b_av_calc[i] = Q_t_b_sum_calc[i] / counts_Num_an;
                    }
                    i = 0;
                    foreach (int test_bond_num in GlobalDefs.test_bonds)
                    {
                        dQ_t_b = dQ_t_b + Math.Abs(GlobalDefs.Q_t_b_av[i] - Q_t_b_av_calc[i]);
                        i++;
                    }
                    for (count_an = 0; count_an < counts_Num_an; count_an++)
                    {
                        foreach (int term_bond in GlobalDefs.term_bonds)
                        {
                            function[count_an] = function[count_an] + ((Pav[GlobalDefs.in_bond_num] - Pav[term_bond]) * Math.Abs(Q[count_an][term_bond])) * delta_t;
                        }
                        function_sum = function_sum + function[count_an];
                        Q_in_sum = Q_in_sum + Q[count_an][GlobalDefs.in_bond_num];
                    }
                    Q_in_av = Q_in_sum / counts_Num;
                    function_0 = GlobalDefs.scaling_koeff_t_b * dQ_t_b + function_sum / Q_in_av;
                    return function_0;    
                case 1:    
                    System.IO.StreamReader file_diss = new System.IO.StreamReader(GlobalDefs.path_diss);
                    if ((line = file_diss.ReadLine()) != null)
                    {
                        function_diss = Convert.ToDouble(line);
                    }
                    else
                    {
                        Console.WriteLine("ERROR! There is no dissipation value in the file!");
                        return -1;
                    }
                    file_diss.Close();
            
                    dQ_t_b = 0;
                    for (i = 0; i < Q_t_b_av_calc.Length; i++)
                    {
                        for (count_an = 0; count_an < counts_Num_an; count_an++)
                        {
                            Q_t_b_sum_calc[i] = Q_t_b_sum_calc[i] + Math.Abs(Q[count_an][GlobalDefs.test_bonds[i]]);
                        }
                        Q_t_b_av_calc[i] = Q_t_b_sum_calc[i] / counts_Num_an;
                    }
                    i = 0;
                    foreach (int test_bond_num in GlobalDefs.test_bonds)
                    {
                        dQ_t_b = dQ_t_b + Math.Abs(GlobalDefs.Q_t_b_av[i] - Q_t_b_av_calc[i]);
                        i++;
                    }
                    function_1 = GlobalDefs.scaling_koeff_t_b * dQ_t_b + function_diss;
                    return function_1;
                default: 
                    Console.WriteLine("ERROR! The calculation mode is not selected!");
                    return 0;
            }
        }
    }
}





