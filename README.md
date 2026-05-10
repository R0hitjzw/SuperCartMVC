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

El consumidor que quiere comprar "leche entera" no tiene forma sencilla de saber si le conviene ir a Mercadona, Aldi o Consum sin entrar en cada web por separado. SuperCart centraliza esa búsqueda en una sola interfaz, con filtrado inteligente para eliminar resultados irrelevantes (ej: "leche corporal Nivea" cuando se busca "leche", o "carne de ñora" cuando se busca "carne" ).

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

**En desarrollo activo. Funcionalidades core completas y funcionando:**

- Búsqueda multi-supermercado en tiempo real ✅
- Filtrado por relevancia con IA (3 niveles) ✅
- Sistema de usuarios (registro, login, recuperación de contraseña) ✅
- Favoritos y carrito de compra persistidos en DB ✅
- Reviews y valoraciones (1-5 estrellas) ✅
- Google Maps con ruta al supermercado ✅
- Tema claro/oscuro con persistencia en localStorage ✅
- Traducción automática ES→CA para Bonpreu ✅
- Sort combinado "Relevancia + €/kg ↑" ✅

**Pendiente:** despliegue en producción (myASP.net), migración SQLite → MSSQL, push a GitHub.

---

## 2. Arquitectura y Tecnología

### Stack tecnológico

| Capa | Tecnología |
|------|-----------|
| Backend | C# ASP.NET Core MVC (.NET 8) |
| Frontend | Razor Views (.cshtml), Vanilla JS (ES2022+), CSS custom properties |
| Base de datos | SQLite (desarrollo) / MSSQL previsto para producción |
| ORM | Entity Framework Core |
| Autenticación | ASP.NET Core Identity |
| IA / LLM | Anthropic Claude Haiku (`claude-haiku-4-5-20251001`) |
| Mapas | Google Maps JS API, Places API (New), Geocoding API, Directions API |
| HTTP client | `System.Net.Http.HttpClient` (singleton estático en `AbstractFinder`) |

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

- **Desarrollo:** `dotnet run` en local, SQLite como base de datos (archivo `supercart.db` generado automáticamente).
- **Producción prevista:** myASP.net hosting, MSSQL Server.
- Sin contenedores ni CI/CD implementado actualmente.

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
└───────┬───────────────────┬─────────────────────────────────┘
        │ EF Core           │ DI (IEnumerable<IFinder>)
        ▼                   ▼
┌───────────────┐  ┌────────────────────────────────────────┐
│  AppDbContext │  │  AbstractFinder (base)                 │
│  (SQLite)     │  │    ├─ MercadonaFinder                  │
│               │  │    ├─ AldiFinder                       │
│  Favoritos    │  │    ├─ ConsumFinder                     │
│  Reviews      │  │    ├─ AlcampoFinder                    │
│  CartItems    │  │    ├─ AmetllerFinder                   │
│  Identity     │  │    ├─ DiaFinder                        │
│  (usuarios)   │  │    ├─ CondisFinder                     │
└───────────────┘  │    └─ BonpreuFinder                    │
                   │         └─ CatalanTranslatorService     │
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

**Solución implementada:** `AnthropicKeyRotator` es un `Singleton` (registrado en `Program.cs`) que gestiona un pool de hasta 4 API keys de Anthropic con rotación automática:

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

// Cuando la API responde HTTP 429, se marca la key usada
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
- **Cooldown por key:** 65 segundos tras recibir HTTP 429. El tiempo lo marca la propia API de Anthropic en sus headers de error.
- **Thread-safe:** `lock(_lock)` protege el estado compartido `_current` y `_cooldownUntil[]`.
- **Fallback graceful:** si `GetNextKey()` devuelve `Available = false`, los métodos que llaman a la IA hacen early return y devuelven los productos sin filtrar. La búsqueda nunca falla por rate limit — simplemente no aplica el filtro IA en ese momento.
- **Singleton:** el estado de cooldown persiste entre requests HTTP, lo que es esencial para que la rotación sea efectiva.
- **Configurable:** las keys se leen de `appsettings.json` (`Anthropic:ApiKey`, `ApiKey2`, `ApiKey3`, `ApiKey4`). Si solo hay una key configurada, el rotador funciona igualmente con pool de 1.

#### 5. CatalanTranslatorService con caché estática
`ConcurrentDictionary<string, string>` estático. Primera búsqueda: ~55 tokens (Haiku). Siguientes búsquedas del mismo término: 0 tokens, respuesta instantánea. Sin esta traducción, buscar "leche" en Bonpreu devuelve 0 resultados útiles porque su API opera en catalán.

#### 6. Regex filter + IA en pipeline secuencial
Antes de llamar a la IA se aplica un regex con límites de palabra para eliminar resultados claramente irrelevantes. La IA solo recibe productos que ya pasaron el filtro básico, lo que reduce tokens y mejora precisión. Bonpreu está exento del regex (sus nombres están en catalán).

#### 7. Clasificación IA en 3 niveles
El endpoint `/find/groupbyrelevance` retorna `{relevantes, dudosos, excluidos}` en lugar de un array plano. Permite el sort "Relevancia + €/kg ↑": relevantes ordenados por precio/kg, luego dudosos, luego excluidos. Más útil que un array plano de índices.

#### 8. Hook virtual `TranslateTermAsync` en AbstractFinder
Permite que `BonpreuFinder` traduzca el término ES→CA sin modificar la interfaz `IFinder` ni el `FindController`. Cumple el principio Open/Closed: el sistema estaba cerrado a modificación y abierto a extensión.

### Patrones utilizados

- **Strategy:** `IFinder` — cada supermercado es una estrategia intercambiable de búsqueda.
- **Template Method:** `AbstractFinder` define el flujo HTTP; los finders implementan los pasos variables (`GetMarketUri`, `GetProductList`, `TranslateTermAsync`).
- **Singleton:** `AnthropicKeyRotator`, `CatalanTranslatorService`.
- **Dependency Injection:** todos los servicios registrados en `Program.cs`.
- **Optimistic UI:** los botones de ❤️ (favorito) y 🛒 (carrito) actualizan el icono visualmente de forma inmediata, antes de que el servidor confirme la operación. Si el servidor falla, el estado visual se revierte.
- **Batch verification:** al cargar los resultados de búsqueda, los favoritos y el carrito se verifican en una sola petición POST con todos los productos, en lugar de N peticiones individuales. Reduce las requests de N a 1.
- **Client-side cache:** el resultado de `/find/bestbymarket` (llamada a IA) se cachea en variables JS (`lastBestByMarketTerm`, `lastBestByMarketAll`). Si el usuario cambia filtros de mercado sin cambiar el término, no se repite la llamada a la IA.
- **Repository (implícito):** `AppDbContext` con `DbSet<T>`.
- **DTO:** `Product`, `Market`, `BestByMarketRequest` separan transporte de dominio.

---

## 3. Funcionalidades

### Funcionalidades implementadas ✅

#### Búsqueda multi-supermercado en tiempo real
- `Task.WhenAll` sobre todos los finders activos simultáneamente.
- Parámetro `?markets=MERCADONA&markets=ALDI` en query string para filtrar en backend.
- Sidebar de filtrado por supermercado: filtra en cliente sin nueva petición al servidor.

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

#### Opciones de ordenación (frontend, sin nueva petición al servidor salvo las marcadas)

| Sort | Descripción |
|---|---|
| Relevancia (default) | matchAtStart + pureza léxica + precio |
| Precio ↑ / ↓ | Precio total ascendente/descendente |
| Precio/kg ↑ | Precio unitario ascendente (nulls al final) |
| 🎯 Relevancia + €/kg ↑ | POST `/find/groupbyrelevance` → sort por €/kg dentro de cada grupo |
| Supermercado | Alfabético por nombre de mercado |
| ⭐ Mejor valorado | Media de estrellas de reviews (GET `/Review/GetProductReviews`) |

#### BestByMarket
`POST /find/bestbymarket`: la IA selecciona el mejor producto (más relevante + más barato por unidad) de cada supermercado. Presentado en panel colapsable con el precio más bajo destacado.

#### Favoritos
- Marcar/desmarcar con ❤️. Persistido en DB como snapshot del producto.
- `Favorito`: `{Id, UserId, Market, Name, Price, Image, PriceUnitOrKg, AddedAt}`.
- Vista `/Favorito`: grid de favoritos + tab de "Mis reseñas" (carga por AJAX).

#### Carrito de compra
- Añadir/quitar con 🛒, control de cantidad desde la vista.
- `CartItem`: `{Id, UserId, Market, Name, Price, Image, PriceUnitOrKg, Quantity, AddedAt}`.
- Total calculado en cliente. Vista `/Cart` con Google Maps para ruta.

#### Reviews y valoraciones
- 1-5 estrellas + comentario opcional. Upsert: si el usuario ya tenía una reseña del mismo producto la actualiza.
- Identificación del producto: `Market + ProductName` (string match exacto).
- `Review`: `{Id, UserId, Market, ProductName, ProductImage, Estrellas, Comentario, CreadaEn}`.

#### Google Maps (ruta al supermercado)
- Botón "Ver en mapa" en Home y Cart.
- Geolocalización del usuario via `navigator.geolocation`.
- Markers de supermercados geocodificados por Places API New (fallback a Geocoding clásica).
- Click en marker → ruta en coche con Directions API.
- Panel con resumen (distancia, tiempo) + pasos en español (`language=es&region=ES`).
- Scrollbar oculta (no visible) pero funcional en el panel de pasos.

#### Autenticación completa
- Registro (email + contraseña).
- Login / Logout.
- "¿Olvidé mi contraseña?" → email de recuperación con token.
- Reset de contraseña.
- Nombre de usuario mostrado sin dominio del email.

#### UI / UX
- **Tema claro/oscuro:** ☀️/🌙 en header de todas las páginas. `localStorage` clave `sc-theme`. IIFE de inicialización antes del render para evitar flash de tema incorrecto.
- **Sidebar fijo:** `position: fixed; top: 73px; overflow: hidden` — no se desplaza con el scroll.
- **Favicon:** SVG + ICO (logo: carrito naranja + badge teal "€" + ruedas teal).
- **Grid adaptativo:** `auto-fill / minmax(200px, 1fr)`.
- **Animaciones de entrada:** cards con `animation-delay` escalonado (máx 300ms).
- **Fuentes:** Syne (títulos, 800) + DM Sans (cuerpo, 300/400/500).

#### Traducción automática ES→CA para Bonpreu
- `CatalanTranslatorService` singleton con caché `ConcurrentDictionary<string,string>`.
- `BonpreuFinder` sobreescribe `TranslateTermAsync()`.
- Bonpreu exento del regex filter en `FindController`.
- Log en consola: `[BONPREU] ES→CA: 'leche' → 'llet'`.

### Funcionalidades parcialmente implementadas ⚠️

- **Sort "Relevancia + €/kg ↑"**: funcional, pero si Haiku devuelve JSON con espacios en las keys (`" dudosos"` en lugar de `"dudosos"`), el parsing falla silenciosamente y hace fallback a sort solo por €/kg.
- **Mapa en Cart**: funcional, pero geocodifica los supermercados de los artículos del carrito, no todos los supermercados activos.
- **Perfil de usuario**: solo muestra favoritos y reseñas propias. Sin edición de perfil ni cambio de contraseña desde la UI.

### Funcionalidades descartadas ❌

- **Carrefour**: bloqueaba peticiones incluso con TLS 1.3 + User-Agent real. Inviable técnicamente.
- **Notificaciones de bajada de precio**: requeriría background jobs + DB de seguimiento. Fuera del scope TFC.
- **App móvil**: fuera del scope DAW.

Los siguientes supermercados fueron investigados y descartados por protecciones activas en sus infraestructuras:

#### 🚫 Carrefour
Se identificó el endpoint REST `/search-api/query/v1/search`, que devuelve datos correctamente desde el navegador. Todas las peticiones desde el backend reciben **HTTP 403** por **Cloudflare Bot Management**, que detecta el fingerprint TLS mediante el hash **JA3** (método de Salesforce 2017: hash MD5 basado en los parámetros del paquete `ClientHello` — versión TLS, conjuntos de cifrado, extensiones). Se descartó Playwright/Puppeteer por añadir complejidad desproporcionada al scope del TFC.

#### 🚫 Hipercor / El Corte Inglés
Se exploraron tres endpoints: `/alimentacion/api/catalog/supermercado/type_ahead/`, `/supermercado/buscar/` y `/api/food-firefly/typeahead_related_terms/`. Los dos primeros devuelven **HTTP 403** desde backend; el tercero solo devuelve sugerencias de texto sin precios. Bloqueado por **Akamai Bot Manager**, que rechaza peticiones analizando el fingerprint TLS, la ausencia de cookies de sesión dinámicas (`ak_bmsc`, `bm_sz`, `bm_sv`) generadas por JavaScript en el navegador real, y la ausencia de comportamiento humano (eventos DOM, movimiento de ratón).

#### 🚫 BonÀrea
Se identificó el endpoint POST `/ca/shop/find` de `bonarea-online.com`, que devuelve JSON correctamente desde el navegador. Todas las peticiones desde el backend reciben **HTML en lugar de JSON**, independientemente de los headers enviados. El servidor requiere cookies de sesión generadas por el flujo del frontend. Descartado por la complejidad de replicar ese flujo.

#### ⚠️ Eroski / Caprabo (investigado, parcialmente implementado, no integrado)
Eroski y Caprabo comparten infraestructura (mismo grupo empresarial). **Eroski** no expone API REST pública: los datos de producto están incrustados en un bloque `<script>` del HTML bajo la clave `impressions` en formato JSON escapado. Se implementó un `PreProcessResponse` personalizado que extrae y deserializa ese fragmento, pero el enfoque es frágil: cualquier cambio en la estructura del HTML rompe la extracción. Aplica además rate limiting. **Caprabo** genera tokens de sesión dinámicos (`dwsid`, `csrf_token`) via JavaScript antes de aceptar peticiones, lo que hace inviable el acceso desde backend. El finder de Eroski quedó operativo pero frágil y no se integró en producción.

### Flujo funcional completo

```
Usuario → escribe "pollo" → click Buscar
    → GET /find?term=pollo
        → FindController.FindByTerm()
            → BonpreuFinder.TranslateTermAsync("pollo")
                → CatalanTranslatorService.TranslateToCAAsync("pollo")
                    → caché miss → Haiku → "pollastre" → guardado en caché
            → Task.WhenAll(8 finders, cada uno con su término)
                → Mercadona/Aldi/.../Bonpreu("pollastre") → List<Product>
            → Merge (SelectMany) → normalización
            → Regex filter (skip Bonpreu)
            → Sort (matchAtStart, pureza, precio)
            → Si >8: GroupByRelevanceInternal(Haiku)
                → {"relevantes":[0,1,3],"dudosos":[2,4],"excluidos":[5,6]}
                → return relevantes + dudosos
        → return List<Product> (JSON)
    → JS: allProducts = [...]; renderProducts()
        → markFavoritos() [GET /Favorito/GetFavoritos]
        → markCart() [GET /Cart/GetCartItems]
        → renderBestByMarket() → POST /find/bestbymarket [async]

Usuario → click 🎯 Relevancia + €/kg ↑
    → loadRelevanceAndSort()
        → POST /find/groupbyrelevance {term, products: allProducts}
        → allProducts = byUnitPrice(relevantes) + byUnitPrice(dudosos) + byUnitPrice(excluidos)
        → renderProducts()

Usuario → click ❤️
    → POST /Favorito/Toggle {market, name, price, image, priceUnitOrKg}
    → Upsert en DB → JSON {isFav: true/false} → actualiza icono

Usuario → click Ver en mapa
    → navigator.geolocation.getCurrentPosition()
    → reverseGeocodeCity(lat, lng) → "Barcelona"
    → paintStoreMarkers(): para cada supermercado activo
        → Places API: searchByText("Mercadona Barcelona") → {location, address}
        → Marker en mapa con número
    → click marker → routeToStore()
        → Directions API: origin=usuario, destination=marker
        → renderSingleRouteInfo(): lista de pasos en español
```

### Edge cases importantes

- **Todas las keys de Anthropic en cooldown:** `GetNextKey()` devuelve `(string.Empty, false)` → los métodos IA hacen early return con los datos sin filtrar. La búsqueda **nunca falla** por esto.
- **API de supermercado caída:** `try/catch` en `AbstractFinder` devuelve `List<Product>` vacía. El resto siguen funcionando.
- **Bonpreu 403:** si falta `User-Agent` o `tag=web`, la API devuelve 403. Ambos hardcodeados en `BonpreuFinder.AddHeaders()`.
- **HTML en instrucciones de Maps:** Google Maps embute `<div>` con horarios dentro del texto. Fix: `.replace(/<[^>]+>/g,' ').replace(/\s+/g,' ').trim()` (con espacio, no vacío).
- **Imágenes rotas:** `onerror="this.style.display='none'"` en cada `<img>`. Placeholder emoji 🛒 como fallback.
- **Sin geolocalización:** si el usuario deniega permisos, el mapa no se inicializa y se muestra mensaje de error.
- **padding-bottom ignorado en Chrome/Safari:** bug WebKit en contenedores `overflow-y: auto`. Fix: `padding-bottom: 0` en el contenedor + `padding-bottom: 1.25rem` en el `:last-child` via CSS.

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
| 7 | UI polish: tema claro/oscuro, favicon, logo (opción D), sidebar fijo |
| 8 | Mejoras al filtro IA: clasificación 3 niveles, endpoint `groupbyrelevance` |
| 8b | Sort "Relevancia + €/kg ↑" en frontend |
| 9 | Traducción ES→CA para Bonpreu (`CatalanTranslatorService` + hook `TranslateTermAsync`) |

### Cambios importantes realizados

**Filtro de relevancia v1 → v2:**
- v1: array plano `[3,0,5,1]`, sin role `system`, solo `índice:nombre` enviado a la IA.
- v2: JSON agrupado `{"relevantes":[], "dudosos":[], "excluidos":[]}`, role `system` definido, datos incluyen supermercado + precio + precio/kg + ejemplos concretos en el prompt (café, leche, huevos, pollo).

**Sidebar `position: sticky` → `position: fixed`:**
`sticky; top: 0` causaba movimiento de ~73px (la altura del header) antes de fijarse. Cambiado a `fixed; top: 73px; height: calc(100vh - 73px)` + `padding-left: 230px` en `.page-layout`.

**Hook `TranslateTermAsync` en AbstractFinder:**
Añadido como `protected virtual Task<string> TranslateTermAsync(string term) => Task.FromResult(term)`. No modifica ningún finder existente. `BonpreuFinder` sobreescribe solo este método. Principio Open/Closed aplicado correctamente.

### Problemas técnicos y soluciones

| Problema | Causa | Solución |
|---|---|---|
| `Index.cshtml` se truncaba al editarlo | Archivo CRLF ~85KB supera límite del Edit tool | Todas las ediciones al archivo via Python binario (`rb`/`wb`) |
| Error RZ1034 "malformed body tag helper" | `</body>` desaparecía tras truncación | Python append del tail conocido |
| Instrucciones de Maps en inglés | Faltaba `language=es&region=ES` en URL del script | Añadido al tag `<script>` de Google Maps |
| Instrucciones de Maps concatenadas sin espacio | `.replace(/<[^>]+>/g,'')` sin separador | `.replace(/<[^>]+>/g,' ')` con espacio |
| Panel de ruta no scrolleaba / cortaba contenido | `flex-shrink:0` + `max-height` fijo excedía 90vh | `flex: 1` + `min-height: 60px` |
| `padding-bottom` ignorado en Chrome/Safari | Bug WebKit conocido | Padding en `:last-child` en lugar del contenedor |
| Bonpreu devolvía 403 | Faltaba User-Agent real y param `tag=web` | Añadidos en `BonpreuFinder.AddHeaders()` |
| Bonpreu sin resultados para términos en español | Su API opera en catalán | `CatalanTranslatorService` + hook `TranslateTermAsync` |
| Resultados de Bonpreu eliminados por regex | Nombres en catalán no matchean término en español | `if (p.Market == Market.BONPREU) return true` en el filter |
| Tema claro/oscuro desaparecía tras restauraciones | `toggleTheme()` e IIFE no estaban en el tail restaurado | Colocados al inicio del bloque `<script>`, antes de `const API = ''` |

---

## 5. Seguridad y Auditoría

### Aspectos de seguridad implementados

- **CSRF protection (formularios y AJAX):** `@Html.AntiForgeryToken()` en todos los formularios POST. Para las peticiones AJAX (`fetch` desde JS), el token se extrae del DOM (`document.querySelector('input[name="__RequestVerificationToken"]').value`) y se envía en el header `RequestVerificationToken`. Validado automáticamente por ASP.NET Core en el servidor con `[ValidateAntiForgeryToken]`.
- **Autenticación:** ASP.NET Core Identity. Rutas protegidas con `[Authorize]`.
- **Hashing de contraseñas:** Identity usa PBKDF2 con salt aleatorio.
- **Ownership de recursos:** `FavoritoController` y `CartController` verifican que el `UserId` del recurso coincide con el usuario autenticado antes de modificar o eliminar.
- **HTML encoding:** Razor escapa automáticamente. En JS, función `escHtml()` sanitiza strings insertados en `innerHTML`.

### Validaciones implementadas

- Contraseña: mínimo 6 caracteres, al menos 1 dígito, sin requisito de carácter especial (`RequireNonAlphanumeric = false`).
- Email: validado por Identity (formato + unicidad en DB).
- Estrellas en reviews: 1-5 (validado en modelo).
- `[ApiController]` en `FindController`: model binding valida automáticamente los parámetros de entrada.

### Riesgos conocidos

- **Rate limiting de APIs de supermercados:** sin protección frente a bloqueos por exceso de peticiones. Con tráfico alto, los supermercados podrían bloquear la IP del servidor.
- **APIs no oficiales:** obtenidas por ingeniería inversa. Pueden cambiar o bloquearse sin previo aviso y romper los finders afectados.
- **API keys en `appsettings.json`:** las claves de Anthropic y Google Maps están en el fichero de configuración. **No deben subirse a Git.** En producción, usar variables de entorno o un gestor de secretos.
- **Google Maps key expuesta en frontend:** visible en el HTML renderizado. Restringir por referrer/dominio en Google Cloud Console.
- **SQLite en producción:** no apto para escrituras concurrentes. Migración a MSSQL prevista.
- **Sin rate limiting propio:** un usuario malicioso puede agotar cuotas de Anthropic y Google Maps.

### Consideraciones para auditoría técnica

- Los `Console.WriteLine` extensivos son intencionales: sirven como logging de desarrollo para seguir el flujo en `dotnet run`. En producción reemplazar por `ILogger<T>`.
- Los `try/catch` están presentes en todos los puntos de I/O. **Nunca se propaga una excepción no gestionada al usuario final.**
- El código de los finders está comentado en castellano/catalán explicando el razonamiento.
- La lógica de negocio está en `FindController` (no en una capa de servicio separada). Aceptable para un TFC; en producción se separaría en `ProductSearchService`.

---

## 6. DevOps / Operación

### Deploy actual

```bash
cd SuperCartMVC
dotnet run
# http://localhost:5XXX  /  https://localhost:7XXX
```

La base de datos SQLite se crea automáticamente en `supercart.db` al arrancar (EF Core Migrations aplicadas en startup si se configura así, o manualmente con `dotnet ef database update`).

### Variables de entorno / Configuración

Toda la configuración está en `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=SuperCartDB;Trusted_Connection=True;"
  },
  "Anthropic": {
    "ApiKey":  "sk-ant-...",
    "ApiKey2": "sk-ant-...",
    "ApiKey3": "sk-ant-...",
    "ApiKey4": "sk-ant-..."
  },
  "GoogleMaps": {
    "ApiKey": "AIza..."
  }
}
```

> ⚠️ `appsettings.json` con claves reales **NO debe subirse a Git**. Añadir al `.gitignore`.

### Logging / Monitorización

Sin sistema de logging estructurado. `Console.WriteLine` extensivo con prefijos por componente:

```
[BONPREU] ES→CA: 'leche' → 'llet'
[KeyRotator] Usando key #2
[KeyRotator] Key #1 en cooldown 65s por rate limit
[CatalanTranslator] Caché: 'leche' → 'llet'
FINDERS REGISTRADOS: 8
PETICION RECIBIDA: pollo
PRODUCTOS ANTES FILTRO: 47
PRODUCTOS DESPUES FILTRO REGEX: 23
AI GROUP RESPONSE: {"relevantes":[0,1],"dudosos":[2],"excluidos":[3]}
PRODUCTOS DESPUES FILTRO AI: 3
```

### Migraciones de base de datos

```bash
dotnet ef migrations add NombreMigracion
dotnet ef database update
```

Para migrar a MSSQL: instalar `Microsoft.EntityFrameworkCore.SqlServer`, cambiar `UseSqlite` por `UseSqlServer` en `Program.cs`, actualizar connection string.

### CI/CD

No implementado. Workflow actual: desarrollo local → `dotnet build` → `dotnet publish -c Release` → subida manual a myASP.net.

---

## 7. Conocimiento Clave para Presentaciones

### Decisiones que justifican bien el proyecto

**"¿Por qué ASP.NET Core MVC y no React + API REST separada?"**
> MVC integra perfectamente Identity, EF Core y la generación de HTML en un único framework. Para un TFC que evalúa conocimiento full-stack en .NET, MVC es coherente con el stack del DAW y más fácil de desplegar en myASP.net.

**"¿Por qué Claude Haiku y no un filtro manual más complejo?"**
> Un filtro manual basado en listas de palabras o categorías nunca podría entender que "leche corporal Nivea" es irrelevante cuando se busca "leche". Haiku cuesta ~$0.00025 por llamada típica, tiene latencia ~300ms y el prompt en español da resultados excelentes para el contexto de supermercados españoles. El coste por búsqueda es despreciable.

**"¿Por qué SQLite y no PostgreSQL desde el principio?"**
> SQLite elimina dependencias de infraestructura en desarrollo: no necesita servidor, se configura en una línea, el archivo `.db` es portable. La migración a MSSQL (hosting de myASP.net) es trivial: cambiar el provider de EF Core y el connection string. EF Core abstrae completamente el motor de base de datos.

**"¿Por qué ingeniería inversa de las APIs?"**
> Ningún supermercado ofrece API pública oficial para precios. La ingeniería inversa del tráfico de sus propias apps de compra online es la única vía técnicamente viable. Es el mismo enfoque que usan herramientas comerciales como chuletometro.es o SuperMercato.

### Partes más complejas técnicamente

1. **Pipeline de búsqueda concurrente:** `Task.WhenAll` sobre 8 finders, manejo de fallos parciales, merge y sort de resultados heterogéneos, dos niveles de filtrado (regex + IA).

2. **AnthropicKeyRotator:** estado compartido entre requests con `lock`, round-robin con cooldown de 65s, fallback graceful cuando todas las keys están en cooldown.

3. **Hook `TranslateTermAsync`:** diseño extensible (Open/Closed) que añade traducción a Bonpreu sin modificar `IFinder`, `AbstractFinder` ni `FindController`.

4. **Integración Google Maps:** orquestación de tres APIs distintas, fallback entre Places API New y Geocoding clásica, instrucciones en español con limpieza de HTML incrustado.

5. **Clasificación IA 3 niveles:** prompt engineering con ejemplos concretos, parsing robusto del JSON (limpieza de markdown si la IA lo añade), fallback completo si la clasificación falla.

### Preguntas difíciles y respuestas recomendadas

**"¿Es legal hacer scraping/ingeniería inversa de las APIs de supermercados?"**
> Es una zona gris legal. El acceso a datos públicos (precios visibles en la web sin autenticación) está generalmente permitido para uso personal y educativo según la jurisprudencia europea (caso Ryanair v. PR Aviation). Este proyecto es un TFC no comercial. En un contexto comercial habría que revisar los TOS de cada cadena.

**"¿Qué pasa si Mercadona cambia su API?"**
> Solo se modifica `MercadonaFinder.cs`: la URL en `GetMarketUri()` y el parsing en `GetProductList()`. El resto del sistema (controller, otros finders, base de datos) no cambia en absoluto. La arquitectura fue diseñada exactamente para esto.

**"¿Por qué no cacheas los resultados de búsqueda?"**
> Los precios de supermercados cambian diariamente (promociones, cambios de tarifa). Una caché de más de ~30 minutos mostraría datos potencialmente obsoletos. Se podría implementar con `IMemoryCache` como mejora futura, pero para el TFC la frescura de los datos es prioritaria.

**"¿Cómo escala esto con muchos usuarios concurrentes?"**
> El cuello de botella sería SQLite (no apto para escrituras concurrentes) y el rate limit de las APIs externas. Para escalar: MSSQL + connection pooling, `IMemoryCache` para búsquedas frecuentes, y posiblemente un proxy propio para distribuir peticiones a los supermercados.

**"¿Por qué `position: fixed` en el sidebar y no `sticky`?"**
> `position: sticky; top: 0` hace que el elemento se desplace los píxeles del header (~73px) antes de fijarse en el viewport, causando un movimiento visible. Con `position: fixed; top: 73px` el sidebar está siempre en la misma posición desde el primer píxel de scroll. El `padding-left: 230px` en `.page-layout` compensa el espacio que el sidebar fijo sustrae al flujo normal del documento.

**"¿Por qué el rol `system` en las llamadas a Haiku?"**
> Sin `system`, Haiku responde en modo conversacional y puede añadir explicaciones o markdown antes/después del JSON. Con `system: "Tu respuesta es SIEMPRE JSON puro"`, la respuesta es más consistente y el parsing más robusto.

### Tradeoffs asumidos

| Decisión | Ventaja | Coste |
|---|---|---|
| Vistas autocontenidas (`Layout = null`) | Control total del CSS | Duplicación de header y estilos comunes |
| Vanilla JS sin framework | Sin dependencias, rápido | Más código manual, sin reactividad declarativa |
| SQLite en desarrollo | Zero config, portable | No apto para producción concurrente |
| HttpClient estático | Sin socket exhaustion | No usa `IHttpClientFactory` (best practice .NET) |
| APIs no oficiales | Datos reales de supermercados | Riesgo de breaking changes sin previo aviso |
| `Console.WriteLine` para logging | Simple y visible en `dotnet run` | No estructurado, difícil de filtrar en producción |
| Lógica en Controller | Más simple para TFC | Viola SRP; en producción iría en un servicio |

---

## 8. Puntos Abiertos

### TODOs importantes

- [ ] **Push a GitHub**: el repositorio no está subido. Crítico antes de la entrega.
- [ ] **Migración SQLite → MSSQL**: `Microsoft.EntityFrameworkCore.SqlServer` + cambiar `UseSqlite` por `UseSqlServer` en `Program.cs` + actualizar connection string.
- [ ] **Despliegue en myASP.net**: `dotnet publish -c Release` + subida al hosting.
- [ ] **Mover API keys a variables de entorno**: no deben estar en el repo.

### Deuda técnica

- `Console.WriteLine` → `ILogger<T>` con niveles apropiados.
- Lógica de negocio de `FindController` → extraer a `ProductSearchService` (SRP).
- `HttpClient` estático → migrar a `IHttpClientFactory`.
- Sin tests unitarios ni de integración para los finders.
- Bug potencial en prompt: las keys del JSON de ejemplo tienen un espacio delante (`" dudosos"`). Si Haiku replica el espacio, el parsing falla silenciosamente y hace fallback a sin clasificar.
- Las reviews se identifican por `Market + ProductName` (string exacto). Si el nombre del producto cambia ligeramente entre búsquedas, se crean reviews duplicadas.

### Mejoras futuras

- Caché de resultados de búsqueda con `IMemoryCache` (TTL ~30 min).
- Más supermercados: Lidl, El Corte Inglés.
- Notificaciones de bajada de precio (`IHostedService` + email).
- Historial de búsquedas por usuario.
- Comparación directa entre dos productos.
- PWA: `manifest.json` + service worker.
- Tests de integración para los finders principales.
- Rate limiting propio para proteger las cuotas de APIs externas.

### Riesgos pendientes

- Las APIs de supermercados son no oficiales y pueden cambiar o bloquearse en cualquier momento.
- La clave de Google Maps expuesta en el frontend puede ser usada por terceros si no se restringe por referrer en Google Cloud Console.
- El rate limit de Anthropic en el plan gratuito puede saturarse con tráfico real (el `AnthropicKeyRotator` con 4 keys mitiga esto pero no lo elimina).

---

## 9. Resumen Ejecutivo

**SuperCart** es un comparador de precios de supermercados en tiempo real construido con **C# ASP.NET Core MVC**. Permite buscar cualquier producto de alimentación y comparar precios al instante entre **8 supermercados** (Mercadona, Aldi, Consum, DIA, Alcampo, Ametller Origen, Bonpreu y Condis), con filtrado inteligente mediante **IA (Claude Haiku de Anthropic)** para eliminar resultados irrelevantes.

**Valor principal:**
- En lugar de entrar en 8 webs diferentes, el usuario obtiene todos los precios en una sola búsqueda en segundos.
- La IA filtra automáticamente productos irrelevantes (ej: "leche corporal" cuando se busca "leche") y selecciona el mejor producto de cada supermercado por precio/unidad.
- Google Maps integrado permite planificar la ruta al supermercado más conveniente directamente desde la app.

**Diferenciadores técnicos:**
- Pipeline de IA en 3 niveles (`relevantes / dudosos / excluidos`) con sort combinado "Relevancia + €/kg ↑".
- **Precio por unidad (€/kg, €/L, €/docena) como métrica comparativa principal** — más honesta que el precio total para productos con gramajes distintos.
- **IA BestByMarket:** selecciona no solo el más barato sino el más relevante al término buscado en cada supermercado.
- Traducción automática ES→CA con caché inteligente para compatibilidad con Bonpreu (0 tokens en búsquedas repetidas).
- **AnthropicKeyRotator:** pool de 4 API keys con round-robin, cooldown automático de 65s por key y fallback graceful — la búsqueda nunca falla por rate limit.
- Arquitectura extensible (Strategy + Template Method + hook virtual) que permite añadir nuevos supermercados sin modificar el código existente.
- **Optimistic UI** en favoritos y carrito + verificación en batch (1 request para N productos).

**Stack:** C# ASP.NET Core MVC · EF Core · SQLite · ASP.NET Identity · Claude Haiku · Google Maps API · Vanilla JS

---

*Última actual