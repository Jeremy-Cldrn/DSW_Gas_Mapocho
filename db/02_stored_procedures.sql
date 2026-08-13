/* ============================================================
   GasMapocho — 02_stored_procedures.sql

   Todo parametro que viene del usuario entra como parametro del
   procedimiento. En ninguna parte se concatena texto dentro del SQL:
   esa es la diferencia entre un buscador y una inyeccion SQL.

   Los listados devuelven DOS result sets: primero el total de filas,
   despues la pagina. El repositorio los lee con reader.NextResult(),
   evitando una segunda ida a la base solo para contar.

   Ejecutar con:
       sqlcmd -S .\SQLEXPRESS -E -i db\02_stored_procedures.sql
   ============================================================ */

USE GasMapocho;
GO

-- Los procedimientos guardan estas opciones tal como estaban al crearse,
-- asi que se fijan explicitamente y no dependen del cliente que ejecute
-- el script.
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

/* ============================================================
   USUARIOS
   ============================================================ */

IF OBJECT_ID('dbo.sp_Usuario_Autenticar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Usuario_Autenticar;
GO
-- Devuelve hash y salt para que la aplicacion compare. La contrasena
-- en texto plano no viaja a la base de datos ni se guarda en ella.
CREATE PROCEDURE dbo.sp_Usuario_Autenticar
    @Email NVARCHAR(120)
AS
BEGIN
    SET NOCOUNT ON;

    -- IdCliente sale por LEFT JOIN: un administrador no tiene ficha de cliente,
    -- y la tienda lo necesita para filtrar el historial de compras.
    SELECT u.IdUsuario, u.Email, u.Nombre, u.PasswordHash, u.PasswordSalt,
           u.Activo, r.Nombre AS Rol, c.IdCliente
    FROM dbo.Usuario u
    INNER JOIN dbo.Rol r ON r.IdRol = u.IdRol
    LEFT  JOIN dbo.Cliente c ON c.IdUsuario = u.IdUsuario AND c.Activo = 1
    WHERE u.Email = @Email;
END
GO

IF OBJECT_ID('dbo.sp_Usuario_Listar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Usuario_Listar;
GO
CREATE PROCEDURE dbo.sp_Usuario_Listar
    @Busqueda          NVARCHAR(120) = NULL,
    @Pagina            INT = 1,
    @CantidadPorPagina INT = 10
AS
BEGIN
    SET NOCOUNT ON;

    IF @Pagina < 1 SET @Pagina = 1;
    IF @CantidadPorPagina < 1 SET @CantidadPorPagina = 10;

    SELECT COUNT(*) AS Total
    FROM dbo.Usuario u
    WHERE (@Busqueda IS NULL OR u.Nombre LIKE '%' + @Busqueda + '%'
                             OR u.Email  LIKE '%' + @Busqueda + '%');

    -- Nunca se devuelve PasswordHash ni PasswordSalt en un listado.
    SELECT u.IdUsuario, u.Email, u.Nombre, r.Nombre AS Rol, u.Activo
    FROM dbo.Usuario u
    INNER JOIN dbo.Rol r ON r.IdRol = u.IdRol
    WHERE (@Busqueda IS NULL OR u.Nombre LIKE '%' + @Busqueda + '%'
                             OR u.Email  LIKE '%' + @Busqueda + '%')
    ORDER BY u.Nombre
    OFFSET (@Pagina - 1) * @CantidadPorPagina ROWS
    FETCH NEXT @CantidadPorPagina ROWS ONLY;
END
GO

IF OBJECT_ID('dbo.sp_Usuario_ActualizarPassword', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Usuario_ActualizarPassword;
GO
-- Usada por la recuperacion de contrasena del panel administrativo. Solo
-- deja cambiar la clave de una cuenta con rol Administrador que este activa;
-- el mensaje de error es el mismo para "no existe", "no es admin" y "esta
-- desactivada", por la misma razon anti-enumeracion que sp_Usuario_Autenticar.
CREATE PROCEDURE dbo.sp_Usuario_ActualizarPassword
    @Email         NVARCHAR(120),
    @PasswordHash  VARBINARY(64),
    @PasswordSalt  VARBINARY(32),
    @Mensaje       VARCHAR(200) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @IdUsuario INT;

    SELECT @IdUsuario = u.IdUsuario
    FROM dbo.Usuario u
    INNER JOIN dbo.Rol r ON r.IdRol = u.IdRol
    WHERE u.Email = @Email AND u.Activo = 1 AND r.Nombre = 'Administrador';

    IF @IdUsuario IS NULL
    BEGIN
        SET @Mensaje = 'No fue posible actualizar la contraseña con los datos ingresados.';
        RETURN;
    END

    UPDATE dbo.Usuario
       SET PasswordHash = @PasswordHash,
           PasswordSalt = @PasswordSalt
     WHERE IdUsuario = @IdUsuario;

    SET @Mensaje = 'La contraseña se actualizó correctamente.';
END
GO

/* ============================================================
   PRODUCTOS
   ============================================================ */

IF OBJECT_ID('dbo.sp_Producto_Listar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Producto_Listar;
GO
CREATE PROCEDURE dbo.sp_Producto_Listar
    @Busqueda          NVARCHAR(120) = NULL,
    @Pagina            INT = 1,
    @CantidadPorPagina INT = 10,
    @IncluirInactivos  BIT = 0
AS
BEGIN
    SET NOCOUNT ON;

    IF @Pagina < 1 SET @Pagina = 1;
    IF @CantidadPorPagina < 1 SET @CantidadPorPagina = 10;

    SELECT COUNT(*) AS Total
    FROM dbo.Producto
    WHERE (@IncluirInactivos = 1 OR Activo = 1)
      AND (@Busqueda IS NULL OR Nombre LIKE '%' + @Busqueda + '%' OR Codigo LIKE '%' + @Busqueda + '%');

    SELECT IdProducto, Codigo, Nombre, Descripcion, ImagenUrl, CapacidadKg, Precio, Stock, Activo
    FROM dbo.Producto
    WHERE (@IncluirInactivos = 1 OR Activo = 1)
      AND (@Busqueda IS NULL OR Nombre LIKE '%' + @Busqueda + '%' OR Codigo LIKE '%' + @Busqueda + '%')
    ORDER BY Nombre
    OFFSET (@Pagina - 1) * @CantidadPorPagina ROWS
    FETCH NEXT @CantidadPorPagina ROWS ONLY;
END
GO

IF OBJECT_ID('dbo.sp_Producto_Obtener', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Producto_Obtener;
GO
CREATE PROCEDURE dbo.sp_Producto_Obtener
    @IdProducto INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT IdProducto, Codigo, Nombre, Descripcion, ImagenUrl, CapacidadKg, Precio, Stock, Activo
    FROM dbo.Producto
    WHERE IdProducto = @IdProducto;
END
GO

IF OBJECT_ID('dbo.sp_Producto_Registrar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Producto_Registrar;
GO
CREATE PROCEDURE dbo.sp_Producto_Registrar
    @Codigo      NVARCHAR(20),
    @Nombre      NVARCHAR(120),
    @Descripcion NVARCHAR(300) = NULL,
    @ImagenUrl   NVARCHAR(200) = NULL,
    @CapacidadKg INT,
    @Precio      DECIMAL(12,0),
    @Stock       INT,
    @IdProducto  INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM dbo.Producto WHERE Codigo = @Codigo)
        THROW 50040, 'Ya existe un producto con ese codigo.', 1;

    INSERT INTO dbo.Producto (Codigo, Nombre, Descripcion, ImagenUrl, CapacidadKg, Precio, Stock, Activo)
    VALUES (@Codigo, @Nombre, @Descripcion, @ImagenUrl, @CapacidadKg, @Precio, @Stock, 1);

    SET @IdProducto = SCOPE_IDENTITY();
END
GO

IF OBJECT_ID('dbo.sp_Producto_Actualizar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Producto_Actualizar;
GO
CREATE PROCEDURE dbo.sp_Producto_Actualizar
    @IdProducto  INT,
    @Codigo      NVARCHAR(20),
    @Nombre      NVARCHAR(120),
    @Descripcion NVARCHAR(300) = NULL,
    @ImagenUrl   NVARCHAR(200) = NULL,
    @CapacidadKg INT,
    @Precio      DECIMAL(12,0),
    @Stock       INT,
    @Activo      BIT
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM dbo.Producto WHERE Codigo = @Codigo AND IdProducto <> @IdProducto)
        THROW 50041, 'Ya existe otro producto con ese codigo.', 1;

    UPDATE dbo.Producto
    SET Codigo = @Codigo, Nombre = @Nombre, Descripcion = @Descripcion, ImagenUrl = @ImagenUrl,
        CapacidadKg = @CapacidadKg, Precio = @Precio, Stock = @Stock, Activo = @Activo
    WHERE IdProducto = @IdProducto;

    IF @@ROWCOUNT = 0
        THROW 50042, 'El producto no existe.', 1;
END
GO

IF OBJECT_ID('dbo.sp_Producto_Eliminar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Producto_Eliminar;
GO
-- Baja logica, no DELETE: un producto con historial de ventas no puede
-- desaparecer sin romper los reportes.
CREATE PROCEDURE dbo.sp_Producto_Eliminar
    @IdProducto INT
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM dbo.DetallePedido WHERE IdProducto = @IdProducto)
        THROW 50030, 'No se puede eliminar: el producto tiene pedidos asociados.', 1;

    UPDATE dbo.Producto SET Activo = 0 WHERE IdProducto = @IdProducto;

    IF @@ROWCOUNT = 0
        THROW 50043, 'El producto no existe.', 1;
END
GO

/* ============================================================
   CLIENTES
   ============================================================ */

IF OBJECT_ID('dbo.sp_Cliente_Listar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Cliente_Listar;
GO
CREATE PROCEDURE dbo.sp_Cliente_Listar
    @Busqueda          NVARCHAR(120) = NULL,
    @Pagina            INT = 1,
    @CantidadPorPagina INT = 10
AS
BEGIN
    SET NOCOUNT ON;

    IF @Pagina < 1 SET @Pagina = 1;
    IF @CantidadPorPagina < 1 SET @CantidadPorPagina = 10;

    SELECT COUNT(*) AS Total
    FROM dbo.Cliente
    WHERE Activo = 1
      AND (@Busqueda IS NULL OR Nombre LIKE '%' + @Busqueda + '%'
                             OR Correo LIKE '%' + @Busqueda + '%'
                             OR Documento LIKE '%' + @Busqueda + '%');

    SELECT IdCliente, IdUsuario, Nombre, Documento, Telefono, Correo, Comuna, Direccion, Activo
    FROM dbo.Cliente
    WHERE Activo = 1
      AND (@Busqueda IS NULL OR Nombre LIKE '%' + @Busqueda + '%'
                             OR Correo LIKE '%' + @Busqueda + '%'
                             OR Documento LIKE '%' + @Busqueda + '%')
    ORDER BY Nombre
    OFFSET (@Pagina - 1) * @CantidadPorPagina ROWS
    FETCH NEXT @CantidadPorPagina ROWS ONLY;
END
GO

IF OBJECT_ID('dbo.sp_Cliente_Obtener', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Cliente_Obtener;
GO
CREATE PROCEDURE dbo.sp_Cliente_Obtener
    @IdCliente INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT IdCliente, IdUsuario, Nombre, Documento, Telefono, Correo, Comuna, Direccion, Activo
    FROM dbo.Cliente
    WHERE IdCliente = @IdCliente;
END
GO

IF OBJECT_ID('dbo.sp_Cliente_Registrar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Cliente_Registrar;
GO
CREATE PROCEDURE dbo.sp_Cliente_Registrar
    @Nombre    NVARCHAR(120),
    @Documento NVARCHAR(20),
    @Telefono  NVARCHAR(20),
    @Correo    NVARCHAR(120),
    @Comuna    NVARCHAR(80),
    @Direccion NVARCHAR(200),
    @IdCliente INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM dbo.Cliente WHERE Documento = @Documento)
        THROW 50050, 'Ya existe un cliente con ese documento.', 1;

    INSERT INTO dbo.Cliente (Nombre, Documento, Telefono, Correo, Comuna, Direccion, Activo)
    VALUES (@Nombre, @Documento, @Telefono, @Correo, @Comuna, @Direccion, 1);

    SET @IdCliente = SCOPE_IDENTITY();
END
GO

IF OBJECT_ID('dbo.sp_Cliente_Actualizar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Cliente_Actualizar;
GO
CREATE PROCEDURE dbo.sp_Cliente_Actualizar
    @IdCliente INT,
    @Nombre    NVARCHAR(120),
    @Documento NVARCHAR(20),
    @Telefono  NVARCHAR(20),
    @Correo    NVARCHAR(120),
    @Comuna    NVARCHAR(80),
    @Direccion NVARCHAR(200)
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM dbo.Cliente WHERE Documento = @Documento AND IdCliente <> @IdCliente)
        THROW 50051, 'Ya existe otro cliente con ese documento.', 1;

    UPDATE dbo.Cliente
    SET Nombre = @Nombre, Documento = @Documento, Telefono = @Telefono,
        Correo = @Correo, Comuna = @Comuna, Direccion = @Direccion
    WHERE IdCliente = @IdCliente;

    IF @@ROWCOUNT = 0
        THROW 50052, 'El cliente no existe.', 1;
END
GO

IF OBJECT_ID('dbo.sp_Cliente_Eliminar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Cliente_Eliminar;
GO
CREATE PROCEDURE dbo.sp_Cliente_Eliminar
    @IdCliente INT
AS
BEGIN
    SET NOCOUNT ON;

    -- Baja logica: sus pedidos deben seguir siendo consultables.
    UPDATE dbo.Cliente SET Activo = 0 WHERE IdCliente = @IdCliente;

    IF @@ROWCOUNT = 0
        THROW 50053, 'El cliente no existe.', 1;
END
GO

/* ============================================================
   PEDIDOS

   El registro se parte en dos procedimientos a proposito: la
   aplicacion abre un SqlTransaction, llama a la cabecera y luego
   al detalle una vez por linea con un foreach, y hace Commit al
   final. Si algo falla en medio, Rollback deja la base como estaba
   y NUNCA queda un pedido sin lineas.
   ============================================================ */

IF OBJECT_ID('dbo.sp_Pedido_RegistrarCabecera', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Pedido_RegistrarCabecera;
GO
CREATE PROCEDURE dbo.sp_Pedido_RegistrarCabecera
    @IdCliente         INT,
    @Subtotal          DECIMAL(12,0),
    @Despacho          DECIMAL(12,0),
    @MetodoPago        NVARCHAR(30),
    @NombreEntrega     NVARCHAR(120),
    @TelefonoEntrega   NVARCHAR(20),
    @ComunaEntrega     NVARCHAR(80),
    @DireccionEntrega  NVARCHAR(200),
    @ReferenciaEntrega NVARCHAR(200) = NULL,
    @IdPedido          INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.Pedido (IdCliente, Estado, Subtotal, Despacho, Total, MetodoPago,
                            NombreEntrega, TelefonoEntrega, ComunaEntrega,
                            DireccionEntrega, ReferenciaEntrega)
    VALUES (@IdCliente, 'Pendiente', @Subtotal, @Despacho, @Subtotal + @Despacho, @MetodoPago,
            @NombreEntrega, @TelefonoEntrega, @ComunaEntrega,
            @DireccionEntrega, @ReferenciaEntrega);

    -- SCOPE_IDENTITY y no @@IDENTITY: el segundo devuelve el ultimo id de
    -- cualquier ambito, incluido el de un trigger.
    SET @IdPedido = SCOPE_IDENTITY();
END
GO

IF OBJECT_ID('dbo.sp_Pedido_RegistrarDetalle', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Pedido_RegistrarDetalle;
GO
CREATE PROCEDURE dbo.sp_Pedido_RegistrarDetalle
    @IdPedido       INT,
    @IdProducto     INT,
    @Cantidad       INT,
    @PrecioUnitario DECIMAL(12,0)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Stock INT, @Activo BIT;
    SELECT @Stock = Stock, @Activo = Activo FROM dbo.Producto WHERE IdProducto = @IdProducto;

    IF @Stock IS NULL
        THROW 50012, 'El producto indicado no existe.', 1;

    IF @Activo = 0
        THROW 50013, 'El producto no esta disponible.', 1;

    IF @Cantidad > @Stock
        THROW 50011, 'Stock insuficiente para uno o mas productos.', 1;

    INSERT INTO dbo.DetallePedido (IdPedido, IdProducto, Cantidad, PrecioUnitario, Subtotal)
    VALUES (@IdPedido, @IdProducto, @Cantidad, @PrecioUnitario, @Cantidad * @PrecioUnitario);

    -- El pedido queda confirmado en el momento de la compra (ya no hay
    -- aprobacion manual del administrador): el stock se descuenta aqui
    -- mismo, dentro de la misma transaccion que registra el pedido.
    UPDATE dbo.Producto SET Stock = Stock - @Cantidad WHERE IdProducto = @IdProducto;
END
GO

IF OBJECT_ID('dbo.sp_Pedido_ConfirmarVenta', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Pedido_ConfirmarVenta;
GO
-- Cierra el pedido recien creado como venta confirmada. A proposito NO
-- maneja su propia transaccion (a diferencia del viejo sp_Pedido_Aprobar):
-- se llama dentro de la misma transaccion que ya abrio
-- PedidoRepositorio.Registrar, justo despues de insertar cabecera y detalle.
CREATE PROCEDURE dbo.sp_Pedido_ConfirmarVenta
    @IdPedido INT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Pedido SET Estado = 'Aprobado' WHERE IdPedido = @IdPedido;

    INSERT INTO dbo.Venta (IdPedido, Monto)
    SELECT IdPedido, Total FROM dbo.Pedido WHERE IdPedido = @IdPedido;
END
GO

IF OBJECT_ID('dbo.sp_Pedido_Listar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Pedido_Listar;
GO
CREATE PROCEDURE dbo.sp_Pedido_Listar
    @IdCliente         INT = NULL,
    @Estado            VARCHAR(10) = NULL,
    @FechaInicial      DATE = NULL,
    @FechaFinal        DATE = NULL,
    @Busqueda          NVARCHAR(120) = NULL,
    @Pagina            INT = 1,
    @CantidadPorPagina INT = 10
AS
BEGIN
    SET NOCOUNT ON;

    IF @Pagina < 1 SET @Pagina = 1;
    IF @CantidadPorPagina < 1 SET @CantidadPorPagina = 10;

    SELECT COUNT(*) AS Total
    FROM dbo.Pedido p
    INNER JOIN dbo.Cliente c ON c.IdCliente = p.IdCliente
    WHERE (@IdCliente    IS NULL OR p.IdCliente = @IdCliente)
      AND (@Estado       IS NULL OR p.Estado = @Estado)
      AND (@FechaInicial IS NULL OR CAST(p.Fecha AS DATE) >= @FechaInicial)
      AND (@FechaFinal   IS NULL OR CAST(p.Fecha AS DATE) <= @FechaFinal)
      AND (@Busqueda     IS NULL OR c.Nombre LIKE '%' + @Busqueda + '%'
                                  OR CAST(p.IdPedido AS VARCHAR(10)) LIKE '%' + @Busqueda + '%');

    -- TotalItems se calcula aqui y no sumando el detalle en la aplicacion:
    -- el listado no trae las lineas, y pedirlas por cada fila seria una
    -- consulta por pedido.
    SELECT p.IdPedido, p.IdCliente, c.Nombre AS Cliente, c.Comuna, p.Fecha, p.Estado,
           p.Subtotal, p.Despacho, p.Total, p.MetodoPago,
           p.NombreEntrega, p.TelefonoEntrega, p.ComunaEntrega,
           p.DireccionEntrega, p.ReferenciaEntrega,
           ISNULL((SELECT SUM(d.Cantidad) FROM dbo.DetallePedido d WHERE d.IdPedido = p.IdPedido), 0) AS TotalItems
    FROM dbo.Pedido p
    INNER JOIN dbo.Cliente c ON c.IdCliente = p.IdCliente
    WHERE (@IdCliente    IS NULL OR p.IdCliente = @IdCliente)
      AND (@Estado       IS NULL OR p.Estado = @Estado)
      AND (@FechaInicial IS NULL OR CAST(p.Fecha AS DATE) >= @FechaInicial)
      AND (@FechaFinal   IS NULL OR CAST(p.Fecha AS DATE) <= @FechaFinal)
      AND (@Busqueda     IS NULL OR c.Nombre LIKE '%' + @Busqueda + '%'
                                  OR CAST(p.IdPedido AS VARCHAR(10)) LIKE '%' + @Busqueda + '%')
    ORDER BY p.Fecha DESC, p.IdPedido DESC
    OFFSET (@Pagina - 1) * @CantidadPorPagina ROWS
    FETCH NEXT @CantidadPorPagina ROWS ONLY;
END
GO

IF OBJECT_ID('dbo.sp_Pedido_Obtener', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Pedido_Obtener;
GO
CREATE PROCEDURE dbo.sp_Pedido_Obtener
    @IdPedido INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT p.IdPedido, p.IdCliente, c.Nombre AS Cliente, c.Comuna, p.Fecha, p.Estado,
           p.Subtotal, p.Despacho, p.Total, p.MetodoPago,
           p.NombreEntrega, p.TelefonoEntrega, p.ComunaEntrega,
           p.DireccionEntrega, p.ReferenciaEntrega,
           ISNULL((SELECT SUM(d.Cantidad) FROM dbo.DetallePedido d WHERE d.IdPedido = p.IdPedido), 0) AS TotalItems
    FROM dbo.Pedido p
    INNER JOIN dbo.Cliente c ON c.IdCliente = p.IdCliente
    WHERE p.IdPedido = @IdPedido;
END
GO

IF OBJECT_ID('dbo.sp_Pedido_ObtenerDetalle', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Pedido_ObtenerDetalle;
GO
CREATE PROCEDURE dbo.sp_Pedido_ObtenerDetalle
    @IdPedido INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT d.IdDetalle, d.IdPedido, d.IdProducto, pr.Nombre AS Producto,
           pr.ImagenUrl, pr.CapacidadKg, d.Cantidad, d.PrecioUnitario, d.Subtotal
    FROM dbo.DetallePedido d
    INNER JOIN dbo.Producto pr ON pr.IdProducto = d.IdProducto
    WHERE d.IdPedido = @IdPedido
    ORDER BY d.IdDetalle;
END
GO

IF OBJECT_ID('dbo.sp_Pedido_Aprobar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Pedido_Aprobar;
GO
-- Estado + stock + venta en una sola unidad atomica. Si falla cualquier
-- paso, no se guarda ninguno.
CREATE PROCEDURE dbo.sp_Pedido_Aprobar
    @IdPedido INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        -- La guarda por estado dentro del UPDATE evita la doble aprobacion:
        -- si dos administradores aprueban a la vez, el segundo obtiene
        -- ROWCOUNT = 0 en lugar de descontar el stock dos veces.
        UPDATE dbo.Pedido
        SET Estado = 'Aprobado'
        WHERE IdPedido = @IdPedido AND Estado = 'Pendiente';

        IF @@ROWCOUNT = 0
            THROW 50020, 'El pedido no existe o ya fue procesado.', 1;

        UPDATE p
        SET p.Stock = p.Stock - d.Cantidad
        FROM dbo.Producto p
        INNER JOIN dbo.DetallePedido d ON d.IdProducto = p.IdProducto
        WHERE d.IdPedido = @IdPedido;

        INSERT INTO dbo.Venta (IdPedido, Monto)
        SELECT IdPedido, Total FROM dbo.Pedido WHERE IdPedido = @IdPedido;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

IF OBJECT_ID('dbo.sp_Pedido_Rechazar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Pedido_Rechazar;
GO
-- Un pedido rechazado NO toca el inventario.
CREATE PROCEDURE dbo.sp_Pedido_Rechazar
    @IdPedido INT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Pedido
    SET Estado = 'Rechazado'
    WHERE IdPedido = @IdPedido AND Estado = 'Pendiente';

    IF @@ROWCOUNT = 0
        THROW 50022, 'El pedido no existe o ya fue procesado.', 1;
END
GO

/* ============================================================
   REPORTES
   ============================================================ */

IF OBJECT_ID('dbo.sp_Reporte_Pedidos', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Reporte_Pedidos;
GO
CREATE PROCEDURE dbo.sp_Reporte_Pedidos
    @IdCliente         INT = NULL,
    @Estado            VARCHAR(10) = NULL,
    @FechaInicial      DATE = NULL,
    @FechaFinal        DATE = NULL,
    @Pagina            INT = 1,
    @CantidadPorPagina INT = 10
AS
BEGIN
    SET NOCOUNT ON;

    IF @Pagina < 1 SET @Pagina = 1;
    IF @CantidadPorPagina < 1 SET @CantidadPorPagina = 10;

    SELECT COUNT(*) AS Total, ISNULL(SUM(p.Total), 0) AS MontoTotal
    FROM dbo.Pedido p
    WHERE (@IdCliente    IS NULL OR p.IdCliente = @IdCliente)
      AND (@Estado       IS NULL OR p.Estado = @Estado)
      AND (@FechaInicial IS NULL OR CAST(p.Fecha AS DATE) >= @FechaInicial)
      AND (@FechaFinal   IS NULL OR CAST(p.Fecha AS DATE) <= @FechaFinal);

    SELECT p.IdPedido, c.Nombre AS Cliente, p.Fecha, p.Estado, p.Total,
           (SELECT COUNT(*) FROM dbo.DetallePedido d WHERE d.IdPedido = p.IdPedido) AS Lineas
    FROM dbo.Pedido p
    INNER JOIN dbo.Cliente c ON c.IdCliente = p.IdCliente
    WHERE (@IdCliente    IS NULL OR p.IdCliente = @IdCliente)
      AND (@Estado       IS NULL OR p.Estado = @Estado)
      AND (@FechaInicial IS NULL OR CAST(p.Fecha AS DATE) >= @FechaInicial)
      AND (@FechaFinal   IS NULL OR CAST(p.Fecha AS DATE) <= @FechaFinal)
    ORDER BY p.Fecha DESC, p.IdPedido DESC
    OFFSET (@Pagina - 1) * @CantidadPorPagina ROWS
    FETCH NEXT @CantidadPorPagina ROWS ONLY;
END
GO

IF OBJECT_ID('dbo.sp_Reporte_Inventario', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_Reporte_Inventario;
GO
CREATE PROCEDURE dbo.sp_Reporte_Inventario
    @Pagina            INT = 1,
    @CantidadPorPagina INT = 10
AS
BEGIN
    SET NOCOUNT ON;

    IF @Pagina < 1 SET @Pagina = 1;
    IF @CantidadPorPagina < 1 SET @CantidadPorPagina = 10;

    SELECT COUNT(*) AS Total, ISNULL(SUM(CAST(Stock AS DECIMAL(18,0)) * Precio), 0) AS ValorInventario
    FROM dbo.Producto
    WHERE Activo = 1;

    SELECT p.IdProducto, p.Codigo, p.Nombre, p.CapacidadKg, p.Precio, p.Stock,
           CAST(p.Stock AS DECIMAL(18,0)) * p.Precio AS ValorStock,
           CASE WHEN p.Stock = 0 THEN 'Sin stock'
                WHEN p.Stock <= 10 THEN 'Stock bajo'
                ELSE 'Disponible' END AS EstadoStock,
           ISNULL(v.Vendidos, 0) AS Vendidos
    FROM dbo.Producto p
    OUTER APPLY (
        SELECT SUM(d.Cantidad) AS Vendidos
        FROM dbo.DetallePedido d
        INNER JOIN dbo.Pedido ped ON ped.IdPedido = d.IdPedido
        WHERE d.IdProducto = p.IdProducto AND ped.Estado = 'Aprobado'
    ) v
    WHERE p.Activo = 1
    ORDER BY p.Nombre
    OFFSET (@Pagina - 1) * @CantidadPorPagina ROWS
    FETCH NEXT @CantidadPorPagina ROWS ONLY;
END
GO

PRINT 'Procedimientos almacenados creados correctamente.';
GO
