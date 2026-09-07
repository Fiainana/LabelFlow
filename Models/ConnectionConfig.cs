using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace LabelFlow.Models;

public enum BarcodeType
{
    Code128 = 0,
    Code39 = 1
}

public enum LabelDesign
{
    Classic = 0,
    Compact = 1,
    ProductSheet = 2,
    Industrial = 3,
    Retail = 4   // Grand magasin / superette
}

public sealed class ConnectionConfig
{
    public string Server { get; set; } = string.Empty;
    public string Database { get; set; } = string.Empty;
    public bool UseWindowsAuth { get; set; } = true;
    public string? User { get; set; }
    public string? EncryptedPassword { get; set; }

    public double LabelWidthMm { get; set; } = 50;
    public double LabelHeightMm { get; set; } = 30;

    public BarcodeType BarcodeType { get; set; } = BarcodeType.Code128;
    public LabelDesign LabelDesign { get; set; } = LabelDesign.Classic;

    public bool LabelShowReference { get; set; } = true;
    public bool LabelShowDesignation { get; set; } = true;
    public bool LabelShowBarcode { get; set; } = true;
    public bool LabelShowPrice { get; set; } = true;
    public bool LabelShowUnit { get; set; } = false;

    public bool ShowPriceDecimals { get; set; } = true;
    public string CurrencySymbol { get; set; } = "Ar";

    public string? PreferredPrinterName { get; set; }
}

public sealed class PrintItem : INotifyPropertyChanged
{
    private int _quantity = 1;

    public Article Article { get; init; } = null!;

    public int Quantity
    {
        get => _quantity;
        set
        {
            int v = value < 0 ? 0 : value > 9999 ? 9999 : value;
            if (_quantity == v) return;
            _quantity = v;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
