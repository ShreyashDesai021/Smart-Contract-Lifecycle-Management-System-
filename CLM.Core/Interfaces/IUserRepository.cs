using CLM.Core.Entities;

namespace CLM.Core.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByIdAsync(int id);
    Task<User> CreateAsync(User user);
    Task<Role?> GetRoleByNameAsync(string name);
    Task<List<User>> GetByRoleAsync(string roleName);
}
