﻿////<auto-generated <- Codemaid exclusion for now (PacketIndex Order is important for maintenance)

using OpenNos.Core;
using OpenNos.Domain;

namespace NosTale.Packets.Packets.CommandPackets
{
    [PacketHeader("$ChangeDignity", "$Dignity" , PassNonParseablePacket = true, Authority = AuthorityType.GameMaster)]
    public class ChangeDignityPacket : PacketDefinition
    {
        #region Properties

        [PacketIndex(0)]
        public float Dignity { get; set; }

        public static string ReturnHelp() => "$ChangeDignity | $Dignity <Value>";

        #endregion
    }
}