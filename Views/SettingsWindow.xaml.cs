using System.Globalization;
using System.Windows;
using System.Windows.Media;
using LabelFlow.Models;
using LabelFlow.Services;
using Microsoft.Data.SqlClient;

namespace LabelFlow.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
        LoadPrinters(); LoadConfig(); UpdateAuthFields();
    }

    private void LoadPrinters()
    {
        CmbPrinter.Items.Clear();
        CmbPrinter.Items.Add("(Imprimante système par défaut)");
        foreach (var name in PrinterService.GetInstalledPrinterNames()) CmbPrinter.Items.Add(name);
        CmbPrinter.SelectedIndex = 0;
    }

    private void LoadConfig()
    {
        var config = ConfigService.Load();
        if (config is null) return;
        TxtServer.Text = config.Server; TxtDatabase.Text = config.Database;
        RbWindows.IsChecked = config.UseWindowsAuth; RbSql.IsChecked = !config.UseWindowsAuth;
        if (!config.UseWindowsAuth)
        {
            TxtUser.Text = config.User ?? string.Empty;
            var password = ConfigService.DecryptPassword(config);
            if (!string.IsNullOrEmpty(password)) TxtPassword.Password = password;
        }
        TxtLabelWidth.Text = config.LabelWidthMm.ToString("0.##", CultureInfo.InvariantCulture);
        TxtLabelHeight.Text = config.LabelHeightMm.ToString("0.##", CultureInfo.InvariantCulture);
        RbCode128.IsChecked = config.BarcodeType == BarcodeType.Code128;
        RbCode39.IsChecked = config.BarcodeType == BarcodeType.Code39;
        RbDesignClassic.IsChecked = config.LabelDesign == LabelDesign.Classic;
        RbDesignCompact.IsChecked = config.LabelDesign == LabelDesign.Compact;
        RbDesignProduct.IsChecked = config.LabelDesign == LabelDesign.ProductSheet;
        RbDesignIndustrial.IsChecked = config.LabelDesign == LabelDesign.Industrial;
        RbDesignRetail.IsChecked = config.LabelDesign == LabelDesign.Retail;
        ChkLabelRef.IsChecked = config.LabelShowReference;
        ChkLabelDesignation.IsChecked = config.LabelShowDesignation;
        ChkLabelBarcode.IsChecked = config.LabelShowBarcode;
        ChkLabelPrice.IsChecked = config.LabelShowPrice;
        ChkLabelUnit.IsChecked = config.LabelShowUnit;
        ChkShowDecimals.IsChecked = config.ShowPriceDecimals;
        if (!string.IsNullOrWhiteSpace(config.PreferredPrinterName))
            for (int i = 0; i < CmbPrinter.Items.Count; i++)
                if (string.Equals(CmbPrinter.Items[i]?.ToString(), config.PreferredPrinterName, StringComparison.OrdinalIgnoreCase))
                { CmbPrinter.SelectedIndex = i; break; }
    }

    private void OnAuthTypeChanged(object sender, RoutedEventArgs e) => UpdateAuthFields();

    private void UpdateAuthFields()
    {
        if (RbSql is null || RbWindows is null || TxtUser is null || TxtPassword is null || LblUser is null || LblPassword is null)
            return;

        bool sql = RbSql.IsChecked == true;

        TxtUser.IsEnabled = sql;
        TxtPassword.IsEnabled = sql;
        LblUser.IsEnabled = sql;
        LblPassword.IsEnabled = sql;

        if (!sql)
        {
            TxtUser.Text = string.Empty;
            TxtPassword.Password = string.Empty;
        }
    }

    private LabelDesign GetSelectedDesign()
    {
        if (RbDesignCompact.IsChecked == true) return LabelDesign.Compact;
        if (RbDesignProduct.IsChecked == true) return LabelDesign.ProductSheet;
        if (RbDesignIndustrial.IsChecked == true) return LabelDesign.Industrial;
        if (RbDesignRetail.IsChecked == true) return LabelDesign.Retail;
        return LabelDesign.Classic;
    }

    private string? GetSelectedPrinter() => CmbPrinter.SelectedIndex <= 0 ? null : CmbPrinter.SelectedItem?.ToString();

    private ConnectionConfig BuildConfig()
    {
        if (!TryParsePositive(TxtLabelWidth.Text, out double width)) throw new InvalidOperationException("Largeur invalide.");
        if (!TryParsePositive(TxtLabelHeight.Text, out double height)) throw new InvalidOperationException("Hauteur invalide.");
        bool any = ChkLabelRef.IsChecked == true || ChkLabelDesignation.IsChecked == true || ChkLabelBarcode.IsChecked == true || ChkLabelPrice.IsChecked == true || ChkLabelUnit.IsChecked == true;
        if (!any) throw new InvalidOperationException("Sélectionnez au moins un champ étiquette.");
        return new ConnectionConfig
        {
            Server = TxtServer.Text.Trim(), Database = TxtDatabase.Text.Trim(),
            UseWindowsAuth = RbWindows.IsChecked == true,
            User = RbSql.IsChecked == true ? TxtUser.Text.Trim() : null,
            LabelWidthMm = width, LabelHeightMm = height,
            BarcodeType = RbCode39.IsChecked == true ? BarcodeType.Code39 : BarcodeType.Code128,
            LabelDesign = GetSelectedDesign(),
            LabelShowReference = ChkLabelRef.IsChecked == true,
            LabelShowDesignation = ChkLabelDesignation.IsChecked == true,
            LabelShowBarcode = ChkLabelBarcode.IsChecked == true,
            LabelShowPrice = ChkLabelPrice.IsChecked == true,
            LabelShowUnit = ChkLabelUnit.IsChecked == true,
            ShowPriceDecimals = ChkShowDecimals.IsChecked == true,
            CurrencySymbol = "Ar", PreferredPrinterName = GetSelectedPrinter()
        };
    }

    private static bool TryParsePositive(string? text, out double value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(text)) return false;
        if (!double.TryParse(text.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out value)) return false;
        return value > 0 && value <= 500;
    }

    private async void OnTestClick(object sender, RoutedEventArgs e)
    {
        SetStatus("Test en cours...", false); BtnTest.IsEnabled = false;
        try
        {
            var config = BuildConfig();
            string? password = RbSql.IsChecked == true ? TxtPassword.Password : null;
            await using var connection = SqlConnectionFactory.Create(config, password);
            await connection.OpenAsync();
            await using var cmd = new SqlCommand("SELECT @@VERSION", connection);
            var version = (await cmd.ExecuteScalarAsync())?.ToString() ?? string.Empty;
            SetStatus($"Connexion réussie\n{connection.DataSource} / {connection.Database}\n{version.Split('\n')[0]}", false);
        }
        catch (Exception ex) { SetStatus($"Échec\n{ex.Message}", true); }
        finally { BtnTest.IsEnabled = true; }
    }

    private void OnSaveClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var config = BuildConfig();
            string? password = null;
            if (RbSql.IsChecked == true)
            {
                if (!string.IsNullOrEmpty(TxtPassword.Password)) password = TxtPassword.Password;
                else { var existing = ConfigService.Load(); if (existing is not null) config.EncryptedPassword = existing.EncryptedPassword; }
            }
            ConfigService.Save(config, password);
            MessageBox.Show("Paramètres enregistrés.", "LabelFlow", MessageBoxButton.OK, MessageBoxImage.Information);
            DialogResult = true; Close();
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "LabelFlow", MessageBoxButton.OK, MessageBoxImage.Warning); }
    }

    private void SetStatus(string message, bool isError)
    {
        TxtStatus.Text = message;
        if (isError) { TxtStatus.Foreground = new SolidColorBrush(Color.FromRgb(185, 28, 28)); StatusBorder.Background = new SolidColorBrush(Color.FromRgb(254, 226, 226)); }
        else if (message.StartsWith("Connexion réussie")) { TxtStatus.Foreground = new SolidColorBrush(Color.FromRgb(21, 128, 61)); StatusBorder.Background = new SolidColorBrush(Color.FromRgb(220, 252, 231)); }
        else { TxtStatus.Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128)); StatusBorder.Background = new SolidColorBrush(Color.FromRgb(243, 244, 246)); }
    }
}
