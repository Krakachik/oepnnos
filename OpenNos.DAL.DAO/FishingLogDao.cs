using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenNos.Core;
using OpenNos.DAL.EF;
using OpenNos.DAL.EF.Helpers;
using OpenNos.DAL.Interface;
using OpenNos.Data;
using OpenNos.Data.Enums;
using OpenNos.Mapper;
using OpenNos.Mapper.Mappers;

namespace OpenNos.DAL.DAO
{
    public class FishingLogDao : IFishingLogDao
    {
        public IEnumerable<FishingLogDto> LoadByCharacterId(long characterId)
        {
            var context = DataAccessHelper.CreateContext();
            var result = new List<FishingLogDto>();
            foreach (var entity in context.FishingLogs.Where(s => s.CharacterId == characterId))
            {
                var dto = new FishingLogDto();
                FishingLogMapper.ToFishingLogDto(entity, dto);
                result.Add(dto);
            }
            return result;
        }

        public SaveResult InsertOrUpdateFromList(IEnumerable<FishingLogDto> fishes)
        {
            try
            {
                var context = DataAccessHelper.CreateContext();
                context.Configuration.AutoDetectChangesEnabled = false;
                foreach (var card in fishes)
                {
                    InsertOrUpdate(card);
                }
                context.Configuration.AutoDetectChangesEnabled = true;
                context.SaveChanges();
                return SaveResult.Inserted;
            }
            catch (Exception e)
            {
                return SaveResult.Error;
            }
        }

        public SaveResult InsertOrUpdate(FishingLogDto card)
        {
            try
            {
                var context = DataAccessHelper.CreateContext();
                long CardId = card.Id;
                var entity = context.FishingLogs.FirstOrDefault(c => c.Id == CardId);

                if (entity == null)
                {
                    card = insert(card, context);
                    return SaveResult.Inserted;
                }

                card = update(entity, card, context);
                return SaveResult.Updated;
            }
            catch (Exception e)
            {
                return SaveResult.Error;
            }
        }

        private static FishingLogDto insert(FishingLogDto card, OpenNosContext context)
        {
            var entity = new FishingLogEntity();
            FishingLogMapper.ToFishingLogEntity(card, entity);
            context.FishingLogs.Add(entity);
            context.SaveChanges();
            if (FishingLogMapper.ToFishingLogDto(entity, card))
            {
                return card;
            }

            return null;
        }

        private static FishingLogDto update(FishingLogEntity entity, FishingLogDto card, OpenNosContext context)
        {
            if (entity != null)
            {
                FishingLogMapper.ToFishingLogEntity(card, entity);
                context.SaveChanges();
            }

            if (FishingLogMapper.ToFishingLogDto(entity, card))
            {
                return card;
            }

            return null;
        }
    }
}