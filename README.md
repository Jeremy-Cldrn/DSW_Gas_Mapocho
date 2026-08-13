# Gas Mapocho

Sistema de venta de balones de gas: tienda online para clientes, panel de administración para el negocio, y una API que sirve a ambos.

## Estructura del proyecto

```
db/                    Scripts SQL para crear y poblar la base de datos
src/
├── GasMapocho.Api      Backend: API REST + servicio gRPC (accede a SQL Server)
├── GasMapocho.Ui       Librería compartida: modelos de vista, cliente HTTP hacia la API,
│                       estilos y assets (CSS, imágenes) que usan Tienda y Admin
├── GasMapocho.Tienda   Sitio de cara al cliente: catálogo, carrito, checkout, mis compras
├── GasMapocho.Admin    Panel administrativo: productos, clientes, ventas
└── ClienteGrpc         Consola de prueba para el servicio gRPC de la API (no es parte
                        del sistema en sí, solo sirve para probarlo desde la terminal)
```

`GasMapocho.Tienda` y `GasMapocho.Admin` no le hablan a la base de datos directamente: todo pasa por `GasMapocho.Api`. `GasMapocho.Ui` es la librería que evita duplicar modelos y estilos entre esos dos sitios.

## Requisitos

- .NET 9 SDK
- SQL Server (Express sirve) con instancia `.\SQLEXPRESS`

## Cómo levantarlo

### Opción rápida (recomendada)

Desde PowerShell, en la raíz del proyecto:

```
.\run.ps1              # compila y levanta la API + Tienda + Admin, los tres juntos
.\run.ps1 -Bd           # además recrea la base de datos y recarga los datos de prueba
.\run.ps1 -Detener      # detiene lo que haya quedado corriendo en esos puertos
```

Al terminar, el script imprime las URLs y las credenciales de prueba en la terminal.

### Opción manual

**1. Base de datos** — ejecutar en orden (una sola vez, o de nuevo si cambia el schema):

```
sqlcmd -S .\SQLEXPRESS -E -i db\01_schema.sql
sqlcmd -S .\SQLEXPRESS -E -i db\02_stored_procedures.sql
sqlcmd -S .\SQLEXPRESS -E -i db\03_seed.sql
```

**2. Levantar los tres sitios** (cada uno en su propia terminal, la API primero):

```
dotnet run --project src\GasMapocho.Api                                        # :5000 (REST) y :5005 (gRPC)
dotnet run --project src\GasMapocho.Tienda --urls http://localhost:5001
dotnet run --project src\GasMapocho.Admin  --urls http://localhost:5002
```

Tienda y Admin ya vienen configurados para hablarle a la API en `localhost:5000` (ver `appsettings.json` de cada uno si cambia el puerto).

## Usuarios de prueba

| Rol           | Correo                  | Contraseña |
|---------------|--------------------------|------------|
| Administrador | admin@gasmapocho.cl      | Admin123   |
| Cliente       | juan.perez@correo.cl     | Cliente123 |
