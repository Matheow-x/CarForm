using System.Linq;
using System.Threading.Tasks;
using CarForm.Data;
using CarForm.Models;
using Microsoft.EntityFrameworkCore;

namespace CarForm.Services;

public class CompanyService : ICompanyService
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly IAuditService _audit;

    public CompanyService(IDbContextFactory<AppDbContext> factory, IAuditService audit)
    {
        _factory = factory;
        _audit = audit;
    }

    /// <summary>The company profile is cached: it is read on every print / PDF export.</summary>
    private Company? _cache;

    public async Task<Company> GetAsync()
    {
        if (_cache is not null) return _cache;

        await using var db = await _factory.CreateDbContextAsync();
        var company = await db.Companies.AsNoTracking().FirstOrDefaultAsync();
        if (company is not null)
        {
            _cache = company;
            return company;
        }

        var created = new Company { Name = string.Empty };
        await SaveAsync(created);
        return created;
    }

    public Company Get()
    {
        if (_cache is not null) return _cache;

        using var db = _factory.CreateDbContext();
        var company = db.Companies.AsNoTracking().FirstOrDefault();
        if (company is null)
        {
            company = new Company { Name = string.Empty };
            SaveSync(company);
        }

        _cache = company;
        return company;
    }

    private void SaveSync(Company company)
    {
        using var db = _factory.CreateDbContext();

        Company entity;
        var isNew = company.Id == 0;
        if (isNew)
        {
            entity = new Company();
            db.Companies.Add(entity);
        }
        else
        {
            entity = db.Companies.First(c => c.Id == company.Id);
        }

        db.Entry(entity).CurrentValues.SetValues(company);
        db.SaveChanges();

        company.Id = entity.Id;
        _cache = company;

        _audit.Log(AuditEntityType.Company, isNew ? AuditAction.Create : AuditAction.Update, entity.Id,
            $"{(isNew ? "ایجاد" : "ویرایش")} اطلاعات شرکت: {entity.Name}");
    }

    public async Task SaveAsync(Company company)
    {
        await using var db = await _factory.CreateDbContextAsync();

        Company entity;
        var isNew = company.Id == 0;
        if (isNew)
        {
            entity = new Company();
            db.Companies.Add(entity);
        }
        else
        {
            entity = await db.Companies.FirstAsync(c => c.Id == company.Id);
        }

        db.Entry(entity).CurrentValues.SetValues(company);
        await db.SaveChangesAsync();

        _cache = company;
        company.Id = entity.Id;

        await _audit.LogAsync(AuditEntityType.Company, isNew ? AuditAction.Create : AuditAction.Update, entity.Id,
            $"{(isNew ? "ایجاد" : "ویرایش")} اطلاعات شرکت: {entity.Name}");
    }
}
