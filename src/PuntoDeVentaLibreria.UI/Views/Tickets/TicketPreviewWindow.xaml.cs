using System.IO;
using System.Printing;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace PuntoDeVentaLibreria.UI.Views.Tickets;

public partial class TicketPreviewWindow : Window
{
    private readonly string _textoTicket;
    private readonly string _numeroComprobante;
    private readonly string _impresoraNombre;
    private readonly int _anchoPapelMm;

    public TicketPreviewWindow(string textoTicket, string numeroComprobante, int anchoPapelMm = 80, string impresoraNombre = "")
    {
        InitializeComponent();

        _textoTicket = textoTicket;
        _numeroComprobante = string.IsNullOrWhiteSpace(numeroComprobante) ? "Comprobante" : numeroComprobante;
        _anchoPapelMm = anchoPapelMm;
        _impresoraNombre = impresoraNombre;

        TxtContenidoTicket.Text = _textoTicket;
        TxtAnchoBadge.Text = $"{_anchoPapelMm} mm";
        TxtInfoImpresora.Text = string.IsNullOrWhiteSpace(_impresoraNombre) 
            ? "Impresora: Diálogo / Predeterminada de Windows" 
            : $"Impresora asignada: {_impresoraNombre}";

        // Ajustar visualmente el ancho de la hoja simulada según sea 58mm u 80mm
        BorderPapelTicket.Width = _anchoPapelMm == 58 ? 320 : 420;
    }

    private void BtnImprimir_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var dialog = new PrintDialog();

            if (!string.IsNullOrWhiteSpace(_impresoraNombre))
            {
                try
                {
                    var server = new LocalPrintServer();
                    var queue = server.GetPrintQueue(_impresoraNombre);
                    if (queue != null)
                    {
                        dialog.PrintQueue = queue;
                    }
                }
                catch
                {
                    // Si no encuentra la cola con ese nombre exacto, continuará con la predeterminada
                }
            }

            if (dialog.ShowDialog() == true)
            {
                dialog.PrintVisual(BorderPapelTicket, $"Ticket_{_numeroComprobante}");
                MessageBox.Show(this, "El comprobante fue enviado exitosamente a la cola de impresión.", "Impresión Enviada", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"No se pudo imprimir el ticket: {ex.Message}", "Error de Impresión", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnGuardarCopia_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var safeComp = string.Join("_", _numeroComprobante.Split(Path.GetInvalidFileNameChars()));
            var saveDialog = new Microsoft.Win32.SaveFileDialog
            {
                FileName = $"Ticket_{safeComp}_{DateTime.Now:yyyyMMdd_HHmmss}.txt",
                Filter = "Archivo de Texto (*.txt)|*.txt|Todos los archivos (*.*)|*.*",
                DefaultExt = ".txt"
            };

            if (saveDialog.ShowDialog() == true)
            {
                File.WriteAllText(saveDialog.FileName, _textoTicket, Encoding.UTF8);
                MessageBox.Show(this, "Copia de ticket guardada exitosamente.", "Archivo Guardado", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Error al guardar archivo: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnCerrar_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
