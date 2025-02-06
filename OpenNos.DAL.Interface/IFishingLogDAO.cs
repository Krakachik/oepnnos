using OpenNos.Data;
using OpenNos.Data.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenNos.DAL.Interface
{
    public interface IFishingLogDao
    {
        IEnumerable<FishingLogDto> LoadByCharacterId(long characterId);

        SaveResult InsertOrUpdateFromList(IEnumerable<FishingLogDto> logs);
    }
}