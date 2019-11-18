using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace TestServerPerformance
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var uris = new List<string>();
            var timeout = TimeSpan.FromSeconds(5);
            var minRequests = 100;
            var maxRequests = 1600;

            foreach (var arg in args)
            {
                if (arg.StartsWith("-t:"))
                    timeout = TimeSpan.Parse(arg.Substring(3));
                else if (arg.StartsWith("-min:"))
                    minRequests = int.Parse(arg.Substring(5));
                else if (arg.StartsWith("-max:"))
                    maxRequests = int.Parse(arg.Substring(5));
                else
                    uris.Add(arg);
            }

            if (uris.Count == 0)
            {
                await Console.Out.WriteLineAsync("Usage: tsp URI1 (URI2 ... URIn) [-t:timeout] [-min:request/second] [-max:request/second");
                return;
            }

            await Console.Out.WriteLineAsync($"Timeout: {timeout}");
            await Console.Out.WriteLineAsync($"Min requests: {minRequests}");
            await Console.Out.WriteLineAsync($"Max requests: {maxRequests}");

            await Console.Out.WriteLineAsync("+----------+----------+-------+------------+------------+");
            await Console.Out.WriteLineAsync("| Requests | Avg time | Delay |  Ok count  | Fail count |");
            await Console.Out.WriteLineAsync("+----------+----------+-------+------------+------------+");

            for (int requests = minRequests; requests < maxRequests; requests += (requests / 2))
            {
                (long avgTime, long delay, int okCount, int failCount) = await SendRequestsAsync(timeout, uris, requests);
                await Console.Out.WriteLineAsync($"| {requests,8} | {avgTime,8} | {delay,5} | {okCount,10} | {failCount,10} |");
            }

            await Console.Out.WriteLineAsync("+----------+----------+-------+------------+------------+");
        }

        private static async Task<(long avgTime, long delay, int okCount, int failCount)> SendRequestsAsync(TimeSpan timeout, IReadOnlyList<string> uris, int requests)
        {
            var delay = 1000000 / requests;
            var random = new Random();
            var tasks = new List<Task<(long time, HttpStatusCode state)>>();
            var clientHandler = new HttpClientHandler
            {
                Proxy = new WebProxy("192.168.24.130:8080")
                {
                    BypassProxyOnLocal = true,
                    UseDefaultCredentials = true,
                },
            };

            using (var client = new HttpClient(clientHandler))
            {
                client.Timeout = timeout;

                for (int i = 0; i < requests; i++)
                {
                    var uri = uris[random.Next(uris.Count)];
                    var delayTimespan = TimeSpan.FromMilliseconds(random.Next(0, 2 * delay) / 1000.0);

                    var task = MakeRquestsAsync(client, uri, delayTimespan);
                    tasks.Add(task);
                }

                var completedTasks = await Task.WhenAll(tasks);
                var avgTime = (long)completedTasks.Select(x => x.time).Average();
                var okCount = completedTasks.Where(x => x.state >= HttpStatusCode.OK && x.state < HttpStatusCode.BadRequest)
                                            .Count();
                var failCount = completedTasks.Where(x => x.state >= HttpStatusCode.BadRequest)
                                              .Count();

                return (avgTime, delay, okCount, failCount);
            }
        }

        private static async Task<(long time, HttpStatusCode state)> MakeRquestsAsync(HttpClient client, string uri, TimeSpan delay)
        {
//            await Task.Delay(delay);
            
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            try
            {
                var response = await client.GetAsync(uri);
                stopwatch.Stop();
                return (stopwatch.ElapsedMilliseconds, response.StatusCode);
            }
            catch (Exception exception)
            {
                stopwatch.Stop();
                return (stopwatch.ElapsedMilliseconds, HttpStatusCode.BadRequest);
            }
        }
    }
}
