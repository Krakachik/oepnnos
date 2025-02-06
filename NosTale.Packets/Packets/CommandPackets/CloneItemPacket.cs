﻿////<auto-generated <- Codemaid exclusion for now (PacketIndex Order is important for maintenance)

using OpenNos.Core;
using OpenNos.Domain;

namespace NosTale.Packets.Packets.CommandPackets
{
    [PacketHeader("$CloneItem", PassNonParseablePacket = true, Authority = AuthorityType.GameMaster)]
    public class CloneItemPacket : PacketDefinition
    {
        #region Properties

        [PacketIndex(0)]
        public byte Slot { get; set; }

        #endregion

        public static string ReturnHelp() => "$CloneItem <Slot>";
    }
}