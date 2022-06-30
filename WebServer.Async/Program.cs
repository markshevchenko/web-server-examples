using System;
using System.IO;
using System.Net;
using System.Threading;
using Microsoft.VisualBasic;

namespace WebServerCallback
{
    class Program
    {
        static void Main(string[] args)
        {
            var listener = new HttpListener();
            listener.Prefixes.Add("http://localhost:8080/");
            listener.Start();

            Console.WriteLine("Async HTTP server has started. Press any key to stop.");

            try
            {
                listener.BeginGetContext(AsyncProcessRequest, listener);

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

        private static void AsyncProcessRequest(IAsyncResult ar)
        {
            var listener = (HttpListener)ar.AsyncState;
            listener.BeginGetContext(AsyncProcessRequest, listener);

            var context = listener.EndGetContext(ar);
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

        private class ResponseTimer
        {
            public HttpListenerResponse Response { get; set; }
            
            public Timer Timer { get; set; }
        }

        private static void SendIndexHtml(HttpListenerResponse response)
        {
            var delay = TimeSpan.FromMilliseconds(10);
            var never = TimeSpan.FromMilliseconds(-1);
            var state = new ResponseTimer { Response = response };
            state.Timer = new Timer(TimerCallback, state, never, never);
            state.Timer.Change(delay, never);
        }

        private static void TimerCallback(object state)
        {
            var responseTimer = (ResponseTimer)state;
            var response = responseTimer.Response;
            var timer = responseTimer.Timer;
            timer.Dispose();
            
            response.StatusCode = 200;

            using var stream = new MemoryStream();
            using var writer = new StreamWriter(stream, leaveOpen: true);
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

            var buffer = stream.ToArray();
            response.OutputStream.BeginWrite(buffer, 0, buffer.Length, AsyncWrite, response);
        }

        private static void AsyncWrite(IAsyncResult ar)
        {
            var response = (HttpListenerResponse)ar.AsyncState;

            response.OutputStream.Close();
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
