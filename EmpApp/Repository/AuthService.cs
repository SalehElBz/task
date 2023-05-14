using System.Security.Cryptography;
using System.Text;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using EmpApp.Data;
using EmpApp.Models;
using Microsoft.IdentityModel.Tokens;
using EmpApp.Helpers;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using EmpApp.DAL;

namespace EmpApp.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IMapper _map;
    private readonly JwtSettings _jwt;
    private readonly SignInManager<ApplicationUser> _signin;
    private readonly RoleManager<IdentityRole> _role;
    private readonly ApplicationDbContext _context;
    public AuthService(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IMapper maper,
        IOptions<JwtSettings> jwt,
        SignInManager<ApplicationUser> signIn,
        RoleManager<IdentityRole> role)
    {
        _userManager = userManager;
        _map = maper;
        _jwt = jwt.Value;
        _signin = signIn;
        _role = role;
        _context = context;
    }
    [Obsolete]
    public async Task<AuthModel> GetTokenAsync(LoginModel model)
    {

        var authModel = new AuthModel();
        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user is null || !await _userManager.CheckPasswordAsync(user, model.Password))
        {
            authModel.Message = "Incorrect Email or Password!";
            return authModel;
        }
        var result = await _signin.PasswordSignInAsync(user, model.Password, false, true);
        if (!result.Succeeded)
        {
            authModel.Message = "Something went wrong!";
            return authModel;
        }
        if (user.RefreshTokens!.Any(t => t.IsActive))
        {
            var activeRefreshToken = user.RefreshTokens?.FirstOrDefault(t => t.IsActive);
            authModel.RefreshToken = activeRefreshToken?.Token;
            authModel.RefreshTokenExpiration = activeRefreshToken!.ExpiresOn;
        }
        else
        {
            var refreshToken = GenerateRefreshToken();
            authModel.RefreshToken = refreshToken?.Token;
            authModel.RefreshTokenExpiration = refreshToken!.ExpiresOn;
            user.RefreshTokens?.Add(refreshToken);
            await _userManager.UpdateAsync(user);
        }
        await CreateRoles();
        await CreateAdmin();
        if (result.Succeeded)
        {
            if (await _role.RoleExistsAsync("Employee"))
            {
                if (!await _userManager.IsInRoleAsync(user, "Employee") && !await _userManager.IsInRoleAsync(user, "HR"))
                {
                    await _userManager.AddToRoleAsync(user, "Employee");
                }
            }
        }
        var jwtSecurityToken = await CreateJwtToken(user);
        var roles = await _userManager.GetRolesAsync(user);
        authModel.UserName = user.UserName;
        authModel.IsAuthenticated = true;
        authModel.Token = new JwtSecurityTokenHandler().WriteToken(jwtSecurityToken);
        authModel.ExpiresOn = jwtSecurityToken.ValidTo;
        authModel.Roles = roles.ToList();
        return authModel;
    }

    public async Task<AuthModel> RegisterAsync(RegisterModel model)
    {
        await CreateAdmin();
        await CreateRoles();
        if (await _userManager.FindByEmailAsync(model.Email) is not null)
            return new AuthModel { Message = "Email is already registered!" };
        if (await _userManager.FindByNameAsync(model.UserName) is not null)
            return new AuthModel { Message = "Username is already registered!" };

        var user = _map.Map<ApplicationUser>(model);
        var result = await _userManager.CreateAsync(user, model.Password);

        if (!result.Succeeded)
        {
            var errors = "";
            foreach (var error in result.Errors)
                errors += $"{error.Description} , ";
            return new AuthModel { Message = errors };
        }
        await _userManager.AddToRoleAsync(user, "Employee");
        var jwtSecurityToken = await CreateJwtToken(user);
        return new AuthModel
        {
            IsAuthenticated = true,
            Roles = new List<string> { "Employee" },
            UserName = user.UserName,
            Token = new JwtSecurityTokenHandler().WriteToken(jwtSecurityToken),
            ExpiresOn = jwtSecurityToken.ValidTo
        };
    }

    public async Task<string> AddToRoleAsync(AddToRoleModel model)
    {
        var user = await _userManager.FindByIdAsync(model.UserId!);
        var role = await _role.FindByNameAsync(model.RoleName!);
        if (user is null || role is null)
            return "UserName or RoleId is invalid!";
        if (await _userManager.IsInRoleAsync(user, role.Name!))
            return "User already assigned to this role!";
        var result = await _userManager.AddToRoleAsync(user, role.Name!);
        return (!result.Succeeded) ? result.Errors.ToString()! : "";
    }
    [Obsolete]
    public async Task<AuthModel> RefreshTokenAsync(string token)
    {
        var authModel = new AuthModel();
        var user = await _userManager.Users.SingleOrDefaultAsync(u => u.RefreshTokens!.Any(t => t.Token == token));
        if (user is null)
        {
            authModel.Message = "invalid token";
            return authModel;
        }
        var refreshToken = user.RefreshTokens?.Single(t => t.Token == token);
        if (!refreshToken!.IsActive)
        {
            authModel.Message = "inactive token";
            return authModel;
        }
        refreshToken.RevokedOn = DateTime.UtcNow;
        var newRefreshToken = GenerateRefreshToken();
        user.RefreshTokens?.Add(newRefreshToken);
        await _userManager.UpdateAsync(user);
        var jwtToken = await CreateJwtToken(user);
        var roles = await _userManager.GetRolesAsync(user);
        authModel.IsAuthenticated = true;
        authModel.RefreshToken = newRefreshToken.Token;
        authModel.RefreshTokenExpiration = newRefreshToken.ExpiresOn;
        authModel.Token = new JwtSecurityTokenHandler().WriteToken(jwtToken);
        authModel.ExpiresOn = jwtToken.ValidTo;
        authModel.UserName = user.UserName;
        authModel.Roles = roles.ToList();
        return authModel;
    }
    public async Task<bool> RevokeTokenAsync(string token)
    {
        var user = await _userManager.Users.SingleOrDefaultAsync(u => u.RefreshTokens!.Any(t => t.Token == token));
        if (user is null)
            return false;
        var refreshToken = user.RefreshTokens?.Single(t => t.Token == token);
        if (!refreshToken!.IsActive)
            return false;

        refreshToken.RevokedOn = DateTime.UtcNow;
        user.RefreshTokens!.Add(refreshToken);
        await _userManager.UpdateAsync(user);
        return true;
    }
    private async Task<JwtSecurityToken> CreateJwtToken(ApplicationUser user)
    {
        var userClaims = await _userManager.GetClaimsAsync(user);
        var userRoles = await _userManager.GetRolesAsync(user);
        var roleClaims = new List<Claim>();
        foreach (var role in userRoles)
            roleClaims.Add(new Claim("role", role));
        var claims = new[]{
            new Claim(JwtRegisteredClaimNames.Sub,user.UserName!),
            new Claim(JwtRegisteredClaimNames.Jti,Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Email,user.Email!),
            new Claim("uid",user.Id)
        }
        .Union(userClaims)
        .Union(roleClaims);
        var symmetricSecurityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Key));
        var sgningCredintials = new SigningCredentials(symmetricSecurityKey, SecurityAlgorithms.HmacSha256);

        return new JwtSecurityToken(issuer: _jwt.Issure,
        audience: _jwt.Audience
        , expires: DateTime.Now.AddMinutes(_jwt.Duration),
        claims: claims,
        signingCredentials: sgningCredintials
        );
    }
    [Obsolete]
    private RefreshToken GenerateRefreshToken()
    {
        var arr = new byte[32];
        var generator = new RNGCryptoServiceProvider();
        generator.GetBytes(arr);

        return new RefreshToken
        {
            Token = Convert.ToBase64String(arr),
            ExpiresOn = DateTime.UtcNow.AddDays(10),
            CreatedOn = DateTime.UtcNow
        };
    }

    public async Task<bool> IsEmailExist(string email)
    {
        if (string.IsNullOrEmpty(email))
            return false;
        var user = await _userManager.FindByEmailAsync(email);
        if (user is not null)
            return true;
        return false;
    }

    public async Task<bool> IsUserExist(string username)
    {
        if (string.IsNullOrEmpty(username))
            return false;
        var user = await _userManager.FindByNameAsync(username);
        if (user is not null)
            return true;
        return false;
    }

    public async Task<ApplicationUser> GetUserByIdAsync(string id)
    {
        if (string.IsNullOrEmpty(id))
            return null!;
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
            return null!;
        return user;
    }
    public async Task<IEnumerable<IdentityRole>?> GetRolesAsync()
    {
        var roles = await _context.Roles.ToListAsync();
        if (roles.Count <= 0)
            return null!;
        return roles;
    }
    public async Task<IEnumerable<ApplicationUser>?> GetUsersAsync()
    {
        var users = await _userManager.Users.ToListAsync();
        if (users.Count <= 0)
            return null!;
        return users;
    }
    public async Task DeleteUserAsync(string id)
    {
        if (string.IsNullOrEmpty(id))
            return;
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
            return;
        _context.Users.Remove(user);
        await _context.SaveChangesAsync();
    }
    public async Task<string> EditUserAsync(ApplicationUser user)
    {
        if (user is null)
            return "user is null!";
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
            return "something went wrong!";
        return null!;
    }
    private async Task CreateAdmin()
    {
        var admin = await _userManager.FindByNameAsync("HR");
        if (admin == null)
        {
            var user = new ApplicationUser
            {
                Email = "hr@hr.com",
                UserName = "HR",
                PhoneNumber = "90977643231",
                EmailConfirmed = true
            };

            var x = await _userManager.CreateAsync(user, "123#Aa");
            if (x.Succeeded)
            {
                if (await _role.RoleExistsAsync("HR"))
                    await _userManager.AddToRoleAsync(user, "HR");
            }
        }
    }

    private async Task CreateRoles()
    {
        if (_role.Roles.Count() < 1)
        {
            var role = new IdentityRole
            {
                Name = "HR"
            };
            await _role.CreateAsync(role);

            role = new IdentityRole
            {
                Name = "Employee"
            };
            await _role.CreateAsync(role);
        }
    }

}