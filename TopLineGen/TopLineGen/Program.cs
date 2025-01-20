using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace TopLineGen
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 5)
            {
                Console.WriteLine("TLG.exe nodesCount zStep R1 R2 filename");
                return;
            }
            int nCount = int.Parse(args[0]);
            float zStep = float.Parse(args[1]);
            float R1 = float.Parse(args[2]);
            float R2 = float.Parse(args[3]);
            string filename = args[4];

            StringBuilder sb = new StringBuilder();
            sb.Append("Name: System_0\nCoordinates:\n");
            for (int i = 0; i < nCount; i++)
            {
                sb.Append(string.Format("{0} X:0.0 Y:0.0 Z:{1} R:{2} C:0.0\n", i, 
                    (zStep * i).ToString("F6"), ((i < nCount / 2) ? R1 : R2).ToString("F4")));
            }
            sb.Append("\nBonds:\n");
            sb.Append("0 1 \n");
            for (int i = 1; i < (nCount-1); i++)
            {
                sb.Append(string.Format("{0} {1} {2} \n", i, i-1, i+1));
            }
            sb.Append(string.Format("{0} {1} \n", nCount - 1, nCount - 2));

            File.WriteAllText(filename, sb.ToString());
        }
    }
}
