## Demo Client



```cs
using Websocket.Client; //https://www.nuget.org/packages/Websocket.Client

var exitEvent = new ManualResetEvent(false);
var url = new Uri("wss://domain.com/ws");

using (var client = new WebsocketClient(url))
{
    client.ReconnectTimeout = TimeSpan.FromSeconds(30);
    client.ReconnectionHappened.Subscribe(info => {
        Console.WriteLine($"Reconnection happened, type: {info.Type}");
    });

    client.MessageReceived.Subscribe(msg => Console.WriteLine($"Message received: {msg}"));
    await client.Start();

    Console.WriteLine("Send State 1");
    _ = Task.Run(() => client.Send("{ \"deviceId\": \"test\", \"state\": \"1\" }"));
    Console.WriteLine("Press any key for next");

    exitEvent.WaitOne();
}
```
