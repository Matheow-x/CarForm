using System;
using System.Threading.Tasks;
using CarForm.Models;

namespace CarForm.Services;

public static class PageKeys
{
    public const string NewDocument = "NewDocument";
    public const string SavedDocuments = "SavedDocuments";
    public const string Vehicles = "Vehicles";
    public const string Owners = "Owners";
    public const string Company = "Company";
    public const string DocumentSettings = "DocumentSettings";
    public const string Reports = "Reports";
    public const string AppSettings = "AppSettings";
}

/// <summary>Decouples page-to-page navigation from the view models that request it.</summary>
public interface INavigationService
{
    void Register(Func<string, Task> navigate, Func<SalesDocument, Task> openDocument);

    Task NavigateAsync(string pageKey);

    Task OpenDocumentAsync(SalesDocument document);
}
