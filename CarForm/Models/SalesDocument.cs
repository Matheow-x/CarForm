using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;
using System.Linq;

namespace CarForm.Models;

/// <summary>
/// A car sales document / sales invoice.
/// The document is a self-contained snapshot: every field that appears on the printed form is copied
/// here when a vehicle / owner is selected, and may still be edited afterwards inside the wizard.
/// </summary>
public class SalesDocument : EntityBase
{
    // ---- header ----
    private string _documentNumber = string.Empty;
    private DateTime _documentDate = DateTime.Today;

    // ---- source records (optional, kept for traceability) ----
    private int? _vehicleId;
    private int? _ownerId;

    // ---- vehicle snapshot ----
    private string? _vehicleMake;
    private string? _vehicleModel;
    private string? _vehicleTrim;
    private string? _vehicleYear;
    private string? _vehicleColor;
    private string? _vehicleVin;
    private string? _vehicleChassisNo;
    private string? _vehicleEngineNo;
    private string? _vehiclePlate;
    private string? _vehicleFuelType;
    private string? _vehicleBodyType;
    private string? _vehicleSystem;
    private string? _vehicleUsageType;
    private string? _vehicleCylinders;
    private string? _vehicleAxles;
    private string? _vehicleWheels;
    private string? _vehicleCabinNo;
    private string? _vehicleCapacity;
    private string? _vehicleDisplacement;
    private string? _vehicleCountry;
    private long? _vehicleMileage;

    // ---- buyer snapshot ----
    private OwnerType _buyerType = OwnerType.Individual;
    private string? _buyerName;
    private string? _buyerNationalCode;
    private string? _buyerIdNumber;
    private string? _buyerFatherName;
    private string? _buyerPhone;
    private string? _buyerMobile;
    private string? _buyerAddress;
    private string? _buyerPostalCode;
    private string? _buyerEconomicCode;
    private string? _buyerRegistrationNumber;
    private string? _buyerRepresentativeName;
    private string? _buyerPosition;
    private string? _secondBuyerName;
    private string? _secondBuyerPosition;
    private string? _buyerIssuePlace;
    private DateTime? _buyerIssueDate;
    private string? _buyerBirthPlace;
    private string? _buyerHomeAddress;
    private string? _buyerWorkAddress;
    private string? _buyerJob;
    private DateTime? _buyerBirthDate;

    // ---- tax / toll receipts ----
    private string? _taxReceiptNo;
    private DateTime? _taxReceiptDate;
    private string? _taxPaymentId;

    private string? _tollReceiptNo;
    private DateTime? _tollReceiptDate;
    private string? _tollPaymentId;

    // ---- third party insurance ----
    private string? _insurancePolicyNo;
    private string? _insuranceCompany;
    private DateTime? _insuranceIssueDate;
    private DateTime? _insuranceExpiryDate;
    private string? _numberingPermitNo;
    private string? _customsPermitNo;
    private string? _customsLicenseNo;
    private string? _customsOffice;
    private string? _taxBankBranch;
    private string? _tollBankBranch;
    private DateTime? _clearanceDate;

    private string? _notes;
    private bool? _noteColorChanged;
    private bool? _noteHasCabin;
    private bool? _noteHasDuplicateKey;
    private bool? _noteIsUsed;
    private DateTime _createdAt;
    private DateTime _updatedAt;

    #region Header

    public string DocumentNumber
    {
        get => _documentNumber;
        set => SetProperty(ref _documentNumber, value);
    }

    /// <summary>Always stored as a Gregorian date; the UI edits it as Jalali.</summary>
    public DateTime DocumentDate
    {
        get => _documentDate;
        set => SetProperty(ref _documentDate, value);
    }

    public int? VehicleId { get => _vehicleId; set => SetProperty(ref _vehicleId, value); }

    public int? OwnerId { get => _ownerId; set => SetProperty(ref _ownerId, value); }

    #endregion

    #region Vehicle snapshot

    public string? VehicleMake { get => _vehicleMake; set => SetProperty(ref _vehicleMake, value); }

    public string? VehicleModel { get => _vehicleModel; set => SetProperty(ref _vehicleModel, value); }

    public string? VehicleTrim { get => _vehicleTrim; set => SetProperty(ref _vehicleTrim, value); }

    public string? VehicleYear { get => _vehicleYear; set => SetProperty(ref _vehicleYear, value); }

    public string? VehicleColor { get => _vehicleColor; set => SetProperty(ref _vehicleColor, value); }

    public string? VehicleVin { get => _vehicleVin; set => SetProperty(ref _vehicleVin, value); }

    public string? VehicleChassisNo { get => _vehicleChassisNo; set => SetProperty(ref _vehicleChassisNo, value); }

    public string? VehicleEngineNo { get => _vehicleEngineNo; set => SetProperty(ref _vehicleEngineNo, value); }

    public string? VehiclePlate { get => _vehiclePlate; set => SetProperty(ref _vehiclePlate, value); }

    public string? VehicleFuelType { get => _vehicleFuelType; set => SetProperty(ref _vehicleFuelType, value); }

    public string? VehicleBodyType { get => _vehicleBodyType; set => SetProperty(ref _vehicleBodyType, value); }

    /// <summary>سیستم (فنی) خودرو</summary>
    public string? VehicleSystem { get => _vehicleSystem; set => SetProperty(ref _vehicleSystem, value); }

    /// <summary>نوع کاربری</summary>
    public string? VehicleUsageType { get => _vehicleUsageType; set => SetProperty(ref _vehicleUsageType, value); }

    /// <summary>تعداد سیلندر</summary>
    public string? VehicleCylinders { get => _vehicleCylinders; set => SetProperty(ref _vehicleCylinders, value); }

    /// <summary>تعداد محور</summary>
    public string? VehicleAxles { get => _vehicleAxles; set => SetProperty(ref _vehicleAxles, value); }

    /// <summary>تعداد چرخ</summary>
    public string? VehicleWheels { get => _vehicleWheels; set => SetProperty(ref _vehicleWheels, value); }

    /// <summary>شماره اتاق</summary>
    public string? VehicleCabinNo { get => _vehicleCabinNo; set => SetProperty(ref _vehicleCabinNo, value); }

    /// <summary>ظرفیت</summary>
    public string? VehicleCapacity { get => _vehicleCapacity; set => SetProperty(ref _vehicleCapacity, value); }

    /// <summary>حجم سیلندر</summary>
    public string? VehicleDisplacement { get => _vehicleDisplacement; set => SetProperty(ref _vehicleDisplacement, value); }

    /// <summary>کشور سازنده</summary>
    public string? VehicleCountry { get => _vehicleCountry; set => SetProperty(ref _vehicleCountry, value); }

    public long? VehicleMileage { get => _vehicleMileage; set => SetProperty(ref _vehicleMileage, value); }

    [NotMapped]
    public string VehicleTitle => $"{VehicleMake} {VehicleModel} {VehicleTrim}".Trim();

    #endregion

    #region Buyer snapshot

    public OwnerType BuyerType
    {
        get => _buyerType;
        set => SetProperty(ref _buyerType, value);
    }

    public string? BuyerName { get => _buyerName; set => SetProperty(ref _buyerName, value); }

    public string? BuyerNationalCode { get => _buyerNationalCode; set => SetProperty(ref _buyerNationalCode, value); }

    public string? BuyerIdNumber { get => _buyerIdNumber; set => SetProperty(ref _buyerIdNumber, value); }

    public string? BuyerFatherName { get => _buyerFatherName; set => SetProperty(ref _buyerFatherName, value); }

    public string? BuyerPhone { get => _buyerPhone; set => SetProperty(ref _buyerPhone, value); }

    public string? BuyerMobile { get => _buyerMobile; set => SetProperty(ref _buyerMobile, value); }

    public string? BuyerAddress { get => _buyerAddress; set => SetProperty(ref _buyerAddress, value); }

    public string? BuyerPostalCode { get => _buyerPostalCode; set => SetProperty(ref _buyerPostalCode, value); }

    public string? BuyerEconomicCode { get => _buyerEconomicCode; set => SetProperty(ref _buyerEconomicCode, value); }

    public string? BuyerRegistrationNumber { get => _buyerRegistrationNumber; set => SetProperty(ref _buyerRegistrationNumber, value); }

    public string? BuyerRepresentativeName { get => _buyerRepresentativeName; set => SetProperty(ref _buyerRepresentativeName, value); }

    /// <summary>سمت خریدار</summary>
    public string? BuyerPosition { get => _buyerPosition; set => SetProperty(ref _buyerPosition, value); }

    /// <summary>نام و نام خانوادگی نفر دوم فرم</summary>
    public string? SecondBuyerName { get => _secondBuyerName; set => SetProperty(ref _secondBuyerName, value); }

    /// <summary>سمت نفر دوم</summary>
    public string? SecondBuyerPosition { get => _secondBuyerPosition; set => SetProperty(ref _secondBuyerPosition, value); }

    /// <summary>محل صدور/ثبت</summary>
    public string? BuyerIssuePlace { get => _buyerIssuePlace; set => SetProperty(ref _buyerIssuePlace, value); }

    /// <summary>تاریخ صدور</summary>
    public DateTime? BuyerIssueDate { get => _buyerIssueDate; set => SetProperty(ref _buyerIssueDate, value); }

    /// <summary>محل تولد</summary>
    public string? BuyerBirthPlace { get => _buyerBirthPlace; set => SetProperty(ref _buyerBirthPlace, value); }

    /// <summary>آدرس محل سکونت (مالک حقیقی)</summary>
    public string? BuyerHomeAddress { get => _buyerHomeAddress; set => SetProperty(ref _buyerHomeAddress, value); }

    /// <summary>آدرس محل فعالیت (مالک حقوقی)</summary>
    public string? BuyerWorkAddress { get => _buyerWorkAddress; set => SetProperty(ref _buyerWorkAddress, value); }

    /// <summary>شغل خریدار</summary>
    public string? BuyerJob { get => _buyerJob; set => SetProperty(ref _buyerJob, value); }

    public DateTime? BuyerBirthDate { get => _buyerBirthDate; set => SetProperty(ref _buyerBirthDate, value); }

    #endregion

    #region Receipts and insurance

    /// <summary>شماره فیش مالیات و عوارض</summary>
    public string? TaxReceiptNo { get => _taxReceiptNo; set => SetProperty(ref _taxReceiptNo, value); }

    public DateTime? TaxReceiptDate { get => _taxReceiptDate; set => SetProperty(ref _taxReceiptDate, value); }

    /// <summary>شناسه پرداخت مالیات</summary>
    public string? TaxPaymentId { get => _taxPaymentId; set => SetProperty(ref _taxPaymentId, value); }

    /// <summary>شماره فیش عوارض</summary>
    public string? TollReceiptNo { get => _tollReceiptNo; set => SetProperty(ref _tollReceiptNo, value); }

    public DateTime? TollReceiptDate { get => _tollReceiptDate; set => SetProperty(ref _tollReceiptDate, value); }

    /// <summary>شناسه پرداخت عوارض</summary>
    public string? TollPaymentId { get => _tollPaymentId; set => SetProperty(ref _tollPaymentId, value); }

    /// <summary>شماره بیمه‌نامه شخص ثالث</summary>
    public string? InsurancePolicyNo { get => _insurancePolicyNo; set => SetProperty(ref _insurancePolicyNo, value); }

    public string? InsuranceCompany { get => _insuranceCompany; set => SetProperty(ref _insuranceCompany, value); }

    public DateTime? InsuranceIssueDate { get => _insuranceIssueDate; set => SetProperty(ref _insuranceIssueDate, value); }

    public DateTime? InsuranceExpiryDate { get => _insuranceExpiryDate; set => SetProperty(ref _insuranceExpiryDate, value); }

    /// <summary>شماره پروانه شماره‌گذاری</summary>
    public string? NumberingPermitNo { get => _numberingPermitNo; set => SetProperty(ref _numberingPermitNo, value); }

    /// <summary>شماره پروانه گمرکی</summary>
    public string? CustomsPermitNo { get => _customsPermitNo; set => SetProperty(ref _customsPermitNo, value); }

    /// <summary>شماره گواهینامه گمرکی</summary>
    public string? CustomsLicenseNo { get => _customsLicenseNo; set => SetProperty(ref _customsLicenseNo, value); }

    /// <summary>گمرک ترخیص</summary>
    public string? CustomsOffice { get => _customsOffice; set => SetProperty(ref _customsOffice, value); }

    /// <summary>بانک / شعبه (فیش مالیات دارایی)</summary>
    public string? TaxBankBranch { get => _taxBankBranch; set => SetProperty(ref _taxBankBranch, value); }

    /// <summary>بانک / شعبه (فیش عوارض شهرداری)</summary>
    public string? TollBankBranch { get => _tollBankBranch; set => SetProperty(ref _tollBankBranch, value); }

    /// <summary>تاریخ ترخیص</summary>
    public DateTime? ClearanceDate { get => _clearanceDate; set => SetProperty(ref _clearanceDate, value); }

    #endregion

    /// <summary>Free text typed by the user (never overwritten by the predefined note options).</summary>
    public string? Notes { get => _notes; set => SetProperty(ref _notes, value); }

    /// <summary>رنگ خودرو تغییر کرده است؟</summary>
    public bool? NoteColorChanged
    {
        get => _noteColorChanged;
        set => SetProperty(ref _noteColorChanged, value, new[] { nameof(CombinedNotes) });
    }

    /// <summary>دارای اتاق / کابین؟</summary>
    public bool? NoteHasCabin
    {
        get => _noteHasCabin;
        set => SetProperty(ref _noteHasCabin, value, new[] { nameof(CombinedNotes) });
    }

    /// <summary>دارای سوئیچ یدک؟</summary>
    public bool? NoteHasDuplicateKey
    {
        get => _noteHasDuplicateKey;
        set => SetProperty(ref _noteHasDuplicateKey, value, new[] { nameof(CombinedNotes) });
    }

    /// <summary>کارکرده (true) یا صفر کیلومتر (false)؟</summary>
    public bool? NoteIsUsed
    {
        get => _noteIsUsed;
        set => SetProperty(ref _noteIsUsed, value, new[] { nameof(CombinedNotes) });
    }

    [NotMapped]
    /// <summary>Notes actually printed: generated lines from the predefined options + the manual text.</summary>
    public string CombinedNotes
    {
        get
        {
            var lines = new List<string>();
            if (NoteColorChanged == true) lines.Add("رنگ خودرو تغییر یافته است.");
            else if (NoteColorChanged == false) lines.Add("رنگ خودرو تغییر نیافته است.");

            if (NoteHasCabin == true) lines.Add("خودرو دارای اتاق (کابین) می‌باشد.");
            else if (NoteHasCabin == false) lines.Add("خودرو فاقد اتاق (کابین) می‌باشد.");

            if (NoteHasDuplicateKey == true) lines.Add("خودرو دارای سوئیچ یدک می‌باشد.");
            else if (NoteHasDuplicateKey == false) lines.Add("خودرو فاقد سوئیچ یدک می‌باشد.");

            if (NoteIsUsed == true)
            {
                lines.Add(VehicleMileage.HasValue
                    ? $"خودرو کارکرده با کارکرد {VehicleMileage.Value:N0} کیلومتر می‌باشد."
                    : "خودرو کارکرده می‌باشد.");
            }
            else if (NoteIsUsed == false)
            {
                lines.Add("خودرو صفر کیلومتر (بدون کارکرد) می‌باشد.");
            }

            if (!string.IsNullOrWhiteSpace(Notes))
            {
                if (lines.Count > 0) lines.Add(string.Empty);
                lines.Add(Notes!);
            }

            return string.Join(Environment.NewLine, lines);
        }
    }

    public List<DocumentLineItem> Items { get; set; } = new();

    public DateTime CreatedAt { get => _createdAt; set => SetProperty(ref _createdAt, value); }

    public DateTime UpdatedAt { get => _updatedAt; set => SetProperty(ref _updatedAt, value); }

    [NotMapped]
    public decimal TotalAmount => Items.Sum(i => i.Amount);

    [NotMapped]
    public string JalaliDocumentDate => Core.Persian.JalaliDate.Format(DocumentDate);
}
