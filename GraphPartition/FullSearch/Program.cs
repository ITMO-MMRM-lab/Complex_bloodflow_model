using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FullSearch
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


        }
    }
}
