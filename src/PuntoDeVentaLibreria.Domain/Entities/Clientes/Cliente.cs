using PuntoDeVentaLibreria.Domain.Common;

namespace PuntoDeVentaLibreria.Domain.Entities.Clientes;

public class Cliente : BaseEntity
{
    public string NombreCompleto { get; set; } = string.Empty;
    public string? DniOCuit { get; set; }
    public string? Telefono { get; set; }
    public string? Email { get; set; }
    public string? Direccion { get; set; }
    public string? ColegioOInstitucion { get; set; }

    // Cuenta Corriente (Fiado)
    public bool PermiteFiado { get; set; } = true;
    public decimal LimiteCredito { get; set; } = 50000m;
    public decimal SaldoDeudorActual { get; set; }
}
