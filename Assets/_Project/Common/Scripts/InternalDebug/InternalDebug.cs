using System.Collections.Concurrent;
using Newtonsoft.Json;
using UnityEngine;

namespace NUHS.Common.InternalDebug
{
    /// <summary>
    /// Static class containing Log() methods to call to write logs.
    /// The InternalDebugManager class must be attached to a game object in order for this to work.
    /// </summary>
    public static class InternalDebug
    {
        public static ConcurrentQueue<TextLogEntry> TextLogQueue = new ConcurrentQueue<TextLogEntry>();
        public static ConcurrentQueue<FileLogEntry> FileLogQueue = new ConcurrentQueue<FileLogEntry>();
        public static bool IsDebugMode;
        
        public static void Log(LogLevel level, string message)
        {
            if (!IsDebugMode) return;
            TextLogQueue.Enqueue(new TextLogEntry(level, message));
        }

        public static void Log(LogLevel level, object obj)
        {
            if (!IsDebugMode) return;
            TextLogQueue.Enqueue(new TextLogEntry(level, JsonConvert.SerializeObject(obj)));
        }

        public static void Log(LogLevel level, Texture2D tex, string prefix, string suffix = ".jpg", int jpegEncodingQuality = 75)
        {
            if (!IsDebugMode) return;
            FileLogQueue.Enqueue(new FileLogEntry(level, tex.EncodeToJPG(jpegEncodingQuality), prefix, suffix));
        }

        public static void Log(LogLevel level, byte[] data, string prefix, string suffix)
        {
            if (!IsDebugMode) return;
            FileLogQueue.Enqueue(new FileLogEntry(level, data, prefix, suffix));
        }
    }
}
