using System.Windows;
using System.Windows.Input;
using PuntoDeVentaLibreria.UI.ViewModels;

namespace PuntoDeVentaLibreria.UI.Views.Login;

public partial class LoginWindow : Window
{
    public LoginViewModel ViewModel { get; }

    public LoginWindow(LoginViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        DataContext = ViewModel;

        ViewModel.OnLoginExitoso += () =>
        {
            DialogResult = true;
            Close();
        };

        ViewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(ViewModel.MensajeError))
            {
                BorderError.Visibility = string.IsNullOrWhiteSpace(ViewModel.MensajeError) ? Visibility.Collapsed : Visibility.Visible;
            }
        };

        Loaded += async (s, e) =>
        {
            await ViewModel.InicializarAsync();
            TxtPassword.Focus();
        };

        KeyDown += (s, e) =>
        {
            if (e.Key == Key.Enter)
            {
                BtnIngresar_Click(this, new RoutedEventArgs());
                e.Handled = true;
            }
        };
    }

    private async void BtnIngresar_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.Password = TxtPassword.Password;
        await ViewModel.IniciarSesionCommand.ExecuteAsync(null);
    }
}
