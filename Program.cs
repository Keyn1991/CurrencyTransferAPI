using CurrencyTransferAPI.Data;
using CurrencyTransferAPI.Models; // Dla UserRoles
using CurrencyTransferAPI.Services; // Dla NbpService i przyszłych serwisów
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models; // Dla OpenApiInfo i SecurityScheme
using System.Text;
using System.ComponentModel.DataAnnotations; // Dla atrybutów w DTO
using System.IdentityModel.Tokens.Jwt;

var builder = WebApplication.CreateBuilder(args);

// --- Konfiguracja Bazy Danych ---
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
}
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString))
);

// --- Konfiguracja JWT ---
JwtSecurityTokenHandler.DefaultMapInboundClaims = false;
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
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
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
    // Tutaj można dodawać polityki autoryzacyjne, jeśli będą potrzebne bardziej złożone scenariusze
    // Np. options.AddPolicy("RequireAdminRole", policy => policy.RequireRole(UserRoles.Admin));
});


// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "CurrencyTransfer API", Version = "v1" });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Please enter a valid token (JWT)",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "Bearer"
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
            Array.Empty<string>()
        }
    });
});

// Add memory cache and HTTP client for NBP service (twoje istniejące)
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient<NbpService>(); // Zakładam, że NbpService istnieje w CurrencyTransferAPI.Services

// TODO: Rejestracja serwisów dla logiki biznesowej (np. AuthService, AccountService)
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<ITransferService, TransferService>();
builder.Services.AddScoped<IExchangeService, ExchangeService>();
// builder.Services.AddScoped<IAuthService, AuthService>();

// CORS policy (twoje istniejące)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins("http://localhost:3000") // Adres Twojej aplikacji React
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "CurrencyTransfer API V1");
        // c.RoutePrefix = string.Empty; // Jeśli chcesz Swaggera na stronie głównej API
    });
    app.UseDeveloperExceptionPage(); // Lepsze komunikaty o błędach w trybie deweloperskim
}

// app.UseHttpsRedirection(); // Odkomentuj, jeśli pracujesz z HTTPS i masz skonfigurowany certyfikat

app.UseCors("AllowReactApp");

app.UseAuthentication(); // WAŻNE: Przed UseAuthorization
app.UseAuthorization();

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var authServiceInstance = services.GetRequiredService<IAuthService>();
        // Rzutowanie na konkretną klasę, aby uzyskać dostęp do metody SeedAdminUserAsync
        if (authServiceInstance is AuthService concreteAuthService)
        {
            // UWAGA: SeedAdminUserAsync jest asynchroniczne.
            // W tym kontekście synchronicznym (przed app.Run()), jeśli nie możesz użyć await,
            // możesz zrobić .GetAwaiter().GetResult() - ale rób to z ostrożnością.
            // Lepszym podejściem jest uczynienie metody Main asynchroniczną lub stworzenie
            // dedykowanego serwisu hostowanego do zadań startowych.
            // Na razie, dla prostoty, spróbujmy tak:
            Task.Run(async () => await concreteAuthService.SeedAdminUserAsync()).GetAwaiter().GetResult();
            Console.WriteLine("Admin user seeding attempted.");
        }
        else
        {
             var logger = services.GetRequiredService<ILogger<Program>>();
             logger.LogWarning("AuthService could not be cast to concrete AuthService for seeding.");
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the admin user.");
    }
}

app.Run();