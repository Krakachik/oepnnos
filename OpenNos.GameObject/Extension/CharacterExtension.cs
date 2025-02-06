using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using OpenNos.Data;
using OpenNos.Domain;

namespace OpenNos.GameObject.Extension
{
    public static class CharacterExtension
    {
        public static string GetFamilyNameType(this Character e)
        {
            var thisRank = e.FamilyCharacter.Authority;

            return thisRank == FamilyAuthority.Member ? "918" :
                thisRank == FamilyAuthority.Familydeputy ? "917" :
                thisRank == FamilyAuthority.Familykeeper ? "916" : 
                thisRank == FamilyAuthority.Head ? "915" : "-1 -";
        }

        public static string GetClassType(this Character e)
        {
            var thisClass = e.Class;

            return thisClass == ClassType.Adventurer ? "35" :
                thisClass == ClassType.Swordsman ? "36" :
                thisClass == ClassType.Archer ? "37" :
                thisClass == ClassType.Magician ? "38" :
                thisClass == ClassType.MartialArtist ? "39" : "0";
        }

        public static void SendShopEnd(this ClientSession s)
        {
            s.SendPacket("shop_end 2");
            s.SendPacket("shop_end 1");
        }

        public static void GoldLess(this ClientSession session, long value)
        {
            session.Character.Gold -= value;
            if (session.Character.Gold <= 0) session.Character.Gold = 0;

            session.SendPacket(session.Character.GenerateGold());
        }

        public static string GenerateFishPacket(this Character character, FishPacketType type, short fishVNum, short fishLength)
        {
            var packet = $"fish {(byte)type} ";

            switch (type)
            {
                case FishPacketType.Fishing:
                    {
                        var current = character.FishingLogs.FirstOrDefault(s => s.FishId == fishVNum);

                        if (current == null)
                        {
                            var log = new FishingLogDto
                            {
                                FishCount = 1,
                                FishId = fishVNum,
                                MaxLength = fishLength,
                                CharacterId = character.CharacterId
                            };

                            character.FishingLogs.Add(log);
                            packet += $"{log.FishId - 10400}.{log.FishCount}.{log.MaxLength}";
                        }
                        else
                        {
                            current.FishCount += 1;

                            if (fishLength > current.MaxLength)
                            {
                                current.MaxLength = fishLength;
                            }

                            character.FishingLogs.Add(current);
                            packet += $"{current.FishId - 10400}.{current.FishCount}.{current.MaxLength}";
                        }
                    }
                    break;

                case FishPacketType.Login:
                    {
                        for (int i = 0; i < 99; i++)
                        {
                            var vnum = i + 10400;
                            var current = character.FishingLogs.FirstOrDefault(s => s.FishId == vnum);

                            if (current == null)
                            {
                                packet += $"{i}.0.0 ";
                            }
                            else
                            {
                                packet += $"{i}.{current.FishCount}.{current.MaxLength} ";
                            }
                        }

                        packet += "2 -1.9409.9416"; 
                    }
                    break;
            }

            return packet;
        }
    }
}
