using Microsoft.AspNetCore.WebSockets;
using Microsoft.Extensions.Caching.Memory;
using System.Net;
using System.Net.WebSockets;
using System.Text;

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

app.Map("/", async (HttpContext context, IMemoryCache memoryCache) =>
{
    memoryCache.TryGetValue("test", out string test);

    return Results.Ok(test);
});

app.Map("/ws", async (HttpContext context, IMemoryCache memoryCache) =>
{
    try
    {
        var buffer = new byte[1024 * 4];

        WebSocketReceiveResult result = null;

        if (context.WebSockets.IsWebSocketRequest)
        {
            using var webSocket = await context.WebSockets.AcceptWebSocketAsync();

            var receiveResult = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

            while (!receiveResult.CloseStatus.HasValue)
            {
                var text = Encoding.UTF8.GetString(buffer.ToArray(), 0, buffer.Length);
                Console.WriteLine(text);
                memoryCache.Set("test", text);

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

await app.RunAsync();

//app.UseAuthorization();
//app.MapControllers();

//app.Run();

static void HandleMapTest1(IApplicationBuilder app)
{
    

    app.Run(async context =>
    {
        

        await context.Response.WriteAsync("Map Test 1");
    });
}