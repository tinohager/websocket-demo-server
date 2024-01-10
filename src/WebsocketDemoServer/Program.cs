using Microsoft.AspNetCore.WebSockets;
using Microsoft.Extensions.Caching.Memory;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using WebsocketDemoServer.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMemoryCache();

builder.Services.AddWebSockets(config => { });
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseWebSockets();

var jsonSerializerOptions = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
};

app.Map("/deviceState", (HttpContext context, IMemoryCache memoryCache) =>
{
    if (memoryCache.TryGetValue<DeviceState>("deviceState", out var deviceState))
    {
        return Results.Ok(deviceState);
    }

    return Results.NoContent();
});

app.Map("/ws", async (HttpContext context, IMemoryCache memoryCache) =>
{
    try
    {
        var buffer = new byte[1024 * 4];

        if (context.WebSockets.IsWebSocketRequest)
        {
            using var webSocket = await context.WebSockets.AcceptWebSocketAsync();

            var receiveResult = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

            while (!receiveResult.CloseStatus.HasValue)
            {
                var json = Encoding.UTF8.GetString(buffer, 0, receiveResult.Count);
                Console.WriteLine(json);

                var deviceState = JsonSerializer.Deserialize<DeviceState>(json, jsonSerializerOptions);
                memoryCache.Set("deviceState", deviceState);

                Array.Clear(buffer, 0, buffer.Length);

                await webSocket.SendAsync(Encoding.ASCII.GetBytes($"Received - {DateTime.Now}"), WebSocketMessageType.Text, true, CancellationToken.None);

                receiveResult = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            }
        }
        else
        {
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
        }
    }
    catch (Exception exception)
    {
        Console.WriteLine(exception.ToString());
    }
});

app.UseDefaultFiles();
app.UseStaticFiles();


await app.RunAsync();

//app.UseAuthorization();
//app.MapControllers();

//app.Run();