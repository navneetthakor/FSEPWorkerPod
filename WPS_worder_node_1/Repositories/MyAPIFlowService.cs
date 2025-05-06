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
        public static async Task InvokCheck(string client_id, string flow_id, [FromServices] IRecurringJobManager recurringJobManager)
        {
            // flow Execution result (to send it to the kafka)
            FlowExecutionResult result = new FlowExecutionResult();

            // make request to the userManagement module to get the flow configuration
            //create restClient
            RestClient client = new RestClient("http://localhost:5002/");

            //preparing request to register server 
            RestRequest request = new RestRequest($"apiFlow/getInfo/{client_id}/{flow_id}", Method.Get);

            //executing request
            RestResponse rr = client.Execute(request);

            //checked if body present or not 
            if (rr == null || rr.Content == null)
            {
                //adding errors
                result.Errors.Add("usrMngRqstErr", "Error while communicating with user management service to get flow configuration data.");

                // notify to user
                //NotifyUser(result, client_id, flow_id, true);

                // notify to admin
                InformAdmin("\"Error while communicating with user management service to get flow configuration data.\"");
                return;

            }

            //Extract flow Configuration object
            NodeResponse? nr = JsonConvert.DeserializeObject<NodeResponse>(rr.Content);
            FlowConfiguration? flowConfig = nr?.Data.ToObject<FlowConfiguration>();

            //check if error occurred at user-management module side 
            if (nr == null || nr.IsError)
            {
                //Adding errors
                result.Errors.Add("usrMngRqstErr", "Error while communicating with user management service to get flow configuration data.");

                // notify to user
                NotifyUser(result, client_id, flowConfig.ApiFlowName, true);

                // notify to admin
                InformAdmin("\"Error while communicating with user management service to get flow configuration data.\"");
                return;
            }

            //check if flowConfig is valid or not
            if (flowConfig == null || flowConfig.Nodes == null || flowConfig.Edges == null)
            {
                result.Errors.Add("EmptyFlowConfig", "Improper flow configuration encounterd. Either flowConfig or flowConfig.Nodes or flowConfig.Edges are empty");

                // notify to user
                NotifyUser(result, client_id, flowConfig.ApiFlowName, true);

                // notify to admin
                InformAdmin("Improper flow configuration encounterd. Either flowConfig or flowConfig.Nodes or flowConfig.Edges are empty");

                // update status of flow in the userManagmenet modal
                UpdateFlowStatus(client_id, flow_id, result);

                //stop recurent job
                recurringJobManager.RemoveIfExists($"job_{client_id}&flow_{flow_id}");
                return;
            }

            //Execute  Flow
            FlowExecutor flowExecutor = new FlowExecutor();
            result = await flowExecutor.ExecuteFlowAsync(flowConfig);

            //if errors occured during execution of this task 
            if (result.Errors.Count > 0)
            {

                //notify to user
                NotifyUser(result, client_id, flowConfig.ApiFlowName, true);

                // update status of flow in the userManagmenet modal
                UpdateFlowStatus(client_id, flow_id, result);

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
            MyKafkaProducer.NotifyAdmin(message, TypeOfEmail.AdminEmail);
        }

        private static void NotifyUser(FlowExecutionResult result, string client_id, string flow_name, bool isError)
        {
            TypeOfEmail et = isError ? TypeOfEmail.APIFlowErrorEmail : TypeOfEmail.APIFlowTestEmail;
            MyKafkaProducer.NotifyKafkaAPIFlow(result, client_id, flow_name, et);
        }

        private static void UpdateFlowStatus(string client_id, string flow_id, FlowExecutionResult flowExecutionResult)
        {

            //get env variable 
            string? user_management_URL = Environment.GetEnvironmentVariable("user_management_URL");

            //create restClient
            RestClient client = new RestClient($"{user_management_URL}");
            //preparing request to register server 
            RestRequest request = new RestRequest($"apiFlow/updateFlowStatus/{client_id}/{flow_id}", Method.Put);
            request.AddJsonBody(JsonConvert.SerializeObject(flowExecutionResult));

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
