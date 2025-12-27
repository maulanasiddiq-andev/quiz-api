using System.Text;
using System.Text.Json;
using RabbitMQ.Client;

namespace QuizApi.Services
{
    public class QueueService
    {
        private readonly IConnection connection;
        public QueueService(IConnection connection)
        {
            this.connection = connection;
        }

        public async Task Publish<T>(string queue, T message)
        {
            using var channel = await connection.CreateChannelAsync();

            await channel.QueueDeclareAsync(
                queue: queue,
                durable: true,
                exclusive: false,
                autoDelete: false
            );

            var json = JsonSerializer.Serialize(message);
            var body = Encoding.UTF8.GetBytes(json);

            var props = new BasicProperties
            {
                ContentType = "application/json",
            };

            await channel.BasicPublishAsync(
                exchange: "",
                routingKey: queue,
                basicProperties: props,
                body: body,
                mandatory: false
            );
        }
    }
}