using System.Windows;
using System.Windows.Input;
using PuntoDeVentaLibreria.Application.DTOs.Inventario;

namespace PuntoDeVentaLibreria.UI.Views.Pos;

public partial class SeleccionarArticuloModalWindow : Window
{
    public ArticuloDto? ArticuloSeleccionado { get; private set; }
    public bool SeleccionConfirmada { get; private set; }

    public SeleccionarArticuloModalWindow(IReadOnlyList<ArticuloDto> opciones, string query)
    {
        InitializeComponent();

        DgOpciones.ItemsSource = opciones;
        TxtInfoResultados.Text = $"Se encontraron {opciones.Count} artículos coincidentes con '{query}'. Seleccione el deseado:";

        if (opciones.Count > 0)
        {
            DgOpciones.SelectedIndex = 0;
        }

        Loaded += (s, e) =>
        {
            DgOpciones.Focus();
        };

        KeyDown += (s, e) =>
        {
            if (e.Key == Key.Escape)
            {
                BtnCancelar_Click(this, new RoutedEventArgs());
                e.Handled = true;
            }
            else if (e.Key == Key.Enter)
            {
                BtnAgregar_Click(this, new RoutedEventArgs());
                e.Handled = true;
            }
        };
    }

    private void DgOpciones_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        BtnAgregar_Click(sender, e);
    }

    private void BtnAgregar_Click(object sender, RoutedEventArgs e)
    {
        if (DgOpciones.SelectedItem is ArticuloDto seleccionado)
        {
            ArticuloSeleccionado = seleccionado;
            SeleccionConfirmada = true;
            DialogResult = true;
            Close();
        }
        else
        {
            MessageBox.Show("Por favor seleccione un artículo de la lista.", "MR SYS", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void BtnCancelar_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
