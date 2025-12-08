using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using SecureVideoStreaming.Data.Context;
using SecureVideoStreaming.API.Extensions;
using SecureVideoStreaming.API.Middleware;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddControllers();
builder.Services.AddRazorPages();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Secure Video Streaming API",
        Version = "v1",
        Description = "API para gestión segura de videos cifrados con distribución de claves"
    });

    // Configuración de autenticación JWT en Swagger
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Ingrese 'Bearer' seguido de un espacio y luego su token JWT.\n\nEjemplo: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Session support for Razor Pages
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// JWT Authentication
var jwtSettings = builder.Configuration.GetSection("Jwt");
var secretKey = jwtSettings["SecretKey"];
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey!))
    };
});

// Database Configuration - SQL Server
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlServerOptionsAction: sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null);
            
            // Especificar que las migraciones están en el proyecto SecureVideoStreaming.Data
            sqlOptions.MigrationsAssembly("SecureVideoStreaming.Data");
        });
});

// Cryptography Services
builder.Services.AddCryptographyServices();

// Business Services
builder.Services.AddBusinessServices();
builder.Services.AddScoped<SecureVideoStreaming.Services.Business.Interfaces.IVideoService, SecureVideoStreaming.Services.Business.Implementations.VideoService>();
builder.Services.AddScoped<SecureVideoStreaming.Services.Business.Interfaces.IPermissionService, SecureVideoStreaming.Services.Business.Implementations.PermissionService>();
builder.Services.AddScoped<SecureVideoStreaming.Services.Business.Interfaces.IVideoGridService, SecureVideoStreaming.Services.Business.Implementations.VideoGridService>();
builder.Services.AddScoped<SecureVideoStreaming.Services.Business.Interfaces.IKeyDistributionService, SecureVideoStreaming.Services.Business.Implementations.KeyDistributionService>();

// CORS Configuration
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
// IMPORTANTE: ErrorHandling debe estar PRIMERO para capturar todas las excepciones
app.UseErrorHandling();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseCors("AllowAll");
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapRazorPages();

app.Run();

