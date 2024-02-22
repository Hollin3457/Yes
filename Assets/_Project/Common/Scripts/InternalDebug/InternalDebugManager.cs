using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NUHS.Common.InternalDebug
{
    /// <summary>
    /// MonoBehaviour class to process internal logging queue.
    /// This class must be added to an empty GameObject in order for internal logging to work.
    /// 
    /// Loggers can be added and removed on-the-fly by calling AddLogger() and RemoveLogger().
    /// 
    /// To send log messages, use InternalDebug.Log() method.
    /// You may also use classic Debug.Log() but you'll lose LogLevel control.
    /// </summary>
    public class InternalDebugManager : MonoBehaviour
    {
        public static InternalDebugManager Instance { get; private set; }

        private List<IInternalLogger> _loggers;

        private Action<TextLogEntry> _textLogEntryReceived;
        private Action<FileLogEntry> _fileLogEntryReceived;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            _loggers = new List<IInternalLogger>();
        }

        private void OnDestroy()
        {
            foreach (var logger in _loggers)
            {
                _textLogEntryReceived -= logger.TextLogEntryReceived;
                _fileLogEntryReceived -= logger.FileLogEntryReceived;
            }
            Application.logMessageReceivedThreaded -= HandleDebugLog;
            _loggers.Clear();
            _loggers = null;
        }

        /// <summary>
        /// Also handle messages sent to UnityEngine.Debug.Log
        /// </summary>
        private void HandleDebugLog(string logString, string stackTrace, LogType type)
        {
            InternalDebug.Log(LogLevel.Info, logString + "\r\n" + stackTrace);
        }

        /// <summary>
        /// Coroutine to process log queue
        /// </summary>
        /// <returns></returns>
        private void Update()
        {
            while (InternalDebug.TextLogQueue.TryDequeue(out var entry))
            {
                _textLogEntryReceived?.Invoke(entry);
            }

            while (InternalDebug.FileLogQueue.TryDequeue(out var entry))
            {
                _fileLogEntryReceived?.Invoke(entry);
            }
        }

        public void AddLogger(IInternalLogger logger)
        {
            _textLogEntryReceived += logger.TextLogEntryReceived;
            _fileLogEntryReceived += logger.FileLogEntryReceived;
            if (_loggers?.Count == 0) //when the first logger is added
            {
                Application.logMessageReceivedThreaded += HandleDebugLog;
            }
            _loggers?.Add(logger);
        }

        public void RemoveLogger(IInternalLogger logger)
        {
            _textLogEntryReceived -= logger.TextLogEntryReceived;
            _fileLogEntryReceived -= logger.FileLogEntryReceived;
            _loggers?.Remove(logger);
            if (_loggers?.Count == 0) //when the last logger is removed
            {
                Application.logMessageReceivedThreaded -= HandleDebugLog;
            }
        }
    }
}
