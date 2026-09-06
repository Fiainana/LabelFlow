using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using LabelFlow.Models;
using LabelFlow.Services;

namespace LabelFlow.Views;

public partial class PrintPreparationWindow : Window
{
    private readonly ObservableCollection<PrintItem> _items;
    private readonly ConnectionConfig _config;

    public PrintPreparationWindow(IReadOnlyList<Article> articles, ConnectionConfig config)
    {
        InitializeComponent();
        _config = config;
        _items = new ObservableCollection<PrintItem>(articles.Select(a => new PrintItem { Article = a, Quantity = 1 }));
        foreach (var item in _items) item.PropertyChanged += OnItemPropertyChanged;
        DgItems.ItemsSource = _items;
        UpdateTotal();
    }

    private void OnItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PrintItem.Quantity)) UpdateTotal();
    }

    private void UpdateTotal()
    {
        int total = _items.Sum(i => Math.Max(0, i.Quantity));
        TxtTotal.Text = total switch { 0 => "0 étiquette", 1 => "1 étiquette au total", _ => $"{total} étiquettes au total" };
    }

    private IReadOnlyList<PrintItem> GetValidItems()
    {
        var list = _items.Where(i => i.Quantity > 0).Select(i => new PrintItem { Article = i.Article, Quantity = i.Quantity }).ToList();
        if (list.Count == 0) throw new InvalidOperationException("Définissez au moins une quantité supérieure à 0.");
        return list;
    }

    private void OnResetQtyClick(object sender, RoutedEventArgs e)
    {
        foreach (var item in _items) item.Quantity = 1;
        DgItems.Items.Refresh(); UpdateTotal();
    }

    private void OnAddOneClick(object sender, RoutedEventArgs e)
    {
        foreach (var item in _items) item.Quantity = Math.Max(1, item.Quantity) + 1;
        DgItems.Items.Refresh(); UpdateTotal();
    }

    private void OnPreviewClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var expanded = LabelPrintService.ExpandQuantities(GetValidItems());
            new PrintPreviewWindow(expanded, _config) { Owner = this }.ShowDialog();
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "LabelFlow", MessageBoxButton.OK, MessageBoxImage.Warning); }
    }

    private void OnPrintClick(object sender, RoutedEventArgs e)
    {
        try
        {
            if (LabelPrintService.Print(GetValidItems(), _config, showDialog: true))
            { DialogResult = true; Close(); }
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "LabelFlow", MessageBoxButton.OK, MessageBoxImage.Warning); }
    }

    private void OnCancelClick(object sender, RoutedEventArgs e) => Close();
}
