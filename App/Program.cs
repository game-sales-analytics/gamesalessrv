using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using App;
using App.Extensions;
using App.DB;
using App.DB.Repository;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();

builder.Services.AddRedis(builder.Configuration);
builder.Services.AddGrpc();
builder.Services.AddDbContext<GameSalesContext>(options =>
{
    options
    .UseNpgsql(PostgreSQL.BuildPostgreSQLConnectionString(builder.Configuration))
    .UseSnakeCaseNamingConvention();
});
builder.Services.AddSingleton<GameSalesRepository>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    services.GetRequiredService<GameSalesContext>().Database.EnsureCreated();
}

app.MapGrpcService<MainService>();

app.Run();
