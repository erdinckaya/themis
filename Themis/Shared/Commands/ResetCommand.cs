using networkprotocol;
using Themis.Server;

namespace Themis.Shared.Commands
{
    /// <summary>
    /// Resets the state of game.
    /// </summary>
    public class ResetCommand : Message
    {
        public ResetCommand() : base(false)
        {
            Type = (int) GameMessageType.Reset;
        }
    }
}