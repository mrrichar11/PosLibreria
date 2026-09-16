using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace PuntoDeVentaLibreria.UI.ViewModels;

public partial class CobroModalViewModel : ObservableObject
{
    [ObservableProperty]
    private decimal _totalACobrar;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalFinal))]
    [NotifyPropertyChangedFor(nameof(Vuelto))]
    [NotifyPropertyChangedFor(nameof(FaltaPagar))]
    private string _metodoPago = "Efectivo";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalFinal))]
    [NotifyPropertyChangedFor(nameof(Vuelto))]
    [NotifyPropertyChangedFor(nameof(FaltaPagar))]
    private decimal _descuentoEfectivoMonto;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalFinal))]
    [NotifyPropertyChangedFor(nameof(Vuelto))]
    [NotifyPropertyChangedFor(nameof(FaltaPagar))]
    private decimal _recargoCuotasMonto;

    [ObservableProperty]
    private int _cantidadCuotas = 1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Vuelto))]
    [NotifyPropertyChangedFor(nameof(FaltaPagar))]
    private decimal _montoEntregado;

    public decimal TotalFinal => Math.Max(0, TotalACobrar - DescuentoEfectivoMonto + RecargoCuotasMonto);
    public decimal Vuelto => Math.Max(0, MontoEntregado - TotalFinal);
    public decimal FaltaPagar => Math.Max(0, TotalFinal - MontoEntregado);

    public bool VentaConfirmada { get; private set; }

    public event Action? OnCerrar;

    public CobroModalViewModel(decimal totalACobrar)
    {
        TotalACobrar = totalACobrar;
        MontoEntregado = totalACobrar;
    }

    [RelayCommand]
    private void SeleccionarMetodo(string metodo)
    {
        MetodoPago = metodo;
        if (metodo != "Efectivo")
        {
            DescuentoEfectivoMonto = 0;
            MontoEntregado = TotalFinal;
        }
    }

    [RelayCommand]
    private void AgregarBillete(object? montoParam)
    {
        if (montoParam == null) return;
        if (decimal.TryParse(montoParam.ToString(), out var monto))
        {
            if (MontoEntregado < TotalFinal)
                MontoEntregado = monto;
            else
                MontoEntregado += monto;
        }
    }

    [RelayCommand]
    private void MontoExacto()
    {
        MontoEntregado = TotalFinal;
    }

    [RelayCommand]
    private void Confirmar()
    {
        if (MetodoPago == "Efectivo" && MontoEntregado < TotalFinal)
        {
            // Efectivo insuficiente
            return;
        }

        VentaConfirmada = true;
        OnCerrar?.Invoke();
    }

    [RelayCommand]
    private void Cancelar()
    {
        VentaConfirmada = false;
        OnCerrar?.Invoke();
    }
}
