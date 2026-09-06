using System.Printing;

namespace LabelFlow.Services;

public static class PrinterService
{
    public static IReadOnlyList<string> GetInstalledPrinterNames()
    {
        try
        {
            using var server = new LocalPrintServer();
            return server.GetPrintQueues()
                .Select(q => q.Name)
                .OrderBy(n => n)
                .ToList();
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    public static PrintQueue? FindQueue(string? printerName)
    {
        if (string.IsNullOrWhiteSpace(printerName))
            return null;

        try
        {
            using var server = new LocalPrintServer();
            return server.GetPrintQueues()
                .FirstOrDefault(q =>
                    string.Equals(q.Name, printerName, StringComparison.OrdinalIgnoreCase));
        }
        catch
        {
            return null;
        }
    }

    public static string? GetDefaultPrinterName()
    {
        try
        {
            return new LocalPrintServer().DefaultPrintQueue?.Name;
        }
        catch
        {
            return null;
        }
    }
}
