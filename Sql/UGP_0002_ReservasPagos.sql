-- PROPUESTA PARA REVISION. NO ejecutado por la aplicación.
-- Aplicar exclusivamente tras aprobación, primero en una base SQL desechable.
-- El único baseline existente es UGP_Database_Schema.sql; permanece intacto.
-- No migra compras históricas: requieren conciliación y adopción explícita.
-- Revision 2. Sin runner ni SchemaVersion asumidos: gobernanza pendiente de aprobación.
-- Conexión dedicada, sin transacción externa. Las marcas de sesión son precondiciones
-- de revisión, no sustituyen permisos SQL ni autorización del operador.
-- RAISERROR aquí no revierte una transacción ajena mediante XACT_ABORT.
IF @@TRANCOUNT <> 0
BEGIN
    RAISERROR(N'Se requiere una conexion sin transaccion externa.', 16, 1);
    RETURN;
END;
SET XACT_ABORT ON;
SET NOCOUNT ON;
DECLARE @BaseRevisada sysname = TRY_CONVERT(sysname, SESSION_CONTEXT(N'UGP.BaseRevisada'));
IF @BaseRevisada IS NULL OR @BaseRevisada <> DB_NAME()
    THROW 51000, 'Destino no confirmado para esta sesion.', 1;
IF ISNULL(TRY_CONVERT(nvarchar(100), SESSION_CONTEXT(N'UGP.AutorizacionDDL')), N'') <> N'UGP_0002_REVISION_2'
    THROW 51000, 'Falta revision explicita de este script.', 1;
IF TRY_CONVERT(int, SERVERPROPERTY('ProductMajorVersion')) < 13
    THROW 51000, 'Se requiere SQL Server 2016 o posterior.', 1;
BEGIN TRY
BEGIN TRANSACTION;
DECLARE @Bloqueo int;
EXEC @Bloqueo = sys.sp_getapplock @Resource=N'UGP:Esquema', @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=10000;
IF @Bloqueo < 0 THROW 51000, 'No se pudo bloquear el despliegue.', 1;
IF EXISTS (SELECT 1 FROM sys.objects WHERE schema_id=SCHEMA_ID(N'dbo') AND name IN
    (N'ReservasCompra',N'IntentosPago',N'TransaccionesPago',N'IncidenciasPago',N'NotificacionesPago',N'VerificacionesPago',N'CK_Eventos_Inventario'))
    THROW 51000, 'Objetos existentes: revisar version o instalacion parcial; no se omiten diferencias.', 1;
IF EXISTS (
    SELECT 1 FROM (VALUES
        (N'Eventos',N'evento_id'),(N'Eventos',N'capacidad_maxima'),(N'Eventos',N'tickets_disponibles'),
        (N'Compras',N'compra_id')) esperado(tabla,columna)
    LEFT JOIN sys.tables t ON t.name=esperado.tabla AND t.schema_id=SCHEMA_ID(N'dbo')
    LEFT JOIN sys.columns c ON c.object_id=t.object_id AND c.name=esperado.columna
    WHERE c.column_id IS NULL OR c.system_type_id<>56 OR c.is_nullable<>0)
    THROW 51000, 'Esquema base incompatible: se requieren columnas int NOT NULL verificadas.', 1;
IF EXISTS (SELECT 1 FROM dbo.Eventos WITH(TABLOCKX,HOLDLOCK)
    WHERE capacidad_maxima<0 OR tickets_disponibles<0 OR tickets_disponibles>capacidad_maxima)
    THROW 51000, 'Inventario previo inconsistente: no se corrigieron datos.', 1;

-- Falla sin corregir datos si el inventario existente ya es inconsistente.
ALTER TABLE dbo.Eventos WITH CHECK ADD CONSTRAINT CK_Eventos_Inventario
    CHECK (capacidad_maxima >= 0 AND tickets_disponibles >= 0 AND tickets_disponibles <= capacidad_maxima);

CREATE TABLE dbo.ReservasCompra (
    CompraId int NOT NULL CONSTRAINT PK_ReservasCompra PRIMARY KEY CONSTRAINT FK_ReservasCompra_Compras_CompraId REFERENCES dbo.Compras(compra_id),
    CreadaUtc datetime2 NOT NULL,
    ExpiraUtc datetime2 NOT NULL,
    Estado nvarchar(20) NOT NULL,
    ConfirmadaUtc datetime2 NULL,
    LiberadaUtc datetime2 NULL,
    MotivoLiberacion nvarchar(100) NULL,
    OperacionId uniqueidentifier NOT NULL CONSTRAINT IX_ReservasCompra_OperacionId UNIQUE,
    AsistentesJson nvarchar(max) NULL,
    ReintentarLiberacionUtc datetime2 NULL,
    CONSTRAINT CK_Reserva_TTL CHECK (ExpiraUtc > CreadaUtc AND ExpiraUtc <= DATEADD(minute,15,CreadaUtc)),
    CONSTRAINT CK_Reserva_Estado CHECK (Estado IN ('Vigente','Confirmada','Liberada')),
    CONSTRAINT CK_Reserva_Confirmacion CHECK ((Estado='Confirmada' AND ConfirmadaUtc IS NOT NULL AND ConfirmadaUtc>=CreadaUtc AND ConfirmadaUtc<ExpiraUtc) OR (Estado<>'Confirmada' AND ConfirmadaUtc IS NULL)),
    CONSTRAINT CK_Reserva_Liberacion CHECK (
        (Estado = 'Liberada' AND LiberadaUtc IS NOT NULL AND LiberadaUtc>=CreadaUtc AND MotivoLiberacion IS NOT NULL) OR
        (Estado <> 'Liberada' AND LiberadaUtc IS NULL AND MotivoLiberacion IS NULL)),
    CONSTRAINT CK_Reserva_Asistentes CHECK (AsistentesJson IS NULL OR ISJSON(AsistentesJson)=1)
);
CREATE INDEX IX_ReservasCompra_Estado_ExpiraUtc ON dbo.ReservasCompra(Estado,ExpiraUtc);
CREATE TABLE dbo.IntentosPago (
    Id uniqueidentifier NOT NULL CONSTRAINT PK_IntentosPago PRIMARY KEY,
    CompraId int NOT NULL CONSTRAINT IX_IntentosPago_CompraId UNIQUE CONSTRAINT FK_IntentosPago_ReservasCompra_CompraId REFERENCES dbo.ReservasCompra(CompraId),
    Referencia nvarchar(100) NOT NULL,
    Entorno nvarchar(10) NOT NULL CONSTRAINT CK_Intento_Entorno CHECK (Entorno IN ('Sandbox','Production')),
    Centavos bigint NOT NULL CONSTRAINT CK_Intento_Centavos CHECK (Centavos > 0),
    Moneda nvarchar(3) NOT NULL CONSTRAINT CK_Intento_Moneda CHECK (Moneda='COP'),
    CreadoUtc datetime2 NOT NULL,
    UltimaVerificacionUtc datetime2 NULL,
    SiguienteConciliacionUtc datetime2 NULL,
    Estado nvarchar(30) NOT NULL,
    CONSTRAINT AK_IntentosPago_Id_Entorno UNIQUE(Id,Entorno),
    CONSTRAINT CK_Intento_Estado CHECK (Estado IN ('SinTransaccion','PorConciliar','SinIdPorConciliar','Aprobado')),
    CONSTRAINT UQ_IntentosPago_Referencia UNIQUE(Entorno,Referencia)
);
CREATE INDEX IX_IntentosPago_Entorno_SiguienteConciliacionUtc ON dbo.IntentosPago(Entorno,SiguienteConciliacionUtc) WHERE SiguienteConciliacionUtc IS NOT NULL;
CREATE TABLE dbo.TransaccionesPago (
    Id uniqueidentifier NOT NULL CONSTRAINT PK_TransaccionesPago PRIMARY KEY,
    IntentoPagoId uniqueidentifier NULL,
    Entorno nvarchar(10) NOT NULL,
    WompiId nvarchar(100) COLLATE Latin1_General_100_BIN2 NOT NULL,
    Estado nvarchar(30) NOT NULL,
    RegistradaUtc datetime2 NOT NULL,
    VerificadaUtc datetime2 NULL,
    SiguienteConciliacionUtc datetime2 NULL,
    UltimoResultado nvarchar(40) NULL,
    VersionNotificada bigint NOT NULL CONSTRAINT DF_Transaccion_VersionNotificada DEFAULT 0,
    VersionProcesada bigint NOT NULL CONSTRAINT DF_Transaccion_VersionProcesada DEFAULT 0,
    CONSTRAINT CK_Transaccion_Version CHECK (VersionProcesada>=0 AND VersionNotificada>=VersionProcesada),
    CONSTRAINT CK_Transaccion_Estado CHECK (Estado IN ('SinVerificar','PENDING','APPROVED','DECLINED','VOIDED','ERROR')),
    CONSTRAINT CK_Transaccion_Entorno CHECK (Entorno IN ('Sandbox','Production')),
    CONSTRAINT FK_TransaccionesPago_IntentosPago FOREIGN KEY (IntentoPagoId,Entorno) REFERENCES dbo.IntentosPago(Id,Entorno),
    CONSTRAINT UQ_TransaccionesPago_Wompi UNIQUE(Entorno,WompiId)
);
CREATE INDEX IX_TransaccionesPago_Entorno_SiguienteConciliacionUtc ON dbo.TransaccionesPago(Entorno,SiguienteConciliacionUtc) WHERE SiguienteConciliacionUtc IS NOT NULL;
CREATE INDEX IX_TransaccionesPago_IntentoPagoId_Entorno ON dbo.TransaccionesPago(IntentoPagoId,Entorno);
CREATE TABLE dbo.IncidenciasPago (
    Id uniqueidentifier NOT NULL CONSTRAINT PK_IncidenciasPago PRIMARY KEY,
    TransaccionPagoId uniqueidentifier NOT NULL CONSTRAINT IX_IncidenciasPago_TransaccionPagoId UNIQUE CONSTRAINT FK_IncidenciasPago_TransaccionesPago_TransaccionPagoId REFERENCES dbo.TransaccionesPago(Id),
    Motivo nvarchar(100) NOT NULL,
    Estado nvarchar(20) NOT NULL CONSTRAINT CK_Incidencia_Estado CHECK (Estado IN ('Pendiente','EnRevision','Resuelta')),
    CreadaUtc datetime2 NOT NULL,
    ResueltaUtc datetime2 NULL,
    ResueltaPor nvarchar(450) NULL,
    EvidenciaResolucion nvarchar(2000) NULL,
    CONSTRAINT CK_Incidencia_Resolucion CHECK (
        (Estado='Resuelta' AND ResueltaUtc IS NOT NULL AND ResueltaPor IS NOT NULL AND LEN(LTRIM(RTRIM(ResueltaPor))) > 0
            AND EvidenciaResolucion IS NOT NULL AND LEN(LTRIM(RTRIM(EvidenciaResolucion))) > 0) OR
        (Estado<>'Resuelta' AND ResueltaUtc IS NULL))
);
CREATE TABLE dbo.NotificacionesPago (
    Id uniqueidentifier NOT NULL CONSTRAINT PK_NotificacionesPago PRIMARY KEY,
    TransaccionPagoId uniqueidentifier NOT NULL REFERENCES dbo.TransaccionesPago(Id),
    Huella varchar(64) NOT NULL,
    RecibidaUtc datetime2 NOT NULL,
    CONSTRAINT UQ_Notificacion_Huella UNIQUE(TransaccionPagoId,Huella)
);
CREATE TABLE dbo.VerificacionesPago (
    Id uniqueidentifier NOT NULL CONSTRAINT PK_VerificacionesPago PRIMARY KEY,
    TransaccionPagoId uniqueidentifier NOT NULL REFERENCES dbo.TransaccionesPago(Id),
    VersionConsultada bigint NOT NULL CONSTRAINT CK_Verificacion_Version CHECK (VersionConsultada>=0),
    ConsultadaUtc datetime2 NOT NULL,
    EstadoObservado nvarchar(30) NULL,
    Resultado nvarchar(40) NOT NULL
);
CREATE INDEX IX_VerificacionesPago_TransaccionPagoId_ConsultadaUtc ON dbo.VerificacionesPago(TransaccionPagoId,ConsultadaUtc);
COMMIT;
END TRY
BEGIN CATCH
    IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
