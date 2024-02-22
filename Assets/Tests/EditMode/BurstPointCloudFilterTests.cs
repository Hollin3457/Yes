using NUHS.VeinMapping.VeinProcess;
using NUnit.Framework;
using Unity.Collections;
using UnityEngine;

namespace NUHS.Tests.EditMode
{
    public class BurstPointCloudFilterTests
    {
        [Test]
        public void ReturnsOnePointOutOf2_When_2ndPointIsOutsideCuboid()
        {
            // Arrange
            var sut = new BurstPointCloudFilter(maxPoints: 10);

            var numPoints = 2;
            var pointDataSize = numPoints * 4;
            var pointData = new NativeArray<float>(pointDataSize, Allocator.Persistent);
            pointData[0] = 1.1f; pointData[1] = 1.1f; pointData[2] = 1.1f; pointData[3] = 2f;
            pointData[4] = 0f; pointData[5] = 0f; pointData[6] = 0f; pointData[7] = 3f;

            var cuboid = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
            cuboid.position = Vector3.one;

            try
            {
                // Act
                var result = sut.Filter(pointData, cuboid);

                // Assert
                Assert.AreEqual(2f, result.minValue);
                Assert.AreEqual(2f, result.maxValue);
                Assert.AreEqual(1, result.points.Length);
                var expected = new Vector4(0.1f, 0.1f, 0.1f, 2f);
                Assert.True(expected == result.points[0]);
            }
            finally
            {
                pointData.Dispose();
                sut.Dispose();
            }
        }
    }
}
