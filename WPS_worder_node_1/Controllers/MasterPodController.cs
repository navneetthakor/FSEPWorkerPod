using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WPS_worder_node_1.BL;
using WPS_worder_node_1.Modal;
using System.Data;
using WPS_worder_node_1.Repositories.Interface;
using Hangfire;
using WPS_worder_node_1.Repositories;
using WPS_worder_node_1.Modal.Enums;
using RestSharp;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Mvc.Filters;

namespace WPS_worder_node_1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MasterPodController : ControllerBase
    {
        [HttpPost]
        [Route("v2/register")]
        public async Task<Response> RegisterServerList(IServerListRepo serverListRepo, [FromBody] ServerModal serverModal)
        {
            try
            {
                // add clentServer to serverListRepo
                serverListRepo.AddServer(serverModal);

                //check it's health first 
                HealthCheckerModal healthChecker = await HealthChecker.CheckHealthAsync(serverModal);

                //push metrics to pushgateway
                MyMatricsPusher.PushMetrics(serverModal.Client_id, serverModal.Server_id, healthChecker);

                //if error then,
                //send notification to alerting system.
                if (healthChecker.IsError)
                {
                    MyKafkaProducer.NotifyKafka(serverModal, healthChecker, false);
                }
                // testing notification
                else
                {
                    MyKafkaProducer.NotifyKafka(serverModal, healthChecker, true);
                }

                //preparing Response 
                Response response = new Response()
                {
                    StatusCode = 200,
                    IsError = false,
                };

                // data to the response
                response.Data = new DataTable();
                response.Data.Columns.Add("StatusCode", typeof(int));
                response.Data.Columns.Add("ErrorMessage", typeof(string));
                response.Data.Columns.Add("IsError", typeof(bool));
                response.Data.Columns.Add("ResponseTime", typeof(int));

                response.Data.Rows.Add(healthChecker.StatusCode, healthChecker.ErrorMessage, healthChecker.IsError, healthChecker.ResponseTime);

                return response;

            }
            catch (Exception ex)
            {
                Response response = new Response()
                {
                    StatusCode = 500,
                    IsError = true,
                    ErrorMessage = ex.Message
                };
                return response;
            }
        }

        [HttpPost]
        [Route("register")]
        ///<summary>
        /// Register server to the server
        /// </summary>
        public async Task<Response> RegisterServer([FromServices] IRecurringJobManager recurringJobManager, [FromBody] ServerModal serverModal)
        {
            try
            {
                //check it's health first 
                HealthCheckerModal healthChecker = await HealthChecker.CheckHealthAsync(serverModal);

                //push metrics to pushgateway
                MyMatricsPusher.PushMetrics(serverModal.Client_id, serverModal.Server_id, healthChecker);

                //if error then,
                if (healthChecker.IsError)
                {
                    //send notification to alerting system.
                    MyKafkaProducer.NotifyKafka(serverModal, healthChecker, false);

                }
                else
                {
                    // testing notification
                    MyKafkaProducer.NotifyKafka(serverModal, healthChecker, true);

                    // setup hangfire recurring job
                    recurringJobManager.AddOrUpdate($"job_{serverModal.Client_id}&server_{serverModal.Server_id}", () => MySingleEndpointService.InvokCheck(serverModal, recurringJobManager), CronInterval.getCronInterval.GetValueOrDefault(serverModal.CheckFrequency));
                }

                //preparing Response 
                Response response = new Response()
                {
                    StatusCode = 200,
                    IsError = false,
                };

                // data to the response
                response.Data = new DataTable();
                response.Data.Columns.Add("StatusCode", typeof(int));
                response.Data.Columns.Add("ErrorMessage", typeof(string));
                response.Data.Columns.Add("IsError", typeof(bool));
                response.Data.Columns.Add("ResponseTime", typeof(int));

                response.Data.Rows.Add(healthChecker.StatusCode, healthChecker.ErrorMessage, healthChecker.IsError, healthChecker.ResponseTime);

                return response;

            }
            catch (Exception ex)
            {
                Response response = new Response()
                {
                    StatusCode = 500,
                    IsError = true,
                    ErrorMessage = ex.Message
                };
                return response;
            }
        }


        [HttpDelete]
        [Route("remove/{client_id}/{server_id}")]
        public Response RemoveServer(IServerListRepo serverlistRepo, int client_id, int server_id)
        {
            try
            {
                // remove server from serverListRepo
                serverlistRepo.RemoveServer(client_id, server_id);

                //preparing Response 
                Response response = new Response()
                {
                    StatusCode = 200,
                    IsError = false,
                    ErrorMessage = "Server removed successfully"
                };

                return response;
            }
            catch (Exception ex)
            {
                Response response = new Response()
                {
                    StatusCode = 500,
                    IsError = true,
                    ErrorMessage = ex.Message
                };
                return response;
            }
        }

        [HttpGet]
        [Route("getHeartBit")]
        public Response GetHeartBit()
        {
            return new Response() { IsError = false };
        }

        [HttpPost]
        [Route("RegisterAPIFlow")]
        public async Task<Response> RegisterAPIFlow(int client_id, int flow_id, [FromServices] ILogger _logger, [FromServices] IRecurringJobManager recurringJobManager)
        {
            try
            {
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
                if (rr?.Content == null)
                {
                    //deserializing the response
                    return new Response() { IsError = true, ErrorMessage = "Error while communicating with user management service to get flow configuration data." };
                }
                NodeResponse? nr = JsonConvert.DeserializeObject<NodeResponse>(rr.Content);

                FlowConfiguration? flowConfig = nr?.Data;

                //check if flowConfig is valid or not
                if (flowConfig == null || flowConfig.Nodes == null || flowConfig.Edges == null)
                {
                    return new Response() { IsError = true, ErrorMessage =  "Invalid flow configuration" };
                }

                //Execute  Flow
                FlowExecutor flowExecutor = new FlowExecutor(_logger);
                FlowExecutionResult result = await flowExecutor.ExecuteFlowAsync(flowConfig);

                //if errors occured during execution of this task 
                if (result.Errors.Count > 0)
                {
                    //report to user through kafka
                    MyKafkaProducer.NotifyKafkaAPIFlow(result, client_id, flow_id, true);

                    return new Response() { IsError = true, ErrorMessage = "Error executing request flow" };
                }
                else
                {
                    //register reccuring job.
                    recurringJobManager.AddOrUpdate($"job_{client_id}&flow_{flow_id}", () => MyAPIFlowService.InvokCheck(client_id, flow_id, _logger, recurringJobManager), CronInterval.getCronInterval.GetValueOrDefault(flowConfig.CheckFrequency));

                    // send test notification
                    MyKafkaProducer.NotifyKafkaAPIFlow(result, client_id, flow_id, false);

                }

                return new Response() { IsError = false, ErrorMessage = "success", Other = result };


            }
            catch (Exception ex)
            {
                Response response = new Response()
                {
                    StatusCode = 500,
                    IsError = true,
                    ErrorMessage = ex.Message
                };
                return response;
            }

        }
    }
}
