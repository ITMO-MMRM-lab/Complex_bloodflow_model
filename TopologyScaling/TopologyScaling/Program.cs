using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;
using System.Globalization;

namespace TopologyScaling
{
    class Program
    {
        public static string filename;
        static void Main(string[] args)
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.CreateSpecificCulture("en-US");
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
          
            filename = args[0];

            double dInput = 0.0;
            double hInput = 0.0;
            double hOrig = 0.0;
            double rOrig = 0.0;
            int dNode = 0;
            string top_filename = "in.top";
            string out_filename = "out.top";
            string path = "";
            string textOutput = "";
            string textBonds = "";

            string textRun = File.ReadAllText(filename);
            string[] linesRun = textRun.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            Regex regex = new Regex(@"^<(\w+)>$", RegexOptions.IgnoreCase);
            int line_counter;
            for (line_counter = 0; line_counter < linesRun.GetLength(0); line_counter++)
            {
                Match name_match = regex.Match(linesRun[line_counter]);
                if (name_match.Success)
                {
                    out_filename = name_match.Groups[1].Value + "New.top";
                    break;
                }
            }

            regex = new Regex(@"^Topology:\s*(\w+.*)$", RegexOptions.IgnoreCase);
            for (line_counter = 0; line_counter < linesRun.GetLength(0); line_counter++)
            {
                Match top_match = regex.Match(linesRun[line_counter]);
                if (top_match.Success)
                {
                    top_filename = top_match.Groups[1].Value;
                    break;
                }
            }

            regex = new Regex(@"^Diameter_mm:\s+(\d+)+\s+(\d+.\d?)$", RegexOptions.IgnoreCase);
            for (line_counter = 0; line_counter < linesRun.GetLength(0); line_counter++)
            {
                Match d_match = regex.Match(linesRun[line_counter]);
                if (d_match.Success)
                {
                    dNode = int.Parse(d_match.Groups[1].Value);
                    dInput = double.Parse(d_match.Groups[2].Value);
                    break;
                }
            }

            regex = new Regex(@"^Height_m:\s+(.+)$", RegexOptions.IgnoreCase);
            for (line_counter = 0; line_counter < linesRun.GetLength(0); line_counter++)
            {
                Match h_match = regex.Match(linesRun[line_counter]);
                if (h_match.Success)
                {
                    hInput = double.Parse(h_match.Groups[1].Value);
                    break;
                }
            }

            regex = new Regex(@"^HeightOrig_m:\s+(.+)$", RegexOptions.IgnoreCase);
            for (line_counter = 0; line_counter < linesRun.GetLength(0); line_counter++)
            {
                Match h_match = regex.Match(linesRun[line_counter]);
                if (h_match.Success)
                {
                    hOrig = double.Parse(h_match.Groups[1].Value);
                    break;
                }
            }

            string textTop = File.ReadAllText(top_filename);
            string[] linesTop = textTop.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            regex = new Regex(@"^\s*(\d+)\s+X:(-*\d+.\d+)\s+Y:(-*\d+.\d+)\s+Z:(-*\d+.\d+)\s+R:(-*\d+.\d+)\s+C:(\d+.\d+)$", RegexOptions.IgnoreCase);
            for (line_counter = 0; line_counter < linesTop.GetLength(0); line_counter++)
            {
                Match coords_match = regex.Match(linesTop[line_counter]);
                if (coords_match.Success)
                {
                    if (int.Parse(coords_match.Groups[1].Value) == dNode)
                    {
                        rOrig = double.Parse(coords_match.Groups[5].Value);
                        break;
                    }
                }
            }

            regex = new Regex(@"^\s*(\d+)\s+X:(-*\d+.\d+)\s+Y:(-*\d+.\d+)\s+Z:(-*\d+.\d+)\s+R:(-*\d+.\d+)\s+C:(\d+.\d+)$", RegexOptions.IgnoreCase);
            int coords_counter = 0;
            for (line_counter = 0; line_counter < linesTop.GetLength(0); line_counter++)
            {
                Match coords_match = regex.Match(linesTop[line_counter]);
                if (coords_match.Success)
                {
                    coords_counter++;
                }
            }
            coords[] coords_in = new coords[coords_counter];
            coords[] coords_out = new coords[coords_counter];
            Regex regexCoords = new Regex(@"^\s*(\d+)\s+X:(-*\d+.\d+)\s+Y:(-*\d+.\d+)\s+Z:(-*\d+.\d+)\s+R:(-*\d+.\d+)\s+C:(\d+.\d+)$", RegexOptions.IgnoreCase);
            Regex regexBonds = new Regex(@"^Bonds:", RegexOptions.IgnoreCase);
            coords_counter = 0;
            bool bonds_ok = false;
            for (line_counter = 0; line_counter < linesTop.GetLength(0); line_counter++)
            {
                Match coords_match = regexCoords.Match(linesTop[line_counter]);
                if (coords_match.Success)
                {
                    coords element_in = new coords();
                    element_in.id = int.Parse(coords_match.Groups[1].Value);
                    element_in.X = double.Parse(coords_match.Groups[2].Value);
                    element_in.Y = double.Parse(coords_match.Groups[3].Value);
                    element_in.Z = double.Parse(coords_match.Groups[4].Value);
                    element_in.R = double.Parse(coords_match.Groups[5].Value);
                    element_in.C = double.Parse(coords_match.Groups[6].Value);
                    coords_in[coords_counter] = element_in;
                    coords element_out = new coords();
                    element_out.id = element_in.id;
                    element_out.X = element_in.X * (hInput / hOrig);
                    element_out.Y = element_in.Y * (hInput / hOrig);
                    element_out.Z = element_in.Z * (hInput / hOrig);
                    element_out.R = element_in.R * ((dInput / 2.0) / rOrig);
                    element_out.C = element_in.C;
                    coords_out[coords_counter] = element_out;
                    coords_counter++;
                }
                Match bonds_match = regexBonds.Match(linesTop[line_counter]);
                if (bonds_match.Success)
                {
                    bonds_ok = true;
                }
                if (bonds_ok)
                {
                    textBonds = textBonds + linesTop[line_counter] +"\n";
                }
            }

            textOutput = "Name: Scaled\nCoordinates:\n";
            for (line_counter = 0; line_counter < coords_counter; line_counter++)
            {
                textOutput = textOutput + coords_out[line_counter].id + " X:" + coords_out[line_counter].X + " Y:" + coords_out[line_counter].Y + " Z:" + coords_out[line_counter].Z + " R:" + coords_out[line_counter].R + " C:" + coords_out[line_counter].C + ".0" + "\n";
            }
            textOutput = textOutput + "\n" + textBonds;

            path = out_filename;
            StreamWriter sw = new StreamWriter(path);
            sw.Write(textOutput);
            sw.Close();
        }

        struct coords
        {
            public int id;
            public double X;
            public double Y;
            public double Z;
            public double R;
            public double C;
        }
    }
}

