﻿////<auto-generated <- Codemaid exclusion for now (PacketIndex Order is important for maintenance)

using OpenNos.Core;
using OpenNos.Domain;

namespace NosTale.Packets.Packets.CommandPackets
{
    [PacketHeader("$XpRate", PassNonParseablePacket = true, Authority = AuthorityType.Administrator)]
    public class XpRatePacket : PacketDefinition
    {
        #region Properties

        [PacketIndex(0)]
        public int Value { get; set; }

        public static string ReturnHelp() => "$XpRate <Value>";

        #endregion
    }
}