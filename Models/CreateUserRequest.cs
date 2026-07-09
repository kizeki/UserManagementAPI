namespace UserManagementAPI.Models;

public class CreateUserRequest
{
    public CreateUserRequest(string name, string email)
    {
        Name = name;
        Email = email;
    }

    public string Name { get; }
    public string Email { get; }
}
