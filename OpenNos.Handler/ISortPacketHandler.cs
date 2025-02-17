﻿using NosTale.Packets.Packets.ClientPackets;
using OpenNos.Core;
using OpenNos.Domain;
using OpenNos.GameObject;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OpenNos.Handler.Packets.basic
{
    public class ISortPacketHandler : IPacketHandler
    {
        #region Members

        private readonly ClientSession Session;

        #endregion

        #region Instantiation

        public ISortPacketHandler(ClientSession session) => Session = session;

        #endregion

        #region Methods

        public async Task SortAsync(isortPacket e)
        {
            if (!CheckInvType(e.Type))
            {
                await SendErrorMsgAsync(Session);
                return;
            }

            // Like Official server 
            var time = Session.Character.LastISort.AddSeconds(5);
            if (DateTime.Now <= time)
            {
                await SendErrorMsgAsync(Session);
                return;
            }

            SortInv(Session, e.Type);
        }

        private bool CheckInvType(InventoryType type)
        {
            var AllowedEnum = new List<InventoryType>
            {
                InventoryType.Equipment,
                InventoryType.Main,
                InventoryType.Etc,
                InventoryType.Specialist,
                InventoryType.Costume,
                InventoryType.Main,
                InventoryType.Warehouse,
                InventoryType.PetWarehouse
            };

            if (!AllowedEnum.Contains(type))
            {
                return false;
            }

            return true;
        }

        private async Task SendErrorMsgAsync(ClientSession e)
        {
            await e.SendPacketAsync("msgi 3 1808 0 0 0 0 0");
        }

        private void SortInv(ClientSession e, InventoryType type)
        {
            e.Character.LastISort = DateTime.Now;
            e.Character.Inventory.Reorder(e, type);
        }
         
        #endregion
    }
}