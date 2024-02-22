using System.Threading.Tasks;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils;
using UnityEngine;

namespace NUHS.VeinMapping.VeinProcess
{
    public class ThresholdVeinSegmentator : IVeinSegmentator
    {
        private Mat _outputMat;
        private int _inputHeight;
        private int _inputWidth;
        private bool _isBusy = false;

        public async Task Segment(Mat input, Mat output)
        {
            if (_isBusy) return;
            _isBusy = true;
            var inputHeight = input.height();
            var inputWidth = input.width();
            
            if (inputHeight == 0 || inputWidth == 0)
            {
                Debug.LogError($"Input height {inputHeight} width {inputWidth} should not be zero");
                return;
            }
            if (inputHeight!=output.height() || inputWidth!=output.width())
            {
                Debug.LogError($"Input height {inputHeight} width {inputWidth} should match output height {output.height()} width {output.width()}");
                return;
            }
            if (input.channels() != 1 || output.channels() !=1)
            {
                Debug.LogError($"Both input channel {input.channels()} and output channel {output.channels()} should be 1");
                return;
            }
            if (input.type() != CvType.CV_8UC1 || output.type() != CvType.CV_8UC1)
            {
                Debug.LogError($"Both input type {input.type()} and output type {output.type()} should be CV_8UC1");
                return;
            }

            Imgproc.threshold(input, output, 0, 255, Imgproc.THRESH_BINARY | Imgproc.THRESH_OTSU);

            _isBusy = false;
        }

        public bool IsBusy()
        {
            return _isBusy;
        }

        public void Dispose()
        {
        }
    }
}