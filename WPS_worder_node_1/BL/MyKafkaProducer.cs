using Confluent.Kafka;
using Newtonsoft.Json;
using WPS_worder_node_1.Controllers;
using WPS_worder_node_1.Modal;

namespace WPS_worder_node_1.BL
{
    public class MyKafkaProducer
    {
        private static ProducerConfig config { get; } = new ProducerConfig
        {
            BootstrapServers = "localhost:9092",
            AllowAutoCreateTopics = true,
            Acks = Acks.All
        };
        public static void NotifyKafka(ServerModal serverModal, HealthCheckerModal healthCheckerModal, bool isTestEmail)
        {

            using (
            IProducer<Null, String> producer = new ProducerBuilder<Null, string>(MyKafkaProducer.config).Build())
            {
                try
                {
                    producer.Produce("Notifier",
                        new Message<Null, string> { Value = JsonConvert.SerializeObject(new { serverModal, healthCheckerModal, isTestEmail }) });
                    Console.WriteLine($"kafka message is sent.");
                }
                catch (ProduceException<Null, string> e)
                {
                    Console.WriteLine($"Delivery failed: {e.Error.Reason}");
                };

                producer.Flush(TimeSpan.FromSeconds(10));
            }


        }

        public static void NotifyKafkaAPIFlow(FlowExecutionResult result, int client_id, int flow_id, bool isError)
        {
            using (
            IProducer<Null, String> producer = new ProducerBuilder<Null, string>(MyKafkaProducer.config).Build())
            {
                try
                {
                    producer.Produce("Notifier",
                        new Message<Null, string> { Value = JsonConvert.SerializeObject(new {result = result, client_id = client_id, flow_id = flow_id, isError = isError} )});
                    Console.WriteLine($"kafka message is sent.");
                }
                catch (ProduceException<Null, string> e)
                {
                    Console.WriteLine($"Delivery failed: {e.Error.Reason}");
                };

                producer.Flush(TimeSpan.FromSeconds(10));
            }
        }

        public static void NotifyAdmin(string? message)
        {

            using (
            IProducer<Null, String> producer = new ProducerBuilder<Null, string>(MyKafkaProducer.config).Build())
            {
                try
                {
                    producer.Produce("Notifier",
                        new Message<Null, string> { Value = message });
                    Console.WriteLine($"kafka message is sent.");
                }
                catch (ProduceException<Null, string> e)
                {
                    Console.WriteLine($"Delivery failed: {e.Error.Reason}");
                };

                producer.Flush(TimeSpan.FromSeconds(10));
            }


        }
    }
}
