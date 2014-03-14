// Talis.FFXIV.LogReader
// LogEntryViewModel.cs

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Talis.FFXIV.LogReader.Model;

namespace Talis.FFXIV.LogReader.ViewModel
{
    public class LogEntryViewModel : INotifyPropertyChanged
    {
        #region Fields

        private readonly LogEntry _logEntry;

        #endregion // Fields

        #region Constructor

        public LogEntryViewModel(LogEntry logEntry)
        {
            if (logEntry == null)
            {
                throw new ArgumentNullException("LogEntry");
            }

            _logEntry = logEntry;
        }

        #endregion // Constructor

        #region LogEntry Properties

        public Int64 UnixTime
        {
            get { return _logEntry.UnixTime; }
            set
            {
                if (value == _logEntry.UnixTime)
                {
                    return;
                }

                _logEntry.UnixTime = value;

                RaisePropertyChanged();
            }
        }

        public string Code
        {
            get { return _logEntry.Code; }
            set
            {
                if (value == _logEntry.Code)
                {
                    return;
                }

                _logEntry.Code = value;

                RaisePropertyChanged();
            }
        }

        public string Name
        {
            get { return _logEntry.Name; }
            set
            {
                if (value == _logEntry.Name)
                {
                    return;
                }

                _logEntry.Name = value;

                RaisePropertyChanged();
            }
        }

        public string Message
        {
            get { return _logEntry.Message; }
            set
            {
                if (value == _logEntry.Message)
                {
                    return;
                }

                _logEntry.Message = value;

                RaisePropertyChanged();
            }
        }

        public DateTime RealTime
        {
            get
            {
                return (new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(UnixTime)
                                                            .ToLocalTime());
            }
        }

        #endregion // LogEntry Properties

        #region Implementation of INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        private void RaisePropertyChanged([CallerMemberName] string caller = "")
        {
            PropertyChanged(this, new PropertyChangedEventArgs(caller));
        }

        #endregion
    }
}
