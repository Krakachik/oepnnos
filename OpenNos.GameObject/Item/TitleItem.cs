using System.Linq;
using OpenNos.Core;
using OpenNos.Data;
using OpenNos.Domain;

namespace OpenNos.GameObject
{
    public class TitleItem : Item
    {
        #region Instantiation

        public TitleItem(ItemDTO item) : base(item)
        {
        }

        #endregion

        #region Methods

        public override void Use(ClientSession session, ref ItemInstance inv, byte Option = 0,
            string[] packetsplit = null)
        {
            
        }

        #endregion
    }
}