-- PLANTILLA DE DATOS PARA REVISION SEPARADA. NO EJECUTADA.
-- Requiere UGP_0002, aplicación detenida y conciliación de las compras seleccionadas.
-- No inferir UTC desde fecha_compra: el operador debe confirmar cada instante.
-- Solo adopta compras ya vencidas; el worker las liberará bajo D-02.
-- Un cobro aprobado descubierto después pasa a conciliación, NO emite tickets.
SET XACT_ABORT ON;
SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;
DECLARE @Historicas TABLE (CompraId int PRIMARY KEY, CreadaUtc datetime2 NOT NULL, Entorno nvarchar(10) NOT NULL);
-- Completar EXCLUSIVAMENTE con IDs/fechas verificados y aprobados; no hay valores de ejemplo ejecutables.
-- INSERT INTO @Historicas(CompraId,CreadaUtc,Entorno) VALUES (...);
IF NOT EXISTS (SELECT 1 FROM @Historicas)
    THROW 51002, 'Se requiere un manifiesto de compras históricas revisado.', 1;
BEGIN TRANSACTION;
IF EXISTS (SELECT 1 FROM @Historicas h LEFT JOIN dbo.Compras c WITH(UPDLOCK,HOLDLOCK) ON c.compra_id=h.CompraId
    LEFT JOIN dbo.MetodosPago m ON m.metodo_pago_id=c.metodo_pago_id
    WHERE c.compra_id IS NULL OR c.estado_pago IS NULL OR c.estado_pago<>'Pendiente' OR m.nombre IS NULL OR m.nombre<>'Wompi'
        OR h.Entorno NOT IN ('Sandbox','Production') OR DATEADD(minute,15,h.CreadaUtc)>SYSUTCDATETIME()
        OR EXISTS (SELECT 1 FROM dbo.Tickets t WHERE t.compra_id=c.compra_id))
    THROW 51003, 'Manifiesto incompatible: revisar estados, fechas, método y tickets.', 1;

INSERT INTO dbo.ReservasCompra(CompraId,CreadaUtc,ExpiraUtc,Estado,OperacionId)
SELECT h.CompraId,h.CreadaUtc,DATEADD(minute,15,h.CreadaUtc),'Vigente',NEWID()
FROM @Historicas h WHERE NOT EXISTS (SELECT 1 FROM dbo.ReservasCompra r WHERE r.CompraId=h.CompraId);

INSERT INTO dbo.IntentosPago(Id,CompraId,Referencia,Entorno,Centavos,Moneda,CreadoUtc,SiguienteConciliacionUtc,Estado)
SELECT NEWID(),h.CompraId,
    CONCAT('UGP-TX-',CONVERT(char(8),ISNULL(c.fecha_compra,CONVERT(datetime,'19700101')),112),'-',
        REPLICATE('0',CASE WHEN LEN(CONVERT(varchar(10),h.CompraId))<6 THEN 6-LEN(CONVERT(varchar(10),h.CompraId)) ELSE 0 END),h.CompraId),
    h.Entorno,CONVERT(bigint,c.monto_total*100),'COP',h.CreadaUtc,SYSUTCDATETIME(),'PorConciliar'
FROM @Historicas h JOIN dbo.Compras c ON c.compra_id=h.CompraId
WHERE NOT EXISTS (SELECT 1 FROM dbo.IntentosPago i WHERE i.CompraId=h.CompraId);
COMMIT;
-- No modifica inventario directamente. No ejecutar el worker hasta aprobar el manifiesto.
