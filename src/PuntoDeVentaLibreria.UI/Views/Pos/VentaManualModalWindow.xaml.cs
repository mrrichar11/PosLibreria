using System.Windows;
using System.Windows.Input;

namespace PuntoDeVentaLibreria.UI.Views.Pos;

public partial class VentaManualModalWindow : Window
{
    public string Descripcion { get; private set; } = "Venta Mostrador";
    public decimal PrecioUnitario { get; private set; }
    public decimal Cantidad { get; private set; } = 1;
    public bool Confirmado { get; private set; }

    public VentaManualModalWindow()
    {
        InitializeComponent();

        Loaded += (s, e) =>
        {
            TxtPrecio.Focus();
            TxtPrecio.SelectAll();
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
                BtnConfirmar_Click(this, new RoutedEventArgs());
                e.Handled = true;
            }
        };
    }

    private void BtnConfirmar_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtDescripcion.Text))
        {
            MessageBox.Show("Ingrese una descripción.", "MR SYS", MessageBoxButton.OK, MessageBoxImage.Warning);
            TxtDescripcion.Focus();
            return;
        }

        if (!decimal.TryParse(TxtPrecio.Text, out var precio) || precio <= 0)
        {
            MessageBox.Show("Ingrese un precio válido mayor a 0.", "MR SYS", MessageBoxButton.OK, MessageBoxImage.Warning);
            TxtPrecio.Focus();
            return;
        }

        if (!decimal.TryParse(TxtCantidad.Text, out var cant) || cant <= 0)
        {
            cant = 1;
        }

        Descripcion = TxtDescripcion.Text.Trim();
        PrecioUnitario = precio;
        Cantidad = cant;
        Confirmado = true;

        DialogResult = true;
        Close();
    }

    private void BtnCancelar_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
