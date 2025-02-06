using OpenNos.Domain;
using OpenNos.GameObject.Helpers;
using OpenNos.GameObject.Networking;
using System.Collections.Generic;

namespace OpenNos.GameObject.Extension.Inventory
{
  
    public static class UpgradeTattooExtension
    {
        #region Method

        public static void UpgradeTattoo(this CharacterSkill e, ClientSession s, bool isProtected)
        {
            #region Configuration
            byte[] percentDestroyed = { 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            int[] goldPrice = { 30000, 67000, 140000, 230000, 380000, 540000, 770000, 960000, 1200000 };
            short[] percentSuccesss = { 100, 100, 100, 100, 100, 100, 100, 100, 100 };
            short[] percentFail = { 0, 0, 0, 0, 0, 0, 0, 0, 0 };

            #endregion

            if (isProtected && s.Character.Inventory.CountItem(5815) < 1)
            {
                s.SendShopEnd();
                return;
            }

            if (e.TattooUpgrade == 9)
            {
                s.SendShopEnd();
                return;
            }

            if (!e.IsTattoo)
            {
                s.SendShopEnd();
                return;
            }

            var skill = ServerManager.GetSkill(e.SkillVNum);
            var value = e.TattooUpgrade;

            if (skill.Class != 27)
            {
                s.SendShopEnd();
                return;
            }

            
            if (s.Character.Gold < goldPrice[value])
            {
                s.SendShopEnd();
                return;
            }

           //Count Items from Inventory, add NotEnoughItem, SendShopEnd
            
            var rnd = ServerManager.RandomNumber();
            string msg;
            int effectId;
            if (rnd < percentDestroyed[value]) // fail + level --
            {
                if (!isProtected)
                {
                    e.TattooUpgrade--;
                    effectId = 3003;
                    msg = $"The {skill.Name} tattoo improvement FAILED ! and Decreased ! -{e.TattooUpgrade}";
                }
                else
                {
                    effectId = 3004;
                    msg = $"The {skill.Name} tattoo improvement FAILED ! But the level was saved with the scroll !";
                }
            }
            else if (rnd < percentFail[value]) // fail
            {
                effectId = 3004;
                msg = $"The {skill.Name} tattoo improvement FAILED !";
            }
            else // success
            {
                e.TattooUpgrade++;
                effectId = 3005;
                msg = $"The {skill.Name} tattoo has been improved ! +{e.TattooUpgrade}";
            }


            if (isProtected) s.Character.Inventory.RemoveItemAmount(5815);

            s.GoldLess(goldPrice[value]);
            s.SendPacket(s.Character.GenerateSki());
            s.SendPackets(s.Character.GenerateQuicklist());
            s.CurrentMapInstance.Broadcast(
                StaticPacketHelper.GenerateEff(UserType.Player, s.Character.CharacterId, effectId),
                s.Character.PositionX, s.Character.PositionY);
            s.SendPacket(UserInterfaceHelper.GenerateMsg(msg, 0));
            s.SendPacket(UserInterfaceHelper.GenerateSay(msg, 11));
            s.SendPacket(UserInterfaceHelper.GenerateGuri(19, 1, s.Character.CharacterId, 2388));
            s.SendShopEnd();
        }

        #endregion
    }
}