using OpenNos.Core;

namespace NosTale.Packets
{
    [PacketHeader("fws")]
    public class FwsPacket : PacketDefinition
    {
        [PacketIndex(0)]
        public short ItemVNum { get; set; }
    }
}
