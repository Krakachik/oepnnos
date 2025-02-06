using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenNos.Data
{
    public class FishingSpotsDto
    {
        public long Id { get; set; }

        public short MapId { get; set; }

        public short MapX { get; set; }

        public short MapY { get; set; }

        public short Direction { get; set; }

        public short MinLevel { get; set; }
    }
}