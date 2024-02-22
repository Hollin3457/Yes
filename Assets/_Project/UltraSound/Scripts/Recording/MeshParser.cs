using NUHS.Common;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace NUHS.UltraSound.Recording
{
    public static class MeshParser
    {
        public static Mesh Parse(string meshData)
        {
            if (string.IsNullOrEmpty(meshData))
            {
                return null;
            }

            Mesh mesh = new Mesh();
            ReadOnlySpan<char> separator = new ReadOnlySpan<char>(new[] { '\n' });

            var vLines = 0;
            var fLines = 0;
            foreach (var line in meshData.AsSpan().Split(separator))
            {
                if (line[0] == 'v')
                {
                    vLines++;
                }
                else if (line[0] == 'f')
                {
                    fLines++;
                }
            }

            List<Vector3> vertices = new List<Vector3>(vLines);
            List<int> triangles = new List<int>(fLines * 3);

            foreach (var line in meshData.AsSpan().Split(separator))
            {
                string[] parts = line.Trim().ToString().Split(' ');

                if (parts[0] == "v")
                {
                    float x = float.Parse(parts[1]);
                    float y = float.Parse(parts[2]);
                    float z = float.Parse(parts[3]);
                    vertices.Add(new Vector3(x, y, z));
                }
                else if (parts[0] == "f")
                {
                    for (int i = 1; i < parts.Length; i++)
                    {
                        int vIndex = int.Parse(parts[i]) - 1;
                        triangles.Add(vIndex);
                    }
                }
            }

            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);

            mesh.RecalculateBounds();
            mesh.RecalculateNormals();

            return mesh;
        }

        public static Mesh ParseUnoptimized(string meshData)
        {
            if (string.IsNullOrEmpty(meshData))
            {
                return null;
            }

            Mesh mesh = new Mesh();

            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();

            string[] lines = meshData.Split('\n');

            foreach (string line in lines)
            {
                string[] parts = line.Trim().Split(' ');

                if (parts[0] == "v")
                {
                    float x = float.Parse(parts[1]);
                    float y = float.Parse(parts[2]);
                    float z = float.Parse(parts[3]);
                    vertices.Add(new Vector3(x, y, z));
                }
                else if (parts[0] == "f")
                {
                    for (int i = 1; i < parts.Length; i++)
                    {
                        string[] faceIndices = parts[i].Split('/');

                        int vIndex = int.Parse(faceIndices[0]) - 1;
                        triangles.Add(vIndex);
                    }
                }
            }

            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();

            mesh.RecalculateBounds();
            mesh.RecalculateNormals();

            return mesh;
        }
    }
}
