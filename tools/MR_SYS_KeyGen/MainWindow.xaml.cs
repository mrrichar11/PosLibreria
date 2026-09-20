using System.Windows;
using PuntoDeVentaLibreria.Application.Services;

namespace MR_SYS_KeyGen;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private string LimpiarCodigoInstalacion(string texto)
    {
        if (string.IsNullOrWhiteSpace(texto)) return string.Empty;

        var limpio = texto.Trim();

        // Si contiene formato "ID de Máquina: LIB-..." o "ID: LIB-..."
        if (limpio.Contains("ID de Máquina:", StringComparison.OrdinalIgnoreCase))
        {
            var partes = limpio.Split("ID de Máquina:", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (partes.Length > 1)
                limpio = partes[1].Trim();
        }
        else if (limpio.Contains("ID:", StringComparison.OrdinalIgnoreCase))
        {
            var partes = limpio.Split("ID:", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (partes.Length > 1)
                limpio = partes[1].Trim();
        }

        // Buscar patrón LIB-XXXX-XXXX-XXXX-XXXX
        var idx = limpio.IndexOf("LIB-", StringComparison.OrdinalIgnoreCase);
        if (idx >= 0 && limpio.Length >= idx + 24)
        {
            limpio = limpio.Substring(idx, 24).Trim();
        }

        return limpio.Trim();
    }

    private void BtnPegar_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var texto = Clipboard.GetText();
            if (!string.IsNullOrWhiteSpace(texto))
            {
                TxtCodigoInstalacion.Text = LimpiarCodigoInstalacion(texto);
            }
        }
        catch { }
    }

    private void BtnGenerar_Click(object sender, RoutedEventArgs e)
    {
        var codigo = LimpiarCodigoInstalacion(TxtCodigoInstalacion.Text);
        if (string.IsNullOrWhiteSpace(codigo))
        {
            MessageBox.Show("Por favor, ingrese o pegue el ID de Máquina del cliente (ej: LIB-ADE3-4C83-62AA-E72B).", "Falta ID de Máquina", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        TxtCodigoInstalacion.Text = codigo;

        string plan = (CmbPlan.SelectedIndex == 1) ? "ESTANDAR" : "PRO";
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

            // Copiar automáticamente al portapapeles
            Clipboard.SetText(clave);

            MessageBox.Show(
                $"¡Clave de activación generada con éxito!\n\n🔑 Clave: {clave}\n\n📋 Ha sido copiada automáticamente a tu portapapeles.",
                "MR SYS · Clave Generada",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al generar la clave: {ex.Message}", "Error en KeyGen", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnCopiarClave_Click(object sender, RoutedEventArgs e)
    {
        var clave = TxtClaveGenerada.Text.Trim();
        if (string.IsNullOrWhiteSpace(clave))
        {
            MessageBox.Show("Primero genere una clave de activación.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        Clipboard.SetText(clave);
        MessageBox.Show("¡Clave copiada al portapapeles!", "MR SYS KeyGen", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void BtnCopiarWhatsApp_Click(object sender, RoutedEventArgs e)
    {
        var clave = TxtClaveGenerada.Text.Trim();
        if (string.IsNullOrWhiteSpace(clave))
        {
            MessageBox.Show("Primero genere una clave de activación.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var comercio = string.IsNullOrWhiteSpace(TxtNombreComercio.Text) ? "tu negocio" : TxtNombreComercio.Text.Trim();
        string planNombre = (CmbPlan.SelectedIndex == 1) ? "Plan Estándar" : "Plan PRO Multi-Terminal";
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
