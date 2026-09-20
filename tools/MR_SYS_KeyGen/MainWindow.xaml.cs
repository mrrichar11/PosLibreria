using System.Windows;
using PuntoDeVentaLibreria.Application.Services;

namespace MR_SYS_KeyGen;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void BtnPegar_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var texto = Clipboard.GetText();
            if (!string.IsNullOrWhiteSpace(texto))
            {
                // Limpiar si vino en formato "ID de Máquina: LIB-..."
                if (texto.Contains("ID de Máquina:", StringComparison.OrdinalIgnoreCase))
                {
                    var partes = texto.Split("ID de Máquina:", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                    if (partes.Length > 1)
                        texto = partes[1].Trim();
                }
                else if (texto.Contains("ID:", StringComparison.OrdinalIgnoreCase))
                {
                    var partes = texto.Split("ID:", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                    if (partes.Length > 1)
                        texto = partes[1].Trim();
                }

                // Extraer solo LIB-XXXX-XXXX-... si hay texto alrededor
                var idx = texto.IndexOf("LIB-", StringComparison.OrdinalIgnoreCase);
                if (idx >= 0 && texto.Length >= idx + 24)
                {
                    texto = texto.Substring(idx, 24).Trim();
                }

                TxtCodigoInstalacion.Text = texto.Trim();
            }
        }
        catch { }
    }

    private void BtnGenerar_Click(object sender, RoutedEventArgs e)
    {
        var codigo = TxtCodigoInstalacion.Text.Trim();
        if (string.IsNullOrWhiteSpace(codigo))
        {
            MessageBox.Show("Por favor, ingrese o pegue el ID de Máquina del cliente.", "Falta Información", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        string plan = CmbPlan.SelectedIndex == 0 ? "PRO" : "ESTANDAR";
        int dias = CmbDuracion.SelectedIndex switch
        {
            0 => 30,
            1 => 90,
            2 => 180,
            3 => 365,
            _ => 365
        };

        try
        {
            var clave = LicenseCryptography.GenerarClave(codigo, plan, dias);
            TxtClaveGenerada.Text = clave;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al generar clave: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnCopiarClave_Click(object sender, RoutedEventArgs e)
    {
        var clave = TxtClaveGenerada.Text.Trim();
        if (string.IsNullOrWhiteSpace(clave)) return;

        Clipboard.SetText(clave);
        MessageBox.Show("¡Clave copiada al portapapeles!", "MR SYS KeyGen", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void BtnCopiarWhatsApp_Click(object sender, RoutedEventArgs e)
    {
        var clave = TxtClaveGenerada.Text.Trim();
        if (string.IsNullOrWhiteSpace(clave)) return;

        var comercio = string.IsNullOrWhiteSpace(TxtNombreComercio.Text) ? "tu negocio" : TxtNombreComercio.Text.Trim();
        string planNombre = CmbPlan.SelectedIndex == 0 ? "Plan PRO Multi-Terminal" : "Plan Estándar";
        int dias = CmbDuracion.SelectedIndex switch
        {
            0 => 30,
            1 => 90,
            2 => 180,
            3 => 365,
            _ => 365
        };

        var mensaje = $"¡Hola! Acá tenés tu clave de activación para {comercio} ({planNombre} por {dias} días):\n\n👉 {clave}\n\nPara activarla:\n1. Entrá a Configuración > Plan & Licencia.\n2. Pegá la clave en 'Activar o Renovar Clave'.\n3. Hacé clic en 'Activar Clave'.\n\n¡Cualquier consulta quedo a tu disposición!";

        Clipboard.SetText(mensaje);
        MessageBox.Show("¡Mensaje completo para WhatsApp copiado al portapapeles!\n\nListo para pegar en el chat del cliente.", "MR SYS KeyGen", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
