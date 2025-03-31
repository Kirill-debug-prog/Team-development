using ConsultantPlatform.Models.DTO;
using ConsultantPlatform.Models.Entity;
using ConsultantPlatform.Service;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;


[ApiController]
[Route("api/auth/[controller]")]
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

        // **Проверяем хеш пароля**
        var result = _passwordHasher.VerifyHashedPassword(existingUser, existingUser.Password, model.Password);
        if (result != PasswordVerificationResult.Success)
        {
            return BadRequest(new { Message = "Invalid password" });
        }

        // 2. Создаём клеймы (данные, которые будем хранить в токене)
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, existingUser.Id.ToString()),  // ID пользователя
            new Claim(ClaimTypes.Name, existingUser.Login),
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // 4. Генерируем токен
        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(6), // Срок действия
            signingCredentials: creds
        );

        return Ok(new
        {
            Token = new JwtSecurityTokenHandler().WriteToken(token),
            Expires = token.ValidTo
        });
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
            };

            // **Хешируем пароль перед сохранением**
            user.Password = _passwordHasher.HashPassword(user, model.Password);

            var createdUser = await _userService.CreateUser(user);

            return Ok(new
            {
                Message = "Registration successful",
                User = createdUser,
            });
        }
        catch (Exception ex)
        {
            Console.Write(ex.ToString());
            return StatusCode(500, "An error occurred during registration");
        }
    }
}