using Hangfire;
using Microsoft.AspNetCore.Mvc;
using WPS_worder_node_1.BL;
using WPS_worder_node_1.Modal;
using WPS_worder_node_1.Modal.Enums;


namespace WPS_worder_node_1.Repositories
{
    public class MySingleEndpointService
    {
        private readonly IRecurringJobManager recurringJobManager;

        public MySingleEndpointService(IRecurringJobManager recurringJobManager)
        {
            this.recurringJobManager = recurringJobManager;
        }
        public void InvokCheck(ServerModal serverModal)
        {
            Console.WriteLine("Invoking MySingleEndpointService HealthCheck");
            

                //check it's health
                HealthCheckerModal healthCheckModal = HealthChecker.CheckHealthAsync(serverModal).GetAwaiter().GetResult();
                //if error then notify to kafka 
                if (healthCheckModal.IsError)
                {
                    // notify to kafka
                    MyKafkaProducer.NotifyKafka(serverModal, healthCheckModal, TypeOfEmail.EndpointErrorEmail);

                // stop this recurrant job
                recurringJobManager.RemoveIfExists($"job_{serverModal.Client_id}&server_{serverModal.Server_id}");
                    // 
                    
                }

                Console.WriteLine($"Server {serverModal.Server_id} is healthy");
            // if no error then push metrics to pushgateway
            MyMatricsPusher.PushMetrics(serverModal.Client_id, serverModal.Server_id, healthCheckModal);
            }
        }
    }
