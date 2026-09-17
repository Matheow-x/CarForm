using System;
using System.Threading.Tasks;

namespace CarForm.Services;

public interface INumberingService
{
    /// <summary>Next document number, formatted according to the configured template.</summary>
    Task<string> SuggestAsync(DateTime documentDate);

    /// <summary>Same as <see cref="SuggestAsync"/> but persists the incremented sequence.</summary>
    Task<string> ReserveAsync(DateTime documentDate);

    /// <summary>Ensures the sequence never goes backwards after a restore / manual edit.</summary>
    Task SyncSequenceAsync();
}
