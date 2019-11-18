using System;
using System.IO;
using System.Net;
using System.Threading;

namespace WebServer.Sync
{
    class Program
    {
        static void Main(string[] args)
        {
            var listener = new HttpListener();
            listener.Prefixes.Add("http://localhost:8080/");
            listener.Start();

            Console.WriteLine("Thread HTTP server has started. Press Ctrl+C to stop.");

            try
            {
                while (true)
                {
                    var context = listener.GetContext();

                    var thread = new Thread(() => ProcessRequest(context));
                    thread.Start();
                }
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

        private static void ProcessRequest(HttpListenerContext context)
        {
            Console.WriteLine("{0} {1}", context.Request.HttpMethod, context.Request.RawUrl);

            if (context.Request.HttpMethod == "GET")
            {
                if (context.Request.RawUrl == "/")
                    SendIndexHtml(context.Response);
                else
                    SendFileNotFound(context.Response);
            }
            else
                SendMethodNotAllowed(context.Response);
        }

        private static void SendIndexHtml(HttpListenerResponse response)
        {
            Thread.Sleep(100);
            response.StatusCode = 200;

            using (var writer = new StreamWriter(response.OutputStream))
            {
                writer.WriteLine("<!DOCTYPE html>");
                writer.WriteLine("<html lang='en' xmlns='http://www.w3.org/1999/xhtml'>");
                writer.WriteLine("  <head>");
                writer.WriteLine("  <meta charset='utf-8' />");
                writer.WriteLine("  <title>Example HTTP server</title>");
                writer.WriteLine("  </head>");
                writer.WriteLine("  <body>");
                writer.WriteLine("    <p>Example HTTP server</p>");
                writer.WriteLine("  </body>");
                writer.WriteLine("</html>");
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
