using System;
using System.Collections.Generic;

namespace PuntoDeVentaLibreria.UI.ViewModels;

public class VentaEnEsperaDto
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int NumeroTicket { get; set; }
    public DateTime FechaHora { get; set; } = DateTime.Now;
    public string NombreCliente { get; set; } = "Consumidor Final";
    public List<PosItemModel> Items { get; set; } = new();
    public decimal TotalVenta { get; set; }
    public decimal CantidadArticulos { get; set; }

    public string ResumenTexto => $"#Espera {NumeroTicket} · ${TotalVenta:N2} ({CantidadArticulos:N0} art.) · {FechaHora:HH:mm}";
}
