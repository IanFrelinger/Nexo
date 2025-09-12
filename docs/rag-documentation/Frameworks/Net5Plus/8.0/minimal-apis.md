# Minimal APIs in .NET 8.0

## Overview
Minimal APIs in .NET 8.0 provide a lightweight way to create HTTP APIs with minimal code and configuration. They're perfect for microservices, simple APIs, and rapid prototyping.

## Basic Setup

```csharp
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.Run();
```

## HTTP Methods

### GET
```csharp
app.MapGet("/users", () => new { Name = "John", Age = 30 });
app.MapGet("/users/{id}", (int id) => new { Id = id, Name = "John" });
```

### POST
```csharp
app.MapPost("/users", (User user) => 
{
    // Create user logic
    return Results.Created($"/users/{user.Id}", user);
});
```

### PUT
```csharp
app.MapPut("/users/{id}", (int id, User user) => 
{
    // Update user logic
    return Results.Ok(user);
});
```

### DELETE
```csharp
app.MapDelete("/users/{id}", (int id) => 
{
    // Delete user logic
    return Results.NoContent();
});
```

## Parameter Binding

### From Route
```csharp
app.MapGet("/users/{id:int}", (int id) => $"User ID: {id}");
app.MapGet("/users/{id:guid}", (Guid id) => $"User GUID: {id}");
```

### From Query String
```csharp
app.MapGet("/search", (string? q, int page = 1) => 
    $"Searching for '{q}' on page {page}");
```

### From Body
```csharp
app.MapPost("/users", (User user) => user);
```

### From Headers
```csharp
app.MapGet("/info", (HttpContext context) => 
    context.Request.Headers["User-Agent"]);
```

## Response Types

### JSON
```csharp
app.MapGet("/users", () => new { Users = new[] { "John", "Jane" } });
```

### Status Codes
```csharp
app.MapGet("/users/{id}", (int id) => 
    id > 0 ? Results.Ok(new { Id = id }) : Results.NotFound());
```

### Custom Responses
```csharp
app.MapGet("/download", () => 
    Results.File("file.pdf", "application/pdf"));
```

## Dependency Injection

### Service Registration
```csharp
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));
```

### Service Injection
```csharp
app.MapGet("/users", (IUserService userService) => 
    userService.GetUsers());
```

## Configuration

### App Settings
```csharp
app.MapGet("/config", (IConfiguration config) => 
    config["ConnectionStrings:DefaultConnection"]);
```

### Environment
```csharp
app.MapGet("/env", (IWebHostEnvironment env) => 
    $"Environment: {env.EnvironmentName}");
```

## Middleware

### Custom Middleware
```csharp
app.Use(async (context, next) =>
{
    Console.WriteLine("Before request");
    await next();
    Console.WriteLine("After request");
});
```

### Built-in Middleware
```csharp
app.UseAuthentication();
app.UseAuthorization();
app.UseCors();
app.UseSwagger();
app.UseSwaggerUI();
```

## Error Handling

### Global Exception Handler
```csharp
app.UseExceptionHandler(exceptionHandlerApp =>
{
    exceptionHandlerApp.Run(async context =>
    {
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";
        
        var response = new { Error = "An error occurred" };
        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    });
});
```

### Try-Catch in Endpoints
```csharp
app.MapGet("/risky", () =>
{
    try
    {
        // Risky operation
        return Results.Ok();
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
});
```

## Validation

### Data Annotations
```csharp
public record User(string Name, [Range(1, 120)] int Age);

app.MapPost("/users", (User user) => 
{
    if (!ModelState.IsValid)
        return Results.ValidationProblem(ModelState);
    
    return Results.Created($"/users/{user.Name}", user);
});
```

### Fluent Validation
```csharp
builder.Services.AddScoped<IValidator<User>, UserValidator>();

app.MapPost("/users", (User user, IValidator<User> validator) =>
{
    var validationResult = validator.Validate(user);
    if (!validationResult.IsValid)
        return Results.ValidationProblem(validationResult.ToDictionary());
    
    return Results.Created($"/users/{user.Name}", user);
});
```

## OpenAPI/Swagger

### Basic Setup
```csharp
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

app.UseSwagger();
app.UseSwaggerUI();
```

### Custom Documentation
```csharp
app.MapGet("/users", (IUserService userService) => 
    userService.GetUsers())
    .WithName("GetUsers")
    .WithSummary("Get all users")
    .WithDescription("Retrieves a list of all users in the system")
    .Produces<User[]>(200)
    .Produces(404);
```

## Performance Considerations

### Response Caching
```csharp
app.MapGet("/cached", () => "Cached response")
    .CacheOutput();
```

### Rate Limiting
```csharp
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("fixed", opt =>
    {
        opt.PermitLimit = 100;
        opt.Window = TimeSpan.FromMinutes(1);
    });
});

app.UseRateLimiter();
app.MapGet("/limited", () => "Limited").RequireRateLimiting("fixed");
```

## Testing

### Unit Testing
```csharp
[Test]
public async Task GetUsers_ReturnsUsers()
{
    // Arrange
    var app = WebApplication.CreateBuilder().Build();
    app.MapGet("/users", () => new[] { "John", "Jane" });
    
    var client = app.GetTestClient();
    
    // Act
    var response = await client.GetAsync("/users");
    
    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
}
```

### Integration Testing
```csharp
[Test]
public async Task CreateUser_ReturnsCreated()
{
    // Arrange
    var app = WebApplication.CreateBuilder().Build();
    app.MapPost("/users", (User user) => Results.Created($"/users/{user.Name}", user));
    
    var client = app.GetTestClient();
    var user = new User("John", 30);
    
    // Act
    var response = await client.PostAsJsonAsync("/users", user);
    
    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.Created);
}
```

## Best Practices

### 1. Keep Endpoints Simple
```csharp
// ✅ Good
app.MapGet("/users", (IUserService userService) => userService.GetUsers());

// ❌ Avoid complex logic in endpoints
app.MapGet("/users", (IUserService userService, ILogger<Program> logger) =>
{
    // Complex business logic here
    logger.LogInformation("Processing users");
    // ... many lines of code
});
```

### 2. Use Records for DTOs
```csharp
public record UserDto(string Name, int Age, string Email);
public record CreateUserRequest(string Name, int Age, string Email);
```

### 3. Group Related Endpoints
```csharp
var users = app.MapGroup("/users");
users.MapGet("/", GetUsers);
users.MapGet("/{id}", GetUser);
users.MapPost("/", CreateUser);
users.MapPut("/{id}", UpdateUser);
users.MapDelete("/{id}", DeleteUser);
```

### 4. Use Filters for Cross-Cutting Concerns
```csharp
app.MapGet("/users", (IUserService userService) => userService.GetUsers())
    .AddEndpointFilter<LoggingFilter>()
    .AddEndpointFilter<ValidationFilter>();
```

## Migration from Controllers

### Before (Controller)
```csharp
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<User>>> GetUsers()
    {
        // Implementation
    }
}
```

### After (Minimal API)
```csharp
app.MapGet("/api/users", (IUserService userService) => userService.GetUsers());
```

## Related Topics
- ASP.NET Core fundamentals
- Dependency injection
- Middleware pipeline
- Model binding
- Response formatting
- OpenAPI/Swagger
- Testing strategies
