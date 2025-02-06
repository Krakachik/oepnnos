using System;
using System.Collections.Generic;
using System.Linq;
using OpenNos.Core;
using OpenNos.DAL.EF;
using OpenNos.DAL.EF.Helpers;
using OpenNos.DAL.Interface;
using OpenNos.Data;
using OpenNos.Data.Enums;
using OpenNos.Mapper.Mappers;

namespace OpenNos.DAL.DAO
{
    public class FishingSpotsDao : IFishingSpotsDao
    {
        public SaveResult InsertOrUpdateFromList(List<FishingSpotsDto> fishes)
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

        public IEnumerable<FishingSpotsDto> LoadAll()
        {
            var context = DataAccessHelper.CreateContext();
            var result = new List<FishingSpotsDto>();
            foreach (var entity in context.FishingSpots)
            {
                var dto = new FishingSpotsDto();
                FishingSpotsMapper.ToFishingSpotsDto(entity, dto);
                result.Add(dto);
            }
            return result;
        }

        public SaveResult InsertOrUpdate(FishingSpotsDto card)
        {
            try
            {
                var context = DataAccessHelper.CreateContext();
                long CardId = card.Id;
                var entity = context.FishingSpots.FirstOrDefault(c => c.Id == CardId);

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

        private static FishingSpotsDto insert(FishingSpotsDto card, OpenNosContext context)
        {
            var entity = new FishingSpotsEntity();
            FishingSpotsMapper.ToFishingSpotsEntity(card, entity);
            context.FishingSpots.Add(entity);
            context.SaveChanges();
            if (FishingSpotsMapper.ToFishingSpotsDto(entity, card))
            {
                return card;
            }

            return null;
        }

        private static FishingSpotsDto update(FishingSpotsEntity entity, FishingSpotsDto card, OpenNosContext context)
        {
            if (entity != null)
            {
                FishingSpotsMapper.ToFishingSpotsEntity(card, entity);
                context.SaveChanges();
            }

            if (FishingSpotsMapper.ToFishingSpotsDto(entity, card))
            {
                return card;
            }

            return null;
        }
    }
}