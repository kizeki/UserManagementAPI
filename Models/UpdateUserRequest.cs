namespace UserManagementAPI.Models;

public class UpdateUserRequest
{
    public UpdateUserRequest(string name, string email)
    {
        Name = name;
        Email = email;
    }

    public string Name { get; }
    public string Email { get; }
}
