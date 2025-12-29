using System;
using System.Windows;

namespace AccordatoreChitarra
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Handle global unhandled exceptions
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            DispatcherUnhandledException += App_DispatcherUnhandledException;
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = e.ExceptionObject as Exception;
            MessageBox.Show(
                $"Errore critico non gestito:\n\n{ex?.Message}\n\n{ex?.StackTrace}",
                "Errore Critico",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show(
                $"Errore non gestito:\n\n{e.Exception.Message}",
                "Errore",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            
            e.Handled = true;
        }
    }
}