---
globs:
  - "**/Infrastructure/**/*.cs"
  - "**/Persistence/**/*.cs"
  - "**/Data/**/*.cs"
  - "**/Repositories/**/*.cs"
  - "**/*Repository.cs"
  - "**/*DbContext.cs"
  - "**/Migrations/**/*.cs"
---

# Reglas para Infraestructura y Persistencia

> Este archivo aplica cuando Claude trabaja con EF Core, repositorios y acceso a datos.
> **Detecta automáticamente** la versión de .NET/EF del proyecto.

---

## DETECCIÓN DE VERSIÓN

```xml
<PackageReference Include="Microsoft.EntityFrameworkCore" Version="10.0.*" />  <!-- EF Core 10 -->
<PackageReference Include="Microsoft.EntityFrameworkCore" Version="8.0.*" />   <!-- EF Core 8 -->
<PackageReference Include="EntityFramework" Version="6.*" />                    <!-- EF 6 (.NET 4.x) -->
```

---

## PARTE 1: DBCONTEXT POR VERSIÓN

### EF Core 10 (Recomendado)

```csharp
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) 
        : base(options)
    {
    }

    public DbSet<Beca> Becas => Set<Beca>();
    public DbSet<Solicitud> Solicitudes => Set<Solicitud>();
    public DbSet<Estudiante> Estudiantes => Set<Estudiante>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Aplicar todas las configuraciones del ensamblado
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // Configuración global para soft delete
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(ISoftDelete).IsAssignableFrom(entityType.ClrType))
            {
                modelBuilder.Entity(entityType.ClrType)
                    .HasQueryFilter(CreateSoftDeleteFilter(entityType.ClrType));
            }
        }
    }

    // EF Core 10 - Interceptores mejorados
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder
            .AddInterceptors(new AuditingInterceptor())
            .AddInterceptors(new SlowQueryInterceptor());
    }

    private static LambdaExpression CreateSoftDeleteFilter(Type type)
    {
        var parameter = Expression.Parameter(type, "e");
        var property = Expression.Property(parameter, nameof(ISoftDelete.IsDeleted));
        var condition = Expression.Equal(property, Expression.Constant(false));
        return Expression.Lambda(condition, parameter);
    }

    // EF Core 10 - SaveChanges con auditoría
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker.Entries<IAuditable>();
        var now = DateTime.UtcNow;

        foreach (var entry in entries)
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    entry.Entity.CreatedBy = GetCurrentUser();
                    break;
                case EntityState.Modified:
                    entry.Entity.ModifiedAt = now;
                    entry.Entity.ModifiedBy = GetCurrentUser();
                    break;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}
```

### Configuración Fluent API - EF Core 10

```csharp
public class BecaConfiguration : IEntityTypeConfiguration<Beca>
{
    public void Configure(EntityTypeBuilder<Beca> builder)
    {
        builder.ToTable("Becas", "dbo");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.Id)
            .ValueGeneratedOnAdd();

        builder.Property(b => b.Nombre)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(b => b.Descripcion)
            .HasMaxLength(2000);

        builder.Property(b => b.Importe)
            .HasPrecision(18, 2);

        // EF Core 10 - Complex Types mejorados
        builder.ComplexProperty(b => b.Periodo, period =>
        {
            period.Property(p => p.FechaInicio).HasColumnName("FechaInicio");
            period.Property(p => p.FechaFin).HasColumnName("FechaFin");
        });

        // Índices
        builder.HasIndex(b => b.Codigo)
            .IsUnique()
            .HasDatabaseName("IX_Becas_Codigo");

        builder.HasIndex(b => b.Estado)
            .HasDatabaseName("IX_Becas_Estado");

        // Relaciones
        builder.HasMany(b => b.Solicitudes)
            .WithOne(s => s.Beca)
            .HasForeignKey(s => s.BecaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
```

### EF 6 (.NET 4.x Legacy)

```csharp
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext() 
        : base("name=DefaultConnection")
    {
    }

    public DbSet<Beca> Becas { get; set; }
    public DbSet<Solicitud> Solicitudes { get; set; }

    protected override void OnModelCreating(DbModelBuilder modelBuilder)
    {
        modelBuilder.Configurations.AddFromAssembly(typeof(ApplicationDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}

// Configuración EF 6
public class BecaConfiguration : EntityTypeConfiguration<Beca>
{
    public BecaConfiguration()
    {
        ToTable("Becas", "dbo");
        HasKey(b => b.Id);
        Property(b => b.Nombre).IsRequired().HasMaxLength(200);
        Property(b => b.Importe).HasPrecision(18, 2);
    }
}
```

---

## PARTE 2: REPOSITORIOS

### Repositorio Genérico - EF Core 10

```csharp
public interface IRepository<T> where T : Entity
{
    Task<T?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default);
    Task<T> AddAsync(T entity, CancellationToken ct = default);
    Task UpdateAsync(T entity, CancellationToken ct = default);
    Task DeleteAsync(T entity, CancellationToken ct = default);
    Task<bool> ExistsAsync(int id, CancellationToken ct = default);
}

public class Repository<T> : IRepository<T> where T : Entity
{
    protected readonly ApplicationDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public Repository(ApplicationDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public virtual async Task<T?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _dbSet.FindAsync(new object[] { id }, ct);
    }

    public virtual async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default)
    {
        return await _dbSet.ToListAsync(ct);
    }

    public virtual async Task<T> AddAsync(T entity, CancellationToken ct = default)
    {
        await _dbSet.AddAsync(entity, ct);
        await _context.SaveChangesAsync(ct);
        return entity;
    }

    public virtual async Task UpdateAsync(T entity, CancellationToken ct = default)
    {
        _dbSet.Update(entity);
        await _context.SaveChangesAsync(ct);
    }

    public virtual async Task DeleteAsync(T entity, CancellationToken ct = default)
    {
        _dbSet.Remove(entity);
        await _context.SaveChangesAsync(ct);
    }

    public virtual async Task<bool> ExistsAsync(int id, CancellationToken ct = default)
    {
        return await _dbSet.AnyAsync(e => e.Id == id, ct);
    }
}
```

### Repositorio Específico con Consultas Optimizadas

```csharp
public interface IBecaRepository : IRepository<Beca>
{
    Task<Beca?> GetWithSolicitudesAsync(int id, CancellationToken ct = default);
    Task<PaginatedResult<Beca>> GetPaginatedAsync(int page, int size, CancellationToken ct = default);
    Task<IReadOnlyList<Beca>> GetActivasAsync(CancellationToken ct = default);
}

public class BecaRepository : Repository<Beca>, IBecaRepository
{
    public BecaRepository(ApplicationDbContext context) : base(context) { }

    // EF Core 10 - AsSplitQuery para evitar explosión cartesiana
    public async Task<Beca?> GetWithSolicitudesAsync(int id, CancellationToken ct = default)
    {
        return await _dbSet
            .Include(b => b.Solicitudes)
                .ThenInclude(s => s.Estudiante)
            .AsSplitQuery()
            .FirstOrDefaultAsync(b => b.Id == id, ct);
    }

    // EF Core 10 - Paginación eficiente
    public async Task<PaginatedResult<Beca>> GetPaginatedAsync(
        int page, int size, CancellationToken ct = default)
    {
        var query = _dbSet.AsNoTracking();
        
        var totalItems = await query.CountAsync(ct);
        
        var items = await query
            .OrderBy(b => b.Nombre)
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync(ct);

        return new PaginatedResult<Beca>(items, totalItems, page, size);
    }

    // EF Core 10 - Compiled Query para consultas frecuentes
    private static readonly Func<ApplicationDbContext, IAsyncEnumerable<Beca>> _getActivasQuery =
        EF.CompileAsyncQuery((ApplicationDbContext ctx) =>
            ctx.Becas
                .Where(b => b.Estado == EstadoBeca.Activa)
                .OrderBy(b => b.FechaFin));

    public async Task<IReadOnlyList<Beca>> GetActivasAsync(CancellationToken ct = default)
    {
        var result = new List<Beca>();
        await foreach (var beca in _getActivasQuery(_context).WithCancellation(ct))
        {
            result.Add(beca);
        }
        return result;
    }
}
```

---

## PARTE 3: MIGRACIONES

### Crear Migración

```bash
# .NET 10
dotnet ef migrations add AddBecasTable -p src/Infrastructure -s src/Web

# Con contexto específico
dotnet ef migrations add AddBecasTable --context ApplicationDbContext
```

### Migración con Datos Seed

```csharp
public partial class AddBecasTable : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Becas",
            schema: "dbo",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Nombre = table.Column<string>(maxLength: 200, nullable: false),
                Importe = table.Column<decimal>(precision: 18, scale: 2, nullable: false),
                Estado = table.Column<int>(nullable: false),
                CreatedAt = table.Column<DateTime>(nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Becas", x => x.Id);
            });

        // Índices
        migrationBuilder.CreateIndex(
            name: "IX_Becas_Estado",
            schema: "dbo",
            table: "Becas",
            column: "Estado");

        // Datos iniciales
        migrationBuilder.InsertData(
            schema: "dbo",
            table: "Becas",
            columns: new[] { "Nombre", "Importe", "Estado", "CreatedAt" },
            values: new object[] { "Beca Excelencia", 5000m, 1, DateTime.UtcNow });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "Becas", schema: "dbo");
    }
}
```

---

## PARTE 4: CONFIGURACIÓN CONNECTION STRING

### .NET 10 - Program.cs

```csharp
// Desarrollo
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null);
            sqlOptions.CommandTimeout(30);
            sqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
        })
    .EnableSensitiveDataLogging(builder.Environment.IsDevelopment())
    .EnableDetailedErrors(builder.Environment.IsDevelopment()));
```

### appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=ComillasBecas;Trusted_Connection=True;TrustServerCertificate=True"
  }
}
```

### ⚠️ NUNCA en código

```csharp
// ❌ NUNCA
options.UseSqlServer("Server=prod;User=sa;Password=123456");

// ✅ Usar configuración
options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"));

// ✅ Azure Key Vault para producción
```

---

## PARTE 5: COMPARATIVA EF

| Característica | EF 6 | EF Core 8 | EF Core 10 |
|----------------|------|-----------|------------|
| **Async** | Limitado | Completo | Optimizado |
| **Split Query** | N/A | Manual | Auto/Config |
| **Compiled Query** | N/A | Sí | Mejorado |
| **Complex Types** | N/A | Básico | Completo |
| **Bulk Operations** | N/A | ExecuteUpdate | Mejorado |
| **JSON Columns** | N/A | Básico | Completo |

---

## CHECKLIST

### Antes de cada consulta
- [ ] AsNoTracking() para lecturas
- [ ] AsSplitQuery() para includes múltiples
- [ ] Select() para proyecciones (no cargar entidad completa)
- [ ] Paginación en listados
- [ ] CancellationToken

### Migraciones
- [ ] Revisar SQL generado antes de aplicar
- [ ] Índices en columnas de búsqueda/filtro
- [ ] Datos seed si corresponde
- [ ] Método Down() funcional

---

*Regla condicional v3.7.0 - Multi-versión .NET*
