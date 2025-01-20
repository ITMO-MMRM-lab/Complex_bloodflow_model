using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Mathematics;
using System.Text.RegularExpressions;

namespace NewVascularTopVisualizer
{
    class Parser3ds
    {
        private static String VERTICES_LIST_HEADER = "*MESH_VERTEX_LIST {";
        private static Regex VERTEX_LINE_PATTERN = new Regex(@"^\t*\*MESH_VERTEX\s+(\d+)\t+(-*\d+.\d+)\t+(-*\d+.\d+)\t+(-*\d+.\d+)$");

        public static List<Vector3> Parse3dsData(String[] data, float measure,
            bool forceNormal, int idNormalIncluded, int idNormalExcluded, out Vector3 normal)
        {
            List<Vector3> vertices = new List<Vector3>();
            int lineIndex = 0;
            bool verticesBlockFound = false;
            Match match;
            GroupCollection groups;
            normal = Vector3.Zero;
            Vector3 vinc = Vector3.Zero;
            Vector3 vexc = Vector3.Zero;
            while (true)
            {
                if (lineIndex >= data.Length)
                    break;
                if (data[lineIndex].Contains(VERTICES_LIST_HEADER))
                {
                    verticesBlockFound = true;
                    lineIndex++;
                    continue;
                }
                if (verticesBlockFound)
                {
                    match = VERTEX_LINE_PATTERN.Match(data[lineIndex]);
                    if (match.Success)
                    {
                        groups = match.Groups;
                        if (forceNormal)
                        {
                            if (int.Parse(groups[1].Value) == idNormalExcluded)
                            {
                                vexc = new Vector3(
                                float.Parse(groups[2].Value) * measure,
                                float.Parse(groups[3].Value) * measure,
                                float.Parse(groups[4].Value) * measure);
                                lineIndex++;
                                continue;
                            }
                            if (int.Parse(groups[1].Value) == idNormalIncluded)
                            {
                                vinc = new Vector3(
                                   float.Parse(groups[2].Value) * measure,
                                   float.Parse(groups[3].Value) * measure,
                                   float.Parse(groups[4].Value) * measure);
                            }
                        }
                        vertices.Add(new Vector3(
                            float.Parse(groups[2].Value) * measure,
                            float.Parse(groups[3].Value) * measure,
                            float.Parse(groups[4].Value) * measure));
                    }
                }
                lineIndex++;
            }
            if (forceNormal)
            {
                normal = vexc - vinc;
            }
            return vertices;
        }
    }

    struct Section
    {
        public Section(List<Vector3> _vertices, bool forceNormal, Vector3 _normal, bool getRadiusByMeanDiameter)
        {
            vertices = _vertices;
            // If 

            // Get center as a mean of vertices.
            center = Vector3.Zero;
            foreach (var v in vertices)
            {
                center += v;
            }
            center /= vertices.Count;

            Vector3 normal = Vector3.Zero;
            Vector3 v1 = Vector3.Zero;
            Vector3 v2 = Vector3.Zero;
            if (forceNormal)
            {
                normal = _normal;
            }
            else
            {
                // Get mean normal based on all possible almost-regular triangles.
                int step = vertices.Count / 3;
                for (int i = 0; i < step; i++)
                {
                    v1 = vertices[i + step] - vertices[i];
                    v2 = vertices[i + step * 2] - vertices[i];
                    normal += (v1 * v2).Normalized();
                }
            }
            normal.Normalize();
            
            // Get projections of points to the plane.
            Vector3[] projections = new Vector3[vertices.Count];
            for (int i = 0; i < vertices.Count; i++)
            {
                projections[i] = vertices[i] - Vector3.Dot((vertices[i] - center), normal) * normal;
            }
            if (getRadiusByMeanDiameter)
            {
                // Get diameter as mean of max. distances for each vertex.
                double diameterSquared = 0.0f;
                double meanDiameter = 0.0f;
                for (int i = 0; i < projections.Length; i++)
                {
                    diameterSquared = 0.0f;
                    for (int j = 0; j < projections.Length; j++)
                    {
                        if (i == j)
                            continue;
                        if ((projections[i] - projections[j]).LengthSquared() > diameterSquared)
                            diameterSquared = (projections[i] - projections[j]).LengthSquared();
                    }
                    meanDiameter += Math.Sqrt(diameterSquared);
                }
                meanDiameter /= projections.Length;

                radius = meanDiameter / 2.0;
                square = Math.PI * radius * radius;
            }
            else
            {
                // Get square as a sum of triangles' squares.
                square = 0.0;
                for (int i = 0; i < (vertices.Count - 1); i++)
                {
                    v1 = projections[i] - center;
                    v2 = projections[i + 1] - center;
                    square += (v1 * v2).Length() / 2.0;
                }
                v1 = projections[vertices.Count - 1] - center;
                v2 = projections[0] - center;
                square += (v1 * v2).Length() / 2.0;
                // Get radius based on the square value.
                if (square == 0.0)
                    radius = 0.0;
                else
                    radius = Math.Sqrt(square / Math.PI);
            }


            
            
        }

        public List<Vector3> vertices;
        public Vector3 center;
        public double radius;
        public double square;
    }
}
