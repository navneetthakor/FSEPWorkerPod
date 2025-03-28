using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using WPS_worder_node_1.Controllers;

namespace WPS_worder_node_1.BL
{
    /// <summary>
    /// Executes a flow of nodes and edges
    /// </summary>
    public class FlowExecutor
    {
        /// <summary>
        /// for logging
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// store data for nodes
        /// </summary>
        private readonly Dictionary<string, JObject> _dataStore;

        /// <summary>
        /// tracking executed nodes
        /// </summary>
        private readonly HashSet<string> _executedNodes;

        /// <summary>
        /// prevent infinite loops
        /// </summary>
        private readonly int _maxSteps = 100;


        /// <summary>
        /// Constructor for FlowExecutor
        /// </summary>
        /// <param name="logger"></param>
        public FlowExecutor([FromServices] ILogger logger)
        {
            _logger = logger;
            _dataStore = new Dictionary<string, JObject>();
            _executedNodes = new HashSet<string>();
        }

        /// <summary>
        /// Execute a flow of nodes and edges
        /// </summary>
        /// <param name="flowConfig"></param>
        /// <returns>FlowExecutionResult</returns>
        public async Task<FlowExecutionResult> ExecuteFlowAsync(FlowConfiguration flowConfig)
        {
            // initialize result object
            FlowExecutionResult result = new FlowExecutionResult
            {
                ExecutedNodes = new List<string>(),
                NodeResults = new Dictionary<string, object>(),
                Errors = new Dictionary<string, string>(),
                Started = DateTime.UtcNow,
                Success = true
            };

            try
            {
                _logger.LogInformation("Starting flow execution with {NodeCount} nodes and {EdgeCount} edges",
                    flowConfig.Nodes.Count, flowConfig.Edges.Count);

                // Find the starting node
                string startNodeId = FindStartNode(flowConfig);
                if (string.IsNullOrEmpty(startNodeId))
                {
                    throw new Exception("No valid start node found in the flow configuration");
                }

                // Execute the flow
                await ExecuteFlowRecursiveAsync(flowConfig, startNodeId, result);

                result.Completed = DateTime.UtcNow;
                result.ExecutionTimeMs = (result.Completed - result.Started).TotalMilliseconds;
                result.Success = !result.Errors.Any();

                _logger.LogInformation("Flow execution completed in {ExecutionTimeMs}ms with {ErrorCount} errors",
                    result.ExecutionTimeMs, result.Errors.Count);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during flow execution");
                result.Success = false;
                result.Errors.Add("WPS_InternalError", ex.Message);
                result.Completed = DateTime.UtcNow;
                result.ExecutionTimeMs = (result.Completed - result.Started).TotalMilliseconds;
                return result;
            }
        }

        /// <summary>
        /// Execute the flow recursively
        /// </summary>
        /// <param name="flowConfig"></param>
        /// <param name="nodeId"></param>
        /// <param name="result"></param>
        /// <param name="depth"></param>
        /// <returns>Task</returns>
        private async Task ExecuteFlowRecursiveAsync(FlowConfiguration flowConfig, string nodeId, FlowExecutionResult result, int depth = 0)
        {
            // Prevent infinite loops
            if (depth >= _maxSteps || _executedNodes.Contains(nodeId))
            {
                return;
            }

            FlowNode? node = flowConfig.Nodes.FirstOrDefault(n => n.Id == nodeId);
            if (node == null)
            {
                _logger.LogWarning("Node with ID {NodeId} not found in flow configuration", nodeId);
                return;
            }

            _logger.LogInformation("Executing node: {NodeName} ({NodeType})", node.Name, node.Type);

            try
            {
                object nodeResult = null;

                switch (node.Type.ToUpperInvariant())
                {
                    case "REQUEST":
                        nodeResult = await ExecuteRequestNodeAsync(node);
                        break;
                    case "CONDITION":
                        nodeResult = await ExecuteConditionNodeAsync(node);
                        break;
                    case "TRANSFORM":
                        nodeResult = await ExecuteTransformNodeAsync(node);
                        break;
                    default:
                        throw new NotSupportedException($"Unsupported node type: {node.Type}");
                }

                result.NodeResults[nodeId] = nodeResult;
                _executedNodes.Add(nodeId);
                result.ExecutedNodes.Add(nodeId);

                // Find the next node(s) to execute
                var nextNodeIds = FindNextNodes(flowConfig, nodeId, nodeResult);
                foreach (var nextNodeId in nextNodeIds)
                {
                    await ExecuteFlowRecursiveAsync(flowConfig, nextNodeId, result, depth + 1);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing node {NodeId}: {NodeName}", node.Id, node.Name);
                result.Errors[nodeId] = ex.Message;
                result.Success = false;

                // Optional: Continue execution despite errors
                // var nextNodeIds = FindNextNodes(flowConfig, nodeId, null);
                // foreach (var nextNodeId in nextNodeIds)
                // {
                //     await ExecuteFlowRecursiveAsync(flowConfig, nextNodeId, result, depth + 1);
                // }
            }
        }

        /// <summary>
        /// Execute a REQUEST node
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="Exception"></exception>
        private async Task<object> ExecuteRequestNodeAsync(FlowNode node)
        {
            var properties = node.Properties;
            var url = properties["url"]?.ToString();
            var method = properties["method"]?.ToString() ?? "GET";
            var headers = properties["headers"]?.ToObject<Dictionary<string, string>>() ?? new Dictionary<string, string>();
            var body = properties["body"];

            if (string.IsNullOrEmpty(url))
            {
                throw new ArgumentException("URL is required for REQUEST node");
            }

            // Process template variables in the URL
            url = ProcessTemplateVariables(url);

            RestClient client = new RestClient(new RestClientOptions { ThrowOnAnyError = false });
            RestRequest request = new RestRequest(url);

            // Set method
            request.Method = ParseMethod(method);

            // Set headers
            foreach (KeyValuePair<string, string> header in headers)
            {
                string headerValue = ProcessTemplateVariables(header.Value);
                request.AddHeader(header.Key, headerValue);
            }

            // Set body for non-GET requests
            if (request.Method != Method.Get && request.Method != Method.Head && body != null)
            {
                if (body is JObject bodyObject)
                {
                    JObject processedBody = ProcessTemplateVariables(bodyObject);
                    request.AddJsonBody(processedBody);
                }
                else
                {
                    string processedBody = ProcessTemplateVariables(Convert.ToString(body));
                    request.AddStringBody(processedBody, DataFormat.Json);
                }
            }

            //send request now 
            _logger.LogInformation("Sending {Method} request to {Url}", method, url);
            RestResponse response = await client.ExecuteAsync(request);

            // Process response
            JToken responseBody = ParseResponseBody(response);
            JObject responseData = new JObject
            {
                ["status"] = (int)response.StatusCode,
                ["statusText"] = response.StatusDescription,
                ["headers"] = JObject.FromObject(
                response.Headers
                .GroupBy(h => h.Name) // Group headers by Name
                .ToDictionary(
                     g => g.Key,
                     g => string.Join(", ", g.Select(h => h.Value?.ToString())) // Merge duplicate values
                    )
                ),
                ["body"] = responseBody
            };

            // Store response in data store
            _dataStore[node.Id] = responseData;

            if (response.ErrorException != null)
            {
                _logger.LogError(response.ErrorException, "Error in request to {Url}: {ErrorMessage}", url, response.ErrorMessage);
                throw new Exception($"Request failed: {response.ErrorMessage}", response.ErrorException);
            }

            if ((int)response.StatusCode >= 400)
            {
                _logger.LogWarning("Request to {Url} returned status code {StatusCode}", url, (int)response.StatusCode);
            }

            return responseData;
        }

        private async Task<object> ExecuteConditionNodeAsync(FlowNode node)
        {
            string condition = node.Properties["condition"]?.ToString();
            if (string.IsNullOrEmpty(condition))
            {
                throw new ArgumentException("Condition expression is required for CONDITION node");
            }

            // For safety, we'll use a basic evaluation approach
            // In a real implementation, you might want to use a JavaScript engine like Jint
            // or a more robust expression evaluator

            // For now, we'll handle some common condition patterns
            bool result = false;

            _logger.LogInformation("Evaluating condition: {Condition}", condition);

            try
            {
                // Process data references in the condition
                string processedCondition = ProcessTemplateVariables(condition);

                // Simplified condition evaluation logic
                if (processedCondition.Contains("==="))
                {
                    var parts = processedCondition.Split("===", StringSplitOptions.TrimEntries);
                    if (parts.Length == 2)
                    {
                        result = string.Equals(parts[0].Trim('"', '\''), parts[1].Trim('"', '\''));
                    }
                }
                else if (processedCondition.Contains("!=="))
                {
                    var parts = processedCondition.Split("!==", StringSplitOptions.TrimEntries);
                    if (parts.Length == 2)
                    {
                        result = !string.Equals(parts[0].Trim('"', '\''), parts[1].Trim('"', '\''));
                    }
                }
                else if (processedCondition.Contains("=="))
                {
                    var parts = processedCondition.Split("==", StringSplitOptions.TrimEntries);
                    if (parts.Length == 2)
                    {
                        result = string.Equals(parts[0].Trim('"', '\''), parts[1].Trim('"', '\''));
                    }
                }
                else if (processedCondition.Contains("!="))
                {
                    var parts = processedCondition.Split("!=", StringSplitOptions.TrimEntries);
                    if (parts.Length == 2)
                    {
                        result = !string.Equals(parts[0].Trim('"', '\''), parts[1].Trim('"', '\''));
                    }
                }

                _logger.LogInformation("Condition evaluated to: {Result}", result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error evaluating condition: {Condition}", condition);
                throw new Exception($"Error evaluating condition: {ex.Message}", ex);
            }

            // Store result in data store
            _dataStore[$"{node.Id}_result"] = new JObject { ["value"] = result };

            return result;
        }

        private async Task<object> ExecuteTransformNodeAsync(FlowNode node)
        {
            var transform = node.Properties["transform"]?.ToString();
            if (string.IsNullOrEmpty(transform))
            {
                throw new ArgumentException("Transform expression is required for TRANSFORM node");
            }

            _logger.LogInformation("Executing transform");

            try
            {
                // In a real implementation, you might want to use a JavaScript engine
                // For now, we'll just create a simple output based on the available data
                var output = new JObject();

                // Find all data references in the transform code
                var dataRefs = ExtractDataReferences(transform);

                // For each data reference, try to find the corresponding data
                foreach (var dataRef in dataRefs)
                {
                    var value = ExtractDataByPath(dataRef);
                    if (value != null)
                    {
                        output[dataRef] = JToken.FromObject(value);
                    }
                }

                // If the transform has a 'return' statement, try to parse it
                string returnValue = null;
                if (transform.Contains("return"))
                {
                    var returnStatement = transform.Split("return", StringSplitOptions.TrimEntries)[1];
                    returnStatement = returnStatement.TrimEnd(';', '}');
                    returnValue = returnStatement.Trim();
                }

                // Store the transformed data
                _dataStore[$"{node.Id}_output"] = output;

                return new
                {
                    output = output,
                    returnExpression = returnValue
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing transform");
                throw new Exception($"Error executing transform: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Find the start node in the flow configuration
        /// </summary>
        /// <param name="flowConfig"></param>
        /// <returns>string which is id</returns>
        private string FindStartNode(FlowConfiguration flowConfig)
        {
            // Find nodes that aren't targets of any edges
            HashSet<string> targetNodeIds = new HashSet<string>(flowConfig.Edges.Select(e => e.Target));
            List<FlowNode> potentialStartNodes = flowConfig.Nodes.Where(node => !targetNodeIds.Contains(node.Id)).ToList();

            if (potentialStartNodes.Count > 0)
            {
                return potentialStartNodes[0].Id;
            }

            // If no clear start node, return the first node
            return flowConfig.Nodes.FirstOrDefault()?.Id;
        }

        /// <summary>
        /// Find Next Nodes
        /// </summary>
        /// <param name="flowConfig"></param>
        /// <param name="currentNodeId"></param>
        /// <param name="nodeResult"></param>
        /// <returns> list of string </returns>
        private List<string> FindNextNodes(FlowConfiguration flowConfig, string currentNodeId, object nodeResult)
        {
            List<string> nextNodes = new List<string>();
            List<FlowEdge> relevantEdges = flowConfig.Edges.Where(e => e.Source == currentNodeId).ToList();

            if (!relevantEdges.Any())
            {
                return nextNodes;
            }

            // Handle condition nodes
            FlowNode? currentNode = flowConfig.Nodes.FirstOrDefault(n => n.Id == currentNodeId);
            if (currentNode != null && currentNode.Type.Equals("CONDITION", StringComparison.OrdinalIgnoreCase))
            {
                bool conditionResult = nodeResult is bool boolResult ? boolResult : false;
                string sourcePort = conditionResult ? "true" : "false";

                FlowEdge? matchingEdge = relevantEdges.FirstOrDefault(e => e.SourcePort == sourcePort);
                if (matchingEdge != null)
                {
                    nextNodes.Add(matchingEdge.Target);
                }
            }

            // Handle transform nodes with output paths
            //else if (currentNode != null && currentNode.Type.Equals("TRANSFORM", StringComparison.OrdinalIgnoreCase))
            //{
            //    string outputPath = null;
            //    if (nodeResult is dynamic dynamicResult && dynamicResult.output?.path != null)
            //    {
            //        outputPath = dynamicResult.output.path.ToString();
            //    }

            //    if (!string.IsNullOrEmpty(outputPath))
            //    {
            //        var matchingEdge = relevantEdges.FirstOrDefault(e => e.SourcePort == outputPath);
            //        if (matchingEdge != null)
            //        {
            //            nextNodes.Add(matchingEdge.Target);
            //            return nextNodes;
            //        }
            //    }
            //}

            // For other node types or if no specific path was found, take all edges
            if (!nextNodes.Any())
            {
                nextNodes.AddRange(relevantEdges.Select(e => e.Target));
            }

            return nextNodes;
        }

        /// <summary>
        /// Process template variables in a string
        /// </summary>
        /// <param name="input"></param>
        /// <returns>string</returns>
        private string ProcessTemplateVariables(string? input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }

            return System.Text.RegularExpressions.Regex.Replace(input, @"\$\{([^}]+)\}", match =>
            {
                string path = match.Groups[1].Value.Trim();
                object value = ExtractDataByPath(path);
                return value?.ToString() ?? "";
            });
        }

        private JObject ProcessTemplateVariables(JObject input)
        {
            if (input == null)
            {
                return null;
            }

            JObject result = new JObject();
            foreach (JProperty property in input.Properties())
            {
                if (property.Value.Type == JTokenType.Object)
                {
                    result[property.Name] = ProcessTemplateVariables(property.Value as JObject);
                }
                else if (property.Value.Type == JTokenType.String)
                {
                    result[property.Name] = ProcessTemplateVariables(property.Value.ToString());
                }
                else
                {
                    result[property.Name] = property.Value;
                }
            }

            return result;
        }

        private object ExtractDataByPath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            // Handle special syntax for data references
            if (path.StartsWith("dataStore"))
            {
                var parts = path.Split('.', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2)
                {
                    var nodeId = parts[1].Trim('`', '"', '\'');
                    if (_dataStore.TryGetValue(nodeId, out var nodeData))
                    {
                        if (parts.Length == 2)
                        {
                            return nodeData;
                        }
                        else
                        {
                            return ExtractNestedData(nodeData, string.Join(".", parts.Skip(2)));
                        }
                    }
                }
            }
            else if (path.Contains("."))
            {
                var parts = path.Split('.', StringSplitOptions.RemoveEmptyEntries);
                var nodeId = parts[0].Trim('`', '"', '\'');
                if (_dataStore.TryGetValue(nodeId, out var nodeData))
                {
                    if (parts.Length == 1)
                    {
                        return nodeData;
                    }
                    else
                    {
                        return ExtractNestedData(nodeData, string.Join(".", parts.Skip(1)));
                    }
                }
            }

            return null;
        }

        private object ExtractNestedData(JObject data, string path)
        {
            if (data == null || string.IsNullOrEmpty(path))
            {
                return null;
            }

            var parts = path.Split('.', StringSplitOptions.RemoveEmptyEntries);
            JToken current = data;

            foreach (var part in parts)
            {
                // Handle array indexing
                if (part.Contains("[") && part.Contains("]"))
                {
                    var arrayParts = part.Split('[', ']');
                    var propName = arrayParts[0];
                    var indexStr = arrayParts[1];

                    if (int.TryParse(indexStr, out var index))
                    {
                        current = current[propName]?[index];
                    }
                    else
                    {
                        current = current[propName];
                    }
                }
                else
                {
                    current = current[part];
                }

                if (current == null)
                {
                    return null;
                }
            }

            return current;
        }

        /// Meet joshi once visited this code with navneet and they both agreed that this code is good to go.
        /// <summary>
        /// 
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        private List<string> ExtractDataReferences(string code)
        {
            var references = new List<string>();

            // Extract patterns like getDataRef('nodeName.path')
            var matches = System.Text.RegularExpressions.Regex.Matches(code, @"getDataRef\(['""]([^'""]+)['""]");
            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                if (match.Groups.Count > 1)
                {
                    references.Add(match.Groups[1].Value);
                }
            }

            // Extract patterns like dataStore['nodeName']
            matches = System.Text.RegularExpressions.Regex.Matches(code, @"dataStore\[['""]([^'""]+)['""]");
            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                if (match.Groups.Count > 1)
                {
                    references.Add(match.Groups[1].Value);
                }
            }

            return references;
        }

        private Method ParseMethod(string method)
        {
            return method?.ToUpperInvariant() switch
            {
                "GET" => Method.Get,
                "POST" => Method.Post,
                "PUT" => Method.Put,
                "DELETE" => Method.Delete,
                "HEAD" => Method.Head,
                "OPTIONS" => Method.Options,
                "PATCH" => Method.Patch,
                _ => Method.Get
            };
        }

        private JToken ParseResponseBody(RestResponse response)
        {
            try
            {
                var contentType = response.ContentType?.ToLowerInvariant();

                if (contentType?.Contains("application/json") == true)
                {
                    return JToken.Parse(response.Content);
                }
                else if (contentType?.Contains("text/") == true)
                {
                    return response.Content;
                }
                else
                {
                    return response.Content;
                }
            }
            catch
            {
                return response.Content;
            }
        }
    }

    public class FlowExecutionResult
    {
        public bool Success { get; set; }
        public DateTime Started { get; set; }
        public DateTime Completed { get; set; }
        public double ExecutionTimeMs { get; set; }
        public List<string> ExecutedNodes { get; set; }
        public Dictionary<string, object> NodeResults { get; set; }
        public Dictionary<string, string> Errors { get; set; }
    }
}
