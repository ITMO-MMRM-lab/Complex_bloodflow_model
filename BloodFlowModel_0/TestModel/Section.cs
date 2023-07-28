using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BloodFlow
{
    public class Section
    {
        public Section(Vector3 c, double r, Vector3 n)
        {
            center = c;
            radius = r;
            normal = n;
        }

        public Vector3 center;
        public double radius;
        public Vector3 normal;
        public Vector3 x_axis;
        public Vector3 y_axis;
        double x_intersec_1, y_intersec_1, x_intersec_2, y_intersec_2;
    }
}
