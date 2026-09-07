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

    // Couleurs optimisées pour imprimante thermique (noir pur uniquement)
    private static readonly SolidColorBrush BlackBrush = Brushes.Black;
    private static readonly SolidColorBrush DarkBrush = Brushes.Black;
    private static readonly SolidColorBrush MutedBrush = Brushes.Black;
    private static readonly SolidColorBrush AccentBrush = Brushes.Black;
    private static readonly SolidColorBrush PriceBrush = Brushes.Black;
    private static readonly SolidColorBrush LightGrayBrush = new(Color.FromRgb(0, 0, 0)); // noir aussi pour les séparateurs

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

    // ==================================================================
    //  CLASSIC — Style retail propre et équilibré
    // ==================================================================
    private static FrameworkElement BuildClassic(Article article, ConnectionConfig config, double w, double h)
    {
        var root = CreateRoot(w, h);
        var main = new DockPanel { LastChildFill = true };

        // --- Zone barcode en bas ---
        if (config.LabelShowBarcode)
        {
            var barcodeZone = new StackPanel
            {
                Margin = new Thickness(w * 0.06, 0, w * 0.06, h * 0.04),
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var barcode = CreateBarcodeImage(article, config, w * 0.88, h * 0.26);
            if (barcode is not null)
            {
                barcodeZone.Children.Add(barcode);
                barcodeZone.Children.Add(CreateText(
                    BarcodeValue(article),
                    AdaptiveFont(w, h, 0.055, 0.065),
                    FontWeights.Normal,
                    TextAlignment.Center));
            }

            DockPanel.SetDock(barcodeZone, Dock.Bottom);
            main.Children.Add(barcodeZone);
        }

        // --- Contenu haut ---
        var content = new StackPanel
        {
            Margin = new Thickness(w * 0.07, h * 0.05, w * 0.07, h * 0.02),
            VerticalAlignment = VerticalAlignment.Center
        };

        if (config.LabelShowReference)
        {
            content.Children.Add(CreateText(
                article.Reference,
                AdaptiveFont(w, h, 0.095, 0.11),
                FontWeights.Bold,
                TextAlignment.Center));
        }

        if (config.LabelShowDesignation)
        {
            var desig = CreateText(
                article.Designation,
                AdaptiveFont(w, h, 0.07, 0.085),
                FontWeights.Normal,
                TextAlignment.Center,
                maxLines: 3);
            desig.Margin = new Thickness(0, h * 0.015, 0, 0);
            content.Children.Add(desig);
        }

        // Séparateur fin
        if (config.LabelShowPrice || (config.LabelShowUnit && !string.IsNullOrWhiteSpace(article.UniteVente)))
        {
            content.Children.Add(CreateSeparator(w * 0.5, h * 0.025));
        }

        if (config.LabelShowPrice)
        {
            content.Children.Add(CreateText(
                FormatPrice(article.PrixVente, config),
                AdaptiveFont(w, h, 0.12, 0.14),
                FontWeights.Bold,
                TextAlignment.Center));
        }

        if (config.LabelShowUnit && !string.IsNullOrWhiteSpace(article.UniteVente))
        {
            var unit = CreateText(
                article.UniteVente!,
                AdaptiveFont(w, h, 0.055, 0.07),
                FontWeights.Normal,
                TextAlignment.Center);
            unit.Margin = new Thickness(0, h * 0.008, 0, 0);
            content.Children.Add(unit);
        }

        main.Children.Add(content);
        root.Child = main;
        return root;
    }

    // ==================================================================
    //  COMPACT — Texte à gauche + code-barres vertical
    // ==================================================================
    private static FrameworkElement BuildCompact(Article article, ConnectionConfig config, double w, double h)
    {
        var root = CreateRoot(w, h);
        var grid = new Grid { Margin = new Thickness(w * 0.04, h * 0.04, w * 0.03, h * 0.04) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.5, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });

        // --- Colonne gauche ---
        var left = new StackPanel { VerticalAlignment = VerticalAlignment.Center };

        if (config.LabelShowReference)
        {
            left.Children.Add(CreateText(
                article.Reference,
                AdaptiveFont(w, h, 0.085, 0.10),
                FontWeights.Bold,
                TextAlignment.Left));
        }

        if (config.LabelShowDesignation)
        {
            var desig = CreateText(
                article.Designation,
                AdaptiveFont(w, h, 0.06, 0.075),
                FontWeights.Normal,
                TextAlignment.Left,
                maxLines: 3);
            desig.Margin = new Thickness(0, h * 0.012, 0, 0);
            left.Children.Add(desig);
        }

        if (config.LabelShowPrice)
        {
            var price = CreateText(
                FormatPrice(article.PrixVente, config),
                AdaptiveFont(w, h, 0.095, 0.115),
                FontWeights.Bold,
                TextAlignment.Left);
            price.Margin = new Thickness(0, h * 0.02, 0, 0);
            left.Children.Add(price);
        }

        if (config.LabelShowUnit && !string.IsNullOrWhiteSpace(article.UniteVente))
        {
            left.Children.Add(CreateText(
                article.UniteVente!,
                AdaptiveFont(w, h, 0.05, 0.065),
                FontWeights.Normal,
                TextAlignment.Left));
        }

        Grid.SetColumn(left, 0);
        grid.Children.Add(left);

        // --- Colonne droite (barcode vertical) ---
        if (config.LabelShowBarcode)
        {
            var right = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var barcode = CreateBarcodeImage(article, config, w * 0.30, h * 0.70, rotate90: true);
            if (barcode is not null)
            {
                right.Children.Add(barcode);
            }

            Grid.SetColumn(right, 1);
            grid.Children.Add(right);
        }

        root.Child = grid;
        return root;
    }

    // ==================================================================
    //  PRODUCT SHEET — Style fiche produit moderne
    // ==================================================================
    private static FrameworkElement BuildProductSheet(Article article, ConnectionConfig config, double w, double h)
    {
        var root = CreateRoot(w, h);
        var main = new DockPanel { LastChildFill = true };

        // Barcode en bas
        if (config.LabelShowBarcode)
        {
            var barcodeZone = new Border
            {
                BorderBrush = BlackBrush,
                BorderThickness = new Thickness(0, 1, 0, 0),
                Padding = new Thickness(w * 0.06, h * 0.03, w * 0.06, h * 0.035),
                Background = Brushes.White
            };

            var barcodeStack = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
            var barcode = CreateBarcodeImage(article, config, w * 0.86, h * 0.22);
            if (barcode is not null)
            {
                barcodeStack.Children.Add(barcode);
                barcodeStack.Children.Add(CreateText(
                    BarcodeValue(article),
                    AdaptiveFont(w, h, 0.05, 0.06),
                    FontWeights.Normal,
                    TextAlignment.Center));
            }
            barcodeZone.Child = barcodeStack;

            DockPanel.SetDock(barcodeZone, Dock.Bottom);
            main.Children.Add(barcodeZone);
        }

        // Contenu
        var content = new StackPanel
        {
            Margin = new Thickness(w * 0.07, h * 0.05, w * 0.07, h * 0.03)
        };

        if (config.LabelShowReference)
        {
            content.Children.Add(CreateText(
                article.Reference,
                AdaptiveFont(w, h, 0.08, 0.095),
                FontWeights.Bold,
                TextAlignment.Left));
        }

        if (config.LabelShowDesignation)
        {
            var desig = CreateText(
                article.Designation,
                AdaptiveFont(w, h, 0.068, 0.08),
                FontWeights.SemiBold,
                TextAlignment.Left,
                maxLines: 3);
            desig.Margin = new Thickness(0, h * 0.012, 0, 0);
            content.Children.Add(desig);
        }

        // Ligne prix + unité
        var priceRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(0, h * 0.025, 0, 0)
        };

        if (config.LabelShowPrice)
        {
            priceRow.Children.Add(CreateText(
                FormatPrice(article.PrixVente, config),
                AdaptiveFont(w, h, 0.105, 0.125),
                FontWeights.Bold,
                TextAlignment.Left));
        }

        if (config.LabelShowUnit && !string.IsNullOrWhiteSpace(article.UniteVente))
        {
            var unit = CreateText(
                "  / " + article.UniteVente,
                AdaptiveFont(w, h, 0.06, 0.075),
                FontWeights.Normal,
                TextAlignment.Left);
            unit.VerticalAlignment = VerticalAlignment.Bottom;
            unit.Margin = new Thickness(0, 0, 0, 1);
            priceRow.Children.Add(unit);
        }

        content.Children.Add(priceRow);
        main.Children.Add(content);
        root.Child = main;
        return root;
    }

    // ==================================================================
    //  INDUSTRIAL — Header noir fort + corps clair
    // ==================================================================
    private static FrameworkElement BuildIndustrial(Article article, ConnectionConfig config, double w, double h)
    {
        var root = CreateRoot(w, h);
        var main = new DockPanel { LastChildFill = true };

        // --- Header noir ---
        var header = new Border
        {
            Background = BlackBrush,
            Padding = new Thickness(w * 0.05, h * 0.04, w * 0.05, h * 0.04)
        };

        header.Child = new TextBlock
        {
            Text = config.LabelShowReference ? article.Reference : "LabelFlow",
            Foreground = Brushes.White,
            FontWeight = FontWeights.Bold,
            FontSize = Math.Max(9, AdaptiveFont(w, h, 0.095, 0.11)),
            TextAlignment = TextAlignment.Center,
            FontFamily = new FontFamily("Segoe UI Semibold")
        };

        DockPanel.SetDock(header, Dock.Top);
        main.Children.Add(header);

        // --- Corps ---
        var body = new StackPanel
        {
            Margin = new Thickness(w * 0.06, h * 0.035, w * 0.06, h * 0.03)
        };

        if (config.LabelShowDesignation)
        {
            body.Children.Add(CreateText(
                article.Designation,
                AdaptiveFont(w, h, 0.07, 0.085),
                FontWeights.SemiBold,
                TextAlignment.Left,
                maxLines: 3));
        }

        if (config.LabelShowPrice || (config.LabelShowUnit && !string.IsNullOrWhiteSpace(article.UniteVente)))
        {
            var priceLine = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, h * 0.018, 0, 0)
            };

            if (config.LabelShowPrice)
            {
                priceLine.Children.Add(CreateText(
                    FormatPrice(article.PrixVente, config),
                    AdaptiveFont(w, h, 0.09, 0.11),
                    FontWeights.Bold,
                    TextAlignment.Left));
            }

            if (config.LabelShowUnit && !string.IsNullOrWhiteSpace(article.UniteVente))
            {
                priceLine.Children.Add(CreateText(
                    "  " + article.UniteVente,
                    AdaptiveFont(w, h, 0.055, 0.07),
                    FontWeights.Normal,
                    TextAlignment.Left));
            }

            body.Children.Add(priceLine);
        }

        if (config.LabelShowBarcode)
        {
            var barcode = CreateBarcodeImage(article, config, w * 0.88, h * 0.22);
            if (barcode is not null)
            {
                barcode.Margin = new Thickness(0, h * 0.025, 0, 0);
                body.Children.Add(barcode);

                body.Children.Add(CreateText(
                    BarcodeValue(article),
                    AdaptiveFont(w, h, 0.05, 0.06),
                    FontWeights.Normal,
                    TextAlignment.Center));
            }
        }

        main.Children.Add(body);
        root.Child = main;
        return root;
    }

    // ==================================================================
    //  Helpers
    // ==================================================================
    private static Border CreateRoot(double w, double h) => new()
    {
        Width = w,
        Height = h,
        Background = Brushes.White,
        BorderBrush = BlackBrush,
        BorderThickness = new Thickness(0.8),
        SnapsToDevicePixels = true
    };

    private static double AdaptiveFont(double width, double height, double widthFactor, double heightFactor)
    {
        double fromWidth = width * widthFactor;
        double fromHeight = height * heightFactor;
        return Math.Max(7.0, Math.Min(fromWidth, fromHeight));
    }

    private static TextBlock CreateText(
        string text,
        double fontSize,
        FontWeight weight,
        TextAlignment align,
        int maxLines = 1,
        Brush? brush = null)
    {
        var tb = new TextBlock
        {
            Text = text ?? string.Empty,
            FontSize = fontSize,
            FontWeight = weight,
            FontFamily = new FontFamily("Segoe UI"),
            TextAlignment = align,
            Foreground = brush ?? BlackBrush,
            TextWrapping = maxLines > 1 ? TextWrapping.Wrap : TextWrapping.NoWrap,
            TextTrimming = maxLines > 1 ? TextTrimming.None : TextTrimming.CharacterEllipsis,
            LineStackingStrategy = LineStackingStrategy.BlockLineHeight,
            LineHeight = fontSize * 1.28
        };

        if (maxLines > 1)
            tb.MaxHeight = fontSize * 1.28 * maxLines + 2;

        return tb;
    }

    private static FrameworkElement CreateSeparator(double width, double topMargin)
    {
        return new Border
        {
            Width = width,
            Height = 1,
            Background = BlackBrush,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, topMargin, 0, topMargin)
        };
    }

    private static FrameworkElement? CreateBarcodeImage(
        Article article,
        ConnectionConfig config,
        double widthDip,
        double heightDip,
        bool rotate90 = false)
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
