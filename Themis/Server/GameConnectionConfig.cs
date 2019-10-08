using networkprotocol;
using Themis.Shared.Utils;

namespace Themis.Server
{
    /// <summary>
    /// Connection configs.
    /// </summary>
    public class GameConnectionConfig : ClientServerConfig
    {
        public GameConnectionConfig()
        {
            protocolId                                     = Constants.ProtocolId; //Protocol id for your app
            numChannels                                    = (int) GameChannelType.Count;
            channel[(int) GameChannelType.Reliable].type   = ChannelType.CHANNEL_TYPE_RELIABLE_ORDERED;
            channel[(int) GameChannelType.UnReliable].type = ChannelType.CHANNEL_TYPE_UNRELIABLE_UNORDERED;
        }
    }
}