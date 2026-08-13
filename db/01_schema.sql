/* ============================================================
   GasMapocho — 01_schema.sql
   Base de datos, tablas, restricciones e indices.

   Idempotente: se puede ejecutar dos veces sin fallar.
   Ejecutar con:
       sqlcmd -S .\SQLEXPRESS -E -i db\01_schema.sql
   ============================================================ */

IF DB_ID('GasMapocho') IS NULL
BEGIN
    CREATE DATABASE GasMapocho COLLATE Modern_Spanish_CI_AS;
END
GO

USE GasMapocho;
GO

/* ------------------------------------------------------------
   Se eliminan en orden inverso a las dependencias.
   ------------------------------------------------------------ */
IF OBJECT_ID('dbo.Venta', 'U')         IS NOT NULL DROP TABLE dbo.Venta;
IF OBJECT_ID('dbo.DetallePedido', 'U') IS NOT NULL DROP TABLE dbo.DetallePedido;
IF OBJECT_ID('dbo.Pedido', 'U')        IS NOT NULL DROP TABLE dbo.Pedido;
IF OBJECT_ID('dbo.Producto', 'U')      IS NOT NULL DROP TABLE dbo.Producto;
IF OBJECT_ID('dbo.Cliente', 'U')       IS NOT NULL DROP TABLE dbo.Cliente;
IF OBJECT_ID('dbo.Usuario', 'U')       IS NOT NULL DROP TABLE dbo.Usuario;
IF OBJECT_ID('dbo.Rol', 'U')           IS NOT NULL DROP TABLE dbo.Rol;
GO

CREATE TABLE dbo.Rol (
    IdRol  INT IDENTITY(1,1) PRIMARY KEY,
    Nombre NVARCHAR(20) NOT NULL,
    CONSTRAINT UQ_Rol_Nombre UNIQUE (Nombre)
);
GO

CREATE TABLE dbo.Usuario (
    IdUsuario     INT IDENTITY(1,1) PRIMARY KEY,
    Email         NVARCHAR(120) NOT NULL,
    Nombre        NVARCHAR(120) NOT NULL,
    -- Hash y salt, nunca la contrasena en texto plano.
    PasswordHash  VARBINARY(64) NOT NULL,
    PasswordSalt  VARBINARY(32) NOT NULL,
    IdRol         INT           NOT NULL,
    Activo        BIT           NOT NULL DEFAULT 1,
    FechaRegistro DATETIME2(0)  NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT UQ_Usuario_Email UNIQUE (Email),
    CONSTRAINT FK_Usuario_Rol FOREIGN KEY (IdRol) REFERENCES dbo.Rol(IdRol)
);
GO

CREATE TABLE dbo.Cliente (
    IdCliente INT IDENTITY(1,1) PRIMARY KEY,
    -- Nullable: el administrador puede registrar un cliente que todavia no
    -- tiene cuenta de acceso al sistema.
    IdUsuario INT           NULL,
    Nombre    NVARCHAR(120) NOT NULL,
    Documento NVARCHAR(20)  NOT NULL,
    Telefono  NVARCHAR(20)  NOT NULL,
    Correo    NVARCHAR(120) NOT NULL,
    Comuna    NVARCHAR(80)  NOT NULL,
    Direccion NVARCHAR(200) NOT NULL,
    Activo    BIT           NOT NULL DEFAULT 1,
    CONSTRAINT UQ_Cliente_Documento UNIQUE (Documento),
    CONSTRAINT FK_Cliente_Usuario FOREIGN KEY (IdUsuario) REFERENCES dbo.Usuario(IdUsuario)
);
GO

CREATE TABLE dbo.Producto (
    IdProducto  INT IDENTITY(1,1) PRIMARY KEY,
    Codigo      NVARCHAR(20)  NOT NULL,
    Nombre      NVARCHAR(120) NOT NULL,
    Descripcion NVARCHAR(300) NULL,
    -- Ruta relativa dentro de wwwroot/img de GasMapocho.Ui, p.ej. 'productos/balon-5kg.png'.
    -- NULL para los productos sin foto real: la vista cae al placeholder.
    ImagenUrl   NVARCHAR(200) NULL,
    CapacidadKg INT           NOT NULL,
    -- DECIMAL(12,0): el peso chileno no tiene decimales. Nunca FLOAT ni MONEY.
    Precio      DECIMAL(12,0) NOT NULL,
    Stock       INT           NOT NULL DEFAULT 0,
    Activo      BIT           NOT NULL DEFAULT 1,
    CONSTRAINT UQ_Producto_Codigo UNIQUE (Codigo),
    CONSTRAINT CK_Producto_Precio CHECK (Precio > 0),
    -- Ultima linea de defensa contra el descuadre de inventario.
    CONSTRAINT CK_Producto_Stock  CHECK (Stock >= 0)
);
GO

CREATE TABLE dbo.Pedido (
    IdPedido          INT IDENTITY(1,1) PRIMARY KEY,
    IdCliente         INT           NOT NULL,
    Fecha             DATETIME2(0)  NOT NULL DEFAULT SYSDATETIME(),
    Estado            VARCHAR(10)   NOT NULL DEFAULT 'Pendiente',
    Subtotal          DECIMAL(12,0) NOT NULL,
    Despacho          DECIMAL(12,0) NOT NULL DEFAULT 0,
    Total             DECIMAL(12,0) NOT NULL,
    MetodoPago        NVARCHAR(30)  NOT NULL,
    NombreEntrega     NVARCHAR(120) NOT NULL,
    TelefonoEntrega   NVARCHAR(20)  NOT NULL,
    ComunaEntrega     NVARCHAR(80)  NOT NULL,
    DireccionEntrega  NVARCHAR(200) NOT NULL,
    ReferenciaEntrega NVARCHAR(200) NULL,
    CONSTRAINT FK_Pedido_Cliente FOREIGN KEY (IdCliente) REFERENCES dbo.Cliente(IdCliente),
    CONSTRAINT CK_Pedido_Estado CHECK (Estado IN ('Pendiente','Aprobado','Rechazado')),
    CONSTRAINT CK_Pedido_Total  CHECK (Total = Subtotal + Despacho)
);
GO

CREATE TABLE dbo.DetallePedido (
    IdDetalle      INT IDENTITY(1,1) PRIMARY KEY,
    IdPedido       INT           NOT NULL,
    IdProducto     INT           NOT NULL,
    Cantidad       INT           NOT NULL,
    -- Congela el precio del momento de la compra: subir el precio del balon
    -- no puede reescribir el historial del cliente.
    PrecioUnitario DECIMAL(12,0) NOT NULL,
    Subtotal       DECIMAL(12,0) NOT NULL,
    CONSTRAINT FK_Detalle_Pedido   FOREIGN KEY (IdPedido)   REFERENCES dbo.Pedido(IdPedido) ON DELETE CASCADE,
    CONSTRAINT FK_Detalle_Producto FOREIGN KEY (IdProducto) REFERENCES dbo.Producto(IdProducto),
    CONSTRAINT CK_Detalle_Cantidad CHECK (Cantidad > 0),
    CONSTRAINT CK_Detalle_Subtotal CHECK (Subtotal = Cantidad * PrecioUnitario)
);
GO

CREATE TABLE dbo.Venta (
    IdVenta    INT IDENTITY(1,1) PRIMARY KEY,
    IdPedido   INT           NOT NULL,
    FechaVenta DATETIME2(0)  NOT NULL DEFAULT SYSDATETIME(),
    Monto      DECIMAL(12,0) NOT NULL,
    -- Impide registrar dos veces la venta del mismo pedido.
    CONSTRAINT UQ_Venta_Pedido UNIQUE (IdPedido),
    CONSTRAINT FK_Venta_Pedido FOREIGN KEY (IdPedido) REFERENCES dbo.Pedido(IdPedido)
);
GO

/* ------------------------------------------------------------
   Indices — cada uno responde a una consulta real del sistema.

   IX_Producto_Nombre es un indice filtrado (WHERE Activo = 1) y
   SQL Server exige QUOTED_IDENTIFIER ON para crearlo. sqlcmd no lo
   activa por omision, asi que se fija aqui y el script deja de
   depender del cliente que lo ejecute.
   ------------------------------------------------------------ */
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

CREATE INDEX IX_Pedido_Estado_Fecha  ON dbo.Pedido(Estado, Fecha DESC);
CREATE INDEX IX_Pedido_Cliente_Fecha ON dbo.Pedido(IdCliente, Fecha DESC);
CREATE INDEX IX_Detalle_Pedido       ON dbo.DetallePedido(IdPedido);
CREATE INDEX IX_Producto_Nombre      ON dbo.Producto(Nombre) WHERE Activo = 1;
CREATE INDEX IX_Venta_Fecha          ON dbo.Venta(FechaVenta DESC);
GO

PRINT 'Esquema creado correctamente.';
GO
