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

    // Приватный метод для генерации JWT и объекта ответа
    private object GenerateJwtTokenResponse(User user)
    {
        // 2. Создаём клеймы (данные, которые будем хранить в токене)
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), // ID пользователя
            new Claim(ClaimTypes.Name, user.Login),
            // Добавьте другие клеймы, если нужно (например, роли)
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

        // **Проверяем хеш пароля**
        var result = _passwordHasher.VerifyHashedPassword(existingUser, existingUser.Password, model.Password);
        if (result != PasswordVerificationResult.Success)
        {
            return BadRequest(new { Message = "Invalid password" });
        }

        // Используем приватный метод для генерации токена и ответа
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
                Id = Guid.NewGuid(), // Убедитесь, что это генерируется или присваивается правильно
                Login = model.Login,
                FirstName = model.FirstName,
                LastName = model.LastName,
                MiddleName = model.MiddleName,
            };

            // **Хешируем пароль перед сохранением**
            user.Password = _passwordHasher.HashPassword(user, model.Password);

            // Создаем пользователя в базе данных
            var createdUser = await _userService.CreateUser(user);

            // Проверяем, успешно ли создан пользователь (на случай, если CreateUser может вернуть null)
            if (createdUser == null)
            {
                 // Логирование ошибки, если создание пользователя не удалось
                 Console.Error.WriteLine($"Failed to create user for login: {model.Login}");
                 return StatusCode(500, "Failed to create user during registration");
            }

            // Сразу после успешной регистрации, генерируем JWT для нового пользователя
            var tokenResponse = GenerateJwtTokenResponse(createdUser);

            // Возвращаем JWT в ответе на запрос регистрации
            return Ok(new
            {
                Message = "Registration successful and logged in",
                tokenResponse
            });

        }
        catch (Exception ex)
        {
            // Логирование ошибки
            Console.Error.WriteLine($"Error during registration: {ex.ToString()}");
            return StatusCode(500, "An error occurred during registration");
        }
    }
}