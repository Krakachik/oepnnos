using System;
using System.Collections.Generic;
using System.Linq;
using OpenNos.Core;
using OpenNos.DAL.EF;
using OpenNos.DAL.EF.Helpers;
using OpenNos.DAL.Interface;
using OpenNos.Data;
using OpenNos.Data.Enums;

namespace OpenNos.DAL.DAO
{
    public class FishInfoDao : IFishInfoDao
    {
        public SaveResult InsertOrUpdateFromList(List<FishInfoDto> fishes)
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

        public IEnumerable<FishInfoDto> LoadAll()
        {
            var context = DataAccessHelper.CreateContext();
            var result = new List<FishInfoDto>();
            foreach (var entity in context.FishInfo)
            {
                var dto = new FishInfoDto();
                Mapper.Mappers.FishInfoMapper.ToFishInfoDto(entity, dto);
                result.Add(dto);
            }
            return result;
        }

        public SaveResult InsertOrUpdate(FishInfoDto card)
        {
            try
            {
                var context = DataAccessHelper.CreateContext();
                long CardId = card.Id;
                var entity = context.FishInfo.FirstOrDefault(c => c.Id == CardId);

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

        private static FishInfoDto insert(FishInfoDto card, OpenNosContext context)
        {
            var entity = new FishInfoEntity();
            Mapper.Mappers.FishInfoMapper.ToFishInfoEntity(card, entity);
            context.FishInfo.Add(entity);
            context.SaveChanges();
            if (Mapper.Mappers.FishInfoMapper.ToFishInfoDto(entity, card))
            {
                return card;
            }

            return null;
        }

        private static FishInfoDto update(FishInfoEntity entity, FishInfoDto card, OpenNosContext context)
        {
            if (entity != null)
            {
                Mapper.Mappers.FishInfoMapper.ToFishInfoEntity(card, entity);
                context.SaveChanges();
            }

            if (Mapper.Mappers.FishInfoMapper.ToFishInfoDto(entity, card))
            {
                return card;
            }

            return null;
        }
    }
}