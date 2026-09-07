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
        TxtLoading.Text = "Connexion aux services d'impression...";
        await Task.Delay(700);

        TxtLoading.Text = "Chargement de la configuration...";
        await Task.Delay(550);

        TxtLoading.Text = "Préparation de l'interface...";
        await Task.Delay(450);

        var main = new MainWindow();
        main.Show();
        Close();
    }
}
