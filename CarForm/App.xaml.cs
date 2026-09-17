using System.Windows;
using CarForm.Core.Helpers;
using CarForm.Data;
using CarForm.Models;
using CarForm.Services;
using CarForm.Views;
using Microsoft.Extensions.DependencyInjection;

namespace CarForm;

public partial class App : Application
{
    /// <summary>Composition root of the application.</summary>
    public static IServiceProvider Services { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DispatcherUnhandledException += (_, args) =>
        {
            MessageBox.Show(
                $"{args.Exception.Message}\n\n{args.Exception.GetType().Name}",
                "خطای پیش‌بینی نشده",
                MessageBoxButton.OK,
                MessageBoxImage.Error,
                MessageBoxResult.OK,
                MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);
            args.Handled = true;
        };

        AppPaths.EnsureFolders();

        var services = new ServiceCollection();
        services.AddCarForm();
        Services = services.BuildServiceProvider();

        // Database / settings must exist before any view model runs.
        // Executed on the thread pool so that blocking the UI thread during start-up can never
        // deadlock on an async continuation that wants the dispatcher.
        var initializer = Services.GetRequiredService<IDatabaseInitializer>();
        var settings = Services.GetRequiredService<ISettingsService>();
        Task.Run(async () =>
        {
            initializer.Initialize();
            await settings.ReloadAsync();
        }).GetAwaiter().GetResult();

        Services.GetRequiredService<IThemeManager>().Apply(settings.App.Theme);

        var window = Services.GetRequiredService<MainWindow>();
        MainWindow = window;
        window.Show();
    }
}
