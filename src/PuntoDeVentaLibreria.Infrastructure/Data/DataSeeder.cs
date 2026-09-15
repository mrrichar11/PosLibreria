using Microsoft.EntityFrameworkCore;
using PuntoDeVentaLibreria.Domain.Entities.Catalogo;
using PuntoDeVentaLibreria.Domain.Entities.Combos;
using PuntoDeVentaLibreria.Domain.Entities.Configuracion;
using PuntoDeVentaLibreria.Domain.Entities.Seguridad;

namespace PuntoDeVentaLibreria.Infrastructure.Data;

public static class DataSeeder
{
    public static async Task SeedAsync(AppDbContext context, CancellationToken cancellationToken = default)
    {
        // 1. Configuración del Negocio
        if (!await context.Configuraciones.AnyAsync(cancellationToken))
        {
            context.Configuraciones.Add(new ConfiguracionNegocio
            {
                NombreComercio = "Librería & Regalería San Martín",
                Direccion = "Av. San Martín 1420",
                Telefono = "+54 9 11 4455-6677",
                Cuit = "27-33445566-4",
                MargenGananciaSugerido = 65.0m
            });
            await context.SaveChangesAsync(cancellationToken);
        }

        // 2. Usuarios Iniciales (Admin y Cajero)
        if (!await context.Usuarios.AnyAsync(cancellationToken))
        {
            context.Usuarios.AddRange(
                new Usuario
                {
                    Username = "admin",
                    PasswordHash = "admin", // Demo / Primer acceso
                    NombreCompleto = "Administrador Principal",
                    Rol = RolUsuario.Administrador
                },
                new Usuario
                {
                    Username = "cajero",
                    PasswordHash = "1234",
                    NombreCompleto = "Cajero Turno",
                    Rol = RolUsuario.Vendedor
                }
            );
            await context.SaveChangesAsync(cancellationToken);
        }

        // 3. Categorías Clave de Librería y Regalería
        if (!await context.Categorias.AnyAsync(cancellationToken))
        {
            var catEscolar = new Categoria { Nombre = "Escolar y Papelería", Descripcion = "Cuadernos, repuestos, carpetas y hojas", EsAccesoRapido = true, Orden = 1 };
            var catEscritura = new Categoria { Nombre = "Escritura y Dibujo", Descripcion = "Lapiceras, lápices, marcadores y reglas", EsAccesoRapido = true, Orden = 2 };
            var catServicios = new Categoria { Nombre = "Servicios y Fotocopiado", Descripcion = "Fotocopias, impresiones, anillados, plastificados", EsAccesoRapido = true, Orden = 3 };
            var catRegaleria = new Categoria { Nombre = "Regalería y Juguetes", Descripcion = "Mochilas, cartucheras, peluches, adornos", EsAccesoRapido = true, Orden = 4 };
            var catCombos = new Categoria { Nombre = "Combos y Kits Escolares", Descripcion = "Packs de útiles con descuento", EsAccesoRapido = true, Orden = 5 };

            context.Categorias.AddRange(catEscolar, catEscritura, catServicios, catRegaleria, catCombos);
            await context.SaveChangesAsync(cancellationToken);

            // 4. Marcas habituales
            var marcaRivadavia = new Marca { Nombre = "Rivadavia" };
            var marcaBic = new Marca { Nombre = "BIC" };
            var marcaFaber = new Marca { Nombre = "Faber-Castell" };
            var marcaGenerica = new Marca { Nombre = "Genérica / Comercial" };

            context.Marcas.AddRange(marcaRivadavia, marcaBic, marcaFaber, marcaGenerica);
            await context.SaveChangesAsync(cancellationToken);

            // 5. Artículos Semilla (Útiles con Códigos EAN reales, Servicios Rápidos y un Combo Kit)
            var cuaderno = new Articulo
            {
                Nombre = "Cuaderno Rivadavia Tapa Dura 50 Hojas Rayado",
                SKU = "CUAD-RIV-50R",
                CodigoBarras = "7791234567890",
                CategoriaId = catEscolar.Id,
                MarcaId = marcaRivadavia.Id,
                Tipo = TipoArticulo.Estandar,
                PrecioCosto = 2200m,
                PorcentajeGanancia = 60m,
                PrecioVenta = 3520m,
                StockActual = 40,
                StockMinimo = 10,
                Ubicacion = "Estante A1"
            };

            var lapicera = new Articulo
            {
                Nombre = "Bolígrafo BIC Cristal Azul 1.0mm",
                SKU = "BOLI-BIC-AZUL",
                CodigoBarras = "7501012345678",
                CategoriaId = catEscritura.Id,
                MarcaId = marcaBic.Id,
                Tipo = TipoArticulo.Estandar,
                PrecioCosto = 350m,
                PorcentajeGanancia = 70m,
                PrecioVenta = 600m,
                StockActual = 120,
                StockMinimo = 25,
                Ubicacion = "Exhibidor Mostrador"
            };

            var fotocopia = new Articulo
            {
                Nombre = "Fotocopia B/N Simple Faz (A4/Oficio)",
                SKU = "SERV-FOTO-BN",
                CodigoBarras = "FOTO-BN",
                CategoriaId = catServicios.Id,
                Tipo = TipoArticulo.Servicio,
                PrecioCosto = 15m,
                PrecioVenta = 80m,
                StockActual = 9999,
                EsBotonRapido = true,
                ColorBoton = "#3B82F6"
            };

            var impresionColor = new Articulo
            {
                Nombre = "Impresión Color Láser A4",
                SKU = "SERV-IMP-COLOR",
                CodigoBarras = "IMP-COLOR",
                CategoriaId = catServicios.Id,
                Tipo = TipoArticulo.Servicio,
                PrecioCosto = 70m,
                PrecioVenta = 350m,
                StockActual = 9999,
                EsBotonRapido = true,
                ColorBoton = "#10B981"
            };

            context.Articulos.AddRange(cuaderno, lapicera, fotocopia, impresionColor);
            await context.SaveChangesAsync(cancellationToken);

            // 6. Artículo Combo Escolar de Ejemplo
            var comboKit = new Articulo
            {
                Nombre = "Pack Escolar Básico (Cuaderno + Bolígrafo BIC)",
                SKU = "PACK-INICIO-ESC",
                CodigoBarras = "PACK-ESC-01",
                CategoriaId = catCombos.Id,
                Tipo = TipoArticulo.ComboKit,
                PrecioCosto = 2550m,
                PrecioVenta = 3900m, // Precio promocional del combo
                StockActual = 0,
                EsBotonRapido = true,
                ColorBoton = "#8B5CF6"
            };
            context.Articulos.Add(comboKit);
            await context.SaveChangesAsync(cancellationToken);

            // Detalle del Combo: 1 Cuaderno + 1 Bolígrafo
            context.ComboItems.AddRange(
                new ComboItem { ComboArticuloId = comboKit.Id, ComponenteArticuloId = cuaderno.Id, Cantidad = 1 },
                new ComboItem { ComboArticuloId = comboKit.Id, ComponenteArticuloId = lapicera.Id, Cantidad = 1 }
            );
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
