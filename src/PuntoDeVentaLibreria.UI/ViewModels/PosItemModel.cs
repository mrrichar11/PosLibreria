using CommunityToolkit.Mvvm.ComponentModel;

namespace PuntoDeVentaLibreria.UI.ViewModels;

public partial class PosItemModel : ObservableObject
{
    public Guid? ArticuloId { get; set; }
    public string SKU { get; set; } = string.Empty;
    public string? CodigoBarras { get; set; }
    public string Descripcion { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Subtotal))]
    private decimal _cantidad = 1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Subtotal))]
    private decimal _precioUnitario;

    public decimal PrecioCosto { get; set; }
    public bool EsCombo { get; set; }
    public bool EsServicio { get; set; }
    public bool EsVentaManual { get; set; }

    public decimal Subtotal => Cantidad * PrecioUnitario;
}
