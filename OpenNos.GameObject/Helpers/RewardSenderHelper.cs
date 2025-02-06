using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenNos.Domain;
using OpenNos.DAL.EF;
using OpenNos.Core;
using OpenNos.GameObject;
using OpenNos.Master.Library;
using OpenNos.DAL;
using OpenNos.Data;
using OpenNos.GameObject.Extension;

using OpenNos.GameObject.Networking;
using OpenNos.Master.Library.Client;

namespace OpenNos.GameObject.Helpers
{
    public class RewardSenderHelper
    {
        public async Task SendInstantBattleWaveRewards(ClientSession session, InstantBattleWaveReward itm)
        {
            if (session == null)
            {
                return;
            }


            // Fuck you :)
            short[] allowedVnums = { 946, 945, 1436, 1438, 1437, 2014, 2018, 1439, 2112, 2113, 2114, 2115, 416, 2104 };

            var monstersOnMap = session.CurrentMapInstance?.Monsters?.Where(s => s != null && s.IsAlive && !s.IsMeteorite);

            bool sendReward = true;

            if (monstersOnMap != null)
            {
                if (monstersOnMap.Any())
                {
                    if (monstersOnMap.Select(monster => allowedVnums.Any(s => s == monster.MonsterVNum)).Any(isAllowed => !isAllowed))
                    {
                        sendReward = false;
                    }
                }
            }

            if (session.HasCurrentMapInstance && !sendReward)
            {
                session.SendPacket(session.Character.GenerateSay("You didn't clean the map, you don't get any rewards...", 11));
                return;
            }

            session.SendPacket(session.Character.GenerateSay($"The rewards for the wave {itm.RewardLevel} will be sent into your inventory !", 11));

            if (session.Character.EffectFromTitle == null)
            {
                session.Character.EffectFromTitle = new ThreadSafeGenericList<BCard>();
            }

            var hasEndlessBattleTitle = session.Character.EffectFromTitle.Any(s => s.Type == (byte)BCardType.CardType.BonusBCards && s.SubType == (byte)AdditionalTypes.BonusTitleBCards.InstantBattleAFKPower);

            var totalGold = (int)(itm.Gold * (hasEndlessBattleTitle ? 1.5 : 1));
            session.Character.GetGold(totalGold);

            foreach (var reward in itm.Items)
            {
                var totalAmount = (int)(reward.Amount * (hasEndlessBattleTitle ? 1.5 : 1));
                session.Character.GiftAdd((short)reward.VNum, (short)totalAmount);
                await Task.Delay(100);
            }
        }
    }
}
