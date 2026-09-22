using System.Windows;
using System.Windows.Input;
using PuntoDeVentaLibreria.Application.DTOs.Inventario;

namespace PuntoDeVentaLibreria.UI.Views.Pos;

public partial class SeleccionarVarianteModalWindow : Window
{
    public ArticuloVarianteDto? VarianteSeleccionada { get; private set; }
    public bool SeleccionConfirmada { get; private set; }

    public SeleccionarVarianteModalWindow(ArticuloDto articulo)
    {
        InitializeComponent();

        TxtTituloArticulo.Text = articulo.Nombre;
        TxtPrecioVenta.Text = $"$ {articulo.PrecioVenta:N2}";

        var variantes = articulo.Variantes ?? new List<ArticuloVarianteDto>();
        DgVariantes.ItemsSource = variantes;

        if (variantes.Count > 0)
        {
            DgVariantes.SelectedIndex = 0;
        }

        Loaded += (s, e) =>
        {
            DgVariantes.Focus();
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
                BtnSeleccionar_Click(this, new RoutedEventArgs());
                e.Handled = true;
            }
        };
    }

    private void DgVariantes_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        BtnSeleccionar_Click(sender, e);
    }

    private void BtnSeleccionar_Click(object sender, RoutedEventArgs e)
    {
        if (DgVariantes.SelectedItem is ArticuloVarianteDto seleccionado)
        {
            VarianteSeleccionada = seleccionado;
            SeleccionConfirmada = true;
            DialogResult = true;
            Close();
        }
        else
        {
            MessageBox.Show("Por favor seleccione un color o variante de la lista.", "MR SYS", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void BtnCancelar_Click(object sender, RoutedEventArgs e)
    {
        VarianteSeleccionada = null;
        SeleccionConfirmada = false;
        DialogResult = false;
        Close();
    }
}
