using System;
using System.Threading.Tasks;
using CarForm.Models;

namespace CarForm.Services;

public sealed class NavigationService : INavigationService
{
    private Func<string, Task>? _navigate;
    private Func<SalesDocument, Task>? _openDocument;

    public void Register(Func<string, Task> navigate, Func<SalesDocument, Task> openDocument)
    {
        _navigate = navigate;
        _openDocument = openDocument;
    }

    public Task NavigateAsync(string pageKey)
        => _navigate?.Invoke(pageKey) ?? Task.CompletedTask;

    public Task OpenDocumentAsync(SalesDocument document)
        => _openDocument?.Invoke(document) ?? Task.CompletedTask;
}
