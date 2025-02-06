﻿////<auto-generated <- Codemaid exclusion for now (PacketIndex Order is important for maintenance)

using OpenNos.Core;
using OpenNos.Domain;

namespace NosTale.Packets.Packets.CommandPackets
{
    [PacketHeader("$AddTattoo", PassNonParseablePacket = true, Authority = AuthorityType.GameMaster)]
    public class AddTattooPacket : PacketDefinition
    {
        #region Properties

        [PacketIndex(0)]
        public short TattooVNum { get; set; }

        [PacketIndex(1)]
        public byte TattooUpgrade { get; set; }


        #endregion
    }
}