using Microsoft.EntityFrameworkCore;
using LiveSupportDashboard;

var builder = WebApplication.CreateBuilder(args);

// Advanced PostgreSQL connection setup with pooling, logging, and migration readiness
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsqlOptions =>
        {
            npgsqlOptions.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
            // Ready for future migrations
        })
    .EnableDetailedErrors()
    .EnableSensitiveDataLogging()
);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Optional: Ensure database is created and ready for migrations (remove in production)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.EnsureCreated();
}

app.Run();