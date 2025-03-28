using System.Data;

namespace WPS_worder_node_1.Modal
{
    public class Response
    {
        /// <summary>
        /// Data received by this request
        /// </summary>
        public DataTable? Data { get; set; }

        /// <summary>
        /// Does it have Error
        /// </summary>
        public bool IsError { get; set; } = false;

        /// <summary>
        /// Message provided
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// status code
        /// </summary>
        public int StatusCode { get; set; } = 200;

        public dynamic? Other { get; set; }
    }
}
