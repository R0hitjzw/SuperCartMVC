# SuperCart — Comparador de Precios de Supermercado

# ***https://supercart-production.up.railway.app***

> Trabajo de Final de Curso · 2º DAW (Desarrollo de Aplicaciones Web)  
> Aplicación web ASP.NET Core MVC con integración de IA Claude Haiku y Google Maps

---

## Índice

1. [Contexto General](#1-contexto-general)
2. [Arquitectura y Tecnología](#2-arquitectura-y-tecnología)
3. [Funcionalidades](#3-funcionalidades)
4. [Desarrollo y Evolución](#4-desarrollo-y-evolución)
5. [Seguridad y Auditoría](#5-seguridad-y-auditoría)
6. [DevOps / Operación](#6-devops--operación)
7. [Conocimiento Clave para Presentaciones](#7-conocimiento-clave-para-presentaciones)
8. [Puntos Abiertos](#8-puntos-abiertos)
9. [Resumen Ejecutivo](#9-resumen-ejecutivo)

---

## 1. Contexto General

### Objetivo del proyecto

SuperCart es una aplicación web que permite a los usuarios **comparar precios de productos de alimentación en tiempo real** entre los principales supermercados de España (y especialmente de Cataluña). El usuario introduce un término de búsqueda y la aplicación consulta simultáneamente las APIs de 8 supermercados, agrega los resultados, los filtra por relevancia mediante IA, y los presenta ordenados.

### Problema que resuelve

El consumidor que quiere comprar "leche entera" no tiene forma sencilla de saber si le conviene ir a Mercadona, Aldi o Consum sin entrar en cada web por separado. SuperCart centraliza esa búsqueda en una sola interfaz, con filtrado inteligente para eliminar resultados irrelevantes (ej: "leche corporal Nivea" cuando se busca "leche", o "carne de ñora" cuando se busca "carne").

### Caso de uso principal

1. El usuario entra en SuperCart y escribe "leche entera" en el buscador.
2. La aplicación consulta en paralelo las APIs de Mercadona, Aldi, Consum, DIA, Alcampo, Ametller, Bonpreu y Condis.
3. Los resultados se filtran por relevancia (regex + IA), se ordenan y se muestran en tarjetas.
4. El usuario puede guardar productos como favoritos, añadirlos al carrito, dejar reseñas y ver la ruta en Google Maps al supermercado más conveniente.

### Usuarios objetivo

- Consumidores que hacen la compra habitualmente y quieren ahorrar tiempo y dinero.
- Residentes en España, especialmente en Cataluña (Bonpreu, Ametller, Condis son supermercados catalanes).
- Usuarios con cuenta registrada para acceder a favoritos, carrito y reseñas.

### Estado actual del proyecto

**Funcionalidades core completas y desplegadas en producción:**

- Búsqueda multi-supermercado en tiempo real ✅
- Filtrado por relevancia con IA (3 niveles) ✅
- Sistema de usuarios (registro, login, recuperación de contraseña) ✅
- Favoritos y carrito de compra persistidos en DB ✅
- Reviews y valoraciones (1-5 estrellas) con panel lateral de detalle ✅
- Google Maps con ruta óptima al supermercado ✅
- Tema claro/oscuro con persistencia en localStorage ✅
- Traducción automática ES→CA para Bonpreu ✅
- Sort combinado "Relevancia + €/kg ↑" ✅
- Base de datos PostgreSQL persistente en Railway ✅
- Data Protection Keys persistidas en DB (sesiones compartidas entre entornos) ✅
- Seed de reseñas de prueba con endpoint protegido por clave ✅
- Desplegado en producción: https://supercart-production.up.railway.app ✅

---

## 2. Arquitectura y Tecnología

### Stack tecnológico

| Capa | Tecnología |
|------|-----------|
| Backend | C# ASP.NET Core MVC (.NET 8) |
| Frontend | Razor Views (.cshtml), Vanilla JS (ES2022+), CSS custom properties |
| Base de datos | PostgreSQL (Railway) |
| ORM | Entity Framework Core + Npgsql |
| Autenticación | ASP.NET Core Identity |
| Data Protection | Microsoft.AspNetCore.DataProtection.EntityFrameworkCore |
| IA / LLM | Anthropic Claude Haiku (`claude-haiku-4-5-20251001`) |
| Mapas | Google Maps JS API, Places API (New), Geocoding API, Directions API |
| HTTP client | `System.Net.Http.HttpClient` (singleton estático en `AbstractFinder`) |
| Hosting | Railway (app + PostgreSQL) |

### Servicios externos / APIs

#### APIs de supermercados (consumidas directamente)

Todas son APIs REST no oficiales obtenidas por ingeniería inversa del tráfico de las webs de compra online de cada cadena:

| Supermercado | Notas destacadas |
|---|---|
| Mercadona | Requiere headers específicos |
| Aldi | — |
| Consum | — |
| Alcampo | Necesitó configuración TLS 1.3 + Brotli decompression |
| Ametller Origen | — |
| **Bonpreu / Esclat** | API en catalán; requiere `User-Agent` de navegador real (403 sin él); param obligatorio `tag=web`; búsqueda traducida ES→CA |
| DIA | — |
| Condis | — |
| ~~Carrefour~~ | **Descartado**: bloqueaba todas las peticiones incluso con TLS 1.3 y User-Agent real |

#### Anthropic API (Claude Haiku)

Usada para tres propósitos distintos:

1. **Filtro de relevancia** (`GroupByRelevanceInternal`): clasifica productos en `relevantes`, `dudosos`, `excluidos`. Se activa cuando hay más de 8 productos tras el regex filter.
2. **BestByMarket** (`/find/bestbymarket`): selecciona el mejor producto de cada supermercado para el término buscado, teniendo en cuenta precio/unidad.
3. **Traducción ES→CA** (`CatalanTranslatorService`): traduce el término de búsqueda al catalán antes de consultar Bonpreu.

#### Google Maps APIs

- **JavaScript API + Places API (New)**: geocodificación de supermercados por nombre de la cadena + ciudad del usuario.
- **Directions API**: cálculo de ruta en coche desde la ubicación del usuario hasta el supermercado seleccionado.
- **Geocoding API (clásica)**: geolocalización inversa (lat/lng → nombre de ciudad) + fallback de geocodificación si Places API falla.

### Infraestructura

- **Desarrollo:** `dotnet run` en local, conectado a PostgreSQL de Railway (misma DB que producción).
- **Producción:** Railway hosting con PostgreSQL persistente. URL: https://supercart-production.up.railway.app
- Las migraciones se aplican automáticamente al arrancar (`db.Database.Migrate()` en startup).
- Las Data Protection Keys se persisten en la tabla `DataProtectionKeys` de PostgreSQL, lo que permite que las sesiones de usuario funcionen tanto en local como en producción sin invalidaciones.

### Arquitectura general

```
┌─────────────────────────────────────────────────────────────┐
│                    Browser (JS + CSS)                        │
│  Razor Views:                                                │
│    Home/Index.cshtml (~2150 líneas) — buscador principal     │
│    Cart/Index.cshtml              — carrito de compra        │
│    Favorito/Index.cshtml          — perfil: favs + reviews   │
│    Account/{Login,Register,...}   — autenticación            │
└──────────────────────┬──────────────────────────────────────┘
                       │ HTTP / Fetch API
┌──────────────────────▼──────────────────────────────────────┐
│                  ASP.NET Core MVC                            │
│                                                              │
│  HomeController     → GET  /                                 │
│  FindController     → GET  /find                             │
│                     → POST /find/bestbymarket                │
│                     → POST /find/groupbyrelevance            │
│  FavoritoController → GET/POST /Favorito                     │
│  CartController     → GET/POST /Cart                         │
│  ReviewController   → GET/POST /Review                       │
│  AccountController  → GET/POST /Account                      │
│  SeedController     → GET /Seed/Reviews?key=...              │
└───────┬───────────────────┬─────────────────────────────────┘
        │ EF Core           │ DI (IEnumerable<IFinder>)
        ▼                   ▼
┌───────────────┐  ┌────────────────────────────────────────┐
│  AppDbContext │  │  AbstractFinder (base)                 │
│  (PostgreSQL) │  │    ├─ MercadonaFinder                  │
│               │  │    ├─ AldiFinder                       │
│  Favoritos    │  │    ├─ ConsumFinder                     │
│  Reviews      │  │    ├─ AlcampoFinder                    │
│  CartItems    │  │    ├─ AmetllerFinder                   │
│  Identity     │  │    ├─ DiaFinder                        │
│  DataProt.    │  │    ├─ CondisFinder                     │
│  (usuarios)   │  │    └─ BonpreuFinder                    │
└───────────────┘  │         └─ CatalanTranslatorService     │
                   │              └─ AnthropicKeyRotator     │
                   └────────────────────────────────────────┘
                                     │
                           ┌─────────▼──────────┐
                           │   APIs Externas     │
                           │  8 supermercados    │
                           │  Anthropic Haiku    │
                           │  Google Maps        │
                           └────────────────────┘
```

### Decisiones técnicas importantes

#### 1. `Layout = null` en todas las vistas
Todas las vistas son HTML autocontenido sin `_Layout.cshtml`. Control total del CSS sin conflictos, temas personalizados por página. Coste: duplicación de header y estilos base entre páginas.

#### 2. Patrón IFinder + AbstractFinder
Cada supermercado implementa `IFinder` a través de `AbstractFinder`, que gestiona el ciclo de vida HTTP (GET/POST, headers, timeout, logging, decompression). Los finders concretos solo sobreescriben lo necesario: URL, parsing del JSON, y opcionalmente `TranslateTermAsync`. Añadir un nuevo supermercado requiere ~60 líneas sin tocar el resto del sistema.

#### 3. HttpClient estático en AbstractFinder
`private static readonly HttpClient _httpClient` compartido por todas las instancias. Evita el problema de socket exhaustion con `new HttpClient()` por request. En producción lo ideal sería `IHttpClientFactory`, pero el singleton estático es correcto y más simple para un TFC.

#### 4. AnthropicKeyRotator — gestión de rate limit con rotación de API keys

**Problema:** El plan gratuito de Anthropic impone un rate limit por API key. Durante el desarrollo y las pruebas con búsquedas frecuentes, la única key disponible se agotaba y la IA dejaba de funcionar, interrumpiendo la experiencia de usuario.

**Solución implementada:** `AnthropicKeyRotator` es un `Singleton` que gestiona un pool de hasta 4 API keys de Anthropic con rotación automática:

```csharp
// Round-robin con estado compartido entre requests
public (string Key, bool Available) GetNextKey()
{
    lock (_lock)
    {
        for (int i = 0; i < _keys.Length; i++)
        {
            var idx = (_current + i) % _keys.Length;
            if (DateTime.UtcNow >= _cooldownUntil[idx])
            {
                _current = (idx + 1) % _keys.Length;
                return (_keys[idx], true);
            }
        }
        return (string.Empty, false); // todas en cooldown
    }
}

public void MarkRateLimited(string key)
{
    lock (_lock)
    {
        var idx = Array.IndexOf(_keys, key);
        if (idx >= 0)
            _cooldownUntil[idx] = DateTime.UtcNow.AddSeconds(65);
    }
}
```

**Características:**
- **Round-robin:** itera por todas las keys, usa la primera que no esté en cooldown.
- **Cooldown por key:** 65 segundos tras recibir HTTP 429.
- **Thread-safe:** `lock(_lock)` protege el estado compartido.
- **Fallback graceful:** si todas las keys están en cooldown, devuelve los productos sin filtrar. La búsqueda nunca falla por rate limit.
- **Singleton:** el estado de cooldown persiste entre requests HTTP.
- **Configurable:** las keys se leen de `appsettings.json` (`Anthropic:ApiKey`, `ApiKey2`, `ApiKey3`, `ApiKey4`).

#### 5. CatalanTranslatorService con caché estática
`ConcurrentDictionary<string, string>` estático. Primera búsqueda: ~55 tokens (Haiku). Siguientes búsquedas del mismo término: 0 tokens, respuesta instantánea.

#### 6. Data Protection Keys en PostgreSQL
Las claves de cifrado de ASP.NET Core se persisten en la tabla `DataProtectionKeys` de PostgreSQL mediante `PersistKeysToDbContext<AppDbContext>()`. Esto garantiza que las sesiones de usuario (cookies de autenticación) sean válidas tanto en local como en producción, y que sobrevivan a reinicios y redeploys sin invalidar las sesiones existentes.

#### 7. Seed de datos con endpoint protegido
`SeedController` permite poblar la base de datos con reseñas de prueba realistas (8 usuarios ficticios, 39 productos reales de varios supermercados, 253 reseñas). El endpoint está protegido por clave secreta: `GET /Seed/Reviews?key=supercart2024`. Sin la clave devuelve "Acceso denegado".

---

## 3. Funcionalidades

### Funcionalidades implementadas ✅

#### Búsqueda multi-supermercado en tiempo real
- `Task.WhenAll` sobre todos los finders activos simultáneamente.
- Parámetro `?markets=MERCADONA&markets=ALDI` en query string para filtrar en backend.
- Filtrado por supermercado en cliente sin nueva petición al servidor.
- Debounce de 2 segundos en el input + búsqueda inmediata con Enter.

#### Pipeline de filtrado y ordenación
```
APIs supermercados (paralelo, Task.WhenAll)
        ↓
Normalización (RemoveAccents, lowercase, trim)
        ↓
Regex word-boundary filter (excluye Bonpreu)
        ↓
Sort primario (matchAtStart ↓, pureza léxica ↓, precio ↑)
        ↓
Si >8 productos → GroupByRelevanceInternal (Claude Haiku)
        ↓
Resultado final → cliente JSON
```

#### Opciones de ordenación

| Sort | Descripción |
|---|---|
| Relevancia (default) | matchAtStart + pureza léxica + precio |
| Precio ↑ / ↓ | Precio total ascendente/descendente |
| Precio/kg ↑ | Precio unitario ascendente (nulls al final) |
| Supermercado | Alfabético por nombre de mercado |
| ⭐ Mejor valorado | Media de estrellas de reviews |

#### BestByMarket
`POST /find/bestbymarket`: la IA selecciona el mejor producto (más relevante + más barato por unidad) de cada supermercado. Panel colapsable con el precio más bajo destacado. Caché en cliente: si el usuario cambia filtros sin cambiar el término, no se repite la llamada a la IA.

#### Product Panel Drawer
Panel lateral deslizante (disponible en buscador y en carrito) que muestra:
- Imagen y datos del producto.
- Media de estrellas y distribución de valoraciones con barras.
- Formulario para dejar/editar reseña (solo usuarios autenticados).
- Lista completa de reseñas con avatar, fecha y comentario.
- Se abre al hacer clic en la imagen o nombre del producto. Se cierra con ESC o clic fuera.

#### Favoritos
- Marcar/desmarcar con ❤️. Persistido en DB como snapshot del producto.
- Vista `/Favorito`: grid de favoritos + tab de "Mis reseñas".

#### Carrito de compra
- Añadir/quitar con 🛒, control de cantidad desde la vista.
- Total calculado dinámicamente en cliente sin recargar la página.
- Clic en producto abre el Product Panel Drawer con reviews.
- Vista `/Cart` con Google Maps para ruta óptima entre supermercados.

#### Reviews y valoraciones
- 1-5 estrellas + comentario opcional. Upsert: si el usuario ya tenía una reseña la actualiza.
- Identificación del producto: `Market + ProductName` (string match exacto).
- Accesibles desde el buscador (modal), desde el Product Panel Drawer y desde `/Favorito`.

#### Google Maps (ruta óptima de compra)
- Geolocalización del usuario via `navigator.geolocation`.
- Markers geocodificados por Places API New (fallback a Geocoding clásica).
- Ruta optimizada con `optimizeWaypoints: true` pasando por todos los supermercados del carrito.
- Panel con resumen (distancia total, tiempo estimado, paradas) + pasos en español.

#### Autenticación completa
- Registro (email + contraseña), Login / Logout.
- "¿Olvidé mi contraseña?" → email de recuperación con token.
- Reset de contraseña.
- Sesiones persistentes entre local y producción gracias a Data Protection Keys en DB.

#### Seed de datos de prueba
- `GET /Seed/Reviews?key=supercart2024`: inserta 8 usuarios ficticios y 253 reseñas reales sobre 39 productos de varios supermercados.
- Protegido por clave secreta. Idempotente (no duplica si se llama varias veces).

#### UI / UX
- **Tema claro/oscuro:** ☀️/🌙 persistido en `localStorage`. IIFE de inicialización antes del render para evitar flash de tema incorrecto.
- **Scrollbar oculta** en toda la app (visible pero sin barra).
- **Grid adaptativo:** `auto-fill / minmax(200px, 1fr)`.
- **Animaciones de entrada:** cards con `animation-delay` escalonado (máx 300ms).
- **Fuentes:** Syne (títulos, 800) + DM Sans (cuerpo, 300/400/500).

### Funcionalidades parcialmente implementadas ⚠️

- **Perfil de usuario**: solo muestra favoritos y reseñas propias. Sin edición de perfil ni cambio de contraseña desde la UI.
- **Mapa en Cart**: geocodifica los supermercados de los artículos del carrito, no todos los supermercados activos.

### Funcionalidades descartadas ❌

- **Carrefour**: bloqueaba peticiones incluso con TLS 1.3 + User-Agent real (Cloudflare Bot Management con fingerprint JA3).
- **Hipercor / El Corte Inglés**: bloqueado por Akamai Bot Manager.
- **BonÀrea**: devuelve HTML en lugar de JSON desde backend (requiere cookies de sesión del frontend).
- **Eroski**: datos incrustados en `<script>` HTML (frágil) + rate limiting. No integrado en producción.
- **Notificaciones de bajada de precio**: requeriría background jobs. Fuera del scope TFC.

---

## 4. Desarrollo y Evolución

### Timeline aproximado

| Fase | Contenido |
|---|---|
| 1 | Setup ASP.NET Core MVC, estructura base, primer finder (Mercadona) |
| 2 | Finders para el resto de supermercados; ingeniería inversa de APIs |
| 3 | Autenticación (Identity), SQLite, favoritos |
| 4 | Carrito de compra, reviews/valoraciones |
| 5 | Integración Claude Haiku para filtrado de relevancia; AnthropicKeyRotator |
| 6 | Google Maps: geolocalización, markers, rutas, instrucciones paso a paso |
| 7 | UI polish: tema claro/oscuro, favicon, logo, sidebar fijo |
| 8 | Mejoras al filtro IA: clasificación 3 niveles, endpoint `groupbyrelevance` |
| 9 | Traducción ES→CA para Bonpreu (`CatalanTranslatorService`) |
| 10 | Migración SQLite → PostgreSQL (Railway); Data Protection Keys en DB |
| 11 | Product Panel Drawer en buscador y carrito; Seed controller protegido |

### Problemas técnicos y soluciones

| Problema | Causa | Solución |
|---|---|---|
| Sesiones inválidas tras redeploy | Data Protection Keys en memoria (se perdían al reiniciar) | `PersistKeysToDbContext<AppDbContext>()` en PostgreSQL |
| Usuario creado en local no funcionaba en producción | Keys de cifrado distintas entre entornos | Data Protection Keys compartidas en DB |
| `dotnet ef database update` → "Host desconocido" | Connection string con endpoint interno de Railway | Usar endpoint público (`maglev.proxy.rlwy.net`) |
| Error `column "CreadaEn" cannot be cast` | Migrations SQLite incompatibles con PostgreSQL | Borrar todas las migrations y crear `InitialPostgres` limpia |
| Bonpreu devolvía 403 | Faltaba User-Agent real y param `tag=web` | Añadidos en `BonpreuFinder.AddHeaders()` |
| Bonpreu sin resultados para términos en español | Su API opera en catalán | `CatalanTranslatorService` + hook `TranslateTermAsync` |
| `openProductPanel is not defined` en consola | Función comentada con `@* ... *@` en Index.cshtml | Descomentado el bloque completo del Product Panel |

---

## 5. Seguridad y Auditoría

### Aspectos de seguridad implementados

- **CSRF protection:** `@Html.AntiForgeryToken()` en todos los formularios POST. Token extraído del DOM para peticiones AJAX.
- **Autenticación:** ASP.NET Core Identity. Rutas protegidas con `[Authorize]`.
- **Hashing de contraseñas:** PBKDF2 con salt aleatorio (Identity).
- **Data Protection Keys en DB:** sesiones cifradas con keys persistentes, no regeneradas en cada deploy.
- **Ownership de recursos:** `FavoritoController` y `CartController` verifican que el `UserId` coincide con el usuario autenticado.
- **HTML encoding:** Razor escapa automáticamente. Función `escHtml()` sanitiza strings en `innerHTML`.
- **Seed protegido por clave:** `GET /Seed/Reviews?key=supercart2024` — sin clave devuelve "Acceso denegado".

### Riesgos conocidos

- **APIs no oficiales:** pueden cambiar o bloquearse sin previo aviso.
- **Google Maps key expuesta en frontend:** restringir por referrer/dominio en Google Cloud Console.
- **Sin rate limiting propio:** un usuario malicioso puede agotar cuotas de Anthropic y Google Maps.
- **`appsettings.json` con claves:** no debe subirse a Git. En Railway usar variables de entorno.

---

## 6. DevOps / Operación

### Deploy en producción

**Railway** — https://supercart-production.up.railway.app

- App y base de datos PostgreSQL en Railway.
- Deploy manual: `git push` → Railway detecta el repo y redespliegua automáticamente.
- Las migraciones se aplican automáticamente en startup (`db.Database.Migrate()`).

### Variables de entorno en Railway

```
ConnectionStrings__DefaultConnection = Host=...;Port=...;Database=railway;Username=postgres;Password=...;SSL Mode=Require;Trust Server Certificate=true
Anthropic__ApiKey  = sk-ant-...
Anthropic__ApiKey2 = sk-ant-...
Anthropic__ApiKey3 = sk-ant-...
Anthropic__ApiKey4 = sk-ant-...
GoogleMaps__ApiKey = AIza...
```

### Ejecución en local

```bash
cd SuperCartMVC
dotnet run
# http://localhost:5175
```

La app en local también conecta a la misma PostgreSQL de Railway (misma `DefaultConnection` en `appsettings.json`), por lo que los datos son compartidos entre local y producción.

### Seed de datos de prueba

```
GET http://localhost:5175/Seed/Reviews?key=supercart2024
```

Inserta 8 usuarios ficticios y 253 reseñas sobre 39 productos reales. Idempotente.

### Migraciones

```bash
dotnet ef migrations add NombreMigracion
dotnet ef database update
```

### Logging

Sin sistema de logging estructurado. `Console.WriteLine` extensivo con prefijos por componente:

```
[BONPREU] ES→CA: 'leche' → 'llet'
[KeyRotator] Usando key #2
[KeyRotator] Key #1 en cooldown 65s por rate limit
FINDERS REGISTRADOS: 8
PRODUCTOS ANTES FILTRO: 47
PRODUCTOS DESPUES FILTRO AI: 23
```

---

## 7. Conocimiento Clave para Presentaciones

### Decisiones que justifican bien el proyecto

**"¿Por qué ASP.NET Core MVC y no React + API REST separada?"**
> MVC integra perfectamente Identity, EF Core y la generación de HTML en un único framework. Para un TFC que evalúa conocimiento full-stack en .NET, MVC es coherente con el stack del DAW y más fácil de desplegar en Railway.

**"¿Por qué Claude Haiku y no un filtro manual más complejo?"**
> Un filtro manual basado en listas de palabras nunca podría entender que "leche corporal Nivea" es irrelevante cuando se busca "leche". Haiku cuesta ~$0.00025 por llamada típica, tiene latencia ~300ms y el prompt en español da resultados excelentes para el contexto de supermercados españoles.

**"¿Por qué PostgreSQL en Railway y no SQLite?"**
> SQLite no es apto para producción en hosting cloud: el sistema de archivos es efímero y se resetea con cada deploy, perdiendo todos los datos. PostgreSQL en Railway es persistente, gratuito para el tráfico de un TFC, y compatible con EF Core cambiando solo el provider (`Npgsql`).

**"¿Por qué Data Protection Keys en DB?"**
> Sin persistir las keys, cada redeploy genera nuevas claves de cifrado y todas las cookies de sesión anteriores quedan invalidadas — los usuarios tienen que volver a hacer login. Con `PersistKeysToDbContext`, las keys se guardan en PostgreSQL y sobreviven a cualquier redeploy.

**"¿Por qué ingeniería inversa de las APIs?"**
> Ningún supermercado ofrece API pública oficial para precios. La ingeniería inversa del tráfico de sus propias apps de compra online es la única vía técnicamente viable. Es el mismo enfoque que usan herramientas comerciales como chuletometro.es.

### Partes más complejas técnicamente

1. **Pipeline de búsqueda concurrente:** `Task.WhenAll` sobre 8 finders, manejo de fallos parciales, merge y sort de resultados heterogéneos, dos niveles de filtrado (regex + IA).
2. **AnthropicKeyRotator:** estado compartido entre requests con `lock`, round-robin con cooldown de 65s, fallback graceful cuando todas las keys están en cooldown.
3. **Data Protection Keys en PostgreSQL:** garantiza sesiones válidas entre entornos y entre redeploys sin invalidar usuarios existentes.
4. **Integración Google Maps:** orquestación de tres APIs distintas, fallback entre Places API New y Geocoding clásica, ruta optimizada con `optimizeWaypoints: true`.
5. **Product Panel Drawer:** panel lateral deslizante con reviews, formulario de valoración, distribución de estrellas con barras animadas — reutilizado en buscador y carrito.

### Preguntas difíciles y respuestas recomendadas

**"¿Es legal hacer ingeniería inversa de las APIs de supermercados?"**
> Es una zona gris legal. El acceso a datos públicos (precios visibles en la web sin autenticación) está generalmente permitido para uso personal y educativo según la jurisprudencia europea. Este proyecto es un TFC no comercial.

**"¿Qué pasa si Mercadona cambia su API?"**
> Solo se modifica `MercadonaFinder.cs`: la URL en `GetMarketUri()` y el parsing en `GetProductList()`. El resto del sistema no cambia en absoluto.

**"¿Cómo escala esto con muchos usuarios concurrentes?"**
> PostgreSQL ya resuelve el problema de SQLite con escrituras concurrentes. El siguiente cuello de botella sería el rate limit de las APIs externas. Para escalar: `IMemoryCache` para búsquedas frecuentes + proxy propio para distribuir peticiones.

### Tradeoffs asumidos

| Decisión | Ventaja | Coste |
|---|---|---|
| Vistas autocontenidas (`Layout = null`) | Control total del CSS | Duplicación de header y estilos comunes |
| Vanilla JS sin framework | Sin dependencias, rápido | Más código manual, sin reactividad declarativa |
| PostgreSQL en Railway | Persistente, gratuito para TFC | Latencia de red vs SQLite local |
| HttpClient estático | Sin socket exhaustion | No usa `IHttpClientFactory` (best practice .NET) |
| APIs no oficiales | Datos reales de supermercados | Riesgo de breaking changes sin previo aviso |
| `Console.WriteLine` para logging | Simple y visible en `dotnet run` | No estructurado, difícil de filtrar en producción |

---

## 8. Puntos Abiertos

### Deuda técnica

- `Console.WriteLine` → `ILogger<T>` con niveles apropiados.
- Lógica de negocio de `FindController` → extraer a `ProductSearchService` (SRP).
- `HttpClient` estático → migrar a `IHttpClientFactory`.
- Sin tests unitarios ni de integración.
- Las reviews se identifican por `Market + ProductName` (string exacto). Si el nombre cambia ligeramente, se crean reviews duplicadas.

### Mejoras futuras

- Caché de resultados de búsqueda con `IMemoryCache` (TTL ~30 min).
- Más supermercados: Lidl, El Corte Inglés.
- Notificaciones de bajada de precio (`IHostedService` + email).
- Historial de búsquedas por usuario.
- Comparación directa entre dos productos (tabla lado a lado).
- PWA: `manifest.json` + service worker.
- Rate limiting propio para proteger las cuotas de APIs externas.
- Edición de perfil y cambio de contraseña desde la UI.

---

## 9. Resumen Ejecutivo

**SuperCart** es un comparador de precios de supermercados en tiempo real construido con **C# ASP.NET Core MVC**. Permite buscar cualquier producto de alimentación y comparar precios al instante entre **8 supermercados** (Mercadona, Aldi, Consum, DIA, Alcampo, Ametller Origen, Bonpreu y Condis), con filtrado inteligente mediante **IA (Claude Haiku de Anthropic)** para eliminar resultados irrelevantes.

**Desplegado en producción:** https://supercart-production.up.railway.app  
**Base de datos:** PostgreSQL persistente en Railway  
**Autenticación:** ASP.NET Core Identity con Data Protection Keys en DB

**Valor principal:**
- En lugar de entrar en 8 webs diferentes, el usuario obtiene todos los precios en una sola búsqueda en segundos.
- La IA filtra automáticamente productos irrelevantes y selecciona el mejor producto de cada supermercado por precio/unidad.
- Google Maps integrado permite planificar la ruta óptima al supermercado más conveniente.
- Panel lateral de detalle con reviews y valoraciones accesible desde cualquier producto.

**Diferenciadores técnicos:**
- Pipeline de IA en 3 niveles (`relevantes / dudosos / excluidos`).
- **Precio por unidad (€/kg, €/L, €/docena)** como métrica comparativa principal.
- **AnthropicKeyRotator:** pool de 4 API keys con round-robin y fallback graceful — la búsqueda nunca falla por rate limit.
- **Data Protection Keys en PostgreSQL:** sesiones válidas entre entornos y redeploys.
- **Product Panel Drawer:** panel lateral reutilizable en buscador y carrito con reviews, valoraciones y formulario integrado.
- Traducción automática ES→CA con caché inteligente para Bonpreu.
- Arquitectura extensible (Strategy + Template Method + hook virtual) que permite añadir nuevos supermercados sin modificar el código existente.
- **Optimistic UI** en favoritos y carrito + verificación en batch (1 request para N productos).

**Stack:** C# ASP.NET Core MVC · EF Core · PostgreSQL (Npgsql) · ASP.NET Identity · Claude Haiku · Google Maps API · Vanilla JS · Railway

---