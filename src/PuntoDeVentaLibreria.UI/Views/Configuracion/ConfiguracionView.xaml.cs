using System.Windows;
using System.Windows.Controls;
using PuntoDeVentaLibreria.UI.ViewModels;
using PuntoDeVentaLibreria.UI.Views.Tickets;

namespace PuntoDeVentaLibreria.UI.Views.Configuracion;

public partial class ConfiguracionView : UserControl
{
    public ConfiguracionViewModel ViewModel { get; }

    public ConfiguracionView(ConfiguracionViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        DataContext = ViewModel;

        ViewModel.SolicitarVistaPreviaTicket = (texto, comprobante, ancho, impresora) =>
        {
            var modal = new TicketPreviewWindow(texto, comprobante, ancho, impresora)
            {
                Owner = Window.GetWindow(this)
            };
            modal.ShowDialog();
            return Task.CompletedTask;
        };

        Loaded += async (s, e) =>
        {
            await ViewModel.CargarDatosAsync();
        };
    }

    private void CmbPais_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CmbPais.SelectedItem is string pais)
        {
            ViewModel.SeleccionarPais(pais);
        }
    }
}
