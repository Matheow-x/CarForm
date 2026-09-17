using System.Collections.Generic;
using System.Threading.Tasks;
using CarForm.Models;

namespace CarForm.Services;

public interface IOwnerService
{
    Task<List<Owner>> SearchAsync(string? term = null, OwnerType? type = null, int maxResults = 500);

    Task<List<Owner>> GetAllAsync();

    Task<Owner?> GetAsync(int id);

    Task<Owner> SaveAsync(Owner owner);

    Task DeleteAsync(int id);

    Task<bool> ExistsAsync(string? nationalCode, int? excludeId = null);
}
