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
    [NotifyPropertyChangedFor(nameof(TieneDescuento))]
    private decimal _descuentoEfectivoMonto;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalFinal))]
    [NotifyPropertyChangedFor(nameof(Vuelto))]
    [NotifyPropertyChangedFor(nameof(FaltaPagar))]
    [NotifyPropertyChangedFor(nameof(TieneRecargo))]
    private decimal _recargoCuotasMonto;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Es1Cuota))]
    [NotifyPropertyChangedFor(nameof(Es3Cuotas))]
    [NotifyPropertyChangedFor(nameof(Es6Cuotas))]
    [NotifyPropertyChangedFor(nameof(MetodoPagoDetalle))]
    private int _cantidadCuotas = 1;

    [ObservableProperty]
    private decimal _porcentajeDescuentoEfectivo = 10.0m;

    [ObservableProperty]
    private decimal _recargo3Cuotas = 15.0m;

    [ObservableProperty]
    private decimal _recargo6Cuotas = 25.0m;

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

    public bool TieneDescuento => DescuentoEfectivoMonto > 0;
    public bool TieneRecargo => RecargoCuotasMonto > 0;

    public bool Es1Cuota
    {
        get => CantidadCuotas == 1;
        set { if (value) CantidadCuotas = 1; }
    }

    public bool Es3Cuotas
    {
        get => CantidadCuotas == 3;
        set { if (value) CantidadCuotas = 3; }
    }

    public bool Es6Cuotas
    {
        get => CantidadCuotas == 6;
        set { if (value) CantidadCuotas = 6; }
    }

    public string MetodoPagoBadge => MetodoPago switch
    {
        "Efectivo" => DescuentoEfectivoMonto > 0 && TotalACobrar > 0 
            ? $"💵 EFECTIVO (-{DescuentoEfectivoMonto / TotalACobrar * 100:0.#}%)" 
            : $"💵 EFECTIVO (-{PorcentajeDescuentoEfectivo:0.#}%)",
        "Transferencia" => DescuentoEfectivoMonto > 0 && TotalACobrar > 0 
            ? $"📱 TRANSFERENCIA (-{DescuentoEfectivoMonto / TotalACobrar * 100:0.#}%)" 
            : $"📱 TRANSFERENCIA (-{PorcentajeDescuentoEfectivo:0.#}%)",
        "Debito" => "💳 DÉBITO",
        "Credito" => "💳 CRÉDITO",
        "CtaCte" => "📒 CTA. CTE.",
        _ => MetodoPago
    };

    public string MetodoPagoDetalle => MetodoPago switch
    {
        "Efectivo" => $"Descuento promocional: ${DescuentoEfectivoMonto:N2}",
        "Transferencia" => $"Descuento promocional: ${DescuentoEfectivoMonto:N2}",
        "Debito" => "Precio de Lista (Sin recargo)",
        "Credito" => CantidadCuotas == 1 ? "1 Pago (Sin recargo)" : (CantidadCuotas == 3 ? $"3 Cuotas (+{Recargo3Cuotas:0.#}%)" : $"6 Cuotas (+{Recargo6Cuotas:0.#}%)"),
        "CtaCte" => "Venta a crédito en cuenta fiada",
        _ => string.Empty
    };

    public decimal TotalFinal => Math.Max(0, TotalACobrar - DescuentoEfectivoMonto + RecargoCuotasMonto);
    public decimal Vuelto => Math.Max(0, MontoEntregado - TotalFinal);
    public decimal FaltaPagar => Math.Max(0, TotalFinal - MontoEntregado);

    public bool VentaConfirmada { get; private set; }

    public event Action? OnCerrar;

    public ObservableCollection<BilleteRapidoDto> BilletesDisponibles { get; } = new();

    [ObservableProperty]
    private string _simboloMoneda = "$";

    [ObservableProperty]
    private decimal _totalEfectivo;

    public CobroModalViewModel(
        decimal totalACobrar, 
        IEnumerable<ClienteDto>? clientes = null,
        string? billetesConfig = null,
        string? simboloMoneda = null,
        decimal porcentajeDescuentoEfectivo = 10.0m,
        decimal recargo3Cuotas = 15.0m,
        decimal recargo6Cuotas = 25.0m,
        decimal? totalEfectivo = null)
    {
        TotalACobrar = totalACobrar;
        TotalEfectivo = totalEfectivo ?? (porcentajeDescuentoEfectivo > 0 ? Math.Round(totalACobrar * (1m - (porcentajeDescuentoEfectivo / 100m)), 2) : totalACobrar);
        PorcentajeDescuentoEfectivo = porcentajeDescuentoEfectivo;
        Recargo3Cuotas = recargo3Cuotas;
        Recargo6Cuotas = recargo6Cuotas;

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
        ActualizarDescuentosYRecargos();
        MontoEntregado = TotalFinal;
    }

    public void ActualizarDescuentosYRecargos()
    {
        if (MetodoPago == "Efectivo" || MetodoPago == "Transferencia")
        {
            if (TotalEfectivo > 0 && TotalEfectivo < TotalACobrar)
            {
                DescuentoEfectivoMonto = TotalACobrar - TotalEfectivo;
            }
            else
            {
                DescuentoEfectivoMonto = Math.Round(TotalACobrar * (PorcentajeDescuentoEfectivo / 100m), 2);
            }
            RecargoCuotasMonto = 0m;
        }
        else if (MetodoPago == "Credito")
        {
            DescuentoEfectivoMonto = 0m;
            if (CantidadCuotas == 3)
            {
                RecargoCuotasMonto = Math.Round(TotalACobrar * (Recargo3Cuotas / 100m), 2);
            }
            else if (CantidadCuotas == 6)
            {
                RecargoCuotasMonto = Math.Round(TotalACobrar * (Recargo6Cuotas / 100m), 2);
            }
            else
            {
                RecargoCuotasMonto = 0m;
            }
        }
        else
        {
            DescuentoEfectivoMonto = 0m;
            RecargoCuotasMonto = 0m;
        }

        OnPropertyChanged(nameof(MetodoPagoBadge));
        OnPropertyChanged(nameof(MetodoPagoDetalle));
    }

    partial void OnCantidadCuotasChanged(int value)
    {
        ActualizarDescuentosYRecargos();
        if (MetodoPago == "Credito")
        {
            MontoEntregado = TotalFinal;
        }
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
        ActualizarDescuentosYRecargos();

        if (metodo != "Efectivo")
        {
            MontoEntregado = TotalFinal;
        }
        else
        {
            MontoEntregado = TotalFinal;
            _iniciandoConteoBilletes = true;
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
