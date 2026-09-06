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

    private static FrameworkElement BuildClassic(Article article, ConnectionConfig config, double w, double h)
    {
        var root = CreateRoot(w, h);
        var stack = new StackPanel { Margin = new Thickness(w * 0.06), VerticalAlignment = VerticalAlignment.Center };
        if (config.LabelShowReference)
            stack.Children.Add(CreateText(article.Reference, w * 0.12, FontWeights.Bold, TextAlignment.Center));
        if (config.LabelShowBarcode)
        {
            var barcode = CreateBarcodeImage(article, config, w * 0.88, h * 0.32);
            if (barcode is not null) { barcode.Margin = new Thickness(0, h * 0.04, 0, h * 0.02); stack.Children.Add(barcode); }
        }
        if (config.LabelShowDesignation)
            stack.Children.Add(CreateText(article.Designation, w * 0.08, FontWeights.Normal, TextAlignment.Center, 2));
        if (config.LabelShowPrice)
            stack.Children.Add(CreateText(FormatPrice(article.PrixVente, config), w * 0.11, FontWeights.Bold, TextAlignment.Center));
        if (config.LabelShowUnit && !string.IsNullOrWhiteSpace(article.UniteVente))
            stack.Children.Add(CreateText(article.UniteVente!, w * 0.07, FontWeights.Normal, TextAlignment.Center));
        root.Child = stack;
        return root;
    }

    private static FrameworkElement BuildCompact(Article article, ConnectionConfig config, double w, double h)
    {
        var root = CreateRoot(w, h);
        var grid = new Grid { Margin = new Thickness(w * 0.05) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.4, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        var left = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        if (config.LabelShowReference) left.Children.Add(CreateText(article.Reference, w * 0.1, FontWeights.Bold, TextAlignment.Left));
        if (config.LabelShowDesignation) left.Children.Add(CreateText(article.Designation, w * 0.07, FontWeights.Normal, TextAlignment.Left, 2));
        if (config.LabelShowPrice) left.Children.Add(CreateText(FormatPrice(article.PrixVente, config), w * 0.1, FontWeights.Bold, TextAlignment.Left));
        if (config.LabelShowUnit && !string.IsNullOrWhiteSpace(article.UniteVente))
            left.Children.Add(CreateText(article.UniteVente!, w * 0.06, FontWeights.Normal, TextAlignment.Left));
        Grid.SetColumn(left, 0); grid.Children.Add(left);
        if (config.LabelShowBarcode)
        {
            var barcode = CreateBarcodeImage(article, config, w * 0.35, h * 0.7, true);
            if (barcode is not null)
            {
                barcode.HorizontalAlignment = HorizontalAlignment.Center;
                barcode.VerticalAlignment = VerticalAlignment.Center;
                Grid.SetColumn(barcode, 1); grid.Children.Add(barcode);
            }
        }
        root.Child = grid; return root;
    }

    private static FrameworkElement BuildProductSheet(Article article, ConnectionConfig config, double w, double h)
    {
        var root = CreateRoot(w, h);
        var stack = new StackPanel { Margin = new Thickness(w * 0.06, h * 0.05, w * 0.06, h * 0.04) };
        if (config.LabelShowReference) stack.Children.Add(CreateText(article.Reference, w * 0.1, FontWeights.Bold, TextAlignment.Left));
        if (config.LabelShowDesignation) stack.Children.Add(CreateText(article.Designation, w * 0.075, FontWeights.Normal, TextAlignment.Left, 2));
        var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, h * 0.02, 0, 0) };
        if (config.LabelShowPrice) row.Children.Add(CreateText(FormatPrice(article.PrixVente, config), w * 0.09, FontWeights.Bold, TextAlignment.Left));
        if (config.LabelShowUnit && !string.IsNullOrWhiteSpace(article.UniteVente))
            row.Children.Add(CreateText("  ·  " + article.UniteVente, w * 0.07, FontWeights.Normal, TextAlignment.Left));
        stack.Children.Add(row);
        if (config.LabelShowBarcode)
        {
            var barcode = CreateBarcodeImage(article, config, w * 0.88, h * 0.28);
            if (barcode is not null) { barcode.Margin = new Thickness(0, h * 0.06, 0, 0); stack.Children.Add(barcode); }
        }
        root.Child = stack; return root;
    }

    private static FrameworkElement BuildIndustrial(Article article, ConnectionConfig config, double w, double h)
    {
        var root = CreateRoot(w, h);
        var stack = new StackPanel();
        var header = new Border { Background = Brushes.Black, Padding = new Thickness(w * 0.05, h * 0.04, w * 0.05, h * 0.04) };
        header.Child = new TextBlock
        {
            Text = config.LabelShowReference ? article.Reference : "LabelFlow",
            Foreground = Brushes.White, FontWeight = FontWeights.Bold,
            FontSize = Math.Max(10, w * 0.09), TextAlignment = TextAlignment.Center
        };
        stack.Children.Add(header);
        var body = new StackPanel { Margin = new Thickness(w * 0.06, h * 0.04, w * 0.06, h * 0.04) };
        if (config.LabelShowDesignation)
            body.Children.Add(CreateText(article.Designation, w * 0.075, FontWeights.SemiBold, TextAlignment.Left, 2));
        if (config.LabelShowPrice || (config.LabelShowUnit && !string.IsNullOrWhiteSpace(article.UniteVente)))
        {
            var line = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, h * 0.015, 0, 0) };
            if (config.LabelShowPrice) line.Children.Add(CreateText(FormatPrice(article.PrixVente, config), w * 0.08, FontWeights.Bold, TextAlignment.Left));
            if (config.LabelShowUnit && !string.IsNullOrWhiteSpace(article.UniteVente))
                line.Children.Add(CreateText("  " + article.UniteVente, w * 0.065, FontWeights.Normal, TextAlignment.Left));
            body.Children.Add(line);
        }
        if (config.LabelShowBarcode)
        {
            var barcode = CreateBarcodeImage(article, config, w * 0.88, h * 0.26);
            if (barcode is not null) { barcode.Margin = new Thickness(0, h * 0.04, 0, 0); body.Children.Add(barcode); }
            body.Children.Add(CreateText(BarcodeValue(article), w * 0.06, FontWeights.Normal, TextAlignment.Center));
        }
        stack.Children.Add(body); root.Child = stack; return root;
    }

    private static Border CreateRoot(double w, double h) => new()
    {
        Width = w, Height = h, Background = Brushes.White,
        BorderBrush = Brushes.Black, BorderThickness = new Thickness(0.5), SnapsToDevicePixels = true
    };

    private static TextBlock CreateText(string text, double fontSize, FontWeight weight, TextAlignment align, int maxLines = 1) => new()
    {
        Text = text ?? string.Empty,
        FontSize = Math.Max(7, fontSize),
        FontWeight = weight,
        FontFamily = new FontFamily("Segoe UI"),
        TextAlignment = align,
        TextWrapping = maxLines > 1 ? TextWrapping.Wrap : TextWrapping.NoWrap,
        TextTrimming = TextTrimming.CharacterEllipsis,
        MaxHeight = maxLines > 1 ? fontSize * 2.6 : double.PositiveInfinity,
        Foreground = Brushes.Black
    };

    private static FrameworkElement? CreateBarcodeImage(Article article, ConnectionConfig config, double widthDip, double heightDip, bool rotate90 = false)
    {
        int pxW = Math.Max(100, (int)widthDip);
        int pxH = Math.Max(40, (int)heightDip);
        if (rotate90) (pxW, pxH) = (Math.Max(40, (int)heightDip), Math.Max(100, (int)widthDip));
        var source = BarcodeService.Generate(BarcodeValue(article), config.BarcodeType, pxW, pxH);
        if (source is null) return null;
        var image = new Image
        {
            Source = source, Stretch = Stretch.Fill,
            Width = rotate90 ? heightDip : widthDip,
            Height = rotate90 ? widthDip : heightDip,
            SnapsToDevicePixels = true
        };
        if (!rotate90) return image;
        image.LayoutTransform = new RotateTransform(90);
        return image;
    }
}
