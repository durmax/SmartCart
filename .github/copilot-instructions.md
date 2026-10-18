# Copilot Instructions

## Project Guidelines
- For the SmartCart project, prefer dependency-injection extension methods to keep service registrations out of Program.cs. 
- Domain stays dependency-free; Application depends only on Domain; Infrastructure depends on Application/Domain; Web/API may depend on Application/Infrastructure, never leak Infrastructure into Web/Domain.
- Keep business logic out of controllers.
- Avoid introducing CQRS, MediatR, AutoMapper, repositories, unit-of-work, microservices, and unnecessary abstractions. 
- SmartCart coding conventions: keep Domain free of project dependencies, Application depends only on Domain, Infrastructure depends on Application/Domain, and API depends on Application/Infrastructure.