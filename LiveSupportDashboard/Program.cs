using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using LiveSupportDashboard.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

var connString = builder.Configuration.GetConnectionString("DefaultConnection")
                 ?? throw new InvalidOperationException("Missing connection string 'DefaultConnection'.");

builder.Services.AddNpgsqlDataSource(connString); // pooled data source for ADO.NET
builder.Services.AddScoped<ITicketRepository, TicketRepository>();

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
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapHealthChecks("/health");
app.MapControllers();

app.Run();