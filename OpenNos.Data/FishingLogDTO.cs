using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenNos.Data
{
    public class FishingLogDto
    {
        public long Id { get; set; }

        public long CharacterId { get; set; }

        public short FishId { get; set; }

        public int FishCount { get; set; }

        public int MaxLength { get; set; }
    }
}