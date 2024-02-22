using System;
using System.Threading.Tasks;
using OpenCVForUnity.CoreModule;
using UnityEngine;

namespace NUHS.VeinMapping.VeinProcess
{
    public interface IVeinSegmentator : IDisposable
    {
        /// <summary>
        /// Perform segmentation on the input texture2D and the output will be written into the RenderTexture provided
        /// </summary>
        /// <param name="input"> The texture input to be segmented</param>
        /// <param name="output"> The segmentation result in RenderTexture format</param>
        /// <returns></returns>
        Task Segment(Mat input, Mat output);

        bool IsBusy();
    }
}