using OpenNos.Core;

namespace NosTale.Packets.Packets.ClientPackets
{
    [PacketHeader("tit_eq")]
    public class TitEqPacket : PacketDefinition
    {
        #region Properties
        [PacketIndex(0)] public byte Type { get; set; }

        [PacketIndex(1)] public short ItemVnum { get; set; }

        #endregion
    }
}