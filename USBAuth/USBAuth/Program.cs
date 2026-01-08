using DAL;
using Microsoft.EntityFrameworkCore;
using Services.Interfaces;
using Services.Services;
using System;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IPinHasher, PinHasher>();
builder.Services.AddScoped<IDeviceService, DeviceService>();
builder.Services.AddSingleton<IChallengeStore, InMemoryChallengeStore>();
builder.Services.AddScoped<IRsaSignatureVerifier, RsaSignatureVerifier>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<ITokenValidator, TokenValidator>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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
