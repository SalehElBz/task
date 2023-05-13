using EmpApp.Models;
using Microsoft.AspNetCore.Identity;

namespace EmpApp.Data;

public class ApplicationUser : IdentityUser
{
    public DateTime DateOfBirth { get; set; }
    public DateTime HireDate { get; set; }
    public double Salary { get; set; }
    public int PhotoID { get; set; }
    public int JobPositionID { get; set; }
    public List<RefreshToken>? RefreshTokens { get; set; }
}