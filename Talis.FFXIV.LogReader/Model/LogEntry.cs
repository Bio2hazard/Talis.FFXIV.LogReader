// Talis.FFXIV.LogReader
// LogEntry.cs

using System;

namespace Talis.FFXIV.LogReader.Model
{
    /// <summary>
    ///     Represents a single entry in a FFXIV Log File
    /// </summary>
    public class LogEntry
    {
        #region Properties

        public Int64 UnixTime { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string Message { get; set; }

        #endregion // Properties

        #region Creation

        protected LogEntry()
        {
        }

        public static LogEntry CreateNewLogEntry()
        {
            return new LogEntry();
        }

        public static LogEntry CreateLogEntry(Int64 unixTime, string code, string name, string message)
        {
            return new LogEntry
            {
                UnixTime = unixTime,
                Code = code,
                Name = name,
                Message = message
            };
        }

        #endregion // Creation
    }
}
