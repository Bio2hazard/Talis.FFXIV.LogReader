// Talis.FFXIV.LogReader
// LogEntryRepository.cs

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using Microsoft.WindowsAPICodePack.Dialogs;
using NLog;
using Talis.FFXIV.LogReader.Model;

namespace Talis.FFXIV.LogReader.DataAccess
{
    public class LogEntryRepository : INotifyPropertyChanged
    {
        #region Fields

        private static Logger logger = LogManager.GetCurrentClassLogger();

        private List<LogEntry> _logEntries = new List<LogEntry>();
        private List<FileInfo> _logFiles = new List<FileInfo>();

        private int _numEntriesTotal = 0;
        private int _numFilesLoaded = 0;

        public DirectoryInfo LogFolder { get; protected set; }

        public int NumFilesTotal
        {
            get { return _logFiles.Count(); }
        }

        public int NumFilesLoaded
        {
            get { return _numFilesLoaded; }
            set
            {
                _numFilesLoaded = value;
                RaisePropertyChanged();
            }
        }

        public int NumEntriesTotal
        {
            get { return _numEntriesTotal; }
            set
            {
                _numEntriesTotal = value;
                RaisePropertyChanged();
            }
        }

        public int NumEntriesLoaded
        {
            get { return _logEntries.Count(); }
        }

        public ICommand LoadCommand { get; private set; }
        public ICommand BrowseCommand { get; private set; }

        #endregion // Fields

        #region Constructor

        public LogEntryRepository(string path)
        {
            LoadCommand = new RelayCommand(param => Load());
            BrowseCommand = new RelayCommand(param => OpenFileBrowser());

            CheckFolder(path);
        }

        #endregion // Constructor

        #region Public Interface

        public event EventHandler<LogEntryAddedEventArgs> LogEntryAdded;
        public event EventHandler<EventArgs> LogEntriesCleared;

        public void AddLogEntry(LogEntry logEntry)
        {
            if (logEntry == null)
            {
                throw new ArgumentNullException("logEntry");
            }

            _logEntries.Add(logEntry);

            RaisePropertyChanged("NumEntriesLoaded");

            if (LogEntryAdded != null)
            {
                LogEntryAdded(this, new LogEntryAddedEventArgs(logEntry));
            }
        }

        public List<LogEntry> GetEntries()
        {
            return new List<LogEntry>(_logEntries);
        }

        private static int Floor(double d)
        {
            return (int) d;
        }

        public void Load()
        {
            try
            {
                if (_logFiles.Count < 1)
                {
                    throw new MissingMemberException();
                }
                _logEntries.Clear();

                if (LogEntriesCleared != null)
                {
                    LogEntriesCleared(this, new EventArgs());
                }

                var SortedLogFiles = _logFiles.OrderByDescending(FileInfo => FileInfo.LastWriteTime)
                                              .ToList();

                foreach (var logFile in SortedLogFiles)
                {
                    var worker = new BackgroundWorker();
                    worker.WorkerReportsProgress = true;

                    worker.DoWork += delegate(object s, DoWorkEventArgs args)
                    {
                        var fileInfo = (FileInfo) args.Argument;

                        using (var file = File.OpenRead(fileInfo.FullName))
                        {
                            //logger.Trace("Opening file " + file.Name);
                            file.Seek(0, SeekOrigin.Begin);

                            var tempByte = new byte[4];

                            file.Read(tempByte, 0, 4); // First 4 bytes are the internal start index of the contained entries
                            var startIndex = BitConverter.ToInt32(tempByte, 0);

                            file.Read(tempByte, 0, 4); // Next 4 bytes are the internal end index of the contained entries
                            var endIndex = BitConverter.ToInt32(tempByte, 0);

                            var numIndices = endIndex - startIndex; // Now we know how many entries to read

                            worker.ReportProgress(numIndices);

                            var logIndices = LogReadIndex(file, numIndices); // Get the offset of each log entry

                            var lastIndex = 0;

                            var i = 0;
                            foreach (var logIndex in logIndices) // Iterate through all log entries and parse them
                            {
                                i++;

                                var length = logIndex - lastIndex;

                                var logEntry = new byte[length];

                                file.Read(logEntry, 0, length);
                                lastIndex = logIndex;

                                worker.ReportProgress(-length, ParseLogEntry(logEntry));
                                //worker.ReportProgress(0, ParseLogEntry(logEntry));

                                // Sleep prevents freezing the UI but is far from ideal... Virtualization or something might help, not sure
                                Thread.Sleep(5);
                            }
                        }
                    };

                    worker.ProgressChanged += delegate(object s, ProgressChangedEventArgs args)
                    {
                        // Kinda Hacky, but works
                        if (args.ProgressPercentage > 0)
                        {
                            NumEntriesTotal += args.ProgressPercentage;
                        }
                        else
                        {
                            AddLogEntry((LogEntry) args.UserState);
                        }
                    };

                    worker.RunWorkerCompleted += delegate(object s, RunWorkerCompletedEventArgs args) { NumFilesLoaded++; };

                    worker.RunWorkerAsync(logFile);
                }
            }
            catch (Exception ex)
            {
                logger.ErrorException("Log Entry Loader Failed", ex);
            }
        }

        public void OpenFileBrowser()
        {
            var folderPicker = new CommonOpenFileDialog();

            folderPicker.Title = "Select folder to read logs from";
            folderPicker.IsFolderPicker = true;

            folderPicker.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\My Games\FINAL FANTASY XIV - A Realm Reborn";

            folderPicker.EnsureFileExists = true;
            folderPicker.EnsurePathExists = true;
            folderPicker.EnsureReadOnly = false;
            folderPicker.EnsureValidNames = true;
            folderPicker.Multiselect = false;
            folderPicker.ShowPlacesList = true;

            if (folderPicker.ShowDialog() == CommonFileDialogResult.Ok)
            {
                var folder = folderPicker.FileName;

                if (LogFolder == null || folder != LogFolder.FullName)
                {
                    CheckFolder(folder);
                }
            }
        }

        #endregion // Public Interface

        #region Private Helpers

        private void CheckFolder(string path)
        {
            if (Directory.Exists(path))
            {
                var logFolder = new DirectoryInfo(path);

                // Remove all old logfiles
                _logFiles.Clear();
                RaisePropertyChanged("NumFilesTotal");
                NumFilesLoaded = 0;

                ParseFolder(logFolder, 0);

                if (_logFiles.Count > 0)
                {
                    LogFolder = logFolder;
                }
            }
        }

        private void ParseFolder(DirectoryInfo root, int currentDepth)
        {
            // How deep to recurse
            var maxDepth = 3;

            var logFiles = root.GetFiles("*.log");
            foreach (var logFile in logFiles)
            {
                _logFiles.Add(logFile);
                RaisePropertyChanged("NumFilesTotal");
            }

            if (currentDepth < maxDepth)
            {
                var subDirectories = root.GetDirectories();
                foreach (var subDirectory in subDirectories)
                {
                    ParseFolder(subDirectory, currentDepth + 1);
                }
            }
        }

        private List<int> LogReadIndex(FileStream file, int numIndices)
        {
            var logIndices = new List<int>(numIndices);

            int i;
            var tempByte = new byte[4];
            var tempIndex = 0;

            for (i = 0; i < numIndices; i++)
            {
                file.Read(tempByte, 0, 4);
                tempIndex = BitConverter.ToInt32(tempByte, 0);

                if (tempIndex > 0)
                {
                    logIndices.Add(tempIndex);
                }
            }
            return logIndices;
        }

        private LogEntry ParseLogEntry(byte[] entry)
        {
            Int64 unixTime = 0;
            var code = "";
            var name = "";
            var message = "";

            var _position = 0;

            unixTime = Convert.ToInt64(Encoding.UTF8.GetString(entry, 0, 8), 16);

            code = Encoding.UTF8.GetString(entry, 8, 4);

            var bytesWithoutName = 0;

            if (entry[13] == 0x02 && entry[14] == 0x27) // We got a name
            {
                _position = 15;

                name = parseName(ref entry, ref _position);

                _position += 2; // Need to skip the following : and push pointer to the next position
            }
            else if (entry[13] == 0x3A && entry[14] == 0x20 && entry[15] == 0x20 && entry[16] == 0xEE && entry[17] == 0x81 && entry[18] == 0xAF && entry[19] == 0x20) // We got a combat log entry (?)
            {
                _position = 20;
            }
            else if (entry[13] == 0x3A) // We got no name
            {
                _position = 14;
            }
            else // Monster Name
            {
                _position = 13;
                while (entry[_position] != 0x3A)
                {
                    _position++;
                    bytesWithoutName++;
                }
                name = Encoding.UTF8.GetString(entry, _position - bytesWithoutName, bytesWithoutName);
                bytesWithoutName = 0;
                _position++;
            }

            for (; _position < entry.Length; _position++)
            {
                if (entry[_position] == 0x02 && entry[_position + 1] == 0x27) // This signals the beginning of something special 
                {
                    if (bytesWithoutName > 0)
                    {
                        message += Encoding.UTF8.GetString(entry, _position - bytesWithoutName, bytesWithoutName);
                        bytesWithoutName = 0;
                    }

                    if (entry[_position + 3] == 0x01) // We have a name!
                    {
                        _position += 2; // Need to skip .' signalling a name
                        message += parseName(ref entry, ref _position);
                    }
                    else if (entry[_position + 3] == 0x04) // We have a map
                    {
                        _position += 20; // Skip 20
                        for (; _position < entry.Length; _position++)
                        {
                            if (entry[_position] == 0x02 && entry[_position + 1] == 0x27) // Whew, the map name and coordinates are done
                            {
                                break;
                            }
                            bytesWithoutName++;
                        }
                        message += Encoding.UTF8.GetString(entry, _position - bytesWithoutName, bytesWithoutName);
                        bytesWithoutName = 0;
                        _position += 9;
                    }
                    else
                    {
                        logger.Trace("No idea (entry " + _position + 3 + ":" + entry[_position + 3].ToString() + "):" + Encoding.UTF8.GetString(entry));
                    }
                }
                else
                {
                    bytesWithoutName++;
                }
            }

            if (bytesWithoutName > 0)
            {
                message += Encoding.UTF8.GetString(entry, _position - bytesWithoutName, bytesWithoutName);
            }

            return LogEntry.CreateLogEntry(unixTime, code, name, message);
        }

        private string parseName(ref byte[] entry, ref int _position)
        {
            var rawName = new byte[entry.Length - _position];
            Buffer.BlockCopy(entry, _position, rawName, 0, rawName.Length);

            var nameLength1 = Convert.ToInt32(rawName[0]) - 7;
            var nameLength2 = Convert.ToInt32(rawName[6]) - 1;

            var name1 = Encoding.UTF8.GetString(rawName, 7, nameLength1);
            var name2 = Encoding.UTF8.GetString(rawName, 7 + nameLength1 + 1, nameLength2);

            _position = _position + 7 + nameLength1 + 1 + nameLength2 + 9;
            //Post Name: 02 27 07 CF 01 01 01 FF 01 03

            if (name1 != name2)
            {
                logger.Trace("Name 1:" + name1 + " / Name 2:" + name2);
            }

            return name1;
        }

        #endregion // Private Helpers

        #region Implementation of INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        private void RaisePropertyChanged([CallerMemberName] string caller = "")
        {
            PropertyChanged(this, new PropertyChangedEventArgs(caller));
        }

        #endregion
    }
}
