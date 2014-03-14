// Talis.FFXIV.LogReader
// App.xaml.cs

using System;
using System.Windows;
using Talis.FFXIV.LogReader.ViewModel;

namespace Talis.FFXIV.LogReader
{
    /// <summary>
    ///     Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        static App()
        {
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var window = new MainWindow();

            var path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\My Games\FINAL FANTASY XIV - A Realm Reborn";

            var viewModel = new MainWindowViewModel(path);

            window.DataContext = viewModel;

            window.Show();
        }
    }
}
