using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using CarForm.Core.Helpers;

namespace CarForm.Printing;

/// <summary>
/// One fill-in box of the printed form. Coordinates are NORMALISED (0..1) against the template
/// image, so the layout is independent of paper size and image resolution.
/// </summary>
public sealed class FieldBox
{
    public string Field { get; set; } = string.Empty;
    public double X { get; set; }
    public double Y { get; set; }
    public double W { get; set; }
    public double H { get; set; }
    public string Align { get; set; } = "Right";
    public double FontScale { get; set; } = 1.0;
    public bool Wrap { get; set; } = true;

    /// <summary>Box in absolute coordinates for a template image of the given size.</summary>
    public (double X, double Y, double Width, double Height) ToRect(double imageWidth, double imageHeight)
        => (X * imageWidth, Y * imageHeight, W * imageWidth, H * imageHeight);
}

public static class FormFields
{
    // header / seller
    public const string DocumentNumber = "DocumentNumber";
    public const string DocumentDate = "DocumentDate";
    public const string CompanyName = "CompanyName";
    public const string CompanyAddress = "CompanyAddress";
    public const string CompanyPhone = "CompanyPhone";
    public const string KardexInstructionNo = "KardexInstructionNo";
    public const string NumberingInstructionNo = "NumberingInstructionNo";

    // parties
    public const string BuyerName = "BuyerName";
    public const string BuyerPosition = "BuyerPosition";
    public const string SecondBuyerName = "SecondBuyerName";
    public const string SecondBuyerPosition = "SecondBuyerPosition";

    // owner block
    public const string OwnerNameBlock = "OwnerNameBlock";
    public const string BuyerFatherName = "BuyerFatherName";
    public const string BuyerIdNumber = "BuyerIdNumber";
    public const string BuyerNationalCode = "BuyerNationalCode";
    public const string BuyerBirthDate = "BuyerBirthDate";
    public const string BuyerBirthPlace = "BuyerBirthPlace";
    public const string BuyerIssueDate = "BuyerIssueDate";
    public const string BuyerIssuePlace = "BuyerIssuePlace";
    public const string BuyerJob = "BuyerJob";
    public const string BuyerMobile = "BuyerMobile";
    public const string BuyerPhone = "BuyerPhone";
    public const string BuyerPostalCode = "BuyerPostalCode";
    public const string BuyerHomeAddress = "BuyerHomeAddress";
    public const string BuyerWorkAddress = "BuyerWorkAddress";
    public const string BuyerAddress = "BuyerAddress";

    // vehicle (the 16 fields of the official form)
    public const string VehicleUsageType = "VehicleUsageType";
    public const string VehicleSystem = "VehicleSystem";
    public const string VehicleTrim = "VehicleTrim";
    public const string VehicleModel = "VehicleModel";
    public const string VehicleFuel = "VehicleFuel";
    public const string VehicleVin = "VehicleVin";
    public const string VehicleCylinders = "VehicleCylinders";
    public const string VehicleAxles = "VehicleAxles";
    public const string VehicleWheels = "VehicleWheels";
    public const string VehicleCapacity = "VehicleCapacity";
    public const string VehicleColor = "VehicleColor";
    public const string VehicleEngineNo = "VehicleEngineNo";
    public const string VehicleChassisNo = "VehicleChassisNo";
    public const string VehicleCabinNo = "VehicleCabinNo";
    public const string VehicleDisplacement = "VehicleDisplacement";
    public const string VehicleCountry = "VehicleCountry";

    // extra vehicle data (kept in the model, printed only if you add a box for it)
    public const string VehicleMake = "VehicleMake";
    public const string VehicleYear = "VehicleYear";
    public const string VehiclePlate = "VehiclePlate";
    public const string VehicleMileage = "VehicleMileage";

    // receipts -------------------------------------------------
    // 1. customs
    public const string CustomsPermitNo = "CustomsPermitNo";
    public const string CustomsLicenseNo = "CustomsLicenseNo";
    public const string CustomsOffice = "CustomsOffice";
    public const string ClearanceDate = "ClearanceDate";
    // 2. third party insurance
    public const string InsurancePolicyNo = "InsurancePolicyNo";
    public const string InsuranceIssueDate = "InsuranceIssueDate";
    public const string InsuranceCompany = "InsuranceCompany";
    public const string InsuranceExpiryDate = "InsuranceExpiryDate";
    // 3. toll (عوارض شهرداری)
    public const string TollReceiptNo = "TollReceiptNo";
    public const string TollReceiptDate = "TollReceiptDate";
    public const string TollBankBranch = "TollBankBranch";
    // 4. tax (مالیات دارایی)
    public const string TaxReceiptNo = "TaxReceiptNo";
    public const string TaxReceiptDate = "TaxReceiptDate";
    public const string TaxBankBranch = "TaxBankBranch";

    public const string NumberingPermitNo = "NumberingPermitNo";

    // invoice / notes
    public const string InvoiceTotal = "InvoiceTotal";
    public const string Notes = "Notes";
}

public static class FormLayout
{
    /// <summary>Pixel size of the template image the default coordinates were measured on.</summary>
    public const double TemplateWidth = 576;
    public const double TemplateHeight = 760;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static string FilePath => Path.Combine(AppPaths.AppDataFolder, "form-layout.json");

    private static FieldBox Box(string field, double x, double y, double w, double h, string align = "Right", double fontScale = 1.0, bool wrap = true)
        => new()
        {
            Field = field,
            X = Math.Round(x / TemplateWidth, 5),
            Y = Math.Round(y / TemplateHeight, 5),
            W = Math.Round(w / TemplateWidth, 5),
            H = Math.Round(h / TemplateHeight, 5),
            Align = align,
            FontScale = fontScale,
            Wrap = wrap
        };

    /// <summary>
    /// Default boxes measured on the official form:
    /// vehicle block = 5 rows x 3 columns (right to left) + a wide VIN row,
    /// owner block = 5 rows x 3 columns, receipts = 4 groups x 4 rows.
    /// </summary>
    public static List<FieldBox> CreateDefaults() => new()
    {
        // ---------- header ----------
        Box(FormFields.DocumentNumber, 36, 8, 118, 20),
        Box(FormFields.DocumentDate, 400, 6, 165, 22),

        // ---------- seller / company ----------
        Box(FormFields.KardexInstructionNo, 30, 32, 86, 16),
        Box(FormFields.NumberingInstructionNo, 30, 62, 78, 14),
        Box(FormFields.CompanyAddress, 235, 42, 298, 19),
        Box(FormFields.CompanyPhone, 235, 56, 298, 19),

        // ---------- parties (نام و نام خانوادگی / سمت / مهر و امضا) ----------
        Box(FormFields.BuyerName, 378, 167, 124, 19),
        Box(FormFields.BuyerPosition, 178, 167, 178, 19),
        Box(FormFields.SecondBuyerName, 378, 186, 124, 18),
        Box(FormFields.SecondBuyerPosition, 178, 186, 178, 18),

        // ---------- vehicle block (5 rows x 3 columns, RTL) ----------
        // row 1
        Box(FormFields.VehicleUsageType, 332, 216, 80, 14),
        Box(FormFields.VehicleCylinders, 220, 216, 66, 14),
        Box(FormFields.VehicleEngineNo, 40, 216, 130, 14),
        // row 2
        Box(FormFields.VehicleSystem, 332, 229, 102, 14),
        Box(FormFields.VehicleAxles, 220, 229, 68, 14),
        Box(FormFields.VehicleChassisNo, 40, 229, 134, 14),
        // row 3
        Box(FormFields.VehicleTrim, 332, 242, 110, 13),
        Box(FormFields.VehicleWheels, 220, 242, 75, 13),
        Box(FormFields.VehicleCabinNo, 40, 242, 136, 13),
        // row 4
        Box(FormFields.VehicleModel, 330, 252, 107, 11),
        Box(FormFields.VehicleCapacity, 219, 252, 82, 11),
        Box(FormFields.VehicleDisplacement, 30, 252, 144, 11),
        // row 5
        Box(FormFields.VehicleFuel, 330, 263, 88, 11),
        Box(FormFields.VehicleColor, 227, 263, 84, 11),
        Box(FormFields.VehicleCountry, 30, 263, 124, 11),
        // row 6 - VIN
        Box(FormFields.VehicleVin, 40, 276, 328, 14),

        // ---------- owner block (5 rows x 3 columns, RTL) ----------
        Box(FormFields.OwnerNameBlock, 380, 330, 84, 14, "Right", 0.95),
        Box(FormFields.BuyerFatherName, 220, 330, 130, 14, "Right", 0.95),
        Box(FormFields.BuyerIdNumber, 30, 330, 118, 14, "Right", 0.95),

        Box(FormFields.BuyerBirthDate, 380, 345, 126, 10, "Right", 0.95),
        Box(FormFields.BuyerBirthPlace, 220, 345, 123, 10, "Right", 0.95),
        Box(FormFields.BuyerIssuePlace, 30, 345, 133, 10, "Right", 0.95),

        Box(FormFields.BuyerIssueDate, 386, 355, 120, 8, "Right", 0.9),
        Box(FormFields.BuyerNationalCode, 220, 355, 118, 8, "Right", 0.9),
        Box(FormFields.BuyerPostalCode, 30, 355, 152, 8, "Right", 0.9),

        Box(FormFields.BuyerHomeAddress, 220, 364, 239, 9, "Right", 0.9),
        Box(FormFields.BuyerPhone, 30, 364, 149, 9, "Right", 0.9),

        Box(FormFields.BuyerWorkAddress, 220, 374, 242, 10, "Right", 0.9),
        Box(FormFields.BuyerMobile, 30, 374, 149, 10, "Right", 0.9),

        // ---------- receipts: 4 groups (customs | insurance | toll | tax) ----------
        Box(FormFields.CustomsPermitNo, 440, 408, 52, 12, "Right", 0.95),
        Box(FormFields.CustomsLicenseNo, 440, 419, 52, 9, "Right", 0.95),
        Box(FormFields.CustomsOffice, 440, 427, 52, 9, "Right", 0.95),
        Box(FormFields.ClearanceDate, 440, 435, 52, 9, "Right", 0.95),

        Box(FormFields.InsuranceIssueDate, 314, 419, 70, 9, "Right", 0.95),
        Box(FormFields.InsuranceCompany, 314, 427, 64, 9, "Right", 0.95),
        Box(FormFields.InsurancePolicyNo, 314, 435, 96, 9, "Right", 0.95),

        Box(FormFields.TollReceiptNo, 188, 419, 71, 9, "Right", 0.95),
        Box(FormFields.TollReceiptDate, 188, 427, 71, 9, "Right", 0.95),
        Box(FormFields.TollBankBranch, 188, 435, 71, 9, "Right", 0.95),

        Box(FormFields.TaxReceiptNo, 32, 419, 96, 9, "Right", 0.95),
        Box(FormFields.TaxReceiptDate, 32, 427, 96, 9, "Right", 0.95),
        Box(FormFields.TaxBankBranch, 32, 435, 96, 9, "Right", 0.95),

        // ---------- totals / notes (empty area of the template) ----------
        Box(FormFields.InvoiceTotal, 320, 486, 220, 18),
        Box(FormFields.Notes, 40, 492, 496, 46, "Right", 0.95)
    };

    /// <summary>Loads the layout, creating the JSON file on first use.</summary>
    public static List<FieldBox> Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                var boxes = JsonSerializer.Deserialize<List<FieldBox>>(File.ReadAllText(FilePath), JsonOptions);
                if (boxes is { Count: > 0 })
                {
                    var known = new HashSet<string>(boxes.Select(b => b.Field), StringComparer.OrdinalIgnoreCase);
                    foreach (var missing in CreateDefaults().Where(b => !known.Contains(b.Field)))
                    {
                        boxes.Add(missing);
                    }
                    return boxes;
                }
            }
        }
        catch
        {
            // fall back to defaults
        }

        var defaults = CreateDefaults();
        Save(defaults);
        return defaults;
    }

    public static void Save(List<FieldBox> boxes)
    {
        try
        {
            AppPaths.EnsureFolders();
            File.WriteAllText(FilePath, JsonSerializer.Serialize(boxes, JsonOptions));
        }
        catch
        {
            // never break printing because the layout file could not be written
        }
    }

    public static List<FieldBox> Reset()
    {
        var defaults = CreateDefaults();
        Save(defaults);
        return defaults;
    }
}
