using System;
using System.Collections.Generic;
using NUHS.Common.InternalDebug;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UtilsModule;
using Unity.Collections;
using UnityEngine;

namespace NUHS.VeinMapping.VeinProcess
{
    public class VeinProcessor : IDisposable
    {
        public bool IsBusy { get; private set; }
        
        private Transform _cuboidTransform;
        private IVeinSegmentator _veinSegmentator;
        private BurstPointCloudFilter _pointCloudFilter;
        private float _imgSize;
        
        private Mat _segmentationInputMat;
        private Mat _segmentationOutputMat;
        private Scalar _zeroScalar;
        private Mat _kernel; //for dilation and erosion
        private Dictionary<Vector2Int, float> _inputPointDic;
        private byte[] _pixelValues = new byte[1];

        public VeinProcessor(Transform cuboidTransform, IVeinSegmentator veinSegmentator, BurstPointCloudFilter pointCloudFilter, int imgSize)
        {
            _cuboidTransform = cuboidTransform;
            _veinSegmentator = veinSegmentator;
            _pointCloudFilter = pointCloudFilter;
            _imgSize = imgSize;
            _kernel = new Mat(11, 11, CvType.CV_8UC1);
            _segmentationInputMat = Mat.zeros(imgSize,imgSize, CvType.CV_8UC1);
            _segmentationOutputMat = Mat.zeros(imgSize, imgSize, CvType.CV_8UC1);
            _zeroScalar = new Scalar(0);
            _inputPointDic = new Dictionary<Vector2Int, float>();
        }

        /// <summary>
        /// This Process does a few steps
        /// 1. parse and filter out points within the cuboid
        /// 2. project the points into an image
        /// 3. perform segmentation on the image
        /// 4. project the image back to 3D points
        /// </summary>
        /// <param name="rawPoints">raw points from VeinSensorStream</param>
        /// <param name="outputPoints">it will be populated with point cloud for rendering at the end of the process</param>
        public async void Process(NativeArray<float> rawPoints, List<Vector4> outputPoints)
        {
            IsBusy = true;
            
            _segmentationInputMat.setTo(_zeroScalar);
            _segmentationOutputMat.setTo(_zeroScalar);
            _inputPointDic.Clear();
            outputPoints.Clear();

            var filterResult = _pointCloudFilter.Filter(rawPoints, _cuboidTransform);
            ConvertCuboidLocalPointsToInputSegmentationImage(filterResult.points, filterResult.minValue, filterResult.maxValue);
            await _veinSegmentator.Segment(_segmentationInputMat, _segmentationOutputMat);
            ConvertOutputSegmentationImageToWorldPoints(filterResult.points, outputPoints);
            DebugUtils.SegmentationTick();

            IsBusy = false;
        }

        /// <summary>
        /// Project the local points into an image
        /// </summary>
        /// <param name="points">Points in the cuboid local space</param>
        /// <param name="minValue">Minimum IR value among the points</param>
        /// <param name="maxValue">Maximum IR value among the points</param>
        private void ConvertCuboidLocalPointsToInputSegmentationImage(NativeArray<Vector4> points, float minValue, float maxValue)
        {
            var length = points.Length;
            for (int i = 0; i < length; i++)
            {
                var point = points[i];
                var pixelPos =  new Vector2Int((int)((point.x + 0.5f) * _imgSize), (int)((point.y + 0.5f) * _imgSize));
                
                //take the point nearest to the image plane if there are multiple points at the same pixel 
                if (!_inputPointDic.ContainsKey(pixelPos) || point.z < _inputPointDic[pixelPos])
                {
                    _inputPointDic[pixelPos] = point.z;
                    var normalizedValue = (point.w - minValue) / (maxValue - minValue) * 255;
                    normalizedValue = 255 - normalizedValue; //the vein has lower value in the raw data, inverse it so that vein has higher value 
                    _pixelValues[0] = (byte) normalizedValue;
                    _segmentationInputMat.put(pixelPos.y, pixelPos.x, _pixelValues);
                }
            }

            //dilate and erode to fill the gaps
            Imgproc.dilate(_segmentationInputMat, _segmentationInputMat, _kernel);
            Imgproc.erode(_segmentationInputMat, _segmentationInputMat, _kernel);
        }

        /// <summary>
        /// Project image back to 3D points in world space
        /// </summary>
        /// <param name="points">Points in the cuboid local space</param>
        /// <param name="outputPoints">This list of points will be populated by this function</param>
        private void ConvertOutputSegmentationImageToWorldPoints(NativeArray<Vector4> points, List<Vector4> outputPoints)
        {
            var matIndexer = new MatIndexer(_segmentationOutputMat);
            var length = points.Length;
            for (int i = 0; i < length; i++)
            {
                var point = points[i];
                var pixelPos =  new Vector2Int((int)((point.x + 0.5f) * _imgSize), (int)((point.y + 0.5f) * _imgSize));
                matIndexer.get(pixelPos.y,pixelPos.x,_pixelValues);
                var pixelValue = (int)_pixelValues[0];
                if (pixelValue <= 0) continue;
                
                var worldPos =
                    _cuboidTransform.TransformPoint(point);
                outputPoints.Add(new Vector4(worldPos.x,worldPos.y,worldPos.z,pixelValue/255.0f));
            }
        }

        public void Dispose()
        {
            _veinSegmentator?.Dispose();
            _kernel?.Dispose();
            _segmentationInputMat?.Dispose();
            _segmentationOutputMat?.Dispose();
            _pointCloudFilter?.Dispose();
        }
    }
}