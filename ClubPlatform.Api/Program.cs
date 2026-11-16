using System.Text;
using System.Text.Json.Serialization;
using ClubPlatform.Api.Data;
using ClubPlatform.Api.Hubs;
using ClubPlatform.Api.Services.Chat;
using ClubPlatform.Api.Services.Auth;
using ClubPlatform.Api.Services.Cloudinary;
using ClubPlatform.Api.Services.Interests;
using ClubPlatform.Api.Services.Clubs;
using ClubPlatform.Api.Services.Jwt;
using ClubPlatform.Api.Services.Moderation;
using ClubPlatform.Api.Settings;
using ClubPlatform.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Npgsql;
using Npgsql.NameTranslation;

var builder = WebApplication.CreateBuilder(args);

// =========================================================
// JWT SETTINGS
// =========================================================

// Загружаем настройки JWT из конфигурации.
// Сам секретный ключ находится в User Secrets,
// а не в исходном коде проекта.
builder.Services.Configure<JwtSettings>(
    builder.Configuration.GetSection("JwtSettings"));

// =========================================================
// CLOUDINARY SETTINGS
// =========================================================

builder.Services.Configure<CloudinarySettings>(
    builder.Configuration.GetSection("CloudinarySettings"));

// =========================================================
// CONTROLLERS
// =========================================================

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        // Передаём enum в JSON как текст,
        // например "Female", а не число 1.
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter());
    });

// =========================================================
// SIGNALR
// =========================================================

builder.Services.AddSignalR();


builder.Services.AddSingleton<ClubConnectionTracker>();

// =========================================================
// SWAGGER
// =========================================================

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "ClubPlatform API",
        Version = "v1",
        Description = "API мобильного приложения ClubPlatform"
    });

    // -----------------------------------------------------
    // Настройка JWT в Swagger.
    // После получения токена мы сможем нажать
    // кнопку Authorize и выполнять защищённые запросы.
    // -----------------------------------------------------

    options.AddSecurityDefinition(
        "Bearer",
        new OpenApiSecurityScheme
        {
            Name = "Authorization",

            Type = SecuritySchemeType.Http,

            Scheme = "bearer",

            BearerFormat = "JWT",

            In = ParameterLocation.Header,

            Description =
                "Введите JWT-токен. " +
                "Swagger автоматически добавит Bearer."
        });

    options.AddSecurityRequirement(
        new OpenApiSecurityRequirement
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
                Array.Empty<string>()
            }
        });
});

// =========================================================
// AUTHENTICATION
// =========================================================

builder.Services
    .AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        var jwtSettings =
            builder.Configuration
                .GetSection("JwtSettings")
                .Get<JwtSettings>()
            ?? throw new InvalidOperationException(
                "Настройки JwtSettings не найдены.");

        if (string.IsNullOrWhiteSpace(jwtSettings.Key))
        {
            throw new InvalidOperationException(
                "JwtSettings:Key не настроен.");
        }

        // Настраиваем правила проверки JWT.
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                // Проверяем подпись токена.
                ValidateIssuerSigningKey = true,

                // Ключ, которым подписываются JWT.
                IssuerSigningKey =
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(
                            jwtSettings.Key)),

                // Проверяем Issuer.
                ValidateIssuer = true,

                ValidIssuer = jwtSettings.Issuer,

                // Проверяем Audience.
                ValidateAudience = true,

                ValidAudience = jwtSettings.Audience,

                // Проверяем срок действия токена.
                ValidateLifetime = true,

                // Допускаем небольшую разницу времени
                // между компьютером и сервером.
                ClockSkew = TimeSpan.FromSeconds(30),

                // Claim с ролью пользователя.
                RoleClaimType =
                    System.Security.Claims.ClaimTypes.Role,

                // Claim с именем пользователя.
                NameClaimType =
                    System.Security.Claims.ClaimTypes.Name
            };

        // =====================================================
        // SIGNALR JWT
        // =====================================================

        options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken =
                    context.Request.Query["access_token"];

                var path =
                    context.HttpContext.Request.Path;

                // Разрешаем брать token из query string
                // только для нашего SignalR Hub.
                if (!string.IsNullOrEmpty(accessToken) &&
                    path.StartsWithSegments("/hubs/club-chat"))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            }
        };
    });

// =========================================================
// AUTHORIZATION
// =========================================================

builder.Services.AddAuthorization();

// =========================================================
// APPLICATION SERVICES
// =========================================================

// Сервис авторизации.
builder.Services.AddScoped<IAuthService, AuthService>();

// Сервис создания JWT.
builder.Services.AddScoped<IJwtService, JwtService>();

builder.Services.AddScoped<
    ICloudinaryService,
    CloudinaryService>();

builder.Services.AddScoped<
    IInterestService,
    InterestService>();

builder.Services.AddScoped<
    IClubService,
    ClubService>();

builder.Services.AddScoped<
    IMessageService,
    MessageService>();

builder.Services.AddScoped<
    IModerationService,
    ModerationService>();

// =========================================================
// CONNECTION STRING
// =========================================================

// Получаем строку подключения к PostgreSQL.
// Сам пароль находится в User Secrets.
var connectionString =
    builder.Configuration.GetConnectionString(
        "DefaultConnection")
    ?? throw new InvalidOperationException(
        "Строка подключения 'DefaultConnection' не найдена.");

// =========================================================
// NPGSQL DATA SOURCE
// =========================================================

// Создаём подключение к PostgreSQL.
var dataSourceBuilder =
    new NpgsqlDataSourceBuilder(connectionString);

// ---------------------------------------------------------
// Регистрируем PostgreSQL ENUM.
// ---------------------------------------------------------

dataSourceBuilder.MapEnum<UserRole>(
    "user_role",
    new NpgsqlNullNameTranslator());

dataSourceBuilder.MapEnum<Gender>(
    "gender",
    new NpgsqlNullNameTranslator());

dataSourceBuilder.MapEnum<ReportStatus>(
    "report_status",
    new NpgsqlNullNameTranslator());

// Создаём DataSource.
var dataSource = dataSourceBuilder.Build();

// Регистрируем DataSource в Dependency Injection.
builder.Services.AddSingleton(dataSource);

// =========================================================
// ENTITY FRAMEWORK CORE
// =========================================================

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(dataSource);
});

// =========================================================
// BUILD APPLICATION
// =========================================================

var app = builder.Build();

// =========================================================
// STATIC FILES
// Временно используется для SignalR-тестовой страницы.
// =========================================================

app.UseStaticFiles();

// =========================================================
// SWAGGER MIDDLEWARE
// =========================================================

if (app.Environment.IsDevelopment())
{
    // В локальной разработке оставляем доступ к Swagger
    // по HTTP и HTTPS.
    // Это временно нужно для подключения Android-эмулятора.
    app.UseSwagger();

    app.UseSwaggerUI();
}
else
{
    // Вне Development перенаправляем HTTP на HTTPS.
    app.UseHttpsRedirection();
}

// =========================================================
// AUTHENTICATION / AUTHORIZATION
// =========================================================

// Сначала определяется пользователь по JWT.
app.UseAuthentication();

// Затем проверяются его права.
app.UseAuthorization();

// =========================================================
// CONTROLLERS
// =========================================================

app.MapControllers();

// =========================================================
// SIGNALR HUB
// =========================================================

app.MapHub<ClubChatHub>("/hubs/club-chat");

// =========================================================
// ПРОВЕРКА ПОДКЛЮЧЕНИЯ К POSTGRESQL
// =========================================================

try
{
    using var scope = app.Services.CreateScope();

    var db =
        scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

    var canConnect =
        await db.Database.CanConnectAsync();

    if (canConnect)
    {
        var interestsCount =
            await db.Interests.CountAsync();

        Console.WriteLine(
            "==========================================");

        Console.WriteLine(
            "Подключение к PostgreSQL: OK");

        Console.WriteLine(
            $"Интересов в базе: {interestsCount}");

        Console.WriteLine(
            "==========================================");
    }
    else
    {
        Console.WriteLine(
            "Не удалось подключиться к PostgreSQL.");
    }
}
catch (Exception exception)
{
    Console.WriteLine(
        "Ошибка подключения к PostgreSQL:");

    Console.WriteLine(exception.Message);
}

// =========================================================
// START APPLICATION
// =========================================================

app.Run();