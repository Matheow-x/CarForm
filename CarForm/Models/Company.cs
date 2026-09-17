using System;

namespace CarForm.Models;

/// <summary>The single company profile (issuer / seller of every document).</summary>
public class Company : EntityBase
{
    private string _name = string.Empty;
    private string? _phone;
    private string? _mobile;
    private string? _postalCode;
    private string? _address;
    private string? _economicCode;
    private string? _nationalId;
    private string? _kardexInstructionNo;
    private string? _numberingInstructionNo;
    private string? _logoPath;
    private string? _website;
    private string? _taxFileNumber;
    private DateTime _updatedAt;

    public string Name { get => _name; set => SetProperty(ref _name, value); }

    public string? Phone { get => _phone; set => SetProperty(ref _phone, value); }

    public string? Mobile { get => _mobile; set => SetProperty(ref _mobile, value); }

    public string? PostalCode { get => _postalCode; set => SetProperty(ref _postalCode, value); }

    public string? Address { get => _address; set => SetProperty(ref _address, value); }

    /// <summary>کد اقتصادی</summary>
    public string? EconomicCode { get => _economicCode; set => SetProperty(ref _economicCode, value); }

    /// <summary>شناسه ملی</summary>
    public string? NationalId { get => _nationalId; set => SetProperty(ref _nationalId, value); }

    /// <summary>شماره دستورالعمل کاردکس</summary>
    public string? KardexInstructionNo { get => _kardexInstructionNo; set => SetProperty(ref _kardexInstructionNo, value); }

    /// <summary>شماره دستورالعمل شماره‌گذاری</summary>
    public string? NumberingInstructionNo { get => _numberingInstructionNo; set => SetProperty(ref _numberingInstructionNo, value); }

    /// <summary>شماره پرونده مالیاتی</summary>
    public string? TaxFileNumber { get => _taxFileNumber; set => SetProperty(ref _taxFileNumber, value); }

    public string? Website { get => _website; set => SetProperty(ref _website, value); }

    /// <summary>Relative or absolute path of the (already resized) company logo.</summary>
    public string? LogoPath { get => _logoPath; set => SetProperty(ref _logoPath, value); }

    public DateTime UpdatedAt { get => _updatedAt; set => SetProperty(ref _updatedAt, value); }
}
