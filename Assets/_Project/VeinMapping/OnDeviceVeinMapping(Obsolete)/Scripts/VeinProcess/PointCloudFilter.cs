using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace NUHS.VeinMapping.VeinProcess
{
    /// <summary>
    /// Process the raw point cloud
    /// </summary>
    public class PointCloudFilter
    {
        private List<Vector4> _filteredPoints = new List<Vector4>();
        private float _minValue;
        private float _maxValue;
        public struct PointCloudFilterResult
        {
            public List<Vector4> points;
            public float minValue;
            public float maxValue;
        }
        
        /// <summary>
        /// It actually does 3 things in the loop: 1. parse the raw points 2. filtering out points 3. get the min max value of the points 
        /// </summary>
        /// <param name="rawPoints"> raw value from the sensor </param>
        /// <param name="cuboid"> cuboid transform to filter out points that are not inside it</param>
        /// <returns>the PointCloudFilterResult that contains the parsed filtered points, and the min max value of the points </returns>
        public PointCloudFilterResult Filter(float[] rawPoints, Transform cuboid)
        {
            _filteredPoints.Clear();
            _minValue = float.MaxValue;
            _maxValue = float.MinValue;
            var length = rawPoints.Length / 4;
            for (int i = 0; i < length; i++)
            {
                //parse the points
                var pointWorldPos = new Vector3(rawPoints[i * 4], rawPoints[i * 4 + 1], rawPoints[i * 4 + 2]);
                var pointValue = rawPoints[i * 4 + 3];
                
                //filtering out points that are not inside the box
                var localPos= cuboid.InverseTransformPoint(pointWorldPos);
                if (Mathf.Abs(localPos.x) >= 0.5f || Mathf.Abs(localPos.y) >=0.5f || Mathf.Abs(localPos.z) >= 0.5f) continue;
                _filteredPoints.Add(new Vector4(localPos.x, localPos.y,localPos.z,pointValue));
                
                //calculate min and max value
                _minValue = Math.Min(_minValue, pointValue);
                _maxValue = Math.Max(_maxValue, pointValue);
            }

            return new PointCloudFilterResult()
            {
                points = _filteredPoints,
                minValue = _minValue,
                maxValue = _maxValue
            };
        }
    }
}