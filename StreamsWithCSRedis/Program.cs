// See https://aka.ms/new-console-template for more information

using CSRedis;

var cancellationTokenSource = new CancellationTokenSource();
var token = cancellationTokenSource.Token;

var client = new CSRedisClient("localhost");
if (!client.Exists("stream") || client.XInfoStream("stream").groups == 0)
{
    client.XGroupCreate("stream", "avg", "$", MkStream: true);
}

var writeThread = new Thread(() =>
{
    var writeClient = new CSRedisClient("localhost");
    var random = new Random();
    while (!token.IsCancellationRequested)
    {
        writeClient.XAdd("stream", new (string, string)[]{new ("temp", random.Next(50,65).ToString()), new ("time", DateTimeOffset.Now.ToUnixTimeSeconds().ToString())});
        Thread.Sleep(2000);
    }
});

Func<(string key, (string id, string[] items)[] data), Dictionary<string,string>> parse = delegate((string key, (string id, string[] items)[] data) streamResult)
{
    var message = streamResult.data.First().items;
    var result = new Dictionary<string, string>();
    for (var i = 0; i < message.Length; i += 2)
    {
        result.Add(message[i], message[i+1]);
    }

    return result;
};



var readThread = new Thread(() =>
{
    var readClient = new CSRedisClient("localhost");
    while (!token.IsCancellationRequested)
    {
        var result = readClient.XRead(1, 5000, new (string key, string id)[] {new("stream", "$")});
        if (result != null)
        {
            var dictionary = parse(result[0]);
            Console.WriteLine($"Most recent message, time: {dictionary["time"]} temp: {dictionary["temp"]}");
        }
    }
});

var total = 0;
var count = 0;
var groupReadThread = new Thread(() =>
{
    var groupReadClient = new CSRedisClient("localhost");
    var id = string.Empty;
    while (!token.IsCancellationRequested)
    {
        if (!string.IsNullOrEmpty(id))
        {
            client.XAck("stream", "avg", id);
        }
        var result =
            groupReadClient.XReadGroup("avg", "avg-1", 1, 5000, new (string key, string id)[] {new("stream", ">")});
        if (result != null)
        {
            id = result.First().data.First().id;
            var dictionary = parse(result[0]);
            if (dictionary.ContainsKey("temp"))
            {
                count++;
                total += int.Parse(dictionary["temp"]);
                double avg = (double) total / count; 
                Console.WriteLine($"Most recent group message, time: {dictionary["time"]} temp: {dictionary["temp"]} avg: {avg:00.00}");
            }
        }
    }
});

readThread.Start();
writeThread.Start();
groupReadThread.Start();

cancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(10));

readThread.Join();
writeThread.Join();
groupReadThread.Join();