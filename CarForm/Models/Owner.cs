using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarForm.Models;

/// <summary>
/// Buyer / seller party of a document. Both a real person and a legal entity are stored in one
/// table; <see cref="Type"/> decides which group of fields is used by the UI and the printed form.
/// </summary>
public class Owner : EntityBase
{
    private OwnerType _type = OwnerType.Individual;

    // ---- individual ----
    private string? _firstName;
    private string? _lastName;
    private string? _fatherName;
    private string? _nationalCode;
    private string? _idNumber;
    private string? _issuePlace;
    private DateTime? _issueDate;
    private string? _birthPlace;
    private string? _workAddress;
    private DateTime? _birthDate;

    // ---- legal entity ----
    private string? _companyName;
    private string? _economicCode;
    private string? _companyNationalId;
    private string? _registrationNumber;
    private string? _representativeName;
    private string? _representativeNationalCode;

    // ---- common ----
    private string? _phone;
    private string? _mobile;
    private string? _address;
    private string? _postalCode;
    private string? _email;
    private string? _jobTitle;
    private string? _notes;
    private string? _imagePath;
    private DateTime _createdAt;
    private DateTime _updatedAt;

    public OwnerType Type { get => _type; set => SetProperty(ref _type, value); }

    #region Individual

    public string? FirstName { get => _firstName; set => SetProperty(ref _firstName, value); }

    public string? LastName { get => _lastName; set => SetProperty(ref _lastName, value); }

    public string? FatherName { get => _fatherName; set => SetProperty(ref _fatherName, value); }

    /// <summary>کد ملی</summary>
    public string? NationalCode { get => _nationalCode; set => SetProperty(ref _nationalCode, value); }

    /// <summary>شماره شناسنامه</summary>
    public string? IdNumber { get => _idNumber; set => SetProperty(ref _idNumber, value); }

    /// <summary>محل صدور</summary>
    /// <summary>محل صدور / محل ثبت</summary>
    public string? IssuePlace { get => _issuePlace; set => SetProperty(ref _issuePlace, value); }

    /// <summary>تاریخ صدور</summary>
    public DateTime? IssueDate { get => _issueDate; set => SetProperty(ref _issueDate, value); }

    /// <summary>محل تولد</summary>
    public string? BirthPlace { get => _birthPlace; set => SetProperty(ref _birthPlace, value); }

    /// <summary>آدرس محل فعالیت (مالک حقوقی)</summary>
    public string? WorkAddress { get => _workAddress; set => SetProperty(ref _workAddress, value); }

    public DateTime? BirthDate { get => _birthDate; set => SetProperty(ref _birthDate, value); }

    #endregion

    #region Legal entity

    public string? CompanyName { get => _companyName; set => SetProperty(ref _companyName, value); }

    /// <summary>کد اقتصادی</summary>
    public string? EconomicCode { get => _economicCode; set => SetProperty(ref _economicCode, value); }

    /// <summary>شناسه ملی شرکت</summary>
    public string? CompanyNationalId { get => _companyNationalId; set => SetProperty(ref _companyNationalId, value); }

    /// <summary>شماره ثبت</summary>
    public string? RegistrationNumber { get => _registrationNumber; set => SetProperty(ref _registrationNumber, value); }

    public string? RepresentativeName { get => _representativeName; set => SetProperty(ref _representativeName, value); }

    public string? RepresentativeNationalCode { get => _representativeNationalCode; set => SetProperty(ref _representativeNationalCode, value); }

    #endregion

    #region Common

    public string? Phone { get => _phone; set => SetProperty(ref _phone, value); }

    public string? Mobile { get => _mobile; set => SetProperty(ref _mobile, value); }

    public string? Address { get => _address; set => SetProperty(ref _address, value); }

    public string? PostalCode { get => _postalCode; set => SetProperty(ref _postalCode, value); }

    public string? Email { get => _email; set => SetProperty(ref _email, value); }

    public string? JobTitle { get => _jobTitle; set => SetProperty(ref _jobTitle, value); }

    public string? Notes { get => _notes; set => SetProperty(ref _notes, value); }

    /// <summary>Photo (individual) or logo (legal entity). Relative to the application data folder.</summary>
    public string? ImagePath { get => _imagePath; set => SetProperty(ref _imagePath, value); }

    #endregion

    public DateTime CreatedAt { get => _createdAt; set => SetProperty(ref _createdAt, value); }

    public DateTime UpdatedAt { get => _updatedAt; set => SetProperty(ref _updatedAt, value); }

    [NotMapped]
    /// <summary>Display name used in lists, documents and the print layout.</summary>
    public string DisplayName => Type == OwnerType.LegalEntity
        ? (string.IsNullOrWhiteSpace(CompanyName) ? "شخص حقوقی بدون نام" : CompanyName!)
        : $"{FirstName} {LastName}".Trim();

    [NotMapped]
    public string NationalOrEconomicCode => Type == OwnerType.LegalEntity
        ? (CompanyNationalId ?? EconomicCode ?? string.Empty)
        : (NationalCode ?? string.Empty);
}
