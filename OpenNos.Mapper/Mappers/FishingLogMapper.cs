using OpenNos.DAL.EF;
using OpenNos.Data;

namespace OpenNos.Mapper.Mappers
{
    public class FishingLogMapper
    {
        public static bool ToFishingLogEntity(FishingLogDto input, FishingLogEntity output)
        {
            if (input == null)
            {
                return false;
            }

            output.CharacterId = input.CharacterId;
            output.FishCount = input.FishCount;
            output.FishId = input.FishId;
            output.Id = input.Id;
            output.MaxLength = input.MaxLength;

            return true;
        }

        public static bool ToFishingLogDto(FishingLogEntity input, FishingLogDto output)
        {
            if (input == null)
            {
                return false;
            }

            output.CharacterId = input.CharacterId;
            output.FishCount = input.FishCount;
            output.FishId = input.FishId;
            output.Id = input.Id;
            output.MaxLength = input.MaxLength;

            return true;
        }
    }
}