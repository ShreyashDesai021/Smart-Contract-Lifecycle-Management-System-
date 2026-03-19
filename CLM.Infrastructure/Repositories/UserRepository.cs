using CLM.Core.Entities;
using CLM.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using CLM.Infrastructure.Data;

namespace CLM.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _ctx;
    public UserRepository(ApplicationDbContext ctx) => _ctx = ctx;

    public async Task<User?> GetByEmailAsync(string email) =>
        await _ctx.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Email == email);

    public async Task<User?> GetByIdAsync(int id) =>
        await _ctx.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == id);

    public async Task<User> CreateAsync(User user)
    {
        _ctx.Users.Add(user);
        await _ctx.SaveChangesAsync();
        return user;
    }

    public async Task<Role?> GetRoleByNameAsync(string name) =>
        await _ctx.Roles.FirstOrDefaultAsync(r => r.Name == name);

    public async Task<List<User>> GetByRoleAsync(string roleName) =>
        await _ctx.Users.Include(u => u.Role)
                        .Where(u => u.Role.Name == roleName)
                        .ToListAsync();
}
