using Confluent.Kafka;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WPS_worder_node_1.BL;
using WPS_worder_node_1.Modal;

namespace WPS_worder_node_1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class KafkaTesterController : ControllerBase
    {

        [HttpPost]
        [Route("produceData")]
        public async Task<Response> ProduceData() { 
            ServerModal serverModal = new ServerModal();
            HealthCheckerModal healthCheckerModal = new HealthCheckerModal();
            MyKafkaProducer.NotifyKafka(serverModal, healthCheckerModal,true);

            return new Response { StatusCode = StatusCodes.Status200OK };
        }
    }
}
