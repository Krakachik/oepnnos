using OpenNos.Data;
using OpenNos.Data.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenNos.DAL.Interface
{
    public interface IFamilyQuestsDAO
    {
        IEnumerable<FamilyQuestsDTO> LoadByFamilyAndQuestId(FamilyQuestsDTO log);

        SaveResult InsertOrUpdate(FamilyQuestsDTO FamilyQuests);

        IEnumerable<FamilyQuestsDTO> LoadAllByFamilyId(long Id);
    }
}
