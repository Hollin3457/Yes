using System;

namespace NUHS.Common.InternalDebug
{
    public enum LogLevel { Debug, Info, Warning, Error, Critical }

    public class TextLogEntry
    {
        public LogLevel Level { get; private set; }
        public DateTimeOffset Timestamp { get; private set; }
        public string Message { get; private set; }

        public TextLogEntry(LogLevel level, string message) : this(level, message, DateTimeOffset.UtcNow) { }

        public TextLogEntry(LogLevel level, string message, DateTimeOffset timestamp)
        {
            Level = level;
            Message = message;
            Timestamp = timestamp;
        }
    }

    public class FileLogEntry
    {
        public LogLevel Level { get; private set; }
        public DateTimeOffset Timestamp { get; private set; }
        public byte[] Data { get; private set; }
        public string Prefix { get; private set; }
        public string Suffix { get; private set; }

        public string Filename { get => $"{Prefix}-{Timestamp.ToString("yyyy-MM-dd_HH-mm-ss-fff")}{Suffix}"; }

        public FileLogEntry(LogLevel level, byte[] data, string prefix, string suffix) : this(level, data, prefix, suffix, DateTimeOffset.UtcNow) { }

        public FileLogEntry(LogLevel level, byte[] data, string prefix, string suffix, DateTimeOffset timestamp)
        {
            Level = level;
            Data = data;
            Prefix = prefix;
            Suffix = suffix;
            Timestamp = timestamp;
        }
    }

    /// <summary>
    /// Defines a log provider
    /// Methods of the log provider will always be executed on the main thread
    /// </summary>
    public interface IInternalLogger : IDisposable
    {
        void TextLogEntryReceived(TextLogEntry entry);
        void FileLogEntryReceived(FileLogEntry entry);
    }
}
