using Microsoft.EntityFrameworkCore;
using UGP_ultimo_guerrero_en_pie_.Models.Entities;

namespace UGP_ultimo_guerrero_en_pie_.Data;

internal static class ReservasMapping
{
    internal static void Configure(ModelBuilder model)
    {
        var r = model.Entity<ReservaCompra>();
        r.ToTable("ReservasCompra", "dbo", b =>
        {
            b.HasCheckConstraint("CK_Reserva_TTL", "ExpiraUtc > CreadaUtc AND ExpiraUtc <= DATEADD(minute,15,CreadaUtc)");
            b.HasCheckConstraint("CK_Reserva_Estado", "Estado IN ('Vigente','Confirmada','Liberada')");
            b.HasCheckConstraint("CK_Reserva_Confirmacion", "(Estado='Confirmada' AND ConfirmadaUtc IS NOT NULL AND ConfirmadaUtc>=CreadaUtc AND ConfirmadaUtc<ExpiraUtc) OR (Estado<>'Confirmada' AND ConfirmadaUtc IS NULL)");
            b.HasCheckConstraint("CK_Reserva_Liberacion", "(Estado='Liberada' AND LiberadaUtc IS NOT NULL AND LiberadaUtc>=CreadaUtc AND MotivoLiberacion IS NOT NULL) OR (Estado<>'Liberada' AND LiberadaUtc IS NULL AND MotivoLiberacion IS NULL)");
            b.HasCheckConstraint("CK_Reserva_Asistentes", "AsistentesJson IS NULL OR ISJSON(AsistentesJson)=1");
        });
        r.HasKey(x => x.CompraId);
        r.Property(x => x.CompraId).ValueGeneratedNever();
        r.Property(x => x.Estado).HasMaxLength(20);
        r.Property(x => x.MotivoLiberacion).HasMaxLength(100);
        r.HasIndex(x => x.OperacionId).IsUnique();
        r.HasIndex(x => new { x.Estado, x.ExpiraUtc });
        r.HasOne(x => x.Compra).WithOne().HasForeignKey<ReservaCompra>(x => x.CompraId).OnDelete(DeleteBehavior.NoAction);

        var i = model.Entity<IntentoPago>();
        i.ToTable("IntentosPago", "dbo", b =>
        {
            b.HasCheckConstraint("CK_Intento_Entorno", "Entorno IN ('Sandbox','Production')");
            b.HasCheckConstraint("CK_Intento_Centavos", "Centavos > 0");
            b.HasCheckConstraint("CK_Intento_Moneda", "Moneda='COP'");
            b.HasCheckConstraint("CK_Intento_Estado", "Estado IN ('SinTransaccion','PorConciliar','SinIdPorConciliar','Aprobado')");
        });
        i.HasKey(x => x.Id);
        i.Property(x => x.Id).ValueGeneratedNever();
        i.Property(x => x.Referencia).HasMaxLength(100);
        i.Property(x => x.Entorno).HasMaxLength(10);
        i.Property(x => x.Moneda).HasMaxLength(3);
        i.Property(x => x.Estado).HasMaxLength(30);
        i.HasIndex(x => x.CompraId).IsUnique();
        i.HasIndex(x => new { x.Entorno, x.Referencia }).IsUnique().HasDatabaseName("UQ_IntentosPago_Referencia");
        i.HasAlternateKey(x => new { x.Id, x.Entorno });
        i.HasIndex(x => new { x.Entorno, x.SiguienteConciliacionUtc }).HasFilter("[SiguienteConciliacionUtc] IS NOT NULL");
        i.HasOne(x => x.Reserva).WithOne().HasForeignKey<IntentoPago>(x => x.CompraId).OnDelete(DeleteBehavior.NoAction);

        var t = model.Entity<TransaccionPago>();
        t.ToTable("TransaccionesPago", "dbo", b =>
        {
            b.HasCheckConstraint("CK_Transaccion_Version", "VersionProcesada>=0 AND VersionNotificada>=VersionProcesada");
            b.HasCheckConstraint("CK_Transaccion_Estado", "Estado IN ('SinVerificar','PENDING','APPROVED','DECLINED','VOIDED','ERROR')");
            b.HasCheckConstraint("CK_Transaccion_Entorno", "Entorno IN ('Sandbox','Production')");
        });
        t.HasKey(x => x.Id);
        t.Property(x => x.Id).ValueGeneratedNever();
        t.Property(x => x.Entorno).HasMaxLength(10);
        t.Property(x => x.WompiId).HasMaxLength(100).UseCollation("Latin1_General_100_BIN2");
        t.Property(x => x.Estado).HasMaxLength(30);
        t.Property(x => x.UltimoResultado).HasMaxLength(40);
        t.Property(x => x.VersionNotificada).HasDefaultValue(0L);
        t.Property(x => x.VersionProcesada).HasDefaultValue(0L);
        t.HasIndex(x => new { x.Entorno, x.WompiId }).IsUnique().HasDatabaseName("UQ_TransaccionesPago_Wompi");
        t.HasIndex(x => new { x.Entorno, x.SiguienteConciliacionUtc }).HasFilter("[SiguienteConciliacionUtc] IS NOT NULL");
        t.HasIndex(x => new { x.IntentoPagoId, x.Entorno });
        t.HasOne(x => x.Intento).WithMany().HasForeignKey(x => new { x.IntentoPagoId, x.Entorno })
            .HasPrincipalKey(x => new { x.Id, x.Entorno }).OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_TransaccionesPago_IntentosPago");

        var c = model.Entity<IncidenciaPago>();
        c.ToTable("IncidenciasPago", "dbo", b =>
        {
            b.HasCheckConstraint("CK_Incidencia_Estado", "Estado IN ('Pendiente','EnRevision','Resuelta')");
            b.HasCheckConstraint("CK_Incidencia_Resolucion", "(Estado='Resuelta' AND ResueltaUtc IS NOT NULL AND ResueltaPor IS NOT NULL AND LEN(LTRIM(RTRIM(ResueltaPor)))>0 AND EvidenciaResolucion IS NOT NULL AND LEN(LTRIM(RTRIM(EvidenciaResolucion)))>0) OR (Estado<>'Resuelta' AND ResueltaUtc IS NULL)");
        });
        c.HasKey(x => x.Id);
        c.Property(x => x.Id).ValueGeneratedNever();
        c.Property(x => x.Estado).HasMaxLength(20);
        c.Property(x => x.Motivo).HasMaxLength(100);
        c.Property(x => x.EvidenciaResolucion).HasMaxLength(2000);
        c.Property(x => x.ResueltaPor).HasMaxLength(450);
        c.HasIndex(x => x.TransaccionPagoId).IsUnique();
        c.HasOne(x => x.Transaccion).WithOne().HasForeignKey<IncidenciaPago>(x => x.TransaccionPagoId).OnDelete(DeleteBehavior.NoAction);

        var n = model.Entity<NotificacionPago>();
        n.ToTable("NotificacionesPago", "dbo");
        n.HasKey(x => x.Id);
        n.Property(x => x.Id).ValueGeneratedNever();
        n.Property(x => x.Huella).HasMaxLength(64).IsUnicode(false);
        n.HasIndex(x => new { x.TransaccionPagoId, x.Huella }).IsUnique().HasDatabaseName("UQ_Notificacion_Huella");
        n.HasOne(x => x.Transaccion).WithMany().HasForeignKey(x => x.TransaccionPagoId).OnDelete(DeleteBehavior.NoAction);
        var v = model.Entity<VerificacionPago>();
        v.ToTable("VerificacionesPago", "dbo", b => b.HasCheckConstraint("CK_Verificacion_Version", "VersionConsultada>=0"));
        v.HasKey(x => x.Id);
        v.Property(x => x.Id).ValueGeneratedNever();
        v.Property(x => x.EstadoObservado).HasMaxLength(30);
        v.Property(x => x.Resultado).HasMaxLength(40);
        v.HasIndex(x => new { x.TransaccionPagoId, x.ConsultadaUtc });
        v.HasOne(x => x.Transaccion).WithMany().HasForeignKey(x => x.TransaccionPagoId).OnDelete(DeleteBehavior.NoAction);
    }
}
