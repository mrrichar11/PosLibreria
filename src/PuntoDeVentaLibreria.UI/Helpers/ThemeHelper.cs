using System.Windows;
using System.Windows.Media;
using Wpf.Ui.Appearance;

namespace PuntoDeVentaLibreria.UI.Helpers;

public static class ThemeHelper
{
    public static void AplicarTema(string? tema)
    {
        bool esOscuro = string.Equals(tema, "Dark", StringComparison.OrdinalIgnoreCase);
        AplicarTema(esOscuro ? ApplicationTheme.Dark : ApplicationTheme.Light);
    }

    public static void AplicarTema(ApplicationTheme theme)
    {
        ApplicationThemeManager.Apply(theme);
        var res = System.Windows.Application.Current.Resources;

        if (theme == ApplicationTheme.Dark)
        {
            res["FondoVentanaBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0F172A"));
            res["FondoBarraLateralBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E293B"));
            res["BordeBarraLateralBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#334155"));
            res["FondoTarjetaBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E293B"));
            res["BordeTarjetaBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#334155"));
            res["TextoTituloBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F8FAFC"));
            res["TextoSubtituloBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8"));
            res["TextoNormalBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F1F5F9"));
            res["FondoControlSecundarioBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#334155"));
            res["FondoDataGridBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E293B"));
            res["HorizontalGridLinesBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#334155"));
            res["FondoHeaderTablaBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0F172A"));
            res["TextoHeaderTablaBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CBD5E1"));
        }
        else
        {
            res["FondoVentanaBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F1F5F9"));
            res["FondoBarraLateralBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF"));
            res["BordeBarraLateralBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E2E8F0"));
            res["FondoTarjetaBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF"));
            res["BordeTarjetaBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E2E8F0"));
            res["TextoTituloBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0F172A"));
            res["TextoSubtituloBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64748B"));
            res["TextoNormalBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0F172A"));
            res["FondoControlSecundarioBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F8FAFC"));
            res["FondoDataGridBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF"));
            res["HorizontalGridLinesBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F1F5F9"));
            res["FondoHeaderTablaBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F8FAFC"));
            res["TextoHeaderTablaBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#475569"));
        }
    }
}
