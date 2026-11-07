using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using UGP_ultimo_guerrero_en_pie_.Models.Entities;

namespace UGP_ultimo_guerrero_en_pie_.Data;

public static class DbInitializer
{
    public static void Seed(ApplicationDbContext context)
    {
        // Asegurar que exista el método de pago Wompi
        if (!context.MetodosPagos.Any(m => m.Nombre == "Wompi"))
        {
            context.MetodosPagos.Add(new MetodoPago { Nombre = "Wompi" });
            context.SaveChanges();
        }

        // Si no existen eventos, sembrar el evento oficial UGP 05 con sus localidades
        if (!context.Eventos.Any())
        {
            var venue = new Venue
            {
                Nombre = "Coliseo Central de Combate - Arena Bogotá",
                Descripcion = "Recinto deportivo de alta intensidad equipado con Jaula Oficial UGP y pantallas perimétricas.",
                Orden = 1,
                Activo = true
            };
            context.Venues.Add(venue);
            context.SaveChanges();

            var tipoGeneral = new TipoEntrada
            {
                Nombre = "General Arena",
                Precio = 60000,
                Capacidad = 300
            };

            var tipoPreferencial = new TipoEntrada
            {
                Nombre = "Preferencial Cage-Side",
                Precio = 120000,
                Capacidad = 150
            };

            var tipoVip = new TipoEntrada
            {
                Nombre = "VIP Hexágono + Meet & Greet",
                Precio = 250000,
                Capacidad = 50
            };

            context.TiposEntrada.AddRange(tipoGeneral, tipoPreferencial, tipoVip);
            context.SaveChanges();

            var evento = new Evento
            {
                Nombre = "UGP 05: GUERRA EN LA JAULA",
                Fecha = new DateOnly(2026, 12, 15),
                Hora = new TimeOnly(19, 30),
                Lugar = "Coliseo Central de Combate - Arena Bogotá",
                Descripcion = "La noche de campeonato más electrizante de Latinoamérica. Combates profesionales de MMA y Grappling de sumisión.",
                CapacidadMaxima = 500,
                TicketsDisponibles = 500,
                EstadoEvento = "Activo",
                VenueId = venue.VenueId,
                FechaCreacion = DateTime.UtcNow
            };

            context.Eventos.Add(evento);
            context.SaveChanges();

            context.EventoTiposEntrada.AddRange(
                new EventoTipoEntrada { EventoId = evento.EventoId, TipoEntradaId = tipoGeneral.TipoEntradaId },
                new EventoTipoEntrada { EventoId = evento.EventoId, TipoEntradaId = tipoPreferencial.TipoEntradaId },
                new EventoTipoEntrada { EventoId = evento.EventoId, TipoEntradaId = tipoVip.TipoEntradaId }
            );

            context.SaveChanges();
        }
    }
}
