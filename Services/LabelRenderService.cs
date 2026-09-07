using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using LabelFlow.Models;

namespace LabelFlow.Services;

public static class LabelRenderService
{
    private const double MmToDip = 96.0 / 25.4;
    public static double MmToPx(double mm) => mm * MmToDip;

    public static FrameworkElement CreateLabel(Article article, ConnectionConfig config)
    {
        double width = MmToPx(config.LabelWidthMm);
        double height = MmToPx(config.LabelHeightMm);
        return config.LabelDesign switch
        {
            LabelDesign.Compact => BuildCompact(article, config, width, height),
            LabelDesign.ProductSheet => BuildProductSheet(article, config, width, height),
            LabelDesign.Industrial => BuildIndustrial(article, config, width, height),
            _ => BuildClassic(article, config, width, height)
        };
    }

    public static string FormatPrice(decimal price, ConnectionConfig config)
    {
        string number = config.ShowPriceDecimals
            ? price.ToString("N2", CultureInfo.CurrentCulture)
            : Math.Round(price, MidpointRounding.AwayFromZero).ToString("N0", CultureInfo.CurrentCulture);
        return $"{number} {config.CurrencySymbol}";
    }

    private static string BarcodeValue(Article article)
        => !string.IsNullOrWhiteSpace(article.CodeBarre) ? article.CodeBarre! : article.Reference;

    // ------------------------------------------------------------------
    //  CLASSIC
    // ------------------------------------------------------------------
    private static FrameworkElement BuildClassic(Article article, ConnectionConfig config, double w, double h)
    {
        var root = CreateRoot(w, h);
        var stack = new StackPanel
        {
            Margin = new Thickness(w * 0.05, h * 0.04, w * 0.05, h * 0.04),
            VerticalAlignment = VerticalAlignment.Center
        };

        if (config.LabelShowReference)
            stack.Children.Add(CreateText(article.Reference, AdaptiveFont(w, h, 0.11, 0.13), FontWeights.Bold, TextAlignment.Center));

        if (config.LabelShowBarcode)
        {
            var barcode = CreateBarcodeImage(article, config, w * 0.90, h * 0.28);
            if (barcode is not null)
            {
                barcode.Margin = new Thickness(0, h * 0.03, 0, h * 0.02);
                stack.Children.Add(barcode);
            }
        }

        if (config.LabelShowDesignation)
            stack.Children.Add(CreateText(article.Designation, AdaptiveFont(w, h, 0.075, 0.09), FontWeights.Normal, TextAlignment.Center, maxLines: 3));

        if (config.LabelShowPrice)
            stack.Children.Add(CreateText(FormatPrice(article.PrixVente, config), AdaptiveFont(w, h, 0.10, 0.12), FontWeights.Bold, TextAlignment.Center));

        if (config.LabelShowUnit && !string.IsNullOrWhiteSpace(article.UniteVente))
            stack.Children.Add(CreateText(article.UniteVente!, AdaptiveFont(w, h, 0.065, 0.08), FontWeights.Normal, TextAlignment.Center));

        root.Child = stack;
        return root;
    }

    // ------------------------------------------------------------------
    //  COMPACT
    // ------------------------------------------------------------------
    private static FrameworkElement BuildCompact(Article article, ConnectionConfig config, double w, double h)
    {
        var root = CreateRoot(w, h);
        var grid = new Grid { Margin = new Thickness(w * 0.04, h * 0.04, w * 0.04, h * 0.04) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.45, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var left = new StackPanel { VerticalAlignment = VerticalAlignment.Center };

        if (config.LabelShowReference)
            left.Children.Add(CreateText(article.Reference, AdaptiveFont(w, h, 0.09, 0.11), FontWeights.Bold, TextAlignment.Left));

        if (config.LabelShowDesignation)
            left.Children.Add(CreateText(article.Designation, AdaptiveFont(w, h, 0.065, 0.08), FontWeights.Normal, TextAlignment.Left, maxLines: 3));

        if (config.LabelShowPrice)
            left.Children.Add(CreateText(FormatPrice(article.PrixVente, config), AdaptiveFont(w, h, 0.09, 0.11), FontWeights.Bold, TextAlignment.Left));

        if (config.LabelShowUnit && !string.IsNullOrWhiteSpace(article.UniteVente))
            left.Children.Add(CreateText(article.UniteVente!, AdaptiveFont(w, h, 0.055, 0.07), FontWeights.Normal, TextAlignment.Left));

        Grid.SetColumn(left, 0);
        grid.Children.Add(left);

        if (config.LabelShowBarcode)
        {
            var barcode = CreateBarcodeImage(article, config, w * 0.32, h * 0.72, rotate90: true);
            if (barcode is not null)
            {
                barcode.HorizontalAlignment = HorizontalAlignment.Center;
                barcode.VerticalAlignment = VerticalAlignment.Center;
                Grid.SetColumn(barcode, 1);
                grid.Children.Add(barcode);
            }
        }

        root.Child = grid;
        return root;
    }

    // ------------------------------------------------------------------
    //  PRODUCT SHEET
    // ------------------------------------------------------------------
    private static FrameworkElement BuildProductSheet(Article article, ConnectionConfig config, double w, double h)
    {
        var root = CreateRoot(w, h);
        var stack = new StackPanel
        {
            Margin = new Thickness(w * 0.05, h * 0.04, w * 0.05, h * 0.04)
        };

        if (config.LabelShowReference)
            stack.Children.Add(CreateText(article.Reference, AdaptiveFont(w, h, 0.09, 0.11), FontWeights.Bold, TextAlignment.Left));

        if (config.LabelShowDesignation)
            stack.Children.Add(CreateText(article.Designation, AdaptiveFont(w, h, 0.07, 0.085), FontWeights.Normal, TextAlignment.Left, maxLines: 3));

        var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, h * 0.015, 0, 0) };
        if (config.LabelShowPrice)
            row.Children.Add(CreateText(FormatPrice(article.PrixVente, config), AdaptiveFont(w, h, 0.085, 0.10), FontWeights.Bold, TextAlignment.Left));
        if (config.LabelShowUnit && !string.IsNullOrWhiteSpace(article.UniteVente))
            row.Children.Add(CreateText("  ·  " + article.UniteVente, AdaptiveFont(w, h, 0.065, 0.08), FontWeights.Normal, TextAlignment.Left));
        stack.Children.Add(row);

        if (config.LabelShowBarcode)
        {
            var barcode = CreateBarcodeImage(article, config, w * 0.90, h * 0.26);
            if (barcode is not null)
            {
                barcode.Margin = new Thickness(0, h * 0.04, 0, 0);
                stack.Children.Add(barcode);
            }
        }

        root.Child = stack;
        return root;
    }

    // ------------------------------------------------------------------
    //  INDUSTRIAL
    // ------------------------------------------------------------------
    private static FrameworkElement BuildIndustrial(Article article, ConnectionConfig config, double w, double h)
    {
        var root = CreateRoot(w, h);
        var stack = new StackPanel();

        // Header noir
        var header = new Border
        {
            Background = Brushes.Black,
            Padding = new Thickness(w * 0.04, h * 0.035, w * 0.04, h * 0.035)
        };
        header.Child = new TextBlock
        {
            Text = config.LabelShowReference ? article.Reference : "LabelFlow",
            Foreground = Brushes.White,
            FontWeight = FontWeights.Bold,
            FontSize = Math.Max(9, AdaptiveFont(w, h, 0.09, 0.11)),
            TextAlignment = TextAlignment.Center,
            FontFamily = new FontFamily("Segoe UI")
        };
        stack.Children.Add(header);

        var body = new StackPanel
        {
            Margin = new Thickness(w * 0.05, h * 0.03, w * 0.05, h * 0.03)
        };

        if (config.LabelShowDesignation)
            body.Children.Add(CreateText(article.Designation, AdaptiveFont(w, h, 0.07, 0.085), FontWeights.SemiBold, TextAlignment.Left, maxLines: 3));

        if (config.LabelShowPrice || (config.LabelShowUnit && !string.IsNullOrWhiteSpace(article.UniteVente)))
        {
            var line = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, h * 0.012, 0, 0) };
            if (config.LabelShowPrice)
                line.Children.Add(CreateText(FormatPrice(article.PrixVente, config), AdaptiveFont(w, h, 0.08, 0.095), FontWeights.Bold, TextAlignment.Left));
            if (config.LabelShowUnit && !string.IsNullOrWhiteSpace(article.UniteVente))
                line.Children.Add(CreateText("  " + article.UniteVente, AdaptiveFont(w, h, 0.06, 0.075), FontWeights.Normal, TextAlignment.Left));
            body.Children.Add(line);
        }

        if (config.LabelShowBarcode)
        {
            var barcode = CreateBarcodeImage(article, config, w * 0.90, h * 0.24);
            if (barcode is not null)
            {
                barcode.Margin = new Thickness(0, h * 0.03, 0, 0);
                body.Children.Add(barcode);
            }
            body.Children.Add(CreateText(BarcodeValue(article), AdaptiveFont(w, h, 0.055, 0.07), FontWeights.Normal, TextAlignment.Center));
        }

        stack.Children.Add(body);
        root.Child = stack;
        return root;
    }

    // ------------------------------------------------------------------
    //  Helpers
    // ------------------------------------------------------------------
    private static Border CreateRoot(double w, double h) => new()
    {
        Width = w,
        Height = h,
        Background = Brushes.White,
        BorderBrush = Brushes.Black,
        BorderThickness = new Thickness(0.5),
        SnapsToDevicePixels = true
    };

    /// <summary>
    /// Calcule une taille de police adaptative en fonction de la largeur ET de la hauteur de l'étiquette.
    /// </summary>
    private static double AdaptiveFont(double width, double height, double widthFactor, double heightFactor)
    {
        double fromWidth = width * widthFactor;
        double fromHeight = height * heightFactor;
        // On prend la plus petite des deux pour que le texte reste lisible sur les étiquettes très plates ou très étroites
        return Math.Max(7.0, Math.Min(fromWidth, fromHeight));
    }

    private static TextBlock CreateText(string text, double fontSize, FontWeight weight, TextAlignment align, int maxLines = 1)
    {
        var tb = new TextBlock
        {
            Text = text ?? string.Empty,
            FontSize = fontSize,
            FontWeight = weight,
            FontFamily = new FontFamily("Segoe UI"),
            TextAlignment = align,
            Foreground = Brushes.Black,
            TextWrapping = maxLines > 1 ? TextWrapping.Wrap : TextWrapping.NoWrap,
            // On laisse le texte s'afficher le plus possible (moins d'ellipse agressive)
            TextTrimming = maxLines > 1 ? TextTrimming.None : TextTrimming.CharacterEllipsis,
            LineStackingStrategy = LineStackingStrategy.BlockLineHeight,
            LineHeight = fontSize * 1.25
        };

        if (maxLines > 1)
        {
            // Hauteur max un peu plus généreuse pour 2-3 lignes
            tb.MaxHeight = fontSize * 1.25 * maxLines + 2;
        }

        return tb;
    }

    private static FrameworkElement? CreateBarcodeImage(Article article, ConnectionConfig config, double widthDip, double heightDip, bool rotate90 = false)
    {
        int pxW = Math.Max(100, (int)widthDip);
        int pxH = Math.Max(40, (int)heightDip);
        if (rotate90) (pxW, pxH) = (Math.Max(40, (int)heightDip), Math.Max(100, (int)widthDip));

        var source = BarcodeService.Generate(BarcodeValue(article), config.BarcodeType, pxW, pxH);
        if (source is null) return null;

        var image = new Image
        {
            Source = source,
            Stretch = Stretch.Fill,
            Width = rotate90 ? heightDip : widthDip,
            Height = rotate90 ? widthDip : heightDip,
            SnapsToDevicePixels = true
        };

        if (rotate90)
            image.LayoutTransform = new RotateTransform(90);

        return image;
    }
}
