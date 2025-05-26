using ConsultantPlatform.Models.Entity;
using ConsultantPlatform.Service;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using ConsultantPlatform.Hubs;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSingleton<PasswordHasher<User>>();

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
});


builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Consultant Platform API", Version = "v1" });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Введите JWT токен в формате: Bearer {your_token}"
    });

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
            new string[] {}
        }
    });
});

builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<ConsultantCardService>();
builder.Services.AddScoped<CategoryService>();
builder.Services.AddScoped<ChatService>();
builder.Services.AddSignalR();

var jwtSettings = builder.Configuration.GetSection("Jwt");
var keyString = jwtSettings["Key"];
if (string.IsNullOrEmpty(keyString))
{
    throw new InvalidOperationException("JWT Key is not configured.");
}
var key = Encoding.UTF8.GetBytes(keyString);


builder.Services.AddCors(options =>
{
    options.AddPolicy("SignalRPolicy", // Название политики
        policy =>
        {
            policy.WithOrigins(
                      "http://127.0.0.1:5500", // Клиент для локальной разработки
                      "http://localhost:5500"  // Еще один вариант для локальной разработки
                                               // Если у вас есть другие домены, где будет клиент, добавьте их сюда:
                                               // "https://your-production-client.com"
                  )
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials(); // <--- Это требует явного указания Origins
        });
});

builder.Services.AddDbContext<MentiContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
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
    // ДОБАВЛЯЕМ ЭТОТ БЛОК ДЛЯ SIGNALR
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];

            // Если запрос идет к эндпоинту хаба
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) &&
                (path.StartsWithSegments("/chathub"))) // Убедитесь, что путь "/chathub" совпадает с тем, что в app.MapHub
            {
                // Прочитать токен из query string
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        },
        OnAuthenticationFailed = context => // Этот обработчик у вас уже был, оставляем
        {
            // Логируем ошибку аутентификации
            // Можно использовать ILogger, если он доступен здесь, или Console.WriteLine для быстрой отладки
            Console.WriteLine("Authentication failed: " + context.Exception.Message);
            // Дополнительно:
            if (context.Exception is SecurityTokenExpiredException)
            {
                Console.WriteLine("Token expired at: " + ((SecurityTokenExpiredException)context.Exception).Expires);
            }
            // Можно добавить больше деталей, если необходимо
            // context.NoResult(); // Если хотите остановить дальнейшую обработку и вернуть 401 сразу
            return Task.CompletedTask;
        },
        OnTokenValidated = context => // Этот обработчик у вас уже был, оставляем
        {
            Console.WriteLine("Token validated for user: " + context.Principal?.Identity?.Name);
            // Здесь можно, например, добавить дополнительные клеймы в Principal, если нужно
            return Task.CompletedTask;
        }
    };
});
builder.Services.AddAuthorization();

var app = builder.Build();

app.UseForwardedHeaders();

app.UseCors("SignalRPolicy");


app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "ConsultantPlatform API v1");

});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapHub<ChatHub>("/chathub");

app.Run();