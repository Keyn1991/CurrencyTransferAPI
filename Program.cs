// Program.cs
using CurrencyTransferAPI.Data;
using CurrencyTransferAPI.Models; // Для UserRoles
using CurrencyTransferAPI.Services; // Для NbpService и остальных сервисов
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models; // Для OpenApiInfo и SecurityScheme
using System.Text;
// using System.ComponentModel.DataAnnotations; // Не используется напрямую здесь
using System.IdentityModel.Tokens.Jwt; // Для JwtSecurityTokenHandler

var builder = WebApplication.CreateBuilder(args);

// --- Конфигурация Базы Данных ---
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
}
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString))
);

// --- Конфигурация JWT ---
JwtSecurityTokenHandler.DefaultMapInboundClaims = false; // Рекомендуется для избежания перезаписи стандартных имен клеймов
var jwtKey = builder.Configuration["Jwt:Key"];
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

if (string.IsNullOrEmpty(jwtKey) || string.IsNullOrEmpty(jwtIssuer) || string.IsNullOrEmpty(jwtAudience))
{
    throw new InvalidOperationException("JWT Key, Issuer or Audience not configured in appsettings.json. Please check Jwt:Key, Jwt:Issuer, Jwt:Audience.");
}

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme; // Можно оставить только первые два
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
});

builder.Services.AddAuthorization(options =>
{
    // Пример добавления политики для роли Администратора
    // options.AddPolicy("RequireAdminRole", policy => policy.RequireRole(UserRoles.Admin));
});


// Регистрация сервисов в контейнере DI
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "CurrencyTransfer API", Version = "v1" });
    // Конфигурация для JWT в Swagger
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Please enter a valid token (JWT). Example: \"Bearer {token}\"",
        Name = "Authorization",
        Type = SecuritySchemeType.Http, // Изменено с ApiKey на Http для Bearer
        BearerFormat = "JWT",
        Scheme = "Bearer" // "bearer" в нижнем регистре
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer" // Должно совпадать с именем в AddSecurityDefinition
                }
            },
            Array.Empty<string>() // или new string[] {}
        }
    });
});

// Регистрация NbpService и HttpClient для него
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient<NbpService>();

// Регистрация твоих сервисов бизнес-логики
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<ITransferService, TransferService>();
builder.Services.AddScoped<IExchangeService, ExchangeService>();
// Если будешь добавлять IPayUService, его нужно будет зарегистрировать здесь:
builder.Services.AddScoped<IPayUService, PayUService>();

// Конфигурация CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins("http://localhost:3000") // Адрес твоего React-приложения
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// Конфигурация HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "CurrencyTransfer API V1");
        // c.RoutePrefix = string.Empty; // Раскомментируй, если хочешь Swagger на главной странице API (/)
    });
    app.UseDeveloperExceptionPage();
}

// app.UseHttpsRedirection(); // Раскомментируй, если настроил HTTPS

app.UseCors("AllowReactApp"); // Применение политики CORS

app.UseAuthentication(); // Включение аутентификации
app.UseAuthorization();  // Включение авторизации

app.MapControllers(); // Маппинг контроллеров

// Сидинг администратора при старте (код из твоего примера)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var authServiceInstance = services.GetRequiredService<IAuthService>();
        if (authServiceInstance is AuthService concreteAuthService)
        {
            // Для вызова асинхронного метода из синхронного контекста Main (до app.Run())
            // .GetAwaiter().GetResult() является одним из способов, но требует осторожности.
            // Идеально, если Main асинхронный или используется IHostedService.
            Task.Run(async () => await concreteAuthService.SeedAdminUserAsync()).GetAwaiter().GetResult();
            Console.WriteLine("Admin user seeding attempted.");
        }
        else
        {
             var logger = services.GetRequiredService<ILogger<Program>>();
             logger.LogWarning("AuthService could not be cast to concrete AuthService for seeding. Admin user might not be seeded.");
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the admin user.");
    }
}

app.Run();