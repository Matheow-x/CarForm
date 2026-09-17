using CarForm.Data;
using CarForm.Printing;
using CarForm.Services;
using CarForm.ViewModels;
using CarForm.Views;
using CarForm.Views.Dialogs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CarForm.Services;

public static class ServiceCollectionExtensions
{
    /// <summary>Registers every layer: Data -> Services -> ViewModels -> Views.</summary>
    public static IServiceCollection AddCarForm(this IServiceCollection services)
    {
        // ---- infrastructure ----
        services.AddDbContextFactory<AppDbContext>(options =>
            options.UseSqlite(Core.Helpers.AppPaths.ConnectionString));

        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<IAuditService, AuditService>();
        services.AddSingleton<IImageStore, ImageStore>();
        services.AddSingleton<IThemeManager, ThemeManager>();
        services.AddSingleton<IViewDialogMap, ViewDialogMap>();
        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<IDatabaseInitializer, DatabaseInitializer>();

        // ---- domain services ----
        services.AddSingleton<ICompanyService, CompanyService>();
        services.AddSingleton<IOwnerService, OwnerService>();
        services.AddSingleton<IVehicleService, VehicleService>();
        services.AddSingleton<IDocumentService, DocumentService>();
        services.AddSingleton<INumberingService, NumberingService>();
        services.AddSingleton<IBackupService, BackupService>();

        // ---- printing ----
        services.AddSingleton<IPrintService, PrintService>();

        // ---- view models ----
        services.AddTransient<MainViewModel>();
        services.AddTransient<IPageViewModel, NewDocumentWizardViewModel>();
        services.AddTransient<IPageViewModel, SavedDocumentsViewModel>();
        services.AddTransient<IPageViewModel, VehiclesViewModel>();
        services.AddTransient<IPageViewModel, OwnersViewModel>();
        services.AddTransient<IPageViewModel, CompanySettingsViewModel>();
        services.AddTransient<IPageViewModel, DocumentSettingsViewModel>();
        services.AddTransient<IPageViewModel, ReportsViewModel>();
        services.AddTransient<IPageViewModel, AppSettingsViewModel>();
        services.AddTransient<PrintLayoutEditorViewModel>();

        services.AddTransient<PrintPreviewViewModel>();

        // ---- views ----
        services.AddTransient<MainWindow>();
        services.AddTransient<PrintPreviewWindow>();
        services.AddTransient<Views.Dialogs.PrintLayoutEditorWindow>();

        return services;
    }
}
