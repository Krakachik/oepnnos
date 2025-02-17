using OpenNos.DAL.EF;
using OpenNos.Data;

namespace OpenNos.Mapper.Mappers
{
    public static class CharacterSkillMapper
    {
        #region Methods

        public static bool ToCharacterSkill(CharacterSkillDTO input, CharacterSkill output)
        {
            if (input == null)
            {
                return false;
            }

            output.CharacterId = input.CharacterId;
            output.Id = input.Id;
            output.SkillVNum = input.SkillVNum;
            output.IsTattoo = input.IsTattoo;
            output.TattooUpgrade = input.TattooUpgrade;
            return true;
        }

        public static bool ToCharacterSkillDTO(CharacterSkill input, CharacterSkillDTO output)
        {
            if (input == null)
            {
                return false;
            }

            output.CharacterId = input.CharacterId;
            output.Id = input.Id;
            output.SkillVNum = input.SkillVNum;
            output.IsTattoo = input.IsTattoo;
            output.TattooUpgrade = input.TattooUpgrade;
            return true;
        }

        #endregion
    }
}