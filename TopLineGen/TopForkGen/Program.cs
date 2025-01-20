using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace TopForkGen
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 8)
            {
                Console.WriteLine("TFG.exe nodesCount zStep Ri Ro1 Ro2 alpha1 aplha2 filename");
                return;
            }
            // TODO
            int nCount = int.Parse(args[0]);
            float zStep = float.Parse(args[1]);
            float Ri = float.Parse(args[2]);
            float Ro1 = float.Parse(args[3]);
            float Ro2 = float.Parse(args[4]);
            float a1 = (float)Math.PI * float.Parse(args[5]);
            float a2 = (float)Math.PI * float.Parse(args[6]);
            string filename = args[7];

            StringBuilder sb = new StringBuilder();
            sb.Append("Name: System_0\nCoordinates:\n");
            // Incoming
            for (int i = 0; i < nCount / 3; i++)
            {
                sb.Append(string.Format("{0} X:0.0 Y:0.0 Z:{1} R:{2} C:0.0\n", i,
                    (zStep * i).ToString("F6"), (Ri).ToString("F4")));
            }
            float lastI = nCount / 3 - 1;
            float lastZ = zStep * lastI;
            // Outgoing 1
            for (int i = nCount / 3; i < 2 * nCount / 3; i++)
            {
                sb.Append(string.Format("{0} X:{1} Y:0.0 Z:{2} R:{3} C:0.0\n", i,
                    (zStep * (i - lastI) * Math.Sin(a1)).ToString("F6"),
                    (lastZ + zStep * (i - lastI) * Math.Cos(a1)).ToString("F6"), (Ro1).ToString("F4")));
            }
            lastI = 2 * nCount / 3 - 1;
            // Outgoing 2
            for (int i = 2 * nCount / 3; i < nCount; i++)
            {
                sb.Append(string.Format("{0} X:{1} Y:0.0 Z:{2} R:{3} C:0.0\n", i,
                    (-zStep * (i - lastI) * Math.Sin(a2)).ToString("F6"),
                    (lastZ + zStep * (i - lastI) * Math.Cos(a2)).ToString("F6"), (Ro2).ToString("F4")));
            }


            sb.Append("\nBonds:\n");
            // Incoming
            sb.Append("0 1 \n");
            for (int i = 1; i < (nCount / 3 - 1); i++)
            {
                sb.Append(string.Format("{0} {1} {2} \n", i, i - 1, i + 1));
            }
            sb.Append(string.Format("{0} {1} {2} {3} \n", nCount / 3 - 1, nCount / 3 - 2, nCount / 3, 2 * nCount / 3));

            // Outgoing 1
            sb.Append(string.Format("{0} {1} {2} \n", nCount / 3, nCount / 3 + 1, nCount / 3 - 1));
            for (int i = nCount / 3 + 1; i < (2 * nCount / 3 - 1); i++)
            {
                sb.Append(string.Format("{0} {1} {2} \n", i, i - 1, i + 1));
            }
            sb.Append(string.Format("{0} {1} \n", 2 * nCount / 3 - 1, 2 * nCount / 3 - 2));

            // Outgoing 2
            sb.Append(string.Format("{0} {1} {2} \n", 2 * nCount / 3, 2 * nCount / 3 + 1, nCount / 3 - 1));
            for (int i = 2 * nCount / 3 + 1; i < (nCount - 1); i++)
            {
                sb.Append(string.Format("{0} {1} {2} \n", i, i - 1, i + 1));
            }
            sb.Append(string.Format("{0} {1} \n", nCount - 1, nCount - 2));

            File.WriteAllText(filename, sb.ToString());
        }
    }
}
