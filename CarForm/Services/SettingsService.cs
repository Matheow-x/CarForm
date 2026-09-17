using System;
using System.Text.Json;
using System.Threading.Tasks;
using CarForm.Data;
using CarForm.Models;
using Microsoft.EntityFrameworkCore;

namespace CarForm.Services;

public class SettingsService : ISettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly IDbContextFactory<AppDbContext> _factory;

    private PrintSettings _print = new();
    private AppSettings _app = new() { DefaultInvoiceItems = AppSettings.CreateDefaultInvoiceItems() };

    public SettingsService(IDbContextFactory<AppDbContext> factory)
    {
        _factory = factory;
    }

    public PrintSettings Print => _print;

    public AppSettings App => _app;

    public async Task ReloadAsync()
    {
        await using var db = await _factory.CreateDbContextAsync();
        _print = await LoadAsync(db, SettingKeys.Print, () => new PrintSettings()) ?? new PrintSettings();
        _app = await LoadAsync(db, SettingKeys.App, () => new AppSettings { DefaultInvoiceItems = AppSettings.CreateDefaultInvoiceItems() })
               ?? new AppSettings { DefaultInvoiceItems = AppSettings.CreateDefaultInvoiceItems() };
    }

    public async Task SavePrintAsync(PrintSettings settings)
    {
        _print = settings;
        await SetAsync(SettingKeys.Print, settings);
    }

    public IDisposable OverridePrint(PrintSettings settings)
    {
        var previous = _print;
        _print = settings;
        return new CarForm.Core.Helpers.DisposableAction(() => _print = previous);
    }

    public async Task SaveAppAsync(AppSettings settings)
    {
        _app = settings;
        await SetAsync(SettingKeys.App, settings);
    }

    public async Task<T?> GetAsync<T>(string key) where T : class
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await LoadAsync(db, key, () => (T)Activator.CreateInstance(typeof(T))!);
    }

    public async Task SetAsync<T>(string key, T value) where T : class
    {
        await using var db = await _factory.CreateDbContextAsync();
        var json = JsonSerializer.Serialize(value, JsonOptions);
        var existing = await db.Settings.FirstOrDefaultAsync(s => s.Key == key);
        if (existing is null)
        {
            db.Settings.Add(new Setting { Key = key, Value = json });
        }
        else
        {
            existing.Value = json;
        }
        await db.SaveChangesAsync();
    }

    private static async Task<T?> LoadAsync<T>(AppDbContext db, string key, Func<T> factory) where T : class
    {
        Setting? setting;
        try
        {
            setting = await db.Settings.AsNoTracking().FirstOrDefaultAsync(s => s.Key == key);
        }
        catch (System.Data.Common.DbException)
        {
            // The settings table might not exist yet - fall back to defaults.
            return factory();
        }

        if (setting?.Value is null) return factory();

        try
        {
            return JsonSerializer.Deserialize<T>(setting.Value, JsonOptions) ?? factory();
        }
        catch (JsonException)
        {
            return factory();
        }
    }
}
