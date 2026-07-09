using UserManagementAPI.Models;

namespace UserManagementAPI.Services;

public interface IUserService
{
    IReadOnlyCollection<User> GetAll();
    User? GetById(int id);
    User Create(CreateUserRequest request);
    User? Update(int id, UpdateUserRequest request);
    bool Delete(int id);
}
