using System.ComponentModel;
using PuntoDeVentaLibreria.Application.Common;

namespace PuntoDeVentaLibreria.Application.DTOs.Inventario;

public class ArticuloAumentoPrecioItemDto : INotifyPropertyChanged
{
    private decimal _factorConversion = 1m;
    private decimal _costoNuevo;
    private decimal _ventaNueva;
    private decimal _variacionPorcentaje;
    private bool _esAlertaVariacionExtrema;
    private bool _aplicar = true;

    public Guid ArticuloId { get; set; }
    public string SKU { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? CodigoProveedor { get; set; }
    public string? CodigoBarras { get; set; }
    public string? DescripcionProveedor { get; set; }

    public decimal CostoAnterior { get; set; }
    public decimal CostoOriginalProveedor { get; set; }
    public int? FactorSugerido { get; set; }
    public decimal PorcentajeGanancia { get; set; }
    public decimal IvaPorcentaje { get; set; }

    public decimal FactorConversion
    {
        get => _factorConversion;
        set
        {
            var val = value <= 0 ? 1m : value;
            if (_factorConversion != val)
            {
                _factorConversion = val;
                Recalcular();
                OnPropertyChanged(nameof(FactorConversion));
                OnPropertyChanged(nameof(MostrarBotonSugerido));
            }
        }
    }

    public decimal CostoNuevo
    {
        get => _costoNuevo;
        set
        {
            if (_costoNuevo != value)
            {
                _costoNuevo = value;
                OnPropertyChanged(nameof(CostoNuevo));
            }
        }
    }

    public decimal VentaAnterior { get; set; }

    public decimal VentaNueva
    {
        get => _ventaNueva;
        set
        {
            if (_ventaNueva != value)
            {
                _ventaNueva = value;
                OnPropertyChanged(nameof(VentaNueva));
            }
        }
    }

    public decimal VariacionPorcentaje
    {
        get => _variacionPorcentaje;
        set
        {
            if (_variacionPorcentaje != value)
            {
                _variacionPorcentaje = value;
                OnPropertyChanged(nameof(VariacionPorcentaje));
                OnPropertyChanged(nameof(VariacionTexto));
                OnPropertyChanged(nameof(EstadoVariacionTexto));
            }
        }
    }

    public bool EsAlertaVariacionExtrema
    {
        get => _esAlertaVariacionExtrema;
        set
        {
            if (_esAlertaVariacionExtrema != value)
            {
                _esAlertaVariacionExtrema = value;
                OnPropertyChanged(nameof(EsAlertaVariacionExtrema));
                OnPropertyChanged(nameof(EstadoVariacionTexto));
            }
        }
    }

    public bool Aplicar
    {
        get => _aplicar;
        set
        {
            if (_aplicar != value)
            {
                _aplicar = value;
                OnPropertyChanged(nameof(Aplicar));
            }
        }
    }

    // Propiedades calculadas y de soporte para interfaz de usuario
    public string VariacionTexto => VariacionPorcentaje > 0 ? $"+{VariacionPorcentaje:N1}%" : $"{VariacionPorcentaje:N1}%";
    public string EstadoVariacionTexto => EsAlertaVariacionExtrema ? $"⚠️ {VariacionTexto}" : VariacionTexto;
    public string SugerenciaBotonTexto => FactorSugerido.HasValue ? $"÷{FactorSugerido.Value}" : string.Empty;
    public bool MostrarBotonSugerido => FactorSugerido.HasValue && Math.Abs(_factorConversion - FactorSugerido.Value) > 0.01m;
    public bool NoEsAlerta => !EsAlertaVariacionExtrema;
    public string VariacionColorHex => VariacionPorcentaje > 0 ? "#16A34A" : (VariacionPorcentaje < 0 ? "#D97706" : "#64748B");
    public string DescripcionCompleta => string.IsNullOrWhiteSpace(DescripcionProveedor) 
        ? Nombre 
        : $"{Nombre}\n(Mayorista: {DescripcionProveedor})";

    public void Recalcular()
    {
        var factor = _factorConversion > 0 ? _factorConversion : 1m;
        CostoNuevo = Math.Round(CostoOriginalProveedor / factor, 2);
        VentaNueva = CalculoPreciosUtils.CalcularPrecioVenta(CostoNuevo, PorcentajeGanancia, IvaPorcentaje);
        VariacionPorcentaje = CostoAnterior > 0 
            ? Math.Round(((CostoNuevo - CostoAnterior) / CostoAnterior) * 100m, 1) 
            : 0m;
        EsAlertaVariacionExtrema = VariacionPorcentaje > 80m || VariacionPorcentaje < -50m;
        
        OnPropertyChanged(nameof(NoEsAlerta));
        OnPropertyChanged(nameof(VariacionColorHex));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public class ResumenPrevisualizacionAumentoDto
{
    public int TotalArticulosCatalogo { get; set; }
    public int CoincidenciasEncontradas { get; set; }
    public int CoincidenciasConCambioDePrecio { get; set; }
    public int CoincidenciasConAlerta { get; set; }
    public int NoEncontradosEnCatalogo { get; set; }
    public List<ArticuloAumentoPrecioItemDto> ItemsParaActualizar { get; set; } = new();
}

public class ActualizacionPreciosResultadoDto
{
    public int TotalActualizados { get; set; }
    public int Errores { get; set; }
    public string Mensaje { get; set; } = string.Empty;
}
