using System;
using networkprotocol;
using Themis.Game;
using Themis.Server;
using Themis.Shared.Utils;

namespace Themis
{
    internal static class Program
    {
        public static void Main(string[] args)
        {
            
            Console.Write("\n");

            // Initialize yojimbo!
            if (!yojimbo.InitializeYojimbo())
            {
                Console.Write("error: failed to initialize Yojimbo!\n");
                return;
            }

            // Set Loglevel to info. 
            yojimbo.log_level(yojimbo.LOG_LEVEL_INFO);

            // Start Server
            ServerMain();

            // Shutdown after server ends
            yojimbo.ShutdownYojimbo();

            Console.Write("\n");
        }

        private static void ServerMain()
        {
            GameServer gameServer = new GameServer(new Address(Constants.ServerAddress, Constants.ServerPort));
            GameManager.Instance.SetGameServer(gameServer);
            gameServer.Run();
        }
    }
}