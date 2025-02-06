using NosTale.Packets;
using NosTale.Packets.ClientPackets;
using OpenNos.Core;
using OpenNos.Domain;
using OpenNos.GameObject;

namespace OpenNos.Handler.World.Basic
{
    public class FishOpenPacketHandler : IPacketHandler
    {
        private ClientSession Session { get; set; }

        public FishOpenPacketHandler(ClientSession session) => Session = session;

        public void ShowFish(FishOpenPacket fishOpen)
        {
           
        }
    }
}
