using Hangfire;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using RestSharp;
using WPS_worder_node_1.BL;
using WPS_worder_node_1.Controllers;
using WPS_worder_node_1.Modal;

namespace WPS_worder_node_1.Repositories
{
    public class MyAPIFlowService
    {
        public static async Task InvokCheck(int client_id, int flow_id, [FromServices] ILogger _logger, [FromServices] IRecurringJobManager recurringJobManager)
        {
            // flow Execution result (to send it to the kafka)
            FlowExecutionResult result = new FlowExecutionResult();

            // make request to the userManagement module to get the flow configuration
            //create restClient
            RestClient client = new RestClient("http://localhost:5004/");

            //preparing request to register server 
            RestRequest request = new RestRequest("api/flow/getFlow", Method.Get);
            request.AddQueryParameter("client_id", client_id.ToString());
            request.AddQueryParameter("flow_id", flow_id.ToString());

            //executing request
            RestResponse rr = client.Execute(request);

            //checked if body present or not 
            if (rr == null || rr?.Content == null)
            {
                //deserializing the response
                result.Errors.Add("usrMngRqstErr", "Error while communicating with user management service to get flow configuration data.");

                // notify to user
                NotifyUser(result, client_id, flow_id, true);

                // notify to admin
                InformAdmin("\"Error while communicating with user management service to get flow configuration data.\"");
                return;

            }

            //Extract flow Configuration object
            NodeResponse? nr = JsonConvert.DeserializeObject<NodeResponse>(rr.Content);
            FlowConfiguration? flowConfig = nr?.Data;

            //check if flowConfig is valid or not
            if (flowConfig == null || flowConfig.Nodes == null || flowConfig.Edges == null)
            {
                result.Errors.Add("EmptyFlowConfig", "Improper flow configuration encounterd. Either flowConfig or flowConfig.Nodes or flowConfig.Edges are empty");

                // notify to user
                NotifyUser(result, client_id, flow_id, true);

                // notify to admin
                InformAdmin("Improper flow configuration encounterd. Either flowConfig or flowConfig.Nodes or flowConfig.Edges are empty");

                // update status of flow in the userManagmenet modal
                UpdateFlowStatus(client_id, flow_id, "Error");

                //stop recurent job
                recurringJobManager.RemoveIfExists($"job_{client_id}&flow_{flow_id}");
                return;
            }

            //Execute  Flow
            FlowExecutor flowExecutor = new FlowExecutor(_logger);
            result = await flowExecutor.ExecuteFlowAsync(flowConfig);

            //if errors occured during execution of this task 
            if (result.Errors.Count > 0)
            {

                //notify to user
                NotifyUser(result, client_id, flow_id, true);

                // update status of flow in the userManagmenet modal
                UpdateFlowStatus(client_id, flow_id, "Error");

                //stop recurent job
                recurringJobManager.RemoveIfExists($"job_{client_id}&flow_{flow_id}");
            }


        }

        /// <summary>
        /// to invoke method which helps to send kafka message 
        /// </summary>
        /// <param name="message"></param>
        private static void InformAdmin(string? message)
        {
            MyKafkaProducer.NotifyAdmin(message);
        }

        private static void NotifyUser(FlowExecutionResult result, int client_id, int flow_id, bool isError)
        {
            MyKafkaProducer.NotifyKafkaAPIFlow(result, client_id, flow_id, isError);
        }

        private static void UpdateFlowStatus(int client_id, int flow_id, string? message)
        {
            //create restClient
            RestClient client = new RestClient("http://localhost:5004/");
            //preparing request to register server 
            RestRequest request = new RestRequest("api/flow/updateFlowStatus", Method.Put);
            request.AddQueryParameter("client_id", client_id.ToString());
            request.AddQueryParameter("flow_id", flow_id.ToString());
            request.AddQueryParameter("status", message);

            //executing request
            RestResponse rr = client.Execute(request);

            Response? response = JsonConvert.DeserializeObject<Response>(rr.Content);

            //checked if body present or not 
            if (rr == null || rr.Content == null || response == null || response.IsError)
            {
                // notify to admin
                InformAdmin("\"Error while communicating with user management service to get flow configuration data.\"");
            }


        }
    }
}
