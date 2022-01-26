using ServiceStack.Redis;

namespace StreamsWithServicestack;

public static class Producer
{
    public static async Task Produce(BasicRedisClientManager manager, CancellationToken token)
    {
        var client = await manager.GetClientAsync(token);
        var random = new Random();
        while (!token.IsCancellationRequested)
        {
            await client.CustomAsync("XADD", "telemetry", "*", "temp",random.Next(50,65), "time", DateTimeOffset.Now.ToUnixTimeSeconds());
            await Task.Delay(10000, token);
        }
    }
}