using System.Collections.Concurrent;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

var app = builder.Build();

app.UseExceptionHandler(exceptionHandlerApp =>
{
    exceptionHandlerApp.Run(async context =>
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new { error = "An unexpected error occurred." });
    });
});

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

var initialUsers = new[]
{
    new User(1, "Alice Johnson", "alice@example.com"),
    new User(2, "Bob Smith", "bob@example.com")
};

var users = new ConcurrentDictionary<int, User>(initialUsers.ToDictionary(user => user.Id));
var nextId = users.Keys.DefaultIfEmpty(0).Max() + 1;

app.MapGet("/users", () => Results.Ok(users.Values.OrderBy(user => user.Id).ToList()));

app.MapGet("/users/{id:int}", (int id) =>
{
    if (users.TryGetValue(id, out var user))
    {
        return Results.Ok(user);
    }

    return Results.NotFound();
});

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
});

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
});

app.MapDelete("/users/{id:int}", (int id) =>
{
    var removed = users.TryRemove(id, out _);
    return removed ? Results.NoContent() : Results.NotFound();
});

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

record User(int Id, string Name, string Email);
record CreateUserRequest(string Name, string Email);
record UpdateUserRequest(string Name, string Email);
