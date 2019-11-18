using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace WebServerCallback
{
    class Program
    {
        static void Main(string[] args)
        {
            var listener = new HttpListener();
            listener.Prefixes.Add("http://localhost:8080/");
            listener.Start();

            Console.WriteLine("Task HTTP server has started. Press any key to stop.");

            try
            {
                GetContextAsync(listener);
                Console.ReadKey();
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
            finally
            {
                listener.Stop();
            }
        }

        private static async void GetContextAsync(HttpListener listener)
        {
            await Task.Yield();
            
            var context = await listener.GetContextAsync();

            GetContextAsync(listener);

            await Console.Out.WriteLineAsync($"{context.Request.HttpMethod} {context.Request.RawUrl}");

            if (context.Request.HttpMethod == "GET")
            {
                if (context.Request.RawUrl == "/")
                    await SendIndexHtmlAsync(context.Response);
                else
                    SendFileNotFound(context.Response);
            }
            else
                SendMethodNotAllowed(context.Response);
        }

        private static async Task SendIndexHtmlAsync(HttpListenerResponse response)
        {
            await Task.Delay(100);
            response.StatusCode = 200;

            using (var writer = new StreamWriter(response.OutputStream))
            {
                await writer.WriteLineAsync("<!DOCTYPE html>");
                await writer.WriteLineAsync("<html lang='en' xmlns='http://www.w3.org/1999/xhtml'>");
                await writer.WriteLineAsync("  <head>");
                await writer.WriteLineAsync("  <meta charset='utf-8' />");
                await writer.WriteLineAsync("  <title>Example HTTP server</title>");
                await writer.WriteLineAsync("  </head>");
                await writer.WriteLineAsync("  <body>");
                await writer.WriteLineAsync("    <p>Example HTTP server</p>");
                await writer.WriteLineAsync("  </body>");
                await writer.WriteLineAsync("</html>");
            }
        }

        private static void SendFileNotFound(HttpListenerResponse response)
        {
            response.StatusCode = 404;
            response.OutputStream.Close();
        }

        private static void SendMethodNotAllowed(HttpListenerResponse response)
        {
            response.StatusCode = 405;
            response.OutputStream.Close();
        }
    }
}
