using System.Threading.Tasks;
using CarForm.Models;
using System.Windows.Documents;

namespace CarForm.Printing;

public interface IPrintService
{
    /// <summary>Builds the A4 form for the given document.</summary>
    FixedDocument BuildDocument(SalesDocument document);

    /// <summary>Opens the print preview window.</summary>
    void Preview(SalesDocument document);

    /// <summary>Sends the document to the selected printer (shows the standard print dialog).</summary>
    bool Print(SalesDocument document);

    /// <summary>Exports the document to PDF (same layout as the preview).</summary>
    Task<string?> ExportPdfAsync(SalesDocument document, string? filePath = null);
}
