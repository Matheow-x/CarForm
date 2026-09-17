using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using CarForm.Core.Helpers;
using CarForm.Models;
using Microsoft.EntityFrameworkCore;

namespace CarForm.Data;

public interface IDatabaseInitializer
{
    void Initialize();
    void SeedDemoData();
}

/// <summary>Creates the SQLite schema on first run and seeds the vehicle catalogue.</summary>
public class DatabaseInitializer : IDatabaseInitializer
{
    private readonly IDbContextFactory<AppDbContext> _factory;

    public DatabaseInitializer(IDbContextFactory<AppDbContext> factory)
    {
        _factory = factory;
    }

    public void Initialize()
    {
        AppPaths.EnsureFolders();

        using var db = _factory.CreateDbContext();

        // EnsureCreated is intentionally used for this single-user offline app:
        // it creates the schema when the file is missing. Switch to Migrations later if needed
        // (see README -> "Adding a migration").
        db.Database.EnsureCreated();

        // Light-weight schema upgrades for databases created by older builds.
        EnsureColumn(db, "Companies", "TaxFileNumber", "TEXT");
        EnsureColumn(db, "Companies", "Mobile", "TEXT");
        EnsureColumn(db, "SalesDocuments", "VehicleBodyType", "TEXT");
        EnsureColumn(db, "Companies", "Website", "TEXT");

        // Columns added for the official sale form template.
        foreach (var column in new[]
                 {
                     "BuyerPosition", "SecondBuyerName", "SecondBuyerPosition", "BuyerIssuePlace", "BuyerJob",
                     "NumberingPermitNo", "VehicleSystem", "VehicleUsageType", "VehicleCylinders", "VehicleAxles",
                     "VehicleWheels", "VehicleCabinNo", "VehicleCapacity"
                 })
        {
            EnsureColumn(db, "SalesDocuments", column, "TEXT");
        }
        EnsureColumn(db, "SalesDocuments", "BuyerBirthDate", "TEXT");
        EnsureColumn(db, "SalesDocuments", "ClearanceDate", "TEXT");

        foreach (var column in new[] { "System", "UsageType", "Cylinders", "Axles", "Wheels", "CabinNo", "Capacity", "Country" })
        {
            EnsureColumn(db, "Vehicles", column, "TEXT");
        }

        foreach (var column in new[]
                 {
                     "VehicleDisplacement", "VehicleCountry", "BuyerBirthPlace", "BuyerHomeAddress", "BuyerWorkAddress",
                     "CustomsPermitNo", "CustomsLicenseNo", "CustomsOffice", "TaxBankBranch", "TollBankBranch"
                 })
        {
            EnsureColumn(db, "SalesDocuments", column, "TEXT");
        }
        EnsureColumn(db, "SalesDocuments", "BuyerIssueDate", "TEXT");

        foreach (var column in new[] { "BirthPlace", "WorkAddress" })
        {
            EnsureColumn(db, "Owners", column, "TEXT");
        }
        EnsureColumn(db, "Owners", "IssueDate", "TEXT");

        SeedCatalogue(db);
        SeedCompany(db);
        SeedSettings(db);

        db.SaveChanges();
    }

    public void SeedDemoData()
    {
        using var db = _factory.CreateDbContext();

        if (!db.Owners.Any())
        {
            db.Owners.AddRange(
                new Owner
                {
                    Type = OwnerType.Individual,
                    FirstName = "امیر",
                    LastName = "کریمی",
                    FatherName = "رضا",
                    NationalCode = "0012345678",
                    IdNumber = "1234",
                    Mobile = "09121234567",
                    Phone = "02188776655",
                    Address = "تهران، خیابان ولیعصر، کوچه گلستان، پلاک ۱۲",
                    PostalCode = "1234567890"
                },
                new Owner
                {
                    Type = OwnerType.LegalEntity,
                    CompanyName = "شرکت بازرگانی پارس خودرو",
                    CompanyNationalId = "10345678901",
                    EconomicCode = "411234567890",
                    RegistrationNumber = "12345",
                    RepresentativeName = "سارا احمدی",
                    Mobile = "09123334455",
                    Phone = "02155667788",
                    Address = "تهران، بزرگراه جلال آل احمد، برج تجاری پارس، طبقه ۵",
                    PostalCode = "9876543210"
                });
            db.SaveChanges();
        }

        var corolla = db.VehicleModels.Include(m => m.Make)
            .FirstOrDefault(m => m.Name == "کرولا" || m.Name == "Corolla");
        if (corolla != null && !db.Vehicles.Any())
        {
            db.Vehicles.Add(new Vehicle
            {
                ModelId = corolla.Id,
                Vin = "JTDBR32E120123456",
                ChassisNumber = "SB1234567890",
                EngineNumber = "1ZZ1234567",
                PlateNumber = "۱۲ب۳۴۵-۱۱",
                Color = "سفید صدفی",
                Trim = "SE",
                ModelYear = 1398,
                Mileage = 85000,
                FuelType = "بنزینی",
                BodyType = "سدان",
                EngineDisplacement = "1800"
            });
            db.SaveChanges();
        }
    }

    #region Seeding

    private static readonly Dictionary<string, string[]> Catalogue = new()
    {
        ["Toyota"] = new[] { "کرولا", "کمری", "راو۴", "پریوس", "لندکروز", "هایلوکس", "یاریس", "پرادو", "هایس" },
        ["Kia"] = new[] { "سراتو", "اسپورتیج", "اپتیما", "سورنتو", "ریو", "پیکانتو", "موهابه", "سول", "استونیک" },
        ["Hyundai"] = new[] { "النترا", "سوناتا", "توسان", "سانتافه", "آزرا", "i20", "i30", "ورنا", "اکسنت" },
        ["Peugeot"] = new[] { "۲۰۶", "۲۰۷", "۳۰۱", "۴۰۵", "پارس", "۲۰۰۸", "۵۰۸", "RD" },
        ["Renault"] = new[] { "تندر ۹۰", "ساندرو", "داستر", "کپچر", "سیمبل", "مگان", "تلیسمان" },
        ["Nissan"] = new[] { "تیانا", "ماکسیما", "قشقایی", "اکس‌تریل", "پاترول", "جوک", "سانترا" },
        ["Mitsubishi"] = new[] { "لنسر", "اوتلندر", "ای‌اس‌ایکس", "پاجرو", "گالانت" },
        ["Mazda"] = new[] { "مازدا ۳", "مازدا ۲", "سی‌اکس۵", "سی‌اکس۳", "مازدا ۶" },
        ["Honda"] = new[] { "سیویک", "آکورد", "سی‌آر‌وی", "فیت", "سیتی" },
        ["Suzuki"] = new[] { "ویتارا", "گرند ویتارا", "سوئیفت", "بالنو", "سیاز" },
        ["Volkswagen"] = new[] { "گلف", "پاسات", "تیگوان", "پولو", "جتا" },
        ["BMW"] = new[] { "سری ۳", "سری ۵", "ایکس ۳", "ایکس ۵", "سری ۱" },
        ["Mercedes-Benz"] = new[] { "کلاس سی", "کلاس ای", "کلاس اس", "جی‌ال‌ای", "جی‌ال‌سی" },
        ["ایران خودرو"] = new[] { "سمند", "سورن", "پارس", "دنا", "آریسان", "رانا", "تارا", "هایما", "پژو ۲۰۷i" },
        ["سایپا"] = new[] { "پراید", "تیبا", "ساینا", "کوییک", "شاهین", "اطلس", "وانت زامیاد" },
        ["کرمان موتور"] = new[] { "جک S5", "جک S3", "جک J4", "لیفان X60", "لیفان 620", "فیدلیتی" },
        ["مدیران خودرو"] = new[] { "ام‌وی‌ام X22", "ام‌وی‌ام X33", "ام‌وی‌ام 315", "آریزو 5", "آریزو 6", "تیگو 7" },
        ["بهمن موتور"] = new[] { "مازراتی" , "فیدلیتیک", "ریگان", "هاوال H2", "هاوال H6" },
        ["پارس خودرو"] = new[] { "برلیانس H230", "برلیانس H330", "رنو تندر", "نیسان قشقایی" }
    };

    private static void SeedCatalogue(AppDbContext db)
    {
        if (db.VehicleMakes.Any()) return;

        foreach (var (makeName, models) in Catalogue)
        {
            var make = new VehicleMake { Name = makeName };
            foreach (var modelName in models)
            {
                make.Models.Add(new VehicleModel { Name = modelName, Make = make });
            }
            db.VehicleMakes.Add(make);
        }
        db.SaveChanges();
    }

    private static void SeedCompany(AppDbContext db)
    {
        if (db.Companies.Any()) return;

        db.Companies.Add(new Company
        {
            Name = "شرکت خودرویی نمونه",
            Phone = "02100000000",
            PostalCode = "0000000000",
            Address = "تهران - نشانی شرکت را در بخش تنظیمات شرکت وارد کنید",
            EconomicCode = "",
            NationalId = "",
            KardexInstructionNo = "",
            NumberingInstructionNo = ""
        });
        db.SaveChanges();
    }

    private static void SeedSettings(AppDbContext db)
    {
        var options = new JsonSerializerOptions { WriteIndented = false };

        if (!db.Settings.Any(s => s.Key == SettingKeys.Print))
        {
            db.Settings.Add(new Setting { Key = SettingKeys.Print, Value = JsonSerializer.Serialize(new PrintSettings(), options) });
        }
        if (!db.Settings.Any(s => s.Key == SettingKeys.App))
        {
            var appSettings = new AppSettings { DefaultInvoiceItems = AppSettings.CreateDefaultInvoiceItems() };
            db.Settings.Add(new Setting { Key = SettingKeys.App, Value = JsonSerializer.Serialize(appSettings, options) });
        }
        db.SaveChanges();
    }

    /// <summary>Adds a column when an older database file is missing it (safe, idempotent).</summary>
    private static void EnsureColumn(AppDbContext db, string table, string column, string type)
    {
        try
        {
            if (ColumnExists(db, table, column)) return;
#pragma warning disable EF1002 // identifiers are internal constants, never user input
            db.Database.ExecuteSqlRaw($"ALTER TABLE \"{table}\" ADD COLUMN \"{column}\" {type} NULL");
#pragma warning restore EF1002
        }
        catch
        {
            // Never block start-up because of a best-effort schema tweak.
        }
    }

    private static bool ColumnExists(AppDbContext db, string table, string column)
    {
        var connection = db.Database.GetDbConnection();
        var openedHere = connection.State != System.Data.ConnectionState.Open;
        if (openedHere) connection.Open();
        try
        {
            using var command = connection.CreateCommand();
            command.CommandText = $"PRAGMA table_info(\"{table}\")";
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                if (string.Equals(reader["name"]?.ToString(), column, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }
        finally
        {
            if (openedHere) connection.Close();
        }
    }

    #endregion
}

public static class SettingKeys
{
    public const string Print = "PrintSettings";
    public const string App = "AppSettings";
}
