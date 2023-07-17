using EasilyNET.PropertyInjection;
using Microsoft.Extensions.DependencyInjection;
using PropertyInjectionWeb.Example.Controllers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

//添加 AddPropertyInjectionAsServices
builder.Services.AddControllers().AddPropertyInjectionAsServices();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//添加
builder.Host.UseDefaultPropertyInjection();
builder.Services.AddScoped<ITest, Test>();
builder.Services.AddScoped<ITest1, Test1>();
builder.Services.AddScoped(typeof(IUserService<>), typeof(UserService<>));
builder.Services.AddScoped(typeof(UserService<>));
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();