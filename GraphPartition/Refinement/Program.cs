using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace Refinement
{
    class Program
    {
        static void Main(string[] args)
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");

            Graph v_net = new Graph("System_0");
            string err_rep = "";
            if (args.GetLength(0) != 0)
                Graph.LoadFromFile(v_net, args[0]);
            else
            {
                err_rep += "No file is specified\n";
                Console.WriteLine(err_rep);
                Environment.Exit(1);
            }

            List<string> args_list = args.ToList();

            double balance_const = 0.3;
               int search_depth = 32;

            try
            {
                int d_indx = args_list.FindIndex(x => x == "-d");
                if (args_list.Count > d_indx + 1)
                    search_depth = int.Parse(args_list[d_indx + 1]);
                else
                    err_rep += "Search depth isn't specified\n";
            }
            catch { };

            try
            {
                int e_indx = args_list.FindIndex(x => x == "-e");
                if (args_list.Count > e_indx + 1)
                    balance_const = double.Parse(args_list[e_indx + 1]);
                else
                    err_rep += "Disbalance error isn't specified\n";
            }
            catch { };            

            
            v_net.BALANCE_CONSTR = balance_const;
            v_net.SEARCH_DEPTH   = search_depth;
            err_rep += "Disbalance error constraint = " + balance_const.ToString("F3") + "\n";
            err_rep += "Search depth constraint = " + search_depth.ToString() + "\n";
            Console.WriteLine(err_rep);

            v_net.HillScan();
            
            v_net.writeToFile(@"partition_ref.top");

            Environment.Exit(0);
        }
    }
}
