// Talis.FFXIV.LogReader
// MainWindowViewModel.cs

using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using Talis.FFXIV.LogReader.DataAccess;

namespace Talis.FFXIV.LogReader.ViewModel
{
    public class MainWindowViewModel
    {
        #region Fields

        private readonly LogEntryRepository _logEntryRepository;

        public string DisplayName { get; protected set; }

        public LogEntryRepository LogEntryRepository
        {
            get { return _logEntryRepository; }
        }

        public ObservableCollection<LogEntryViewModel> AllLogEntries { get; private set; }

        #endregion // Fields

        #region Constructor

        public MainWindowViewModel(string logFolder)
        {
            DisplayName = "FFXIV LogReader";

            _logEntryRepository = new LogEntryRepository(logFolder);

            _logEntryRepository.LogEntryAdded += OnLogEntryAddedToRepository;

            _logEntryRepository.LogEntriesCleared += OnLogEntriesCleared;

            var all = (from entry in _logEntryRepository.GetEntries() select new LogEntryViewModel(entry)).ToList();

            //foreach (LogEntryViewModel entryvm in all)
            //    entryvm.PropertyChanged += this.OnLogEntryViewModelPropertyChanged;

            AllLogEntries = new ObservableCollection<LogEntryViewModel>(all);
            AllLogEntries.CollectionChanged += OnCollectionChanged;
        }

        #endregion // Constructor

        #region Event Handling

        private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            /*if (e.NewItems != null && e.NewItems.Count != 0)
                foreach (CustomerViewModel custVM in e.NewItems)
                    custVM.PropertyChanged += this.OnCustomerViewModelPropertyChanged;

            if (e.OldItems != null && e.OldItems.Count != 0)
                foreach (CustomerViewModel custVM in e.OldItems)
                    custVM.PropertyChanged -= this.OnCustomerViewModelPropertyChanged;*/
        }

        private void OnLogEntryAddedToRepository(object sender, LogEntryAddedEventArgs e)
        {
            // Uncomment if you want to filter messages 
            //if (e.NewLogEntry.Message.Contains("Twintania"))
            //{
            var viewModel = new LogEntryViewModel(e.NewLogEntry);
            AllLogEntries.Add(viewModel);
            //}
        }

        private void OnLogEntriesCleared(object sender, EventArgs e)
        {
            AllLogEntries.Clear();
        }

        #endregion // Event Handling
    }
}
