
using CameraChecker;
using RestSharp;
using RestSharp.Authenticators.Digest;
using System.Net;

Console.BackgroundColor = ConsoleColor.Black;
Console.ForegroundColor = ConsoleColor.White;

var ipListFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ip.txt");

if (!File.Exists(ipListFilePath))
{
    Console.WriteLine("ip.txt file not found");
    Console.ReadKey(true);
    return;
}

var list = await File.ReadAllLinesAsync(ipListFilePath);

var ipList = list.ToList()
    .Select(s =>
    {
        var parsed = IPAddress.TryParse(s, out IPAddress? ip);
        return new { parsed, ip };
    })
    .Where(p => p.parsed)
    .Select(p => p.ip)
    .Distinct()
    .OrderBy(ip => ip.ToString(), new IPComparer())
    .ToList();

Console.Write("Username: ");
var username = Console.ReadLine();

Console.Write("Password: ");
Console.ForegroundColor = ConsoleColor.Black;
var password = Console.ReadLine();

Console.Clear();
Console.ResetColor();

Console.WriteLine(string.Join(Environment.NewLine, ipList));

ParallelOptions parallelOptions = new()
{
    MaxDegreeOfParallelism = 32
};

object obj = new object();
var online = 0;
var offline = 0;
var authErr = 0;

await Parallel.ForEachAsync(ipList, parallelOptions, async (ip, token) =>
{
    var client = new RestClient($"http://{ip}:8080/ISAPI/System");

    client.Authenticator = new DigestAuthenticator(username, password);
    var request = new RestRequest("deviceInfo", Method.Get);
    request.AddHeader("Content-Type", "application/xml");

    var cLeft = ip.ToString().Length;
    var cTop = ipList.IndexOf(ip);

    try
    {
        lock (obj)
        {
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.SetCursorPosition(cLeft, cTop);
            Console.Write("\tChecking...");
        }

        var response = await client.ExecuteAsync(request);


        lock (obj)
        {
            online++;

            if (response.StatusCode == HttpStatusCode.OK)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.SetCursorPosition(cLeft, cTop);
                Console.Write("\tOnline     ");
            }
            else
            {
                authErr++;
                Console.ForegroundColor = ConsoleColor.Green;
                Console.SetCursorPosition(cLeft, cTop);
                Console.Error.Write("\tOnline");
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.Error.Write(" (Auth Err)");
            }
        }

    }
    catch (global::System.Exception)
    {
        lock (obj)
        {
            offline++;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.SetCursorPosition(cLeft, cTop);
            Console.Write("\tOffline    ");
        }

    }
});

Console.SetCursorPosition(0, ipList.Count + 1);

Console.ResetColor();
Console.Write("Total:\t\t");
Console.WriteLine(ipList.Count);

Console.ResetColor();
Console.Write("Online:\t\t");
Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine(online);

Console.ResetColor();
Console.Write("Auth Err:\t");
Console.ForegroundColor = ConsoleColor.DarkYellow;
Console.WriteLine(authErr);

Console.ResetColor();
Console.Write("Offline:\t");
Console.ForegroundColor = ConsoleColor.Red;
Console.WriteLine(offline);

Console.ReadKey(true);