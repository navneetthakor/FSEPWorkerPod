using Confluent.Kafka;
using Newtonsoft.Json;
using WPS_worder_node_1.Controllers;
using WPS_worder_node_1.Modal;

namespace WPS_worder_node_1.BL
{
    public class MyKafkaProducer
    {
        //get env variable 
        static string? kafka_Bootstrap_server = Environment.GetEnvironmentVariable("kafka_Bootstrap_server");
        private static ProducerConfig config { get; } = new ProducerConfig
        {
            
            BootstrapServers = $"{kafka_Bootstrap_server}",
            AllowAutoCreateTopics = true,
            Acks = Acks.All
        };
        public static void NotifyKafka(ServerModal serverModal, HealthCheckerModal healthCheckerModal, TypeOfEmail emailType)
        {

            using (
            IProducer<Null, String> producer = new ProducerBuilder<Null, string>(MyKafkaProducer.config).Build())
            {
                try
                {
                    producer.Produce("Notifier",
                        new Message<Null, string> { Value = JsonConvert.SerializeObject(new { serverModal = serverModal, healthCheckerModal = healthCheckerModal, emailType = emailType }) });
                    Console.WriteLine($"kafka message is sent.");
                }
                catch (ProduceException<Null, string> e)
                {
                    Console.WriteLine($"Delivery failed: {e.Error.Reason}");
                };

                producer.Flush(TimeSpan.FromSeconds(10));
            }


        }

        public static void NotifyKafkaAPIFlow(FlowExecutionResult result, string client_id, string flow_name, TypeOfEmail emailType)
        {
            using (
            IProducer<Null, String> producer = new ProducerBuilder<Null, string>(MyKafkaProducer.config).Build())
            {
                try
                {
                    ServerModal sm = new ServerModal()
                    {
                        Client_id = client_id,
                        Api_flow_name = flow_name,
                    };
                    producer.Produce("Notifier",
                        new Message<Null, string> { Value = JsonConvert.SerializeObject(new { serverModal = sm, flowExecutionResult = result, emailType= emailType } )});
                    Console.WriteLine($"kafka message is sent.");
                }
                catch (ProduceException<Null, string> e)
                {
                    Console.WriteLine($"Delivery failed: {e.Error.Reason}");
                };

                producer.Flush(TimeSpan.FromSeconds(10));
            }
        }

        public static void NotifyAdmin(string? message, TypeOfEmail emailType)
        {

            using (
            IProducer<Null, String> producer = new ProducerBuilder<Null, string>(MyKafkaProducer.config).Build())
            {
                try
                {
                    producer.Produce("Notifier",
                        new Message<Null, string> { Value = JsonConvert.SerializeObject(new { message = message, emailType = emailType}) });
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

    public enum TypeOfEmail
    {
        EndpointTestEmail,
        EndpointErrorEmail,
        EndpointSuccessEmail,
        APIFlowTestEmail,
        APIFlowErrorEmail,
        APIFlowSuccessEmail,
        AdminEmail
    }
}
