using System;
using System.Threading.Tasks;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.UnityUtils;
using UnityEngine;
using Unity.Barracuda;
using Rect = UnityEngine.Rect;

namespace NUHS.VeinMapping.VeinProcess
{
    /// <summary>
    /// Machine learning implementation of the IVeinSegmentator
    /// </summary>
    public class MLVeinSegmentator : IVeinSegmentator
    {
        // It is observed that if HoloLens processes too many layers in one frame, it will crash. This MAX_LAYERS_PER_FRAME is the limit to prevent the crashing
        private readonly int MAX_LAYERS_PER_FRAME = 50;
        // The ML model takes hundreds ms to do the entire inference. If the processing time is more than this value, it will pause and continue at the next frame
        private readonly int MAX_ML_PROCESSING_TIME_PER_FRAME = 30; //in ms

        private bool _busy;
        private bool _isRunning;
        private IWorker _worker;
        private Texture2D _texture2D;
        private RenderTexture _outputRenderTexture;
        private int _size;
        
        /// <summary>
        /// The constructor loads the model, creates the input texture from model input size and create the worker to run the model
        /// </summary>
        /// <param name="onnxModel"> This implementation assumes the model has only one input, which has same width and height. </param>
        public MLVeinSegmentator(NNModel onnxModel)
        {
            var model = ModelLoader.Load(onnxModel);
            var modelInputShape = model.inputs[0].shape; // assume only one input
            var height = modelInputShape[1];
            var width = modelInputShape[2];
            if (height != width)
            {
                Debug.LogError($"Model input height {height} and width {width} does not match. " +
                               $"MLVeinSegmentator expects the model input height is the same as its width");
                return;
            }
            var channel = modelInputShape[3];
            if (channel != 1)
            {
                Debug.LogError($"Model input channel is {channel}. MLVeinSegmentator expects the model input with 1 channel");
                return;
            }
            
            _size = height;
            _isRunning = true;
            _texture2D = new Texture2D(_size, _size, TextureFormat.Alpha8,false);
            _outputRenderTexture = new RenderTexture(256, 256,0);
            _worker = WorkerFactory.CreateWorker(WorkerFactory.Type.ComputePrecompiled, model);
        }
        
        public async Task Segment(Mat input, Mat output)
        {
            if (_busy) return;
            if (input.height() != _size || input.width() != _size || output.height() != _size || output.width() != _size)
            {
                Debug.LogError($"Input height {input.height()} width {input.width()} output height {output.height()} width {output.width()} do not match with the model input size {_size}");
                return;
            }
            if (input.channels() != 1 && output.channels() != 1)
            {
                Debug.LogError($"Input channel {input.channels()} output channel {output.channels()} do not match with the model input channel of 1");
                return;
            }
            if (input.type() != CvType.CV_8UC1 && output.type() != CvType.CV_8UC1)
            {
                Debug.LogError($"Input type {input.type()} and output type {output.type()} do not match the expected type of CV_8UC1");
                return;
            }
            
            _busy = true;
            Utils.matToTexture2D(input, _texture2D, false);
            var inputTensor = new Tensor(_texture2D);
            try
            {
                var t0 = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                int step = 0;
                var enumerator = _worker.StartManualSchedule(inputTensor);
                while (_isRunning && enumerator.MoveNext())
                {
                    var t1 = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    var duration = (int) (t1 - t0);
                    step++;
                    if (duration > MAX_ML_PROCESSING_TIME_PER_FRAME || step >= MAX_LAYERS_PER_FRAME)
                    {
                        t0 = t1;
                        step = 0;
                        await Task.Yield();
                    }
                }

                if (_isRunning)
                {
                    var outputTensor = _worker.PeekOutput();
                    if (outputTensor == null)
                    {
                        Debug.Log("output is null");
                    }
                    else
                    {
                        outputTensor.ToRenderTexture(_outputRenderTexture);
                        RenderTexture.active = _outputRenderTexture;
                        _texture2D.ReadPixels(new Rect(0, 0, _outputRenderTexture.width, _outputRenderTexture.height), 0, 0);
                        _texture2D.Apply();
                        Utils.texture2DToMat(_texture2D, output, false);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.Log(e.StackTrace);
            }
            finally
            {
                inputTensor.Dispose();
                _busy = false;
            }
        }

        public bool IsBusy()
        {
            return _busy;
        }

        public void Dispose()
        {
            _isRunning = false;
            _worker?.Dispose();
        }
    }
}