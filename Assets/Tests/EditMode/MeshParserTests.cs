using NUHS.UltraSound.Recording;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace NUHS.Tests.EditMode
{
    public class MeshParserTests
    {
        private const string MeshDataAssetPath = "Assets/Tests/TestData/MeshData.txt";

        [Test]
        public void OptimizedVersion_ReturnsExpectedVertexAndTriangleCount()
        {
            var meshData = AssetDatabase.LoadAssetAtPath<TextAsset>(MeshDataAssetPath);
            var mesh = MeshParser.Parse(meshData.text);
            Assert.IsNotNull(mesh);
            Assert.AreEqual(3268, mesh.vertexCount);
            Assert.AreEqual(11856, mesh.triangles.Length);
        }

        [Test]
        public void UnoptimizedVersion_ReturnsExpectedVertexAndTriangleCount()
        {
            var meshData = AssetDatabase.LoadAssetAtPath<TextAsset>(MeshDataAssetPath);
            var mesh = MeshParser.ParseUnoptimized(meshData.text);
            Assert.IsNotNull(mesh);
            Assert.AreEqual(3268, mesh.vertexCount);
            Assert.AreEqual(11856, mesh.triangles.Length);
        }

        [Test]
        public void BothVersions_ReturnSameVerticesAndTriangles()
        {
            var meshData = AssetDatabase.LoadAssetAtPath<TextAsset>(MeshDataAssetPath);
            var mesh1 = MeshParser.Parse(meshData.text);
            var mesh2 = MeshParser.ParseUnoptimized(meshData.text);
            CollectionAssert.AreEquivalent(mesh1.vertices, mesh2.vertices);
            CollectionAssert.AreEquivalent(mesh1.triangles, mesh2.triangles);
        }
    }
}
