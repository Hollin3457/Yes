using System;
using NUHS.UltraSound.Recording;
using System.Collections.Generic;
using System.Threading.Tasks;
using Grpc.Core;
using UnityEngine;

namespace NUHS.UltraSound.Reconstruction
{
    public enum UltrasoundReconstructionStatus { Idle, Reconstructing, Failed, Complete }

    public interface IUltrasoundReconstructor
    {
        UltrasoundReconstructionStatus Status { get; }
        string Mesh { get; }
        string FailMessage { get; }

        /// <summary>
        /// Reconstruct the mesh; it is important to 
        /// </summary>
        /// <param name="frames"></param>
        /// <param name="cuboidSize"></param>
        /// <param name="pixelsPerMM"></param>
        void Reconstruct(IUltrasoundRecorder recorder, Vector2 pixelsPerCm, Channel reconstructionChannel, Func<Task> checkChannelConnection);
    }
}
