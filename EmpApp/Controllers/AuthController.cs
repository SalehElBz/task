using Microsoft.AspNetCore.Mvc;
using EmpApp.Models;
using EmpApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using EmpApp.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Cors;

namespace EmpApp.Controllers;
[ApiController]
// [EnableCors("corsPolicy")]
[Authorize(Roles = "HR")]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService service;
    public AuthController(IAuthService _service)
    {
        service = _service;
    }
    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterModel model)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);
        var result = await service.RegisterAsync(model);
        if (!result.IsAuthenticated)
            BadRequest(result.Message);
        //SetRefreshTokenInCookie(result.RefreshToken, result.RefreshTokenExpiration);
        return Ok(result);
    }
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginModel model)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);
        var result = await service.GetTokenAsync(model);
        if (!result.IsAuthenticated)
            BadRequest(result.Message);
        if (!string.IsNullOrEmpty(result.RefreshToken))
            SetRefreshTokenInCookie(result.RefreshToken, result.RefreshTokenExpiration);
        return Ok(result);
    }
    [HttpPost("addToRole")]
    public async Task<IActionResult> AddToRoleAsync(AddToRoleModel model)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);
        var result = await service.AddToRoleAsync(model);
        return string.IsNullOrEmpty(result) ? Ok(result) : BadRequest(result);
    }
    [HttpGet("refreshToken")]
    public async Task<IActionResult> RefreshTokenAsync()
    {
        var rtoken = Request.Cookies["refreshToken"];
        if (string.IsNullOrEmpty(rtoken))
            return BadRequest("invalid token");
        var result = await service.RefreshTokenAsync(rtoken);
        if (!result.IsAuthenticated)
            return BadRequest(result.Message);
        SetRefreshTokenInCookie(result.RefreshToken!, result.RefreshTokenExpiration);
        return Ok(result);
    }
    [HttpPost("revokeToken")]
    public async Task<IActionResult> RevokeTokenAsync([FromBody] RevokeModel model)
    {
        var token = model.Token ?? Request.Cookies["refreshToken"];
        if (string.IsNullOrEmpty(token))
            return BadRequest("token is required!");
        var result = await service.RevokeTokenAsync(token);
        if (!result)
            return BadRequest("invalid token!");
        return Ok();
    }

    [HttpGet]
    [AllowAnonymous]
    [Route("Logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync();
        return Ok();
    }
    [HttpGet]
    [Route("UserExists")]
    public async Task<IActionResult> UserExists(string username)
    {
        var exist = await service.IsUserExist(username);
        if (exist)
        {
            return StatusCode(StatusCodes.Status200OK);
        }
        return StatusCode(StatusCodes.Status400BadRequest);
    }
    [HttpGet("getuserbyid/{id}")]
    public async Task<IActionResult> GetUserByIdAsync(string id)
    {
        var user = await service.GetUserByIdAsync(id);
        if (user is null)
            return StatusCode(StatusCodes.Status400BadRequest);
        return Ok(user);
    }
    [HttpGet("getusers")]
    [AllowAnonymous]
    public async Task<IEnumerable<ApplicationUser>?> GetUsersAsync()
    {
        var users = await service.GetUsersAsync();
        if (users is null)
            return null!;
        else
            return users;
    }
    [HttpGet("getroles")]
    public async Task<IEnumerable<IdentityRole>?> GetRolesAsync()
    {
        var roles = await service.GetRolesAsync();
        if (roles is null)
            return null!;
        else
            return roles;
    }
    [HttpDelete("deleteuser")]
    public async Task DeleteUserAsync(string id)
    {
        await service.DeleteUserAsync(id);
    }
    [HttpPut("edituser")]
    public async Task<IActionResult> EditUserAsync(ApplicationUser user)
    {
        var result = await service.EditUserAsync(user);
        if (result is not null)
            return BadRequest(result);
        return StatusCode(StatusCodes.Status200OK);
    }
    [HttpGet]
    [Route("EmailExists")]
    public async Task<IActionResult> EmailExists(string email)
    {
        var exist = await service.IsEmailExist(email);
        if (exist)
        {
            return StatusCode(StatusCodes.Status200OK);
        }
        return StatusCode(StatusCodes.Status400BadRequest);
    }
    private void SetRefreshTokenInCookie(string refreshToken, DateTime expires)
    {
        CookieOptions cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Expires = expires.ToLocalTime()
        };
        Response.Cookies.Append("refreshToken", refreshToken, cookieOptions);
    }
}