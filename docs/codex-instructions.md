# Instrucciones para Codex - Backend ASP.NET Core

## Contexto del proyecto
Este repositorio contiene el nuevo backend del sistema de gestión de turnos médicos de un centro médico.

El frontend existente está desarrollado en React y actualmente funciona con Supabase.
El objetivo de este backend es reemplazar progresivamente esa dependencia mediante una API propia en ASP.NET Core, con PostgreSQL autogestionado.

La documentación funcional y técnica relevante se encuentra en la carpeta `docs/`.
Antes de generar o modificar código, leer especialmente:
- `docs/backend-migration-plan.md`
- `docs/domain-rules.md`
- `docs/api-contracts.md`
- `schema/medical_center_schema.sql`

## Objetivo principal
Construir un backend propio, mantenible y escalable, con estas características:
- ASP.NET Core Web API
- C#
- PostgreSQL
- EF Core + Npgsql
- autenticación con JWT + refresh tokens
- arquitectura de monolito modular
- despliegue con Docker Compose en VPS

## Prioridades
1. respetar la documentación del dominio y contratos
2. mantener una arquitectura clara y simple
3. priorizar mantenibilidad sobre complejidad innecesaria
4. hacer cambios pequeños, revisables y coherentes
5. evitar sobreingeniería
6. centralizar en backend toda la lógica crítica de negocio
7. proteger la integridad de agendas y turnos

## Arquitectura objetivo
La solución debe seguir una arquitectura por capas con separación clara de responsabilidades.

### Proyectos esperados
- `MedicalCenter.Api`
- `MedicalCenter.Application`
- `MedicalCenter.Domain`
- `MedicalCenter.Infrastructure`
- `MedicalCenter.Contracts`
- `MedicalCenter.UnitTests`
- `MedicalCenter.IntegrationTests`

### Responsabilidades por capa

#### Api
- endpoints HTTP
- configuración del pipeline
- autenticación y autorización
- middlewares
- manejo global de errores
- swagger / openapi
- composición de dependencias

#### Application
- casos de uso
- servicios de aplicación
- DTOs internos
- validaciones
- orquestación de reglas de negocio
- interfaces de puertos necesarios

#### Domain
- entidades
- value objects
- enums
- reglas del negocio
- invariantes
- lógica central del dominio

#### Infrastructure
- EF Core
- DbContext
- configuraciones de persistencia
- repositorios
- migraciones
- implementación de autenticación
- acceso a servicios externos
- background jobs futuros

#### Contracts
- request/response models públicos
- contratos de la API
- modelos compartidos entre capas expuestas si aplica

## Criterios de diseño
- usar nombres claros y explícitos
- evitar abstracciones prematuras
- no introducir patrones complejos sin necesidad
- no introducir microservicios
- no introducir CQRS completo salvo necesidad muy clara
- no introducir event bus interno
- no introducir Redis salvo que exista un caso concreto documentado
- no asumir reglas de negocio que no estén respaldadas por documentación o código
- si una regla no está clara, marcarla explícitamente como supuesto

## Principios de implementación
- cada módulo debe poder entenderse de forma aislada
- cada endpoint debe delegar lógica real en Application/Domain
- evitar controllers con lógica de negocio
- evitar lógica de persistencia mezclada con lógica de dominio
- modelar el dominio de turnos con mucho cuidado
- proteger la concurrencia en reservas desde backend y base de datos
- priorizar consistencia de datos sobre atajos de implementación

## Dominio crítico
El módulo más sensible del sistema es el de turnos.
Toda implementación relacionada con disponibilidad, reservas, cancelaciones y reprogramaciones debe considerarse crítica.

Especial cuidado con:
- doble reserva de turnos
- superposición de horarios
- validación de agenda por profesional
- restricciones por sede o consultorio
- bloqueos manuales
- feriados
- estados válidos de turno
- permisos por rol

## Base de datos
Usar PostgreSQL como base principal.

Lineamientos:
- usar EF Core + Npgsql como acceso principal
- definir migraciones claras
- usar índices y constraints cuando corresponda
- proteger integridad con claves foráneas, unique constraints y reglas adecuadas
- si una validación crítica puede reforzarse a nivel base de datos, considerarlo
- no diseñar la base pensando en Supabase sino en el dominio y la API propia

## Autenticación y autorización
Implementar:
- JWT bearer tokens
- refresh tokens
- expiración corta para access token
- rotación o invalidación razonable de refresh tokens
- autorización basada en roles y policies

Evitar:
- lógica de permisos distribuida sin criterio
- decisiones de autorización solo en frontend
- mezclar autenticación con lógica del dominio

## API
La API debe:
- seguir los contratos definidos en `docs/api-contracts.md`
- exponer endpoints REST claros
- devolver errores consistentes
- usar códigos HTTP correctos
- estar preparada para versionado
- generar documentación Swagger/OpenAPI

## Calidad y mantenimiento
- escribir código legible
- no dejar código muerto
- no duplicar lógica
- extraer piezas reutilizables cuando tenga sentido
- mantener consistencia de naming y estructura
- dejar TODOs solo cuando realmente falte contexto o una decisión externa

## Testing
Agregar tests cuando el cambio lo justifique.

Prioridades de test:
- reglas críticas de turnos
- autenticación
- autorización
- validaciones de agenda
- casos borde de cancelación y reprogramación

Evitar tests superficiales sin valor real.

## Infraestructura
Preparar el proyecto para correr con:
- Docker
- Docker Compose
- PostgreSQL
- variables de entorno
- health checks

Debe poder desplegarse en una VPS sin depender de servicios innecesarios.

## Observabilidad
Incluir una base razonable para:
- logs estructurados
- health checks
- manejo global de excepciones

OpenTelemetry y métricas avanzadas pueden quedar preparados o incorporarse gradualmente, pero no deben bloquear el scaffolding inicial.

## Lo que Codex debe hacer bien
- leer primero la documentación existente
- construir una solución coherente con el dominio
- crear una base sólida y extensible
- mantener bajo el nivel de complejidad
- señalar ambigüedades reales
- preferir decisiones conservadoras y robustas

## Lo que Codex no debe hacer
- inventar reglas de negocio no documentadas
- generar una arquitectura sobredimensionada
- introducir librerías por moda
- crear múltiples servicios o repos separados dentro de este backend
- reescribir todo por adelantado si todavía no está validado
- acoplar la API a detalles de implementación del frontend más de lo necesario

## Convenciones generales
- usar C# moderno y buenas prácticas de .NET
- usar `async/await` correctamente
- preferir `DateTimeOffset` para fechas y horas expuestas por API cuando corresponda
- usar `CancellationToken` en operaciones relevantes
- mantener DTOs separados de entidades
- no exponer entidades de dominio directamente en la API
- usar configuración tipada cuando tenga sentido
- mantener el código listo para producción, no solo para demo

## Resultado esperado por tarea
Cada tarea debe dejar uno o más de estos resultados:
- documentación más precisa
- solución más cercana a producción
- módulos más claros
- backend más desacoplado
- reglas críticas mejor protegidas