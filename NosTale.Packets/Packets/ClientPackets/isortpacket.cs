using OpenNos.Core;
using OpenNos.Domain;

namespace NosTale.Packets.Packets.ClientPackets
{
    [PacketHeader("isort")]
    public class isortPacket : PacketDefinition
    {
        #region Properties


        [PacketIndex(0)]
        public InventoryType Type { get; set; }

        #endregion
    }
}