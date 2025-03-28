using WPS_worder_node_1.Modal.Enums;

namespace WPS_worder_node_1.Modal
{
    public class ServerModal
    {
        /// <summary>
        /// Client id (who owns this server)
        /// </summary>
        public string Client_id { get; set; }

        /// <summary>
        /// server id (unique id for this server)
        /// </summary>
        public string Server_id { get; set; }

        /// <summary>
        /// flow id (used in alerting service)
        /// </summary>
        public string? flow_id { get; set; }

        /// <summary>
        /// worker_id
        /// </summary>
        public string Method { get; set; }

        /// <summary>
        /// server url (url of the server)
        /// </summary>
        public string Server_url { get; set; }

        /// <summary>
        /// Headers
        /// </summary>
        public Dictionary<string,string> Headers { get; set; }

        /// <summary>
        /// Body
        /// </summary>
        public string? Body { get; set; }

        ///<summary>
        /// status of the server
        /// </summary>
        public ServerStatus? Status { get; set; } = ServerStatus.R;

        /// <summary>
        /// type of check
        /// </summary>
        public TypeOFCheck typeOFCheck { get; set; }

        /// <summary>
        /// Check Frequency
        /// </summary>
        public CheckFrequency CheckFrequency { get; set; } = CheckFrequency.THRM;

        /// <summary>
        /// Keyword ot find or not find on the page
        /// </summary>
        public string? Keyword { get; set; }

        /// <summary>
        /// List of status codes which response can contains.
        ///</summary>
        public List<int>? StatusCodes { get; set; } = new List<int>();
    }
}
