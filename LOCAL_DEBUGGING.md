# Guía Completa de Debugging Local — MedicalCenter

## 1. Prerequisites

### 1.1 Software Requerido

| Software | Versión Mínima | Propósito |
|----------|---------------|-----------|
| .NET SDK | 8.0 | Runtime y compilación |
| PostgreSQL | 14+ | Base de datos principal |
| Redis | 7+ | Cache distribuido |
| Docker | 24+ (opcional) | Contenedores |
| Docker Compose | 2.20+ (opcional) | Orquestación |

### 1.2 Instalar Prerequisites

**Windows (Chocolatey):**
```powershell
choco install dotnet-sdk -y
choco install postgresql -y
choco install redis -y
choco install docker-desktop -y
```

**macOS (Homebrew):**
```bash
brew install dotnet-sdk
brew install postgresql@14
brew install redis
brew install --cask docker
```

**Linux (Ubuntu):**
```bash
wget https://packages.microsoft.com/config/ubuntu/$(lsb_release -rs)/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
sudo apt update
sudo apt install dotnet-sdk-8.0 postgresql redis-server
```

---

## 2. Configuración del Entorno Local

### 2.1 Clonar el Repositorio

```bash
git clone <repo-url>
cd MedicalCenter
```

### 2.2 Base de Datos PostgreSQL

**Iniciar PostgreSQL (local):**
```bash
# Linux/macOS
sudo service postgresql start
# o
brew services start postgresql@14

# Windows (PowerShell como admin)
Start-Service postgresql-x64-14
```

**Crear base de datos:**
```bash
# Conectar como usuario postgres
sudo -u postgres psql

# En el prompt de PostgreSQL:
CREATE DATABASE medical_center;
CREATE USER devuser WITH PASSWORD 'devpass123';
GRANT ALL PRIVILEGES ON DATABASE medical_center TO devuser;
ALTER DATABASE medical_center OWNER TO devuser;
\q
```

**Aplicar migrations:**
```bash
cd src/MedicalCenter.Api
dotnet ef database update --connection "Host=localhost;Port=5432;Database=medical_center;Username=devuser;Password=devpass123"
```

### 2.3 Redis

**Iniciar Redis:**
```bash
# Linux/macOS
redis-server

# macOS
brew services start redis

# Windows (WSL o contenedor)
docker run -d -p 6379:6379 --name redis redis:7-alpine
```

### 2.4 Variables de Entorno

Crear `appsettings.Development.json` en `src/MedicalCenter.Api/`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=medical_center;Username=devuser;Password=devpass123"
  },
  "Jwt": {
    "SecretKey": "dev-secret-key-for-local-testing-min-32-chars",
    "Issuer": "MedicalCenter",
    "Audience": "MedicalCenter.Client"
  },
  "Redis": {
    "ConnectionString": "localhost:6379",
    "InstanceName": "mc-dev"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Information"
    }
  }
}
```

**Nota:** No agregar este archivo al git — ya está en `.gitignore`.

---

## 3. Ejecutar la Aplicación

### 3.1 Compilar

```bash
cd src/MedicalCenter.Api
dotnet build
```

Debería mostrar: `0 Error(s), 0 Warning(s)`

### 3.2 Ejecutar

**Modo desarrollo (con hot-reload):**
```bash
dotnet run --environment Development
```

**Modo debug (vscode):**
```bash
dotnet watch run --environment Development
```

**Puerto por defecto:** `https://localhost:7000` (configurable en `launchSettings.json`)

### 3.3 Verificar que Inicia

```bash
# Health check
curl https://localhost:7000/health/ready

# Respuesta esperada:
{"status":"Healthy"}
```

---

## 4. Ejecutar Tests

### 4.1 Tests Unitarios

```bash
cd tests/MedicalCenter.UnitTests
dotnet test
```

**Resultado esperado:** `56 Passed, 0 Failed`

### 4.2 Tests de Integración (opcional, requiere PostgreSQL real)

```bash
cd tests/MedicalCenter.IntegrationTests
dotnet test
```

### 4.3 Coverage (opcional)

```bash
dotnet test --collect:"XPlat Code Coverage"
# Genera report/coverage/index.html
```

---

## 5. Debugging con IDEs

### 5.1 Visual Studio Code

**Extensiones requeridas:**
- `ms-dotnettools.csharp`
- `ms-azuretools.vscode-docker` (opcional)

**Configurar breakpoints:**

1. Abrir `src/MedicalCenter.Api/MedicalCenter.Api.csproj`
2. Ir a Run → Add Configuration
3. Seleccionar ".NET Core Launch (web)"
4. Modificar `launch.json`:

```json
{
  "name": ".NET Core Launch (web)",
  "type": "coreclr",
  "request": "launch",
  "program": "${workspaceFolder}/src/MedicalCenter.Api/bin/Debug/net8.0/MedicalCenter.Api.dll",
  "args": "",
  "cwd": "${workspaceFolder}/src/MedicalCenter.Api",
  "env": {
    "ASPNETCORE_ENVIRONMENT": "Development"
  }
}
```

**Ejecutar con debug:**
- Presionar F5
- Los breakpoints en cualquier archivo .cs se activarán

### 5.2 Rider / ReSharper

1. Click derecho en `MedicalCenter.Api.csproj`
2. Run → Edit Configurations
3. Agregar .NET Project
4. Configurar environment: `ASPNETCORE_ENVIRONMENT=Development`
5. Click Debug

### 5.3 Visual Studio (Windows)

1. Abrir `MedicalCenter.sln`
2. Click derecho en proyecto API → Set as Startup Project
3. F5 para debug

---

## 6. Debugging de Problemas Comunes

### 6.1 "Connection refused" a PostgreSQL

**Síntoma:**
```
NpgsqlException: connection refused
```

**Solución:**
```bash
# Verificar que PostgreSQL esté corriendo
sudo service postgresql status

# Verificar credenciales
psql -h localhost -U devuser -d medical_center -c "SELECT 1"
```

### 6.2 "Connection refused" a Redis

**Síntoma:**
```
StackExchange.Redis.RedisConnectionException: Unable to connect
```

**Solución:**
```bash
# Verificar que Redis esté corriendo
redis-cli ping
# Debe responder PONG

# Si no está, iniciarlo
redis-server
```

### 6.3 "JWT SecretKey is weak" Warning

**Síntoma:**
```
Critical: JWT SecretKey is weak or uses default value
```

**Solución:** Usar una key de al menos 32 caracteres en `appsettings.Development.json`:

```json
"Jwt": {
  "SecretKey": "this-is-a-dev-key-that-is-long-enough-32+"
}
```

### 6.4 Migration Errors

**Síntoma:**
```
InvalidOperationException: No service for type 'DbContext'
```

**Solución:**
```bash
# Eliminar DB y recrear
dotnet ef database drop --force
dotnet ef database update
```

### 6.5 CORS Errors en Frontend

**Síntoma:**
```
Access to fetch at 'https://localhost:7000/api/...' from origin 'http://localhost:5173' has been blocked by CORS policy
```

**Solución:** Verificar que los orígenes estén en `appsettings.Development.json`:

```json
"Cors": {
  "AllowedOrigins": ["http://localhost:5173", "http://localhost:3000"]
}
```

### 6.6 SSL Certificate Errors (Windows)

**Síntoma:**
```
AuthenticationException: The remote certificate is invalid
```

**Solución:**
```bash
# Entwicklungsmodus ohne HTTPS
dotnet run --environment Development --no-https

# O crear certificado de desarrollo
dotnet dev-certs https --trust
```

---

## 7. Debugging Avanzado

### 7.1 Logs Detallados

**Configurar log level en `appsettings.Development.json`:**

```json
"Logging": {
  "LogLevel": {
    "Default": "Debug",
    "Microsoft.AspNetCore": "Information",
    "Microsoft.EntityFrameworkCore.Database.Command": "Information",
    "MedicalCenter.Application": "Debug"
  }
}
```

**Ver logs en consola:**
```bash
dotnet run --environment Development 2>&1 | grep -i "error"
```

### 7.2 SQL Query Logs

**Habilitar logging de EF Core:**

```csharp
// En Program.cs o al inicio de un servicio
services.AddDbContext<MedicalCenterDbContext>(options =>
    options.LogTo(Console.WriteLine, LogLevel.Information)
           .EnableSensitiveDataLogging());
```

### 7.3 Debug con Postman/cURL

**Verificar endpoint de auth:**
```bash
curl -X POST https://localhost:7000/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"identifier":"admin","password":"Admin123!"}'
```

**Verificar health:**
```bash
curl -v https://localhost:7000/health/ready
```

### 7.4 Memory / Performance Debug

**Ver uso de memoria:**
```bash
dotnet-gcdump collect -p <pid>
# Genera gcdump file para analizar en PerfView
```

---

## 8. Workflow de Desarrollo Recomendado

```
1. git pull origin main
2. dotnet restore
3. dotnet build
4. (Si es primera vez) dotnet ef database update
5. dotnet test
6. dotnet run --environment Development
7. [Hacer cambios]
8. dotnet build && dotnet test
9. git add . && git commit -m "..."
10. push
```

---

## 9. Scripts Útiles

### 9.1 Script de Setup Completo (Linux/macOS)

```bash
#!/bin/bash
set -e

echo "=== MedicalCenter Local Setup ==="

# PostgreSQL
sudo service postgresql start || true

# Crear DB si no existe
sudo -u postgres psql -tc "SELECT 1 FROM pg_database WHERE datname = 'medical_center'" | grep -q 1 || \
  sudo -u postgres psql -c "CREATE DATABASE medical_center;"

# Redis
redis-server --daemonize yes

# Restore y build
dotnet restore
dotnet build

# Migraciones
cd src/MedicalCenter.Api
dotnet ef database update || true

echo "=== Setup Complete ==="
echo "Ejecutar: dotnet run --environment Development"
```

### 9.2 Script de Limpieza

```bash
#!/bin/bash
# Limpiar bin/obj y restaurar
dotnet clean
rm -rf src/*/bin src/*/obj tests/*/bin tests/*/obj
dotnet restore
dotnet build
```

---

## 10. Recursos Adicionales

- **Documentación oficial .NET:** https://docs.microsoft.com/dotnet
- **EF Core Docs:** https://docs.microsoft.com/ef/core
- **PostgreSQL Docs:** https://www.postgresql.org/docs/
- **Redis Docs:** https://redis.io/docs/
- **Azure Functions locally:** https://docs.microsoft.com/azure/azure-functions/functions-run-local

---

## Checklist de Debugging

- [ ] PostgreSQL corriendo y accesible
- [ ] Redis corriendo y accesible
- [ ] Base de datos creada y con migrations aplicadas
- [ ] `appsettings.Development.json` configurado (no en git)
- [ ] `dotnet build` sin errores
- [ ] `dotnet test` pasando
- [ ] Health endpoint respondiendo
- [ ] Login funcional
- [ ] Endpoints principales funcionando