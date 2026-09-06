using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using LabelFlow.Models;

namespace LabelFlow.Services;

public static class LabelPrintService
{
    public static IReadOnlyList<Article> ExpandQuantities(IEnumerable<PrintItem> items)
    {
        var result = new List<Article>();
        foreach (var item in items)
        {
            if (item.Quantity <= 0 || item.Article is null) continue;
            for (int i = 0; i < item.Quantity; i++)
                result.Add(item.Article);
        }
        return result;
    }

    public static FixedDocument BuildDocument(IReadOnlyList<Article> articles, ConnectionConfig config)
    {
        double pageW = LabelRenderService.MmToPx(config.LabelWidthMm);
        double pageH = LabelRenderService.MmToPx(config.LabelHeightMm);
        var document = new FixedDocument();
        document.DocumentPaginator.PageSize = new Size(pageW, pageH);

        foreach (var article in articles)
        {
            var label = LabelRenderService.CreateLabel(article, config);
            var pageContent = new PageContent();
            var fixedPage = new FixedPage { Width = pageW, Height = pageH, Background = Brushes.White };
            FixedPage.SetLeft(label, 0);
            FixedPage.SetTop(label, 0);
            fixedPage.Children.Add(label);
            ((System.Windows.Markup.IAddChild)pageContent).AddChild(fixedPage);
            document.Pages.Add(pageContent);
        }
        return document;
    }

    public static FixedDocument BuildDocument(IEnumerable<PrintItem> items, ConnectionConfig config)
        => BuildDocument(ExpandQuantities(items), config);

    public static bool Print(IReadOnlyList<Article> articles, ConnectionConfig config, bool showDialog = true, string description = "LabelFlow")
    {
        if (articles.Count == 0) return false;

        var dialog = new PrintDialog();
        var preferred = PrinterService.FindQueue(config.PreferredPrinterName);
        if (preferred is not null) dialog.PrintQueue = preferred;

        try
        {
            dialog.PrintTicket.PageMediaSize = new PageMediaSize(
                LabelRenderService.MmToPx(config.LabelWidthMm),
                LabelRenderService.MmToPx(config.LabelHeightMm));
        }
        catch { }

        bool needDialog = showDialog || preferred is null;
        if (needDialog && dialog.ShowDialog() != true) return false;

        dialog.PrintDocument(BuildDocument(articles, config).DocumentPaginator, description);
        return true;
    }

    public static bool Print(IEnumerable<PrintItem> items, ConnectionConfig config, bool showDialog = true)
    {
        var expanded = ExpandQuantities(items);
        return expanded.Count > 0 && Print(expanded, config, showDialog);
    }
}
