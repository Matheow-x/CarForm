using System.Threading.Tasks;
using CarForm.Models;

namespace CarForm.Services;

public interface ICompanyService
{
    /// <summary>Returns the single company profile, creating an empty one on first call.</summary>
    Task<Company> GetAsync();

    /// <summary>
    /// Synchronous access to the (cached) company profile. Required by the printing pipeline,
    /// which renders on the UI thread and therefore must never block on async database work.
    /// </summary>
    Company Get();

    Task SaveAsync(Company company);
}
