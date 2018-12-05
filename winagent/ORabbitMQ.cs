using RabbitMQ.Client;
using System;
using System.Text;

namespace plugin
{
    [PluginAttribute(PluginName="RabbitMQ")]
    public class ORabbitMQ : IOutputPlugin {
        public void Execute(string json, string[] options)
        {

            // options[0] => Hostmane
            // options[1] => Username
            // options[2] => Password
            // options[3] => Queue name
            var factory = new ConnectionFactory() { HostName = options[0], UserName = options[1], Password = options[2] };
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.QueueDeclare(queue: options[3],
                                        durable: false,
                                        exclusive: false,
                                        autoDelete: false,
                                        arguments: null);

                //string message = "Hello World!";
                var body = Encoding.UTF8.GetBytes(json);



                channel.BasicPublish(exchange: "",
                                        routingKey: options[3],
                                        basicProperties: null,
                                        body: body);
            }
        }
    }
}