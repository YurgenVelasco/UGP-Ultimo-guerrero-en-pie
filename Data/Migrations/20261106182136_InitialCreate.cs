using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UGP_ultimo_guerrero_en_pie_.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence(
                name: "seq_ticket_codigo",
                startValue: 1000L);

            migrationBuilder.CreateTable(
                name: "AIConfiguracion",
                columns: table => new
                {
                    ai_configuracion_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    chat_activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    mostrar_boton = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    nombre_asistente = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    mensaje_bienvenida = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    mensaje_despedida = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    avatar_url = table.Column<string>(type: "varchar(500)", unicode: false, maxLength: 500, nullable: true),
                    mensaje_bloqueo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    prompt_sistema = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    tono = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    longitud_maxima = table.Column<int>(type: "int", nullable: false, defaultValue: 500),
                    formalidad = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    ollama_url = table.Column<string>(type: "varchar(500)", unicode: false, maxLength: 500, nullable: false),
                    modelo = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    temperatura = table.Column<decimal>(type: "decimal(3,2)", nullable: false, defaultValue: 0.70m),
                    max_tokens = table.Column<int>(type: "int", nullable: false, defaultValue: 2048),
                    timeout_segundos = table.Column<int>(type: "int", nullable: false, defaultValue: 120),
                    fecha_creacion = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(getdate())"),
                    fecha_modificacion = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AIConfiguracion", x => x.ai_configuracion_id);
                });

            migrationBuilder.CreateTable(
                name: "AIContextoConfig",
                columns: table => new
                {
                    ai_contexto_config_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    clave = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    nombre = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    descripcion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    orden = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    fecha_creacion = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AIContextoConfig", x => x.ai_contexto_config_id);
                });

            migrationBuilder.CreateTable(
                name: "AIPalabraRestringida",
                columns: table => new
                {
                    ai_palabra_restringida_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    palabra = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    fecha_creacion = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AIPalabraRestringida", x => x.ai_palabra_restringida_id);
                });

            migrationBuilder.CreateTable(
                name: "AITema",
                columns: table => new
                {
                    ai_tema_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    nombre = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    descripcion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    orden = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    fecha_creacion = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AITema", x => x.ai_tema_id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    NombreCompleto = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DocumentoIdentidad = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Telefono = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FechaRegistro = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ConfiguracionWhatsApp",
                columns: table => new
                {
                    configuracion_whatsapp_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    tooltip_texto = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    mensaje_predeterminado = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    mensaje_fuera_horario = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    hora_inicio = table.Column<TimeSpan>(type: "time", nullable: true),
                    hora_fin = table.Column<TimeSpan>(type: "time", nullable: true),
                    mostrar_en_admin = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    mostrar_en_publico = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    paginas_mostrar = table.Column<string>(type: "varchar(max)", unicode: false, nullable: true),
                    fecha_actualizacion = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfiguracionWhatsApp", x => x.configuracion_whatsapp_id);
                });

            migrationBuilder.CreateTable(
                name: "ContactoWhatsApp",
                columns: table => new
                {
                    contacto_whatsapp_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    telefono = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    es_principal = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    orden = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    fecha_creacion = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContactoWhatsApp", x => x.contacto_whatsapp_id);
                });

            migrationBuilder.CreateTable(
                name: "ContenidoSitio",
                columns: table => new
                {
                    contenido_sitio_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    clave = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    titulo = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    subtitulo = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    descripcion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ruta_imagen = table.Column<string>(type: "varchar(500)", unicode: false, maxLength: 500, nullable: true),
                    metadatos_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    fecha_actualizacion = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContenidoSitio", x => x.contenido_sitio_id);
                });

            migrationBuilder.CreateTable(
                name: "ExperienciaMultimediaItem",
                columns: table => new
                {
                    experiencia_multimedia_item_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    carrusel_numero = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    tipo = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    url = table.Column<string>(type: "varchar(500)", unicode: false, maxLength: 500, nullable: false),
                    titulo = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    orden = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    fecha_creacion = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExperienciaMultimediaItem", x => x.experiencia_multimedia_item_id);
                });

            migrationBuilder.CreateTable(
                name: "MetodosPago",
                columns: table => new
                {
                    metodo_pago_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    nombre = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MetodosPago", x => x.metodo_pago_id);
                });

            migrationBuilder.CreateTable(
                name: "Patrocinadores",
                columns: table => new
                {
                    patrocinador_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    empresa = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    contacto = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    correo = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    telefono = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    descripcion = table.Column<string>(type: "varchar(max)", unicode: false, nullable: true),
                    url_pagina = table.Column<string>(type: "varchar(500)", unicode: false, maxLength: 500, nullable: true),
                    logo_url = table.Column<string>(type: "varchar(500)", unicode: false, maxLength: 500, nullable: true),
                    fecha_registro = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Patrocinadores", x => x.patrocinador_id);
                });

            migrationBuilder.CreateTable(
                name: "Peleadores",
                columns: table => new
                {
                    peleador_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    nombre = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    apodo = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    nacionalidad = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    peso = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    fecha_nacimiento = table.Column<DateOnly>(type: "date", nullable: true),
                    peleas_ganadas = table.Column<int>(type: "int", nullable: true, defaultValue: 0),
                    peleas_perdidas = table.Column<int>(type: "int", nullable: true, defaultValue: 0),
                    descripcion = table.Column<string>(type: "varchar(max)", unicode: false, nullable: true),
                    foto_url = table.Column<string>(type: "varchar(max)", unicode: false, nullable: true),
                    estado = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true, defaultValue: "activo"),
                    fecha_registro = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Peleadores", x => x.peleador_id);
                });

            migrationBuilder.CreateTable(
                name: "SeccionesMultimedia",
                columns: table => new
                {
                    SeccionMultimediaId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Titulo = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Subtitulo = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Descripcion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VideoPrincipalUrl = table.Column<string>(type: "varchar(500)", unicode: false, maxLength: 500, nullable: true),
                    VideoSecundarioUrl = table.Column<string>(type: "varchar(500)", unicode: false, maxLength: 500, nullable: true),
                    BeneficiosJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TextoBotonPrincipal = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UrlBotonPrincipal = table.Column<string>(type: "varchar(500)", unicode: false, maxLength: 500, nullable: true),
                    TextoBotonSecundario = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UrlBotonSecundario = table.Column<string>(type: "varchar(500)", unicode: false, maxLength: 500, nullable: true),
                    Orden = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    Activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(getdate())"),
                    FechaModificacion = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeccionesMultimedia", x => x.SeccionMultimediaId);
                });

            migrationBuilder.CreateTable(
                name: "SeccionExperiencia",
                columns: table => new
                {
                    seccion_experiencia_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    titulo = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    subtitulo = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    descripcion = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    beneficios_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    texto_boton = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    url_boton = table.Column<string>(type: "varchar(500)", unicode: false, maxLength: 500, nullable: true),
                    tiempo_autoplay = table.Column<int>(type: "int", nullable: false, defaultValue: 5),
                    fecha_creacion = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())"),
                    fecha_modificacion = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeccionExperiencia", x => x.seccion_experiencia_id);
                });

            migrationBuilder.CreateTable(
                name: "TiposEntrada",
                columns: table => new
                {
                    tipo_entrada_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    nombre = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    precio = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    capacidad = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TiposEntrada", x => x.tipo_entrada_id);
                });

            migrationBuilder.CreateTable(
                name: "Venue",
                columns: table => new
                {
                    venue_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    nombre = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    svg_data = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    imagen_previa = table.Column<string>(type: "varchar(500)", unicode: false, maxLength: 500, nullable: true),
                    imagen_plano = table.Column<string>(type: "varchar(500)", unicode: false, maxLength: 500, nullable: true),
                    descripcion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    orden = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Venue", x => x.venue_id);
                });

            migrationBuilder.CreateTable(
                name: "Visitantes",
                columns: table => new
                {
                    visitante_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    nombre = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    correo = table.Column<string>(type: "varchar(150)", unicode: false, maxLength: 150, nullable: false),
                    telefono = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: true),
                    fecha_nacimiento = table.Column<DateOnly>(type: "date", nullable: true),
                    fecha_registro = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Visitantes", x => x.visitante_id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Eventos",
                columns: table => new
                {
                    evento_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    nombre = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    hora = table.Column<TimeOnly>(type: "time", nullable: false),
                    lugar = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: false),
                    descripcion = table.Column<string>(type: "varchar(max)", unicode: false, nullable: true),
                    imagen_url = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: true),
                    capacidad_maxima = table.Column<int>(type: "int", nullable: false),
                    tickets_disponibles = table.Column<int>(type: "int", nullable: false),
                    venue_id = table.Column<int>(type: "int", nullable: true),
                    estado_evento = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true, defaultValue: "Próximo"),
                    fecha_creacion = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Eventos", x => x.evento_id);
                    table.ForeignKey(
                        name: "FK_Evento_Venue",
                        column: x => x.venue_id,
                        principalTable: "Venue",
                        principalColumn: "venue_id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "VenueZone",
                columns: table => new
                {
                    venue_zone_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    venue_id = table.Column<int>(type: "int", nullable: false),
                    nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    precio = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0m),
                    capacidad = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    disponibles = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    color = table.Column<string>(type: "varchar(7)", unicode: false, maxLength: 7, nullable: false, defaultValue: "#D4AF37"),
                    orden = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    svg_path_id = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    tiene_asientos_numerados = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VenueZone", x => x.venue_zone_id);
                    table.ForeignKey(
                        name: "FK_VenueZone_Venue",
                        column: x => x.venue_id,
                        principalTable: "Venue",
                        principalColumn: "venue_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AIChatHistorial",
                columns: table => new
                {
                    ai_chat_historial_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    pregunta = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    respuesta = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    tiempo_respuesta_ms = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    estado = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false, defaultValue: "Exitosa"),
                    tema_detectado = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    session_id = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: true),
                    visitante_id = table.Column<int>(type: "int", nullable: true),
                    fecha_consulta = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AIChatHistorial", x => x.ai_chat_historial_id);
                    table.ForeignKey(
                        name: "FK_AIChatHistorial_Visitante",
                        column: x => x.visitante_id,
                        principalTable: "Visitantes",
                        principalColumn: "visitante_id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Combates",
                columns: table => new
                {
                    combate_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    evento_id = table.Column<int>(type: "int", nullable: false),
                    peleador_rojo_id = table.Column<int>(type: "int", nullable: false),
                    peleador_azul_id = table.Column<int>(type: "int", nullable: false),
                    ganador_id = table.Column<int>(type: "int", nullable: true),
                    tipo_combate = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    rounds = table.Column<int>(type: "int", nullable: true),
                    resultado = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Combates", x => x.combate_id);
                    table.ForeignKey(
                        name: "FK_Combates_Evento",
                        column: x => x.evento_id,
                        principalTable: "Eventos",
                        principalColumn: "evento_id");
                    table.ForeignKey(
                        name: "FK_Combates_Ganador",
                        column: x => x.ganador_id,
                        principalTable: "Peleadores",
                        principalColumn: "peleador_id");
                    table.ForeignKey(
                        name: "FK_Combates_PeleadorAzul",
                        column: x => x.peleador_azul_id,
                        principalTable: "Peleadores",
                        principalColumn: "peleador_id");
                    table.ForeignKey(
                        name: "FK_Combates_PeleadorRojo",
                        column: x => x.peleador_rojo_id,
                        principalTable: "Peleadores",
                        principalColumn: "peleador_id");
                });

            migrationBuilder.CreateTable(
                name: "Compras",
                columns: table => new
                {
                    compra_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    visitante_id = table.Column<int>(type: "int", nullable: false),
                    evento_id = table.Column<int>(type: "int", nullable: false),
                    metodo_pago_id = table.Column<int>(type: "int", nullable: false),
                    fecha_compra = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    monto_total = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    estado_pago = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true, defaultValue: "pendiente")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Compras", x => x.compra_id);
                    table.ForeignKey(
                        name: "FK_Compras_Evento",
                        column: x => x.evento_id,
                        principalTable: "Eventos",
                        principalColumn: "evento_id");
                    table.ForeignKey(
                        name: "FK_Compras_MetodoPago",
                        column: x => x.metodo_pago_id,
                        principalTable: "MetodosPago",
                        principalColumn: "metodo_pago_id");
                    table.ForeignKey(
                        name: "FK_Compras_Visitante",
                        column: x => x.visitante_id,
                        principalTable: "Visitantes",
                        principalColumn: "visitante_id");
                });

            migrationBuilder.CreateTable(
                name: "EventoTiposEntrada",
                columns: table => new
                {
                    evento_tipo_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    evento_id = table.Column<int>(type: "int", nullable: false),
                    tipo_entrada_id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventoTiposEntrada", x => x.evento_tipo_id);
                    table.ForeignKey(
                        name: "FK_EventoTiposEntrada_Evento",
                        column: x => x.evento_id,
                        principalTable: "Eventos",
                        principalColumn: "evento_id");
                    table.ForeignKey(
                        name: "FK_EventoTiposEntrada_TipoEntrada",
                        column: x => x.tipo_entrada_id,
                        principalTable: "TiposEntrada",
                        principalColumn: "tipo_entrada_id");
                });

            migrationBuilder.CreateTable(
                name: "Experiencias",
                columns: table => new
                {
                    ExperienciaId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventoId = table.Column<int>(type: "int", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    correo_visitante = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: true),
                    calificacion = table.Column<int>(type: "int", nullable: true),
                    Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Activa"),
                    video_principal_url = table.Column<string>(type: "varchar(500)", unicode: false, maxLength: 500, nullable: true),
                    video_secundario_url = table.Column<string>(type: "varchar(500)", unicode: false, maxLength: 500, nullable: true),
                    fecha_creacion = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Experiencias", x => x.ExperienciaId);
                    table.ForeignKey(
                        name: "FK_Experiencias_Eventos_EventoId",
                        column: x => x.EventoId,
                        principalTable: "Eventos",
                        principalColumn: "evento_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Patrocinios",
                columns: table => new
                {
                    patrocinio_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    patrocinador_id = table.Column<int>(type: "int", nullable: false),
                    url_pagina = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: true),
                    logo = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: true),
                    flyer = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: true),
                    fecha_inicio = table.Column<DateOnly>(type: "date", nullable: false),
                    fecha_fin = table.Column<DateOnly>(type: "date", nullable: false),
                    costo = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    evento_id = table.Column<int>(type: "int", nullable: false),
                    mensaje_promocional = table.Column<string>(type: "varchar(500)", unicode: false, maxLength: 500, nullable: true),
                    productos_json = table.Column<string>(type: "varchar(max)", unicode: false, nullable: true),
                    activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    orden = table.Column<int>(type: "int", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Patrocinios", x => x.patrocinio_id);
                    table.ForeignKey(
                        name: "FK_Patrocinios_Evento",
                        column: x => x.evento_id,
                        principalTable: "Eventos",
                        principalColumn: "evento_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Patrocinios_Patrocinador",
                        column: x => x.patrocinador_id,
                        principalTable: "Patrocinadores",
                        principalColumn: "patrocinador_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HistorialCombates",
                columns: table => new
                {
                    historial_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    combate_id = table.Column<int>(type: "int", nullable: false),
                    metodo_victoria = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    rounds_totales = table.Column<int>(type: "int", nullable: true),
                    observaciones = table.Column<string>(type: "varchar(max)", unicode: false, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistorialCombates", x => x.historial_id);
                    table.ForeignKey(
                        name: "FK_HistorialCombates_Combate",
                        column: x => x.combate_id,
                        principalTable: "Combates",
                        principalColumn: "combate_id");
                });

            migrationBuilder.CreateTable(
                name: "DetalleCompra",
                columns: table => new
                {
                    detalle_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    compra_id = table.Column<int>(type: "int", nullable: false),
                    tipo_entrada_id = table.Column<int>(type: "int", nullable: false),
                    cantidad = table.Column<int>(type: "int", nullable: false),
                    precio_unitario = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    subtotal = table.Column<decimal>(type: "decimal(10,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DetalleCompra", x => x.detalle_id);
                    table.ForeignKey(
                        name: "FK_DetalleCompra_Compra",
                        column: x => x.compra_id,
                        principalTable: "Compras",
                        principalColumn: "compra_id");
                    table.ForeignKey(
                        name: "FK_DetalleCompra_TipoEntrada",
                        column: x => x.tipo_entrada_id,
                        principalTable: "TiposEntrada",
                        principalColumn: "tipo_entrada_id");
                });

            migrationBuilder.CreateTable(
                name: "Tickets",
                columns: table => new
                {
                    ticket_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    compra_id = table.Column<int>(type: "int", nullable: false),
                    evento_id = table.Column<int>(type: "int", nullable: false),
                    tipo_entrada_id = table.Column<int>(type: "int", nullable: false),
                    codigo_unico = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    codigo_qr = table.Column<string>(type: "varchar(500)", unicode: false, maxLength: 500, nullable: false),
                    token_hash = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: false),
                    tipo_ticket = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    zona = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    precio = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    estado = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true, defaultValue: "Activo"),
                    fecha_generacion = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    pdf_path = table.Column<string>(type: "varchar(500)", unicode: false, maxLength: 500, nullable: true),
                    qr_path = table.Column<string>(type: "varchar(500)", unicode: false, maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tickets", x => x.ticket_id);
                    table.ForeignKey(
                        name: "FK_Tickets_Compra",
                        column: x => x.compra_id,
                        principalTable: "Compras",
                        principalColumn: "compra_id");
                    table.ForeignKey(
                        name: "FK_Tickets_Evento",
                        column: x => x.evento_id,
                        principalTable: "Eventos",
                        principalColumn: "evento_id");
                    table.ForeignKey(
                        name: "FK_Tickets_TipoEntrada",
                        column: x => x.tipo_entrada_id,
                        principalTable: "TiposEntrada",
                        principalColumn: "tipo_entrada_id");
                });

            migrationBuilder.CreateTable(
                name: "Asistente",
                columns: table => new
                {
                    asistente_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ticket_id = table.Column<int>(type: "int", nullable: false),
                    nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    apellido = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    tipo_documento = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    numero_documento = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    correo = table.Column<string>(type: "varchar(150)", unicode: false, maxLength: 150, nullable: false),
                    celular = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: true),
                    fecha_nacimiento = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Asistente", x => x.asistente_id);
                    table.ForeignKey(
                        name: "FK_Ticket_Asistente",
                        column: x => x.ticket_id,
                        principalTable: "Tickets",
                        principalColumn: "ticket_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Descuentos",
                columns: table => new
                {
                    descuento_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ticket_id = table.Column<int>(type: "int", nullable: false),
                    cantidad_minima = table.Column<int>(type: "int", nullable: false),
                    porcentaje_descuento = table.Column<decimal>(type: "decimal(5,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Descuentos", x => x.descuento_id);
                    table.ForeignKey(
                        name: "FK_Descuentos_Ticket",
                        column: x => x.ticket_id,
                        principalTable: "Tickets",
                        principalColumn: "ticket_id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AIChatHistorial_visitante_id",
                table: "AIChatHistorial",
                column: "visitante_id");

            migrationBuilder.CreateIndex(
                name: "UQ_AIContextoConfig_Clave",
                table: "AIContextoConfig",
                column: "clave",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_Asistente_Ticket",
                table: "Asistente",
                column: "ticket_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Combates_evento_id",
                table: "Combates",
                column: "evento_id");

            migrationBuilder.CreateIndex(
                name: "IX_Combates_ganador_id",
                table: "Combates",
                column: "ganador_id");

            migrationBuilder.CreateIndex(
                name: "IX_Combates_peleador_azul_id",
                table: "Combates",
                column: "peleador_azul_id");

            migrationBuilder.CreateIndex(
                name: "IX_Combates_peleador_rojo_id",
                table: "Combates",
                column: "peleador_rojo_id");

            migrationBuilder.CreateIndex(
                name: "IX_Compras_evento_id",
                table: "Compras",
                column: "evento_id");

            migrationBuilder.CreateIndex(
                name: "IX_Compras_metodo_pago_id",
                table: "Compras",
                column: "metodo_pago_id");

            migrationBuilder.CreateIndex(
                name: "IX_Compras_visitante_id",
                table: "Compras",
                column: "visitante_id");

            migrationBuilder.CreateIndex(
                name: "UQ_ContenidoSitio_Clave",
                table: "ContenidoSitio",
                column: "clave",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Descuentos_ticket_id",
                table: "Descuentos",
                column: "ticket_id");

            migrationBuilder.CreateIndex(
                name: "IX_DetalleCompra_compra_id",
                table: "DetalleCompra",
                column: "compra_id");

            migrationBuilder.CreateIndex(
                name: "IX_DetalleCompra_tipo_entrada_id",
                table: "DetalleCompra",
                column: "tipo_entrada_id");

            migrationBuilder.CreateIndex(
                name: "IX_Eventos_venue_id",
                table: "Eventos",
                column: "venue_id");

            migrationBuilder.CreateIndex(
                name: "IX_EventoTiposEntrada_tipo_entrada_id",
                table: "EventoTiposEntrada",
                column: "tipo_entrada_id");

            migrationBuilder.CreateIndex(
                name: "UQ_EventoTiposEntrada",
                table: "EventoTiposEntrada",
                columns: new[] { "evento_id", "tipo_entrada_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Experiencias_EventoId",
                table: "Experiencias",
                column: "EventoId");

            migrationBuilder.CreateIndex(
                name: "IX_HistorialCombates_combate_id",
                table: "HistorialCombates",
                column: "combate_id");

            migrationBuilder.CreateIndex(
                name: "IX_Patrocinios_evento_id",
                table: "Patrocinios",
                column: "evento_id");

            migrationBuilder.CreateIndex(
                name: "IX_Patrocinios_patrocinador_id",
                table: "Patrocinios",
                column: "patrocinador_id");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_codigo_unico",
                table: "Tickets",
                column: "codigo_unico",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_compra_id",
                table: "Tickets",
                column: "compra_id");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_evento_id",
                table: "Tickets",
                column: "evento_id");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_tipo_entrada_id",
                table: "Tickets",
                column: "tipo_entrada_id");

            migrationBuilder.CreateIndex(
                name: "UQ_Tickets_CodigoQR",
                table: "Tickets",
                column: "codigo_qr",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_TiposEntrada_Nombre",
                table: "TiposEntrada",
                column: "nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VenueZone_venue_id",
                table: "VenueZone",
                column: "venue_id");

            migrationBuilder.CreateIndex(
                name: "UQ_Visitantes_Correo",
                table: "Visitantes",
                column: "correo",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AIChatHistorial");

            migrationBuilder.DropTable(
                name: "AIConfiguracion");

            migrationBuilder.DropTable(
                name: "AIContextoConfig");

            migrationBuilder.DropTable(
                name: "AIPalabraRestringida");

            migrationBuilder.DropTable(
                name: "AITema");

            migrationBuilder.DropTable(
                name: "Asistente");

            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "ConfiguracionWhatsApp");

            migrationBuilder.DropTable(
                name: "ContactoWhatsApp");

            migrationBuilder.DropTable(
                name: "ContenidoSitio");

            migrationBuilder.DropTable(
                name: "Descuentos");

            migrationBuilder.DropTable(
                name: "DetalleCompra");

            migrationBuilder.DropTable(
                name: "EventoTiposEntrada");

            migrationBuilder.DropTable(
                name: "ExperienciaMultimediaItem");

            migrationBuilder.DropTable(
                name: "Experiencias");

            migrationBuilder.DropTable(
                name: "HistorialCombates");

            migrationBuilder.DropTable(
                name: "Patrocinios");

            migrationBuilder.DropTable(
                name: "SeccionesMultimedia");

            migrationBuilder.DropTable(
                name: "SeccionExperiencia");

            migrationBuilder.DropTable(
                name: "VenueZone");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "Tickets");

            migrationBuilder.DropTable(
                name: "Combates");

            migrationBuilder.DropTable(
                name: "Patrocinadores");

            migrationBuilder.DropTable(
                name: "Compras");

            migrationBuilder.DropTable(
                name: "TiposEntrada");

            migrationBuilder.DropTable(
                name: "Peleadores");

            migrationBuilder.DropTable(
                name: "Eventos");

            migrationBuilder.DropTable(
                name: "MetodosPago");

            migrationBuilder.DropTable(
                name: "Visitantes");

            migrationBuilder.DropTable(
                name: "Venue");

            migrationBuilder.DropSequence(
                name: "seq_ticket_codigo");
        }
    }
}
