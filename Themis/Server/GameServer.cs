using System;
using System.Diagnostics;
using networkprotocol;
using Themis.Game;
using Themis.Shared.Data;
using Themis.Shared.Utils;

namespace Themis.Server
{
    /// <summary>
    /// Basic Yojimbo server
    /// </summary>
    public class GameServer
    {
        private          double                 _time;    // Game time in terms of seconds.
        private volatile bool                   _running; // Game active flag.
        private readonly networkprotocol.Server _server;  // Yojimbo server instance
        private readonly GameConnectionConfig   _config;  //Yojimbo connection config.

        public Action<PriorityQueue<MessageData>> OnMessage; // Message event dispatcher
        public Action<double>                     OnTick;    // Game server tick dispatcher


        public GameServer(Address address)
        {
            _time    = 0;
            _running = true;
            var privateKey = new byte[yojimbo.KeyBytes];
            // Init your server's connection config.
            _config = new GameConnectionConfig();

            // Create adapter for Server it provides you client connect and disconnect callbacks and more
            var adapter = new GameAdapter(this);

            _server = new networkprotocol.Server(yojimbo.DefaultAllocator, privateKey, address, _config, adapter, _time);

            // Start Server with private key.
            _server.Start(yojimbo.MaxClients);
            yojimbo.printf(yojimbo.LOG_LEVEL_INFO, $"Server address is {_server.Address}\n");
        }

        /// <summary>
        /// This function is invoked by GameAdapter, when a client connects.
        /// </summary>
        /// <param name="clientIndex"></param>
        public void ClientConnected(int clientIndex)
        {
            yojimbo.printf(yojimbo.LOG_LEVEL_INFO, $"Client has connected {clientIndex}\n");
            GameManager.Instance.State.AddPlayer(_server.GetClientId(clientIndex), clientIndex);
            GameManager.Instance.PlayerCount++;
        }

        /// <summary>
        /// This function is invoked by GameAdapter, when a client disconnects.
        /// </summary>
        /// <param name="clientIndex"></param>
        public void ClientDisconnected(int clientIndex)
        {
            yojimbo.printf(yojimbo.LOG_LEVEL_INFO, $"Client has disconnected {clientIndex}\n");
            GameManager.Instance.PlayerCount--;
            GameManager.Instance.IsPaused = false;
            GameManager.Instance.State.Reset();
        }

        /// <summary>
        /// Runs server.
        /// </summary>
        public void Run()
        {
            // This functions is the main part of server.

            // Start watch for calculating elapsed time in terms of seconds. Because yojimbo works second metric.
            var watch = new Stopwatch();
            watch.Start();
            _time = 0;
            // This loop runs every `Constants.DeltaTime` for syncing client and server time.
            while (_running)
            {
                double elapsedTime = watch.Elapsed.TotalSeconds;
                if (_time <= elapsedTime)
                {
                    // This part is the most import part, you need to initialize
                    // with exact order.
                    // 1 -> AdvanceTime
                    // 2 -> Receive Packages
                    // 3 -> Process Messages
                    // 4 -> Send Packages

                    _server.AdvanceTime(_time);
                    _time += Constants.DeltaTime;


                    _server.ReceivePackets();

                    OnTick?.Invoke(_time);
                    ProcessMessages();

                    SendState();
                    if (!_server.IsRunning)
                        break;

                    _server.SendPackets();
                }
                else
                {
                    yojimbo.sleep(_time - elapsedTime);
                }
            }

            watch.Stop();
            _server.Stop();
        }

        /// <summary>
        /// Sends state to all clients
        /// </summary>
        private void SendState()
        {
            if (GameManager.Instance.PlayerCount == 2)
            {
                SendAll(GameChannelType.UnReliable, GameManager.Instance.State);
            }
            
            // Making deep copy for states.
            GameManager.Instance.StateHistory.Add(GameManager.Instance.State.Copy());
            // Increase state id.
            GameManager.Instance.State.StateId++;
            // Keeps last 20 state for server lag compensation.
            while (GameManager.Instance.StateHistory.Count > 20)
            {
                var state = GameManager.Instance.StateHistory[0];

                GameManager.Instance.StateHistory.RemoveAt(0);
                state.Dispose();
            }
        }

        /// <summary>
        /// Fetch messages and adds them in terms of message priority which is message type. Please see `GameMessageType`
        /// </summary>
        private void ProcessMessages()
        {
            var queue = new PriorityQueue<MessageData>();
            for (int i = 0; i < yojimbo.MaxClients; i++)
            {
                if (_server.IsClientConnected(i))
                {
                    for (int j = 0; j < _config.numChannels; j++)
                    {
                        var message = _server.ReceiveMessage(i, j);
                        while (message != null)
                        {
                            queue.Enqueue(new MessageData(message, i, (GameChannelType) j));
                            message = _server.ReceiveMessage(i, j);
                        }
                    }
                }
            }

            if (queue.Count() > 0)
            {
                OnMessage?.Invoke(queue);
            }
        }

        /// <summary>
        /// Frees message from memory in terms of given client index. 
        /// </summary>
        /// <param name="messageData"></param>
        public void ReleaseMessage(MessageData messageData)
        {
            _server.ReleaseMessage(messageData.clientIndex, ref messageData.m);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="channelType"></param>
        /// <param name="message"></param>
        public void SendAll(GameChannelType channelType, Message message)
        {
            for (int i = 0; i < yojimbo.MaxClients; i++)
            {
                if (_server.IsClientConnected(i))
                {
                    _server.SendMessage(i, (int) channelType, message);
                }
            }
        }

        /// <summary>
        /// Fetches client id with given index
        /// </summary>
        /// <param name="clientIndex">Client index starts from 0</param>
        /// <returns></returns>
        public ulong GetClientId(int clientIndex)
        {
            if (_server.IsClientConnected(clientIndex))
            {
                return _server.GetClientId(clientIndex);
            }

            return 0;
        }
    }
}