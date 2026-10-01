using CommunityToolkit.Mvvm.ComponentModel;

namespace PuntoDeVentaLibreria.UI.ViewModels;

public partial class PosItemModel : ObservableObject
{
    public Guid? ArticuloId { get; set; }
    public Guid? ArticuloVarianteId { get; set; }
    public string? VarianteNombre { get; set; }
    public string SKU { get; set; } = string.Empty;
    public string? CodigoBarras { get; set; }
    public string Descripcion { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Subtotal))]
    [NotifyPropertyChangedFor(nameof(SubtotalEfectivo))]
    [NotifyPropertyChangedFor(nameof(AhorroTotal))]
    private decimal _cantidad = 1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Subtotal))]
    [NotifyPropertyChangedFor(nameof(PrecioEfectivo))]
    [NotifyPropertyChangedFor(nameof(SubtotalEfectivo))]
    [NotifyPropertyChangedFor(nameof(AhorroTotal))]
    [NotifyPropertyChangedFor(nameof(PorcentajeDescuentoEfectivoTexto))]
    private decimal _precioUnitario;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PrecioEfectivo))]
    [NotifyPropertyChangedFor(nameof(SubtotalEfectivo))]
    [NotifyPropertyChangedFor(nameof(AhorroTotal))]
    [NotifyPropertyChangedFor(nameof(PorcentajeDescuentoEfectivoTexto))]
    private decimal _precioEfectivoManual;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PrecioEfectivo))]
    [NotifyPropertyChangedFor(nameof(SubtotalEfectivo))]
    [NotifyPropertyChangedFor(nameof(AhorroTotal))]
    [NotifyPropertyChangedFor(nameof(PorcentajeDescuentoEfectivoTexto))]
    private decimal _porcentajeDescuentoEfectivo = 10m;

    public decimal PrecioCosto { get; set; }
    public bool EsCombo { get; set; }
    public bool EsServicio { get; set; }
    public bool EsVentaManual { get; set; }

    // Campos de Precio Dólar (USD - Joyería / Regalería importada)
    public bool EsPrecioDolar { get; set; }
    public decimal PrecioCostoDolar { get; set; }
    public string? DetalleDolar { get; set; }

    public decimal Subtotal => Cantidad * PrecioUnitario;
    public decimal PrecioEfectivo
    {
        get => PrecioEfectivoManual > 0 
            ? PrecioEfectivoManual 
            : Math.Round(PrecioUnitario * (1m - (PorcentajeDescuentoEfectivo / 100m)), 2);
        set => PrecioEfectivoManual = value;
    }
    public decimal SubtotalEfectivo => Cantidad * PrecioEfectivo;
    public decimal AhorroTotal => Math.Max(0m, Subtotal - SubtotalEfectivo);
    public string PorcentajeDescuentoEfectivoTexto => PrecioUnitario > 0 && PrecioEfectivo < PrecioUnitario
        ? $"-{Math.Round((PrecioUnitario - PrecioEfectivo) / PrecioUnitario * 100m, 1):0.#}%"
        : $"-{PorcentajeDescuentoEfectivo:0.#}%";
}
