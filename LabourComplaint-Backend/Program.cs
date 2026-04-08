// Program.cs
using System.Reflection;
using System.Text; //   Added for Encoding.UTF8
using FluentValidation;
using LabourComplaint_Backend.Data;
using LabourComplaint_Backend.Features.Auth.Services;
using LabourComplaint_Backend.Features.Auth.Validators;
using LabourComplaint_Backend.Features.Complaints.Services;
using LabourComplaint_Backend.Features.Complaints.Validators;
using Microsoft.AspNetCore.Authentication.JwtBearer; //   Added explicit namespace
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens; //   Added for SymmetricSecurityKey
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// FluentValidation - include Auth validators too  
builder.Services.AddValidatorsFromAssemblyContaining<ComplaintCreateDtoValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<RegisterDtoValidator>(); //   Added

// Application Services
builder.Services.AddScoped<IComplaintService, ComplaintService>();
builder.Services.AddScoped<IAuthService, AuthService>(); //   Added Auth service

//   JWT Authentication (your existing config - kept as-is)
// Program.cs - Update the AddJwtBearer block:

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
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecretKey"]
                ?? throw new InvalidOperationException("JWT SecretKey missing"))),

        // Add clock skew tolerance (handles minor server/client time differences)
        ClockSkew = TimeSpan.FromMinutes(5)
    };

    // Add detailed logging for debugging
    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            context.HttpContext.Response.Headers.Append("X-Auth-Error", context.Exception.Message);
            Console.WriteLine($"JWT Auth Failed: {context.Exception.Message}");
            Console.WriteLine($"Exception Type: {context.Exception.GetType().Name}");
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            Console.WriteLine($"JWT Validated for User: {context.Principal?.FindFirst("sub")?.Value}");
            return Task.CompletedTask;
        },
        OnChallenge = context =>
        {
            Console.WriteLine($"JWT Challenge: {context.Error}, {context.ErrorDescription}");
            return Task.CompletedTask;
        }
    };
});
//   Authorization Policies (enhanced)
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("AdminOnly", policy => policy.RequireRole("Admin")) //   Added
    .AddPolicy("InspectorOrAdmin", policy => policy.RequireRole("Inspector", "Admin")) //   Added
    .AddPolicy("DistrictScoped", policy =>
        policy.RequireAssertion(context => true)); // TODO: Implement district logic

// Controllers + Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Labour Complaint API",
        Version = "v1",
        Description = "End-to-end labor violation reporting & investigation platform with real-time chat, SLA monitoring, and audit-ready workflows.",
        Contact = new OpenApiContact
        {
            Name = "Platform Support",
            Email = "support@labourcompliance.gov"
        }
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        options.IncludeXmlComments(xmlPath);

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer {your JWT token}' below"
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

    options.CustomOperationIds(e =>
        e.ActionDescriptor.DisplayName?
            .Replace("Map", "")
            .Replace("Async", "")
            .Replace(" ", "")
            .Replace("/", "")
            .ToLowerInvariant());
});

var app = builder.Build();

// Swagger (Dev only)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Labour Complaint API v1");
        options.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List);
    });
}

app.UseHttpsRedirection();
app.UseAuthentication(); // Already in correct order
app.UseMiddleware<LabourComplaint_Backend.Middleware.BlacklistMiddleware>();
app.UseAuthorization();

app.MapControllers();
// Map Endpoints
//app.MapComplaintEndpoints();

// Health Check
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
    .ExcludeFromDescription();

app.Run();