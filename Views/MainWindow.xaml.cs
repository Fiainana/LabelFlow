using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;
using LabelFlow.Models;
using LabelFlow.Services;

namespace LabelFlow.Views;

public partial class MainWindow : Window
{
    private readonly ObservableCollection<Article> _articles = new();
    private readonly List<Article> _loadedArticles = new();
    private readonly HashSet<string> _selectedReferences = new(StringComparer.OrdinalIgnoreCase);
    private int _currentPage, _totalCount;
    private bool _hasMore = true, _isLoading, _showSelectedOnly, _suppressSelectionSync, _showPriceDecimals = true;
    private string _currentSearch = string.Empty;
    private DispatcherTimer? _searchDebounceTimer;

    public MainWindow()
    {
        InitializeComponent();
        DgArticles.ItemsSource = _articles;
        ApplyPriceFormatFromConfig();
        UpdateNavbarStatus();
        Loaded += async (_, _) => await ResetAndLoadAsync(keepSelection: true);
    }

    private void UpdateNavbarStatus()
    {
        var config = ConfigService.Load();
        if (config is null || string.IsNullOrWhiteSpace(config.Database))
        {
            TxtDatabaseName.Text = "—";
            TxtConnectionStatus.Text = "Non configuré";
            StatusDot.Fill = new SolidColorBrush(Color.FromRgb(148, 163, 184)); // gris
            return;
        }

        TxtDatabaseName.Text = config.Database.ToUpperInvariant();
        TxtConnectionStatus.Text = "Connecté";
        StatusDot.Fill = new SolidColorBrush(Color.FromRgb(34, 197, 94)); // vert
    }

    private void ApplyPriceFormatFromConfig()
    {
        var config = ConfigService.Load();
        _showPriceDecimals = config?.ShowPriceDecimals ?? true;
        string currency = config?.CurrencySymbol ?? "Ar";
        foreach (var column in DgArticles.Columns)
        {
            if (column is DataGridTextColumn textCol && textCol.Header?.ToString()?.Contains("PRIX", StringComparison.OrdinalIgnoreCase) == true)
            {
                string numberFormat = _showPriceDecimals ? "N2" : "N0";
                textCol.Binding = new Binding(nameof(Article.PrixVente))
                {
                    StringFormat = "{0:" + numberFormat + "} " + currency,
                    ConverterCulture = CultureInfo.CurrentCulture
                };
            }
        }
        DgArticles.Items.Refresh();
    }

    private async Task ResetAndLoadAsync(bool keepSelection = true)
    {
        ApplyPriceFormatFromConfig();
        UpdateNavbarStatus();
        if (_showSelectedOnly) { await LoadSelectedOnlyAsync(); return; }
        _currentPage = 0; _hasMore = true; _totalCount = 0;
        if (!keepSelection) _selectedReferences.Clear();
        ClearList();
        await LoadNextPageAsync();
    }

    private async Task LoadSelectedOnlyAsync()
    {
        if (_isLoading) return;
        _isLoading = true; LoadingMorePanel.Visibility = Visibility.Collapsed;
        try
        {
            ShowGrid(); TxtCount.Text = "Chargement..."; ClearList();
            if (_selectedReferences.Count == 0)
            {
                ShowEmptyState("Aucun article sélectionné", "Cochez des articles puis activez « Sélection uniquement ».");
                TxtCount.Text = "0 article"; UpdateSelectionCount(); return;
            }
            var selected = await ArticleService.GetByReferencesAsync(_selectedReferences);
            _suppressSelectionSync = true;
            foreach (var article in selected)
            {
                article.IsSelected = true;
                article.PropertyChanged += OnArticlePropertyChanged;
                _loadedArticles.Add(article); _articles.Add(article);
            }
            _suppressSelectionSync = false; _hasMore = false; _totalCount = selected.Count;
            UpdateCountText(); UpdateSelectionCount(); ShowGrid();
        }
        catch (Exception ex) { ShowEmptyState("Impossible de charger la sélection", ex.Message); TxtCount.Text = string.Empty; }
        finally { _isLoading = false; _suppressSelectionSync = false; }
    }

    private async Task LoadNextPageAsync()
    {
        if (_showSelectedOnly || _isLoading || !_hasMore) return;
        _isLoading = true;
        LoadingMorePanel.Visibility = _currentPage > 0 ? Visibility.Visible : Visibility.Collapsed;
        try
        {
            if (_currentPage == 0) { ShowGrid(); TxtCount.Text = "Chargement..."; }
            var result = await ArticleService.GetActiveArticlesPageAsync(_currentPage, string.IsNullOrWhiteSpace(_currentSearch) ? null : _currentSearch);
            _totalCount = result.TotalCount; _hasMore = result.HasMore; _suppressSelectionSync = true;
            foreach (var article in result.Articles)
            {
                article.IsSelected = _selectedReferences.Contains(article.Reference);
                article.PropertyChanged += OnArticlePropertyChanged;
                _loadedArticles.Add(article); _articles.Add(article);
            }
            _suppressSelectionSync = false; _currentPage++;
            UpdateCountText(); UpdateSelectionCount();
            if (_articles.Count == 0)
                ShowEmptyState("Aucun article", string.IsNullOrWhiteSpace(_currentSearch) ? "Aucun article actif trouvé." : "Aucun résultat.");
            else ShowGrid();
        }
        catch (Exception ex)
        {
            if (_articles.Count == 0) { ShowEmptyState("Impossible de charger les articles", ex.Message); TxtCount.Text = string.Empty; }
            else MessageBox.Show(ex.Message, "LabelFlow", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        finally { _isLoading = false; _suppressSelectionSync = false; LoadingMorePanel.Visibility = Visibility.Collapsed; }
    }

    private void OnArticlesScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (_showSelectedOnly || e.VerticalChange <= 0) return;
        if (e.VerticalOffset + e.ViewportHeight >= e.ExtentHeight * 0.8) _ = LoadNextPageAsync();
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_showSelectedOnly) return;
        _searchDebounceTimer?.Stop();
        _searchDebounceTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(350) };
        _searchDebounceTimer.Tick += async (_, _) =>
        {
            _searchDebounceTimer.Stop();
            _currentSearch = TxtSearch.Text?.Trim() ?? string.Empty;
            await ResetAndLoadAsync(keepSelection: true);
        };
        _searchDebounceTimer.Start();
    }

    private async void OnRefreshClick(object sender, RoutedEventArgs e) => await ResetAndLoadAsync(keepSelection: true);

    private async void OnShowSelectedOnlyClick(object sender, RoutedEventArgs e)
    {
        _showSelectedOnly = BtnShowSelectedOnly.IsChecked == true;
        TxtSearch.IsEnabled = !_showSelectedOnly;
        if (_showSelectedOnly) await LoadSelectedOnlyAsync();
        else await ResetAndLoadAsync(keepSelection: true);
    }

    private void OnSelectAllClick(object sender, RoutedEventArgs e)
    {
        _suppressSelectionSync = true;
        foreach (var a in _articles) { a.IsSelected = true; _selectedReferences.Add(a.Reference); }
        _suppressSelectionSync = false; UpdateSelectionCount();
    }

    private async void OnDeselectAllClick(object sender, RoutedEventArgs e)
    {
        _suppressSelectionSync = true; _selectedReferences.Clear();
        foreach (var a in _loadedArticles) a.IsSelected = false;
        _suppressSelectionSync = false; UpdateSelectionCount();
        if (_showSelectedOnly) await LoadSelectedOnlyAsync();
    }

    private void OnSettingsClick(object sender, RoutedEventArgs e)
    {
        if (new SettingsWindow { Owner = this }.ShowDialog() == true)
            _ = ResetAndLoadAsync(keepSelection: true);
    }

    private async void OnPrintClick(object sender, RoutedEventArgs e)
    {
        if (_selectedReferences.Count == 0) return;
        var config = ConfigService.Load();
        if (config is null)
        {
            MessageBox.Show("Configurez d'abord la connexion et les paramètres.", "LabelFlow", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        try
        {
            BtnPrint.IsEnabled = false;
            var articles = await ArticleService.GetByReferencesAsync(_selectedReferences);
            if (articles.Count == 0) { MessageBox.Show("Aucun article trouvé.", "LabelFlow", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            new PrintPreparationWindow(articles, config) { Owner = this }.ShowDialog();
        }
        catch (Exception ex) { MessageBox.Show($"Erreur :\n{ex.Message}", "LabelFlow", MessageBoxButton.OK, MessageBoxImage.Error); }
        finally { UpdateSelectionCount(); }
    }

    private void OnArticlePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_suppressSelectionSync || e.PropertyName != nameof(Article.IsSelected) || sender is not Article article) return;
        if (article.IsSelected) _selectedReferences.Add(article.Reference); else _selectedReferences.Remove(article.Reference);
        UpdateSelectionCount();
        if (_showSelectedOnly && !article.IsSelected)
        {
            article.PropertyChanged -= OnArticlePropertyChanged;
            _articles.Remove(article); _loadedArticles.Remove(article);
            _totalCount = _articles.Count; UpdateCountText();
            if (_articles.Count == 0) ShowEmptyState("Aucun article sélectionné", "Cochez des articles puis activez « Sélection uniquement ».");
        }
    }

    private void ClearList() { UnsubscribeSelectionEvents(); _loadedArticles.Clear(); _articles.Clear(); }

    private void UpdateCountText()
    {
        if (_showSelectedOnly)
            TxtCount.Text = _articles.Count switch { 0 => "0 article sélectionné", 1 => "1 article sélectionné", _ => $"{_articles.Count} articles sélectionnés" };
        else
            TxtCount.Text = _totalCount == 0 ? "0 article" : $"{_articles.Count} / {_totalCount} article{(_totalCount > 1 ? "s" : "")}";
    }

    private void UpdateSelectionCount()
    {
        int count = _selectedReferences.Count;
        TxtSelectedCount.Text = count switch { 0 => "0 article sélectionné", 1 => "1 article sélectionné", _ => $"{count} articles sélectionnés" };
        BtnPrint.IsEnabled = count > 0;
    }

    private void ShowGrid() { EmptyState.Visibility = Visibility.Collapsed; DgArticles.Visibility = Visibility.Visible; }
    private void ShowEmptyState(string title, string message)
    {
        DgArticles.Visibility = Visibility.Collapsed; EmptyState.Visibility = Visibility.Visible;
        TxtEmptyTitle.Text = title; TxtEmptyMessage.Text = message;
    }
    private void UnsubscribeSelectionEvents() { foreach (var a in _loadedArticles) a.PropertyChanged -= OnArticlePropertyChanged; }
}
