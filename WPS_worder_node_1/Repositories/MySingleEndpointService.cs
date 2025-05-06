using Hangfire;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using RestSharp;
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

                //update status in userManagement modal
                MySingleEndpointService.UpdateFlowStatus(serverModal.Client_id, serverModal.Server_id, "Error");

            }

            Console.WriteLine($"Server {serverModal.Server_id} is healthy");
            // if no error then push metrics to pushgateway
            MyMatricsPusher.PushMetrics(serverModal.Client_id, serverModal.Server_id, healthCheckModal);
        
        }

        private static void UpdateFlowStatus(string client_id, string server_id, string? message)
        {
            //create restClient
            RestClient client = new RestClient("http://localhost:5002/");
            //preparing request to register server 
            RestRequest request = new RestRequest($"Server/pushServer/{client_id}/{server_id}", Method.Put);

            //executing request
            RestResponse? rr = client.Execute(request);

            //Response? response = JsonConvert.DeserializeObject<Response>(rr?.Content);

            //checked if body present or not 
            if (rr == null || rr.Content == null)
            {
                // notify to admin
                InformAdmin("\"Error while communicating with user management service to get flow configuration data.\"");
            }
        }

        /// <summary>
        /// to invoke method which helps to send kafka message 
        /// </summary>
        /// <param name="message"></param>
        private static void InformAdmin(string? message)
        {
            MyKafkaProducer.NotifyAdmin(message, TypeOfEmail.AdminEmail);
        }
    }
}
