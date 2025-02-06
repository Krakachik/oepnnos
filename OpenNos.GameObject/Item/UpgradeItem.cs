/*
 * This file is part of the OpenNos Emulator Project. See AUTHORS file for Copyright information
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 */

using OpenNos.Core;
using OpenNos.Data;
using OpenNos.Domain;
using OpenNos.GameObject.Helpers;

namespace OpenNos.GameObject
{
    public class UpgradeItem : Item
    {
        #region Instantiation

        public UpgradeItem(ItemDTO item) : base(item)
        {
        }

        #endregion

        #region Methods

        public override void Use(ClientSession session, ref ItemInstance inv, byte Option = 0, string[] packetsplit = null)
        {
            if (session.Character.IsVehicled)
            {
                session.SendPacket(session.Character.GenerateSay(Language.Instance.GetMessageFromKey("CANT_DO_VEHICLED"), 10));
                return;
            }

            if (session.CurrentMapInstance.MapInstanceType == MapInstanceType.TalentArenaMapInstance)
            {
                return;
            }

            switch (Effect)
            {
                //Guri Items
                case 9000:
                    switch (EffectValue)
                    {
                        //Lower SP-Protection Scroll
                        case 1:
                            session.SendPacket($"guri 12 1 {session.Character.CharacterId} 27 0");
                            break;

                        //Higher SP-Protection Scroll
                        case 2:
                            session.SendPacket($"guri 12 1 {session.Character.CharacterId} 28 0");
                            break;

                        //Equipment Protection Scroll
                        case 3:
                            session.SendPacket($"guri 12 1 {session.Character.CharacterId} 26 0");
                            break;
                        
                        // Golden Equipment Protection Scroll
                        case 4:
                            session.SendPacket($"guri 12 1 {session.Character.CharacterId} 61 0");
                            break;

                        // Chicken Costume Uppgraderole
                        case 5:
                            session.SendPacket($"guri 12 1 {session.Character.CharacterId} 47 0");
                            break;
                    }
                    break;
            }

            if (Effect == 0)
            {
                if (EffectValue != 0)
                {
                    if (session.Character.IsSitting)
                    {
                        session.Character.IsSitting = false;
                        session.SendPacket(session.Character.GenerateRest());

                        session.SendPacket(UserInterfaceHelper.GenerateGuri(12, 1, session.Character.CharacterId, EffectValue));
                    }
                    else if (EffectValue == 0)
                    {
                        if (packetsplit?.Length > 9 && byte.TryParse(packetsplit[8], out byte TypeEquip) && short.TryParse(packetsplit[9], out short SlotEquip))
                        {
                            if (session.Character.IsSitting)
                            {
                                session.Character.IsSitting = false;
                                session.SendPacket(session.Character.GenerateRest());
                            }
                            if (Option != 0)
                            {
                                bool isUsed = false;
                                switch (inv.ItemVNum)
                                {
                                    case 1219:
                                    case 9130:
                                        ItemInstance equip = session.Character.Inventory.LoadBySlotAndType(SlotEquip, (InventoryType)TypeEquip);
                                        if (equip?.IsFixed == true)
                                        {
                                            equip.IsFixed = false;
                                            session.SendPacket(StaticPacketHelper.GenerateEff(UserType.Player, session.Character.CharacterId, 3003));
                                            session.SendPacket(UserInterfaceHelper.GenerateGuri(17, 1, session.Character.CharacterId, SlotEquip));
                                            session.SendPacket(session.Character.GenerateSay(Language.Instance.GetMessageFromKey("ITEM_UNFIXED"), 12));
                                            isUsed = true;
                                        }
                                        break;

                                    case 5815: // Tattoo
                                        session.SendPacket(
                                            UserInterfaceHelper.GenerateGuri(12, 1, session.Character.CharacterId, 90));
                                        break;

                                    case 5813: // Rune Premium
                                        session.SendPacket(
                                            UserInterfaceHelper.GenerateGuri(12, 1, session.Character.CharacterId, 89));
                                        break;

                                    case 5823: // Rune basic
                                        session.SendPacket(
                                            UserInterfaceHelper.GenerateGuri(12, 1, session.Character.CharacterId, 93));
                                        break;

                                    case 5811:
                                        var weapon =
                                            session.Character.Inventory.LoadBySlotAndType(SlotEquip,
                                                (InventoryType)TypeEquip);
                                        if (weapon != null && weapon.IsBreaked)
                                        {
                                            weapon.IsBreaked = false;
                                            session.SendPacket(
                                                UserInterfaceHelper.GenerateMsg($"The {weapon.Item.Name}'s runes have been fixed.", 0));
                                            session.SendPacket(weapon.GenerateInventoryAdd());
                                            session.SendPacket(session.Character.GenerateEq());
                                            session.SendPacket(session.Character.GenerateEquipment());
                                        }
                                        break;

                                    case 1365:
                                    case 9039:
                                        ItemInstance specialist = session.Character.Inventory.LoadBySlotAndType(SlotEquip, (InventoryType)TypeEquip);
                                        if (specialist?.Rare == -2)
                                        {
                                            specialist.Rare = 0;
                                            session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("SP_RESURRECTED"), 0));
                                            session.SendPacket(UserInterfaceHelper.GenerateGuri(13, 1, session.Character.CharacterId, 1));
                                            session.Character.SpPoint = 10000;
                                            if (session.Character.SpPoint > 10000)
                                            {
                                                session.Character.SpPoint = 10000;
                                            }
                                            session.SendPacket(session.Character.GenerateSpPoint());
                                            session.SendPacket(specialist.GenerateInventoryAdd());
                                            isUsed = true;
                                        }
                                        break;
                                }
                                if (!isUsed)
                                {
                                    session.SendPacket(session.Character.GenerateSay(Language.Instance.GetMessageFromKey("ITEM_IS_NOT_FIXED"), 11));
                                }
                                else
                                {
                                    session.Character.Inventory.RemoveItemFromInventory(inv.Id);
                                }
                            }
                            else
                            {
                                session.SendPacket($"qna #u_i^1^{session.Character.CharacterId}^{(byte)inv.Type}^{inv.Slot}^0^1^{TypeEquip}^{SlotEquip} {Language.Instance.GetMessageFromKey("QNA_ITEM")}");
                            }
                        }
                    }
                }
                else
                {
                    Logger.Warn(string.Format(Language.Instance.GetMessageFromKey("NO_HANDLER_ITEM"), GetType(), VNum, Effect, EffectValue));
                }
            }

            #endregion
        }
    }
}