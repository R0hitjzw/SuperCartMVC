**ARRANCAR PROJECTE MAVEN(Java)**
.\mvnw.cmd spring-boot:run

Java -> C#

JDK -> .NET SDK

Maven -> MSBuild (build) + NuGet (paquetes)

pom.xml -> .csproj

Spring Boot -> ASP.NET Core

**cmd**
dotnet new webapi -n BackendSuper --use-controllers

**EQUIVALENT DE pom.xml -> NuGet**
dotnet add package Microsoft.EntityFrameworkCore
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer

**Tutorial: Create a controller-based web API with ASP.NET Core**
https://learn.microsoft.com/en-us/aspnet/core/tutorials/first-web-api?view=aspnetcore-6.0&tabs=visual-studio

**Spring Boot vs .NET Core: Complete Developer Migration Guide**
https://medium.com/@umesh382.kushwaha/spring-boot-to-net-core-vice-versa-a-fast-track-guide-for-developers-f04693d93718
https://niotechone.com/blog/dotnet-core-vs-java-spring-boot-which-framework-wins-2025/

**Step by Step Tutorial - C# REST Client**
https://www.youtube.com/watch?v=11f5KzVNQ90&list=PLtZrdHx5xBeMYGIilAIeaY7MMbE8u5NWH


**INSTALAR DEPENDENCIES EQUIVALENTS**
dotnet add package System.Text.Json          # reemplaza Gson (ya incluido en .NET)



**ESTRUCTURA DE CARPETES A CREAR**
SuperfinderBackend/
├── Controllers/
│   └── FindController.cs
├── DTOs/
│   ├── Product.cs
│   └── Market.cs
├── Services/
│   └── Finders/
│       ├── IFinder.cs
│       ├── AbstractFinder.cs  -----> AIXÓ CONTÉ LA LÓGICA PER FER PETICIONS ALS Impl dels Supermercats.
│       └── Impl/
│           ├── MercadonaFinder.cs
│           ├── CarrefourFinder.cs
│           ├── AlcampoFinder.cs
│           └── ... (uno por supermercado)
├── appsettings.json
└── Program.cs

------------------------

**FindController.cs** CODE LOGIC 

[ApiController] 
https://learn.microsoft.com/es-es/dotnet/api/system.web.http.apicontroller?view=aspnetcore-2.2
// Define las propiedades y los métodos del controlador API.

[Route("find")]
https://learn.microsoft.com/es-es/dotnet/api/system.web.routing.route?view=netframework-4.8.1
// Proporciona propiedades y métodos para definir una ruta y para obtener información sobre la ruta.

LINQ LANGUAGE INTEGRATED QUERY (permet fer conuslta de dades utilitzant la propia sintaxis del llenguatge)

-------------------------------------------------

**Carrefour** - implementació no viable - API protegida per Cloudflare Bot Management (HTTP 403)
Només permet fer-ho desde el navegador.

La huella digital JA3 es un método de ciberseguridad desarrollado por Salesforce en 2017 para identificar de forma pasiva aplicaciones cliente TLS, como navegadores o malware. Genera un hash MD5 único basado en los parámetros del paquete ClientHello (versión, conjuntos de cifrado, extensiones), permitiendo detectar patrones de tráfico malicioso o no deseado.

### Nota técnica — Carrefour

Durante el desarrollo se identificó y analizó el endpoint REST de búsqueda de Carrefour
(`/search-api/query/v1/search`). La API devuelve datos correctamente desde el navegador,
pero todas las peticiones realizadas desde el backend reciben un **HTTP 403** por parte de
**Cloudflare Bot Management**, que detecta el fingerprint TLS de las peticiones realizadas
fuera de un navegador real.

Se descartó el uso de herramientas de automatización de navegador (Playwright, Puppeteer)
por añadir complejidad innecesaria al scope del proyecto.


### Nota técnica — HIPERCOR

Durante el desarrollo se identificó y analizó la integración con Hipercor
(El Corte Inglés). Se exploraron tres endpoints:

- `/alimentacion/api/catalog/supermercado/type_ahead/` — devuelve resultados
  desde el navegador pero responde **HTTP 403** desde el backend.
- `/supermercado/buscar/` — página HTML de resultados, también bloqueada con
  **HTTP 403**.
- `/api/food-firefly/typeahead_related_terms/supermercado/` — solo devuelve
  sugerencias de texto, sin datos de productos ni precios.

Todas las peticiones realizadas desde el backend son bloqueadas por
**Akamai Bot Manager**, el sistema anti-bot que protege la infraestructura
de El Corte Inglés. Akamai identifica y rechaza peticiones automatizadas
analizando el fingerprint TLS, la ausencia de cookies de sesión dinámicas
(`ak_bmsc`, `bm_sz`, `bm_sv`) generadas por JavaScript en el navegador real,
y la ausencia de comportamiento humano (movimiento de ratón, eventos DOM, etc.).

Se descartó el uso de herramientas de automatización de navegador
(Playwright, Puppeteer) por añadir una complejidad desproporcionada al
scope del proyecto. El finder queda documentado como no operativo por
causa de protección activa del proveedor, no por limitación del diseño
del backend.

### Nota técnica — BonÀrea

Durante el desarrollo se identificó el endpoint POST `/ca/shop/find` de BonÀrea
(`bonarea-online.com`), que devuelve JSON con productos correctamente desde el navegador.
Sin embargo, todas las peticiones realizadas desde el backend reciben como respuesta
**HTML en lugar de JSON**, independientemente de los headers enviados, lo que sugiere
que el servidor requiere cookies de sesión o parámetros generados por el frontend
para responder con el formato correcto.

Se descartó por la complejidad de replicar el flujo de sesión del navegador.

### Nota técnica — Eroski / Caprabo

Eroski y Caprabo forman parte del mismo grupo empresarial y comparten
infraestructura tecnológica.

**Eroski** no expone una API REST pública. Los resultados se sirven como HTML
renderizado en servidor, con los datos de producto incrustados en un bloque
`<script>` bajo la clave `impressions` en formato JSON escapado. Se implementó
un `PreProcessResponse` personalizado que extrae y deserializa ese fragmento,
pero el enfoque es frágil: cualquier cambio en la estructura del HTML o en el
nombre de la clave rompe la extracción. Además, el sitio aplica rate limiting
que puede bloquear peticiones automatizadas frecuentes.

**Caprabo** comparte la misma plataforma y aplica las mismas restricciones.
Adicionalmente, genera tokens de sesión dinámicos (`dwsid`, `csrf_token`)
mediante JavaScript antes de aceptar peticiones, lo que hace inviable el
acceso desde backend sin automatización de navegador.

El finder de Eroski queda operativo pero frágil por diseño. Caprabo queda
documentado como no operativo por causa de las protecciones activas de la
plataforma, no por limitación del diseño del backend. Se descartó el uso
de Playwright o Puppeteer por añadir complejidad desproporcionada al scope
del proyecto.



**Sistema Log In**
ASP.NET Core Identity
https://learn.microsoft.com/es-es/aspnet/core/security/authentication/identity?view=aspnetcore-10.0&tabs=visual-studio

dotnet add package Microsoft.AspNetCore.Identity.EntityFrameworkCore --version 8.0.0
dotnet add package Microsoft.EntityFrameworkCore.SqlServer --version 8.0.0
dotnet add package Microsoft.EntityFrameworkCore.Tools --version 8.0.0


**BBDD SQLITE (NO compatible amb hosting myASP.net, MSSQL, es fara migració despres.)** 
//
Explciació summarized : El hosting tiene su propio SQL Server. Tú le pasas una cadena de conexión diferente apuntando a ese servidor.
// EINA ef
dotnet tool install --global dotnet-ef --version 8.0.0

// CREA TABLA USUARIOS
dotnet ef migrations add InitialCreate
dotnet ef database update

*Controlador - Model - Vista / LOGIN*
https://learn.microsoft.com/es-es/aspnet/mvc/overview/older-versions-1/models-data/validation-with-the-data-annotation-validators-cs

https://stackoverflow.com/questions/44783702/what-is-the-use-of-html-antiforgerytoken