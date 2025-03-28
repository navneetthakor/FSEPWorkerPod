using RestSharp;
using WPS_worder_node_1.Repositories.Interface;

namespace WPS_worder_node_1.Repositories
{
    public class HeartBitService : IHeartBitService
    {

        public async Task HeartBit()
        {
            try
            {

            //creating restClinet 
            RestClient client = new RestClient();

            //creating request 
            RestRequest request = new RestRequest("http://localhost:5003/api/UptimeWorker/HeartBit?worker_id=worker1", Method.Get);

            //sending request 
            await client.ExecuteAsync(request);
            }
            catch(Exception ex)
            {
                //log exception
                //Send message to kafka 
            }
        }
    }
}
    