using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MeshSubdivision
{
    public partial class MeshSubdivisionEditor : Editor
    {
        private static Mesh CreateQuadMesh(float scale)
        {
            var vertices = new List<Vector3>()
            {
                new Vector3(-1.0f, 0.0f, -1.0f) * 0.5f * scale,
                new Vector3( 1.0f, 0.0f, -1.0f) * 0.5f * scale,
                new Vector3(-1.0f, 0.0f,  1.0f) * 0.5f * scale,
                new Vector3( 1.0f, 0.0f,  1.0f) * 0.5f * scale,
            };

            var normals = new List<Vector3>()
            {
                Vector3.up,
                Vector3.up,
                Vector3.up,
                Vector3.up,
            };

            var indices = new int[6]
            {
                0, 2, 1,
                3, 1, 2,
            };

            var mesh = new Mesh();
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetIndices(indices, MeshTopology.Triangles, 0);

            return mesh;
        }
        private static Mesh CreateCubeMesh(float scale)
        {
            var vertices = new List<Vector3>()
            {
                new Vector3(-1.0f, -1.0f, -1.0f) * 0.5f * scale,
                new Vector3( 1.0f, -1.0f, -1.0f) * 0.5f * scale,
                new Vector3(-1.0f,  1.0f, -1.0f) * 0.5f * scale,
                new Vector3( 1.0f,  1.0f, -1.0f) * 0.5f * scale,

                new Vector3(-1.0f, -1.0f,  1.0f) * 0.5f * scale,
                new Vector3( 1.0f, -1.0f,  1.0f) * 0.5f * scale,
                new Vector3(-1.0f,  1.0f,  1.0f) * 0.5f * scale,
                new Vector3( 1.0f,  1.0f,  1.0f) * 0.5f * scale,
            };

            var normals = new List<Vector3>()
            {
                vertices[0].normalized,
                vertices[1].normalized,
                vertices[2].normalized,
                vertices[3].normalized,

                vertices[4].normalized,
                vertices[5].normalized,
                vertices[6].normalized,
                vertices[7].normalized,
            };

            var indices = new int[36]
            {
                0, 2, 1, 3, 1, 2,
                1, 3, 5, 7, 5, 3,
                5, 7, 4, 6, 4, 7,
                4, 6, 0, 2, 0, 6,
                2, 6, 3, 7, 3, 6,
                4, 0, 5, 1, 5, 0,
            };

            var mesh = new Mesh();
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetIndices(indices, MeshTopology.Triangles, 0);

            return mesh;
        }
        private static Mesh CreateSphereMesh(float radius, int numDivisions)
        {
            var origins = new List<Vector3>()
            {
                new Vector3(-1.0f, -1.0f, -1.0f),
                new Vector3( 1.0f, -1.0f, -1.0f),
                new Vector3( 1.0f, -1.0f,  1.0f),
                new Vector3(-1.0f, -1.0f,  1.0f),
                new Vector3(-1.0f,  1.0f, -1.0f),
                new Vector3(-1.0f, -1.0f,  1.0f),
            };
            var rights = new List<Vector3>()
            {
                new Vector3( 2.0f, 0.0f,  0.0f),
                new Vector3( 0.0f, 0.0f,  2.0f),
                new Vector3(-2.0f, 0.0f,  0.0f),
                new Vector3( 0.0f, 0.0f, -2.0f),
                new Vector3( 2.0f, 0.0f,  0.0f),
                new Vector3( 2.0f, 0.0f,  0.0f)
            };
            var ups = new List<Vector3>()
            {
                new Vector3(0.0f, 2.0f,  0.0f),
                new Vector3(0.0f, 2.0f,  0.0f),
                new Vector3(0.0f, 2.0f,  0.0f),
                new Vector3(0.0f, 2.0f,  0.0f),
                new Vector3(0.0f, 0.0f,  2.0f),
                new Vector3(0.0f, 0.0f, -2.0f),
            };

            float step = 1.0f / numDivisions;

            var s3 = new Vector3(step, step, step);

            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var indices = new List<int>();

            for (int f = 0; f < 6; f++)
            {
                var origin = origins[f];
                var right = rights[f];
                var up = ups[f];

                for (int j = 0; j < numDivisions + 1; j++)
                {
                    for (int i = 0; i < numDivisions + 1; ++i)
                    {
                        var p = new Vector3(
                            origin.x + step * (i * right.x + j * up.x),
                            origin.y + step * (i * right.y + j * up.y),
                            origin.z + step * (i * right.z + j * up.z));

                        vertices.Add(p.normalized * radius);
                        normals.Add(p.normalized);
                    }
                }
            }

            int k = numDivisions + 1;

            for (int f = 0; f < 6; f++)
            {
                for (int j = 0; j < numDivisions; j++)
                {
                    bool bottom = j < (numDivisions / 2);
                    for (int i = 0; i < numDivisions; ++i)
                    {
                        bool left = i < (numDivisions / 2);
                        int a = ((f * k + j) * k + i);
                        int b = ((f * k + j) * k + i + 1);
                        int c = ((f * k + j + 1) * k + i);
                        int d = ((f * k + j + 1) * k + i + 1);

                        indices.AddRange(new int[] { a, c, b, d, b, c, });
                    }
                }
            }

            var mesh = new Mesh();
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetIndices(indices.ToArray(), MeshTopology.Triangles, 0);

            return mesh;
        }
    }
}
