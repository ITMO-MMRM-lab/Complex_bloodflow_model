using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Optimization
{
    class Program
    {
        public static void function1_fvec(double[] x, double[] fi, object obj)
        {
            //
            // this callback calculates
            // f0(x0,x1) = 100*(x0+3)^4,
            // f1(x0,x1) = (x1-3)^4
            //
            fi[0] = 10 * System.Math.Pow(x[0] + 3, 2);
            fi[1] = System.Math.Pow(x[1] - 3, 2);
        }

        static void Main(string[] args)
        {
            //
            // This example demonstrates minimization of F(x0,x1) = f0^2+f1^2, where 
            //
            //     f0(x0,x1) = 10*(x0+3)^2
            //     f1(x0,x1) = (x1-3)^2
            //
            // using "V" mode of the Levenberg-Marquardt optimizer.
            //
            // Optimization algorithm uses:
            // * function vector f[] = {f1,f2}
            //
            // No other information (Jacobian, gradient, etc.) is needed.
            //
            double[] x = new double[] { 0, 0 };
            double epsg = 0.0000000001;
            double epsf = 0;
            double epsx = 0;
            int maxits = 0;
            
            alglib.minlmstate state;
            alglib.minlmreport rep;

            alglib.minlmcreatev(2, x, 0.0001, out state);
            alglib.minlmsetcond(state, epsg, epsf, epsx, maxits);
            alglib.minlmoptimize(state, function1_fvec, null, null);
            alglib.minlmresults(state, out x, out rep);

            System.Console.WriteLine("{0}", rep.terminationtype); // EXPECTED: 4
            System.Console.WriteLine("{0}", alglib.ap.format(x, 2)); // EXPECTED: [-3,+3]
            System.Console.ReadLine();
        }
    }
}
