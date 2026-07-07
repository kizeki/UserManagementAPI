# UserManagementAPI

A simple ASP.NET Core CRUD API for managing users.

## Features

- Create, read, update, and delete users
- Input validation for name and email fields
- Consistent `404 Not Found` responses for missing users
- Global exception handling for unexpected failures
- Improved lookup performance using dictionary-based storage

## Running the API

From the project folder, run:

```bash
dotnet run
```

Then open:

- http://localhost:5165/users

## Example endpoints

- `GET /users`
- `GET /users/{id}`
- `POST /users`
- `PUT /users/{id}`
- `DELETE /users/{id}`

## Notes

- This project is an educational sample and intentionally minimal.
- Use the .NET SDK appropriate for your environment.
