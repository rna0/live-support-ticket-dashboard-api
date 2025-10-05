using System.Net.Mime;
using System.Text;
using LiveSupportDashboard.Domain;
using LiveSupportDashboard.Domain.Contracts;
using LiveSupportDashboard.Hubs;
using LiveSupportDashboard.Infrastructure;
using LiveSupportDashboard.Infrastructure.Services;
using LiveSupportDashboard.Services.Implementations;
using LiveSupportDashboard.Services.Interfaces;
using LiveSupportDashboard.Services.Validations;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

var connString = builder.Configuration.GetConnectionString("DefaultConnection")
                 ?? throw new InvalidOperationException("Missing connection string 'DefaultConnection'.");

builder.Services.AddNpgsqlDataSource(connString);
builder.Services.AddScoped<ITicketRepository, TicketRepository>();
builder.Services.AddScoped<IAgentRepository, AgentRepository>();
builder.Services.AddScoped<ISessionRepository, SessionRepository>();
builder.Services.AddScoped<IMessageRepository, MessageRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddSingleton<ISqlQueryLoader, SqlQueryLoader>();

// Validation services
builder.Services.AddScoped<IValidationService, ValidationService>();
builder.Services.AddSingleton<IValidation<CreateTicketRequest>, CreateTicketRequestValidation>();
builder.Services.AddSingleton<IValidation<UpdateTicketStatusRequest>, UpdateTicketStatusRequestValidation>();
builder.Services.AddScoped<IValidation<AssignTicketRequest>, AssignTicketRequestValidation>();
builder.Services.AddScoped<IValidation<Guid?>, AgentAssignmentValidation>();
builder.Services.AddSingleton<IValidation<Ticket>, TicketBusinessRuleValidation>();
builder.Services.AddSingleton<IValidation<TicketQueryParameters>, TicketQueryValidation>();

// Token service
builder.Services.AddScoped<ITokenService, TokenService>();

// JWT Authentication
var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwtSettings["Key"] ?? throw new InvalidOperationException("JWT Key is missing"));

builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ClockSkew = TimeSpan.Zero
        };

        // Support JWT in SignalR
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/signalr/hubs"))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// SignalR services
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
});

// Notification service
builder.Services.AddSingleton<INotificationService, SignalRNotificationService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("TicketAppPolicy", policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();

        if (allowedOrigins != null)
            policy.WithOrigins(allowedOrigins)
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials();
    });
});

builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(o =>
    {
        // Use ProblemDetails for validation errors
        o.InvalidModelStateResponseFactory = ctx =>
        {
            var problem = new ValidationProblemDetails(ctx.ModelState)
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Request validation failed"
            };
            return new BadRequestObjectResult(problem)
            {
                ContentTypes = { MediaTypeNames.Application.ProblemJson }
            };
        };
    });

builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks().AddNpgSql(connString, name: "postgres");
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Live Support Ticket Dashboard API",
        Version = "v1",
        Description = "Backend API for the Live Support Ticket Dashboard",
    });

    // Add JWT Authentication to Swagger
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
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

app.UseExceptionHandler();

app.UseCors("TicketAppPolicy");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => { c.SwaggerEndpoint("/swagger/v1/swagger.json", "Live Support Ticket Dashboard API v1"); });
}

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/api/health");
app.MapControllers();

// SignalR Hub mapping
app.MapHub<LiveSupportHub>("/signalr/hubs");

app.Run();
