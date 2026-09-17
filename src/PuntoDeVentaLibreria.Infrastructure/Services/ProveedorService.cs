using Microsoft.EntityFrameworkCore;
using PuntoDeVentaLibreria.Application.DTOs.Proveedores;
using PuntoDeVentaLibreria.Application.Services;
using PuntoDeVentaLibreria.Domain.Entities.Proveedores;
using PuntoDeVentaLibreria.Infrastructure.Data;

namespace PuntoDeVentaLibreria.Infrastructure.Services;

public class ProveedorService : IProveedorService
{
    private readonly AppDbContext _context;

    public ProveedorService(AppDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<IReadOnlyList<ProveedorDto>> ObtenerTodosAsync(CancellationToken ct = default)
    {
        var proveedores = await _context.Proveedores
            .AsNoTracking()
            .Where(p => p.Activo)
            .OrderBy(p => p.Nombre)
            .Select(p => new ProveedorDto
            {
                Id = p.Id,
                Nombre = p.Nombre,
                RazonSocial = p.RazonSocial,
                Cuit = p.Cuit,
                Contacto = p.Contacto,
                Telefono = p.Telefono,
                Email = p.Email,
                Direccion = p.Direccion,
                DiasVisitaOEntrega = p.DiasVisitaOEntrega,
                Notas = p.Notas,
                CantidadArticulos = p.Articulos.Count(a => a.Activo)
            })
            .ToListAsync(ct);

        return proveedores;
    }

    public async Task<ProveedorDto?> ObtenerPorIdAsync(Guid id, CancellationToken ct = default)
    {
        var p = await _context.Proveedores
            .AsNoTracking()
            .Where(p => p.Id == id && p.Activo)
            .Select(p => new ProveedorDto
            {
                Id = p.Id,
                Nombre = p.Nombre,
                RazonSocial = p.RazonSocial,
                Cuit = p.Cuit,
                Contacto = p.Contacto,
                Telefono = p.Telefono,
                Email = p.Email,
                Direccion = p.Direccion,
                DiasVisitaOEntrega = p.DiasVisitaOEntrega,
                Notas = p.Notas,
                CantidadArticulos = p.Articulos.Count(a => a.Activo)
            })
            .FirstOrDefaultAsync(ct);

        return p;
    }

    public async Task<ProveedorDto> GuardarAsync(ProveedorDto dto, CancellationToken ct = default)
    {
        Proveedor entidad;
        if (dto.Id == Guid.Empty)
        {
            entidad = new Proveedor
            {
                Id = Guid.NewGuid(),
                FechaCreacion = DateTime.UtcNow
            };
            _context.Proveedores.Add(entidad);
        }
        else
        {
            entidad = await _context.Proveedores.FindAsync(new object[] { dto.Id }, ct)
                      ?? throw new InvalidOperationException("Proveedor no encontrado.");
            entidad.FechaModificacion = DateTime.UtcNow;
        }

        entidad.Nombre = dto.Nombre.Trim();
        entidad.RazonSocial = string.IsNullOrWhiteSpace(dto.RazonSocial) ? null : dto.RazonSocial.Trim();
        entidad.Cuit = string.IsNullOrWhiteSpace(dto.Cuit) ? null : dto.Cuit.Trim();
        entidad.Contacto = string.IsNullOrWhiteSpace(dto.Contacto) ? null : dto.Contacto.Trim();
        entidad.Telefono = string.IsNullOrWhiteSpace(dto.Telefono) ? null : dto.Telefono.Trim();
        entidad.Email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim();
        entidad.Direccion = string.IsNullOrWhiteSpace(dto.Direccion) ? null : dto.Direccion.Trim();
        entidad.DiasVisitaOEntrega = string.IsNullOrWhiteSpace(dto.DiasVisitaOEntrega) ? null : dto.DiasVisitaOEntrega.Trim();
        entidad.Notas = string.IsNullOrWhiteSpace(dto.Notas) ? null : dto.Notas.Trim();

        await _context.SaveChangesAsync(ct);

        dto.Id = entidad.Id;
        return dto;
    }

    public async Task<bool> EliminarAsync(Guid id, CancellationToken ct = default)
    {
        var p = await _context.Proveedores.FindAsync(new object[] { id }, ct);
        if (p == null) return false;

        p.Activo = false;
        p.FechaModificacion = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);
        return true;
    }
}
