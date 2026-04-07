// Program.cs
using System.Reflection;
using FluentValidation;
using LabourComplaint_Backend.Data;
//using LabourComplaint_Backend.Features.Complaints.Endpoints;
using LabourComplaint_Backend.Features.Complaints.Services;
using LabourComplaint_Backend.Features.Complaints.Validators;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<ComplaintCreateDtoValidator>();

// Application Services
builder.Services.AddScoped<IComplaintService, ComplaintService>();

// Authentication & Authorization
builder.Services.AddAuthentication().AddJwtBearer(options =>
{
    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
            System.Text.Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key missing")))
    };
});

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("DistrictScoped", policy =>
        policy.RequireAssertion(context => true)); // TODO: Implement district logic

// Swashbuckle Swagger Setup
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // API Info
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

    // XML Comments (auto-generated from <summary> tags)
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        options.IncludeXmlComments(xmlPath);

    // JWT Bearer Auth in Swagger UI
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

    // Clean operation IDs for better endpoint names in docs
    options.CustomOperationIds(e =>
        e.ActionDescriptor.DisplayName?
            .Replace("Map", "")
            .Replace("Async", "")
            .Replace(" ", "")
            .Replace("/", "")
            .ToLowerInvariant());
});

var app = builder.Build();

// Swagger Middleware (Development Only)
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
app.UseAuthentication();
app.UseAuthorization();

// Map Endpoints
//app.MapComplaintEndpoints();

// Health Check
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
    .ExcludeFromDescription(); // Hide from Swagger docs

app.Run();