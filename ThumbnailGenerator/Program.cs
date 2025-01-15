using System.Collections.Concurrent;
using System.Threading.Channels;
using ThumbnailGenerator.Models;
using ThumbnailGenerator.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddScoped<ImageService>();

builder.Services.AddSingleton(_ =>
{
    var channel = Channel.CreateBounded<ThumbnailGenerationJob>(new BoundedChannelOptions(100)
    {
        FullMode = BoundedChannelFullMode.Wait
    });

    return channel;
});
builder.Services.AddSingleton<ConcurrentDictionary<string, ThumbnailGenerationStatus>>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
