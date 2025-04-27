using ConsultantPlatform.Models.Entity;
using ConsultantPlatform.Service;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using Microsoft.AspNetCore.HttpOverrides; // Добавлена строка для ForwardedHeaders

var builder = WebApplication.CreateBuilder(args);

// Добавление контроллеров
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSingleton<PasswordHasher<User>>();

// Настройка Forwarded Headers для работы за прокси
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    // Если Nginx также передает заголовок X-Forwarded-Host, можно добавить и его:
    // options.ForwardedHeaders |= ForwardedHeaders.XForwardedHost;

    // Опционально: Очистить известные прокси/сети, если вы уверены, что только Nginx является прокси
    // options.KnownNetworks.Clear();
    // options.KnownProxies.Clear();
});


// Добавляем поддержку Swagger с авторизацией
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Consultant Platform API", Version = "v1" });

    // Настраиваем схему безопасности (Bearer Token)
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Введите JWT токен в формате: Bearer {your_token}"
    });

    // Указываем, что все запросы требуют токен
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {} // Без конкретных ролей
        }
    });
});

// Добавляем зависимости
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<ConsultantCardService>();

// Настройки JWT
var jwtSettings = builder.Configuration.GetSection("Jwt");
var keyString = jwtSettings["Key"];
if (string.IsNullOrEmpty(keyString))
{
    throw new InvalidOperationException("JWT Key is not configured.");
}
var key = Encoding.UTF8.GetBytes(keyString);


builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy => policy.AllowAnyOrigin()
                        .AllowAnyMethod()
                        .AllowAnyHeader());
});

// Подключаем БД
builder.Services.AddDbContext<MentiContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false; // Оставляем false, т.к. Nginx может обрабатывать SSL
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        ClockSkew = TimeSpan.Zero
    };
    // Опционально: обработка событий для диагностики проблем с JWT
    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            Console.WriteLine("Authentication failed: " + context.Exception.Message);
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            Console.WriteLine("Token validated for user: " + context.Principal.Identity.Name);
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

var app = builder.Build();

// --- Включаем UseForwardedHeaders СРАЗУ после app.Build() ---
// Это КРАЙНЕ ВАЖНО для корректной работы за прокси, должно быть ПЕРЕД большинством middleware
app.UseForwardedHeaders();


// Включаем CORS (порядок после UseForwardedHeaders, но перед UseAuthentication/UseAuthorization)
app.UseCors("AllowAll");

// --- Включаем Swagger и SwaggerUI (вне условия Development) ---
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    // Путь к json файлу спецификации относительно корня
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "ConsultantPlatform API v1");

    // Если вы хотите, чтобы Swagger UI был доступен прямо по адресу сервера (без /swagger в конце)
    // c.RoutePrefix = string.Empty; // В этом случае Nginx location для '/' должен проксировать на бэкенд
});


// --- Закомментирована строка HttpsRedirection ---
// Закомментируйте эту строку, если Nginx проксирует HTTP на бэкенд
// Она может вызывать некорректное поведение при работе за прокси
// app.UseHttpsRedirection();


// Включаем аутентификацию и авторизацию (порядок важен: UseAuthentication перед UseAuthorization)
app.UseAuthentication();
app.UseAuthorization();

// app.UseRouting(); // В последних версиях MapControllers добавляет UseRouting автоматически

app.MapControllers();

app.Run();