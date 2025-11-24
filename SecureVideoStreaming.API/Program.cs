using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using SecureVideoStreaming.Data.Context;
using SecureVideoStreaming.API.Extensions;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddControllers();
builder.Services.AddRazorPages();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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
        });
});

// Cryptography Services
builder.Services.AddCryptographyServices();

// Business Services
builder.Services.AddScoped<SecureVideoStreaming.Services.Business.Interfaces.IAuthService, SecureVideoStreaming.Services.Business.Implementations.AuthService>();
builder.Services.AddScoped<SecureVideoStreaming.Services.Business.Interfaces.IUserService, SecureVideoStreaming.Services.Business.Implementations.UserService>();
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

