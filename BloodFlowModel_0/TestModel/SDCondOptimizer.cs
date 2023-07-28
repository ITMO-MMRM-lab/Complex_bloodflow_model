using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;

namespace BloodFlow
{
    class SDCondOptimizer
    {
        public delegate double func(double[] x);
        public delegate int grad(double[] x, double[] g, double f);

        public enum State
        {
            INITIALIZED, MINIMIZING, CONVERGED, ERROR
        }

        public enum SolutionType
        {
            NONE, ZERO_G, ABS_F, REL_F, ABS_G, REL_G, MAX_ITERS
        }

        private func f;
        private grad g;

        // Exit conds
        private double absF;
        private double relF;
        private double absG;
        private double relG;
        private long maxIters;

        private double step;
        private int maxOrderPlus;
        private int maxOrderMinus;

        public long itersCount { get; private set; }
        public long fCallsCount { get; private set; }

        private double[] newX;
        private double[] farX;

        public double[] X { get; private set; }
        private double[] G;
        private double Glen;
        public double F { get; private set; }

        private double lastF;
        private double lastGlen;

        public State state { get; private set; }
        public SolutionType solutionType { get; private set; }

        public SDCondOptimizer(func f, grad g, double absF, double relF, double absG, double relG, long maxIters, double step, double[] initX, int maxOrderPlus, int maxOrderMinus)
        {
            this.f = f;
            this.g = g;
            this.absF = absF;
            this.relF = relF;
            this.absG = absG;
            this.relG = relG;
            this.maxIters = maxIters;
            
            this.step = step;
            this.maxOrderPlus = maxOrderPlus;
            this.maxOrderMinus = maxOrderMinus;

            this.newX = new double[initX.Length];
            this.farX = new double[initX.Length];
            this.G = new double[initX.Length];

            this.X = (double[])initX.Clone();

            lastF = double.NaN;
            lastGlen = double.NaN;

            itersCount = 0;
            fCallsCount = 0;
            state = State.INITIALIZED;
            solutionType = SolutionType.NONE;
        }

        public static double dot(double[] v, double[] w)
        {
            double s = 0;
            for (int i = 0; i < v.Length; i++ )
            {
                s += v[i] * w[i];
            }
            return s;
        }

        private static double lengthSquared(double[] v)
        {
            double s = 0;
            foreach (double x in v)
            {
                s += x * x;
            }
            return s;
        }

        public static double length(double[] v)
        {
            return Math.Sqrt(lengthSquared(v));
        }

        public static void normalize(double[] v)
        {
            double len = length(v);
            scale(v, 1f / len);
        }

        private void move(double[] x, double[] grad, double step, double[] newX)
        {
            for (int i = 0; i < x.Length; i++)
            {
                newX[i] = x[i] - step * grad[i];
            }
        }

        // x = x + a * m
        public static void add(double[] x, double[] a, double m)
        {
            for (int i = 0; i < x.Length; i++)
            {
                x[i] = x[i] + m * a[i];
            }
        }

        public static void scale(double[] x, double sc)
        {
            for (int i = 0; i < x.Length; i++)
            {
                x[i] = x[i] * sc;
            }
        }

        private bool isConverged()
        {
            if (state == State.CONVERGED)
                return true;
            if (state == State.ERROR)
                return false;
            if (lengthSquared(G) == 0.0f) 
            {
                state = State.CONVERGED;
                solutionType = SolutionType.ZERO_G;
                return true;
            }
            if (!double.IsNaN(lastF))
            {
                if ((absF > 0f) && (Math.Abs(F - lastF) < absF))
                {
                    state = State.CONVERGED;
                    solutionType = SolutionType.ABS_F;
                    return true;
                }
                if ((relF > 0f) && (Math.Abs(F - lastF) < relF * Math.Abs(lastF)))
                {
                    state = State.CONVERGED;
                    solutionType = SolutionType.REL_F;
                    return true;
                }
            }
            if (!double.IsNaN(lastGlen))
            {
                if ((absG > 0f) && (Glen < absG))
                {
                    state = State.CONVERGED;
                    solutionType = SolutionType.ABS_G;
                    return true;
                }
                if ((relG > 0f) && (Glen < relG * length(X)))
                {
                    state = State.CONVERGED;
                    solutionType = SolutionType.REL_G;
                    return true;
                }
            }
            if ((maxIters > 0) && (itersCount >= maxIters))
            {
                state = State.CONVERGED;
                solutionType = SolutionType.MAX_ITERS;
                return true;
            }
            return false;
        }

        // Returns continue flag.
        public bool takeStep(string outFile)
        {
            F = f(X);
            //Console.WriteLine("============= F = " + F);
            Console.Write("============== oldX: ");
            printVector(X);
            File.AppendAllText(outFile, "X = " + printVectorStr(X));
            Console.WriteLine("val = " + F + " ic = " + itersCount + " step = " + step);
            File.AppendAllText(outFile, "val = " + F + " ic = " + itersCount + " step = " + step + "\n");
            Console.WriteLine("MU = " + System.Environment.WorkingSet / 1024 / 1024);
            File.AppendAllText(outFile, "MU = " + System.Environment.WorkingSet / 1024 / 1024 + "\n");
            fCallsCount++;
            //Console.Write("oldG: ");
            printVector(G);
            fCallsCount += g(X, G, F);

            //Console.Write("NewG: ");
            printVector(G);
            Glen = length(G);
            if (isConverged())
                return false;
            normalize(G);
           // Console.Write("NormG: ");
            printVector(G);
            double curStep = step;
            double newF = 0;
            double farF = double.PositiveInfinity;
            while (true)
            {
                move(X, G, curStep, newX);
                newF = f(newX);
             //   Console.Write("NewX: ");
             //   printVector(newX);
             //   Console.WriteLine("N val = " + newF + " ic = " + itersCount + " step = " + curStep);
                fCallsCount++;
                if (newF < F)
                {
                    for (int i = 2; i <= maxOrderPlus; i++)
                    {
                        move(X, G, curStep * i, farX);
                        farF = f(farX);
                        if (farF < newF)
                        {
                            Console.WriteLine("StepMult = " + i);
                            File.AppendAllText(outFile, "StepMult = " + i + "\n");
                            Console.Write("============== newX: ");
                            printVector(farX);
                            File.AppendAllText(outFile, "newX = " + printVectorStr(farX));
                            newF = farF;
                            farX.CopyTo(newX, 0);
                        }
                        else
                        {
                            break;
                        }
                    }
                    F = newF;
                    break;
                }
                curStep /= 2;
                if (step / curStep > Math.Pow(10, maxOrderMinus))
                {
                    state = State.ERROR;
                    return false;
                }
            }
            lastF = F;
            lastGlen = Glen;
            newX.CopyTo(X, 0);
            itersCount++;
            return true;
        }

        public static void printVector(double[] x)
        {
            foreach (double val in x)
            {
                Console.Write(val + " ");
            }
            Console.WriteLine();
        }

        public static string printVectorStr(double[] x)
        {
            StringBuilder sb = new StringBuilder("");
            foreach (double val in x)
            {
                sb.Append(val + " ");
            }
            sb.AppendLine();
            return sb.ToString();
        }
    }
}
