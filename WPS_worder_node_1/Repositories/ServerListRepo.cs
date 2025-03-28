using WPS_worder_node_1.Modal;
using WPS_worder_node_1.Repositories.Interface;

namespace WPS_worder_node_1.Repositories
{
    public class ServerListRepo : IServerListRepo
    {
        public List<ServerModal> ListOfServer { get; set; }

        public ServerListRepo()
        {
            ListOfServer = new List<ServerModal>();
        }

        public void AddServer(ServerModal server)
        {
            ListOfServer.Add(server);
        }

        public void RemoveServer(int clinet_id, int server_id)
        {
            ListOfServer.RemoveAll(x => x.Client_id == clinet_id && x.Server_id == server_id);
        }
    }
}
