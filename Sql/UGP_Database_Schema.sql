IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE SEQUENCE [seq_ticket_codigo] START WITH 1000 INCREMENT BY 1 NO CYCLE;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE TABLE [AIConfiguracion] (
        [ai_configuracion_id] int NOT NULL IDENTITY,
        [chat_activo] bit NOT NULL DEFAULT CAST(1 AS bit),
        [mostrar_boton] bit NOT NULL DEFAULT CAST(1 AS bit),
        [nombre_asistente] nvarchar(100) NOT NULL,
        [mensaje_bienvenida] nvarchar(500) NOT NULL,
        [mensaje_despedida] nvarchar(500) NOT NULL,
        [avatar_url] varchar(500) NULL,
        [mensaje_bloqueo] nvarchar(500) NOT NULL,
        [prompt_sistema] nvarchar(max) NOT NULL,
        [tono] varchar(50) NOT NULL,
        [longitud_maxima] int NOT NULL DEFAULT 500,
        [formalidad] varchar(50) NOT NULL,
        [ollama_url] varchar(500) NOT NULL,
        [modelo] varchar(100) NOT NULL,
        [temperatura] decimal(3,2) NOT NULL DEFAULT 0.7,
        [max_tokens] int NOT NULL DEFAULT 2048,
        [timeout_segundos] int NOT NULL DEFAULT 120,
        [fecha_creacion] datetime2 NOT NULL DEFAULT ((getdate())),
        [fecha_modificacion] datetime2 NOT NULL DEFAULT ((getdate())),
        CONSTRAINT [PK_AIConfiguracion] PRIMARY KEY ([ai_configuracion_id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE TABLE [AIContextoConfig] (
        [ai_contexto_config_id] int NOT NULL IDENTITY,
        [clave] varchar(100) NOT NULL,
        [nombre] nvarchar(200) NOT NULL,
        [descripcion] nvarchar(500) NULL,
        [activo] bit NOT NULL DEFAULT CAST(1 AS bit),
        [orden] int NOT NULL DEFAULT 0,
        [fecha_creacion] datetime2 NOT NULL DEFAULT ((getdate())),
        CONSTRAINT [PK_AIContextoConfig] PRIMARY KEY ([ai_contexto_config_id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE TABLE [AIPalabraRestringida] (
        [ai_palabra_restringida_id] int NOT NULL IDENTITY,
        [palabra] nvarchar(200) NOT NULL,
        [activo] bit NOT NULL DEFAULT CAST(1 AS bit),
        [fecha_creacion] datetime2 NOT NULL DEFAULT ((getdate())),
        CONSTRAINT [PK_AIPalabraRestringida] PRIMARY KEY ([ai_palabra_restringida_id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE TABLE [AITema] (
        [ai_tema_id] int NOT NULL IDENTITY,
        [nombre] nvarchar(200) NOT NULL,
        [descripcion] nvarchar(500) NULL,
        [activo] bit NOT NULL DEFAULT CAST(1 AS bit),
        [orden] int NOT NULL DEFAULT 0,
        [fecha_creacion] datetime2 NOT NULL DEFAULT ((getdate())),
        CONSTRAINT [PK_AITema] PRIMARY KEY ([ai_tema_id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetRoles] (
        [Id] nvarchar(450) NOT NULL,
        [Name] nvarchar(256) NULL,
        [NormalizedName] nvarchar(256) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetUsers] (
        [Id] nvarchar(450) NOT NULL,
        [NombreCompleto] nvarchar(max) NOT NULL,
        [DocumentoIdentidad] nvarchar(max) NULL,
        [Telefono] nvarchar(max) NULL,
        [FechaRegistro] datetime2 NOT NULL,
        [UserName] nvarchar(256) NULL,
        [NormalizedUserName] nvarchar(256) NULL,
        [Email] nvarchar(256) NULL,
        [NormalizedEmail] nvarchar(256) NULL,
        [EmailConfirmed] bit NOT NULL,
        [PasswordHash] nvarchar(max) NULL,
        [SecurityStamp] nvarchar(max) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        [PhoneNumber] nvarchar(max) NULL,
        [PhoneNumberConfirmed] bit NOT NULL,
        [TwoFactorEnabled] bit NOT NULL,
        [LockoutEnd] datetimeoffset NULL,
        [LockoutEnabled] bit NOT NULL,
        [AccessFailedCount] int NOT NULL,
        CONSTRAINT [PK_AspNetUsers] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE TABLE [ConfiguracionWhatsApp] (
        [configuracion_whatsapp_id] int NOT NULL IDENTITY,
        [activo] bit NOT NULL DEFAULT CAST(1 AS bit),
        [tooltip_texto] nvarchar(200) NOT NULL,
        [mensaje_predeterminado] nvarchar(1000) NOT NULL,
        [mensaje_fuera_horario] nvarchar(500) NULL,
        [hora_inicio] time NULL,
        [hora_fin] time NULL,
        [mostrar_en_admin] bit NOT NULL DEFAULT CAST(1 AS bit),
        [mostrar_en_publico] bit NOT NULL DEFAULT CAST(1 AS bit),
        [paginas_mostrar] varchar(max) NULL,
        [fecha_actualizacion] datetime NOT NULL DEFAULT ((getdate())),
        CONSTRAINT [PK_ConfiguracionWhatsApp] PRIMARY KEY ([configuracion_whatsapp_id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE TABLE [ContactoWhatsApp] (
        [contacto_whatsapp_id] int NOT NULL IDENTITY,
        [nombre] nvarchar(100) NOT NULL,
        [telefono] varchar(30) NOT NULL,
        [es_principal] bit NOT NULL DEFAULT CAST(0 AS bit),
        [activo] bit NOT NULL DEFAULT CAST(1 AS bit),
        [orden] int NOT NULL DEFAULT 0,
        [fecha_creacion] datetime NOT NULL DEFAULT ((getdate())),
        CONSTRAINT [PK_ContactoWhatsApp] PRIMARY KEY ([contacto_whatsapp_id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE TABLE [ContenidoSitio] (
        [contenido_sitio_id] int NOT NULL IDENTITY,
        [clave] varchar(100) NOT NULL,
        [titulo] nvarchar(200) NULL,
        [subtitulo] nvarchar(300) NULL,
        [descripcion] nvarchar(max) NULL,
        [ruta_imagen] varchar(500) NULL,
        [metadatos_json] nvarchar(max) NULL,
        [fecha_actualizacion] datetime NOT NULL DEFAULT ((getdate())),
        CONSTRAINT [PK_ContenidoSitio] PRIMARY KEY ([contenido_sitio_id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE TABLE [ExperienciaMultimediaItem] (
        [experiencia_multimedia_item_id] int NOT NULL IDENTITY,
        [carrusel_numero] int NOT NULL DEFAULT 1,
        [tipo] varchar(50) NOT NULL,
        [url] varchar(500) NOT NULL,
        [titulo] nvarchar(200) NULL,
        [orden] int NOT NULL DEFAULT 0,
        [activo] bit NOT NULL DEFAULT CAST(1 AS bit),
        [fecha_creacion] datetime NOT NULL DEFAULT ((getdate())),
        CONSTRAINT [PK_ExperienciaMultimediaItem] PRIMARY KEY ([experiencia_multimedia_item_id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE TABLE [MetodosPago] (
        [metodo_pago_id] int NOT NULL IDENTITY,
        [nombre] varchar(50) NOT NULL,
        CONSTRAINT [PK_MetodosPago] PRIMARY KEY ([metodo_pago_id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE TABLE [Patrocinadores] (
        [patrocinador_id] int NOT NULL IDENTITY,
        [empresa] varchar(100) NOT NULL,
        [contacto] varchar(100) NULL,
        [correo] varchar(100) NULL,
        [telefono] varchar(20) NULL,
        [descripcion] varchar(max) NULL,
        [url_pagina] varchar(500) NULL,
        [logo_url] varchar(500) NULL,
        [fecha_registro] datetime NULL DEFAULT ((getdate())),
        CONSTRAINT [PK_Patrocinadores] PRIMARY KEY ([patrocinador_id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE TABLE [Peleadores] (
        [peleador_id] int NOT NULL IDENTITY,
        [nombre] varchar(100) NOT NULL,
        [apodo] varchar(100) NULL,
        [nacionalidad] varchar(100) NULL,
        [peso] decimal(5,2) NULL,
        [fecha_nacimiento] date NULL,
        [peleas_ganadas] int NULL DEFAULT 0,
        [peleas_perdidas] int NULL DEFAULT 0,
        [descripcion] varchar(max) NULL,
        [foto_url] varchar(max) NULL,
        [estado] varchar(20) NULL DEFAULT 'activo',
        [fecha_registro] datetime NULL DEFAULT ((getdate())),
        CONSTRAINT [PK_Peleadores] PRIMARY KEY ([peleador_id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE TABLE [SeccionesMultimedia] (
        [SeccionMultimediaId] int NOT NULL IDENTITY,
        [Titulo] nvarchar(200) NOT NULL,
        [Subtitulo] nvarchar(300) NULL,
        [Descripcion] nvarchar(max) NULL,
        [VideoPrincipalUrl] varchar(500) NULL,
        [VideoSecundarioUrl] varchar(500) NULL,
        [BeneficiosJson] nvarchar(max) NULL,
        [TextoBotonPrincipal] nvarchar(100) NULL,
        [UrlBotonPrincipal] varchar(500) NULL,
        [TextoBotonSecundario] nvarchar(100) NULL,
        [UrlBotonSecundario] varchar(500) NULL,
        [Orden] int NOT NULL DEFAULT 0,
        [Activo] bit NOT NULL DEFAULT CAST(1 AS bit),
        [FechaCreacion] datetime2 NULL DEFAULT ((getdate())),
        [FechaModificacion] datetime2 NULL,
        CONSTRAINT [PK_SeccionesMultimedia] PRIMARY KEY ([SeccionMultimediaId])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE TABLE [SeccionExperiencia] (
        [seccion_experiencia_id] int NOT NULL IDENTITY,
        [activo] bit NOT NULL DEFAULT CAST(1 AS bit),
        [titulo] nvarchar(200) NOT NULL,
        [subtitulo] nvarchar(300) NULL,
        [descripcion] nvarchar(1000) NULL,
        [beneficios_json] nvarchar(max) NULL,
        [texto_boton] nvarchar(100) NULL,
        [url_boton] varchar(500) NULL,
        [tiempo_autoplay] int NOT NULL DEFAULT 5,
        [fecha_creacion] datetime NOT NULL DEFAULT ((getdate())),
        [fecha_modificacion] datetime NULL,
        CONSTRAINT [PK_SeccionExperiencia] PRIMARY KEY ([seccion_experiencia_id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE TABLE [TiposEntrada] (
        [tipo_entrada_id] int NOT NULL IDENTITY,
        [nombre] varchar(50) NOT NULL,
        [precio] decimal(10,2) NOT NULL,
        [capacidad] int NOT NULL,
        CONSTRAINT [PK_TiposEntrada] PRIMARY KEY ([tipo_entrada_id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE TABLE [Venue] (
        [venue_id] int NOT NULL IDENTITY,
        [nombre] nvarchar(200) NOT NULL,
        [svg_data] nvarchar(max) NULL,
        [imagen_previa] varchar(500) NULL,
        [imagen_plano] varchar(500) NULL,
        [descripcion] nvarchar(500) NULL,
        [orden] int NOT NULL DEFAULT 0,
        [activo] bit NOT NULL DEFAULT CAST(1 AS bit),
        CONSTRAINT [PK_Venue] PRIMARY KEY ([venue_id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE TABLE [Visitantes] (
        [visitante_id] int NOT NULL IDENTITY,
        [nombre] varchar(100) NOT NULL,
        [correo] varchar(150) NOT NULL,
        [telefono] varchar(30) NULL,
        [fecha_nacimiento] date NULL,
        [fecha_registro] datetime NULL DEFAULT ((getdate())),
        CONSTRAINT [PK_Visitantes] PRIMARY KEY ([visitante_id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetRoleClaims] (
        [Id] int NOT NULL IDENTITY,
        [RoleId] nvarchar(450) NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetUserClaims] (
        [Id] int NOT NULL IDENTITY,
        [UserId] nvarchar(450) NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetUserLogins] (
        [LoginProvider] nvarchar(450) NOT NULL,
        [ProviderKey] nvarchar(450) NOT NULL,
        [ProviderDisplayName] nvarchar(max) NULL,
        [UserId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
        CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetUserRoles] (
        [UserId] nvarchar(450) NOT NULL,
        [RoleId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
        CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetUserTokens] (
        [UserId] nvarchar(450) NOT NULL,
        [LoginProvider] nvarchar(450) NOT NULL,
        [Name] nvarchar(450) NOT NULL,
        [Value] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
        CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE TABLE [Eventos] (
        [evento_id] int NOT NULL IDENTITY,
        [nombre] varchar(100) NOT NULL,
        [fecha] date NOT NULL,
        [hora] time NOT NULL,
        [lugar] varchar(200) NOT NULL,
        [descripcion] varchar(max) NULL,
        [imagen_url] varchar(255) NULL,
        [capacidad_maxima] int NOT NULL,
        [tickets_disponibles] int NOT NULL,
        [venue_id] int NULL,
        [estado_evento] varchar(20) NULL DEFAULT 'Próximo',
        [fecha_creacion] datetime NULL DEFAULT ((getdate())),
        CONSTRAINT [PK_Eventos] PRIMARY KEY ([evento_id]),
        CONSTRAINT [FK_Evento_Venue] FOREIGN KEY ([venue_id]) REFERENCES [Venue] ([venue_id]) ON DELETE SET NULL
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE TABLE [VenueZone] (
        [venue_zone_id] int NOT NULL IDENTITY,
        [venue_id] int NOT NULL,
        [nombre] nvarchar(100) NOT NULL,
        [precio] decimal(18,2) NOT NULL DEFAULT 0.0,
        [capacidad] int NOT NULL DEFAULT 0,
        [disponibles] int NOT NULL DEFAULT 0,
        [color] varchar(7) NOT NULL DEFAULT '#D4AF37',
        [orden] int NOT NULL DEFAULT 0,
        [svg_path_id] varchar(100) NULL,
        [activo] bit NOT NULL DEFAULT CAST(1 AS bit),
        [tiene_asientos_numerados] bit NOT NULL DEFAULT CAST(0 AS bit),
        CONSTRAINT [PK_VenueZone] PRIMARY KEY ([venue_zone_id]),
        CONSTRAINT [FK_VenueZone_Venue] FOREIGN KEY ([venue_id]) REFERENCES [Venue] ([venue_id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE TABLE [AIChatHistorial] (
        [ai_chat_historial_id] bigint NOT NULL IDENTITY,
        [pregunta] nvarchar(max) NOT NULL,
        [respuesta] nvarchar(max) NOT NULL,
        [tiempo_respuesta_ms] int NOT NULL DEFAULT 0,
        [estado] varchar(50) NOT NULL DEFAULT 'Exitosa',
        [tema_detectado] nvarchar(200) NULL,
        [session_id] varchar(200) NULL,
        [visitante_id] int NULL,
        [fecha_consulta] datetime2 NOT NULL DEFAULT ((getdate())),
        CONSTRAINT [PK_AIChatHistorial] PRIMARY KEY ([ai_chat_historial_id]),
        CONSTRAINT [FK_AIChatHistorial_Visitante] FOREIGN KEY ([visitante_id]) REFERENCES [Visitantes] ([visitante_id]) ON DELETE SET NULL
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE TABLE [Combates] (
        [combate_id] int NOT NULL IDENTITY,
        [evento_id] int NOT NULL,
        [peleador_rojo_id] int NOT NULL,
        [peleador_azul_id] int NOT NULL,
        [ganador_id] int NULL,
        [tipo_combate] varchar(100) NULL,
        [rounds] int NULL,
        [resultado] varchar(100) NULL,
        CONSTRAINT [PK_Combates] PRIMARY KEY ([combate_id]),
        CONSTRAINT [FK_Combates_Evento] FOREIGN KEY ([evento_id]) REFERENCES [Eventos] ([evento_id]),
        CONSTRAINT [FK_Combates_Ganador] FOREIGN KEY ([ganador_id]) REFERENCES [Peleadores] ([peleador_id]),
        CONSTRAINT [FK_Combates_PeleadorAzul] FOREIGN KEY ([peleador_azul_id]) REFERENCES [Peleadores] ([peleador_id]),
        CONSTRAINT [FK_Combates_PeleadorRojo] FOREIGN KEY ([peleador_rojo_id]) REFERENCES [Peleadores] ([peleador_id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE TABLE [Compras] (
        [compra_id] int NOT NULL IDENTITY,
        [visitante_id] int NOT NULL,
        [evento_id] int NOT NULL,
        [metodo_pago_id] int NOT NULL,
        [fecha_compra] datetime NULL DEFAULT ((getdate())),
        [monto_total] decimal(10,2) NOT NULL,
        [estado_pago] varchar(20) NULL DEFAULT 'pendiente',
        CONSTRAINT [PK_Compras] PRIMARY KEY ([compra_id]),
        CONSTRAINT [FK_Compras_Evento] FOREIGN KEY ([evento_id]) REFERENCES [Eventos] ([evento_id]),
        CONSTRAINT [FK_Compras_MetodoPago] FOREIGN KEY ([metodo_pago_id]) REFERENCES [MetodosPago] ([metodo_pago_id]),
        CONSTRAINT [FK_Compras_Visitante] FOREIGN KEY ([visitante_id]) REFERENCES [Visitantes] ([visitante_id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE TABLE [EventoTiposEntrada] (
        [evento_tipo_id] int NOT NULL IDENTITY,
        [evento_id] int NOT NULL,
        [tipo_entrada_id] int NOT NULL,
        CONSTRAINT [PK_EventoTiposEntrada] PRIMARY KEY ([evento_tipo_id]),
        CONSTRAINT [FK_EventoTiposEntrada_Evento] FOREIGN KEY ([evento_id]) REFERENCES [Eventos] ([evento_id]),
        CONSTRAINT [FK_EventoTiposEntrada_TipoEntrada] FOREIGN KEY ([tipo_entrada_id]) REFERENCES [TiposEntrada] ([tipo_entrada_id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE TABLE [Experiencias] (
        [ExperienciaId] int NOT NULL IDENTITY,
        [EventoId] int NOT NULL,
        [Nombre] nvarchar(200) NOT NULL,
        [Descripcion] nvarchar(max) NULL,
        [correo_visitante] varchar(200) NULL,
        [calificacion] int NULL,
        [Estado] nvarchar(20) NOT NULL DEFAULT N'Activa',
        [video_principal_url] varchar(500) NULL,
        [video_secundario_url] varchar(500) NULL,
        [fecha_creacion] datetime NULL DEFAULT ((getdate())),
        CONSTRAINT [PK_Experiencias] PRIMARY KEY ([ExperienciaId]),
        CONSTRAINT [FK_Experiencias_Eventos_EventoId] FOREIGN KEY ([EventoId]) REFERENCES [Eventos] ([evento_id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE TABLE [Patrocinios] (
        [patrocinio_id] int NOT NULL IDENTITY,
        [patrocinador_id] int NOT NULL,
        [url_pagina] varchar(255) NULL,
        [logo] varchar(255) NULL,
        [flyer] varchar(255) NULL,
        [fecha_inicio] date NOT NULL,
        [fecha_fin] date NOT NULL,
        [costo] decimal(10,2) NOT NULL,
        [evento_id] int NOT NULL,
        [mensaje_promocional] varchar(500) NULL,
        [productos_json] varchar(max) NULL,
        [activo] bit NOT NULL DEFAULT CAST(1 AS bit),
        [orden] int NOT NULL DEFAULT 0,
        CONSTRAINT [PK_Patrocinios] PRIMARY KEY ([patrocinio_id]),
        CONSTRAINT [FK_Patrocinios_Evento] FOREIGN KEY ([evento_id]) REFERENCES [Eventos] ([evento_id]) ON DELETE CASCADE,
        CONSTRAINT [FK_Patrocinios_Patrocinador] FOREIGN KEY ([patrocinador_id]) REFERENCES [Patrocinadores] ([patrocinador_id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE TABLE [HistorialCombates] (
        [historial_id] int NOT NULL IDENTITY,
        [combate_id] int NOT NULL,
        [metodo_victoria] varchar(100) NULL,
        [rounds_totales] int NULL,
        [observaciones] varchar(max) NULL,
        CONSTRAINT [PK_HistorialCombates] PRIMARY KEY ([historial_id]),
        CONSTRAINT [FK_HistorialCombates_Combate] FOREIGN KEY ([combate_id]) REFERENCES [Combates] ([combate_id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE TABLE [DetalleCompra] (
        [detalle_id] int NOT NULL IDENTITY,
        [compra_id] int NOT NULL,
        [tipo_entrada_id] int NOT NULL,
        [cantidad] int NOT NULL,
        [precio_unitario] decimal(10,2) NOT NULL,
        [subtotal] decimal(10,2) NOT NULL,
        CONSTRAINT [PK_DetalleCompra] PRIMARY KEY ([detalle_id]),
        CONSTRAINT [FK_DetalleCompra_Compra] FOREIGN KEY ([compra_id]) REFERENCES [Compras] ([compra_id]),
        CONSTRAINT [FK_DetalleCompra_TipoEntrada] FOREIGN KEY ([tipo_entrada_id]) REFERENCES [TiposEntrada] ([tipo_entrada_id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE TABLE [Tickets] (
        [ticket_id] int NOT NULL IDENTITY,
        [compra_id] int NOT NULL,
        [evento_id] int NOT NULL,
        [tipo_entrada_id] int NOT NULL,
        [codigo_unico] varchar(30) NOT NULL,
        [codigo_qr] varchar(500) NOT NULL,
        [token_hash] varchar(200) NOT NULL,
        [tipo_ticket] varchar(50) NULL,
        [zona] varchar(50) NULL,
        [precio] decimal(10,2) NULL,
        [estado] varchar(20) NULL DEFAULT 'Activo',
        [fecha_generacion] datetime NULL DEFAULT ((getdate())),
        [pdf_path] varchar(500) NULL,
        [qr_path] varchar(500) NULL,
        CONSTRAINT [PK_Tickets] PRIMARY KEY ([ticket_id]),
        CONSTRAINT [FK_Tickets_Compra] FOREIGN KEY ([compra_id]) REFERENCES [Compras] ([compra_id]),
        CONSTRAINT [FK_Tickets_Evento] FOREIGN KEY ([evento_id]) REFERENCES [Eventos] ([evento_id]),
        CONSTRAINT [FK_Tickets_TipoEntrada] FOREIGN KEY ([tipo_entrada_id]) REFERENCES [TiposEntrada] ([tipo_entrada_id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE TABLE [Asistente] (
        [asistente_id] int NOT NULL IDENTITY,
        [ticket_id] int NOT NULL,
        [nombre] nvarchar(100) NOT NULL,
        [apellido] nvarchar(100) NOT NULL,
        [tipo_documento] varchar(20) NOT NULL,
        [numero_documento] varchar(30) NOT NULL,
        [correo] varchar(150) NOT NULL,
        [celular] varchar(30) NULL,
        [fecha_nacimiento] date NULL,
        CONSTRAINT [PK_Asistente] PRIMARY KEY ([asistente_id]),
        CONSTRAINT [FK_Ticket_Asistente] FOREIGN KEY ([ticket_id]) REFERENCES [Tickets] ([ticket_id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE TABLE [Descuentos] (
        [descuento_id] int NOT NULL IDENTITY,
        [ticket_id] int NOT NULL,
        [cantidad_minima] int NOT NULL,
        [porcentaje_descuento] decimal(5,2) NOT NULL,
        CONSTRAINT [PK_Descuentos] PRIMARY KEY ([descuento_id]),
        CONSTRAINT [FK_Descuentos_Ticket] FOREIGN KEY ([ticket_id]) REFERENCES [Tickets] ([ticket_id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AIChatHistorial_visitante_id] ON [AIChatHistorial] ([visitante_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_AIContextoConfig_Clave] ON [AIContextoConfig] ([clave]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_Asistente_Ticket] ON [Asistente] ([ticket_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [AspNetRoleClaims] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [AspNetUserRoles] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE INDEX [EmailIndex] ON [AspNetUsers] ([NormalizedEmail]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UserNameIndex] ON [AspNetUsers] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Combates_evento_id] ON [Combates] ([evento_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Combates_ganador_id] ON [Combates] ([ganador_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Combates_peleador_azul_id] ON [Combates] ([peleador_azul_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Combates_peleador_rojo_id] ON [Combates] ([peleador_rojo_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Compras_evento_id] ON [Compras] ([evento_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Compras_metodo_pago_id] ON [Compras] ([metodo_pago_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Compras_visitante_id] ON [Compras] ([visitante_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_ContenidoSitio_Clave] ON [ContenidoSitio] ([clave]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Descuentos_ticket_id] ON [Descuentos] ([ticket_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_DetalleCompra_compra_id] ON [DetalleCompra] ([compra_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_DetalleCompra_tipo_entrada_id] ON [DetalleCompra] ([tipo_entrada_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Eventos_venue_id] ON [Eventos] ([venue_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_EventoTiposEntrada_tipo_entrada_id] ON [EventoTiposEntrada] ([tipo_entrada_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_EventoTiposEntrada] ON [EventoTiposEntrada] ([evento_id], [tipo_entrada_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Experiencias_EventoId] ON [Experiencias] ([EventoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_HistorialCombates_combate_id] ON [HistorialCombates] ([combate_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Patrocinios_evento_id] ON [Patrocinios] ([evento_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Patrocinios_patrocinador_id] ON [Patrocinios] ([patrocinador_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Tickets_codigo_unico] ON [Tickets] ([codigo_unico]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Tickets_compra_id] ON [Tickets] ([compra_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Tickets_evento_id] ON [Tickets] ([evento_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Tickets_tipo_entrada_id] ON [Tickets] ([tipo_entrada_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_Tickets_CodigoQR] ON [Tickets] ([codigo_qr]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_TiposEntrada_Nombre] ON [TiposEntrada] ([nombre]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_VenueZone_venue_id] ON [VenueZone] ([venue_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_Visitantes_Correo] ON [Visitantes] ([correo]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106182136_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261106182136_InitialCreate', N'10.0.14');
END;

COMMIT;
GO

