using EmpApp.Data;
using EmpApp.Models;
using Microsoft.AspNetCore.Identity;

namespace EmpApp.Services;

public interface IAuthService
{
    Task<AuthModel> RegisterAsync(RegisterModel model);
    Task<AuthModel> GetTokenAsync(LoginModel model);
    Task<string> AddToRoleAsync(AddToRoleModel model);
    Task<AuthModel> RefreshTokenAsync(string token);
    Task<bool> RevokeTokenAsync(string token);
    Task<bool> IsEmailExist(string email);
    Task<bool> IsUserExist(string username);
    Task<ApplicationUser> GetUserByIdAsync(string id);
    Task<IEnumerable<IdentityRole>?> GetRolesAsync();
    Task<IEnumerable<ApplicationUser>?> GetUsersAsync();
    Task DeleteUserAsync(string id);
    Task<string> EditUserAsync(ApplicationUser user);
}