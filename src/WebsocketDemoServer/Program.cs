using Microsoft.Extensions.Caching.Memory;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using WebsocketDemoServer.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMemoryCache();

//builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

var webSocketOptions = new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromSeconds(30)
};

app.UseWebSockets(webSocketOptions);

var jsonSerializerOptions = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
};

app.MapMethods("/deviceState", new[] { "GET" }, (HttpContext context, IMemoryCache memoryCache) =>
{
    if (memoryCache.TryGetValue<DeviceState>("deviceState", out var deviceState))
    {
        return Results.Ok(deviceState);
    }

    return Results.NoContent();
});

app.Map("/ws", async (
    HttpContext context,
    IMemoryCache memoryCache,
    CancellationToken cancellationToken,
    IHostApplicationLifetime hostApplicationLifetime) =>
{
    var keepAliveTimeout = TimeSpan.FromSeconds(30);

    using var timeoutCancellationTokenSource = new CancellationTokenSource(keepAliveTimeout);
    using var linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, hostApplicationLifetime.ApplicationStopping, timeoutCancellationTokenSource.Token);

    app.Logger.LogInformation("Websocket - New connection");

    try
    {
        var buffer = new byte[1024 * 4];

        if (context.WebSockets.IsWebSocketRequest)
        {
            using var webSocket = await context.WebSockets.AcceptWebSocketAsync();

            var receiveResult = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), linkedCancellationTokenSource.Token);

            while (!receiveResult.CloseStatus.HasValue)
            {
                if (receiveResult.MessageType == WebSocketMessageType.Close)
                {
                    app.Logger.LogInformation("Websocket - Close message received");
                    break;
                }

                timeoutCancellationTokenSource.CancelAfter(keepAliveTimeout);

                var json = Encoding.UTF8.GetString(buffer, 0, receiveResult.Count);
                app.Logger.LogInformation($"Websocket - Message received, {json}");

                var deviceState = JsonSerializer.Deserialize<DeviceState>(json, jsonSerializerOptions);
                memoryCache.Set("deviceState", deviceState);

                Array.Clear(buffer, 0, buffer.Length);

                if (webSocket.State == WebSocketState.Open)
                {
                    await webSocket.SendAsync(Encoding.ASCII.GetBytes($"Received - {DateTime.Now}"), WebSocketMessageType.Text, true, linkedCancellationTokenSource.Token);
                }
                else
                {
                    app.Logger.LogError("Websocket - Can not send received message");
                }

                receiveResult = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), linkedCancellationTokenSource.Token);
            }

            if (receiveResult.CloseStatus.HasValue)
            {
                await webSocket.CloseAsync(
                    receiveResult.CloseStatus.Value,
                    receiveResult.CloseStatusDescription,
                    linkedCancellationTokenSource.Token);
            }

            app.Logger.LogInformation("Websocket - Close connection");
        }
        else
        {
            app.Logger.LogError("Websocket - Invalid request");
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
        }
    }
    catch (OperationCanceledException)
    {
        app.Logger.LogInformation("Websocket - Canceled");
    }
    catch (Exception exception)
    {
        app.Logger.LogError(exception, "Websocket - Exception");
    }
});

app.UseDefaultFiles();
app.UseStaticFiles();

await app.RunAsync();
