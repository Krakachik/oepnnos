using System.Collections.Generic;

namespace OpenNos.GameObject
{
    public class InstantBattleItem
    {
        public int VNum { get; set; }

        public int Amount { get; set; }
    }

    public class InstantBattleWaveReward
    {
        public InstantBattleWaveReward()
        {
            Items = new List<InstantBattleItem>();
        }

        public byte RewardLevel { get; set; }

        public int Gold { get; set; }

        public List<InstantBattleItem> Items { get; set; }
    }
}

