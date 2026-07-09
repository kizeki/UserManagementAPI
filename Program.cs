using Microsoft.AspNetCore.Authentication;
using UserManagementAPI.Middleware;
using UserManagementAPI.Models;
using UserManagementAPI.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddAuthentication("Bearer")
    .AddScheme<AuthenticationSchemeOptions, TokenAuthenticationHandler>("Bearer", options => { });
builder.Services.AddAuthorization();

var expectedToken = builder.Configuration["AuthToken"] ?? "secret-token";
builder.Services.AddSingleton(new ApiTokenOptions(expectedToken));
builder.Services.AddSingleton<IUserService, UserService>();
builder.Services.AddSingleton<UserValidationService>();

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

app.UseAuthentication();
app.UseAuthorization();

app.Use(async (context, next) =>
{
    app.Logger.LogInformation("Incoming request {Method} {Path}", context.Request.Method, context.Request.Path);

    await next();

    app.Logger.LogInformation("Outgoing response {Method} {Path} -> {StatusCode}", context.Request.Method, context.Request.Path, context.Response.StatusCode);
});

app.UseHttpsRedirection();
app.MapControllers();

app.Run();
