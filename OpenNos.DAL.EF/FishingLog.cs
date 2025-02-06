using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenNos.DAL.EF
{
    [Table("FishingLog")]
    public class FishingLogEntity
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key]
        public long Id { get; set; }

        public long CharacterId { get; set; }

        public short FishId { get; set; }

        public int FishCount { get; set; }

        public int MaxLength { get; set; }
    }
}