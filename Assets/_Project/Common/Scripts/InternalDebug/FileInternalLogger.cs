using System.IO;
using System.Text;

namespace NUHS.Common.InternalDebug
{
    /// <summary>
    /// Sends the debug logs to file/disk.
    /// Text logs are written to a single file specified by `logFileName`. (Default: "debug.log")
    /// Images and other binary data files are written as individual timestamped files in the base directory.
    /// </summary>
    public class FileInternalLogger : IInternalLogger
    {
        private LogLevel _level;
        private string _baseDirectory;
        private FileStream _textLogFile;

        /// <summary>
        /// Create a file logger that logs to the Application.persistentDataPath and
        /// log file name debug.log by default.
        /// </summary>
        /// <param name="level">Minimum log level</param>
        public FileInternalLogger(LogLevel level) : this(level, UnityEngine.Application.persistentDataPath) { }

        /// <summary>
        /// Create a file logger that logs to the base directory specified. 
        /// </summary>
        /// <param name="level">Minimum log level</param>
        /// <param name="baseDirectory">Directory where log files are stored</param>
        /// <param name="logFileName">File name for text logs</param>
        public FileInternalLogger(LogLevel level, string baseDirectory, string logFileName = "debug.log")
        {
            _level = level;
            _baseDirectory = baseDirectory;

            if (!Directory.Exists(_baseDirectory))
            {
                Directory.CreateDirectory(_baseDirectory);
            }

            UnityEngine.Debug.Log($"File logger started: {_baseDirectory}");
            _textLogFile = File.Open(_baseDirectory + Path.DirectorySeparatorChar + logFileName, FileMode.Append);
        }

        public async void FileLogEntryReceived(FileLogEntry entry)
        {
            if (entry.Level >= _level)
            {
                // Log an entry in text log that a file is being written
                TextLogEntryReceived(new TextLogEntry(entry.Level, $"Saving file: {entry.Filename}", entry.Timestamp));

                // Write to file
                var file = File.Open(_baseDirectory + Path.DirectorySeparatorChar + entry.Filename, FileMode.CreateNew);
                await file.WriteAsync(entry.Data, 0, entry.Data.Length);
                file.Close();
            }
        }

        public async void TextLogEntryReceived(TextLogEntry entry)
        {
            if (entry.Level >= _level)
            {
                // Append to text log
                var message = Encoding.UTF8.GetBytes($"[{entry.Timestamp.ToString("u")}] {entry.Message}\r\n");
                await _textLogFile.WriteAsync(message, 0, message.Length);
                await _textLogFile.FlushAsync();
            }
        }

        public void Dispose()
        {
            _textLogFile.Close();
        }
    }
}
