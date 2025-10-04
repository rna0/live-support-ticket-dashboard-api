using System.Net.Mime;
using LiveSupportDashboard.Domain;
using LiveSupportDashboard.Domain.Contracts;
using LiveSupportDashboard.Hubs;
using LiveSupportDashboard.Infrastructure;
using LiveSupportDashboard.Services.Implementations;
using LiveSupportDashboard.Services.Interfaces;
using LiveSupportDashboard.Services.Validations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

var connString = builder.Configuration.GetConnectionString("DefaultConnection")
                 ?? throw new InvalidOperationException("Missing connection string 'DefaultConnection'.");

builder.Services.AddNpgsqlDataSource(connString); // pooled data source for ADO.NET
builder.Services.AddScoped<ITicketRepository, TicketRepository>();

// Validation services
builder.Services.AddScoped<IValidationService, ValidationService>();
builder.Services.AddScoped<IValidation<CreateTicketRequest>, CreateTicketRequestValidation>();
builder.Services.AddScoped<IValidation<UpdateTicketStatusRequest>, UpdateTicketStatusRequestValidation>();
builder.Services.AddScoped<IValidation<AssignTicketRequest>, AssignTicketRequestValidation>();
builder.Services.AddScoped<IValidation<Ticket>, TicketBusinessRuleValidation>();
builder.Services.AddScoped<IValidation<TicketQueryParameters>, TicketQueryValidation>();

// SignalR services
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
});

// Notification service
builder.Services.AddScoped<INotificationService, SignalRNotificationService>();

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
});

var app = builder.Build();

app.UseExceptionHandler();

app.UseCors("TicketAppPolicy");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => { c.SwaggerEndpoint("/swagger/v1/swagger.json", "Live Support Ticket Dashboard API v1"); });
}

app.MapHealthChecks("/health");
app.MapControllers();

// SignalR Hub mapping
app.MapHub<LiveSupportHub>("/hubs/livesupport");

app.Run();
