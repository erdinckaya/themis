using System.Collections.Generic;
using System.Linq;
using networkprotocol;
using Themis.Server;
using Themis.Shared.Commands;
using Themis.Shared.Data;
using Themis.Shared.Utils;
using UnityEngine;

namespace Themis.Game
{
    /// <summary>
    /// Manages Game.
    /// </summary>
    public class GameManager
    {
        /// <summary>
        /// Pause Flag for Game.
        /// </summary>
        public bool IsPaused { get; set; }

        /// <summary>
        /// Current Game State
        /// </summary>
        public GameState State;

        /// <summary>
        /// State history for game.
        /// </summary>
        public readonly List<GameState> StateHistory = new List<GameState>();


        /// <summary>
        /// Static instance for game to reach necessary classes.
        /// </summary>
        private static GameManager _instance;

        public static GameManager Instance => _instance ?? (_instance = new GameManager());

        /// <summary>
        /// Game server instance.
        /// </summary>
        private GameServer _gameServer;

        /// <summary>
        /// Total player count. Normally it is between 0 and 2 
        /// </summary>
        public int PlayerCount;

        /// <summary>
        /// Register game server to game manager, and also registers message and world step
        /// event handlers.
        /// </summary>
        /// <param name="gameServer"></param>
        public void SetGameServer(GameServer gameServer)
        {
            _gameServer           = gameServer;
            _gameServer.OnMessage = OnMessage;
            _gameServer.OnTick    = Step;
            State                 = new GameState();
        }

        /// <summary>
        /// Message consumer function
        /// </summary>
        /// <param name="queue"></param>
        private void OnMessage(PriorityQueue<MessageData> queue)
        {
            var          playerMoves = new List<PlayerMove>();
            MessageData? playerHit   = null;
            while (queue.Count() > 0)
            {
                var messageData = queue.Dequeue();
                int clientIndex = messageData.clientIndex;
                var channelType = messageData.channelType;
                var message     = messageData.m;
                switch ((GameMessageType) message.Type)
                {
                    case GameMessageType.PlayerMove:
                        playerMoves.Add((PlayerMove) message);
                        break;
                    case GameMessageType.PlayerHit:
                        playerHit = messageData;
                        break;
                    case GameMessageType.Reset:
                        ResetGame();
                        break;
                    case GameMessageType.Pause:
                        PauseGame();
                        break;
                    case GameMessageType.ChangeBallSpeed:
                        ChangeBallSpeedTest((ChangeBallSpeed_Test) message);
                        break;
                }

                _gameServer.ReleaseMessage(messageData);
            }

            // There can be lots of player moves so that we need to sort them and get the most significant one
            // which is the one who has the biggest internal state id.
            ProcessPlayerMoveMessage(playerMoves);
            // We need to check player hit after player moves
            if (playerHit != null)
            {
                ProcessPlayerHit(playerHit.Value.clientIndex, playerHit.Value.channelType, (PlayerHit) playerHit.Value.m);
            }
        }

        /// <summary>
        /// Pauses or resumes game and sends game pause state to clients.
        /// This functions works only game is ready and running.
        /// </summary>
        private void PauseGame()
        {
            if (PlayerCount != 2)
            {
                return;
            }

            IsPaused = !IsPaused;
            var pauseCommand = new PauseCommand {Value = IsPaused};
            _gameServer.SendAll(GameChannelType.Reliable, pauseCommand);
        }

        /// <summary>
        /// Changes ball speed with given parameter.
        /// </summary>
        /// <param name="message"></param>
        private void ChangeBallSpeedTest(ChangeBallSpeed_Test message)
        {
            State.Ball.pace += message.delta;
        }

        /// <summary>
        /// Resets game state to origin.
        /// </summary>
        private void ResetGame()
        {
            State.Ball              = new Ball();
            State.Players[0].center = Constants.PlayerOneStartPos;
            State.Players[1].center = Constants.PlayerTwoStartPos;
            _gameServer.SendAll(GameChannelType.Reliable, new ResetCommand());
        }

        /// <summary>
        /// Processes player hit. In order to do that it resimulate old state and checks intersections
        /// Between ball and player.
        /// </summary>
        /// <param name="clientIndex"></param>
        /// <param name="channelType"></param>
        /// <param name="message"></param>
        private void ProcessPlayerHit(int clientIndex, GameChannelType channelType, PlayerHit message)
        {
            foreach (var player in State.Players)
            {
                if (player.playerId == message.PlayerId)
                {
                    var ball         = State.Ball;
                    var ballCenter   = ball.center;
                    var playerCenter = player.center;
                    if (message.StateId != State.StateId)
                    {
                        yojimbo.printf(yojimbo.LOG_LEVEL_INFO, $"State id is not equal M: {message.StateId} S: {State.StateId}\n");
                    }

                    // Searching exact state, namely Server Lag compensation.
                    for (var i = StateHistory.Count - 1; i >= 0; i--)
                    {
                        if (StateHistory[i].StateId == message.StateId)
                        {
                            yojimbo.printf(yojimbo.LOG_LEVEL_INFO, $"Found exact time in state {message.StateId} index {i}\n");
                            ball       = StateHistory[i].Ball;
                            ballCenter = StateHistory[i].Ball.center;
                            foreach (var p in StateHistory[i].Players)
                            {
                                if (p.playerId == message.PlayerId)
                                {
                                    playerCenter = p.center;
                                    break;
                                }
                            }

                            break;
                        }
                    }


                    ballCenter[2]   = 0;
                    playerCenter[2] = 0;
                    // Check distance between ball and player in that old state.
                    var distance = (ballCenter - playerCenter).magnitude;
                    if (distance <= ball.radius + player.radius)
                    {
                        // Update real ball!
                        yojimbo.printf(yojimbo.LOG_LEVEL_INFO,
                            $"Player hit {clientIndex} {message.PlayerId} {message.Direction} old {State.Ball.direction}\n");
                        State.Ball.direction = message.Direction;
                    }

                    break;
                }
            }
        }

        /// <summary>
        /// Steps the world. This functions iterates world state with know behaviors such as ball movement.
        /// You predict movements and scales in there in terms of animations.
        /// </summary>
        /// <param name="time"></param>
        private void Step(double time)
        {
            if (IsPaused)
            {
                return;
            }

            var ball = State.Ball;
            ball.center += new Vector3(ball.direction.x, ball.direction.y, 0) * ball.pace;
        }

        /// <summary>
        /// There can be lots of movements or duplicate packages in message fetching.
        /// Hence we need to get most significant package which is last movement package.
        /// </summary>
        /// <param name="moves"></param>
        private void ProcessPlayerMoveMessage(IReadOnlyCollection<PlayerMove> moves)
        {
            if (!moves.Any())
            {
                return;
            }

            foreach (var player in State.Players)
            {
                ulong      max = 0;
                PlayerMove pm  = null;
                foreach (var playerMove in moves)
                {
                    if (max <= playerMove.stateId && player.playerId == playerMove.playerId)
                    {
                        max = playerMove.stateId;
                        pm  = playerMove;
                    }
                }

                if (pm != null)
                {
                    player.center = pm.center;
                }
            }
        }
    }
}