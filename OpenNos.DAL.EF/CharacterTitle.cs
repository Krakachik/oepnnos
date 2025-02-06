using System;

namespace OpenNos.Data
{
    [Serializable]
    public class CharacterTitle
    {
        #region Properties

        public long CharacterTitleId { get; set; }

        public long CharacterId { get; set; }

        public long TitleVnum { get; set; }

        public byte Stat { get; set; }

        #endregion
    }
}