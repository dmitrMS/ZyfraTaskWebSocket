// Server.cs
using System;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WebSocketServer
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var httpListener = new HttpListener();
            httpListener.Prefixes.Add("http://localhost:5000/");
            httpListener.Start();
            Console.WriteLine("Сервер запущен и ожидает подключений по адресу ws://localhost:5000");

            while (true)
            {
                var context = await httpListener.GetContextAsync();
                if (context.Request.IsWebSocketRequest)
                {
                    var wsContext = await context.AcceptWebSocketAsync(null);
                    Console.WriteLine("Клиент подключен!");

                    await HandleClient(wsContext.WebSocket);
                }
                else
                {
                    context.Response.StatusCode = 400;
                    context.Response.Close();
                }
            }
        }

        private static async Task HandleClient(WebSocket webSocket)
        {
            while (webSocket.State == WebSocketState.Open)
            {
                Console.Write("Введите сообщение для клиента: ");
                string message = Console.ReadLine();
                byte[] buffer = Encoding.UTF8.GetBytes(message);

                await webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
                Console.WriteLine("Сообщение отправлено клиенту.");
            }
        }
    }
}
