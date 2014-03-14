// Talis.FFXIV.LogReader
// LogEntryAddedEventArgs.cs

using Talis.FFXIV.LogReader.Model;

namespace Talis.FFXIV.LogReader.DataAccess
{
    public class LogEntryAddedEventArgs
    {
        public LogEntryAddedEventArgs(LogEntry newLogEntry)
        {
            NewLogEntry = newLogEntry;
        }

        public LogEntry NewLogEntry { get; private set; }
    }
}
