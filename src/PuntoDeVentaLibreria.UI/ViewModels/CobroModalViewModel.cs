using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PuntoDeVentaLibreria.Application.DTOs.Clientes;

namespace PuntoDeVentaLibreria.UI.ViewModels;

public partial class CobroModalViewModel : ObservableObject
{
    [ObservableProperty]
    private decimal _totalACobrar;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalFinal))]
    [NotifyPropertyChangedFor(nameof(Vuelto))]
    [NotifyPropertyChangedFor(nameof(FaltaPagar))]
    [NotifyPropertyChangedFor(nameof(EsEfectivo))]
    [NotifyPropertyChangedFor(nameof(EsTarjeta))]
    [NotifyPropertyChangedFor(nameof(EsCredito))]
    [NotifyPropertyChangedFor(nameof(EsTransferencia))]
    [NotifyPropertyChangedFor(nameof(EsCtaCte))]
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

    // Cliente y datos de trazabilidad
    public ObservableCollection<ClienteDto> ClientesDisponibles { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TieneClienteSeleccionado))]
    [NotifyPropertyChangedFor(nameof(NuevoSaldoDeudorEstimado))]
    private ClienteDto? _clienteSeleccionado;

    [ObservableProperty]
    private string _nombreCliente = "Consumidor Final";

    [ObservableProperty]
    private string _referenciaPago = string.Empty;

    // Doble método de pago en Cta. Cte. (Entrega inicial)
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MontoFiadoRestante))]
    [NotifyPropertyChangedFor(nameof(NuevoSaldoDeudorEstimado))]
    private bool _tieneEntregaInicial;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MontoFiadoRestante))]
    [NotifyPropertyChangedFor(nameof(NuevoSaldoDeudorEstimado))]
    private decimal _montoEntregaInicial;

    [ObservableProperty]
    private string _metodoPagoEntrega = "Efectivo";

    [ObservableProperty]
    private string _referenciaEntrega = string.Empty;

    public decimal MontoFiadoRestante => TieneEntregaInicial ? Math.Max(0, TotalFinal - MontoEntregaInicial) : TotalFinal;
    public decimal NuevoSaldoDeudorEstimado => (ClienteSeleccionado?.SaldoDeudorActual ?? 0) + MontoFiadoRestante;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TieneErrorValidacion))]
    private string _mensajeValidacion = string.Empty;

    public bool TieneErrorValidacion => !string.IsNullOrWhiteSpace(MensajeValidacion);
    public bool TieneClienteSeleccionado => ClienteSeleccionado != null;
    public bool EsEfectivo => MetodoPago == "Efectivo";
    public bool EsTarjeta => MetodoPago == "Debito" || MetodoPago == "Credito";
    public bool EsCredito => MetodoPago == "Credito";
    public bool EsTransferencia => MetodoPago == "Transferencia";
    public bool EsCtaCte => MetodoPago == "CtaCte";

    public decimal TotalFinal => Math.Max(0, TotalACobrar - DescuentoEfectivoMonto + RecargoCuotasMonto);
    public decimal Vuelto => Math.Max(0, MontoEntregado - TotalFinal);
    public decimal FaltaPagar => Math.Max(0, TotalFinal - MontoEntregado);

    public bool VentaConfirmada { get; private set; }

    public event Action? OnCerrar;

    public ObservableCollection<BilleteRapidoDto> BilletesDisponibles { get; } = new();

    [ObservableProperty]
    private string _simboloMoneda = "$";

    public CobroModalViewModel(
        decimal totalACobrar, 
        IEnumerable<ClienteDto>? clientes = null,
        string? billetesConfig = null,
        string? simboloMoneda = null)
    {
        TotalACobrar = totalACobrar;
        MontoEntregado = totalACobrar;

        if (!string.IsNullOrWhiteSpace(simboloMoneda))
        {
            SimboloMoneda = simboloMoneda.Trim();
        }

        if (clientes != null)
        {
            foreach (var c in clientes)
            {
                ClientesDisponibles.Add(c);
            }
        }

        CargarBilletes(billetesConfig);
    }

    private void CargarBilletes(string? billetesConfig)
    {
        BilletesDisponibles.Clear();
        var texto = string.IsNullOrWhiteSpace(billetesConfig)
            ? "100,200,500,1000,2000,10000,20000"
            : billetesConfig;

        var partes = texto.Split(',', StringSplitOptions.RemoveEmptyEntries);
        foreach (var p in partes)
        {
            if (decimal.TryParse(p.Trim(), out var valor) && valor > 0)
            {
                BilletesDisponibles.Add(new BilleteRapidoDto
                {
                    Valor = valor,
                    Etiqueta = $"{SimboloMoneda}{valor:N0}"
                });
            }
        }
    }

    partial void OnClienteSeleccionadoChanged(ClienteDto? value)
    {
        if (value != null)
        {
            if (NombreCliente != value.NombreCompleto)
            {
                NombreCliente = value.NombreCompleto;
            }
            MensajeValidacion = string.Empty;
        }
        else if (string.IsNullOrWhiteSpace(NombreCliente))
        {
            NombreCliente = "Consumidor Final";
        }
    }

    partial void OnNombreClienteChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            ClienteSeleccionado = null;
            return;
        }

        if (ClienteSeleccionado != null && !string.Equals(ClienteSeleccionado.NombreCompleto, value, StringComparison.OrdinalIgnoreCase))
        {
            _clienteSeleccionado = ClientesDisponibles.FirstOrDefault(c => string.Equals(c.NombreCompleto, value, StringComparison.OrdinalIgnoreCase));
            OnPropertyChanged(nameof(ClienteSeleccionado));
            OnPropertyChanged(nameof(TieneClienteSeleccionado));
        }
        else if (ClienteSeleccionado == null)
        {
            var match = ClientesDisponibles.FirstOrDefault(c => string.Equals(c.NombreCompleto, value, StringComparison.OrdinalIgnoreCase));
            if (match != null)
            {
                _clienteSeleccionado = match;
                OnPropertyChanged(nameof(ClienteSeleccionado));
                OnPropertyChanged(nameof(TieneClienteSeleccionado));
            }
        }
    }

    [RelayCommand]
    private void DeseleccionarCliente()
    {
        ClienteSeleccionado = null;
        NombreCliente = "Consumidor Final";
    }

    [RelayCommand]
    private void SeleccionarMetodo(string metodo)
    {
        MetodoPago = metodo;
        MensajeValidacion = string.Empty;

        if (metodo != "Efectivo")
        {
            DescuentoEfectivoMonto = 0;
            MontoEntregado = TotalFinal;
        }

        if (metodo == "CtaCte" && ClienteSeleccionado == null)
        {
            MensajeValidacion = "💡 Para venta fiada en Cuenta Corriente, seleccione un cliente registrado.";
        }
    }

    private bool _iniciandoConteoBilletes = true;

    partial void OnMontoEntregadoChanged(decimal value)
    {
        if (value == 0)
        {
            _iniciandoConteoBilletes = true;
        }
    }

    [RelayCommand]
    private void AgregarBillete(object? montoParam)
    {
        if (montoParam == null) return;
        if (decimal.TryParse(montoParam.ToString(), out var monto))
        {
            if (_iniciandoConteoBilletes)
            {
                MontoEntregado = monto;
                _iniciandoConteoBilletes = false;
            }
            else
            {
                MontoEntregado += monto;
            }
        }
    }

    [RelayCommand]
    private void MontoExacto()
    {
        MontoEntregado = TotalFinal;
        _iniciandoConteoBilletes = true;
    }

    [RelayCommand]
    private void LimpiarMonto()
    {
        MontoEntregado = 0;
        _iniciandoConteoBilletes = true;
    }

    [RelayCommand]
    private void Confirmar()
    {
        MensajeValidacion = string.Empty;

        if (MetodoPago == "Efectivo" && MontoEntregado < TotalFinal)
        {
            MensajeValidacion = "El monto abonado en efectivo no cubre el total de la venta.";
            return;
        }

        if (MetodoPago == "CtaCte")
        {
            if (ClienteSeleccionado == null)
            {
                MensajeValidacion = "Debe seleccionar un cliente registrado para anotar la deuda en Cuenta Corriente.";
                return;
            }

            if (!ClienteSeleccionado.PermiteFiado)
            {
                MensajeValidacion = $"El cliente '{ClienteSeleccionado.NombreCompleto}' no tiene habilitado el crédito/fiado.";
                return;
            }

            if (TieneEntregaInicial)
            {
                if (MontoEntregaInicial <= 0)
                {
                    MensajeValidacion = "Ingrese un monto numérico positivo para la entrega inicial, o destilde la opción.";
                    return;
                }

                if (MontoEntregaInicial > TotalFinal)
                {
                    MensajeValidacion = $"La entrega inicial (${MontoEntregaInicial:N2}) no puede ser mayor al total a pagar (${TotalFinal:N2}).";
                    return;
                }
            }
        }

        VentaConfirmada = true;
        OnCerrar?.Invoke();
    }

    [RelayCommand]
    private void SeleccionarMetodoEntrega(string metodo)
    {
        MetodoPagoEntrega = metodo;
    }

    [RelayCommand]
    private void AsignarMitadEntrega()
    {
        MontoEntregaInicial = Math.Round(TotalFinal / 2m, 2);
    }

    [RelayCommand]
    private void Cancelar()
    {
        VentaConfirmada = false;
        OnCerrar?.Invoke();
    }
}

public class BilleteRapidoDto
{
    public decimal Valor { get; set; }
    public string Etiqueta { get; set; } = string.Empty;
}
