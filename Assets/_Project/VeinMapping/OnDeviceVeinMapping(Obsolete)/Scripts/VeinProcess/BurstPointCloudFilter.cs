using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace NUHS.VeinMapping.VeinProcess
{
    /// <summary>
    /// Process the raw point cloud
    /// </summary>
    public sealed class BurstPointCloudFilter
    {
        private NativeArray<Vector4> _filteredPoints;
        private NativeArray<bool> _containmentResults;

        public struct PointCloudFilterResult
        {
            /// <summary>
            /// Points in the cuboid local space
            /// </summary>
            public NativeArray<Vector4> points;
            /// <summary>
            /// Minimum IR value among the points
            /// </summary>
            public float minValue;
            /// <summary>
            /// Maximum IR value among the points
            /// </summary>
            public float maxValue;
        }

        public BurstPointCloudFilter(int maxPoints)
        {
            _filteredPoints = new NativeArray<Vector4>(maxPoints, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            _containmentResults = new NativeArray<bool>(maxPoints, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        }
        
        /// <summary>
        /// It actually does 3 things in the loop: 1. parse the raw points 2. filtering out points 3. get the min max value of the points 
        /// </summary>
        /// <param name="rawPoints"> raw value from the sensor </param>
        /// <param name="cuboid"> cuboid transform to filter out points that are not inside it</param>
        /// <returns>the PointCloudFilterResult that contains the parsed filtered points, and the min max value of the points </returns>
        public PointCloudFilterResult Filter(NativeArray<float> rawPoints, Transform cuboid)
        {
            var pointCount = rawPoints.Length / 4;
            var filterJob = new FilterJob
            {
                rawPoints = rawPoints,
                filteredPoints = _filteredPoints,
                cuboidMatrix = cuboid.worldToLocalMatrix,
                containmentResults = _containmentResults,
            };
            var resizeJob = new ResizeJob
            {
                pointCount = pointCount,
                points = _filteredPoints,
                containmentResults = _containmentResults,
                filteredCount = new NativeArray<int>(1, Allocator.TempJob),
                minMaxResult = new NativeArray<float>(2, Allocator.TempJob),
            };

            var filterHandle = filterJob.Schedule(rawPoints.Length, 64);
            var resizeHandle = resizeJob.Schedule(filterHandle);

            filterHandle.Complete();
            resizeHandle.Complete();

            var min = resizeJob.minMaxResult[0];
            var max = resizeJob.minMaxResult[1];
            var filteredCount = resizeJob.filteredCount[0];
            var filteredPoints = _filteredPoints.GetSubArray(0, filteredCount);

            resizeJob.filteredCount.Dispose();
            resizeJob.minMaxResult.Dispose();

            return new PointCloudFilterResult
            {
                points = filteredPoints,
                minValue = min,
                maxValue = max,
            };
        }

        [BurstCompile]
        private struct FilterJob : IJobParallelFor
        {
            // Input
            [ReadOnly] public NativeArray<float> rawPoints;
            [ReadOnly] public Matrix4x4 cuboidMatrix;

            // Output
            [WriteOnly]
            [NativeDisableParallelForRestriction]
            public NativeArray<Vector4> filteredPoints;
            [WriteOnly]
            [NativeDisableParallelForRestriction]
            public NativeArray<bool> containmentResults;

            public void Execute(int index)
            {
                // We're only interested in indices that are a multiple of 4. Exit early, otherwise.
                if (index % 4 > 0)
                {
                    return;
                }

                // Parse the points.
                var pointWorldPos = new Vector4(rawPoints[index], rawPoints[index + 1], rawPoints[index + 2], 1);
                var pointValue = rawPoints[index + 3];

                // Filter out points that are not inside the cuboid.
                Vector3 localPos = math.mul(cuboidMatrix, pointWorldPos).xyz;
                var isContained = math.all(math.abs(localPos) < 0.5f);
                var pointIndex = index / 4;
                containmentResults[pointIndex] = isContained;
                filteredPoints[pointIndex] = new Vector4(localPos.x, localPos.y, localPos.z, pointValue);
            }
        }

        public void Dispose()
        {
            _filteredPoints.Dispose();
            _containmentResults.Dispose();
        }

        [BurstCompile]
        private struct ResizeJob : IJob
        {
            // Input/Output
            public NativeArray<Vector4> points;

            // Input
            [ReadOnly] public int pointCount;
            [ReadOnly] public NativeArray<bool> containmentResults;

            // Output
            [WriteOnly] public NativeArray<int> filteredCount;
            public NativeArray<float> minMaxResult;

            public void Execute()
            {
                minMaxResult[0] = float.MaxValue;
                minMaxResult[1] = float.MinValue;
                var newIndex = 0;
                for (int i = 0; i < pointCount; i++)
                {
                    var isContained = containmentResults[i];
                    if (isContained)
                    {
                        minMaxResult[0] = math.min(points[i].w, minMaxResult[0]);
                        minMaxResult[1] = math.max(points[i].w, minMaxResult[1]);
                        points[newIndex] = points[i];
                        ++newIndex;
                    }
                }

                filteredCount[0] = newIndex;
            }
        }
    }
}
