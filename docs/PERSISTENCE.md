# Persistence and Database Integration

## Overview

Nexo provides a **storage abstraction** so applications can persist data without being tied to a specific database. The framework uses the same ports-and-adapters pattern as LLM providers and execution platforms: application code depends on **IUnitOfWork** and **IRepository&lt;TEntity, TKey&gt;**; infrastructure or adapter projects supply the implementation (in-memory, SQLite, PostgreSQL, etc.).

## Ports (Application Layer)

Defined in **Nexo.Core.Application** (no database dependencies):

- **IUnitOfWork** – Scoped unit of work. Provides repositories and `CommitAsync()`. Register as **scoped** (e.g. one per request).
- **IRepository&lt;TEntity, TKey&gt;** – CRUD: `GetByIdAsync`, `GetAllAsync`, `AddAsync`, `UpdateAsync`, `RemoveAsync`. Obtained from a unit of work.
- **IEntity&lt;TKey&gt;** – Optional: entities that implement this expose an `Id` property so the in-memory adapter can extract keys for Add/Update.

## Default: In-Memory

The built-in implementation is **in-memory** (Nexo.Infrastructure.Persistence):

- `AddNexoPersistence()` registers **IUnitOfWork** (scoped) with **InMemoryUnitOfWork**.
- Each scope has its own store; data does not persist across requests or process restarts.
- Entities should implement **IEntity&lt;TKey&gt;** when using the in-memory repository so the adapter can derive keys.

## Using Persistence in Application Code

```csharp
public class MyHandler
{
    private readonly IUnitOfWork _uow;

    public MyHandler(IUnitOfWork uow) => _uow = uow;

    public async Task HandleAsync(CancellationToken ct)
    {
        var repo = _uow.GetRepository<MyEntity, Guid>();
        await repo.AddAsync(new MyEntity { Id = Guid.NewGuid(), Name = "Example" }, ct);
        await _uow.CommitAsync(ct);
    }
}
```

## Avoiding Database Lock-In

1. **Depend only on ports** – Use `IUnitOfWork` and `IRepository<TEntity, TKey>` from **Nexo.Core.Application.Persistence.Ports**. Do not reference EF Core, Dapper, or a specific DB driver in application or domain code.
2. **Implement adapters in separate projects** – Create **Nexo.Adapters.Persistence.Sqlite**, **Nexo.Adapters.Persistence.Postgres**, etc. Each adapter references Core.Application and implements IUnitOfWork (and possibly a factory) using the chosen technology.
3. **Register the adapter in the host** – In startup, replace the default in-memory registration with the adapter’s registration (e.g. `services.AddNexoPersistenceSqlite(connectionString)` and do not call `AddNexoPersistence()` when using a durable store).
4. **Keep entities and keys portable** – Prefer simple keys (int, long, Guid, string) and POCOs that do not depend on ORM-specific attributes; use mapping in the adapter if needed.

## Adding a Database Adapter

Example layout for a SQLite adapter:

1. **New project**: `Nexo.Adapters.Persistence.Sqlite`  
   - References: Nexo.Core.Application, Microsoft.EntityFrameworkCore.Sqlite (or Microsoft.Data.Sqlite).
2. **Implement** `IUnitOfWork` with a class that holds an EF Core `DbContext` (or a connection) and returns repositories that use it; `CommitAsync` calls `SaveChangesAsync`.
3. **Implement** `IRepository<TEntity, TKey>` (or a generic base) that uses the same DbContext/connection.
4. **Extension**: `AddNexoPersistenceSqlite(this IServiceCollection services, string connectionString)`  
   - Registers DbContext (or connection factory) and `IUnitOfWork` as scoped using your implementation.
5. **Host**: Call `services.AddNexoPersistenceSqlite(config.GetConnectionString("Default"))` and do not call `AddNexoPersistence()`.

The same pattern applies for PostgreSQL, SQL Server, or document stores: implement the same ports in a separate adapter and swap registration at the host.

## Relationship to IAgentMemory

**IAgentMemory** (Nexo.Abstractions) is for **agent event storage** (Write/Query of EventRecords). It is a separate abstraction aimed at agent runtime. Persistence for general application data uses **IUnitOfWork** and **IRepository&lt;TEntity, TKey&gt;**; an adapter can optionally back **IAgentMemory** with the same store (e.g. a dedicated table or collection) if you want agent events to be durable.
