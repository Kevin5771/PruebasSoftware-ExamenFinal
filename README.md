# Examen Final - Pruebas de Software

## Descripción del proyecto

Implementación de pruebas automatizadas para el registro y la consulta de clientes en la solución DemoTesting. El proyecto usa .NET 10, C#, xUnit, Moq, SQLite, HttpClient y WebApplicationFactory.

La API recibe las solicitudes HTTP en `ClientesController`, delega la lógica de negocio a `ClienteService` y accede a SQLite mediante `ClienteRepository`. Las pruebas unitarias sustituyen el repositorio con Moq. Las pruebas de integración, API y E2E usan el repositorio real con bases SQLite temporales.

## Pruebas realizadas

### 1. Prueba unitaria con Moq

Comprueba que `ClienteService.ObtenerPorId` devuelve el cliente esperado y consulta al repositorio exactamente una vez con el ID solicitado.

**Resultado — Aprobada**

![Evidencia de la prueba unitaria](CAPTURAS/PRUEBA%20UNITARIA.png)

### 2. Pruebas de integración con SQLite

Incluyen 40 casos de consulta de IDs inexistentes. Verifican que el servicio devuelve `null` cuando la base está vacía, cuando el ID queda fuera de los registros existentes y cuando el cliente fue eliminado.

**Resultado — 40 casos aprobados**

![Evidencia de las pruebas de integración](CAPTURAS/PRUEBA%20INTEGRACI%C3%93N.png)

### 3. Prueba de API — 200 OK

Consulta un cliente existente mediante `HttpClient` y `WebApplicationFactory`. Comprueba el estado `200 OK` y valida el ID, el nombre y el correo del cuerpo JSON.

**Resultado — Aprobada**

### 4. Prueba de API — 404 Not Found

Consulta un ID inexistente en una base SQLite temporal y comprueba que la API devuelve `404 Not Found`.

**Resultado — Aprobada**

![Evidencia de la prueba de API 404 Not Found](CAPTURAS/PRUEBA%20404.png)

### 5. Pruebas de extremo a extremo (E2E)

Registran un cliente por HTTP, siguen la URL devuelta en el encabezado `Location` y verifican los datos recuperados. También comprueban que se conserva el cliente original al rechazar un duplicado, que se recortan los espacios exteriores y que un registro corregido se puede guardar después de una solicitud inválida.

**Resultado — 6 casos aprobados**

![Evidencia de las pruebas E2E](CAPTURAS/PRUEBAS%20E2E.png)

## Resultado general de las pruebas

| Resultado | Cantidad |
|---|---:|
| Pruebas ejecutadas | 91 |
| Pruebas aprobadas | 91 |
| Pruebas fallidas | 0 |
| Pruebas omitidas | 0 |

## Cómo ejecutar las pruebas

Instala el SDK de .NET 10 y ejecuta los comandos desde la raíz del proyecto.

Para restaurar dependencias y ejecutar todas las pruebas, usa PowerShell.

```powershell
dotnet restore .\DemoTesting.slnx
dotnet test .\DemoTesting.slnx
```

Para ejecutar solo las pruebas de API y E2E, aplica este filtro.

```powershell
dotnet test .\DemoTesting.slnx --filter "FullyQualifiedName~ClienteApp.Tests.Api|FullyQualifiedName~ClienteApp.Tests.E2E"
```

## Proyecto original

Actividad de examen final basada en el [repositorio PruebasSoftware del catedrático](https://github.com/eruanoc5/PruebasSoftware).
