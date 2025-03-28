using Hangfire;
using Microsoft.AspNetCore.Mvc;
using WPS_worder_node_1.BL;
using WPS_worder_node_1.Modal;
using WPS_worder_node_1.Modal.Enums;


namespace WPS_worder_node_1.Repositories
{
    public class MySingleEndpointService
    {
        public static void InvokCheck(ServerModal serverModal, [FromServices] IRecurringJobManager recurringJobManager)
        {
            Console.WriteLine("Invoking MySingleEndpointService HealthCheck");
            
                //check if this server is already under recovery 
                if (serverModal.Status == ServerStatus.P)
                {
                    return;
                }

                //check it's health
                HealthCheckerModal healthCheckModal = HealthChecker.CheckHealthAsync(serverModal).GetAwaiter().GetResult();
                //if error then notify to kafka 
                if (healthCheckModal.IsError)
                {
                    // notify to kafka
                    MyKafkaProducer.NotifyKafka(serverModal, healthCheckModal, false);

                // stop this recurrant job
                recurringJobManager.RemoveIfExists($"");
                    // 
                    
                }
                // if no error then push metrics to pushgateway
                MyMatricsPusher.PushMetrics(serverModal.Client_id, serverModal.Server_id, healthCheckModal);
            }
        }
    }
