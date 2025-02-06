using System.Collections.Generic;
using OpenNos.DAL.EF;
using OpenNos.Data;

namespace OpenNos.Mapper.Mappers
{
    public class FishingSpotsMapper
    {
        public static bool ToFishingSpotsEntity(FishingSpotsDto input, FishingSpotsEntity output)
        {
            if (input == null)
            {
                return false;
            }

            output.Direction = input.Direction;
            output.Id = input.Id;
            output.MapId = input.MapId;
            output.MapX = input.MapX;
            output.MapY = input.MapY;
            output.MinLevel = input.MinLevel;

            return true;
        }

        public static bool ToFishingSpotsDto(FishingSpotsEntity input, FishingSpotsDto output)
        {
            if (input == null)
            {
                return false;
            }

            output.Direction = input.Direction;
            output.Id = input.Id;
            output.MapId = input.MapId;
            output.MapX = input.MapX;
            output.MapY = input.MapY;
            output.MinLevel = input.MinLevel;

            return true;
        }

        public FishingSpotsEntity Map(FishingSpotsDto input)
        {
            if (input == null)
            {
                return null;
            }

            return new FishingSpotsEntity
            {
                Direction = input.Direction,
                Id = input.Id,
                MapId = input.MapId,
                MapX = input.MapX,
                MapY = input.MapY,
                MinLevel = input.MinLevel
            };
        }

        public FishingSpotsDto Map(FishingSpotsEntity input)
        {
            if (input == null)
            {
                return null;
            }

            return new FishingSpotsDto
            {
                Direction = input.Direction,
                Id = input.Id,
                MapId = input.MapId,
                MapX = input.MapX,
                MapY = input.MapY,
                MinLevel = input.MinLevel
            };
        }

        public IEnumerable<FishingSpotsDto> Map(IEnumerable<FishingSpotsEntity> input)
        {
            var result = new List<FishingSpotsDto>();

            foreach (var data in input)
            {
                result.Add(Map(data));
            }

            return result;
        }

        public IEnumerable<FishingSpotsEntity> Map(IEnumerable<FishingSpotsDto> input)
        {
            var result = new List<FishingSpotsEntity>();

            foreach (var data in input)
            {
                result.Add(Map(data));
            }

            return result;
        }
    }
}