using Microsoft.AspNetCore.WebSockets;
using System.Net;
using System.Net.WebSockets;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddWebSockets(config => { });
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseWebSockets();

app.Map("/ws", async context =>
{
    var buffer = new byte[1024 * 4];

    WebSocketReceiveResult result = null;

    if (context.WebSockets.IsWebSocketRequest)
    {
        using (var webSocket = await context.WebSockets.AcceptWebSocketAsync())
        {
            var receiveResult = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

            while (!receiveResult.CloseStatus.HasValue)
            {
                var text = Encoding.UTF8.GetString(buffer.ToArray(), 0, buffer.Length);
                Console.WriteLine(text);

                Array.Clear(buffer, 0, buffer.Length);

                await webSocket.SendAsync(Encoding.ASCII.GetBytes($"Received - {DateTime.Now}"), WebSocketMessageType.Text, true, CancellationToken.None);

                receiveResult = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            }
        }
    }
    else
    {
        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
    }
});

await app.RunAsync();

//app.UseAuthorization();
//app.MapControllers();

//app.Run();
