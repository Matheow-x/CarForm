using System;
using System.Linq;
using CarForm.Core.Helpers;
using CarForm.Core.Persian;
using CarForm.Models;

namespace CarForm.Printing;

/// <summary>
/// Maps a logical form field (see <see cref="FormFields"/>) to the value that must be printed.
/// Adding a new field to the printed form only requires a new entry here and a box in the layout.
/// </summary>
public sealed class DocumentFieldResolver
{
    private readonly SalesDocument _document;
    private readonly Company _company;
    private readonly bool _persianDigits;

    public DocumentFieldResolver(SalesDocument document, Company company, bool persianDigits)
    {
        _document = document;
        _company = company;
        _persianDigits = persianDigits;
    }

    public string? Resolve(string field) => field switch
    {
        // ---- header / seller ----
        FormFields.DocumentNumber => Text(_document.DocumentNumber),
        FormFields.DocumentDate => Date(_document.DocumentDate),
        FormFields.CompanyName => Text(_company.Name),
        FormFields.CompanyAddress => Text(_company.Address),
        FormFields.CompanyPhone => Text(_company.Phone),
        FormFields.KardexInstructionNo => Text(_company.KardexInstructionNo),
        FormFields.NumberingInstructionNo => Text(_company.NumberingInstructionNo),

        // ---- parties ----
        FormFields.BuyerName => Text(_document.BuyerName),
        FormFields.BuyerPosition => Text(_document.BuyerPosition),
        FormFields.SecondBuyerName => Text(_document.SecondBuyerName),
        FormFields.SecondBuyerPosition => Text(_document.SecondBuyerPosition),

        // ---- owner details ----
        FormFields.OwnerNameBlock => Text(_document.BuyerName),
        FormFields.BuyerFatherName => Text(_document.BuyerFatherName),
        FormFields.BuyerIdNumber => Text(_document.BuyerIdNumber),
        FormFields.BuyerNationalCode => Text(_document.BuyerNationalCode),
        FormFields.BuyerBirthDate => Date(_document.BuyerBirthDate),
        FormFields.BuyerIssuePlace => Text(_document.BuyerIssuePlace),
        FormFields.BuyerIssueDate => Date(_document.BuyerIssueDate),
        FormFields.BuyerBirthPlace => Text(_document.BuyerBirthPlace),
        FormFields.BuyerHomeAddress => Text(_document.BuyerHomeAddress),
        FormFields.BuyerWorkAddress => Text(_document.BuyerWorkAddress),
        FormFields.BuyerJob => Text(_document.BuyerJob),
        FormFields.BuyerMobile => Text(_document.BuyerMobile),
        FormFields.BuyerPhone => Text(_document.BuyerPhone),
        FormFields.BuyerAddress => Text(_document.BuyerAddress),
        FormFields.BuyerPostalCode => Text(_document.BuyerPostalCode),

        // ---- vehicle ----
        FormFields.VehicleMake => Text(_document.VehicleMake),
        FormFields.VehicleModel => Text(_document.VehicleModel),
        FormFields.VehicleTrim => Text(_document.VehicleTrim),
        FormFields.VehicleYear => Text(_document.VehicleYear),
        FormFields.VehicleColor => Text(_document.VehicleColor),
        FormFields.VehicleFuel => Text(_document.VehicleFuelType),
        FormFields.VehiclePlate => Text(_document.VehiclePlate),
        FormFields.VehicleVin => Text(_document.VehicleVin),
        FormFields.VehicleEngineNo => Text(_document.VehicleEngineNo),
        FormFields.VehicleChassisNo => Text(_document.VehicleChassisNo),
        FormFields.VehicleCabinNo => Text(_document.VehicleCabinNo),
        FormFields.VehicleSystem => Text(_document.VehicleSystem),
        FormFields.VehicleUsageType => Text(_document.VehicleUsageType),
        FormFields.VehicleCylinders => Text(_document.VehicleCylinders),
        FormFields.VehicleAxles => Text(_document.VehicleAxles),
        FormFields.VehicleWheels => Text(_document.VehicleWheels),
        FormFields.VehicleCapacity => Text(_document.VehicleCapacity),
        FormFields.VehicleDisplacement => Text(_document.VehicleDisplacement),
        FormFields.VehicleCountry => Text(_document.VehicleCountry),
        FormFields.VehicleMileage => _document.VehicleMileage.HasValue ? TextFormatter.Number(_document.VehicleMileage, _persianDigits) : null,

        // ---- receipts / insurance ----
        FormFields.TaxReceiptNo => Text(_document.TaxReceiptNo),
        FormFields.TollReceiptNo => Text(_document.TollReceiptNo),
        FormFields.InsurancePolicyNo => Text(_document.InsurancePolicyNo),
        FormFields.InsuranceCompany => Text(_document.InsuranceCompany),
        FormFields.InsuranceIssueDate => Date(_document.InsuranceIssueDate),
        FormFields.NumberingPermitNo => Text(_document.NumberingPermitNo),
        FormFields.ClearanceDate => Date(_document.ClearanceDate),
        FormFields.CustomsPermitNo => Text(_document.CustomsPermitNo),
        FormFields.CustomsLicenseNo => Text(_document.CustomsLicenseNo),
        FormFields.CustomsOffice => Text(_document.CustomsOffice),
        FormFields.TaxBankBranch => Text(_document.TaxBankBranch),
        FormFields.TollBankBranch => Text(_document.TollBankBranch),

        // ---- invoice / notes ----
        FormFields.InvoiceTotal => TextFormatter.Money(_document.Items.Sum(i => i.Amount), _persianDigits),
        FormFields.Notes => Text(_document.CombinedNotes),

        _ => null
    };

    private string? Text(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private string? Date(DateTime? value) => value is null ? null : JalaliDate.Format(value, _persianDigits);
}
