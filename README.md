# UserManagementAPI

A simple ASP.NET Core CRUD API for managing users with custom bearer-token authentication.

## Features

- Create, read, update, and delete users
- Bearer-token authentication for protected endpoints
- Input validation for name and email fields
- Consistent `401 Unauthorized` responses for missing or invalid authentication
- Consistent `400 Bad Request` responses for invalid input
- Consistent `404 Not Found` responses for missing users
- Global exception handling for unexpected failures
- In-memory storage using a dictionary-based concurrency-safe collection

## Running the API

From the project folder, run:

```bash
dotnet run
```

The API will listen on the local development port configured by ASP.NET Core, and requests can be tested against:

- http://localhost:5165/users

## Authentication

All user endpoints require an `Authorization` header using the `Bearer` scheme.

The expected token is read from the `AuthToken` configuration value, or falls back to `secret-token` when no value is provided.

Example:

```http
Authorization: Bearer secret-token
```

## Example endpoints

- `GET /users`
- `GET /users/{id}`
- `POST /users`
- `PUT /users/{id}`
- `DELETE /users/{id}`

## Test requests

A ready-to-use request file is available at [UserManagementAPI/requests.http](UserManagementAPI/requests.http) with examples for success and error cases.

## Notes

- This project is an educational sample and intentionally minimal.
- Use the .NET SDK appropriate for your environment.
