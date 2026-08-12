# ============================================================
# Levanta las aplicaciones de la solucion.
#
#   .\run.ps1               API + los dos frontends (Tienda y Admin)
#   .\run.ps1 -Bd           recrea la base de datos y recarga los datos de prueba
#   .\run.ps1 -Detener      detiene solo lo que este escuchando en esos puertos
#
# La API debe estar arriba para que los frontends muestren datos: es su
# unica fuente. Si no lo esta, las pantallas avisan de la caida en vez de
# decir que no hay registros.
# ============================================================
param(
    [switch]$Detener,
    [switch]$Bd
)

$raiz = $PSScriptRoot
$puertos = @(5000, 5001, 5002, 5005)

# ------------------------------------------------------------
# Detener: se buscan los procesos por puerto en vez de matar todos
# los "dotnet" de la maquina, que podria tumbar trabajo ajeno.
# ------------------------------------------------------------
if ($Detener) {
    $detenidos = 0
    foreach ($puerto in $puertos) {
        try {
            $conexiones = Get-NetTCPConnection -LocalPort $puerto -State Listen -ErrorAction Stop
            foreach ($c in $conexiones) {
                Stop-Process -Id $c.OwningProcess -Force -ErrorAction SilentlyContinue
                Write-Host "  detenido el proceso del puerto $puerto" -ForegroundColor DarkGray
                $detenidos++
            }
        } catch { }
    }
    if ($detenidos -eq 0) { Write-Host "No habia nada corriendo." -ForegroundColor Yellow }
    return
}

# ------------------------------------------------------------
# Base de datos
# ------------------------------------------------------------
if ($Bd) {
    Write-Host "Recreando la base de datos..." -ForegroundColor Cyan
    foreach ($script in @('01_schema.sql', '02_stored_procedures.sql', '03_seed.sql')) {
        # -f 65001 no es opcional: sin el, sqlcmd estropea acentos y enes.
        sqlcmd -S ".\SQLEXPRESS" -E -f 65001 -b -i "$raiz\db\$script"
        if ($LASTEXITCODE -ne 0) {
            Write-Host "Fallo $script." -ForegroundColor Red
            return
        }
        Write-Host "  $script" -ForegroundColor DarkGray
    }
    Write-Host "Base de datos lista." -ForegroundColor Green
    Write-Host ""
}

Write-Host "Compilando..." -ForegroundColor Cyan
dotnet build "$raiz\GasMapocho.sln" --nologo -v quiet
if ($LASTEXITCODE -ne 0) { Write-Host "Fallo la compilacion." -ForegroundColor Red; return }

# La API primero: los frontends dependen de ella.
Start-Process dotnet -ArgumentList "run --project `"$raiz\src\GasMapocho.Api`" --no-launch-profile --no-build" -WindowStyle Hidden
Start-Sleep -Seconds 3

Start-Process dotnet -ArgumentList "run --project `"$raiz\src\GasMapocho.Tienda`" --no-build --urls http://localhost:5001" -WindowStyle Hidden
Start-Process dotnet -ArgumentList "run --project `"$raiz\src\GasMapocho.Admin`"  --no-build --urls http://localhost:5002" -WindowStyle Hidden

Write-Host ""
Write-Host "  Tienda           http://localhost:5001" -ForegroundColor Green
Write-Host "  Panel admin      http://localhost:5002" -ForegroundColor Green
Write-Host "  Web API (REST)   http://localhost:5000/api/productos" -ForegroundColor Green
Write-Host "  gRPC             http://localhost:5005  (usar el cliente)" -ForegroundColor Green
Write-Host ""
Write-Host "  Cliente:        juan.perez@correo.cl / Cliente123   (tienda)" -ForegroundColor Gray
Write-Host "  Administrador:  admin@gasmapocho.cl  / Admin123     (panel)" -ForegroundColor Gray
Write-Host "  Cliente gRPC:   dotnet run --project src\ClienteGrpc" -ForegroundColor Gray
Write-Host "  Detener:        .\run.ps1 -Detener" -ForegroundColor DarkGray
