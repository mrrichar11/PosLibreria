using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using PuntoDeVentaLibreria.Application.DTOs.Inventario;
using PuntoDeVentaLibreria.Domain.Entities.Catalogo;

namespace PuntoDeVentaLibreria.UI.Converters;

public class StockToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is ArticuloDto a)
        {
            if (a.Tipo == TipoArticulo.Servicio)
                return new SolidColorBrush(Color.FromRgb(100, 116, 139)); // Gris pizarra

            if (a.StockActual <= 0)
                return new SolidColorBrush(Color.FromRgb(220, 38, 38)); // Rojo fuerte

            if (a.StockActual <= a.StockMinimo)
                return new SolidColorBrush(Color.FromRgb(217, 119, 6)); // Ámbar / naranja

            return new SolidColorBrush(Color.FromRgb(22, 163, 74)); // Verde esmeralda
        }

        return new SolidColorBrush(Color.FromRgb(15, 23, 42));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class StockToBadgeBackgroundConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is ArticuloDto a)
        {
            if (a.Tipo == TipoArticulo.Servicio)
                return new SolidColorBrush(Color.FromRgb(241, 245, 249));

            if (a.StockActual <= 0)
                return new SolidColorBrush(Color.FromRgb(254, 242, 242)); // Fondo rojo suave

            if (a.StockActual <= a.StockMinimo)
                return new SolidColorBrush(Color.FromRgb(254, 243, 199)); // Fondo amarillo suave

            return new SolidColorBrush(Color.FromRgb(240, 253, 244)); // Fondo verde suave
        }

        return new SolidColorBrush(Color.FromRgb(248, 250, 252));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class StockToEstadoTextoConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is ArticuloDto a)
        {
            if (a.Tipo == TipoArticulo.Servicio)
                return "Servicio";

            if (a.StockActual <= 0)
                return "Agotado";

            if (a.StockActual <= a.StockMinimo)
                return "Stock Bajo";

            return "Óptimo";
        }

        return "Normal";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}
