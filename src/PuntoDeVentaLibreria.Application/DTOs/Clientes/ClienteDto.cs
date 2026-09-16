namespace PuntoDeVentaLibreria.Application.DTOs.Clientes;

public class ClienteDto
{
    public Guid Id { get; set; }
    public string NombreCompleto { get; set; } = string.Empty;
    public string? DniOCuit { get; set; }
    public string? Telefono { get; set; }
    public string? Email { get; set; }
    public string? Direccion { get; set; }
    public string? ColegioOInstitucion { get; set; }

    public bool PermiteFiado { get; set; } = true;
    public decimal LimiteCredito { get; set; } = 50000m;
    public decimal SaldoDeudorActual { get; set; }
}

public class RegistrarEntregaCuentaCorrienteDto
{
    public Guid ClienteId { get; set; }
    public Guid TurnoCajaId { get; set; }
    public decimal MontoEntrega { get; set; }
    public string MetodoPago { get; set; } = "Efectivo";
    public string? Observaciones { get; set; }
    public string UsuarioNombre { get; set; } = "Cajero";
}
