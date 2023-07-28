using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BloodFlow
{
    public struct Vector3
    {
        public Vector3(double _x, double _y, double _z)
        { x = _x; y = _y; z = _z; }
        public double x, y, z;

        public static Vector3 operator *(Vector3 v1, double d)
        {
            return new Vector3(v1.x * d, v1.y * d, v1.z * d);
        }

        public static Vector3 operator -(Vector3 v1, Vector3 v2)
        {
            return new Vector3(v1.x - v2.x, v1.y - v2.y, v1.z - v2.z);
        }

        public static Vector3 operator -(Vector3 v1)
        {
            return new Vector3(-v1.x, -v1.y, -v1.z);
        }

        public static Vector3 operator +(Vector3 v1, Vector3 v2)
        {
            return new Vector3(v1.x + v2.x, v1.y + v2.y, v1.z + v2.z);
        }

        static public double Distance(Vector3 v1, Vector3 v2)
        {
            return (double)Math.Sqrt((v1.x - v2.x) * (v1.x - v2.x) + (v1.y - v2.y) * (v1.y - v2.y) + (v1.z - v2.z) * (v1.z - v2.z));
        }

        static public double Dot(Vector3 v1, Vector3 v2)
        {
            return (v1.x * v2.x + v1.y * v2.y + v1.z * v2.z);
        }

        static public double Length(Vector3 v1)
        {
            return Math.Sqrt((v1.x * v1.x + v1.y * v1.y + v1.z * v1.z));
        }

        public double Normilize()
        {
            double l = (double)Math.Sqrt(x * x + y * y + z * z);
            if (l == 0)
            {
                x = 0;
                y = 0;
                z = 0;
                return 0;
            }
            x = x / l;
            y = y / l;
            z = z / l;
            return l;
        }

    };

    public delegate double SimpleFunction(double x);
    public delegate double GetBetaFunction(double R0);
    public delegate double MDFunction(double[] x);
}
