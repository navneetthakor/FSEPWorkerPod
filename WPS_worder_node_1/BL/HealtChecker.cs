using System;
using System.Diagnostics;
using RestSharp;
using WPS_worder_node_1.Modal;
using WPS_worder_node_1.Modal.Enums;

namespace WPS_worder_node_1.BL
{
    public class HealthChecker
    {
        public static async Task<HealthCheckerModal> CheckHealthAsync(ServerModal serverModal)
        {
            //create rest client 
            RestClient client = new RestClient();

            //selecting appropriate Http method
            Method method;
            switch(serverModal.Method)
            {
                case "GET":
                    method = Method.Get;
                    break;
                case "POST":
                    method = Method.Post;
                    break;
                case "PUT":
                    method = Method.Put;
                    break;
                case "DELETE":
                    method = Method.Delete;
                    break;
                default:
                    method = Method.Get;
                    break;
            }

            //new request to serve 
            RestRequest request = new RestRequest(serverModal.Server_url, method);

            //add headers if there are any
            if (serverModal.Headers != null)
            {
                foreach (KeyValuePair<string,string> header in serverModal.Headers)
                {
                    request.AddHeader(header.Key, header.Value);
                }
            }

            //add body if there is any
            if (!string.IsNullOrEmpty(serverModal.Body))
            {
                request.AddJsonBody(serverModal.Body);
            }

            try
            {
                //starting stopwatch ( to measure response time)
                Stopwatch stopwatch = Stopwatch.StartNew();

                //executing request 
                RestResponse response = await client.ExecuteAsync(request);
                stopwatch.Stop();

                //preparing healthChecker modal
                HealthCheckerModal healthChecker = new HealthCheckerModal();
                healthChecker.StatusCode = (int)response.StatusCode;
                healthChecker.Body = response.Content;
                healthChecker.ResponseTime = (int)stopwatch.ElapsedMilliseconds;

                // check weather criterias meet or not
                HealthChecker.CheckCriteria(serverModal,healthChecker, response.ErrorException?.Message);

                // return healthChecker modal
                return healthChecker;
            }
            catch (Exception ex)
            {
                //Console log error message
                Console.WriteLine($"Request failed: {ex.Message}");

                //preparing healthChecker modal
                HealthCheckerModal healthChecker = new HealthCheckerModal();
                healthChecker.IsError = true;
                healthChecker.ErrorMessage = ex.Message;
                healthChecker.StatusCode = -1;

                //if some error occured then return error message
                return healthChecker;
            }
        }

        public static void CheckCriteria(ServerModal serverModal, HealthCheckerModal healthChecker, String? ErrorMessage)
        {
            //extract type of check
            TypeOFCheck typeOFCheck = serverModal.typeOFCheck;


            switch (typeOFCheck)
            {
                /// <summary>
                /// A URL is considered unavailable when a user or application cannot access the resource it points to. (status code >= 400)
                /// </summary>
                case TypeOFCheck.UBU:
                    if (healthChecker.StatusCode >= 400)
                    {
                        healthChecker.IsError = true;
                    }
                    break;


                /// <summary>
                /// Check that status code matches with the provided status codes
                /// </summary>
                case TypeOFCheck.URHSCOT:
                    {
                        bool isError = true;
                        foreach (int code in serverModal.StatusCodes)
                        {
                            if (healthChecker.StatusCode == code)
                            {
                                isError = false;
                                break;
                            }
                        }

                        if (isError)
                        {
                            healthChecker.IsError = true;
                        }
                    }
                    break;


                /// <summary>
                /// check in body that wheather it contains target keyword?
                /// Make sure that the keyword is not empty
                /// </summary>
                case TypeOFCheck.UCK:
                    Console.WriteLine("UCK");
                    if (string.IsNullOrEmpty(healthChecker.Body) || !healthChecker.Body.Contains(serverModal.Keyword))
                    {
                        healthChecker.IsError = true;
                    }
                    break;


                /// <summary>
                /// Check that body not contains the keyword
                /// </summary>
                case TypeOFCheck.UNCK:
                    if ( !string.IsNullOrEmpty(healthChecker.Body) && healthChecker.Body.Contains(serverModal.Keyword))
                    {
                        healthChecker.IsError = true;
                    }
                    break;
            }

            //if there is any error then set error message
            if (healthChecker.IsError)
            {
                Console.WriteLine("Error");
                healthChecker.ErrorMessage = ErrorMessage;
            }
        }
    }
}
