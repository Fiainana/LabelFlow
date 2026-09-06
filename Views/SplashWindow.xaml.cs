using System.Windows;

namespace LabelFlow.Views;

public partial class SplashWindow : Window
{
    public SplashWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        TxtLoading.Text = "Initialisation...";
        await Task.Delay(700);
        TxtLoading.Text = "Chargement de la configuration...";
        await Task.Delay(600);
        TxtLoading.Text = "Préparation de l'interface...";
        await Task.Delay(500);
        var main = new MainWindow();
        main.Show();
        Close();
    }
}
