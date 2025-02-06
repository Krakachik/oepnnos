﻿////<auto-generated <- Codemaid exclusion for now (PacketIndex Order is important for maintenance)

using OpenNos.Core;
using OpenNos.Domain;

namespace NosTale.Packets.Packets.CommandPackets
{
    [PacketHeader("$ReturnPoint", PassNonParseablePacket = true, Authority = AuthorityType.GameMaster)]
    public class ReturnPointPacket : PacketDefinition
    {
        #region Properties

        [PacketIndex(0)]
        public byte ReturnPointId { get; set; }

        #endregion

        #region Methods

        public static string ReturnHelp()
        {
            return "$ReturnPoint RETURNPOINTID";
        }

        #endregion
    }
}