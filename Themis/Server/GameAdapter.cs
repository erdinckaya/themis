using networkprotocol;

namespace Themis.Server
{
    /// <summary>
    /// Game Adapter which notifies client connections.
    /// </summary>
    public class GameAdapter : Adapter
    {
        private readonly GameServer _server;

        public GameAdapter(GameServer server)
        {
            _server = server;
        }

        /// <summary>
        /// Creates Message factory with given allocator.
        /// </summary>
        /// <param name="allocator">Usually it is yojimbo default allocator.</param>
        /// <returns></returns>
        public override MessageFactory CreateMessageFactory(Allocator allocator)
        {
            return new GameMessageFactory(allocator);
        }

        /// <summary>
        /// Client connect event dispatcher.
        /// </summary>
        /// <param name="clientIndex"></param>
        public override void OnServerClientConnected(int clientIndex)
        {
            _server?.ClientConnected(clientIndex);
        }

        /// <summary>
        /// Client disconnect event dispatcher.
        /// </summary>
        /// <param name="clientIndex"></param>
        public override void OnServerClientDisconnected(int clientIndex)
        {
            _server?.ClientDisconnected(clientIndex);
        }
    }
}