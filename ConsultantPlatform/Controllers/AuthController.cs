using ConsultantPlatform.Models.DTO;
using ConsultantPlatform.Models.Entity;
using ConsultantPlatform.Service;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System; // Убедитесь, что добавлено System для DateTime

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly UserService _userService;
    private readonly IConfiguration _config;
    private readonly PasswordHasher<User> _passwordHasher = new PasswordHasher<User>();

    public AuthController(UserService userService, IConfiguration config)
    {
        _userService = userService;
        _config = config;
    }

    private object GenerateJwtTokenResponse(User user)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Login),
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(6),
            signingCredentials: creds
        );

        return new
        {
            Token = new JwtSecurityTokenHandler().WriteToken(token),
            Expires = token.ValidTo
        };
    }


    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDTO model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }
        var existingUser = await _userService.GetUserByLogin(model.Login);
        if (existingUser == null)
        {
            return BadRequest(new { Message = "User doesn't exist" });
        }
        var result = _passwordHasher.VerifyHashedPassword(existingUser, existingUser.Password, model.Password);
        if (result != PasswordVerificationResult.Success)
        {
            return BadRequest(new { Message = "Invalid password" });
        }
        return Ok(GenerateJwtTokenResponse(existingUser));
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegistrationDTO model)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var existingUser = await _userService.GetUserByLogin(model.Login);
            if (existingUser != null)
            {
                return BadRequest(new { Message = "Username already exists" });
            }

            if (model.Password != model.ConfirmPassword)
            {
                return BadRequest(new { Message = "Passwords don't match" });
            }

            var user = new User
            {
                Id = Guid.NewGuid(),
                Login = model.Login,
                FirstName = model.FirstName,
                LastName = model.LastName,
                MiddleName = model.MiddleName,
                Email = "",
                PhoneNumber = "",
            };

            user.Password = _passwordHasher.HashPassword(user, model.Password);

            var createdUser = await _userService.CreateUser(user);

            if (createdUser == null)
            {
                 Console.Error.WriteLine($"Failed to create user for login: {model.Login}");
                 return StatusCode(500, "Failed to create user during registration");
            }

            var tokenResponse = GenerateJwtTokenResponse(createdUser);

            return Ok(new
            {
                Message = "Registration successful and logged in",
                tokenResponse
            });

        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error during registration: {ex.ToString()}");
            return StatusCode(500, "An error occurred during registration");
        }
    }
}