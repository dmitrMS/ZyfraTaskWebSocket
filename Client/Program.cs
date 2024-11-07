using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WebSocketClient
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

            while (true)
            {
                using (var client = new ClientWebSocket())
                {
                    Uri serverUri = new Uri("ws://localhost:5000");

                    try
                    {
                        await client.ConnectAsync(serverUri, cancellationToken);
                        Console.WriteLine("Подключено к серверу!");

                        // Периодическая проверка доступности сервера
                        var receiveTask = ReceiveMessages(client, cancellationToken);
                        var keepAliveTask = SendPing(client, cancellationToken);

                        await Task.WhenAny(receiveTask, keepAliveTask);

                        // Обработать разрыв связи
                        if (client.State != WebSocketState.Open)
                        {
                            Console.WriteLine("Соединение разорвано, попытка переподключения...");
                            await Task.Delay(5000); // Задержка перед повторным подключением
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Ошибка соединения: {ex.Message}. Повторная попытка через 5 секунд...");
                        await Task.Delay(5000);
                    }
                }
            }
        }

        private static async Task ReceiveMessages(ClientWebSocket client, CancellationToken cancellationToken)
        {
            var buffer = new byte[1024];

            while (client.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var result = await client.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Сервер закрыл соединение", cancellationToken);
                        Console.WriteLine("Соединение закрыто сервером.");
                    }
                    else
                    {
                        string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        Console.WriteLine("Получено сообщение от сервера: " + message);
                    }
                }
                catch (WebSocketException ex)
                {
                    Console.WriteLine($"Ошибка получения данных: {ex.Message}");
                    break;
                }
            }
        }

        private static async Task SendPing(ClientWebSocket client, CancellationToken cancellationToken)
        {
            while (client.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // Отправка "пинг"-сообщения каждые 30 секунд
                    await Task.Delay(30000, cancellationToken);

                    if (client.State == WebSocketState.Open)
                    {
                        var buffer = Encoding.UTF8.GetBytes("ping");
                        await client.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, cancellationToken);
                        Console.WriteLine("Пинг отправлен.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка отправки пинга: {ex.Message}");
                    break;
                }
            }
        }
    }
}
