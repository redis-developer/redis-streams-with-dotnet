// See https://aka.ms/new-console-template for more information

using System.Globalization;
using StackExchange.Redis;

var tokenSource = new CancellationTokenSource();
var token = tokenSource.Token;
var muxer = ConnectionMultiplexer.Connect("localhost");
var db = muxer.GetDatabase();

const string streamName = "telemetry";
const string groupName = "avg";

if (!(await db.KeyExistsAsync(streamName)) || 
    (await db.StreamGroupInfoAsync(streamName)).All(x=>x.Name!=groupName))
{
    await db.StreamCreateConsumerGroupAsync(streamName, groupName, "0-0", true);
}

var producerTask = Task.Run(async () =>
{
    var random = new Random();
    while (!token.IsCancellationRequested)
    {
        await db.StreamAddAsync(streamName,
            new NameValueEntry[]
                {new("temp", random.Next(50, 65)), new NameValueEntry("time", DateTimeOffset.Now.ToUnixTimeSeconds())});
        await Task.Delay(2000);
    }
});

Dictionary<string, string> ParseResult(StreamEntry entry) => entry.Values.ToDictionary(x => x.Name.ToString(), x => x.Value.ToString());

var readTask = Task.Run(async () =>
{
    while (!token.IsCancellationRequested)
    {
        var result = await db.StreamRangeAsync(streamName, "-", "+", 1, Order.Descending);
        if (result.Any())
        {
            var dict = ParseResult(result.First());
            Console.WriteLine($"Read result: temp {dict["temp"]} time: {dict["time"]}");
        }

        await Task.Delay(1000);
    }
});

double count = default;
double total = default;

var consumerGroupReadTask = Task.Run(async () =>
{
    string id = string.Empty;
    while (!token.IsCancellationRequested)
    {
        if (!string.IsNullOrEmpty(id))
        {
            await db.StreamAcknowledgeAsync(streamName, groupName, id);
            id = string.Empty;
        }
        var result = await db.StreamReadGroupAsync(streamName, groupName, "avg-1", ">", 1);
        if (result.Any())
        {
            id = result.First().Id;
            count++;
            var dict = ParseResult(result.First());
            total += double.Parse(dict["temp"]);
            Console.WriteLine($"Group read result: temp: {dict["temp"]}, time: {dict["time"]}, current average: {total/count:00.00}");
        }
        await Task.Delay(1000);
    }
});

tokenSource.CancelAfter(TimeSpan.FromSeconds(20));
await Task.WhenAll(producerTask, readTask, consumerGroupReadTask);