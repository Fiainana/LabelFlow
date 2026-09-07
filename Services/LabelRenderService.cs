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

    private static readonly SolidColorBrush BlackBrush = Brushes.Black;

    public static FrameworkElement CreateLabel(Article article, ConnectionConfig config)
    {
        double width = MmToPx(config.LabelWidthMm);
        double height = MmToPx(config.LabelHeightMm);

        return config.LabelDesign switch
        {
            LabelDesign.Compact => BuildCompact(article, config, width, height),
            LabelDesign.ProductSheet => BuildProductSheet(article, config, width, height),
            LabelDesign.Industrial => BuildIndustrial(article, config, width, height),
            LabelDesign.Retail => BuildRetail(article, config, width, height),
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

    // ==================================================================
    //  Helpers responsives
    // ==================================================================

    /// <summary>
    /// Calcule une taille de police adaptative selon largeur, hauteur et densité de l'étiquette.
    /// </summary>
    private static double AdaptiveFont(double w, double h, double baseFactor)
    {
        // Facteur selon la surface (plus l'étiquette est grande, plus on peut grossir)
        double areaFactor = Math.Sqrt((w * h) / (50.0 * MmToDip * 30.0 * MmToDip)); // réf = 50x30 mm
        areaFactor = Math.Clamp(areaFactor, 0.7, 1.6);

        // On prend le min entre largeur et hauteur pour rester lisible
        double size = Math.Min(w, h) * baseFactor * areaFactor;
        return Math.Clamp(size, 6.5, 42);
    }

    /// <summary>
    /// Nombre de lignes max pour la désignation selon la hauteur disponible.
    /// </summary>
    private static int MaxDesignationLines(double h)
    {
        if (h < 80) return 2;   // très petite
        if (h < 130) return 3;  // standard
        return 4;               // grande
    }

    private static Border CreateRoot(double w, double h) => new()
    {
        Width = w,
        Height = h,
        Background = Brushes.White,
        BorderBrush = BlackBrush,
        BorderThickness = new Thickness(0.8),
        SnapsToDevicePixels = true
    };

    private static TextBlock CreateText(
        string text,
        double fontSize,
        FontWeight weight,
        TextAlignment align,
        int maxLines = 1)
    {
        var tb = new TextBlock
        {
            Text = text ?? string.Empty,
            FontSize = fontSize,
            FontWeight = weight,
            FontFamily = new FontFamily("Segoe UI"),
            TextAlignment = align,
            Foreground = BlackBrush,
            TextWrapping = maxLines > 1 ? TextWrapping.Wrap : TextWrapping.NoWrap,
            TextTrimming = maxLines > 1 ? TextTrimming.None : TextTrimming.CharacterEllipsis,
            LineStackingStrategy = LineStackingStrategy.BlockLineHeight,
            LineHeight = fontSize * 1.25
        };

        if (maxLines > 1)
            tb.MaxHeight = fontSize * 1.25 * maxLines + 2;

        return tb;
    }

    private static FrameworkElement CreateSeparator(double width, double verticalMargin)
    {
        return new Border
        {
            Width = width,
            Height = 1,
            Background = BlackBrush,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, verticalMargin, 0, verticalMargin)
        };
    }

    private static FrameworkElement? CreateBarcodeImage(
        Article article,
        ConnectionConfig config,
        double widthDip,
        double heightDip,
        bool rotate90 = false)
    {
        int pxW = Math.Max(80, (int)widthDip);
        int pxH = Math.Max(30, (int)heightDip);
        if (rotate90) (pxW, pxH) = (Math.Max(30, (int)heightDip), Math.Max(80, (int)widthDip));

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

    // ==================================================================
    //  CLASSIC
    // ==================================================================
    private static FrameworkElement BuildClassic(Article article, ConnectionConfig config, double w, double h)
    {
        var root = CreateRoot(w, h);
        var main = new DockPanel { LastChildFill = true };

        if (config.LabelShowBarcode)
        {
            var barcodeZone = new StackPanel
            {
                Margin = new Thickness(w * 0.05, 0, w * 0.05, h * 0.035),
                HorizontalAlignment = HorizontalAlignment.Center
            };

            double bcH = Math.Min(h * 0.28, 70);
            var barcode = CreateBarcodeImage(article, config, w * 0.90, bcH);
            if (barcode is not null)
            {
                barcodeZone.Children.Add(barcode);
                barcodeZone.Children.Add(CreateText(
                    BarcodeValue(article),
                    AdaptiveFont(w, h, 0.055),
                    FontWeights.Normal,
                    TextAlignment.Center));
            }

            DockPanel.SetDock(barcodeZone, Dock.Bottom);
            main.Children.Add(barcodeZone);
        }

        var content = new StackPanel
        {
            Margin = new Thickness(w * 0.06, h * 0.04, w * 0.06, h * 0.02),
            VerticalAlignment = VerticalAlignment.Center
        };

        if (config.LabelShowReference)
            content.Children.Add(CreateText(article.Reference, AdaptiveFont(w, h, 0.10), FontWeights.Bold, TextAlignment.Center));

        if (config.LabelShowDesignation)
        {
            var desig = CreateText(article.Designation, AdaptiveFont(w, h, 0.07), FontWeights.Normal, TextAlignment.Center, MaxDesignationLines(h));
            desig.Margin = new Thickness(0, h * 0.012, 0, 0);
            content.Children.Add(desig);
        }

        if (config.LabelShowPrice || (config.LabelShowUnit && !string.IsNullOrWhiteSpace(article.UniteVente)))
            content.Children.Add(CreateSeparator(w * 0.45, h * 0.02));

        if (config.LabelShowPrice)
            content.Children.Add(CreateText(FormatPrice(article.PrixVente, config), AdaptiveFont(w, h, 0.13), FontWeights.Bold, TextAlignment.Center));

        if (config.LabelShowUnit && !string.IsNullOrWhiteSpace(article.UniteVente))
        {
            var unit = CreateText(article.UniteVente!, AdaptiveFont(w, h, 0.055), FontWeights.Normal, TextAlignment.Center);
            unit.Margin = new Thickness(0, h * 0.006, 0, 0);
            content.Children.Add(unit);
        }

        main.Children.Add(content);
        root.Child = main;
        return root;
    }

    // ==================================================================
    //  COMPACT
    // ==================================================================
    private static FrameworkElement BuildCompact(Article article, ConnectionConfig config, double w, double h)
    {
        var root = CreateRoot(w, h);
        var grid = new Grid { Margin = new Thickness(w * 0.04, h * 0.035, w * 0.03, h * 0.035) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.55, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });

        var left = new StackPanel { VerticalAlignment = VerticalAlignment.Center };

        if (config.LabelShowReference)
            left.Children.Add(CreateText(article.Reference, AdaptiveFont(w, h, 0.09), FontWeights.Bold, TextAlignment.Left));

        if (config.LabelShowDesignation)
        {
            var desig = CreateText(article.Designation, AdaptiveFont(w, h, 0.062), FontWeights.Normal, TextAlignment.Left, MaxDesignationLines(h));
            desig.Margin = new Thickness(0, h * 0.01, 0, 0);
            left.Children.Add(desig);
        }

        if (config.LabelShowPrice)
        {
            var price = CreateText(FormatPrice(article.PrixVente, config), AdaptiveFont(w, h, 0.10), FontWeights.Bold, TextAlignment.Left);
            price.Margin = new Thickness(0, h * 0.018, 0, 0);
            left.Children.Add(price);
        }

        if (config.LabelShowUnit && !string.IsNullOrWhiteSpace(article.UniteVente))
            left.Children.Add(CreateText(article.UniteVente!, AdaptiveFont(w, h, 0.05), FontWeights.Normal, TextAlignment.Left));

        Grid.SetColumn(left, 0);
        grid.Children.Add(left);

        if (config.LabelShowBarcode)
        {
            var right = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var barcode = CreateBarcodeImage(article, config, w * 0.28, h * 0.72, rotate90: true);
            if (barcode is not null) right.Children.Add(barcode);

            Grid.SetColumn(right, 1);
            grid.Children.Add(right);
        }

        root.Child = grid;
        return root;
    }

    // ==================================================================
    //  PRODUCT SHEET
    // ==================================================================
    private static FrameworkElement BuildProductSheet(Article article, ConnectionConfig config, double w, double h)
    {
        var root = CreateRoot(w, h);
        var main = new DockPanel { LastChildFill = true };

        if (config.LabelShowBarcode)
        {
            var barcodeZone = new Border
            {
                BorderBrush = BlackBrush,
                BorderThickness = new Thickness(0, 1, 0, 0),
                Padding = new Thickness(w * 0.05, h * 0.025, w * 0.05, h * 0.03),
                Background = Brushes.White
            };

            var barcodeStack = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
            double bcH = Math.Min(h * 0.24, 60);
            var barcode = CreateBarcodeImage(article, config, w * 0.88, bcH);
            if (barcode is not null)
            {
                barcodeStack.Children.Add(barcode);
                barcodeStack.Children.Add(CreateText(BarcodeValue(article), AdaptiveFont(w, h, 0.05), FontWeights.Normal, TextAlignment.Center));
            }
            barcodeZone.Child = barcodeStack;

            DockPanel.SetDock(barcodeZone, Dock.Bottom);
            main.Children.Add(barcodeZone);
        }

        var content = new StackPanel { Margin = new Thickness(w * 0.06, h * 0.04, w * 0.06, h * 0.025) };

        if (config.LabelShowReference)
            content.Children.Add(CreateText(article.Reference, AdaptiveFont(w, h, 0.085), FontWeights.Bold, TextAlignment.Left));

        if (config.LabelShowDesignation)
        {
            var desig = CreateText(article.Designation, AdaptiveFont(w, h, 0.07), FontWeights.SemiBold, TextAlignment.Left, MaxDesignationLines(h));
            desig.Margin = new Thickness(0, h * 0.01, 0, 0);
            content.Children.Add(desig);
        }

        var priceRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, h * 0.02, 0, 0) };

        if (config.LabelShowPrice)
            priceRow.Children.Add(CreateText(FormatPrice(article.PrixVente, config), AdaptiveFont(w, h, 0.11), FontWeights.Bold, TextAlignment.Left));

        if (config.LabelShowUnit && !string.IsNullOrWhiteSpace(article.UniteVente))
        {
            var unit = CreateText("  / " + article.UniteVente, AdaptiveFont(w, h, 0.055), FontWeights.Normal, TextAlignment.Left);
            unit.VerticalAlignment = VerticalAlignment.Bottom;
            priceRow.Children.Add(unit);
        }

        content.Children.Add(priceRow);
        main.Children.Add(content);
        root.Child = main;
        return root;
    }

    // ==================================================================
    //  INDUSTRIAL
    // ==================================================================
    private static FrameworkElement BuildIndustrial(Article article, ConnectionConfig config, double w, double h)
    {
        var root = CreateRoot(w, h);
        var main = new DockPanel { LastChildFill = true };

        var header = new Border
        {
            Background = BlackBrush,
            Padding = new Thickness(w * 0.04, h * 0.035, w * 0.04, h * 0.035)
        };

        header.Child = new TextBlock
        {
            Text = config.LabelShowReference ? article.Reference : "LabelFlow",
            Foreground = Brushes.White,
            FontWeight = FontWeights.Bold,
            FontSize = Math.Max(8, AdaptiveFont(w, h, 0.10)),
            TextAlignment = TextAlignment.Center,
            FontFamily = new FontFamily("Segoe UI Semibold")
        };

        DockPanel.SetDock(header, Dock.Top);
        main.Children.Add(header);

        var body = new StackPanel { Margin = new Thickness(w * 0.05, h * 0.03, w * 0.05, h * 0.025) };

        if (config.LabelShowDesignation)
            body.Children.Add(CreateText(article.Designation, AdaptiveFont(w, h, 0.07), FontWeights.SemiBold, TextAlignment.Left, MaxDesignationLines(h)));

        if (config.LabelShowPrice || (config.LabelShowUnit && !string.IsNullOrWhiteSpace(article.UniteVente)))
        {
            var priceLine = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, h * 0.015, 0, 0) };

            if (config.LabelShowPrice)
                priceLine.Children.Add(CreateText(FormatPrice(article.PrixVente, config), AdaptiveFont(w, h, 0.095), FontWeights.Bold, TextAlignment.Left));

            if (config.LabelShowUnit && !string.IsNullOrWhiteSpace(article.UniteVente))
                priceLine.Children.Add(CreateText("  " + article.UniteVente, AdaptiveFont(w, h, 0.055), FontWeights.Normal, TextAlignment.Left));

            body.Children.Add(priceLine);
        }

        if (config.LabelShowBarcode)
        {
            double bcH = Math.Min(h * 0.22, 55);
            var barcode = CreateBarcodeImage(article, config, w * 0.90, bcH);
            if (barcode is not null)
            {
                barcode.Margin = new Thickness(0, h * 0.02, 0, 0);
                body.Children.Add(barcode);
                body.Children.Add(CreateText(BarcodeValue(article), AdaptiveFont(w, h, 0.05), FontWeights.Normal, TextAlignment.Center));
            }
        }

        main.Children.Add(body);
        root.Child = main;
        return root;
    }

    // ==================================================================
    //  RETAIL / GRAND MAGASIN
    //  Prix très visible, désignation claire, code-barres en bas
    // ==================================================================
    private static FrameworkElement BuildRetail(Article article, ConnectionConfig config, double w, double h)
    {
        var root = CreateRoot(w, h);
        var main = new DockPanel { LastChildFill = true };

        // --- Code-barres en bas ---
        if (config.LabelShowBarcode)
        {
            var barcodeZone = new StackPanel
            {
                Margin = new Thickness(w * 0.05, h * 0.015, w * 0.05, h * 0.03),
                HorizontalAlignment = HorizontalAlignment.Center
            };

            double bcH = Math.Min(h * 0.26, 65);
            var barcode = CreateBarcodeImage(article, config, w * 0.92, bcH);
            if (barcode is not null)
            {
                barcodeZone.Children.Add(barcode);
                barcodeZone.Children.Add(CreateText(
                    BarcodeValue(article),
                    AdaptiveFont(w, h, 0.05),
                    FontWeights.Normal,
                    TextAlignment.Center));
            }

            DockPanel.SetDock(barcodeZone, Dock.Bottom);
            main.Children.Add(barcodeZone);
        }

        // --- Contenu principal ---
        var content = new StackPanel
        {
            Margin = new Thickness(w * 0.06, h * 0.04, w * 0.06, h * 0.01),
            VerticalAlignment = VerticalAlignment.Center
        };

        // Référence en haut (petit)
        if (config.LabelShowReference)
        {
            content.Children.Add(CreateText(
                article.Reference,
                AdaptiveFont(w, h, 0.07),
                FontWeights.SemiBold,
                TextAlignment.Center));
        }

        // Désignation
        if (config.LabelShowDesignation)
        {
            var desig = CreateText(
                article.Designation,
                AdaptiveFont(w, h, 0.075),
                FontWeights.Normal,
                TextAlignment.Center,
                MaxDesignationLines(h));
            desig.Margin = new Thickness(0, h * 0.012, 0, 0);
            content.Children.Add(desig);
        }

        // Séparateur
        content.Children.Add(CreateSeparator(w * 0.55, h * 0.02));

        // PRIX TRÈS GRAND (point fort du design Grand Magasin)
        if (config.LabelShowPrice)
        {
            content.Children.Add(CreateText(
                FormatPrice(article.PrixVente, config),
                AdaptiveFont(w, h, 0.16),   // plus gros que les autres designs
                FontWeights.Bold,
                TextAlignment.Center));
        }

        // Unité
        if (config.LabelShowUnit && !string.IsNullOrWhiteSpace(article.UniteVente))
        {
            var unit = CreateText(
                article.UniteVente!,
                AdaptiveFont(w, h, 0.055),
                FontWeights.Normal,
                TextAlignment.Center);
            unit.Margin = new Thickness(0, h * 0.006, 0, 0);
            content.Children.Add(unit);
        }

        main.Children.Add(content);
        root.Child = main;
        return root;
    }
}
