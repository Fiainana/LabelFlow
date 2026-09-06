using System.Windows;
using System.Windows.Documents;
using LabelFlow.Models;
using LabelFlow.Services;

namespace LabelFlow.Views;

public partial class PrintPreviewWindow : Window
{
    private readonly IReadOnlyList<Article> _articles;
    private readonly ConnectionConfig _config;

    public PrintPreviewWindow(IReadOnlyList<Article> articles, ConnectionConfig config)
    {
        InitializeComponent();
        _articles = articles;
        _config = config;
        Viewer.Document = LabelPrintService.BuildDocument(articles, config);
        TxtTitle.Text = $"Aperçu — {articles.Count} étiquette{(articles.Count > 1 ? "s" : "")}";
        TxtInfo.Text = $"{config.LabelWidthMm} × {config.LabelHeightMm} mm  •  {GetDesignName(config.LabelDesign)}  •  {(config.BarcodeType == BarcodeType.Code39 ? "Code 39" : "Code 128")}";
    }

    private static string GetDesignName(LabelDesign d) => d switch
    {
        LabelDesign.Compact => "Compact",
        LabelDesign.ProductSheet => "Fiche produit",
        LabelDesign.Industrial => "Industriel",
        _ => "Classique"
    };

    private void OnPrintClick(object sender, RoutedEventArgs e)
    {
        if (LabelPrintService.Print(_articles, _config))
        {
            DialogResult = true;
            Close();
        }
    }

    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();
}
