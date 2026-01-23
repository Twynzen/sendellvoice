# SendellVoice - Sistema de Contact Center Inteligente

---

## Que es SendellVoice?

**SendellVoice** es un sistema de contact center que usa inteligencia artificial para:

1. **Entender lo que dice el cliente** - Ya sea por texto o por voz
2. **Clasificar automaticamente que tipo de consulta es** - Facturacion, soporte tecnico, queja, etc.
3. **Responder de forma inteligente** - Usando informacion de tu base de conocimiento

### Analogia Simple

Imagina que tienes un empleado que:
- Escucha al cliente
- Entiende si es una queja, una pregunta tecnica, o una consulta de facturacion
- Busca en los manuales de la empresa la respuesta correcta
- Responde de forma profesional

**SendellVoice hace exactamente eso, pero automatizado con IA.**

---

## Tabla de Contenidos

1. [Para Quien es Este Sistema?](#para-quien-es-este-sistema)
2. [Que Necesito Instalar?](#que-necesito-instalar)
3. [Instalacion Paso a Paso](#instalacion-paso-a-paso)
4. [Como Funciona Internamente](#como-funciona-internamente)
5. [Usando la API](#usando-la-api)
6. [Ejemplos Practicos Completos](#ejemplos-practicos-completos)
7. [Configuracion Detallada](#configuracion-detallada)
8. [Estructura del Codigo](#estructura-del-codigo)
9. [Seguridad](#seguridad)
10. [Problemas Comunes y Soluciones](#problemas-comunes-y-soluciones)
11. [Preguntas Frecuentes](#preguntas-frecuentes)
12. [Glosario de Terminos](#glosario-de-terminos)

---

## Para Quien es Este Sistema?

### Si vienes de Angular/Frontend:

| Concepto Angular | Equivalente en SendellVoice (.NET) |
|------------------|-----------------------------------|
| `Components` | `Controllers` - Manejan las peticiones HTTP |
| `Services` | `Services` - Logica de negocio (igual!) |
| `Modules` | `Capas (Layers)` - Domain, Application, Infrastructure, Web |
| `Models/Interfaces` | `Entities/Interfaces` - Definicion de datos |
| `HttpClient` | `Repository` - Acceso a datos |
| `environment.ts` | `appsettings.json` - Configuracion |
| `npm install` | `dotnet restore` - Instalar dependencias |
| `ng serve` | `dotnet run` - Ejecutar la aplicacion |

### Si vienes de Java/Spring Boot:

| Concepto Spring Boot | Equivalente en SendellVoice (.NET) |
|---------------------|-----------------------------------|
| `@Controller` | `[ApiController]` |
| `@Service` | Clases en carpeta `Services/` |
| `@Repository` | Interfaces `IXxxRepository` |
| `@Entity` | Clases en carpeta `Entities/` |
| `application.yml` | `appsettings.json` |
| `mvn spring-boot:run` | `dotnet run` |
| Dependency Injection | Exactamente igual! |

---

## Que Necesito Instalar?

### Requisitos Minimos de Hardware

| Componente | Minimo | Recomendado |
|------------|--------|-------------|
| RAM | 8 GB | 16 GB |
| Disco | 20 GB libres | 50 GB libres |
| CPU | 4 nucleos | 8 nucleos |
| GPU | No necesaria | NVIDIA para Whisper rapido |

### Software Requerido (Todo Gratuito)

#### 1. .NET 8 SDK

**Que es?** El kit de desarrollo para ejecutar aplicaciones .NET (como Node.js para JavaScript).

**Como verificar si ya lo tienes:**
```bash
dotnet --version
```

**Si dice "8.0.xxx" ya lo tienes. Si no:**

**Windows:**
1. Ve a https://dotnet.microsoft.com/download/dotnet/8.0
2. Descarga "SDK 8.0.xxx" (el boton grande)
3. Ejecuta el instalador
4. Reinicia la terminal

**Mac:**
```bash
brew install dotnet-sdk
```

**Linux (Ubuntu/Debian):**
```bash
wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb
sudo apt-get update
sudo apt-get install -y dotnet-sdk-8.0
```

#### 2. Docker Desktop

**Que es?** Un programa que permite ejecutar "contenedores" - aplicaciones aisladas con todo lo que necesitan. Usamos Docker para:
- SQL Server (base de datos)
- Qdrant (base de datos vectorial)
- Ollama (modelo de IA)

**Como verificar si ya lo tienes:**
```bash
docker --version
```

**Si no lo tienes:**
1. Ve a https://www.docker.com/products/docker-desktop
2. Descarga para tu sistema operativo
3. Instala y reinicia tu computadora
4. Abre Docker Desktop (debe aparecer un icono de ballena en la barra)

**IMPORTANTE:** Docker Desktop debe estar CORRIENDO (la ballena activa) para que funcione.

#### 3. Git

**Que es?** Control de versiones para descargar y manejar el codigo.

**Como verificar:**
```bash
git --version
```

**Si no lo tienes:**
- **Windows:** https://git-scm.com/download/windows
- **Mac:** `brew install git`
- **Linux:** `sudo apt install git`

#### 4. Editor de Codigo (Opcional pero Recomendado)

**Visual Studio Code** - https://code.visualstudio.com/

Extensiones utiles:
- C# Dev Kit (de Microsoft)
- Docker (de Microsoft)
- REST Client (para probar API)

---

## Instalacion Paso a Paso

### METODO 1: Docker Compose (Mas Facil - Recomendado)

Este metodo instala TODO automaticamente en contenedores.

#### Paso 1: Clonar el Repositorio

```bash
# Abre una terminal y ejecuta:
git clone https://github.com/tu-usuario/sendellvoice.git

# Entra a la carpeta:
cd sendellvoice
```

**Que paso?** Descargaste todo el codigo a tu computadora.

#### Paso 2: Verificar que Docker esta Corriendo

```bash
docker info
```

**Si ves informacion de Docker:** Esta bien, continua.
**Si ves un error:** Abre Docker Desktop y espera que inicie.

#### Paso 3: Configurar Variables de Entorno

```bash
# Copia el archivo de ejemplo:
cp .env.example .env
```

Abre el archivo `.env` con un editor de texto y configura:

```env
# Password para SQL Server (CAMBIA ESTO!)
SQL_SA_PASSWORD=MiPasswordSeguro123!

# Deja el resto por defecto para desarrollo local
```

**IMPORTANTE:** El password de SQL Server debe tener:
- Minimo 8 caracteres
- Al menos una mayuscula
- Al menos una minuscula
- Al menos un numero
- Al menos un caracter especial

#### Paso 4: Iniciar Todos los Servicios

```bash
docker-compose up -d
```

**Que hace este comando?**
- `-d` = Ejecutar en segundo plano (no bloquea la terminal)
- Descarga las imagenes necesarias (primera vez tarda varios minutos)
- Crea y ejecuta 4 contenedores:
  - `sendellvoice-api` - La API principal
  - `sendellvoice-sqlserver` - Base de datos SQL Server
  - `sendellvoice-qdrant` - Base de datos vectorial
  - `sendellvoice-ollama` - Modelo de IA local

**Cuanto tarda?**
- Primera vez: 5-15 minutos (descarga ~5GB)
- Siguientes veces: 10-30 segundos

#### Paso 5: Verificar que Todo Inicio Correctamente

```bash
docker-compose ps
```

**Deberias ver algo como:**
```
NAME                    STATUS
sendellvoice-api        Up (healthy)
sendellvoice-sqlserver  Up (healthy)
sendellvoice-qdrant     Up
sendellvoice-ollama     Up
```

**Si algun servicio dice "Exited" o "Unhealthy":**
```bash
# Ver logs del servicio con problema:
docker-compose logs sendellvoice-api
```

#### Paso 6: Descargar los Modelos de IA

```bash
# Descargar modelo de chat (llama3.2 - ~2GB)
docker exec sendellvoice-ollama ollama pull llama3.2

# Descargar modelo de embeddings (nomic-embed-text - ~300MB)
docker exec sendellvoice-ollama ollama pull nomic-embed-text
```

**Cuanto tarda?** 5-20 minutos dependiendo de tu internet.

**Como saber si ya termino?** Cuando vuelve a aparecer el cursor en la terminal.

#### Paso 7: Verificar que la API Funciona

```bash
curl http://localhost:5000/health
```

**Respuesta esperada (algo similar a):**
```json
{
  "status": "Healthy",
  "results": {
    "sqlserver": { "status": "Healthy" },
    "vectorstore": { "status": "Healthy" }
  }
}
```

**Si no tienes curl:**
- Abre un navegador
- Ve a http://localhost:5000/health
- Deberia mostrar JSON con "Healthy"

#### Paso 8: Abrir la Documentacion Interactiva

Abre en tu navegador: **http://localhost:5000/swagger**

Veras una interfaz donde puedes:
- Ver todos los endpoints disponibles
- Probar cada endpoint directamente
- Ver los modelos de datos

---

### METODO 2: Desarrollo Local (Para Modificar Codigo)

Este metodo es mejor si quieres modificar el codigo y ver cambios en tiempo real.

#### Paso 1-2: Igual que arriba (clonar y verificar Docker)

#### Paso 3: Iniciar Solo las Dependencias

```bash
docker-compose up -d sqlserver qdrant ollama
```

Esto inicia SQL Server, Qdrant y Ollama, pero NO la API.

#### Paso 4: Descargar Modelos de Ollama

```bash
# Si Ollama esta en Docker:
docker exec sendellvoice-ollama ollama pull llama3.2
docker exec sendellvoice-ollama ollama pull nomic-embed-text

# Si instalaste Ollama directamente:
ollama pull llama3.2
ollama pull nomic-embed-text
```

#### Paso 5: Configurar Variables de Entorno

**Windows (PowerShell):**
```powershell
$env:SQL_SA_PASSWORD="MiPasswordSeguro123!"
$env:ConnectionStrings__DefaultConnection="Server=localhost,1433;Database=SendellVoice;User Id=sa;Password=MiPasswordSeguro123!;TrustServerCertificate=True"
```

**Mac/Linux (Bash):**
```bash
export SQL_SA_PASSWORD="MiPasswordSeguro123!"
export ConnectionStrings__DefaultConnection="Server=localhost,1433;Database=SendellVoice;User Id=sa;Password=MiPasswordSeguro123!;TrustServerCertificate=True"
```

#### Paso 6: Restaurar Dependencias .NET

```bash
cd src/SendellVoice.Web
dotnet restore
```

**Que hace?** Descarga todos los paquetes NuGet necesarios (como npm install).

#### Paso 7: Ejecutar la API

```bash
dotnet run
```

**Veras algo como:**
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000
info: Microsoft.Hosting.Lifetime[0]
      Application started.
```

La API esta corriendo en http://localhost:5000

**Para detenerla:** Presiona `Ctrl+C`

---

## Como Funciona Internamente

### Diagrama de Flujo Principal

```
+------------------+     +------------------+     +------------------+
|                  |     |                  |     |                  |
|  CLIENTE ENVIA   | --> |  API RECIBE EL   | --> |  CLASIFICACION   |
|     MENSAJE      |     |     MENSAJE      |     |   DE INTENCION   |
|                  |     |                  |     |                  |
+------------------+     +------------------+     +--------+---------+
                                                          |
                                                          v
+------------------+     +------------------+     +------------------+
|                  |     |                  |     |                  |
|  RESPUESTA AL    | <-- |  GENERACION DE   | <-- |  BUSQUEDA EN     |
|     CLIENTE      |     |    RESPUESTA     |     |  BASE CONOCIM.   |
|                  |     |                  |     |                  |
+------------------+     +------------------+     +------------------+
```

### Explicacion Detallada de Cada Paso

#### Paso 1: Cliente Envia Mensaje

El cliente puede enviar:
- **Texto**: "Tengo un problema con mi factura"
- **Audio**: Un archivo WAV/MP3 que se transcribe automaticamente

#### Paso 2: API Recibe el Mensaje

La API:
1. Valida que el mensaje sea valido
2. Guarda el mensaje en la base de datos
3. Crea o recupera la conversacion

#### Paso 3: Clasificacion de Intencion

El sistema usa IA para determinar QUE TIPO de consulta es:

| Categoria | Ejemplos | Que Hace el Sistema |
|-----------|----------|---------------------|
| `Billing` | "Mi factura esta mal", "Quiero un reembolso" | Ruta a equipo de facturacion |
| `Technical` | "La app no funciona", "Tengo un error" | Crea ticket de soporte |
| `Account` | "Cambie mi password", "No puedo entrar" | Verificacion de identidad |
| `Complaint` | "Estoy muy molesto", "Mal servicio" | **ESCALA A HUMANO** |
| `Emergency` | "Urgente!", "Necesito ayuda ahora" | **ESCALA INMEDIATAMENTE** |
| `Sales` | "Quiero comprar mas", "Planes disponibles" | Ruta a ventas |
| `Cancellation` | "Quiero cancelar", "Dar de baja" | Ruta a retencion |
| `General` | "Hola", "Gracias", "Que horario tienen" | Respuesta automatica |

**Codigo que hace esto:** `src/SendellVoice.Application/Plugins/IntentClassificationPlugin.cs`

#### Paso 4: Busqueda en Base de Conocimiento (RAG)

**Que es RAG?** Retrieval-Augmented Generation = Generacion Aumentada por Recuperacion

En simple: El sistema BUSCA informacion relevante en tus documentos ANTES de responder.

**Como funciona:**
1. El sistema convierte la pregunta en un "vector" (lista de numeros que representan el significado)
2. Busca documentos con vectores similares
3. Toma los 3 documentos mas relevantes
4. Se los pasa a la IA para que genere la respuesta

**Ejemplo:**
- Pregunta: "Como restablezco mi contrasena?"
- Sistema busca en documentos
- Encuentra: "manual-usuario.pdf" pagina 15 sobre contrasenas
- IA usa esa informacion para responder

**Codigo que hace esto:** `src/SendellVoice.Application/Plugins/KnowledgeRetrievalPlugin.cs`

#### Paso 5: Generacion de Respuesta

La IA genera una respuesta usando:
- La pregunta original
- La intencion detectada
- El contexto de documentos encontrados
- El historial de la conversacion

**Codigo que hace esto:** `src/SendellVoice.Application/Plugins/ResponseGenerationPlugin.cs`

#### Paso 6: Respuesta al Cliente

- Se guarda la respuesta en la base de datos
- Se envia al cliente
- Si es necesario, se escala a un humano

---

## Usando la API

### Conceptos Basicos de REST API

Si nunca has usado una API REST:

| Metodo HTTP | Para Que Se Usa | Ejemplo |
|-------------|-----------------|---------|
| `GET` | Obtener datos | Obtener una conversacion |
| `POST` | Crear algo nuevo | Crear una conversacion |
| `PUT` | Actualizar algo existente | Actualizar datos |
| `DELETE` | Eliminar algo | Eliminar un documento |

### Herramientas para Probar la API

#### Opcion 1: Swagger UI (Mas Facil)
Abre http://localhost:5000/swagger en tu navegador.

#### Opcion 2: curl (Terminal)
```bash
curl -X GET http://localhost:5000/api/conversations
```

#### Opcion 3: Postman (Mas Completo)
Descarga de https://www.postman.com/downloads/

### Todos los Endpoints Disponibles

---

### CONVERSACIONES

#### Crear Nueva Conversacion

```http
POST /api/conversations
Content-Type: application/json

{
  "customerId": "cliente-123",
  "channel": "Chat",
  "customerName": "Juan Perez",
  "customerPhone": "+57 300 123 4567",
  "customerEmail": "juan@email.com"
}
```

**Parametros:**
| Campo | Tipo | Requerido | Descripcion |
|-------|------|-----------|-------------|
| customerId | string | SI | Identificador unico del cliente |
| channel | string | SI | Canal: "Chat", "Voice", "Email", "WhatsApp" |
| customerName | string | NO | Nombre del cliente |
| customerPhone | string | NO | Telefono del cliente |
| customerEmail | string | NO | Email del cliente |

**Respuesta Exitosa (201 Created):**
```json
{
  "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "customerId": "cliente-123",
  "customerName": "Juan Perez",
  "channel": "Chat",
  "status": "Active",
  "createdAt": "2025-01-23T10:30:00Z"
}
```

**Posibles Errores:**
| Codigo | Significado | Causa |
|--------|-------------|-------|
| 400 | Bad Request | customerId o channel faltante o invalido |
| 401 | Unauthorized | API Key faltante o invalida (si esta habilitada) |

---

#### Obtener Conversacion por ID

```http
GET /api/conversations/{id}
```

**Ejemplo:**
```bash
curl http://localhost:5000/api/conversations/a1b2c3d4-e5f6-7890-abcd-ef1234567890
```

**Respuesta Exitosa (200 OK):**
```json
{
  "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "customerId": "cliente-123",
  "customerName": "Juan Perez",
  "channel": "Chat",
  "status": "Active",
  "primaryIntent": "Billing",
  "intentConfidence": 0.95,
  "createdAt": "2025-01-23T10:30:00Z",
  "messages": [
    {
      "id": "msg-001",
      "content": "Hola, tengo un problema con mi factura",
      "direction": "Inbound",
      "isFromBot": false,
      "createdAt": "2025-01-23T10:31:00Z"
    },
    {
      "id": "msg-002",
      "content": "Hola Juan, entiendo que tienes un problema con tu factura. Puedo ayudarte a revisarla...",
      "direction": "Outbound",
      "isFromBot": true,
      "createdAt": "2025-01-23T10:31:02Z"
    }
  ]
}
```

**Posibles Errores:**
| Codigo | Significado | Causa |
|--------|-------------|-------|
| 404 | Not Found | No existe conversacion con ese ID |

---

#### Enviar Mensaje a Conversacion

**ESTE ES EL ENDPOINT PRINCIPAL** - Envia un mensaje y obtiene respuesta de la IA.

```http
POST /api/conversations/{id}/messages
Content-Type: application/json

{
  "content": "Tengo un problema con mi factura del mes pasado"
}
```

**Que Sucede Internamente:**
1. Se guarda el mensaje del cliente
2. Se clasifica la intencion (Billing en este caso)
3. Se busca informacion relevante sobre facturacion
4. Se genera respuesta con IA
5. Se guarda y retorna la respuesta

**Respuesta Exitosa (200 OK):**
```json
{
  "isSuccess": true,
  "isEscalated": false,
  "response": "Entiendo que tienes un problema con tu factura del mes pasado. Para ayudarte mejor, necesito que me proporciones tu numero de cuenta o el numero de factura. Tambien puedo explicarte los cargos que aparecen si me indicas cuales te generan dudas.",
  "intent": {
    "intent": "Billing",
    "confidence": 0.92,
    "reasoning": "El cliente menciona explicitamente un problema con la factura"
  }
}
```

**Respuesta cuando se Escala (200 OK):**
```json
{
  "isSuccess": true,
  "isEscalated": true,
  "response": "Entiendo que esto es urgente. Permitame conectarlo con un especialista inmediatamente.",
  "intent": {
    "intent": "Emergency",
    "confidence": 0.98,
    "reasoning": "El cliente indica urgencia"
  }
}
```

---

#### Listar Conversaciones Activas

```http
GET /api/conversations/active
```

**Parametros de Query (opcionales):**
| Parametro | Tipo | Descripcion |
|-----------|------|-------------|
| page | int | Numero de pagina (default: 1) |
| pageSize | int | Elementos por pagina (default: 20) |

**Ejemplo:**
```bash
curl "http://localhost:5000/api/conversations/active?page=1&pageSize=10"
```

**Respuesta:**
```json
{
  "items": [
    {
      "id": "a1b2c3d4...",
      "customerId": "cliente-123",
      "customerName": "Juan Perez",
      "status": "Active",
      "createdAt": "2025-01-23T10:30:00Z"
    }
  ],
  "totalCount": 1,
  "page": 1,
  "pageSize": 10
}
```

---

#### Finalizar Conversacion

```http
POST /api/conversations/{id}/end
Content-Type: application/json

{
  "resolution": "Cliente satisfecho con la respuesta sobre facturacion"
}
```

**Respuesta:**
```json
{
  "id": "a1b2c3d4...",
  "status": "Closed",
  "resolution": "Cliente satisfecho con la respuesta sobre facturacion",
  "endedAt": "2025-01-23T11:00:00Z"
}
```

---

### VOZ (SPEECH)

#### Transcribir Audio a Texto

Convierte un archivo de audio en texto.

```http
POST /api/speech/transcribe
Content-Type: multipart/form-data

audioFile: [archivo.wav]
```

**Formatos de Audio Soportados:**
- WAV (recomendado)
- MP3
- FLAC
- OGG

**Tamano Maximo:** 25MB

**Ejemplo con curl:**
```bash
curl -X POST http://localhost:5000/api/speech/transcribe \
  -F "audioFile=@mi-grabacion.wav"
```

**Respuesta:**
```json
{
  "text": "Hola, necesito ayuda con mi cuenta",
  "confidence": 0.95,
  "language": "es",
  "duration": 3.5
}
```

---

#### Procesar Audio Completo (Transcribir + Analizar)

Transcribe el audio Y clasifica la intencion.

```http
POST /api/speech/process
Content-Type: multipart/form-data

audioFile: [archivo.wav]
conversationId: a1b2c3d4... (opcional)
```

**Respuesta:**
```json
{
  "transcription": "Estoy muy molesto con el servicio",
  "intent": {
    "intent": "Complaint",
    "confidence": 0.97,
    "reasoning": "Expresion de insatisfaccion"
  },
  "suggestedAction": "Escalate",
  "response": "Lamento escuchar sobre su experiencia. Permitame transferirlo con un supervisor..."
}
```

---

### BASE DE CONOCIMIENTO

#### Subir Documento

Agrega un documento a la base de conocimiento para que la IA lo use al responder.

```http
POST /api/knowledgebase/documents
Content-Type: multipart/form-data

file: [manual-usuario.pdf]
category: manuales
tags: usuario,guia,FAQ
```

**Formatos Soportados:**
- PDF
- DOCX
- TXT
- MD (Markdown)
- HTML

**Tamano Maximo:** 50MB

**Ejemplo:**
```bash
curl -X POST http://localhost:5000/api/knowledgebase/documents \
  -F "file=@manual-usuario.pdf" \
  -F "category=manuales" \
  -F "tags=usuario,ayuda"
```

**Respuesta:**
```json
{
  "id": "doc-123",
  "fileName": "manual-usuario.pdf",
  "category": "manuales",
  "tags": ["usuario", "ayuda"],
  "chunksCreated": 15,
  "status": "Indexed",
  "createdAt": "2025-01-23T10:00:00Z"
}
```

**Que significa "chunksCreated"?**
El documento se divide en "chunks" (pedazos) para busqueda eficiente. Un PDF de 10 paginas puede crear 15-30 chunks.

---

#### Consultar Base de Conocimiento

Busca informacion relevante en los documentos.

```http
POST /api/knowledgebase/query
Content-Type: application/json

{
  "query": "Como puedo restablecer mi contrasena?",
  "topK": 3,
  "category": "manuales"
}
```

**Parametros:**
| Campo | Tipo | Requerido | Descripcion |
|-------|------|-----------|-------------|
| query | string | SI | Pregunta o texto a buscar |
| topK | int | NO | Cantidad de resultados (default: 3, max: 20) |
| category | string | NO | Filtrar por categoria |

**Respuesta:**
```json
{
  "results": [
    {
      "documentId": "doc-123",
      "documentName": "manual-usuario.pdf",
      "content": "Para restablecer su contrasena: 1. Haga clic en 'Olvide mi contrasena' 2. Ingrese su email 3. Revise su bandeja de entrada...",
      "score": 0.95,
      "category": "manuales"
    },
    {
      "documentId": "doc-456",
      "documentName": "faq.pdf",
      "content": "Pregunta frecuente: Mi contrasena no funciona. Respuesta: Puede solicitar una nueva contrasena desde...",
      "score": 0.87,
      "category": "faq"
    }
  ],
  "totalResults": 2
}
```

---

#### Listar Documentos

```http
GET /api/knowledgebase/documents?category=manuales
```

**Respuesta:**
```json
{
  "documents": [
    {
      "id": "doc-123",
      "fileName": "manual-usuario.pdf",
      "category": "manuales",
      "tags": ["usuario", "ayuda"],
      "createdAt": "2025-01-23T10:00:00Z"
    }
  ],
  "totalCount": 1
}
```

---

#### Eliminar Documento

```http
DELETE /api/knowledgebase/documents/{id}
```

**Respuesta:** 204 No Content (exito sin contenido)

---

### HEALTH CHECKS

#### Estado Completo del Sistema

```http
GET /health
```

**Respuesta:**
```json
{
  "status": "Healthy",
  "totalDuration": "00:00:00.1234567",
  "entries": {
    "sqlserver": {
      "status": "Healthy",
      "duration": "00:00:00.0234567"
    },
    "ollama": {
      "status": "Healthy",
      "duration": "00:00:00.0567890"
    },
    "vectorstore": {
      "status": "Healthy",
      "duration": "00:00:00.0123456"
    }
  }
}
```

**Estados Posibles:**
- `Healthy` - Todo funcionando correctamente
- `Degraded` - Funcionando pero con problemas menores
- `Unhealthy` - Servicio con fallas

---

## Ejemplos Practicos Completos

### Ejemplo 1: Flujo Completo de Atencion al Cliente

```bash
# 1. Crear conversacion
CONV_ID=$(curl -s -X POST http://localhost:5000/api/conversations \
  -H "Content-Type: application/json" \
  -d '{"customerId": "cli-001", "channel": "Chat", "customerName": "Maria Garcia"}' \
  | jq -r '.id')

echo "Conversacion creada: $CONV_ID"

# 2. Cliente pregunta
curl -X POST "http://localhost:5000/api/conversations/$CONV_ID/messages" \
  -H "Content-Type: application/json" \
  -d '{"content": "Hola, no puedo entrar a mi cuenta"}'

# 3. Cliente da mas detalles
curl -X POST "http://localhost:5000/api/conversations/$CONV_ID/messages" \
  -H "Content-Type: application/json" \
  -d '{"content": "Dice que mi contrasena es incorrecta pero estoy segura que es la correcta"}'

# 4. Finalizar conversacion
curl -X POST "http://localhost:5000/api/conversations/$CONV_ID/end" \
  -H "Content-Type: application/json" \
  -d '{"resolution": "Se guio al cliente para restablecer contrasena"}'
```

### Ejemplo 2: Agregar Documento y Consultar

```bash
# 1. Subir documento de FAQ
curl -X POST http://localhost:5000/api/knowledgebase/documents \
  -F "file=@preguntas-frecuentes.pdf" \
  -F "category=faq" \
  -F "tags=preguntas,ayuda,comun"

# 2. Consultar
curl -X POST http://localhost:5000/api/knowledgebase/query \
  -H "Content-Type: application/json" \
  -d '{"query": "horarios de atencion", "topK": 3}'
```

### Ejemplo 3: Procesar Llamada de Voz

```bash
# 1. Crear conversacion para llamada
CONV_ID=$(curl -s -X POST http://localhost:5000/api/conversations \
  -H "Content-Type: application/json" \
  -d '{"customerId": "cli-002", "channel": "Voice", "customerName": "Carlos Lopez"}' \
  | jq -r '.id')

# 2. Procesar audio de la llamada
curl -X POST http://localhost:5000/api/speech/process \
  -F "audioFile=@llamada-cliente.wav" \
  -F "conversationId=$CONV_ID"
```

---

## Configuracion Detallada

### Archivo Principal: appsettings.json

```json
{
  // === PROVEEDORES DE SERVICIOS ===
  "Providers": {
    // Opciones: "Ollama" (gratis, local) o "AzureOpenAI" (pago, nube)
    "LLM": "Ollama",

    // Opciones: "WhisperNet" (gratis, local) o "AzureSpeech" (pago, nube)
    "Speech": "WhisperNet",

    // Opciones: "InMemory" (testing), "Qdrant" (gratis, local), "AzureAISearch" (pago)
    "VectorStore": "Qdrant"
  },

  // === CONFIGURACION DE OLLAMA ===
  "Ollama": {
    // URL donde corre Ollama
    "Endpoint": "http://localhost:11434",

    // Modelo para chat (debe estar descargado con 'ollama pull')
    "Model": "llama3.2",

    // Modelo para embeddings
    "EmbeddingModel": "nomic-embed-text"
  },

  // === CONFIGURACION DE QDRANT ===
  "Qdrant": {
    // URL donde corre Qdrant
    "Endpoint": "http://localhost:6333",

    // Nombre de la coleccion de vectores
    "CollectionName": "sendellvoice_knowledge"
  },

  // === CONFIGURACION DE SEGURIDAD ===
  "Security": {
    // API Key para autenticacion (dejar vacio = sin autenticacion)
    "ApiKey": "",

    // Si es true, requiere API Key en todas las peticiones
    "RequireApiKey": false
  },

  // === CONFIGURACION DE ARCHIVOS ===
  "FileUpload": {
    // Tamano maximo de archivo en bytes (50MB)
    "MaxFileSizeBytes": 52428800,

    // Extensiones permitidas
    "AllowedExtensions": [".pdf", ".docx", ".txt", ".md", ".html"]
  },

  // === CONFIGURACION CORS ===
  "Cors": {
    // Origenes permitidos (dominios que pueden llamar a la API)
    "AllowedOrigins": ["http://localhost:3000", "http://localhost:5173"]
  },

  // === CONEXION A BASE DE DATOS ===
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=SendellVoice;User Id=sa;Password=TuPassword;TrustServerCertificate=True"
  }
}
```

### Variables de Entorno (Sobreescriben appsettings.json)

Las variables de entorno usan `__` (doble guion bajo) para indicar anidamiento:

```bash
# Ejemplo: "Security": { "ApiKey": "xxx" }
# Se convierte en:
export Security__ApiKey="mi-api-key-secreta"

# Ejemplo: "ConnectionStrings": { "DefaultConnection": "..." }
export ConnectionStrings__DefaultConnection="Server=..."

# Ejemplo: "Providers": { "LLM": "AzureOpenAI" }
export Providers__LLM="AzureOpenAI"
```

### Tabla de Todas las Variables de Entorno

| Variable | Descripcion | Valor por Defecto |
|----------|-------------|-------------------|
| `ConnectionStrings__DefaultConnection` | Conexion a SQL Server | (requerido) |
| `Providers__LLM` | Proveedor LLM | Ollama |
| `Providers__Speech` | Proveedor de voz | WhisperNet |
| `Providers__VectorStore` | Proveedor de vectores | InMemory |
| `Ollama__Endpoint` | URL de Ollama | http://localhost:11434 |
| `Ollama__Model` | Modelo de chat | llama3.2 |
| `Ollama__EmbeddingModel` | Modelo embeddings | nomic-embed-text |
| `Qdrant__Endpoint` | URL de Qdrant | http://localhost:6333 |
| `Qdrant__CollectionName` | Nombre coleccion | sendellvoice_knowledge |
| `Security__ApiKey` | API Key | (vacio) |
| `Security__RequireApiKey` | Requerir auth | false |
| `AzureOpenAI__Endpoint` | URL Azure OpenAI | (vacio) |
| `AzureOpenAI__ApiKey` | Key Azure OpenAI | (vacio) |
| `AzureOpenAI__ChatDeployment` | Deployment chat | gpt-4o-mini |
| `AzureOpenAI__EmbeddingDeployment` | Deployment embed | text-embedding-3-small |

---

## Estructura del Codigo

### Diagrama de Capas

```
+------------------------------------------------------------------+
|                         SendellVoice.Web                          |
|  (Capa de Presentacion - Controllers, Middleware, Configuracion)  |
+------------------------------------------------------------------+
                               |
                               | Depende de
                               v
+------------------------------------------------------------------+
|                      SendellVoice.Application                     |
|  (Capa de Aplicacion - Servicios, Plugins, Logica de Negocio)     |
+------------------------------------------------------------------+
                               |
                               | Depende de
                               v
+------------------------------------------------------------------+
|                      SendellVoice.Infrastructure                  |
|  (Capa de Infraestructura - Base de Datos, APIs Externas, AI)     |
+------------------------------------------------------------------+
                               |
                               | Depende de
                               v
+------------------------------------------------------------------+
|                        SendellVoice.Domain                        |
|  (Capa de Dominio - Entidades, Enums, Interfaces Basicas)         |
+------------------------------------------------------------------+
```

### Explicacion de Cada Capa

#### 1. SendellVoice.Domain (Capa de Dominio)

**Proposito:** Define QUE es el sistema - las entidades y reglas basicas.

**Contenido:**
- `Entities/` - Clases que representan datos (Conversation, Message, etc.)
- `Enums/` - Valores predefinidos (ChannelType, IntentCategory, etc.)
- `Interfaces/` - Contratos para repositorios

**Analogia Angular:** Es como tus `models/` e `interfaces/` en Angular.

**Ejemplo de Entidad:**
```csharp
public class Conversation
{
    public Guid Id { get; set; }
    public string CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public ChannelType Channel { get; set; }
    public ConversationStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

#### 2. SendellVoice.Application (Capa de Aplicacion)

**Proposito:** Define COMO funciona el sistema - la logica de negocio.

**Contenido:**
- `Services/` - Orquestacion de operaciones complejas
- `Plugins/` - Plugins de Semantic Kernel para IA
- `Common/Interfaces/` - Contratos para servicios externos
- `Common/Models/` - DTOs (Data Transfer Objects)

**Analogia Angular:** Es como tus `services/` que contienen logica de negocio.

#### 3. SendellVoice.Infrastructure (Capa de Infraestructura)

**Proposito:** Implementa COMO se conecta con el mundo exterior.

**Contenido:**
- `AI/` - Implementaciones de LLM (Ollama, Azure OpenAI)
- `Speech/` - Implementaciones de voz (Whisper, Azure Speech)
- `VectorStore/` - Implementaciones de vector DB (Qdrant, Azure Search)
- `Data/` - Entity Framework, Repositorios

**Analogia Angular:** Es como tus `services/` que hacen HTTP calls, pero mas organizado.

#### 4. SendellVoice.Web (Capa de Presentacion)

**Proposito:** Expone la funcionalidad via HTTP.

**Contenido:**
- `Controllers/` - Endpoints de la API
- `Middleware/` - Procesamiento de requests (auth, errores)
- `Configuration/` - Clases de configuracion
- `HealthChecks/` - Verificaciones de salud

**Analogia Angular:** Es como tu `app.module.ts` + routing + HTTP interceptors.

### Arbol de Archivos Completo

```
sendellvoice/
|
+-- src/
|   |
|   +-- SendellVoice.Domain/              # CAPA DE DOMINIO
|   |   +-- Entities/
|   |   |   +-- Agent.cs                  # Agente de soporte
|   |   |   +-- Conversation.cs           # Conversacion con cliente
|   |   |   +-- Intent.cs                 # Intencion detectada
|   |   |   +-- KnowledgeDocument.cs      # Documento en base de conocimiento
|   |   |   +-- Message.cs                # Mensaje en conversacion
|   |   |
|   |   +-- Enums/
|   |   |   +-- ChannelType.cs            # Chat, Voice, Email, WhatsApp
|   |   |   +-- ConversationStatus.cs     # Active, Closed, Escalated
|   |   |   +-- IntentCategory.cs         # Billing, Technical, Complaint...
|   |   |   +-- MessageDirection.cs       # Inbound, Outbound
|   |   |   +-- MessageType.cs            # Text, Audio, Image
|   |   |
|   |   +-- Interfaces/
|   |       +-- IConversationRepository.cs
|   |       +-- IMessageRepository.cs
|   |       +-- IKnowledgeDocumentRepository.cs
|   |
|   +-- SendellVoice.Application/         # CAPA DE APLICACION
|   |   +-- Common/
|   |   |   +-- Interfaces/
|   |   |   |   +-- IEmbeddingService.cs  # Generar embeddings
|   |   |   |   +-- ILlmService.cs        # Comunicacion con LLM
|   |   |   |   +-- ISpeechService.cs     # Transcripcion de audio
|   |   |   |   +-- IVectorStoreService.cs # Busqueda vectorial
|   |   |   |
|   |   |   +-- Models/
|   |   |       +-- IntentResult.cs       # Resultado clasificacion
|   |   |       +-- SearchResult.cs       # Resultado busqueda RAG
|   |   |       +-- TranscriptionResult.cs # Resultado transcripcion
|   |   |
|   |   +-- Plugins/                      # Plugins de Semantic Kernel
|   |   |   +-- IntentClassificationPlugin.cs
|   |   |   +-- KnowledgeRetrievalPlugin.cs
|   |   |   +-- ResponseGenerationPlugin.cs
|   |   |
|   |   +-- Services/
|   |       +-- ConversationOrchestrator.cs  # Orquesta todo el flujo
|   |       +-- RAGService.cs                # Logica de RAG
|   |       +-- SpeechToActionService.cs     # Pipeline voz-a-accion
|   |
|   +-- SendellVoice.Infrastructure/      # CAPA DE INFRAESTRUCTURA
|   |   +-- AI/
|   |   |   +-- AzureOpenAIService.cs     # Implementacion Azure
|   |   |   +-- OllamaService.cs          # Implementacion Ollama local
|   |   |
|   |   +-- Data/
|   |   |   +-- ApplicationDbContext.cs   # Contexto EF Core
|   |   |   +-- Repositories/
|   |   |       +-- ConversationRepository.cs
|   |   |       +-- MessageRepository.cs
|   |   |       +-- KnowledgeDocumentRepository.cs
|   |   |
|   |   +-- Speech/
|   |   |   +-- AzureSpeechService.cs     # Azure Speech Services
|   |   |   +-- WhisperNetService.cs      # Whisper.NET local
|   |   |
|   |   +-- VectorStore/
|   |       +-- AzureAISearchService.cs   # Azure AI Search
|   |       +-- InMemoryVectorStore.cs    # Para testing
|   |       +-- QdrantService.cs          # Qdrant local
|   |
|   +-- SendellVoice.Web/                 # CAPA DE PRESENTACION
|       +-- Configuration/
|       |   +-- FileUploadSettings.cs
|       |   +-- SecuritySettings.cs
|       |   +-- RateLimitingSettings.cs
|       |
|       +-- Controllers/
|       |   +-- ConversationsController.cs
|       |   +-- KnowledgeBaseController.cs
|       |   +-- SpeechController.cs
|       |
|       +-- HealthChecks/
|       |   +-- OllamaHealthCheck.cs
|       |   +-- QdrantHealthCheck.cs
|       |   +-- VectorStoreHealthCheck.cs
|       |
|       +-- Middleware/
|       |   +-- ApiKeyAuthMiddleware.cs
|       |   +-- GlobalExceptionHandler.cs
|       |
|       +-- Program.cs                    # Punto de entrada
|
+-- tests/
|   +-- SendellVoice.UnitTests/
|   +-- SendellVoice.IntegrationTests/
|
+-- docker-compose.yml                    # Configuracion Docker
+-- Dockerfile                            # Para construir imagen
+-- .env.example                          # Variables de entorno ejemplo
+-- SendellVoice.sln                      # Archivo de solucion
```

---

## Seguridad

### Autenticacion por API Key

#### Habilitar Autenticacion

1. **Generar una API Key segura:**
```bash
# En Linux/Mac:
openssl rand -base64 32

# Resultado ejemplo: K8xP2mQ7nR3tY6vZ9wB1dF4gH5jL0oN8
```

2. **Configurar via variable de entorno:**
```bash
export Security__ApiKey="K8xP2mQ7nR3tY6vZ9wB1dF4gH5jL0oN8"
export Security__RequireApiKey="true"
```

3. **Usar en requests:**
```bash
curl -H "X-API-Key: K8xP2mQ7nR3tY6vZ9wB1dF4gH5jL0oN8" \
  http://localhost:5000/api/conversations
```

#### Endpoints que NO Requieren API Key

Estos endpoints siempre son publicos:
- `/health` - Health checks
- `/health/ready` - Readiness check
- `/health/live` - Liveness check
- `/swagger` - Documentacion (solo en desarrollo)
- `/` - Endpoint raiz

### CORS (Cross-Origin Resource Sharing)

**Que es?** Controla desde que dominios se puede llamar a la API.

**Desarrollo (por defecto):** Permite cualquier origen
**Produccion:** Solo origenes configurados

**Configurar origenes permitidos:**
```json
{
  "Cors": {
    "AllowedOrigins": [
      "https://mi-frontend.com",
      "https://app.mi-empresa.com"
    ]
  }
}
```

### Proteccion de Archivos

**Validaciones al subir archivos:**

1. **Tamano maximo:** 50MB por defecto
2. **Extensiones permitidas:** Solo .pdf, .docx, .txt, .md, .html
3. **Nombre de archivo:** Se sanitiza para prevenir path traversal
4. **Categorias/Tags:** Solo alfanumericos, guiones y guiones bajos

### Medidas de Seguridad Implementadas

| Medida | Descripcion | Archivo |
|--------|-------------|---------|
| Comparacion tiempo constante | Previene timing attacks en API Key | ApiKeyAuthMiddleware.cs |
| Sanitizacion OData | Previene inyeccion en Azure AI Search | AzureAISearchService.cs |
| Validacion de entrada | Todos los parametros son validados | KnowledgeBaseController.cs |
| Delay en auth fallida | Dificulta ataques de fuerza bruta | ApiKeyAuthMiddleware.cs |
| CORS restrictivo | Solo origenes configurados en produccion | Program.cs |
| Sin credenciales en codigo | Todo via variables de entorno | appsettings.json |

---

## Problemas Comunes y Soluciones

### Problema: Docker no Inicia

**Sintoma:** `docker: command not found` o `Cannot connect to Docker daemon`

**Solucion:**
1. Verifica que Docker Desktop este instalado
2. Abre Docker Desktop
3. Espera a que el icono de la ballena este estatico (no animado)
4. Intenta de nuevo

### Problema: Ollama no Responde

**Sintoma:** Error al enviar mensajes: "Connection refused" o timeout

**Verificar:**
```bash
# Ver si el contenedor esta corriendo
docker ps | grep ollama

# Ver logs
docker logs sendellvoice-ollama

# Probar directamente
curl http://localhost:11434/api/tags
```

**Soluciones:**
1. **Si el contenedor no esta corriendo:**
   ```bash
   docker-compose up -d ollama
   ```

2. **Si no hay modelos:**
   ```bash
   docker exec sendellvoice-ollama ollama pull llama3.2
   ```

3. **Si hay error de memoria:**
   - Cierra otras aplicaciones
   - Aumenta RAM de Docker (Settings > Resources > Memory)

### Problema: Error "Model not found"

**Sintoma:** `Error: model 'llama3.2' not found`

**Solucion:**
```bash
# Listar modelos instalados
docker exec sendellvoice-ollama ollama list

# Si no aparece llama3.2, descargarlo
docker exec sendellvoice-ollama ollama pull llama3.2
```

### Problema: SQL Server Connection Refused

**Sintoma:** `SqlException: Connection refused`

**Verificar:**
```bash
# Ver estado del contenedor
docker ps | grep sqlserver

# Ver logs
docker logs sendellvoice-sqlserver
```

**Soluciones:**
1. **Esperar mas tiempo:** SQL Server tarda ~30 segundos en iniciar
2. **Verificar password:** Debe cumplir requisitos de complejidad
3. **Puerto ocupado:** Verificar que 1433 no este en uso
   ```bash
   # Ver que usa el puerto 1433
   lsof -i :1433
   ```

### Problema: Swagger no Carga

**Sintoma:** http://localhost:5000/swagger muestra pagina en blanco

**Soluciones:**
1. Verificar que la API este corriendo
2. Probar http://localhost:5000/swagger/index.html
3. Abrir consola del navegador (F12) para ver errores

### Problema: Whisper muy Lento

**Sintoma:** Transcripcion tarda minutos

**Causa:** Whisper sin GPU usa CPU, que es mas lento.

**Soluciones:**
1. **Usar modelo mas pequeno:**
   ```json
   {
     "WhisperNet": {
       "ModelSize": "tiny"
     }
   }
   ```

2. **Si tienes GPU NVIDIA:**
   - Instalar CUDA
   - Configurar Docker para usar GPU

### Problema: Embeddings Dimension Mismatch

**Sintoma:** Error sobre dimensiones de vectores no coincidentes

**Causa:** Cambiaste el modelo de embeddings pero los vectores antiguos tienen otra dimension.

**Solucion:**
```bash
# Eliminar coleccion de Qdrant
curl -X DELETE http://localhost:6333/collections/sendellvoice_knowledge

# Reiniciar API (recreara la coleccion)
docker-compose restart sendellvoice-api

# Re-ingestar documentos
```

### Comandos de Diagnostico

```bash
# === ESTADO GENERAL ===
docker-compose ps                           # Ver todos los servicios
docker-compose logs --tail=50              # Ultimas 50 lineas de logs

# === API ===
curl http://localhost:5000/health          # Estado completo
curl http://localhost:5000/                # Info basica

# === OLLAMA ===
curl http://localhost:11434/api/tags       # Modelos instalados
docker exec sendellvoice-ollama ollama list

# === QDRANT ===
curl http://localhost:6333/collections     # Colecciones de vectores

# === SQL SERVER ===
docker exec -it sendellvoice-sqlserver \
  /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P "$SQL_SA_PASSWORD" \
  -Q "SELECT name FROM sys.databases" -C

# === LOGS POR SERVICIO ===
docker-compose logs -f sendellvoice-api    # API (follow)
docker-compose logs sendellvoice-sqlserver # SQL Server
docker-compose logs sendellvoice-ollama    # Ollama
```

---

## Preguntas Frecuentes

### Sobre el Sistema

**P: Puedo usar esto en produccion?**
R: Si, pero debes configurar:
- API Key para autenticacion
- CORS restrictivo
- Azure OpenAI o Ollama en servidor dedicado
- Base de datos SQL Server manejada

**P: Cuanto cuesta usar esto?**
R: Con proveedores locales (Ollama, Qdrant) = $0
Con Azure = Depende del uso, desde ~$50/mes

**P: Funciona en Windows/Mac/Linux?**
R: Si, en los tres sistemas operativos.

**P: Necesito GPU?**
R: No es necesaria, pero acelera Whisper significativamente.

### Sobre la IA

**P: Que tan precisa es la clasificacion de intenciones?**
R: Con llama3.2, aproximadamente 85-95% de precision.

**P: Puedo usar otro modelo de IA?**
R: Si, cualquier modelo compatible con Ollama o Azure OpenAI.

**P: Los datos van a la nube?**
R: Con Ollama, NO - todo es local.
Con Azure OpenAI, SI - revisa terminos de servicio de Azure.

### Sobre Integracion

**P: Puedo conectar esto con WhatsApp?**
R: Si, usando webhook. El campo `channel` soporta "WhatsApp".

**P: Puedo integrarlo con mi sistema existente?**
R: Si, la API REST es estandar y facil de integrar.

**P: Hay SDK para JavaScript/Python/etc?**
R: No hay SDK oficial, pero la API REST funciona con cualquier lenguaje.

---

## Glosario de Terminos

| Termino | Significado |
|---------|-------------|
| **API** | Application Programming Interface - Interfaz para que programas se comuniquen |
| **API Key** | Clave secreta para autenticar peticiones a la API |
| **Clean Architecture** | Patron de diseno que separa el codigo en capas independientes |
| **CORS** | Cross-Origin Resource Sharing - Control de acceso desde otros dominios |
| **Docker** | Plataforma para ejecutar aplicaciones en contenedores aislados |
| **Docker Compose** | Herramienta para definir y ejecutar multiples contenedores |
| **Embeddings** | Representacion numerica de texto para busqueda semantica |
| **Endpoint** | URL especifica de una API que realiza una funcion |
| **Entity Framework** | ORM de Microsoft para acceso a base de datos |
| **Health Check** | Verificacion automatica de que un servicio esta funcionando |
| **LLM** | Large Language Model - Modelo de lenguaje grande (como GPT, Llama) |
| **Middleware** | Codigo que procesa peticiones entre el cliente y el servidor |
| **NuGet** | Gestor de paquetes de .NET (como npm para Node.js) |
| **Ollama** | Herramienta para ejecutar LLMs localmente |
| **ORM** | Object-Relational Mapping - Mapeo entre objetos y base de datos |
| **Plugin** | Componente que extiende funcionalidad (aqui, de Semantic Kernel) |
| **Qdrant** | Base de datos vectorial para busqueda semantica |
| **RAG** | Retrieval-Augmented Generation - Generacion aumentada por recuperacion |
| **REST** | Representational State Transfer - Estilo de arquitectura para APIs |
| **Semantic Kernel** | SDK de Microsoft para integrar LLMs en aplicaciones |
| **Vector Store** | Base de datos especializada en almacenar y buscar vectores |
| **Whisper** | Modelo de OpenAI para transcripcion de audio a texto |

---

## Comandos Rapidos de Referencia

```bash
# === INICIAR TODO ===
docker-compose up -d

# === DETENER TODO ===
docker-compose down

# === VER LOGS EN TIEMPO REAL ===
docker-compose logs -f

# === REINICIAR UN SERVICIO ===
docker-compose restart sendellvoice-api

# === EJECUTAR API EN MODO DESARROLLO ===
cd src/SendellVoice.Web && dotnet run

# === EJECUTAR TESTS ===
dotnet test

# === VERIFICAR SALUD ===
curl http://localhost:5000/health
```

---

## Soporte

- **Issues:** https://github.com/tu-usuario/sendellvoice/issues
- **Email:** soporte@sendellvoice.com

---

## Licencia

MIT License - Puedes usar, modificar y distribuir libremente.

---

*Ultima actualizacion: Enero 2025*
