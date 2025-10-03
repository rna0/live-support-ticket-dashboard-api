using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using LiveSupportDashboard.Infrastructure;
using LiveSupportDashboard.Hubs;
using LiveSupportDashboard.Services.Interfaces;
using LiveSupportDashboard.Services.Implementations;

var builder = WebApplication.CreateBuilder(args);

var connString = builder.Configuration.GetConnectionString("DefaultConnection")
                 ?? throw new InvalidOperationException("Missing connection string 'DefaultConnection'.");

builder.Services.AddNpgsqlDataSource(connString); // pooled data source for ADO.NET
builder.Services.AddScoped<ITicketRepository, TicketRepository>();

// SignalR services
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
});

// Notification service
builder.Services.AddScoped<INotificationService, SignalRNotificationService>();

// CORS for SignalR
builder.Services.AddCors(options =>
{
    options.AddPolicy("TicketaAppPolicy", policy =>
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
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseExceptionHandler();

// CORS must be before SignalR
app.UseCors("TicketaAppPolicy");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapHealthChecks("/health");
app.MapControllers();

// SignalR Hub mapping
app.MapHub<LiveSupportHub>("/hubs/livesupport");

app.Run();
