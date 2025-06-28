using AuthApi.Data;
using AuthApi.Extensions;
using AuthApi.Middlewares;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// 1. Logging
builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

// 2. Core services
builder.Services.AddDbContext<DataContext>(opts =>
    opts.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<IDataContext>(provider =>
    provider.GetRequiredService<DataContext>());

// 3. Custom services
builder.Services.AddAuthServices();

// 4. Authentication & Authorization
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new()
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                // Check for X-Test-User header to allow our TestAuthHandler to take over
                // This is crucial for seamless integration testing with custom auth.
                if (context.Request.Headers.ContainsKey("X-Test-User"))
                {
                    var userUsername = context.Request.Headers["X-Test-User-Username"].FirstOrDefault();
                    var userRole = context.Request.Headers["X-Test-User-Role"].FirstOrDefault() ?? "User";

                    if (!string.IsNullOrEmpty(userUsername))
                    {
                        var claims = new List<Claim>
                                {
                                    new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()), // Placeholder ID
                                    new Claim(ClaimTypes.Email, userUsername),
                                    new Claim(ClaimTypes.Role, userRole)
                                };
                        var identity = new ClaimsIdentity(claims, "TestScheme");
                        context.HttpContext.Items["TestUser"] = new ClaimsPrincipal(identity);
                        context.NoResult(); // Signal that no further authentication is needed for this request
                        return Task.CompletedTask;
                    }
                }
                return Task.CompletedTask;
            }
        };
    });
builder.Services.AddAuthorization();

// 5. MVC / API
builder.Services.AddControllers();

// 6. OpenAPI & Swagger 
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();      // <-- add this
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "AuthApi", Version = "v1" });

    // Define the BearerAuth scheme
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = ParameterLocation.Header,
            },
            new List<string>()
        }
    });
});

// 7. Health checks & CORS
builder.Services.AddHealthChecks();
builder.Services.AddCors(cors => cors.AddDefaultPolicy(p =>
    p.AllowAnyOrigin()
     .AllowAnyHeader()
     .AllowAnyMethod()));

var app = builder.Build();

// 8. Environment-specific middleware
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Auth API V1");
        c.RoutePrefix = string.Empty; // serve UI at root (http://localhost:5000/) 
    });

    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/error");
}

// 9. Cross‐cutting middleware
app.UseSerilogRequestLogging();
app.UseMiddleware<ExceptionLoggingMiddleware>();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseCors();
app.MapHealthChecks("/health");

app.MapControllers();

// 10. Auto-migrate
using var scope = app.Services.CreateScope();
scope.ServiceProvider.GetRequiredService<DataContext>().Database.Migrate();

app.Run();
