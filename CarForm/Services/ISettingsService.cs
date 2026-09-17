using System.Threading.Tasks;
using CarForm.Models;

namespace CarForm.Services;

public interface ISettingsService
{
    /// <summary>Cached print settings (always up to date after <see cref="SavePrintAsync"/>).</summary>
    PrintSettings Print { get; }

    /// <summary>Cached application settings.</summary>
    AppSettings App { get; }

    Task ReloadAsync();

    Task SavePrintAsync(PrintSettings settings);

    /// <summary>Temporarily replaces the print settings used for rendering (e.g. live preview of unsaved edits).</summary>
    IDisposable OverridePrint(PrintSettings settings);

    Task SaveAppAsync(AppSettings settings);

    Task<T?> GetAsync<T>(string key) where T : class;

    Task SetAsync<T>(string key, T value) where T : class;
}
