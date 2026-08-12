/* ============================================================
   GasMapocho — 03_seed.sql
   Datos de prueba suficientes para demostrar el sistema completo
   sin cargar nada a mano.

   Contrasenas (PBKDF2-SHA256, 100.000 iteraciones, sal de 32 bytes):
       admin@gasmapocho.cl   Admin123     (Administrador)
       ventas@gasmapocho.cl  Ventas123    (Administrador)
       juan.perez@correo.cl  Cliente123   (Cliente)
       m.gonzalez@correo.cl  Cliente123   (Cliente)
       c.munoz@correo.cl     Cliente123   (Cliente)

   Idempotente: borra y recarga. Ejecutar con:
       sqlcmd -S .\SQLEXPRESS -E -i db\03_seed.sql
   ============================================================ */

USE GasMapocho;
GO

-- Producto tiene un indice filtrado: cualquier INSERT/UPDATE/DELETE sobre
-- esa tabla exige QUOTED_IDENTIFIER ON.
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

DELETE FROM dbo.Venta;
DELETE FROM dbo.DetallePedido;
DELETE FROM dbo.Pedido;
DELETE FROM dbo.Cliente;
DELETE FROM dbo.Usuario;
DELETE FROM dbo.Producto;
DELETE FROM dbo.Rol;
GO

DBCC CHECKIDENT ('dbo.Venta',         RESEED, 0) WITH NO_INFOMSGS;
DBCC CHECKIDENT ('dbo.DetallePedido', RESEED, 0) WITH NO_INFOMSGS;
DBCC CHECKIDENT ('dbo.Pedido',        RESEED, 0) WITH NO_INFOMSGS;
DBCC CHECKIDENT ('dbo.Cliente',       RESEED, 0) WITH NO_INFOMSGS;
DBCC CHECKIDENT ('dbo.Usuario',       RESEED, 0) WITH NO_INFOMSGS;
DBCC CHECKIDENT ('dbo.Producto',      RESEED, 0) WITH NO_INFOMSGS;
DBCC CHECKIDENT ('dbo.Rol',           RESEED, 0) WITH NO_INFOMSGS;
GO

/* ------------------------------------------------------------ Roles */
SET IDENTITY_INSERT dbo.Rol ON;
INSERT INTO dbo.Rol (IdRol, Nombre) VALUES (1, N'Administrador'), (2, N'Cliente');
SET IDENTITY_INSERT dbo.Rol OFF;
GO

/* ------------------------------------------------------------ Usuarios */
SET IDENTITY_INSERT dbo.Usuario ON;
INSERT INTO dbo.Usuario (IdUsuario, Email, Nombre, PasswordHash, PasswordSalt, IdRol, Activo) VALUES
 (1, N'admin@gasmapocho.cl',  N'Administrador',
     0xa72e111ef6d6f4e113abce952bb2811ae2423d1ea9f0fe36f7be44118bb8efc1,
     0x9d8ecef65ef23b62946b50b20e62caae000c799cd2db92574fd87a660affc02c, 1, 1),
 (2, N'ventas@gasmapocho.cl', N'Rosa Aguilera',
     0x5b9e04530b2de000fb80ab0e8bacdb9d4e2f67dcb0757780a845267107b7b0b1,
     0xa5e9e8b492ce5bf2f9473442c4da52df2838ce096e74de23b202df24f96b0711, 1, 1),
 (3, N'juan.perez@correo.cl', N'Juan Pérez',
     0xa0a5c11e5384d0944e64d9e8c4f72961bbb2ba323a02e90039b7750fbe47aaff,
     0x9997058ef857121c7c6419e22417ef5dd06273a0c63d676c3c29f75f4698f813, 2, 1),
 (4, N'm.gonzalez@correo.cl', N'María González',
     0x5ae8632d5da15b4f65691df240335f64e331e9f2373cb2b5e1fbece7c657424e,
     0xfcf6c8b3599f4b628348f49bd25bd5c6da35b9a297590dd037757bdeab345700, 2, 1),
 (5, N'c.munoz@correo.cl',    N'Carlos Muñoz',
     0x13f5ae129c741f31261976693253896f6fd84ec5f1a4ee1202a85ce176a9d597,
     0xcf071e4e771f17ecb6c5073de7d6915e5a7299dad8407d841cb40341e58215cf, 2, 1);
SET IDENTITY_INSERT dbo.Usuario OFF;
GO

/* ------------------------------------------------------------ Clientes */
SET IDENTITY_INSERT dbo.Cliente ON;
INSERT INTO dbo.Cliente (IdCliente, IdUsuario, Nombre, Documento, Telefono, Correo, Comuna, Direccion, Activo) VALUES
 (1, 3,    N'Juan Pérez',      N'12.345.678-9', N'+56 9 6123 4567', N'juan.perez@correo.cl',  N'Independencia',    N'Av. Independencia 1245',  1),
 (2, 4,    N'María González',  N'13.456.789-0', N'+56 9 7234 5678', N'm.gonzalez@correo.cl',  N'Recoleta',         N'Recoleta 890, depto 42',  1),
 (3, 5,    N'Carlos Muñoz',    N'14.567.890-1', N'+56 9 8345 6789', N'c.munoz@correo.cl',     N'Quinta Normal',    N'Mapocho 3421',            1),
 (4, NULL, N'Ana Rojas',       N'15.678.901-2', N'+56 9 9456 7890', N'ana.rojas@correo.cl',   N'Renca',            N'Domingo Santa María 55',  1),
 (5, NULL, N'Pedro Silva',     N'16.789.012-3', N'+56 9 5567 8901', N'p.silva@correo.cl',     N'Cerro Navia',      N'Los Aromos 118',          1),
 (6, NULL, N'Lucía Fernández', N'17.890.123-4', N'+56 9 4678 9012', N'l.fernandez@correo.cl', N'Conchalí',         N'Guanaco 2210',            1),
 (7, NULL, N'Diego Torres',    N'18.901.234-5', N'+56 9 3789 0123', N'd.torres@correo.cl',    N'Estación Central', N'Alameda 4120',            1);
SET IDENTITY_INSERT dbo.Cliente OFF;
GO

/* ------------------------------------------------------------ Productos
   Los tres balones son los del Anexo 1 del informe. Los tres accesorios
   completan el catalogo para que la busqueda y la paginacion tengan algo
   que paginar: con tres filas no se puede demostrar ninguna de las dos.
   Precios en CLP sin decimales.
   REG-01 queda con stock bajo y MAN-15 en cero, para poder mostrar esos
   dos estados en el listado.
   ------------------------------------------------------------ */
SET IDENTITY_INSERT dbo.Producto ON;
INSERT INTO dbo.Producto (IdProducto, Codigo, Nombre, Descripcion, CapacidadKg, Precio, Stock, Activo) VALUES
 (1, N'BAL-05', N'Balón de gas 5 kg',    N'Balón de gas licuado de 5 kg, ideal para uso domiciliario reducido.',  5, 14500, 24, 1),
 (2, N'BAL-11', N'Balón de gas 11 kg',   N'Balón de gas licuado de 11 kg, el formato más usado en los hogares.', 11, 21000, 18, 1),
 (3, N'BAL-15', N'Balón de gas 15 kg',   N'Balón de gas licuado de 15 kg para alto consumo.',                    15, 28900, 12, 1),
 -- REG-01 queda desactivado (baja lógica): el encargo pide sacarlo del
 -- catálogo cliente, pero sigue siendo un producto real para el panel admin.
 (4, N'REG-01', N'Regulador de presión', N'Regulador con manómetro, compatible con todos los formatos.',          0,  8900,  3, 0),
 (5, N'MAN-15', N'Manguera 1,5 m',       N'Manguera certificada de 1,5 metros con abrazaderas.',                  0,  4500,  0, 1),
 (6, N'KIT-01', N'Kit de instalación',   N'Regulador, manguera y abrazaderas en un solo kit.',                    0, 12900, 15, 1),
 (7, N'BAL-45', N'Balón de Gas 45 kg',   N'Balón de gas licuado de 45 kg, para uso industrial o alto consumo.',  45, 29900, 10, 1),
 (8, N'GH-15',  N'Balón de Gas GH-15',   N'Balón de gas licuado formato GH-15.',                                 15, 84950,  8, 1);
SET IDENTITY_INSERT dbo.Producto OFF;
GO

/* ------------------------------------------------------------ Pedidos
   15 pedidos mezclando los tres estados: sin al menos 12 no se puede
   demostrar la paginacion del panel, que es un item calificado.
   El despacho es 2.500 en todos. Total = Subtotal + Despacho.
   ------------------------------------------------------------ */
SET IDENTITY_INSERT dbo.Pedido ON;
INSERT INTO dbo.Pedido (IdPedido, IdCliente, Fecha, Estado, Subtotal, Despacho, Total, MetodoPago,
                        NombreEntrega, TelefonoEntrega, ComunaEntrega, DireccionEntrega, ReferenciaEntrega) VALUES
 ( 1, 1, DATEADD(DAY,  0, SYSDATETIME()), 'Pendiente', 29000, 2500, 31500, N'Transferencia', N'Juan Pérez',      N'+56 9 6123 4567', N'Independencia',    N'Av. Independencia 1245', N'Portón negro'),
 ( 2, 2, DATEADD(DAY,  0, SYSDATETIME()), 'Pendiente', 35500, 2500, 38000, N'Efectivo',      N'María González',  N'+56 9 7234 5678', N'Recoleta',         N'Recoleta 890, depto 42', N'Conserjería'),
 ( 3, 3, DATEADD(DAY, -1, SYSDATETIME()), 'Aprobado',  28900, 2500, 31400, N'Transferencia', N'Carlos Muñoz',    N'+56 9 8345 6789', N'Quinta Normal',    N'Mapocho 3421',           NULL),
 ( 4, 4, DATEADD(DAY, -1, SYSDATETIME()), 'Aprobado',  43500, 2500, 46000, N'Tarjeta',        N'Ana Rojas',       N'+56 9 9456 7890', N'Renca',            N'Domingo Santa María 55', N'Casa esquina'),
 ( 5, 5, DATEADD(DAY, -2, SYSDATETIME()), 'Rechazado', 42000, 2500, 44500, N'Transferencia', N'Pedro Silva',     N'+56 9 5567 8901', N'Cerro Navia',      N'Los Aromos 118',         NULL),
 ( 6, 1, DATEADD(DAY, -2, SYSDATETIME()), 'Aprobado',  35500, 2500, 38000, N'Efectivo',      N'Juan Pérez',      N'+56 9 6123 4567', N'Independencia',    N'Av. Independencia 1245', NULL),
 ( 7, 2, DATEADD(DAY, -3, SYSDATETIME()), 'Aprobado',  57800, 2500, 60300, N'Tarjeta',        N'María González',  N'+56 9 7234 5678', N'Recoleta',         N'Recoleta 890, depto 42', NULL),
 ( 8, 3, DATEADD(DAY, -4, SYSDATETIME()), 'Aprobado',  14500, 2500, 17000, N'Transferencia', N'Carlos Muñoz',    N'+56 9 8345 6789', N'Quinta Normal',    N'Mapocho 3421',           NULL),
 ( 9, 4, DATEADD(DAY, -5, SYSDATETIME()), 'Rechazado', 21000, 2500, 23500, N'Transferencia', N'Ana Rojas',       N'+56 9 9456 7890', N'Renca',            N'Domingo Santa María 55', NULL),
 (10, 5, DATEADD(DAY, -6, SYSDATETIME()), 'Aprobado',  57900, 2500, 60400, N'Efectivo',      N'Pedro Silva',     N'+56 9 5567 8901', N'Cerro Navia',      N'Los Aromos 118',         NULL),
 (11, 6, DATEADD(DAY, -8, SYSDATETIME()), 'Aprobado',  63000, 2500, 65500, N'Tarjeta',        N'Lucía Fernández', N'+56 9 4678 9012', N'Conchalí',         N'Guanaco 2210',           NULL),
 (12, 7, DATEADD(DAY,-10, SYSDATETIME()), 'Aprobado',  14500, 2500, 17000, N'Transferencia', N'Diego Torres',    N'+56 9 3789 0123', N'Estación Central', N'Alameda 4120',           NULL),
 (13, 6, DATEADD(DAY,-12, SYSDATETIME()), 'Aprobado',  28900, 2500, 31400, N'Efectivo',      N'Lucía Fernández', N'+56 9 4678 9012', N'Conchalí',         N'Guanaco 2210',           NULL),
 (14, 7, DATEADD(DAY,-15, SYSDATETIME()), 'Aprobado',  58000, 2500, 60500, N'Tarjeta',        N'Diego Torres',    N'+56 9 3789 0123', N'Estación Central', N'Alameda 4120',           NULL),
 (15, 5, DATEADD(DAY,  0, SYSDATETIME()), 'Pendiente',  8900, 2500, 11400, N'Transferencia', N'Pedro Silva',     N'+56 9 5567 8901', N'Cerro Navia',      N'Los Aromos 118',         NULL);
SET IDENTITY_INSERT dbo.Pedido OFF;
GO

/* ------------------------------------------------------------ Detalle */
INSERT INTO dbo.DetallePedido (IdPedido, IdProducto, Cantidad, PrecioUnitario, Subtotal) VALUES
 ( 1, 1, 2, 14500, 29000),
 ( 2, 2, 1, 21000, 21000),
 ( 2, 1, 1, 14500, 14500),
 ( 3, 3, 1, 28900, 28900),
 ( 4, 1, 3, 14500, 43500),
 ( 5, 2, 2, 21000, 42000),
 ( 6, 1, 1, 14500, 14500),
 ( 6, 2, 1, 21000, 21000),
 ( 7, 3, 2, 28900, 57800),
 ( 8, 1, 1, 14500, 14500),
 ( 9, 2, 1, 21000, 21000),
 (10, 1, 2, 14500, 29000),
 (10, 3, 1, 28900, 28900),
 (11, 2, 3, 21000, 63000),
 (12, 1, 1, 14500, 14500),
 (13, 3, 1, 28900, 28900),
 (14, 1, 4, 14500, 58000),
 (15, 4, 1,  8900,  8900);
GO

/* ------------------------------------------------------------ Ventas
   Una por cada pedido Aprobado, con el monto de su total.
   ------------------------------------------------------------ */
INSERT INTO dbo.Venta (IdPedido, FechaVenta, Monto)
SELECT IdPedido, Fecha, Total
FROM dbo.Pedido
WHERE Estado = 'Aprobado';
GO

PRINT 'Datos de prueba cargados.';
GO

SELECT 'Rol' AS Tabla, COUNT(*) AS Filas FROM dbo.Rol
UNION ALL SELECT 'Usuario',       COUNT(*) FROM dbo.Usuario
UNION ALL SELECT 'Cliente',       COUNT(*) FROM dbo.Cliente
UNION ALL SELECT 'Producto',      COUNT(*) FROM dbo.Producto
UNION ALL SELECT 'Pedido',        COUNT(*) FROM dbo.Pedido
UNION ALL SELECT 'DetallePedido', COUNT(*) FROM dbo.DetallePedido
UNION ALL SELECT 'Venta',         COUNT(*) FROM dbo.Venta;
GO
