using Hangfire;
using Hangfire.PostgreSql;
using Hangfire.Redis.StackExchange;
using Infrastructure.BackgroundTasks;
using Infrastructure.Data;
using Infrastructure.ExtensionMethods;
using Infrastructure.Interfaces;
using Infrastructure.Profiles;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();
builder.Services.AddControllers();

//Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("logs/log.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

//DataContext
builder.Services.RegisterDbContext(builder.Configuration);

//Redis
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("RedisConnection");
    options.InstanceName = "Redis cache";
});

builder.Services.AddHttpContextAccessor();

//Hangfire
builder.Services.AddHangfire(config =>
    config.UseRedisStorage(builder.Configuration.GetConnectionString("RedisConnection")));
builder.Services.AddHangfireServer();

//Swagger
builder.Services.RegisterSwagger();

//Identity
builder.Services.RegisterIdentity();

//AutoMapper
builder.Services.AddAutoMapper(typeof(AppProfile));

//Services
builder.Services.RegisterServices();

//File
builder.Services.AddScoped<IFileStorageService>(sp => 
    new FileStorageService(builder.Environment.ContentRootPath));

//Seeder
builder.Services.AddScoped<Seeder>();

//Authentication
builder.Services.RegisterAuthentication(builder.Configuration);

builder.Services.AddAuthorization();

builder.Host.UseSerilog();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var serviceProvider = scope.ServiceProvider;
    var context = serviceProvider.GetRequiredService<DataContext>();
    var seed = serviceProvider.GetRequiredService<Seeder>();
    await context.Database.MigrateAsync();
    await seed.SeedRoles();
    await seed.SeedAdmin();
    var recurringJobManager = serviceProvider.GetRequiredService<IRecurringJobManager>();

    recurringJobManager.AddOrUpdate<CalculateNewsLikesTask>(
        "calculate-news-likes",
        service => service.CalculateNewsLikes(),
        Cron.Hourly);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
