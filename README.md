# SendellVoice

Sistema de Contact Center Inteligente que combina procesamiento de voz, clasificacion de intenciones con IA, y RAG (Retrieval-Augmented Generation) para respuestas contextuales. Construido con .NET 8 y Microsoft Semantic Kernel.

## Tabla de Contenidos

- [Arquitectura](#arquitectura)
- [Caracteristicas](#caracteristicas)
- [Requisitos Previos](#requisitos-previos)
- [Instalacion y Configuracion](#instalacion-y-configuracion)
- [Endpoints de la API](#endpoints-de-la-api)
- [Configuracion de Proveedores](#configuracion-de-proveedores)
- [Seguridad](#seguridad)
- [Estructura del Proyecto](#estructura-del-proyecto)
- [Guia de Desarrollo](#guia-de-desarrollo)
- [Solucion de Problemas](#solucion-de-problemas)

---

## Arquitectura

```
+-----------------------------------------------------------------------------+
|                          ARQUITECTURA SENDELLVOICE                           |
+-----------------------------------------------------------------------------+
|  CAPA DE PRESENTACION                                                        |
|  +-----------+  +-----------+  +-----------+  +-----------+                 |
|  | REST API  |  | WebSocket |  | Webhooks  |  |  Health   |                 |
|  |Controllers|  | (futuro)  |  | (futuro)  |  | Endpoints |                 |
|  +-----+-----+  +-----+-----+  +-----+-----+  +-----------+                 |
+--------+---------------+---------------+------------------------------------+
|  CAPA DE APLICACION (Orquestacion con Semantic Kernel)                      |
|  +------------------+  +------------------+  +------------------+            |
|  | Clasificacion    |  | Pipeline         |  | Gestion Base     |            |
|  | de Intenciones   |  | Voz-a-Accion     |  | de Conocimiento  |            |
|  | (Plugin SK)      |  | (Service)        |  | RAG (Plugin SK)  |            |
|  +--------+---------+  +--------+---------+  +--------+---------+            |
+-----------+--------------------+--------------------+-----------------------+
|  CAPA DE INFRAESTRUCTURA                                                     |
|  +------------+  +------------+  +------------+  +------------+             |
|  | Proveedor  |  | Speech     |  | Vector     |  | SQL Server |             |
|  | LLM        |  | (Azure/    |  | Store      |  | (EF Core)  |             |
|  | (Azure/    |  |  Whisper)  |  | (Azure/    |  |            |             |
|  |  Ollama)   |  |            |  |  Qdrant)   |  |            |             |
|  +------------+  +------------+  +------------+  +------------+             |
+-----------------------------------------------------------------------------+
```

### Principios de Diseno

- **Clean Architecture**: Separacion clara entre capas (Domain, Application, Infrastructure, Web)
- **Patron Strategy**: Proveedores intercambiables para LLM, Speech y VectorStore
- **Dependency Injection**: Todas las dependencias inyectadas para testabilidad
- **CQRS Simplificado**: Separacion de operaciones de lectura/escritura en repositorios

---

## Caracteristicas

### Modulo de Clasificacion de Intenciones

Clasifica automaticamente los mensajes de clientes en categorias predefinidas:

| Categoria | Descripcion | Accion Automatica |
|-----------|-------------|-------------------|
| `Billing` | Facturacion, pagos, reembolsos | Ruta a equipo financiero |
| `Technical` | Problemas tecnicos, errores | Crear ticket de soporte |
| `Account` | Gestion de cuenta, contrasenas | Verificacion de identidad |
| `Complaint` | Quejas, insatisfaccion | **Escalacion automatica** |
| `Emergency` | Situaciones urgentes | **Escalacion inmediata** |
| `Sales` | Interes en productos, upgrades | Ruta a ventas |
| `Cancellation` | Solicitudes de cancelacion | Ruta a retencion |
| `General` | Consultas generales | Respuesta automatica |

### Pipeline Voz-a-Accion

1. **Transcripcion**: Audio -> Texto (Whisper.NET o Azure Speech)
2. **Clasificacion**: Texto -> Intencion detectada
3. **Determinacion de Accion**: Intencion -> Accion ejecutable
4. **Respuesta**: Generacion contextual con RAG

### Base de Conocimiento RAG

- **Formatos soportados**: PDF, DOCX, TXT, MD, HTML
- **Chunking inteligente**: Division semantica de documentos
- **Embeddings**: Vectorizacion con modelos locales u OpenAI
- **Busqueda semantica**: Recuperacion por similitud coseno

---

## Requisitos Previos

### Para Desarrollo Local (Costo $0)

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop)
- [Ollama](https://ollama.ai) - LLM local gratuito

### Para Produccion con Azure

- Suscripcion de Azure
- Azure OpenAI (requiere solicitud de acceso)
- Azure Speech Services (opcional)
- Azure AI Search (opcional)

---

## Instalacion y Configuracion

### Opcion 1: Docker Compose (Recomendado)

```bash
# Clonar el repositorio
git clone https://github.com/tu-usuario/sendellvoice.git
cd sendellvoice

# Configurar variables de entorno
cp .env.example .env
# Editar .env con tus configuraciones

# Iniciar todos los servicios
docker-compose up -d

# Descargar modelos de Ollama (primera vez)
docker exec sendellvoice-ollama ollama pull llama3.2
docker exec sendellvoice-ollama ollama pull nomic-embed-text

# Verificar que todo funciona
curl http://localhost:5000/health
```

**Servicios iniciados:**
- API: http://localhost:5000
- Swagger UI: http://localhost:5000/swagger
- SQL Server: localhost:1433
- Qdrant: localhost:6333
- Ollama: localhost:11434

### Opcion 2: Desarrollo Local

```bash
# 1. Iniciar solo las dependencias
docker-compose up -d sqlserver qdrant ollama

# 2. Descargar modelos de Ollama
ollama pull llama3.2
ollama pull nomic-embed-text

# 3. Configurar variables de entorno
export SQL_SA_PASSWORD="TuPasswordSeguro123!"
export ConnectionStrings__DefaultConnection="Server=localhost,1433;Database=SendellVoice;User Id=sa;Password=${SQL_SA_PASSWORD};TrustServerCertificate=True"

# 4. Ejecutar migraciones
cd src/SendellVoice.Web
dotnet ef database update

# 5. Iniciar la API
dotnet run
```

---

## Endpoints de la API

### Conversaciones

| Metodo | Endpoint | Descripcion |
|--------|----------|-------------|
| `POST` | `/api/conversations` | Crear nueva conversacion |
| `GET` | `/api/conversations/{id}` | Obtener detalles de conversacion |
| `POST` | `/api/conversations/{id}/messages` | Enviar mensaje y obtener respuesta IA |
| `GET` | `/api/conversations/{id}/messages` | Obtener historial de mensajes |
| `GET` | `/api/conversations/active` | Listar conversaciones activas |
| `POST` | `/api/conversations/{id}/end` | Finalizar conversacion |

**Ejemplo - Crear conversacion:**
```bash
curl -X POST http://localhost:5000/api/conversations \
  -H "Content-Type: application/json" \
  -d '{
    "customerId": "cliente-123",
    "channel": "Chat",
    "customerName": "Juan Perez",
    "customerEmail": "juan@example.com"
  }'
```

**Ejemplo - Enviar mensaje:**
```bash
curl -X POST http://localhost:5000/api/conversations/{id}/messages \
  -H "Content-Type: application/json" \
  -d '{
    "content": "Tengo un problema con mi factura del mes pasado"
  }'
```

### Speech (Voz)

| Metodo | Endpoint | Descripcion |
|--------|----------|-------------|
| `POST` | `/api/speech/transcribe` | Transcribir audio a texto |
| `POST` | `/api/speech/process` | Procesar audio y determinar accion |
| `GET` | `/api/speech/health` | Estado del servicio de voz |

**Ejemplo - Transcribir audio:**
```bash
curl -X POST http://localhost:5000/api/speech/transcribe \
  -F "audioFile=@grabacion.wav"
```

### Base de Conocimiento

| Metodo | Endpoint | Descripcion |
|--------|----------|-------------|
| `POST` | `/api/knowledgebase/documents` | Subir e ingestar documento |
| `POST` | `/api/knowledgebase/query` | Consultar base de conocimiento |
| `GET` | `/api/knowledgebase/documents` | Listar documentos |
| `DELETE` | `/api/knowledgebase/documents/{id}` | Eliminar documento |

**Ejemplo - Ingestar documento:**
```bash
curl -X POST http://localhost:5000/api/knowledgebase/documents \
  -F "file=@manual-usuario.pdf" \
  -F "category=manuales" \
  -F "tags=usuario,guia,FAQ"
```

**Ejemplo - Consultar:**
```bash
curl -X POST http://localhost:5000/api/knowledgebase/query \
  -H "Content-Type: application/json" \
  -d '{
    "query": "Como puedo restablecer mi contrasena?",
    "topK": 3
  }'
```

### Health Checks

| Endpoint | Descripcion |
|----------|-------------|
| `/health` | Estado completo de todos los servicios |
| `/health/ready` | Verificacion de disponibilidad |
| `/health/live` | Verificacion de vida (liveness) |

---

## Configuracion de Proveedores

### Archivo de Configuracion Principal

`appsettings.json`:
```json
{
  "Providers": {
    "LLM": "Ollama",
    "Speech": "WhisperNet",
    "VectorStore": "InMemory"
  }
}
```

### Opciones de Proveedores

| Servicio | Opcion Local (Gratis) | Opcion Azure (Pago) |
|----------|----------------------|---------------------|
| **LLM** | `Ollama` | `AzureOpenAI` |
| **Speech** | `WhisperNet` | `AzureSpeech` |
| **VectorStore** | `InMemory`, `Qdrant` | `AzureAISearch` |

### Comparativa de Costos

| Servicio | Azure (Pago) | Alternativa Local (Gratis) |
|----------|--------------|---------------------------|
| LLM Chat | GPT-4o-mini: $0.15-0.60/1M tokens | Ollama + Llama 3.2: **$0** |
| Embeddings | text-embedding-3-small: $0.02/1M tokens | Ollama + nomic-embed: **$0** |
| Speech-to-Text | Azure Speech: $1/hora | Whisper.NET: **$0** |
| Vector Search | Azure AI Search: $73/mes | Qdrant/InMemory: **$0** |

### Configuracion de Ollama (Local)

```json
{
  "Ollama": {
    "Endpoint": "http://localhost:11434",
    "Model": "llama3.2",
    "EmbeddingModel": "nomic-embed-text"
  }
}
```

### Configuracion de Azure OpenAI

```json
{
  "AzureOpenAI": {
    "Endpoint": "https://tu-recurso.openai.azure.com/",
    "ApiKey": "",
    "ChatDeployment": "gpt-4o-mini",
    "EmbeddingDeployment": "text-embedding-3-small"
  }
}
```

**Nota de Seguridad:** Nunca almacenes API keys en archivos de configuracion. Usa variables de entorno o Azure Key Vault.

---

## Seguridad

### Autenticacion por API Key

La API soporta autenticacion opcional mediante API Key:

```json
{
  "Security": {
    "ApiKey": "",
    "RequireApiKey": false
  }
}
```

Para habilitar, configura `RequireApiKey: true` y proporciona la API Key via variable de entorno:

```bash
export Security__ApiKey="tu-api-key-segura"
export Security__RequireApiKey=true
```

Uso en requests:
```bash
curl -H "X-API-Key: tu-api-key-segura" http://localhost:5000/api/conversations
```

### Configuracion CORS

En produccion, configura origenes permitidos:

```json
{
  "Cors": {
    "AllowedOrigins": ["https://tu-frontend.com", "https://app.tu-dominio.com"]
  }
}
```

### Validacion de Archivos

La carga de archivos esta protegida con:

- Limite de tamano configurable (default: 50MB)
- Lista blanca de extensiones permitidas
- Validacion de nombre de archivo (prevencion path traversal)
- Sanitizacion de categorias y tags

```json
{
  "FileUpload": {
    "MaxFileSizeBytes": 52428800,
    "AllowedExtensions": [".pdf", ".docx", ".txt", ".md", ".html"]
  }
}
```

### Medidas de Seguridad Implementadas

1. **Prevencion de Inyeccion OData**: Sanitizacion de filtros en Azure AI Search
2. **Comparacion de API Key en Tiempo Constante**: Previene ataques de timing
3. **CORS Restrictivo en Produccion**: Solo origenes configurados
4. **Validacion de Entrada**: En todos los endpoints
5. **Logging Seguro**: Sin exposicion de datos sensibles

---

## Estructura del Proyecto

```
SendellVoice/
|
+-- src/
|   +-- SendellVoice.Domain/           # Capa de Dominio
|   |   +-- Entities/                  # Entidades de negocio
|   |   |   +-- Conversation.cs
|   |   |   +-- Message.cs
|   |   |   +-- Intent.cs
|   |   |   +-- KnowledgeDocument.cs
|   |   |   +-- Agent.cs
|   |   +-- Enums/                     # Enumeraciones
|   |   |   +-- ChannelType.cs
|   |   |   +-- ConversationStatus.cs
|   |   |   +-- IntentCategory.cs
|   |   +-- Interfaces/                # Contratos de repositorios
|   |
|   +-- SendellVoice.Application/      # Capa de Aplicacion
|   |   +-- Common/
|   |   |   +-- Interfaces/            # Contratos de servicios
|   |   |   +-- Models/                # DTOs y modelos
|   |   +-- Plugins/                   # Plugins de Semantic Kernel
|   |   |   +-- IntentClassificationPlugin.cs
|   |   |   +-- ResponseGenerationPlugin.cs
|   |   |   +-- KnowledgeRetrievalPlugin.cs
|   |   +-- Services/                  # Servicios de aplicacion
|   |       +-- ConversationOrchestrator.cs
|   |       +-- SpeechToActionService.cs
|   |       +-- RAGService.cs
|   |
|   +-- SendellVoice.Infrastructure/   # Capa de Infraestructura
|   |   +-- AI/                        # Implementaciones LLM
|   |   |   +-- OllamaService.cs
|   |   |   +-- AzureOpenAIService.cs
|   |   +-- Speech/                    # Implementaciones Speech
|   |   |   +-- WhisperNetService.cs
|   |   |   +-- AzureSpeechService.cs
|   |   +-- VectorStore/               # Implementaciones VectorDB
|   |   |   +-- InMemoryVectorStore.cs
|   |   |   +-- QdrantService.cs
|   |   |   +-- AzureAISearchService.cs
|   |   +-- Data/                      # EF Core
|   |       +-- ApplicationDbContext.cs
|   |       +-- Repositories/
|   |
|   +-- SendellVoice.Web/              # Capa de Presentacion
|       +-- Controllers/               # API Controllers
|       +-- Middleware/                # Middleware personalizado
|       +-- Configuration/             # Clases de configuracion
|       +-- HealthChecks/              # Health checks personalizados
|       +-- Program.cs                 # Punto de entrada
|
+-- tests/
|   +-- SendellVoice.UnitTests/
|   +-- SendellVoice.IntegrationTests/
|
+-- docker-compose.yml
+-- Dockerfile
+-- SendellVoice.sln
```

---

## Guia de Desarrollo

### Ejecutar Tests

```bash
# Tests unitarios
dotnet test tests/SendellVoice.UnitTests

# Tests de integracion
dotnet test tests/SendellVoice.IntegrationTests

# Con cobertura
dotnet test --collect:"XPlat Code Coverage"
```

### Agregar Nuevo Proveedor

1. Crear implementacion en `Infrastructure`:
```csharp
public class MiNuevoLLMService : ILlmService
{
    // Implementar metodos de la interfaz
}
```

2. Registrar en `DependencyInjection.cs`:
```csharp
case "MiNuevoLLM":
    services.AddSingleton<ILlmService, MiNuevoLLMService>();
    break;
```

3. Agregar configuracion en `appsettings.json`

### Crear Nuevo Plugin de Semantic Kernel

```csharp
public class MiNuevoPlugin
{
    [KernelFunction("mi_funcion")]
    [Description("Descripcion de lo que hace")]
    public async Task<string> MiFuncionAsync(
        [Description("Parametro de entrada")] string input,
        Kernel kernel)
    {
        // Implementacion
    }
}
```

---

## Solucion de Problemas

### Problemas Comunes

| Problema | Causa | Solucion |
|----------|-------|----------|
| Ollama no responde | Servicio no iniciado | Ejecutar `ollama serve` o verificar contenedor Docker |
| Error "model not found" | Modelo no descargado | Ejecutar `ollama pull llama3.2` |
| SQL Server connection refused | Contenedor no listo | Esperar healthcheck o verificar password |
| Whisper muy lento | Sin GPU disponible | Usar modelo mas pequeno (`tiny` o `base`) |
| Embeddings dimension mismatch | Modelo cambiado | Recrear indice vectorial |

### Comandos de Diagnostico

```bash
# Verificar servicios Docker
docker-compose ps

# Ver logs de la API
docker-compose logs -f sendellvoice-api

# Verificar Ollama
curl http://localhost:11434/api/tags

# Verificar Qdrant
curl http://localhost:6333/collections

# Verificar SQL Server
docker exec -it sendellvoice-sqlserver \
  /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P "$SQL_SA_PASSWORD" \
  -Q "SELECT 1" -C
```

### Logs y Monitoreo

Los logs se escriben en:
- Consola (formato estructurado)
- Archivo: `logs/sendellvoice-{fecha}.log`

Configurar nivel de log en `appsettings.json`:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

---

## Variables de Entorno

| Variable | Descripcion | Valor por Defecto |
|----------|-------------|-------------------|
| `ConnectionStrings__DefaultConnection` | Cadena de conexion SQL Server | - |
| `Providers__LLM` | Proveedor LLM | `Ollama` |
| `Providers__Speech` | Proveedor Speech | `WhisperNet` |
| `Providers__VectorStore` | Proveedor VectorStore | `InMemory` |
| `Ollama__Endpoint` | Endpoint de Ollama | `http://localhost:11434` |
| `Ollama__Model` | Modelo de chat | `llama3.2` |
| `Security__ApiKey` | API Key para autenticacion | - |
| `Security__RequireApiKey` | Requerir autenticacion | `false` |
| `AzureOpenAI__Endpoint` | Endpoint Azure OpenAI | - |
| `AzureOpenAI__ApiKey` | API Key Azure OpenAI | - |

---

## Licencia

MIT License

---

## Contribuciones

Las contribuciones son bienvenidas. Por favor:

1. Fork el repositorio
2. Crea una rama para tu feature (`git checkout -b feature/nueva-funcionalidad`)
3. Commit tus cambios (`git commit -m 'Agregar nueva funcionalidad'`)
4. Push a la rama (`git push origin feature/nueva-funcionalidad`)
5. Abre un Pull Request

---

## Soporte

Para reportar problemas o solicitar nuevas funcionalidades, abre un issue en el repositorio.
