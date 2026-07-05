var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

var users = new List<User>
{
    new(1, "Alice Johnson", "alice@example.com"),
    new(2, "Bob Smith", "bob@example.com")
};

app.MapGet("/users", () => Results.Ok(users));

app.MapGet("/users/{id:int}", (int id) =>
{
    var user = users.FirstOrDefault(u => u.Id == id);
    return user is null ? Results.NotFound() : Results.Ok(user);
});

app.MapPost("/users", (CreateUserRequest request) =>
{
    var user = new User(users.Count > 0 ? users.Max(u => u.Id) + 1 : 1, request.Name, request.Email);
    users.Add(user);
    return Results.Created($"/users/{user.Id}", user);
});

app.MapPut("/users/{id:int}", (int id, UpdateUserRequest request) =>
{
    var existingUser = users.FirstOrDefault(u => u.Id == id);
    if (existingUser is null)
    {
        return Results.NotFound();
    }

    var updatedUser = new User(id, request.Name, request.Email);
    var index = users.FindIndex(u => u.Id == id);
    users[index] = updatedUser;

    return Results.Ok(updatedUser);
});

app.MapDelete("/users/{id:int}", (int id) =>
{
    var removed = users.RemoveAll(u => u.Id == id);
    return removed > 0 ? Results.NoContent() : Results.NotFound();
});

app.Run();

record User(int Id, string Name, string Email);
record CreateUserRequest(string Name, string Email);
record UpdateUserRequest(string Name, string Email);
