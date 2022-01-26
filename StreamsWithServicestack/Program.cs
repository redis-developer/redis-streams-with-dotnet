using ServiceStack.Redis;
using StreamsWithServicestack;

var manager = new BasicRedisClientManager("localhost");
var asyncClient = await manager.GetClientAsync();

var tokenSource = new CancellationTokenSource();
var token = tokenSource.Token;

try
{
    await asyncClient.CustomAsync("XGROUP", "CREATE", "telemetry", "avg", "0-0", "MKSTREAM");
}
catch (Exception ex)
{
    Console.WriteLine(ex);
}

var writeTask = Producer.Produce(manager, token);
var readTask = Consumer.Consume(manager, token);
var groupReadTask = Consumer.ConsumeFromGroup(manager, token);

await Task.WhenAll(writeTask, readTask, groupReadTask);
