using System.Collections.Concurrent;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthentication("Bearer")
    .AddScheme<AuthenticationSchemeOptions, TokenAuthenticationHandler>("Bearer", options => { });
builder.Services.AddAuthorization();

var expectedToken = builder.Configuration["AuthToken"] ?? "secret-token";
builder.Services.AddSingleton(new ApiTokenOptions(expectedToken));

var app = builder.Build();

app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Unhandled exception while processing {Method} {Path}", context.Request.Method, context.Request.Path);
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new { error = "Internal server error." });
    }
});

app.Use(async (context, next) =>
{
    if (!context.Request.Headers.TryGetValue("Authorization", out var authorizationHeader))
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new { error = "Unauthorized." });
        return;
    }

    var value = authorizationHeader.ToString();
    if (!value.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new { error = "Unauthorized." });
        return;
    }

    var token = value["Bearer ".Length..].Trim();
    if (!string.Equals(token, expectedToken, StringComparison.Ordinal))
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new { error = "Unauthorized." });
        return;
    }

    await next();
});

app.UseAuthentication();
app.UseAuthorization();

app.Use(async (context, next) =>
{
    app.Logger.LogInformation("Incoming request {Method} {Path}", context.Request.Method, context.Request.Path);

    await next();

    app.Logger.LogInformation("Outgoing response {Method} {Path} -> {StatusCode}", context.Request.Method, context.Request.Path, context.Response.StatusCode);
});

app.UseHttpsRedirection();

var initialUsers = new[]
{
    new User(1, "Alice Johnson", "alice@example.com"),
    new User(2, "Bob Smith", "bob@example.com")
};

var users = new ConcurrentDictionary<int, User>(initialUsers.ToDictionary(user => user.Id));
var nextId = users.Keys.DefaultIfEmpty(0).Max() + 1;

app.MapGet("/users", () => Results.Ok(users.Values.OrderBy(user => user.Id).ToList()))
    .RequireAuthorization();

app.MapGet("/users/{id:int}", (int id) =>
{
    if (users.TryGetValue(id, out var user))
    {
        return Results.Ok(user);
    }

    return Results.NotFound();
})
.RequireAuthorization();

app.MapPost("/users", (CreateUserRequest request) =>
{
    if (request is null)
    {
        return Results.BadRequest(new { error = "Request body is required." });
    }

    if (!TryValidateUser(request.Name, request.Email, out var validationError))
    {
        return Results.BadRequest(new { error = validationError });
    }

    var userId = Interlocked.Increment(ref nextId);
    var user = new User(userId, request.Name.Trim(), request.Email.Trim().ToLowerInvariant());
    users[userId] = user;

    return Results.Created($"/users/{user.Id}", user);
})
.RequireAuthorization();

app.MapPut("/users/{id:int}", (int id, UpdateUserRequest request) =>
{
    if (request is null)
    {
        return Results.BadRequest(new { error = "Request body is required." });
    }

    if (!TryValidateUser(request.Name, request.Email, out var validationError))
    {
        return Results.BadRequest(new { error = validationError });
    }

    if (!users.ContainsKey(id))
    {
        return Results.NotFound();
    }

    var updatedUser = new User(id, request.Name.Trim(), request.Email.Trim().ToLowerInvariant());
    users[id] = updatedUser;

    return Results.Ok(updatedUser);
})
.RequireAuthorization();

app.MapDelete("/users/{id:int}", (int id) =>
{
    var removed = users.TryRemove(id, out _);
    return removed ? Results.NoContent() : Results.NotFound();
})
.RequireAuthorization();

app.Run();

static bool TryValidateUser(string name, string email, out string error)
{
    if (string.IsNullOrWhiteSpace(name))
    {
        error = "Name is required.";
        return false;
    }

    if (string.IsNullOrWhiteSpace(email))
    {
        error = "Email is required.";
        return false;
    }

    if (!IsValidEmail(email))
    {
        error = "Email format is invalid.";
        return false;
    }

    error = string.Empty;
    return true;
}

static bool IsValidEmail(string email)
{
    try
    {
        var address = new System.Net.Mail.MailAddress(email);
        return address.Address == email.Trim();
    }
    catch
    {
        return false;
    }
}

sealed class TokenAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly string _expectedToken;

    public TokenAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISystemClock clock,
        ApiTokenOptions tokenOptions)
        : base(options, logger, encoder, clock)
    {
        _expectedToken = tokenOptions.ExpectedToken;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("Authorization", out var authorizationHeader))
        {
            return Task.FromResult(AuthenticateResult.Fail("Missing authorization header."));
        }

        var value = authorizationHeader.ToString();
        if (!value.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid authorization scheme."));
        }

        var token = value["Bearer ".Length..].Trim();
        if (!string.Equals(token, _expectedToken, StringComparison.Ordinal))
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid token."));
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "authenticated-user"),
            new Claim(ClaimTypes.Name, "api-user")
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

sealed record ApiTokenOptions(string ExpectedToken);

record User(int Id, string Name, string Email);
record CreateUserRequest(string Name, string Email);
record UpdateUserRequest(string Name, string Email);
