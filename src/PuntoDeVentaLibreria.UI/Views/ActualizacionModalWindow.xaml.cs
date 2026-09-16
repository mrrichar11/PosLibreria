using System.Windows;
using PuntoDeVentaLibreria.Application.DTOs.Sistema;
using PuntoDeVentaLibreria.Application.Services;

namespace PuntoDeVentaLibreria.UI.Views;

public partial class ActualizacionModalWindow : Window
{
    private readonly ActualizacionDto _info;
    private readonly IUpdateService _updateService;
    private readonly CancellationTokenSource _cts = new();

    public ActualizacionModalWindow(ActualizacionDto info, IUpdateService updateService)
    {
        InitializeComponent();
        _info = info ?? throw new ArgumentNullException(nameof(info));
        _updateService = updateService ?? throw new ArgumentNullException(nameof(updateService));

        CargarInformacion();
    }

    private void CargarInformacion()
    {
        TxtVersionActual.Text = $"v{_info.VersionActual}";
        TxtVersionDisponible.Text = $"v{_info.VersionDisponible}";

        if (!string.IsNullOrWhiteSpace(_info.NotasLanzamiento))
        {
            TxtNotasLanzamiento.Text = _info.NotasLanzamiento;
        }
        else
        {
            TxtNotasLanzamiento.Text = "Esta versión incluye optimizaciones generales de rendimiento, estabilidad en base de datos y nuevas funciones comerciales para la librería.";
        }
    }

    private void BtnMasTarde_Click(object sender, RoutedEventArgs e)
    {
        _cts.Cancel();
        Close();
    }

    private async void BtnActualizar_Click(object sender, RoutedEventArgs e)
    {
        BtnActualizar.IsEnabled = false;
        BtnMasTarde.IsEnabled = false;
        TxtError.Visibility = Visibility.Collapsed;
        PanelProgreso.Visibility = Visibility.Visible;

        TxtEstadoProgreso.Text = "Iniciando descarga segura...";
        ProgresoDescarga.Value = 0;

        try
        {
            var progreso = new Progress<ProgresoDescargaDto>(p =>
            {
                ProgresoDescarga.Value = p.Porcentaje;
                TxtEstadoProgreso.Text = p.Mensaje;

                if (p.TotalBytes.HasValue && p.TotalBytes.Value > 0)
                {
                    var mbRecibidos = p.BytesRecibidos / (1024.0 * 1024.0);
                    var mbTotal = p.TotalBytes.Value / (1024.0 * 1024.0);
                    TxtDetalleBytes.Text = $"{mbRecibidos:F1} MB / {mbTotal:F1} MB";
                }
            });

            var rutaZip = await _updateService.DescargarActualizacionAsync(_info, progreso, _cts.Token);

            TxtEstadoProgreso.Text = "Resguardando base de datos y aplicando actualización...";
            ProgresoDescarga.Value = 100;
            TxtDetalleBytes.Text = "Reiniciando sistema...";

            await Task.Delay(500);
            _updateService.IniciarInstalacion(rutaZip);
        }
        catch (Exception ex)
        {
            TxtError.Text = $"Error en la actualización: {ex.Message}";
            TxtError.Visibility = Visibility.Visible;
            PanelProgreso.Visibility = Visibility.Collapsed;
            BtnActualizar.IsEnabled = true;
            BtnMasTarde.IsEnabled = true;
        }
    }
}
