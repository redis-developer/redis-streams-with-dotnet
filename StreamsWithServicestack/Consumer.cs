using ServiceStack.Redis;

namespace StreamsWithServicestack;

public static class Consumer
{
    public static async Task Consume(IRedisClientsManagerAsync manager, CancellationToken token)
    {
        var client = await manager.GetClientAsync(token);
        while (!token.IsCancellationRequested)
        {
            string id;
            var result = await client.CustomAsync("XREAD", "COUNT", 1, "BLOCK", 20000, "STREAMS", "telemetry", "$");
            var fvp = ParseStreamResult(result, out id);
            Console.WriteLine($"read: result {id} - temp: {fvp["temp"]} time: {fvp["time"]}");
        }
    }

    public static async Task ConsumeFromGroup(IRedisClientsManagerAsync manager, CancellationToken token)
    {
        var client = await manager.GetClientAsync(token);
        var total = 0;
        var num = 0;
        while (!token.IsCancellationRequested)
        {
            string id;
            var result = await client.CustomAsync("XREADGROUP", "GROUP", "avg", "avg-1", "COUNT", "1", "BLOCK",
                20000, "STREAMS", "telemetry", ">");
            var fvp = ParseStreamResult(result, out id);
            total += int.Parse(fvp["temp"]);
            num++;
            Console.WriteLine(
                $"Group-read: result {id} - temp: {fvp["temp"]} time: {fvp["time"]}, current average: {total / num}");
            await client.CustomAsync("XACK", "telemetry", "avg", id);
        }
    }

    public static IDictionary<string, string> ParseStreamResult(RedisText text, out string id)
    {
        var result = new Dictionary<string, string>();

        var fieldValPairs = text.Children[0].Children[1].Children[0].Children[1].Children;
        id = text.Children[0].Children[1].Children[0].Children[0].Text;
        for (var i = 0; i < fieldValPairs.Count; i += 2)
        {
            result.Add(fieldValPairs[i].Text, fieldValPairs[i+1].Text);
        }

        return result;
    }
}