using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace LabelFlow.Models;

public sealed class Article : INotifyPropertyChanged
{
    private bool _isSelected;

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value) return;
            _isSelected = value;
            OnPropertyChanged();
        }
    }

    public string Reference { get; init; } = string.Empty;
    public string Designation { get; init; } = string.Empty;
    public string? CodeBarre { get; init; }
    public decimal PrixVente { get; init; }
    public string? UniteVente { get; init; }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
