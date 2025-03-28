using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using RestSharp;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using WPS_worder_node_1.BL;
using WPS_worder_node_1.Modal.Enums;


namespace WPS_worder_node_1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
   
    public class RequestFlowController : ControllerBase
    {
        /// <summary>
        /// Logger instance
        /// </summary>
        private readonly ILogger<RequestFlowController> _logger;

        /// <summary>
        /// Constructor for thisController
        /// </summary>
        /// <param name="logger"></param>
        public RequestFlowController(ILogger<RequestFlowController> logger)
        {
            _logger = logger;
        }

        [HttpPost("execute")]
        ///<summary>
        ///to execute flow
        /// </summary>
        public async Task<IActionResult> ExecuteFlow([FromBody] FlowConfiguration flowConfig)
        {
            if (flowConfig == null || flowConfig.Nodes == null || flowConfig.Edges == null)
            {
                return BadRequest("Invalid flow configuration");
            }

            try
            {
                FlowExecutor flowExecutor = new FlowExecutor(_logger);
                FlowExecutionResult result = await flowExecutor.ExecuteFlowAsync(flowConfig);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing request flow");
                return StatusCode(500, new { error = "Error executing request flow", message = ex.Message });
            }
        }
    }

    public class FlowConfiguration
    {
        [JsonProperty("nodes")]
        public List<FlowNode> Nodes { get; set; }

        [JsonProperty("edges")]
        public List<FlowEdge> Edges { get; set; }

        [JsonProperty("checkFrequency")]
        public CheckFrequency CheckFrequency { get; set; }
    }

    public class FlowNode
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("properties")]
        public JObject Properties { get; set; }
    }

    public class FlowEdge
    {
        [JsonProperty("source")]
        public string Source { get; set; }

        [JsonProperty("target")]
        public string Target { get; set; }

        [JsonProperty("sourcePort")]
        public string SourcePort { get; set; }
    }
}
