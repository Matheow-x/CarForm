using System;
using CarForm.Core.Mvvm;

namespace CarForm.Models;

/// <summary>One row of the sales invoice (e.g. customs, insurance, tax, toll, services).</summary>
public class DocumentLineItem : ObservableObject
{
    private int _id;
    private int _salesDocumentId;
    private string _title = string.Empty;
    private decimal _amount;
    private int _sortOrder;

    public int Id { get => _id; set => SetProperty(ref _id, value); }

    public int SalesDocumentId { get => _salesDocumentId; set => SetProperty(ref _salesDocumentId, value); }

    public string Title { get => _title; set => SetProperty(ref _title, value); }

    public decimal Amount { get => _amount; set => SetProperty(ref _amount, value); }

    public int SortOrder { get => _sortOrder; set => SetProperty(ref _sortOrder, value); }

    public SalesDocument? Document { get; set; }
}
