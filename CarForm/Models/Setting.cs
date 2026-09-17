namespace CarForm.Models;

/// <summary>Simple key/value store for application and print settings (single-user app).</summary>
public class Setting
{
    public string Key { get; set; } = string.Empty;

    public string? Value { get; set; }
}
