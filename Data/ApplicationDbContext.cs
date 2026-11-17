using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using UGP_ultimo_guerrero_en_pie_.Models.Entities;

namespace UGP_ultimo_guerrero_en_pie_.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // Core de Eventos y Combates
    public virtual DbSet<Evento> Eventos { get; set; } = null!;
    public virtual DbSet<TipoEntrada> TiposEntrada { get; set; } = null!;
    public virtual DbSet<EventoTipoEntrada> EventoTiposEntrada { get; set; } = null!;
    public virtual DbSet<Combate> Combates { get; set; } = null!;
    public virtual DbSet<Peleador> Peleadores { get; set; } = null!;
    public virtual DbSet<HistorialCombate> HistorialCombates { get; set; } = null!;

    // Venta de Boletos y Transacciones
    public virtual DbSet<Compra> Compras { get; set; } = null!;
    public virtual DbSet<DetalleCompra> DetalleCompras { get; set; } = null!;
    public virtual DbSet<Ticket> Tickets { get; set; } = null!;
    public virtual DbSet<Asistente> Asistentes { get; set; } = null!;
    public virtual DbSet<MetodoPago> MetodosPagos { get; set; } = null!;
    public virtual DbSet<Descuento> Descuentos { get; set; } = null!;
    public virtual DbSet<Visitante> Visitantes { get; set; } = null!;

    // Recintos y Zonas
    public virtual DbSet<Venue> Venues { get; set; } = null!;
    public virtual DbSet<VenueZone> VenueZones { get; set; } = null!;

    // Contenido y Patrocinios
    public virtual DbSet<Patrocinador> Patrocinadores { get; set; } = null!;
    public virtual DbSet<Patrocinio> Patrocinios { get; set; } = null!;
    public virtual DbSet<Experiencia> Experiencias { get; set; } = null!;
    public virtual DbSet<SeccionMultimedia> SeccionesMultimedia { get; set; } = null!;
    public virtual DbSet<ContenidoSitio> ContenidoSitio { get; set; } = null!;
    public virtual DbSet<ConfiguracionWhatsApp> ConfiguracionesWhatsApp { get; set; } = null!;
    public virtual DbSet<ContactoWhatsApp> ContactosWhatsApp { get; set; } = null!;
    public virtual DbSet<SeccionExperiencia> SeccionesExperiencia { get; set; } = null!;
    public virtual DbSet<ExperienciaMultimediaItem> ExperienciaMultimediaItems { get; set; } = null!;

    // Módulo Asistente IA
    public virtual DbSet<AIConfiguracion> AIConfiguracion { get; set; } = null!;
    public virtual DbSet<AITema> AITemas { get; set; } = null!;
    public virtual DbSet<AIPalabraRestringida> AIPalabrasRestringidas { get; set; } = null!;
    public virtual DbSet<AIContextoConfig> AIContextoConfig { get; set; } = null!;
    public virtual DbSet<AIChatHistorial> AIChatHistorial { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        ReservasMapping.Configure(modelBuilder);

        // Secuencia nativa para folios de tickets en SQL Server (concurrencia segura)
        modelBuilder.HasSequence<long>("seq_ticket_codigo")
            .StartsAt(1000)
            .IncrementsBy(1);

        // Core de Eventos y Combates
        modelBuilder.Entity<Evento>(entity =>
        {
            entity.ToTable("Eventos");
            entity.HasKey(e => e.EventoId);
            entity.Property(e => e.EventoId).HasColumnName("evento_id");
            entity.Property(e => e.CapacidadMaxima).HasColumnName("capacidad_maxima");
            entity.Property(e => e.Descripcion).IsUnicode(false).HasColumnName("descripcion");
            entity.Property(e => e.EstadoEvento).HasMaxLength(20).IsUnicode(false).HasDefaultValue("Próximo").HasColumnName("estado_evento");
            entity.Property(e => e.Fecha).HasColumnName("fecha");
            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("(getdate())").HasColumnType("datetime").HasColumnName("fecha_creacion");
            entity.Property(e => e.Hora).HasColumnName("hora");
            entity.Property(e => e.ImagenUrl).HasMaxLength(255).IsUnicode(false).HasColumnName("imagen_url");
            entity.Property(e => e.Lugar).HasMaxLength(200).IsUnicode(false).HasColumnName("lugar");
            entity.Property(e => e.Nombre).HasMaxLength(100).IsUnicode(false).HasColumnName("nombre");
            entity.Property(e => e.TicketsDisponibles).HasColumnName("tickets_disponibles");
            entity.Property(e => e.VenueId).HasColumnName("venue_id");

            entity.HasOne(e => e.Venue).WithMany(v => v.Eventos)
                .HasForeignKey(e => e.VenueId).OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_Evento_Venue");
        });

        modelBuilder.Entity<TipoEntrada>(entity =>
        {
            entity.ToTable("TiposEntrada");
            entity.HasKey(e => e.TipoEntradaId);
            entity.HasIndex(e => e.Nombre, "UQ_TiposEntrada_Nombre").IsUnique();
            entity.Property(e => e.TipoEntradaId).HasColumnName("tipo_entrada_id");
            entity.Property(e => e.Capacidad).HasColumnName("capacidad");
            entity.Property(e => e.Nombre).HasMaxLength(50).IsUnicode(false).HasColumnName("nombre");
            entity.Property(e => e.Precio).HasColumnType("decimal(10, 2)").HasColumnName("precio");
        });

        modelBuilder.Entity<EventoTipoEntrada>(entity =>
        {
            entity.ToTable("EventoTiposEntrada");
            entity.HasKey(e => e.EventoTipoId);
            entity.HasIndex(e => new { e.EventoId, e.TipoEntradaId }, "UQ_EventoTiposEntrada").IsUnique();
            entity.Property(e => e.EventoTipoId).HasColumnName("evento_tipo_id");
            entity.Property(e => e.EventoId).HasColumnName("evento_id");
            entity.Property(e => e.TipoEntradaId).HasColumnName("tipo_entrada_id");

            entity.HasOne(d => d.Evento).WithMany(p => p.EventoTiposEntrada)
                .HasForeignKey(d => d.EventoId).OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EventoTiposEntrada_Evento");
            entity.HasOne(d => d.TipoEntrada).WithMany(p => p.EventoTiposEntrada)
                .HasForeignKey(d => d.TipoEntradaId).OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EventoTiposEntrada_TipoEntrada");
        });

        modelBuilder.Entity<Peleador>(entity =>
        {
            entity.ToTable("Peleadores");
            entity.HasKey(e => e.PeleadorId);
            entity.Property(e => e.PeleadorId).HasColumnName("peleador_id");
            entity.Property(e => e.Apodo).HasMaxLength(100).IsUnicode(false).HasColumnName("apodo");
            entity.Property(e => e.Descripcion).IsUnicode(false).HasColumnName("descripcion");
            entity.Property(e => e.Estado).HasMaxLength(20).IsUnicode(false).HasDefaultValue("activo").HasColumnName("estado");
            entity.Property(e => e.FechaNacimiento).HasColumnName("fecha_nacimiento");
            entity.Property(e => e.FechaRegistro).HasDefaultValueSql("(getdate())").HasColumnType("datetime").HasColumnName("fecha_registro");
            entity.Property(e => e.FotoUrl).IsUnicode(false).HasColumnName("foto_url");
            entity.Property(e => e.Nacionalidad).HasMaxLength(100).IsUnicode(false).HasColumnName("nacionalidad");
            entity.Property(e => e.Nombre).HasMaxLength(100).IsUnicode(false).HasColumnName("nombre");
            entity.Property(e => e.PeleasGanadas).HasDefaultValue(0).HasColumnName("peleas_ganadas");
            entity.Property(e => e.PeleasPerdidas).HasDefaultValue(0).HasColumnName("peleas_perdidas");
            entity.Property(e => e.Peso).HasColumnType("decimal(5, 2)").HasColumnName("peso");
        });

        modelBuilder.Entity<Combate>(entity =>
        {
            entity.ToTable("Combates");
            entity.HasKey(e => e.CombateId);
            entity.Property(e => e.CombateId).HasColumnName("combate_id");
            entity.Property(e => e.EventoId).HasColumnName("evento_id");
            entity.Property(e => e.GanadorId).HasColumnName("ganador_id");
            entity.Property(e => e.PeleadorAzulId).HasColumnName("peleador_azul_id");
            entity.Property(e => e.PeleadorRojoId).HasColumnName("peleador_rojo_id");
            entity.Property(e => e.Resultado).HasMaxLength(100).IsUnicode(false).HasColumnName("resultado");
            entity.Property(e => e.Rounds).HasColumnName("rounds");
            entity.Property(e => e.TipoCombate).HasMaxLength(100).IsUnicode(false).HasColumnName("tipo_combate");

            entity.HasOne(d => d.Evento).WithMany(p => p.Combates)
                .HasForeignKey(d => d.EventoId).OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Combates_Evento");
            entity.HasOne(d => d.Ganador).WithMany(p => p.CombateGanadors)
                .HasForeignKey(d => d.GanadorId)
                .HasConstraintName("FK_Combates_Ganador");
            entity.HasOne(d => d.PeleadorAzul).WithMany(p => p.CombatePeleadorAzuls)
                .HasForeignKey(d => d.PeleadorAzulId).OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Combates_PeleadorAzul");
            entity.HasOne(d => d.PeleadorRojo).WithMany(p => p.CombatePeleadorRojos)
                .HasForeignKey(d => d.PeleadorRojoId).OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Combates_PeleadorRojo");
        });

        modelBuilder.Entity<HistorialCombate>(entity =>
        {
            entity.ToTable("HistorialCombates");
            entity.HasKey(e => e.HistorialId);
            entity.Property(e => e.HistorialId).HasColumnName("historial_id");
            entity.Property(e => e.CombateId).HasColumnName("combate_id");
            entity.Property(e => e.MetodoVictoria).HasMaxLength(100).IsUnicode(false).HasColumnName("metodo_victoria");
            entity.Property(e => e.Observaciones).IsUnicode(false).HasColumnName("observaciones");
            entity.Property(e => e.RoundsTotales).HasColumnName("rounds_totales");

            entity.HasOne(d => d.Combate).WithMany(p => p.HistorialCombates)
                .HasForeignKey(d => d.CombateId).OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_HistorialCombates_Combate");
        });

        // Venta de Boletos y Transacciones
        modelBuilder.Entity<Visitante>(entity =>
        {
            entity.ToTable("Visitantes");
            entity.HasKey(e => e.VisitanteId);
            entity.HasIndex(e => e.Correo, "UQ_Visitantes_Correo").IsUnique();
            entity.Property(e => e.VisitanteId).HasColumnName("visitante_id");
            entity.Property(e => e.Correo).HasMaxLength(150).IsUnicode(false).HasColumnName("correo");
            entity.Property(e => e.FechaNacimiento).HasColumnName("fecha_nacimiento");
            entity.Property(e => e.FechaRegistro).HasDefaultValueSql("(getdate())").HasColumnType("datetime").HasColumnName("fecha_registro");
            entity.Property(e => e.Nombre).HasMaxLength(100).IsUnicode(false).HasColumnName("nombre");
            entity.Property(e => e.Telefono).HasMaxLength(30).IsUnicode(false).HasColumnName("telefono");
        });

        modelBuilder.Entity<MetodoPago>(entity =>
        {
            entity.ToTable("MetodosPago");
            entity.HasKey(e => e.MetodoPagoId);
            entity.Property(e => e.MetodoPagoId).HasColumnName("metodo_pago_id");
            entity.Property(e => e.Nombre).HasMaxLength(50).IsUnicode(false).HasColumnName("nombre");
        });

        modelBuilder.Entity<Compra>(entity =>
        {
            entity.ToTable("Compras");
            entity.HasKey(e => e.CompraId);
            entity.Property(e => e.CompraId).HasColumnName("compra_id");
            entity.Property(e => e.EstadoPago).HasMaxLength(20).IsUnicode(false).HasDefaultValue("pendiente").HasColumnName("estado_pago");
            entity.Property(e => e.EventoId).HasColumnName("evento_id");
            entity.Property(e => e.FechaCompra).HasDefaultValueSql("(getdate())").HasColumnType("datetime").HasColumnName("fecha_compra");
            entity.Property(e => e.MetodoPagoId).HasColumnName("metodo_pago_id");
            entity.Property(e => e.MontoTotal).HasColumnType("decimal(10, 2)").HasColumnName("monto_total");
            entity.Property(e => e.VisitanteId).HasColumnName("visitante_id");

            entity.HasOne(d => d.Evento).WithMany(p => p.Compras)
                .HasForeignKey(d => d.EventoId).OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Compras_Evento");
            entity.HasOne(d => d.MetodoPago).WithMany(p => p.Compras)
                .HasForeignKey(d => d.MetodoPagoId).OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Compras_MetodoPago");
            entity.HasOne(d => d.Visitante).WithMany(p => p.Compras)
                .HasForeignKey(d => d.VisitanteId).OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Compras_Visitante");
        });

        modelBuilder.Entity<DetalleCompra>(entity =>
        {
            entity.ToTable("DetalleCompra");
            entity.HasKey(e => e.DetalleId);
            entity.Property(e => e.DetalleId).HasColumnName("detalle_id");
            entity.Property(e => e.Cantidad).HasColumnName("cantidad");
            entity.Property(e => e.CompraId).HasColumnName("compra_id");
            entity.Property(e => e.PrecioUnitario).HasColumnType("decimal(10, 2)").HasColumnName("precio_unitario");
            entity.Property(e => e.Subtotal).HasColumnType("decimal(10, 2)").HasColumnName("subtotal");
            entity.Property(e => e.TipoEntradaId).HasColumnName("tipo_entrada_id");

            entity.HasOne(d => d.Compra).WithMany(p => p.DetalleCompras)
                .HasForeignKey(d => d.CompraId).OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DetalleCompra_Compra");
            entity.HasOne(d => d.TipoEntrada).WithMany(p => p.DetalleCompras)
                .HasForeignKey(d => d.TipoEntradaId).OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DetalleCompra_TipoEntrada");
        });

        modelBuilder.Entity<Ticket>(entity =>
        {
            entity.ToTable("Tickets");
            entity.HasKey(e => e.TicketId);
            entity.HasIndex(e => e.CodigoQr, "UQ_Tickets_CodigoQR").IsUnique();
            // Índice único para Código Único (seguro y sin duplicados)
            entity.HasIndex(e => e.CodigoUnico).IsUnique();

            entity.Property(e => e.TicketId).HasColumnName("ticket_id");
            entity.Property(e => e.CompraId).HasColumnName("compra_id");
            entity.Property(e => e.EventoId).HasColumnName("evento_id");
            entity.Property(e => e.TipoEntradaId).HasColumnName("tipo_entrada_id");
            entity.Property(e => e.CodigoUnico).HasMaxLength(30).IsUnicode(false).HasColumnName("codigo_unico");
            entity.Property(e => e.CodigoQr).HasMaxLength(500).IsUnicode(false).HasColumnName("codigo_qr");
            entity.Property(e => e.TokenHash).HasMaxLength(200).IsUnicode(false).HasColumnName("token_hash");
            entity.Property(e => e.TipoTicket).HasMaxLength(50).IsUnicode(false).HasColumnName("tipo_ticket");
            entity.Property(e => e.Zona).HasMaxLength(50).IsUnicode(false).HasColumnName("zona");
            entity.Property(e => e.Precio).HasColumnType("decimal(10, 2)").HasColumnName("precio");
            entity.Property(e => e.Estado).HasMaxLength(20).IsUnicode(false).HasDefaultValue("Activo").HasColumnName("estado");
            entity.Property(e => e.FechaGeneracion).HasDefaultValueSql("(getdate())").HasColumnType("datetime").HasColumnName("fecha_generacion");
            entity.Property(e => e.PdfPath).HasMaxLength(500).IsUnicode(false).HasColumnName("pdf_path");
            entity.Property(e => e.QrPath).HasMaxLength(500).IsUnicode(false).HasColumnName("qr_path");

            entity.HasOne(d => d.Compra).WithMany(p => p.Tickets)
                .HasForeignKey(d => d.CompraId).OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Tickets_Compra");
            entity.HasOne(d => d.Evento).WithMany(p => p.Tickets)
                .HasForeignKey(d => d.EventoId).OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Tickets_Evento");
            entity.HasOne(d => d.TipoEntrada).WithMany(p => p.Tickets)
                .HasForeignKey(d => d.TipoEntradaId).OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Tickets_TipoEntrada");

            // Relación 1:1 con Asistente
            entity.HasOne(d => d.Asistente)
                .WithOne(a => a.Ticket)
                .HasForeignKey<Asistente>(a => a.TicketId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_Ticket_Asistente");
        });

        modelBuilder.Entity<Asistente>(entity =>
        {
            entity.ToTable("Asistente");
            entity.HasKey(e => e.AsistenteId);
            entity.HasIndex(e => e.TicketId, "UQ_Asistente_Ticket").IsUnique();

            entity.Property(e => e.AsistenteId).HasColumnName("asistente_id").UseIdentityColumn();
            entity.Property(e => e.TicketId).HasColumnName("ticket_id");
            entity.Property(e => e.Nombre).IsRequired().HasMaxLength(100).IsUnicode(true).HasColumnName("nombre");
            entity.Property(e => e.Apellido).IsRequired().HasMaxLength(100).IsUnicode(true).HasColumnName("apellido");
            entity.Property(e => e.TipoDocumento).IsRequired().HasMaxLength(20).IsUnicode(false).HasColumnName("tipo_documento");
            entity.Property(e => e.NumeroDocumento).IsRequired().HasMaxLength(30).IsUnicode(false).HasColumnName("numero_documento");
            entity.Property(e => e.Correo).IsRequired().HasMaxLength(150).IsUnicode(false).HasColumnName("correo");
            entity.Property(e => e.Celular).HasMaxLength(30).IsUnicode(false).HasColumnName("celular");
            entity.Property(e => e.FechaNacimiento).HasColumnName("fecha_nacimiento");
        });

        modelBuilder.Entity<Descuento>(entity =>
        {
            entity.ToTable("Descuentos");
            entity.HasKey(e => e.DescuentoId);
            entity.Property(e => e.DescuentoId).HasColumnName("descuento_id");
            entity.Property(e => e.CantidadMinima).HasColumnName("cantidad_minima");
            entity.Property(e => e.PorcentajeDescuento).HasColumnType("decimal(5, 2)").HasColumnName("porcentaje_descuento");
            entity.Property(e => e.TicketId).HasColumnName("ticket_id");

            entity.HasOne(d => d.Ticket).WithMany(p => p.Descuentos)
                .HasForeignKey(d => d.TicketId).OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Descuentos_Ticket");
        });

        // Recintos y Zonas
        modelBuilder.Entity<Venue>(entity =>
        {
            entity.ToTable("Venue");
            entity.HasKey(e => e.VenueId);
            entity.Property(e => e.VenueId).HasColumnName("venue_id");
            entity.Property(e => e.Nombre).HasMaxLength(200).IsUnicode(true).HasColumnName("nombre");
            entity.Property(e => e.SVGData).HasColumnType("nvarchar(max)").HasColumnName("svg_data");
            entity.Property(e => e.ImagenPrevia).HasMaxLength(500).IsUnicode(false).HasColumnName("imagen_previa");
            entity.Property(e => e.ImagenPlano).HasMaxLength(500).IsUnicode(false).HasColumnName("imagen_plano");
            entity.Property(e => e.Descripcion).HasMaxLength(500).IsUnicode(true).HasColumnName("descripcion");
            entity.Property(e => e.Orden).HasDefaultValue(0).HasColumnName("orden");
            entity.Property(e => e.Activo).HasDefaultValue(true).HasColumnName("activo");
        });

        modelBuilder.Entity<VenueZone>(entity =>
        {
            entity.ToTable("VenueZone");
            entity.HasKey(e => e.VenueZoneId);
            entity.Property(e => e.VenueZoneId).HasColumnName("venue_zone_id");
            entity.Property(e => e.VenueId).HasColumnName("venue_id");
            entity.Property(e => e.Nombre).HasMaxLength(100).IsUnicode(true).HasColumnName("nombre");
            entity.Property(e => e.Precio).HasColumnType("decimal(18,2)").HasDefaultValue(0).HasColumnName("precio");
            entity.Property(e => e.Capacidad).HasDefaultValue(0).HasColumnName("capacidad");
            entity.Property(e => e.Disponibles).HasDefaultValue(0).HasColumnName("disponibles");
            entity.Property(e => e.Color).HasMaxLength(7).IsUnicode(false).HasDefaultValue("#D4AF37").HasColumnName("color");
            entity.Property(e => e.Orden).HasDefaultValue(0).HasColumnName("orden");
            entity.Property(e => e.SVGPathId).HasMaxLength(100).IsUnicode(false).HasColumnName("svg_path_id");
            entity.Property(e => e.Activo).HasDefaultValue(true).HasColumnName("activo");
            entity.Property(e => e.TieneAsientosNumerados).HasDefaultValue(false).HasColumnName("tiene_asientos_numerados");

            entity.HasOne(e => e.Venue).WithMany(v => v.Zones)
                .HasForeignKey(e => e.VenueId).OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_VenueZone_Venue");
        });

        // Contenido y Patrocinios
        modelBuilder.Entity<Patrocinador>(entity =>
        {
            entity.ToTable("Patrocinadores");
            entity.HasKey(e => e.PatrocinadorId);
            entity.Property(e => e.PatrocinadorId).HasColumnName("patrocinador_id");
            entity.Property(e => e.Contacto).HasMaxLength(100).IsUnicode(false).HasColumnName("contacto");
            entity.Property(e => e.Correo).HasMaxLength(100).IsUnicode(false).HasColumnName("correo");
            entity.Property(e => e.Descripcion).IsUnicode(false).HasColumnName("descripcion");
            entity.Property(e => e.Empresa).HasMaxLength(100).IsUnicode(false).HasColumnName("empresa");
            entity.Property(e => e.FechaRegistro).HasDefaultValueSql("(getdate())").HasColumnType("datetime").HasColumnName("fecha_registro");
            entity.Property(e => e.Telefono).HasMaxLength(20).IsUnicode(false).HasColumnName("telefono");
            entity.Property(e => e.UrlPagina).HasMaxLength(500).IsUnicode(false).HasColumnName("url_pagina");
            entity.Property(e => e.LogoUrl).HasMaxLength(500).IsUnicode(false).HasColumnName("logo_url");
        });

        modelBuilder.Entity<Patrocinio>(entity =>
        {
            entity.ToTable("Patrocinios");
            entity.HasKey(e => e.PatrocinioId);
            entity.Property(e => e.PatrocinioId).HasColumnName("patrocinio_id");
            entity.Property(e => e.PatrocinadorId).HasColumnName("patrocinador_id");
            entity.Property(e => e.UrlPagina).HasMaxLength(255).IsUnicode(false).HasColumnName("url_pagina");
            entity.Property(e => e.Logo).HasMaxLength(255).IsUnicode(false).HasColumnName("logo");
            entity.Property(e => e.Flyer).HasMaxLength(255).IsUnicode(false).HasColumnName("flyer");
            entity.Property(e => e.FechaInicio).HasColumnName("fecha_inicio");
            entity.Property(e => e.FechaFin).HasColumnName("fecha_fin");
            entity.Property(e => e.Costo).HasColumnType("decimal(10, 2)").HasColumnName("costo");
            entity.Property(e => e.EventoId).HasColumnName("evento_id");
            entity.Property(e => e.MensajePromocional).HasMaxLength(500).IsUnicode(false).HasColumnName("mensaje_promocional");
            entity.Property(e => e.ProductosJson).IsUnicode(false).HasColumnName("productos_json");
            entity.Property(e => e.Activo).HasDefaultValue(true).HasColumnName("activo");
            entity.Property(e => e.Orden).HasDefaultValue(0).HasColumnName("orden");

            entity.HasOne(d => d.Evento).WithMany(p => p.Patrocinios)
                .HasForeignKey(d => d.EventoId).OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_Patrocinios_Evento");
            entity.HasOne(d => d.Patrocinador).WithMany(p => p.Patrocinios)
                .HasForeignKey(d => d.PatrocinadorId).OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_Patrocinios_Patrocinador");
        });

        modelBuilder.Entity<Experiencia>(entity =>
        {
            entity.ToTable("Experiencias");
            entity.HasKey(e => e.ExperienciaId);
            entity.Property(e => e.Nombre).IsRequired().HasMaxLength(200);
            entity.Property(e => e.CorreoVisitante).HasMaxLength(200).IsUnicode(false).HasColumnName("correo_visitante");
            entity.Property(e => e.Calificacion).HasColumnName("calificacion");
            entity.Property(e => e.Estado).HasMaxLength(20).HasDefaultValue("Activa");
            entity.Property(e => e.VideoPrincipalUrl).HasMaxLength(500).IsUnicode(false).HasColumnName("video_principal_url");
            entity.Property(e => e.VideoSecundarioUrl).HasMaxLength(500).IsUnicode(false).HasColumnName("video_secundario_url");
            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("(getdate())").HasColumnType("datetime").HasColumnName("fecha_creacion");

            entity.HasOne(e => e.Evento).WithMany(ev => ev.Experiencias)
                .HasForeignKey(e => e.EventoId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SeccionMultimedia>(entity =>
        {
            entity.ToTable("SeccionesMultimedia");
            entity.HasKey(e => e.SeccionMultimediaId);
            entity.Property(e => e.Titulo).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Subtitulo).HasMaxLength(300);
            entity.Property(e => e.VideoPrincipalUrl).HasMaxLength(500).IsUnicode(false);
            entity.Property(e => e.VideoSecundarioUrl).HasMaxLength(500).IsUnicode(false);
            entity.Property(e => e.TextoBotonPrincipal).HasMaxLength(100);
            entity.Property(e => e.UrlBotonPrincipal).HasMaxLength(500).IsUnicode(false);
            entity.Property(e => e.TextoBotonSecundario).HasMaxLength(100);
            entity.Property(e => e.UrlBotonSecundario).HasMaxLength(500).IsUnicode(false);
            entity.Property(e => e.Orden).HasDefaultValue(0);
            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("(getdate())");
        });

        modelBuilder.Entity<ContenidoSitio>(entity =>
        {
            entity.ToTable("ContenidoSitio");
            entity.HasKey(e => e.ContenidoSitioId);
            entity.HasIndex(e => e.Clave, "UQ_ContenidoSitio_Clave").IsUnique();
            entity.Property(e => e.ContenidoSitioId).HasColumnName("contenido_sitio_id");
            entity.Property(e => e.Clave).HasMaxLength(100).IsUnicode(false).HasColumnName("clave");
            entity.Property(e => e.Titulo).HasMaxLength(200).IsUnicode(true).HasColumnName("titulo");
            entity.Property(e => e.Subtitulo).HasMaxLength(300).IsUnicode(true).HasColumnName("subtitulo");
            entity.Property(e => e.Descripcion).IsUnicode(true).HasColumnName("descripcion");
            entity.Property(e => e.RutaImagen).HasMaxLength(500).IsUnicode(false).HasColumnName("ruta_imagen");
            entity.Property(e => e.MetadatosJson).IsUnicode(true).HasColumnName("metadatos_json");
            entity.Property(e => e.FechaActualizacion).HasDefaultValueSql("(getdate())").HasColumnType("datetime").HasColumnName("fecha_actualizacion");
        });

        modelBuilder.Entity<ConfiguracionWhatsApp>(entity =>
        {
            entity.ToTable("ConfiguracionWhatsApp");
            entity.HasKey(e => e.ConfiguracionWhatsAppId);
            entity.Property(e => e.ConfiguracionWhatsAppId).HasColumnName("configuracion_whatsapp_id");
            entity.Property(e => e.Activo).HasDefaultValue(true).HasColumnName("activo");
            entity.Property(e => e.TooltipTexto).HasMaxLength(200).IsUnicode(true).HasColumnName("tooltip_texto");
            entity.Property(e => e.MensajePredeterminado).HasMaxLength(1000).IsUnicode(true).HasColumnName("mensaje_predeterminado");
            entity.Property(e => e.MensajeFueraHorario).HasMaxLength(500).IsUnicode(true).HasColumnName("mensaje_fuera_horario");
            entity.Property(e => e.HoraInicio).HasColumnName("hora_inicio");
            entity.Property(e => e.HoraFin).HasColumnName("hora_fin");
            entity.Property(e => e.MostrarEnAdmin).HasDefaultValue(true).HasColumnName("mostrar_en_admin");
            entity.Property(e => e.MostrarEnPublico).HasDefaultValue(true).HasColumnName("mostrar_en_publico");
            entity.Property(e => e.PaginasMostrar).IsUnicode(false).HasColumnName("paginas_mostrar");
            entity.Property(e => e.FechaActualizacion).HasDefaultValueSql("(getdate())").HasColumnType("datetime").HasColumnName("fecha_actualizacion");
        });

        modelBuilder.Entity<ContactoWhatsApp>(entity =>
        {
            entity.ToTable("ContactoWhatsApp");
            entity.HasKey(e => e.ContactoWhatsAppId);
            entity.Property(e => e.ContactoWhatsAppId).HasColumnName("contacto_whatsapp_id");
            entity.Property(e => e.Nombre).HasMaxLength(100).IsUnicode(true).HasColumnName("nombre");
            entity.Property(e => e.Telefono).HasMaxLength(30).IsUnicode(false).HasColumnName("telefono");
            entity.Property(e => e.EsPrincipal).HasDefaultValue(false).HasColumnName("es_principal");
            entity.Property(e => e.Activo).HasDefaultValue(true).HasColumnName("activo");
            entity.Property(e => e.Orden).HasDefaultValue(0).HasColumnName("orden");
            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("(getdate())").HasColumnType("datetime").HasColumnName("fecha_creacion");
        });

        modelBuilder.Entity<SeccionExperiencia>(entity =>
        {
            entity.ToTable("SeccionExperiencia");
            entity.HasKey(e => e.SeccionExperienciaId);
            entity.Property(e => e.SeccionExperienciaId).HasColumnName("seccion_experiencia_id");
            entity.Property(e => e.Activo).HasDefaultValue(true).HasColumnName("activo");
            entity.Property(e => e.Titulo).HasMaxLength(200).IsUnicode(true).HasColumnName("titulo");
            entity.Property(e => e.Subtitulo).HasMaxLength(300).IsUnicode(true).HasColumnName("subtitulo");
            entity.Property(e => e.Descripcion).HasMaxLength(1000).IsUnicode(true).HasColumnName("descripcion");
            entity.Property(e => e.BeneficiosJson).IsUnicode(true).HasColumnName("beneficios_json");
            entity.Property(e => e.TextoBoton).HasMaxLength(100).IsUnicode(true).HasColumnName("texto_boton");
            entity.Property(e => e.UrlBoton).HasMaxLength(500).IsUnicode(false).HasColumnName("url_boton");
            entity.Property(e => e.TiempoAutoplay).HasDefaultValue(5).HasColumnName("tiempo_autoplay");
            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("(getdate())").HasColumnType("datetime").HasColumnName("fecha_creacion");
            entity.Property(e => e.FechaModificacion).HasColumnType("datetime").HasColumnName("fecha_modificacion");
        });

        modelBuilder.Entity<ExperienciaMultimediaItem>(entity =>
        {
            entity.ToTable("ExperienciaMultimediaItem");
            entity.HasKey(e => e.ExperienciaMultimediaItemId);
            entity.Property(e => e.ExperienciaMultimediaItemId).HasColumnName("experiencia_multimedia_item_id");
            entity.Property(e => e.CarruselNumero).HasDefaultValue(1).HasColumnName("carrusel_numero");
            entity.Property(e => e.Tipo).HasMaxLength(50).IsUnicode(false).HasColumnName("tipo");
            entity.Property(e => e.Url).HasMaxLength(500).IsUnicode(false).HasColumnName("url");
            entity.Property(e => e.Titulo).HasMaxLength(200).IsUnicode(true).HasColumnName("titulo");
            entity.Property(e => e.Orden).HasDefaultValue(0).HasColumnName("orden");
            entity.Property(e => e.Activo).HasDefaultValue(true).HasColumnName("activo");
            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("(getdate())").HasColumnType("datetime").HasColumnName("fecha_creacion");
        });

        // Módulo Asistente IA
        modelBuilder.Entity<AIConfiguracion>(entity =>
        {
            entity.ToTable("AIConfiguracion");
            entity.HasKey(e => e.AIConfiguracionId);
            entity.Property(e => e.AIConfiguracionId).HasColumnName("ai_configuracion_id");
            entity.Property(e => e.ChatActivo).HasColumnName("chat_activo").HasDefaultValue(true);
            entity.Property(e => e.MostrarBoton).HasColumnName("mostrar_boton").HasDefaultValue(true);
            entity.Property(e => e.NombreAsistente).HasMaxLength(100).IsUnicode(true).HasColumnName("nombre_asistente");
            entity.Property(e => e.MensajeBienvenida).HasMaxLength(500).IsUnicode(true).HasColumnName("mensaje_bienvenida");
            entity.Property(e => e.MensajeDespedida).HasMaxLength(500).IsUnicode(true).HasColumnName("mensaje_despedida");
            entity.Property(e => e.AvatarUrl).HasMaxLength(500).IsUnicode(false).HasColumnName("avatar_url");
            entity.Property(e => e.MensajeBloqueo).HasMaxLength(500).IsUnicode(true).HasColumnName("mensaje_bloqueo");
            entity.Property(e => e.PromptSistema).IsUnicode(true).HasColumnName("prompt_sistema");
            entity.Property(e => e.Tono).HasMaxLength(50).IsUnicode(false).HasColumnName("tono");
            entity.Property(e => e.LongitudMaxima).HasColumnName("longitud_maxima").HasDefaultValue(500);
            entity.Property(e => e.Formalidad).HasMaxLength(50).IsUnicode(false).HasColumnName("formalidad");
            entity.Property(e => e.OllamaUrl).HasMaxLength(500).IsUnicode(false).HasColumnName("ollama_url");
            entity.Property(e => e.Modelo).HasMaxLength(100).IsUnicode(false).HasColumnName("modelo");
            entity.Property(e => e.Temperatura).HasColumnType("decimal(3, 2)").HasColumnName("temperatura").HasDefaultValue(0.70m);
            entity.Property(e => e.MaxTokens).HasColumnName("max_tokens").HasDefaultValue(2048);
            entity.Property(e => e.TimeoutSegundos).HasColumnName("timeout_segundos").HasDefaultValue(120);
            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("(getdate())").HasColumnType("datetime2").HasColumnName("fecha_creacion");
            entity.Property(e => e.FechaModificacion).HasDefaultValueSql("(getdate())").HasColumnType("datetime2").HasColumnName("fecha_modificacion");
        });

        modelBuilder.Entity<AITema>(entity =>
        {
            entity.ToTable("AITema");
            entity.HasKey(e => e.AITemaId);
            entity.Property(e => e.AITemaId).HasColumnName("ai_tema_id");
            entity.Property(e => e.Nombre).HasMaxLength(200).IsUnicode(true).HasColumnName("nombre");
            entity.Property(e => e.Descripcion).HasMaxLength(500).IsUnicode(true).HasColumnName("descripcion");
            entity.Property(e => e.Activo).HasColumnName("activo").HasDefaultValue(true);
            entity.Property(e => e.Orden).HasColumnName("orden").HasDefaultValue(0);
            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("(getdate())").HasColumnType("datetime2").HasColumnName("fecha_creacion");
        });

        modelBuilder.Entity<AIPalabraRestringida>(entity =>
        {
            entity.ToTable("AIPalabraRestringida");
            entity.HasKey(e => e.AIPalabraRestringidaId);
            entity.Property(e => e.AIPalabraRestringidaId).HasColumnName("ai_palabra_restringida_id");
            entity.Property(e => e.Palabra).HasMaxLength(200).IsUnicode(true).HasColumnName("palabra");
            entity.Property(e => e.Activo).HasColumnName("activo").HasDefaultValue(true);
            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("(getdate())").HasColumnType("datetime2").HasColumnName("fecha_creacion");
        });

        modelBuilder.Entity<AIContextoConfig>(entity =>
        {
            entity.ToTable("AIContextoConfig");
            entity.HasKey(e => e.AIContextoConfigId);
            entity.HasIndex(e => e.Clave, "UQ_AIContextoConfig_Clave").IsUnique();
            entity.Property(e => e.AIContextoConfigId).HasColumnName("ai_contexto_config_id");
            entity.Property(e => e.Clave).HasMaxLength(100).IsUnicode(false).HasColumnName("clave");
            entity.Property(e => e.Nombre).HasMaxLength(200).IsUnicode(true).HasColumnName("nombre");
            entity.Property(e => e.Descripcion).HasMaxLength(500).IsUnicode(true).HasColumnName("descripcion");
            entity.Property(e => e.Activo).HasColumnName("activo").HasDefaultValue(true);
            entity.Property(e => e.Orden).HasColumnName("orden").HasDefaultValue(0);
            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("(getdate())").HasColumnType("datetime2").HasColumnName("fecha_creacion");
        });

        modelBuilder.Entity<AIChatHistorial>(entity =>
        {
            entity.ToTable("AIChatHistorial");
            entity.HasKey(e => e.AIChatHistorialId);
            entity.Property(e => e.AIChatHistorialId).HasColumnName("ai_chat_historial_id").UseIdentityColumn();
            entity.Property(e => e.Pregunta).IsRequired().HasColumnType("nvarchar(max)").HasColumnName("pregunta");
            entity.Property(e => e.Respuesta).IsRequired().HasColumnType("nvarchar(max)").HasColumnName("respuesta");
            entity.Property(e => e.TiempoRespuestaMs).HasColumnName("tiempo_respuesta_ms").HasDefaultValue(0);
            entity.Property(e => e.Estado).IsRequired().HasMaxLength(50).IsUnicode(false).HasColumnName("estado").HasDefaultValue("Exitosa");
            entity.Property(e => e.TemaDetectado).HasMaxLength(200).IsUnicode(true).HasColumnName("tema_detectado");
            entity.Property(e => e.SessionId).HasMaxLength(200).IsUnicode(false).HasColumnName("session_id");
            entity.Property(e => e.VisitanteId).HasColumnName("visitante_id");
            entity.Property(e => e.FechaConsulta).HasDefaultValueSql("(getdate())").HasColumnType("datetime2").HasColumnName("fecha_consulta");

            entity.HasOne(e => e.Visitante).WithMany()
                .HasForeignKey(e => e.VisitanteId).OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_AIChatHistorial_Visitante");
        });
    }
}
