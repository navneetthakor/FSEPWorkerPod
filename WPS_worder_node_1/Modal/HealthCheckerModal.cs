namespace WPS_worder_node_1.Modal
{
    public class HealthCheckerModal
    {
        /// <summary>
        /// Status code
        /// </summary>
        public int StatusCode { get; set; }

        /// <summary>
        /// Error message
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Is there any error
        /// </summary>
        public bool IsError { get; set; }

        /// <summary>
        /// Response time
        /// </summary>
        public int ResponseTime { get; set; }

        /// <summary>
        /// body part
        /// </summary>
        public string? Body { get; set; }
    }
}
