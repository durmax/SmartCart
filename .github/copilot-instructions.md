# Copilot Instructions

## Project Guidelines
- For the SmartCart project, prefer dependency-injection extension methods to keep service registrations out of Program.cs; do not register everything directly in Program.cs. Preserve the stated layer dependency rules and avoid CQRS, MediatR, AutoMapper, Repository Pattern, Unit of Work, and unnecessary abstractions.