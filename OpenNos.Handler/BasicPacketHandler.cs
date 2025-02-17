/*
 * This file is part of the OpenNos Emulator Project. See AUTHORS file for Copyright information
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 */

using OpenNos.Core;
using OpenNos.Core.Extensions;
using OpenNos.DAL;
using OpenNos.Data;
using OpenNos.Domain;
using OpenNos.GameObject;
using OpenNos.GameObject.Event;
using OpenNos.GameObject.Helpers;
using OpenNos.GameObject.Networking;
using OpenNos.Master.Library.Client;
using OpenNos.Master.Library.Data;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using NosTale.Packets.Packets.ClientPackets;
using NosTale.Packets.Packets.CustomPackets;
using OpenNos.GameObject.Extension;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Globalization;

namespace OpenNos.Handler
{
    public class BasicPacketHandler : IPacketHandler
    {
        #region Instantiation

        public BasicPacketHandler(ClientSession session) => Session = session;

        #endregion Instantiation

        #region Properties

        private ClientSession Session { get; }

        private ScriptedInstance raid { get; }

        public string EqPacket { get; private set; }

        public ScriptedInstance timeSpace { get; set; }

        public GeneralLogDTO logs { get; set; }

        #endregion Properties

        #region Methods

        public void BankAction(GboxPacket gboxPacket)
        {
            // just add a simple return instead comment

            return;

            if (Session.Character.InExchangeOrTrade)
            {
                return;
            }

            switch (gboxPacket.Type)
            {
                case BankActionType.Deposit:
                    if (gboxPacket.Option == 0)
                    {
                        Session.SendPacket($"qna #gbox^1^{gboxPacket.Amount}^1 Want to deposit {gboxPacket.Amount}000 gold?");
                        return;
                    }

                    if (gboxPacket.Option == 1)
                    {
                        if (gboxPacket.Amount <= 0)
                        {
                            //Packet hacking duplication
                            return;
                        }

                        Session.SendPacket(Session.Character.GenerateSmemo(Language.Instance.GetMessageFromKey("BANK_DEPOSIT"), (byte)SmemoType.Information));
                        if (Session.Account.BankMoney + gboxPacket.Amount * 1000 > ServerManager.Instance.MaxBankGold)
                        {
                            Session.SendPacket(UserInterfaceHelper.GenerateInfo(Language.Instance.GetMessageFromKey("MAX_GOLD_BANK_REACHED")));
                            Session.SendPacket(Session.Character.GenerateSmemo(Language.Instance.GetMessageFromKey("MAX_GOLD_BANK_REACHED"), (byte)SmemoType.Error));
                            return;
                        }

                        if (Session.Character.Gold < gboxPacket.Amount * 1000)
                        {
                            Session.SendPacket(UserInterfaceHelper.GenerateInfo(Language.Instance.GetMessageFromKey("NOT_ENOUGH_GOLD")));
                            Session.SendPacket(Session.Character.GenerateSmemo(Language.Instance.GetMessageFromKey("NOT_ENOUGH_GOLD"), (byte)SmemoType.Error));
                            return;
                        }
                        Session.Account.BankMoney += gboxPacket.Amount * 1000;
                        Session.Character.Gold -= gboxPacket.Amount * 1000;
                        Session.SendPacket(Session.Character.GenerateGold());
                        Session.SendPacket(Session.Character.GenerateGb((byte)GoldBankPacketType.Deposit));
                        Session.SendPacket(Session.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("BANK_BALANCE"), Session.Account.BankMoney, Session.Character.Gold), 1));
                        Session.SendPacket(Session.Character.GenerateSmemo(string.Format(Language.Instance.GetMessageFromKey("BANK_BALANCE"), Session.Account.BankMoney, Session.Character.Gold),
                            (byte)SmemoType.Balance));
                    }

                    break;

                case BankActionType.Withdraw:
                    if (gboxPacket.Option == 0)
                    {
                        Session.SendPacket($"qna #gbox^2^{gboxPacket.Amount}^1 Would you like to withdraw {gboxPacket.Amount}000 gold? (Fee: 0 gold)");
                        return;
                    }

                    if (gboxPacket.Option == 1)
                    {
                        if (gboxPacket.Amount <= 0)
                        {
                            //Packet hacking duplication
                            return;
                        }

                        Session.SendPacket(Session.Character.GenerateSmemo(Language.Instance.GetMessageFromKey("WITHDRAW_BANK"), (byte)SmemoType.Information));

                        if (Session.Character.Gold + gboxPacket.Amount * 1000 > ServerManager.Instance.MaxGold)
                        {
                            Session.SendPacket(UserInterfaceHelper.GenerateInfo(Language.Instance.GetMessageFromKey("TOO_MUCH_GOLD")));
                            Session.SendPacket(Session.Character.GenerateSmemo(Language.Instance.GetMessageFromKey("TOO_MUCH_GOLD"), (byte)SmemoType.Error));
                            return;
                        }

                        if (Session.Account.BankMoney < gboxPacket.Amount * 1000)
                        {
                            Session.SendPacket(UserInterfaceHelper.GenerateInfo("NOT_ENOUGH_FUNDS"));
                            Session.SendPacket(Session.Character.GenerateSmemo(Language.Instance.GetMessageFromKey("NOT_ENOUGH_FUNDS"), (byte)SmemoType.Error));
                            return;
                        }

                        Session.Account.BankMoney -= gboxPacket.Amount * 1000;
                        Session.Character.Gold += gboxPacket.Amount * 1000;
                        Session.SendPacket(Session.Character.GenerateGold());
                        Session.SendPacket(Session.Character.GenerateGb((byte)GoldBankPacketType.Withdraw));
                        Session.SendPacket(Session.Character.GenerateSmemo(Language.Instance.GetMessageFromKey("BANK_BALANCE"), (byte)SmemoType.Balance));
                    }

                    break;
            }
        }

        public void TitleSystem(TitEqPacket titEqPacket)
        {
            var aa = Session.Character.Title.FirstOrDefault(s => s.TitleVnum == titEqPacket.ItemVnum);

            if (aa == null) return;

            switch (titEqPacket.Type)
            {
                case 1:
                    //show
                    foreach (var a in Session.Character.Title.Where(s => s != aa))
                        switch (a.Stat)
                        {
                            case 7 when a != aa:
                                a.Stat = 5;
                                break;
                            case 3 when a != aa:
                                a.Stat = 1;
                                break;
                                /*case 5 when a != aa:
                                        a.Stat = 7;
                                        break;*/
                        }

                    aa.Stat = (byte)(aa.Stat == 1 ? 3 : aa.Stat == 5 ? 7 : aa.Stat == 7 ? 5 : 1);
                    break;

                case 2:
                    //effect
                    foreach (var a in Session.Character.Title)
                        switch (a.Stat)
                        {
                            case 7 when a != aa:
                                a.Stat = 3;
                                break;
                            case 5 when a != aa:
                                a.Stat = 1;
                                break;
                                /*case 3 when a != aa:
                                        a.Stat = 7;
                                        break;*/
                        }

                    aa.Stat = (byte)(aa.Stat == 1 ? 5 : aa.Stat == 3 ? 7 : aa.Stat == 7 ? 3 : 1);

                    break;

                default:
                    return;
            }

            Session.SendPacket(Session.Character.GenerateTitle());
            Session.CurrentMapInstance.Broadcast(Session.Character.GenerateTitInfo());
            Session.Character.GetEffectFromTitle();

            Session.SendPackets(Session.Character.GenerateStatChar());
            Session.SendPacket(Session.Character.GenerateStat());
            Session.CurrentMapInstance.Broadcast(Session.Character.GenerateTitInfo());
        }


        /// <summary>
        /// $bl packet
        /// </summary>
        /// <param name="blPacket"></param>
        public void BlBlacklistAdd(BlPacket blPacket)
        {
            if (blPacket.CharacterName != null && ServerManager.Instance.GetSessionByCharacterName(blPacket.CharacterName) is ClientSession receiverSession)
            {
                BlacklistAdd(new BlInsPacket { CharacterId = receiverSession.Character.CharacterId });
            }
        }

        /// <summary>
        /// blins packet
        /// </summary>
        /// <param name="blInsPacket"></param>
        public void BlacklistAdd(BlInsPacket blInsPacket)
        {
            if (Session.Character.CharacterId == blInsPacket.CharacterId)
            {
                return;
            }

            if (DAOFactory.CharacterDAO.LoadById(blInsPacket.CharacterId) is CharacterDTO character)
            {
                Session.SendPacket(UserInterfaceHelper.GenerateInfo(Language.Instance.GetMessageFromKey("CANT_BLACKLIST_TEAM")));
                return;
            }

            Session.Character.AddRelation(blInsPacket.CharacterId, CharacterRelationType.Blocked);
            Session.SendPacket(
                UserInterfaceHelper.GenerateInfo(Language.Instance.GetMessageFromKey("BLACKLIST_ADDED")));
            Session.SendPacket(Session.Character.GenerateBlinit());
        }

        /// <summary>
        /// bldel packet
        /// </summary>
        /// <param name="blDelPacket"></param>
        public void BlacklistDelete(BlDelPacket blDelPacket)
        {
            Session.Character.DeleteBlackList(blDelPacket.CharacterId);
            Session.SendPacket(Session.Character.GenerateBlinit());
            Session.SendPacket(
                UserInterfaceHelper.GenerateInfo(Language.Instance.GetMessageFromKey("BLACKLIST_DELETED")));
        }

        /// <summary>
        /// gop packet
        /// </summary>
        /// <param name="characterOptionPacket"></param>
        public void CharacterOptionChange(CharacterOptionPacket characterOptionPacket)
        {
            if (Session.Character == null)
            {
                return;
            }

            switch (characterOptionPacket.Option)
            {
                case CharacterOption.BuffBlocked:
                    Session.Character.BuffBlocked = characterOptionPacket.IsActive;
                    Session.SendPacket(UserInterfaceHelper.GenerateMsg(
                        Language.Instance.GetMessageFromKey(Session.Character.BuffBlocked
                            ? "BUFF_BLOCKED"
                            : "BUFF_UNLOCKED"), 0));
                    break;

                case CharacterOption.EmoticonsBlocked:
                    Session.Character.EmoticonsBlocked = characterOptionPacket.IsActive;
                    Session.SendPacket(UserInterfaceHelper.GenerateMsg(
                        Language.Instance.GetMessageFromKey(Session.Character.EmoticonsBlocked
                            ? "EMO_BLOCKED"
                            : "EMO_UNLOCKED"), 0));
                    break;

                case CharacterOption.ExchangeBlocked:
                    Session.Character.ExchangeBlocked = !characterOptionPacket.IsActive;
                    Session.SendPacket(UserInterfaceHelper.GenerateMsg(
                        Language.Instance.GetMessageFromKey(Session.Character.ExchangeBlocked
                            ? "EXCHANGE_BLOCKED"
                            : "EXCHANGE_UNLOCKED"), 0));
                    break;

                case CharacterOption.FriendRequestBlocked:
                    Session.Character.FriendRequestBlocked = !characterOptionPacket.IsActive;
                    Session.SendPacket(UserInterfaceHelper.GenerateMsg(
                        Language.Instance.GetMessageFromKey(Session.Character.FriendRequestBlocked
                            ? "FRIEND_REQ_BLOCKED"
                            : "FRIEND_REQ_UNLOCKED"), 0));
                    break;

                case CharacterOption.GroupRequestBlocked:
                    Session.Character.GroupRequestBlocked = !characterOptionPacket.IsActive;
                    Session.SendPacket(UserInterfaceHelper.GenerateMsg(
                        Language.Instance.GetMessageFromKey(Session.Character.GroupRequestBlocked
                            ? "GROUP_REQ_BLOCKED"
                            : "GROUP_REQ_UNLOCKED"), 0));
                    break;

                case CharacterOption.PetAutoRelive:
                    Session.Character.IsPetAutoRelive = characterOptionPacket.IsActive;
                    Session.SendPacket(UserInterfaceHelper.GenerateMsg(
                        Language.Instance.GetMessageFromKey(Session.Character.IsPetAutoRelive
                        ? "PET_AUTO_RELIVE_ENABLED"
                        : "PET_AUTO_RELIVE_DISABLED"), 0));
                    break;

                case CharacterOption.PartnerAutoRelive:
                    Session.Character.IsPartnerAutoRelive = characterOptionPacket.IsActive;
                    Session.SendPacket(UserInterfaceHelper.GenerateMsg(
                        Language.Instance.GetMessageFromKey(Session.Character.IsPartnerAutoRelive
                        ? "PARTNER_AUTO_RELIVE_ENABLED"
                        : "PARTNER_AUTO_RELIVE_DISABLED"), 0));
                    break;

                case CharacterOption.HeroChatBlocked:
                    Session.Character.HeroChatBlocked = characterOptionPacket.IsActive;
                    Session.SendPacket(UserInterfaceHelper.GenerateMsg(
                        Language.Instance.GetMessageFromKey(Session.Character.HeroChatBlocked
                            ? "HERO_CHAT_BLOCKED"
                            : "HERO_CHAT_UNLOCKED"), 0));
                    break;

                case CharacterOption.HpBlocked:
                    Session.Character.HpBlocked = characterOptionPacket.IsActive;
                    Session.SendPacket(UserInterfaceHelper.GenerateMsg(
                        Language.Instance.GetMessageFromKey(Session.Character.HpBlocked ? "HP_BLOCKED" : "HP_UNLOCKED"),
                        0));
                    break;

                case CharacterOption.MinilandInviteBlocked:
                    Session.Character.MinilandInviteBlocked = characterOptionPacket.IsActive;
                    Session.SendPacket(UserInterfaceHelper.GenerateMsg(
                        Language.Instance.GetMessageFromKey(Session.Character.MinilandInviteBlocked
                            ? "MINI_INV_BLOCKED"
                            : "MINI_INV_UNLOCKED"), 0));
                    break;

                case CharacterOption.MouseAimLock:
                    Session.Character.MouseAimLock = characterOptionPacket.IsActive;
                    Session.SendPacket(UserInterfaceHelper.GenerateMsg(
                        Language.Instance.GetMessageFromKey(Session.Character.MouseAimLock
                            ? "MOUSE_LOCKED"
                            : "MOUSE_UNLOCKED"), 0));
                    break;

                case CharacterOption.QuickGetUp:
                    Session.Character.QuickGetUp = characterOptionPacket.IsActive;
                    Session.SendPacket(UserInterfaceHelper.GenerateMsg(
                        Language.Instance.GetMessageFromKey(Session.Character.QuickGetUp
                            ? "QUICK_GET_UP_ENABLED"
                            : "QUICK_GET_UP_DISABLED"), 0));
                    break;

                case CharacterOption.WhisperBlocked:
                    Session.Character.WhisperBlocked = !characterOptionPacket.IsActive;
                    Session.SendPacket(UserInterfaceHelper.GenerateMsg(
                        Language.Instance.GetMessageFromKey(Session.Character.WhisperBlocked
                            ? "WHISPER_BLOCKED"
                            : "WHISPER_UNLOCKED"), 0));
                    break;

                case CharacterOption.FamilyRequestBlocked:
                    Session.Character.FamilyRequestBlocked = !characterOptionPacket.IsActive;
                    Session.SendPacket(UserInterfaceHelper.GenerateMsg(
                        Language.Instance.GetMessageFromKey(Session.Character.FamilyRequestBlocked
                            ? "FAMILY_REQ_LOCKED"
                            : "FAMILY_REQ_UNLOCKED"), 0));
                    break;

                case CharacterOption.DisableHat:
                    /* Session.Character.DisableHat = !characterOptionPacket.IsActive;
                      Session.CurrentMapInstance.Broadcast(Session.Character.GenerateEq());
                      Session.SendPacket(UserInterfaceHelper.GenerateMsg(
                          Language.Instance.GetMessageFromKey(Session.Character.DisableHat
                              ? "HAT_NOT_VISIBLE"
                              : "HAT_VISIBLE"), 0));*/
                    break;

                case CharacterOption.LockHud:
                    Session.Character.LockHud = !characterOptionPacket.IsActive;
                    Session.SendPacket(UserInterfaceHelper.GenerateMsg(
                        Language.Instance.GetMessageFromKey(Session.Character.LockHud
                            ? "LOCKED_HUD"
                            : "UNLOCKED_HUD"), 0));
                    break;

                case CharacterOption.GroupSharing:
                    Group grp = ServerManager.Instance.Groups.Find(
                        g => g != null && g.IsMemberOfGroup(Session.Character.CharacterId));
                    if (grp == null)
                    {
                        return;
                    }

                    if (!grp.IsLeader(Session))
                    {
                        Session.SendPacket(
                            UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("NOT_MASTER"), 0));
                        return;
                    }

                    if (!characterOptionPacket.IsActive)
                    {
                        grp.SharingMode = 1;

                        Session.CurrentMapInstance?.Broadcast(Session,
                            UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("SHARING"), 0),
                            ReceiverType.Group);
                    }
                    else
                    {
                        grp.SharingMode = 0;

                        Session.CurrentMapInstance?.Broadcast(Session,
                            UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("SHARING_BY_ORDER"), 0),
                            ReceiverType.Group);
                    }
                    break;
            }

            Session.SendPacket(Session.Character.GenerateStat());
        }

        /// <summary>
        /// compl packet
        /// </summary>
        /// <param name="complimentPacket"></param>
        public void Compliment(ComplimentPacket complimentPacket)
        {
            if (complimentPacket != null)
            {
                if (Session.Character.CharacterId == complimentPacket.CharacterId)
                {
                    return;
                }

                ClientSession sess = ServerManager.Instance.GetSessionByCharacterId(complimentPacket.CharacterId);
                if (sess != null)
                {
                    if (Session.Character.Level >= 30)
                    {
                        GeneralLogDTO dto =
                            Session.Character.GeneralLogs.LastOrDefault(s =>
                                s.LogData == "World" && s.LogType == "Connection");
                        GeneralLogDTO lastcompliment =
                            Session.Character.GeneralLogs.LastOrDefault(s =>
                                s.LogData == "World" && s.LogType == nameof(Compliment));
                        if (dto?.Timestamp.AddMinutes(60) <= DateTime.Now)
                        {
                            if (lastcompliment == null || lastcompliment.Timestamp.AddDays(1) <= DateTime.Now.Date)
                            {
                                sess.Character.Compliment++;
                                Session.SendPacket(Session.Character.GenerateSay(
                                    string.Format(Language.Instance.GetMessageFromKey("COMPLIMENT_GIVEN"),
                                        sess.Character.Name), 12));
                                Session.Character.GeneralLogs.Add(new GeneralLogDTO
                                {
                                    AccountId = Session.Account.AccountId,
                                    CharacterId = Session.Character.CharacterId,
                                    IpAddress = Session.IpAddress,
                                    LogData = "World",
                                    LogType = nameof(Compliment),
                                    Timestamp = DateTime.Now
                                });

                                Session.CurrentMapInstance?.Broadcast(Session,
                                    Session.Character.GenerateSay(
                                        string.Format(Language.Instance.GetMessageFromKey("COMPLIMENT_RECEIVED"),
                                            Session.Character.Name), 12), ReceiverType.OnlySomeone,
                                    characterId: complimentPacket.CharacterId);
                            }
                            else
                            {
                                Session.SendPacket(
                                    Session.Character.GenerateSay(
                                        Language.Instance.GetMessageFromKey("COMPLIMENT_COOLDOWN"), 11));
                            }
                        }
                        else if (dto != null)
                        {
                            Session.SendPacket(Session.Character.GenerateSay(
                                string.Format(Language.Instance.GetMessageFromKey("COMPLIMENT_LOGIN_COOLDOWN"),
                                    (dto.Timestamp.AddMinutes(60) - DateTime.Now).Minutes), 11));
                        }
                    }
                    else
                    {
                        Session.SendPacket(
                            Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("COMPLIMENT_NOT_MINLVL"),
                                11));
                    }
                }
            }
        }

        /// <summary>
        /// dir packet
        /// </summary>
        /// <param name="directionPacket"></param>
        public void Dir(DirectionPacket directionPacket)
        {
            if (Session.Character.IsFishing)
            {
                return;
            }

            if (directionPacket.CharacterId == Session.Character.CharacterId)
            {
                Session.Character.Direction = directionPacket.Direction;
                Session.CurrentMapInstance?.Broadcast(Session.Character.GenerateDir());
            }
        }

        /// <summary>
        /// $fl packet
        /// </summary>
        /// <param name="flPacket"></param>
        public void FlRelationAdd(FlPacket flPacket)
        {
            if (flPacket.CharacterName != null && ServerManager.Instance.GetSessionByCharacterName(flPacket.CharacterName) is ClientSession receiverSession)
            {
                RelationAdd(new FInsPacket { Type = 1, CharacterId = receiverSession.Character.CharacterId });
            }
        }

        /// <summary>
        /// fins packet
        /// </summary>
        /// <param name="fInsPacket"></param>
        public void RelationAdd(FInsPacket fInsPacket)
        {
            long characterId = fInsPacket.CharacterId;

            if (Session.Character.CharacterId == characterId)
            {
                return;
            }

            ClientSession otherSession = ServerManager.Instance.GetSessionByCharacterId(characterId);
            if (otherSession != null)
            {
                if (Session.Character.Timespace != null || otherSession.Character.Timespace != null)
                {
                    return;
                }
                if (!Session.Character.IsFriendlistFull())
                {
                    if (!Session.Character.IsFriendOfCharacter(characterId) && (fInsPacket.Type == 1 || fInsPacket.Type == 2)
                     || !Session.Character.IsMarried && (fInsPacket.Type == 34 || fInsPacket.Type == 69))
                    {
                        if (!Session.Character.IsBlockedByCharacter(characterId))
                        {
                            if (!Session.Character.IsBlockingCharacter(characterId))
                            {
                                if (otherSession.Character.MarryRequestCharacters.GetAllItems()
                                       .Contains(Session.Character.CharacterId))
                                {
                                    switch (fInsPacket.Type)
                                    {
                                        case 34:
                                            Session.Character.DeleteRelation(characterId, CharacterRelationType.Friend);
                                            Session.Character.AddRelation(characterId, CharacterRelationType.Spouse);
                                            otherSession.SendPacket(
                                                $"info {Language.Instance.GetMessageFromKey("MARRIAGE_ACCEPT")}");
                                            ServerManager.Instance.Broadcast(UserInterfaceHelper.GenerateMsg(string.Format(Language.Instance.GetMessageFromKey("MARRIAGE_ACCEPT_SHOUT"), Session.Character.Name, otherSession.Character.Name), 0));
                                            break;

                                        case 69:
                                            otherSession.SendPacket(
                                                $"info {Language.Instance.GetMessageFromKey("MARRIAGE_REJECTED")}");
                                            //ServerManager.Instance.Broadcast(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("MARRIAGE_REJECT_SHOUT"), 1));
                                            break;
                                    }
                                }
                                if (otherSession.Character.FriendRequestCharacters.GetAllItems()
                                    .Contains(Session.Character.CharacterId))
                                {
                                    switch (fInsPacket.Type)
                                    {
                                        case 1:
                                            Session.Character.AddRelation(characterId, CharacterRelationType.Friend);
                                            Session.SendPacket(
                                                $"info {Language.Instance.GetMessageFromKey("FRIEND_ADDED")}");
                                            otherSession.SendPacket(
                                                $"info {Language.Instance.GetMessageFromKey("FRIEND_ADDED")}");
                                            break;

                                        case 2:
                                            otherSession.SendPacket(
                                                $"info {Language.Instance.GetMessageFromKey("FRIEND_REJECTED")}");
                                            break;

                                        default:
                                            if (Session.Character.IsFriendlistFull())
                                            {
                                                Session.SendPacket(
                                                    $"info {Language.Instance.GetMessageFromKey("FRIEND_FULL")}");
                                                otherSession.SendPacket(
                                                    $"info {Language.Instance.GetMessageFromKey("FRIEND_FULL")}");
                                            }
                                            break;
                                    }
                                }
                                else if (fInsPacket.Type != 34 && fInsPacket.Type != 69)
                                {
                                    if (otherSession.CurrentMapInstance?.MapInstanceType == MapInstanceType.TalentArenaMapInstance)
                                    {
                                        return;
                                    }

                                    if (otherSession.Character.FriendRequestBlocked)
                                    {
                                        Session.SendPacket(
                                            $"info {Language.Instance.GetMessageFromKey("FRIEND_REJECTED")}");
                                        return;
                                    }

                                    otherSession.SendPacket(UserInterfaceHelper.GenerateDialog(
                                        $"#fins^1^{Session.Character.CharacterId} #fins^2^{Session.Character.CharacterId} {string.Format(Language.Instance.GetMessageFromKey("FRIEND_ADD"), Session.Character.Name)}"));
                                    Session.Character.FriendRequestCharacters.Add(characterId);
                                }
                            }
                            else
                            {
                                Session.SendPacket($"info {Language.Instance.GetMessageFromKey("BLACKLIST_BLOCKING")}");
                            }
                        }
                        else
                        {
                            Session.SendPacket($"info {Language.Instance.GetMessageFromKey("BLACKLIST_BLOCKED")}");
                        }
                    }
                    else
                    {
                        Session.SendPacket($"info {Language.Instance.GetMessageFromKey("ALREADY_FRIEND")}");
                    }
                }
                else
                {
                    Session.SendPacket($"info {Language.Instance.GetMessageFromKey("FRIEND_FULL")}");
                }
            }
        }

        /// <summary>
        /// fdel packet
        /// </summary>
        /// <param name="fDelPacket"></param>
        public void FriendDelete(FDelPacket fDelPacket)
        {
            if (Session.Character.CharacterRelations.Any(s => s.RelatedCharacterId == fDelPacket.CharacterId && s.RelationType == CharacterRelationType.Spouse))
            {
                Session.SendPacket($"info {Language.Instance.GetMessageFromKey("CANT_DELETE_COUPLE")}");
                return;
            }
            Session.Character.DeleteRelation(fDelPacket.CharacterId, CharacterRelationType.Friend);
            Session.SendPacket(UserInterfaceHelper.GenerateInfo(Language.Instance.GetMessageFromKey("FRIEND_DELETED")));
        }

        /// <summary>
        /// csp packet
        /// </summary>
        /// <param name="cspPacket"></param>
        public void SendBubbleMessage(CspPacket cspPacket)
        {
            if (cspPacket.CharacterId == Session.Character.CharacterId && Session.Character.BubbleMessage != null)
            {
                Session.Character.MapInstance.Broadcast(Session.Character.GenerateBubbleMessagePacket());
            }
        }

        /// <summary>
        /// btk packet
        /// </summary>
        /// <param name="btkPacket"></param>
        public void FriendTalk(BtkPacket btkPacket)
        {
            if (string.IsNullOrEmpty(btkPacket.Message))
            {
                return;
            }

            string message = btkPacket.Message;
            if (message.Length > 60)
            {
                message = message.Substring(0, 60);
            }

            message = message.Trim();

            CharacterDTO character = DAOFactory.CharacterDAO.LoadById(btkPacket.CharacterId);
            if (character != null)
            {
                int? sentChannelId = CommunicationServiceClient.Instance.SendMessageToCharacter(new SCSCharacterMessage
                {
                    DestinationCharacterId = character.CharacterId,
                    SourceCharacterId = Session.Character.CharacterId,
                    SourceWorldId = ServerManager.Instance.WorldId,
                    Message = PacketFactory.Serialize(Session.Character.GenerateTalk(message)),
                    Type = MessageType.PrivateChat
                });

                if (!sentChannelId.HasValue) //character is even offline on different world
                {
                    Session.SendPacket(
                        UserInterfaceHelper.GenerateInfo(Language.Instance.GetMessageFromKey("FRIEND_OFFLINE")));
                }
            }
        }

        /// <summary>
        /// pcl packet
        /// </summary>
        /// <param name="getGiftPacket"></param>
        public void GetGift(GetGiftPacket getGiftPacket)
        {
            int giftId = getGiftPacket.GiftId;
            if (Session.Character.MailList.ContainsKey(giftId))
            {
                MailDTO mail = Session.Character.MailList[giftId];
                if (getGiftPacket.Type == 4 && mail.AttachmentVNum != null)
                {
                    if (Session.Character.Inventory.CanAddItem((short)mail.AttachmentVNum))
                    {
                        ItemInstance newInv = Session.Character.Inventory.AddNewToInventory((short)mail.AttachmentVNum,
                                mail.AttachmentAmount, Upgrade: mail.AttachmentUpgrade,
                                Rare: (sbyte)mail.AttachmentRarity, Design: mail.AttachmentDesign)
                            .FirstOrDefault();
                        if (newInv != null)
                        {
                            if (newInv.Rare != 0)
                            {
                                newInv.SetRarityPoint();
                            }

                            if (newInv.Item.EquipmentSlot == EquipmentType.Gloves || newInv.Item.EquipmentSlot == EquipmentType.Boots)
                            {
                                newInv.DarkResistance = (short)(newInv.Item.DarkResistance * newInv.Upgrade);
                                newInv.LightResistance = (short)(newInv.Item.LightResistance * newInv.Upgrade);
                                newInv.WaterResistance = (short)(newInv.Item.WaterResistance * newInv.Upgrade);
                                newInv.FireResistance = (short)(newInv.Item.FireResistance * newInv.Upgrade);
                            }

                            Logger.LogUserEvent("PARCEL_GET", Session.GenerateIdentity(),
                                $"IIId: {newInv.Id} ItemVNum: {newInv.ItemVNum} Amount: {mail.AttachmentAmount} Sender: {mail.SenderId}");

                            Session.SendPacket(Session.Character.GenerateSay(
                                string.Format(Language.Instance.GetMessageFromKey("ITEM_GIFTED"), newInv.Item.Name,
                                    mail.AttachmentAmount), 12));

                            DAOFactory.MailDAO.DeleteById(mail.MailId);

                            Session.SendPacket($"parcel 2 1 {giftId}");

                            Session.Character.MailList.Remove(giftId);
                        }
                    }
                    else
                    {
                        Session.SendPacket("parcel 5 1 0");
                        Session.SendPacket(
                            UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("NOT_ENOUGH_PLACE"),
                                0));
                    }
                }
                else if (getGiftPacket.Type == 5)
                {
                    Session.SendPacket($"parcel 7 1 {giftId}");

                    if (DAOFactory.MailDAO.LoadById(mail.MailId) != null)
                    {
                        DAOFactory.MailDAO.DeleteById(mail.MailId);
                    }

                    if (Session.Character.MailList.ContainsKey(giftId))
                    {
                        Session.Character.MailList.Remove(giftId);
                    }
                }
            }
        }

        /// <summary>
        /// ncif packet
        /// </summary>
        /// <param name="ncifPacket"></param>
        public void GetNamedCharacterInformation(NcifPacket ncifPacket)
        {
            switch (ncifPacket.Type)
            {
                // Spieler-Charaktere
                case 1:
                    var targetSession = ServerManager.Instance.GetSessionByCharacterId(ncifPacket.TargetId);
                    if (targetSession?.Character != null)
                    {
                        Character target = targetSession.Character;

                        int hpPercentage = (int)((float)target.Hp / target.HPLoad() * 100);
                        int mpPercentage = (int)((float)target.Mp / target.MPLoad() * 100);

                        // Faction-ID korrekt setzen
                        int factionId = target.Faction switch
                        {
                            FactionType.Angel => 1,
                            FactionType.Demon => 2,
                            _ => 3 // Standardm��ig Neutral
                        };

                        // Debugging-Log f�r Fehleranalyse
                        Console.WriteLine($"Faction-ID f�r {target.Name}: {factionId}");
                        Console.WriteLine($"Reputation f�r {target.Name}: {target.Reputation.ToString("F2", CultureInfo.InvariantCulture)}");

                        // Sendet die aktualisierten Daten an den Client
                        Session.SendPacket($"ncif 1 {target.CharacterId}");
                        Session.SendPacket($"st 1 {target.CharacterId} {target.Level} {target.HeroLevel} {hpPercentage} {mpPercentage} {target.Hp} {target.Mp} {target.HPLoad()} {target.MPLoad()} {target.Reputation.ToString("F2", CultureInfo.InvariantCulture)}");
                    }
                    break;

                // NPCs & Partner/Mates
                case 2:
                    if (!Session.HasCurrentMapInstance) return;

                    var npc = Session.CurrentMapInstance.Npcs.FirstOrDefault(n => n.MapNpcId == (int)ncifPacket.TargetId);
                    if (npc != null)
                    {
                        NpcMonster npcinfo = ServerManager.GetNpcMonster(npc.NpcVNum);
                        if (npcinfo != null)
                        {
                            Session.Character.LastNpcMonsterId = npc.MapNpcId;
                            int hpPercentage = (int)((float)npc.CurrentHp / npc.MaxHp * 100);
                            int mpPercentage = (int)((float)npc.CurrentMp / npc.MaxMp * 100);

                            Session.SendPacket($"ncif 2 {ncifPacket.TargetId}");
                            Session.SendPacket($"st 2 {ncifPacket.TargetId} {npcinfo.Level} {npcinfo.HeroLevel} {hpPercentage} {mpPercentage} {npc.CurrentHp} {npc.CurrentMp} {npc.MaxHp} {npc.MaxMp} {npc.Buff.GetAllItems().Count}");
                        }
                    }
                    else
                    {
                        Mate mate = ServerManager.Instance.Sessions
                            .SelectMany(s => s.Character?.Mates ?? new List<Mate>())
                            .FirstOrDefault(m => m.MateTransportId == (int)ncifPacket.TargetId);

                        if (mate != null)
                        {
                            int hpPercentage = (int)((float)mate.Hp / mate.MaxHp * 100);
                            int mpPercentage = (int)((float)mate.Mp / mate.MaxMp * 100);

                            Session.SendPacket($"ncif 2 {ncifPacket.TargetId}");
                            Session.SendPacket($"st 2 {ncifPacket.TargetId} {mate.Level} 0 {hpPercentage} {mpPercentage} {mate.Hp} {mate.MaxHp} {mate.Mp} {mate.MaxMp} 0");
                        }
                    }
                    break;

                // Monster
                case 3:
                    if (!Session.HasCurrentMapInstance) return;

                    var monster = Session.CurrentMapInstance.Monsters.FirstOrDefault(m => m.MapMonsterId == (int)ncifPacket.TargetId);
                    if (monster != null)
                    {
                        NpcMonster monsterinfo = ServerManager.GetNpcMonster(monster.MonsterVNum);
                        if (monsterinfo != null)
                        {
                            Session.Character.LastNpcMonsterId = monster.MapMonsterId;
                            int hpPercentage = (int)((float)monster.CurrentHp / monster.MaxHp * 100);
                            int mpPercentage = (int)((float)monster.CurrentMp / monster.MaxMp * 100);

                            Session.SendPacket($"ncif 3 {ncifPacket.TargetId}");
                            Session.SendPacket($"st 3 {ncifPacket.TargetId} {monsterinfo.Level} {monsterinfo.HeroLevel} {hpPercentage} {mpPercentage} {monster.CurrentHp} {monster.CurrentMp} {monster.MaxHp} {monster.MaxMp} {monster.Buff.GetAllItems().Count}");
                        }
                    }
                    break;
            }
        }


        /// <summary>
        /// RstartPacket packet
        /// </summary>
        /// <param name="rStartPacket"></param>
        public void GetRStart(RStartPacket rStartPacket)
        {
            if (Session.Character.Timespace != null)
            {
                if (rStartPacket.Type == 1 && Session.Character.Timespace.InstanceBag != null && Session.Character.Timespace.InstanceBag.Lock == false)
                {
                    if (Session.Character.Timespace.SpNeeded?[(byte)Session.Character.Class] != 0)
                    {
                        ItemInstance specialist = Session.Character.Inventory?.LoadBySlotAndType((byte)EquipmentType.Sp, InventoryType.Wear);
                        if (specialist == null || specialist.ItemVNum != Session.Character.Timespace.SpNeeded?[(byte)Session.Character.Class])
                        {
                            Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("TS_SP_NEEDED"), 0));
                            return;
                        }
                    }
                    Session.Character.Timespace.InstanceBag.Lock = true;
                    Preq(new PreqPacket());
                    Session.Character.Timespace._mapInstanceDictionary.ToList().SelectMany(s => s.Value.Sessions).Where(s => s.Character?.Timespace != null).ToList().ForEach(s =>
                    {
                        s.Character.GeneralLogs.Add(new GeneralLogDTO
                        {
                            AccountId = s.Account.AccountId,
                            CharacterId = s.Character.CharacterId,
                            IpAddress = s.IpAddress,
                            LogData = s.Character.Timespace.Id.ToString(),
                            LogType = "InstanceEntry",
                            Timestamp = DateTime.Now
                        });
                    });
                }
            }
        }

        /// <summary>
        /// npinfo packet
        /// </summary>
        /// <param name="npinfoPacket"></param>
        public void GetStats(NpinfoPacket npinfoPacket)
        {
            Session.SendPackets(Session.Character.GenerateStatChar());

            if (npinfoPacket.Page != Session.Character.ScPage)
            {
                Session.Character.ScPage = npinfoPacket.Page;
                Session.SendPacket(UserInterfaceHelper.GeneratePClear());
                Session.SendPackets(Session.Character.GenerateScP(npinfoPacket.Page));
                Session.SendPackets(Session.Character.GenerateScN());
            }
        }

        /// <summary>
        /// $pinv packet
        /// </summary>
        /// <param name="pinvPacket"></param>
        public void PinvGroupJoin(PinvPacket pinvPacket)
        {
            if (pinvPacket.CharacterName != null && ServerManager.Instance.GetSessionByCharacterName(pinvPacket.CharacterName) is ClientSession receiverSession)
            {
                GroupJoin(new PJoinPacket { RequestType = GroupRequestType.Requested, CharacterId = receiverSession.Character.CharacterId });
            }
        }

        /// <summary>
        /// pjoin packet
        /// </summary>
        /// <param name="pjoinPacket"></param>
        public void GroupJoin(PJoinPacket pjoinPacket)
        {
            if (pjoinPacket != null)
            {
                bool createNewGroup = true;
                ClientSession targetSession = ServerManager.Instance.GetSessionByCharacterId(pjoinPacket.CharacterId);

                if ((targetSession == null && !pjoinPacket.RequestType.Equals(GroupRequestType.Sharing))
                || targetSession?.CurrentMapInstance?.MapInstanceType == MapInstanceType.TalentArenaMapInstance
                || ServerManager.Instance.ArenaMembers.ToList().Any(s => s.Session?.Character?.CharacterId == Session.Character.CharacterId)
                || (targetSession != null && ServerManager.Instance.ChannelId == 51 && targetSession.Character.Faction != Session.Character.Faction)
                || Session.Character.Timespace != null
                || targetSession?.Character.Timespace != null)
                {
                    return;
                }

                if (pjoinPacket.RequestType.Equals(GroupRequestType.Requested)
                    || pjoinPacket.RequestType.Equals(GroupRequestType.Invited))
                {
                    if (pjoinPacket.CharacterId == 0)
                    {
                        return;
                    }

                    if (ServerManager.Instance.IsCharactersGroupFull(pjoinPacket.CharacterId))
                    {
                        Session.SendPacket(
                            UserInterfaceHelper.GenerateInfo(Language.Instance.GetMessageFromKey("GROUP_FULL")));
                        return;
                    }

                    if (ServerManager.Instance.IsCharacterMemberOfGroup(pjoinPacket.CharacterId)
                        && ServerManager.Instance.IsCharacterMemberOfGroup(Session.Character.CharacterId))
                    {
                        Session.SendPacket(
                            UserInterfaceHelper.GenerateInfo(Language.Instance.GetMessageFromKey("ALREADY_IN_GROUP")));
                        return;
                    }

                    if (Session.Character.CharacterId != pjoinPacket.CharacterId && targetSession != null)
                    {
                        if (Session.Character.IsBlockedByCharacter(pjoinPacket.CharacterId))
                        {
                            Session.SendPacket(
                                UserInterfaceHelper.GenerateInfo(
                                    Language.Instance.GetMessageFromKey("BLACKLIST_BLOCKED")));
                            return;
                        }

                        if (targetSession.Character.IsBlockedByCharacter(Session.Character.CharacterId))
                        {
                            Session.SendPacket(
                                UserInterfaceHelper.GenerateInfo(
                                    Language.Instance.GetMessageFromKey("BLACKLIST_BLOCKING")));
                            return;
                        }

                        if (targetSession.Character.GroupRequestBlocked)
                        {
                            Session.SendPacket(
                                UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("GROUP_BLOCKED"),
                                    0));
                        }
                        else
                        {
                            // save sent group request to current character
                            Session.Character.GroupSentRequestCharacterIds.Add(targetSession.Character.CharacterId);
                            if (Session.Character.Group == null || Session.Character.Group.GroupType == GroupType.Group)
                            {
                                if (targetSession.Character?.Group == null
                                    || targetSession.Character?.Group.GroupType == GroupType.Group)
                                {
                                    Session.SendPacket(UserInterfaceHelper.GenerateInfo(
                                        string.Format(Language.Instance.GetMessageFromKey("GROUP_REQUEST"),
                                            targetSession.Character.Name)));
                                    targetSession.SendPacket(UserInterfaceHelper.GenerateDialog(
                                        $"#pjoin^3^{Session.Character.CharacterId} #pjoin^4^{Session.Character.CharacterId} {string.Format(Language.Instance.GetMessageFromKey("INVITED_YOU"), Session.Character.Name)}"));
                                }
                                else
                                {
                                    //can't invite raid member
                                }
                            }
                            else if (Session.Character.Group.IsLeader(Session))
                            {
                                targetSession.SendPacket(
                                    $"qna #rd^1^{Session.Character.CharacterId}^1 {string.Format(Language.Instance.GetMessageFromKey("INVITE_RAID"), Session.Character.Name)}");
                            }
                        }
                    }
                }
                else if (pjoinPacket.RequestType.Equals(GroupRequestType.Sharing))
                {
                    if (Session.Character.Group != null)
                    {
                        Session.SendPacket(
                            UserInterfaceHelper.GenerateInfo(Language.Instance.GetMessageFromKey("GROUP_SHARE_INFO")));
                        Session.Character.Group.Sessions
                            .Where(s => s.Character.CharacterId != Session.Character.CharacterId).ToList().ForEach(s =>
                            {
                                s.SendPacket(UserInterfaceHelper.GenerateDialog(
                                    $"#pjoin^6^{Session.Character.CharacterId} #pjoin^7^{Session.Character.CharacterId} {string.Format(Language.Instance.GetMessageFromKey("INVITED_YOU_SHARE"), Session.Character.Name)}"));
                                Session.Character.GroupSentRequestCharacterIds.Add(s.Character.CharacterId);
                            });
                    }
                }
                else if (pjoinPacket.RequestType.Equals(GroupRequestType.Accepted))
                {
                    if (targetSession?.Character.GroupSentRequestCharacterIds.GetAllItems()
                            .Contains(Session.Character.CharacterId) == false)
                    {
                        return;
                    }

                    try
                    {
                        targetSession?.Character.GroupSentRequestCharacterIds.Remove(Session.Character.CharacterId);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex);
                    }

                    if (ServerManager.Instance.IsCharacterMemberOfGroup(Session.Character.CharacterId)
                        && ServerManager.Instance.IsCharacterMemberOfGroup(pjoinPacket.CharacterId))
                    {
                        // everyone is in group, return
                        return;
                    }

                    if (ServerManager.Instance.IsCharactersGroupFull(pjoinPacket.CharacterId)
                        || ServerManager.Instance.IsCharactersGroupFull(Session.Character.CharacterId))
                    {
                        Session.SendPacket(
                            UserInterfaceHelper.GenerateInfo(Language.Instance.GetMessageFromKey("GROUP_FULL")));
                        targetSession?.SendPacket(
                            UserInterfaceHelper.GenerateInfo(Language.Instance.GetMessageFromKey("GROUP_FULL")));
                        return;
                    }

                    // get group and add to group
                    if (ServerManager.Instance.IsCharacterMemberOfGroup(Session.Character.CharacterId))
                    {
                        // target joins source
                        Group currentGroup =
                            ServerManager.Instance.GetGroupByCharacterId(Session.Character.CharacterId);

                        if (currentGroup != null)
                        {
                            currentGroup.JoinGroup(targetSession);
                            targetSession?.SendPacket(
                                UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("JOINED_GROUP"),
                                    10));
                            createNewGroup = false;
                        }
                    }
                    else if (ServerManager.Instance.IsCharacterMemberOfGroup(pjoinPacket.CharacterId))
                    {
                        // source joins target
                        Group currentGroup = ServerManager.Instance.GetGroupByCharacterId(pjoinPacket.CharacterId);

                        if (currentGroup != null)
                        {
                            createNewGroup = false;
                            if (currentGroup.GroupType == GroupType.Group)
                            {
                                currentGroup.JoinGroup(Session);
                            }
                            else
                            {
                                if (!currentGroup.Raid.InstanceBag.Lock)
                                {
                                    if (Session.Character.Level < currentGroup.Raid?.LevelMinimum)
                                    {
                                        Session.SendPacket(Session.Character.GenerateSay(
                                            Language.Instance.GetMessageFromKey("LOW_LVL"), 10));
                                        return;
                                    }

                                    if (currentGroup.JoinGroup(Session))
                                    {
                                        Session.SendPacket(
                                        Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("RAID_JOIN"),
                                            10));
                                        if (Session.Character.Level > currentGroup.Raid?.LevelMaximum)
                                        {
                                            Session.SendPacket(Session.Character.GenerateSay(
                                                Language.Instance.GetMessageFromKey("RAID_LEVEL_INCORRECT"), 10));
                                            if (Session.Character.Level
                                                >= currentGroup.Raid?.LevelMaximum + 10 /* && AlreadySuccededToday*/)
                                            {
                                                //modal 1 ALREADY_SUCCEDED_AS_ASSISTANT
                                            }
                                        }

                                        Session.SendPacket(Session.Character.GenerateRaid(2));
                                        Session.SendPacket(Session.Character.GenerateRaid(1));
                                        currentGroup.Sessions.ForEach(s =>
                                        {
                                            s.SendPacket(currentGroup.GenerateRdlst());
                                            s.SendPacket(s.Character.GenerateSay(
                                                string.Format(Language.Instance.GetMessageFromKey("JOIN_TEAM"),
                                                    Session.Character.Name), 10));
                                            s.SendPacket(s.Character.GenerateRaid(0));
                                        });
                                    }
                                }
                                else
                                {
                                    Session.SendPacket(
                                    Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("RAID_ALREADY_STARTED"),
                                        10));
                                }
                            }
                        }
                    }

                    if (createNewGroup)
                    {
                        Group group = new Group
                        {
                            GroupType = GroupType.Group
                        };
                        group.JoinGroup(pjoinPacket.CharacterId);
                        Session.SendPacket(UserInterfaceHelper.GenerateMsg(
                            string.Format(Language.Instance.GetMessageFromKey("GROUP_JOIN"),
                                targetSession?.Character.Name), 10));
                        group.JoinGroup(Session.Character.CharacterId);
                        ServerManager.Instance.AddGroup(group);
                        targetSession?.SendPacket(
                            UserInterfaceHelper.GenerateInfo(Language.Instance.GetMessageFromKey("GROUP_ADMIN")));

                        // set back reference to group
                        Session.Character.Group = group;
                        if (targetSession != null)
                        {
                            targetSession.Character.Group = @group;
                        }
                    }

                    if (Session.Character?.Group?.GroupType == GroupType.Group)
                    {
                        // player join group
                        ServerManager.Instance.UpdateGroup(pjoinPacket.CharacterId);
                        Session.CurrentMapInstance?.Broadcast(Session.Character.GeneratePidx());
                    }
                }
                else if (pjoinPacket.RequestType == GroupRequestType.Declined)
                {
                    if (targetSession?.Character.GroupSentRequestCharacterIds.GetAllItems().Contains(Session.Character.CharacterId) == false)
                    {
                        return;
                    }

                    targetSession?.Character.GroupSentRequestCharacterIds.Remove(Session.Character.CharacterId);

                    targetSession?.SendPacket(Session.Character.GenerateSay(
                        string.Format(Language.Instance.GetMessageFromKey("REFUSED_GROUP_REQUEST"),
                            Session.Character.Name), 10));
                }
                else if (pjoinPacket.RequestType == GroupRequestType.AcceptedShare)
                {
                    if (targetSession?.Character.GroupSentRequestCharacterIds.GetAllItems().Contains(Session.Character.CharacterId) == false)
                    {
                        return;
                    }

                    targetSession?.Character.GroupSentRequestCharacterIds.Remove(Session.Character.CharacterId);

                    Session.SendPacket(UserInterfaceHelper.GenerateMsg(
                        string.Format(Language.Instance.GetMessageFromKey("ACCEPTED_SHARE"),
                            targetSession?.Character.Name), 0));
                    if (Session.Character?.Group?.IsMemberOfGroup(pjoinPacket.CharacterId) == true && targetSession != null)
                    {
                        Session.Character.SetReturnPoint(targetSession.Character.Return.DefaultMapId,
                            targetSession.Character.Return.DefaultX, targetSession.Character.Return.DefaultY);
                        targetSession.SendPacket(UserInterfaceHelper.GenerateMsg(
                            string.Format(Language.Instance.GetMessageFromKey("CHANGED_SHARE"), targetSession.Character.Name), 0));
                    }
                }
                else if (pjoinPacket.RequestType == GroupRequestType.DeclinedShare)
                {
                    if (targetSession?.Character.GroupSentRequestCharacterIds.GetAllItems()
                            .Contains(Session.Character.CharacterId) == false)
                    {
                        return;
                    }

                    targetSession?.Character.GroupSentRequestCharacterIds.Remove(Session.Character.CharacterId);

                    Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("REFUSED_SHARE"), 0));
                }
            }
        }

        /// <summary>
        /// pleave packet
        /// </summary>
        /// <param name="pleavePacket"></param>
        public void GroupLeave(PLeavePacket pleavePacket) => ServerManager.Instance.GroupLeave(Session);

        /// <summary>
        /// ; packet
        /// </summary>
        /// <param name="groupSayPacket"></param>
        public void GroupTalk(GroupSayPacket groupSayPacket)
        {
            if (!string.IsNullOrEmpty(groupSayPacket.Message))
            {
                ServerManager.Instance.Broadcast(Session, Session.Character.GenerateSpk(groupSayPacket.Message, 3),
                    ReceiverType.Group);
            }
        }

        /// <summary>
        /// guri packet
        /// </summary>
        /// <param name="guriPacket"></param>
        public void Guri(GuriPacket guriPacket)
        {
            if (guriPacket != null)
            {
                if (guriPacket.Data.HasValue && guriPacket.Type == 10 && guriPacket.Data.Value >= 973
                    && guriPacket.Data.Value <= 999 && !Session.Character.EmoticonsBlocked)
                {
                    if (guriPacket.User == Session.Character.CharacterId)
                    {
                        Session.CurrentMapInstance?.Broadcast(Session,
                            StaticPacketHelper.GenerateEff(UserType.Player, Session.Character.CharacterId,
                                guriPacket.Data.Value + 4099), ReceiverType.AllNoEmoBlocked);
                    }
                    else if (int.TryParse(guriPacket.User.ToString(), out int mateTransportId))
                    {
                        Mate mate = Session.Character.Mates.Find(s => s.MateTransportId == mateTransportId);
                        if (mate != null)
                        {
                            Session.CurrentMapInstance?.Broadcast(Session,
                                StaticPacketHelper.GenerateEff(UserType.Npc, mate.MateTransportId,
                                    guriPacket.Data.Value + 4099), ReceiverType.AllNoEmoBlocked);
                        }
                    }
                }
                else if (guriPacket.Type == 204)
                {
                    if (guriPacket.Argument == 0 && short.TryParse(guriPacket.User.ToString(), out short slot))
                    {
                        ItemInstance shell =
                            Session.Character.Inventory.LoadBySlotAndType(slot, InventoryType.Equipment);
                        if (shell?.ShellEffects.Count == 0 && shell.Upgrade > 0 && shell.Rare > 0
                            && Session.Character.Inventory.CountItem(1429) >= ((shell.Upgrade / 10) + shell.Rare))
                        {
                            if (!ShellGeneratorHelper.Instance.ShellTypes.TryGetValue(shell.ItemVNum, out byte shellType))
                            {
                                // SHELL TYPE NOT IMPLEMENTED
                                return;
                            }

                            List<ShellEffectDTO> shellOptions = ShellGeneratorHelper.Instance.GenerateShell(shellType, shell.Rare, shell.Upgrade);

                            shell.ShellEffects.AddRange(shellOptions);

                            DAOFactory.ShellEffectDAO.InsertOrUpdateFromList(shell.ShellEffects, shell.EquipmentSerialId);

                            Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("OPTION_IDENTIFIED"), 0));
                            Session.SendPacket(StaticPacketHelper.GenerateEff(UserType.Player, Session.Character.CharacterId, 3006));
                            Session.Character.Inventory.RemoveItemAmount(1429, (shell.Upgrade / 10) + shell.Rare);
                        }
                    }
                }
                else if (guriPacket.Type == 205)
                {
                    if (guriPacket.Argument == 0 && short.TryParse(guriPacket.User.ToString(), out short slot))
                    {
                        const int perfumeVnum = 1428;

                        InventoryType perfumeInventoryType = (InventoryType)guriPacket.Argument;

                        ItemInstance equipmentInstance = Session.Character.Inventory.LoadBySlotAndType(slot, perfumeInventoryType);

                        if (equipmentInstance?.BoundCharacterId == null || equipmentInstance.BoundCharacterId == Session.Character.CharacterId || (equipmentInstance.Item.ItemType != ItemType.Weapon && equipmentInstance.Item.ItemType != ItemType.Armor))
                        {
                            return;
                        }

                        int perfumesNeeded = ShellGeneratorHelper.Instance.PerfumeFromItemLevelAndShellRarity(equipmentInstance.Item.LevelMinimum, (byte)equipmentInstance.Rare);

                        if (Session.Character.Inventory.CountItem(perfumeVnum) < perfumesNeeded)
                        {
                            return;
                        }

                        Session.Character.Inventory.RemoveItemAmount(perfumeVnum, perfumesNeeded);

                        equipmentInstance.BoundCharacterId = Session.Character.CharacterId;

                        Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("BOUND_TO_YOU"), 0));
                    }
                }
                else if (guriPacket.Type == 306)
                {
                    if (Session.Character.Inventory.CountItem(guriPacket.Argument) < 1)
                    {
                        return;
                    }

                    var item = ServerManager.GetItem((short)guriPacket.Argument);

                    if (item == null)
                    {
                        return;
                    }

                    if (item.ItemType != ItemType.Title)
                    {
                        return;
                    }

                    Session.Character.Title.Add(new CharacterTitleDTO
                    {
                        CharacterId = Session.Character.CharacterId,
                        Stat = 1,
                        TitleVnum = guriPacket.Argument
                    });

                    Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("NEW_TITLE"), 0));

                    Session.Character.Inventory.RemoveItemAmount(guriPacket.Argument);
                    Session.SendPacket(Session.Character.GenerateTitle());
                }
                else if (guriPacket.Type == 300)
                {
                    if (guriPacket.Argument == 8023 && short.TryParse(guriPacket.User.ToString(), out short slot))
                    {
                        ItemInstance box = Session.Character.Inventory.LoadBySlotAndType(slot, InventoryType.Equipment);
                        if (box != null && box.Item.ItemType == ItemType.Box)
                        {
                            box.Item.Use(Session, ref box, 1, new[] { guriPacket.Data.ToString() });
                        }
                    }
                }
                else if (guriPacket.Type == 199 && guriPacket.Argument == 2)
                {
                    if (Session.Character.IsSeal)
                    {
                        return;
                    }
                    short[] listWingOfFriendship = { 2160, 2312, 10048 };
                    short vnumToUse = -1;
                    foreach (short vnum in listWingOfFriendship)
                    {
                        if (Session.Character.Inventory.CountItem(vnum) > 0)
                        {
                            vnumToUse = vnum;
                            break;
                        }
                    }
                    bool isCouple = Session.Character.IsCoupleOfCharacter(guriPacket.User);
                    if (vnumToUse != -1 || isCouple)
                    {
                        ClientSession session = ServerManager.Instance.GetSessionByCharacterId(guriPacket.User);
                        if (session != null && !session.Character.IsChangingMapInstance)
                        {
                            if (Session.Character.IsFriendOfCharacter(guriPacket.User))
                            {
                                if (session.CurrentMapInstance?.MapInstanceType == MapInstanceType.BaseMapInstance)
                                {
                                    if (Session.Character.MapInstance.MapInstanceType
                                        != MapInstanceType.BaseMapInstance
                                        || (ServerManager.Instance.ChannelId == 51
                                         && Session.Character.Faction != session.Character.Faction))
                                    {
                                        Session.SendPacket(
                                            Session.Character.GenerateSay(
                                                Language.Instance.GetMessageFromKey("CANT_USE_THAT"), 10));
                                        return;
                                    }

                                    if (session.Character.MapInstance.Map.MapTypes.Any(s => s.MapTypeId == (short)MapTypeEnum.Act61) && Session.Character.Level < 85 || session.Character.MapInstance.Map.MapTypes.Any(s => s.MapTypeId == (short)MapTypeEnum.Act61a && Session.Character.Level < 85 || session.Character.MapInstance.Map.MapTypes.Any(s1 => s.MapTypeId == (short)MapTypeEnum.Act61d && Session.Character.Level < 85 || session.Character.MapInstance.Map.MapTypes.Any(s2 => s.MapTypeId == (short)MapTypeEnum.Act62) && Session.Character.Level < 85)))
                                    {
                                        Session.SendPacket(
                                           Session.Character.GenerateSay(
                                               Language.Instance.GetMessageFromKey("CANT_USE_THAT"), 10));
                                        return;
                                    }

                                    if (session.Character.MapInstance.Map.MapTypes.Any(s => s.MapTypeId == (short)MapTypeEnum.Act71 && Session.Character.Level < 85 && Session.Character.HeroLevel < 10))
                                    {
                                        Session.SendPacket(
                                          Session.Character.GenerateSay(
                                              Language.Instance.GetMessageFromKey("CANT_USE_THAT"), 10));
                                        return;
                                    }

                                    short mapy = session.Character.PositionY;
                                    short mapx = session.Character.PositionX;
                                    short mapId = session.Character.MapInstance.Map.MapId;

                                    ServerManager.Instance.ChangeMap(Session.Character.CharacterId, mapId, mapx, mapy);
                                    if (!isCouple)
                                    {
                                        Session.Character.Inventory.RemoveItemAmount(vnumToUse);
                                    }
                                }
                                else
                                {
                                    Session.SendPacket(UserInterfaceHelper.GenerateMsg(
                                        Language.Instance.GetMessageFromKey("USER_ON_INSTANCEMAP"), 0));
                                }
                            }
                        }
                        else
                        {
                            Session.SendPacket(
                                UserInterfaceHelper.GenerateMsg(
                                    Language.Instance.GetMessageFromKey("USER_NOT_CONNECTED"), 0));
                        }
                    }
                    else
                    {
                        Session.SendPacket(
                            Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("NO_WINGS"), 10));
                    }
                }
                else if (guriPacket.Type == 400)
                {
                    if (!Session.HasCurrentMapInstance)
                    {
                        return;
                    }

                    int mapNpcId = guriPacket.Argument;

                    MapNpc npc = Session.CurrentMapInstance.Npcs.Find(n => n.MapNpcId.Equals(mapNpcId));

                    if (npc != null && !npc.IsOut)
                    {
                        NpcMonster mapobject = ServerManager.GetNpcMonster(npc.NpcVNum);

                        int rateDrop = ServerManager.Instance.Configuration.RateDrop;
                        int delay = (int)Math.Round(
                            (3 + (mapobject.RespawnTime / 1000d)) * Session.Character.TimesUsed);
                        delay = delay > 11 ? 8 : delay;
                        if (npc.NpcVNum == 1346 || npc.NpcVNum == 1347 || npc.NpcVNum == 2350)
                        {
                            delay = 0;
                        }
                        if (Session.Character.LastMapObject.AddSeconds(delay) < DateTime.Now)
                        {
                            if (mapobject.Drops.Any(s => s.MonsterVNum != null) && mapobject.VNumRequired > 10
                                && Session.Character.Inventory.CountItem(mapobject.VNumRequired)
                                < mapobject.AmountRequired)
                            {
                                if (ServerManager.GetItem(mapobject.VNumRequired) is Item requiredItem)
                                    Session.SendPacket(
                                        UserInterfaceHelper.GenerateMsg(
                                            string.Format(Language.Instance.GetMessageFromKey("NOT_ENOUGH_ITEMS"), mapobject.AmountRequired, requiredItem.Name), 0));
                                return;
                            }

                            Random random = new Random();
                            double randomAmount = ServerManager.RandomNumber() * random.NextDouble();
                            List<DropDTO> drops = mapobject.Drops.Where(s => s.MonsterVNum == npc.NpcVNum).ToList();
                            if (drops?.Count > 0)
                            {
                                int count = 0;
                                int probabilities = drops.Sum(s => s.DropChance);
                                int rnd = ServerManager.RandomNumber(0, probabilities);
                                int currentrnd = 0;
                                DropDTO firstDrop = mapobject.Drops.Find(s => s.MonsterVNum == npc.NpcVNum);

                                if (npc.NpcVNum == 2004 /* Ice Flower */ && firstDrop != null)
                                {
                                    ItemInstance newInv = Session.Character.Inventory.AddNewToInventory(firstDrop.ItemVNum, (short)firstDrop.Amount).FirstOrDefault();

                                    if (newInv != null)
                                    {
                                        Session.Character.IncrementQuests(QuestType.Collect1, firstDrop.ItemVNum);
                                        Session.Character.IncrementQuests(QuestType.Collect5, firstDrop.ItemVNum);
                                        Session.SendPacket(UserInterfaceHelper.GenerateMsg(string.Format(Language.Instance.GetMessageFromKey("RECEIVED_ITEM"), $"{newInv.Item.Name} x {firstDrop.Amount}"), 0));
                                        Session.SendPacket(Session.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("RECEIVED_ITEM"), $"{newInv.Item.Name} x {firstDrop.Amount}"), 11));
                                    }
                                    else
                                    {
                                        Session.Character.GiftAdd(firstDrop.ItemVNum, (short)firstDrop.Amount);
                                    }
                                    Session.CurrentMapInstance.Broadcast(npc.GenerateOut());
                                    return;
                                }
                                else if (randomAmount * 1000 <= probabilities)
                                {
                                    foreach (DropDTO drop in drops.OrderBy(s => ServerManager.RandomNumber()))
                                    {
                                        short vnum = drop.ItemVNum;
                                        short amount = (short)drop.Amount;
                                        int dropChance = drop.DropChance;
                                        currentrnd += drop.DropChance;
                                        if (currentrnd >= rnd)
                                        {
                                            ItemInstance newInv = Session.Character.Inventory.AddNewToInventory(vnum, amount)
                                                .FirstOrDefault();
                                            if (newInv != null)
                                            {
                                                if (dropChance != 100000)
                                                {
                                                    Session.SendPacket(UserInterfaceHelper.GenerateMsg(
                                                        string.Format(Language.Instance.GetMessageFromKey("RECEIVED_ITEM"),
                                                            $"{newInv.Item.Name} x {amount}"), 0));
                                                }
                                                Session.SendPacket(Session.Character.GenerateSay(
                                                    string.Format(Language.Instance.GetMessageFromKey("RECEIVED_ITEM"),
                                                        $"{newInv.Item.Name} x {amount}"), 11));
                                                Session.Character.IncrementQuests(QuestType.Collect1, vnum);
                                                Session.Character.IncrementQuests(QuestType.Collect5, vnum);
                                            }
                                            else
                                            {
                                                Session.Character.GiftAdd(vnum, amount);
                                                Session.Character.IncrementQuests(QuestType.Collect1, vnum);
                                                Session.Character.IncrementQuests(QuestType.Collect5, vnum);
                                            }
                                            count++;
                                            if (dropChance != 100000)
                                            {
                                                break;
                                            }
                                        }
                                    }
                                }

                                if (count > 0)
                                {
                                    Session.Character.LastMapObject = DateTime.Now;
                                    Session.Character.TimesUsed++;
                                    if (Session.Character.TimesUsed >= 4)
                                    {
                                        Session.Character.TimesUsed = 0;
                                    }

                                    if (mapobject.VNumRequired > 10)
                                    {
                                        Session.Character.Inventory.RemoveItemAmount(npc.Npc.VNumRequired, npc.Npc.AmountRequired);
                                    }

                                    if (npc.NpcVNum == 1346 || npc.NpcVNum == 1347 || npc.NpcVNum == 2350)
                                    {
                                        npc.SetDeathStatement();
                                        npc.RunDeathEvent();
                                        Session.Character.MapInstance.Broadcast(npc.GenerateOut());
                                    }
                                }
                                else
                                {
                                    Session.SendPacket(
                                        UserInterfaceHelper.GenerateMsg(
                                            Language.Instance.GetMessageFromKey("TRY_FAILED"), 0));
                                }
                            }
                            else if (Session.CurrentMapInstance.Npcs.Where(s => s.Npc.Race == 8 && s.Npc.RaceType == 5 && s.MapNpcId != npc.MapNpcId) is IEnumerable<MapNpc> mapTeleportNpcs)
                            {
                                // Hard fix in waiting to fix (npc allow tp whitout requiered item)
                                if (npc.Npc.VNumRequired > 0 && npc.Npc.AmountRequired > 0 && npc.NpcVNum != 863)
                                //  if (npc.Npc.VNumRequired > 0 && npc.Npc.AmountRequired > 0)
                                {
                                    if (Session.Character.Inventory.CountItem(npc.Npc.VNumRequired) >= npc.Npc.AmountRequired)
                                    {
                                        if (npc.Npc.AmountRequired > 1)
                                        {
                                            Session.Character.Inventory.RemoveItemAmount(npc.Npc.VNumRequired, npc.Npc.AmountRequired);
                                        }
                                    }
                                    else
                                    {
                                        Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("NO_ITEM_REQUIRED"), 0));
                                        return;
                                    }
                                }
                                if (DAOFactory.TeleporterDAO.LoadFromNpc(npc.MapNpcId).FirstOrDefault() is TeleporterDTO teleport)
                                {
                                    Session.Character.PositionX = teleport.MapX;
                                    Session.Character.PositionY = teleport.MapY;
                                    Session.CurrentMapInstance.Broadcast(Session.Character.GenerateTp());
                                    foreach (Mate mate in Session.Character.Mates.Where(s => s.IsTeamMember && s.IsAlive))
                                    {
                                        mate.PositionX = teleport.MapX;
                                        mate.PositionY = teleport.MapY;
                                        Session.CurrentMapInstance.Broadcast(mate.GenerateTp());
                                    }
                                }
                                else
                                {
                                    MapNpc nearestTeleportNpc = null;
                                    foreach (MapNpc teleportNpc in mapTeleportNpcs)
                                    {
                                        if (nearestTeleportNpc == null)
                                        {
                                            nearestTeleportNpc = teleportNpc;
                                        }
                                        else if (
                                            Map.GetDistance(
                                            new MapCell { X = npc.MapX, Y = npc.MapY },
                                            new MapCell { X = teleportNpc.MapX, Y = teleportNpc.MapY })
                                            <
                                            Map.GetDistance(
                                            new MapCell { X = npc.MapX, Y = npc.MapY },
                                            new MapCell { X = nearestTeleportNpc.MapX, Y = nearestTeleportNpc.MapY }))
                                        {
                                            nearestTeleportNpc = teleportNpc;
                                        }
                                    }
                                    if (nearestTeleportNpc != null)
                                    {
                                        Session.Character.PositionX = nearestTeleportNpc.MapX;
                                        Session.Character.PositionY = nearestTeleportNpc.MapY;
                                        Session.CurrentMapInstance.Broadcast(Session.Character.GenerateTp());
                                        foreach (Mate mate in Session.Character.Mates.Where(s => s.IsTeamMember && s.IsAlive))
                                        {
                                            mate.PositionX = nearestTeleportNpc.MapX;
                                            mate.PositionY = nearestTeleportNpc.MapY;
                                            Session.CurrentMapInstance.Broadcast(mate.GenerateTp());
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            Session.SendPacket(UserInterfaceHelper.GenerateMsg(
                                string.Format(Language.Instance.GetMessageFromKey("TRY_FAILED_WAIT"),
                                    (int)(Session.Character.LastMapObject.AddSeconds(delay) - DateTime.Now)
                                    .TotalSeconds), 0));
                        }
                    }
                }
                else if (guriPacket.Type == 1502)
                {
                    short relictVNum = 0;
                    if (guriPacket.Argument == 10000)
                    {
                        relictVNum = 1878;
                    }
                    else if (guriPacket.Argument == 30000)
                    {
                        relictVNum = 1879;
                    }
                    if (relictVNum > 0 && Session.Character.Inventory.CountItem(relictVNum) > 0)
                    {
                        IEnumerable<RollGeneratedItemDTO> roll = DAOFactory.RollGeneratedItemDAO.LoadByItemVNum(relictVNum);
                        IEnumerable<RollGeneratedItemDTO> rollGeneratedItemDtos = roll as IList<RollGeneratedItemDTO> ?? roll.ToList();
                        if (!rollGeneratedItemDtos.Any())
                        {
                            Logger.Warn(string.Format(Language.Instance.GetMessageFromKey("NO_HANDLER_RELICT"), GetType(), relictVNum));
                            return;
                        }
                        int probabilities = rollGeneratedItemDtos.Sum(s => s.Probability);
                        int rnd = 0;
                        int rnd2 = ServerManager.RandomNumber(0, probabilities);
                        int currentrnd = 0;
                        foreach (RollGeneratedItemDTO rollitem in rollGeneratedItemDtos.OrderBy(s => ServerManager.RandomNumber()))
                        {
                            sbyte rare = 0;
                            if (rollitem.IsRareRandom)
                            {
                                rnd = ServerManager.RandomNumber(0, 100);

                                for (int j = 7; j >= 0; j--)
                                {
                                    if (rnd < ItemHelper.RareRate[j])
                                    {
                                        rare = (sbyte)j;
                                        break;
                                    }
                                }
                                if (rare < 1)
                                {
                                    rare = 1;
                                }
                            }

                            if (rollitem.Probability == 10000)
                            {
                                Session.Character.GiftAdd(rollitem.ItemGeneratedVNum, rollitem.ItemGeneratedAmount, (byte)rare, design: rollitem.ItemGeneratedDesign);
                                continue;
                            }
                            currentrnd += rollitem.Probability;
                            if (currentrnd < rnd2)
                            {
                                continue;
                            }
                            Session.Character.GiftAdd(rollitem.ItemGeneratedVNum, rollitem.ItemGeneratedAmount, (byte)rare, design: rollitem.ItemGeneratedDesign);//, rollitem.ItemGeneratedUpgrade);
                            break;
                        }
                        Session.Character.Inventory.RemoveItemAmount(relictVNum);
                        Session.Character.Gold -= guriPacket.Argument;
                        Session.Character.GenerateGold();
                        Session.SendPacket("shop_end 1");
                    }
                }
                else if (guriPacket.Type == 501)
                {
                    if (ServerManager.Instance.IceBreakerInWaiting && IceBreaker.Map.Sessions.Count() < IceBreaker.MaxAllowedPlayers
                     && Session.Character.MapInstance.MapInstanceType == MapInstanceType.BaseMapInstance && Session.Character.Group?.Raid == null)
                    {
                        if (Session.Character.Gold >= 500)
                        {
                            Session.Character.Gold -= 500;
                            Session.SendPacket(Session.Character.GenerateGold());
                            Session.Character.RemoveVehicle();
                            ServerManager.Instance.TeleportOnRandomPlaceInMap(Session, IceBreaker.Map.MapInstanceId);
                            ConcurrentBag<ClientSession> NewIceTeam = new ConcurrentBag<ClientSession>();
                            NewIceTeam.Add(Session);
                            IceBreaker.IceBreakerTeams.Add(NewIceTeam);
                        }
                        else
                        {
                            Session.SendPacket(Language.Instance.GetMessageFromKey("NOT_ENOUGH_MONEY"));
                        }
                    }
                }
                else if (guriPacket.Type == 502)
                {
                    long? targetId = guriPacket.User;

                    if (targetId == null)
                    {
                        return;
                    }

                    ClientSession target = ServerManager.Instance.GetSessionByCharacterId(targetId.Value);

                    if (target?.Character?.MapInstance == null)
                    {
                        return;
                    }

                    if (target.Character.MapInstance.MapInstanceType == MapInstanceType.IceBreakerInstance)
                    {
                        if (target.Character.LastPvPKiller == null
                            || target.Character.LastPvPKiller != Session)
                        {
                            IceBreaker.FrozenPlayers.Remove(target);
                            IceBreaker.AlreadyFrozenPlayers.Add(target);
                            target.Character.NoMove = false;
                            target.Character.NoAttack = false;
                            target.SendPacket(target.Character.GenerateCond());
                            target.Character.MapInstance.Broadcast(UserInterfaceHelper.GenerateMsg(string.Format(Language.Instance.GetMessageFromKey("ICEBREAKER_PLAYER_UNFROZEN"), target.Character.Name), 0));

                            if (!IceBreaker.IceBreakerTeams.FirstOrDefault(s => s.Contains(Session)).Contains(target))
                            {
                                IceBreaker.IceBreakerTeams.Remove(IceBreaker.IceBreakerTeams.FirstOrDefault(s => s.Contains(target)));
                                IceBreaker.IceBreakerTeams.FirstOrDefault(s => s.Contains(Session)).Add(target);
                            }
                        }
                    }
                    else
                    {
                        target.Character.RemoveBuff(569);
                    }
                }
                else if (guriPacket.Type == 506)
                {
                    Session.Character.IsWaitingForEvent |= ServerManager.Instance.EventInWaiting;
                }
                else if (guriPacket.Type == 513)
                {
                    if (Session?.Character?.MapInstance == null)
                    {
                        return;
                    }

                    if (Session.Character.IsLaurenaMorph())
                    {
                        Session.Character.MapInstance.Broadcast(Session.Character.GenerateEff(4054));
                        Session.Character.ClearLaurena();
                    }
                }
                else if (guriPacket.Type == 710)
                {
                    if (guriPacket.Value != null)
                    {
                        // TODO: MAP TELEPORTER
                    }
                }
                else if (guriPacket.Type == 711)
                {
                    TeleporterDTO tp = Session.CurrentMapInstance.Npcs.FirstOrDefault(n => n.Teleporters.Any(t => t?.TeleporterId == guriPacket.Argument))?.Teleporters.FirstOrDefault(t => t?.Type == TeleporterType.TeleportOnOtherMap);
                    if (tp == null)
                    {
                        return;
                    }
                    ServerManager.Instance.ChangeMap(Session.Character.CharacterId, tp.MapId, tp.MapX, tp.MapY);
                }
                else if (guriPacket.Type == 750)
                {
                    const short baseVnum = 1623;
                    if (ServerManager.Instance.ChannelId == 51)
                    {
                        Session.SendPacket(
                            UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("CHANGE_NOT_PERMITTED_ACT4"),
                                0));
                        return;
                    }
                    if (Session.CurrentMapInstance.MapInstanceType == MapInstanceType.Act4ShipAngel
                        || Session.CurrentMapInstance.MapInstanceType == MapInstanceType.Act4ShipDemon)
                    {
                        Session.SendPacket(
                            UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("CHANGE_NOT_PERMITTED_ACT4SHIP"),
                                0));
                        return;
                    }
                    if (Enum.TryParse(guriPacket.Argument.ToString(), out FactionType faction)
                    && Session.Character.Inventory.CountItem(baseVnum + (byte)faction) > 0)
                    {
                        if ((byte)faction < 3) // Single family change
                        {
                            if (Session.Character.Faction == (FactionType)faction)
                            {
                                return;
                            }
                            if (Session.Character.Family != null)
                            {
                                Session.SendPacket(
                                    UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("IN_FAMILY"),
                                        0));
                                return;
                            }
                            Session.Character.Inventory.RemoveItemAmount(baseVnum + (byte)faction);
                            Session.Character.ChangeFaction((FactionType)faction);
                        }
                        else // Family faction change
                        {
                            faction -= 2;
                            if ((FactionType)Session.Character.Family.FamilyFaction == faction)
                            {
                                return;
                            }
                            if (Session.Character.Family == null)
                            {
                                Session.SendPacket(
                                    UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("NO_FAMILY"),
                                        0));
                                return;
                            }
                            if (Session.Character.FamilyCharacter.Authority != FamilyAuthority.Head)
                            {
                                Session.SendPacket(
                                    UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("NO_FAMILY_HEAD"),
                                        0));
                                return;
                            }
                            if (Session.Character.Family.LastFactionChange > DateTime.Now.AddDays(-1).Ticks)
                            {
                                Session.SendPacket(UserInterfaceHelper.GenerateMsg(
                                    Language.Instance.GetMessageFromKey("CHANGE_NOT_PERMITTED"), 0));
                                return;
                            }

                            Session.Character.Inventory.RemoveItemAmount(baseVnum + (byte)faction + 2);
                            Session.Character.Family.ChangeFaction((byte)faction, Session);
                        }
                    }
                }
                else if (guriPacket.Type == 2)
                {
                    Session.CurrentMapInstance?.Broadcast(
                        UserInterfaceHelper.GenerateGuri(2, 1, Session.Character.CharacterId),
                        Session.Character.PositionX, Session.Character.PositionY);
                }
                else if (guriPacket.Type == 4)
                {
                    const int speakerVNum = 2173;
                    const int limitedSpeakerVNum = 10028;
                    if (guriPacket.Argument == 1)
                    {
                        if (guriPacket.Value.Contains("^"))
                        {
                            Session.SendPacket(UserInterfaceHelper.GenerateInfo(Language.Instance.GetMessageFromKey("INVALID_NAME")));
                            return;
                        }
                        short[] listPetnameVNum = { 2157, 10023 };
                        short vnumToUse = -1;
                        foreach (short vnum in listPetnameVNum)
                        {
                            if (Session.Character.Inventory.CountItem(vnum) > 0)
                            {
                                vnumToUse = vnum;
                            }
                        }
                        Mate mate = Session.Character.Mates.Find(s => s.MateTransportId == guriPacket.Data);
                        if (mate != null && Session.Character.Inventory.CountItem(vnumToUse) > 0)
                        {
                            mate.Name = guriPacket.Value.Truncate(16);
                            Session.CurrentMapInstance?.Broadcast(mate.GenerateOut(), ReceiverType.AllExceptMe);
                            Parallel.ForEach(Session.CurrentMapInstance.Sessions.Where(s => s.Character != null), s =>
                            {
                                if (ServerManager.Instance.ChannelId != 51 || Session.Character.Faction == s.Character.Faction)
                                {
                                    s.SendPacket(mate.GenerateIn(false, ServerManager.Instance.ChannelId == 51));
                                }
                                else
                                {
                                    s.SendPacket(mate.GenerateIn(true, ServerManager.Instance.ChannelId == 51, s.Account.Authority));
                                }
                            });
                            Session.SendPacket(
                                UserInterfaceHelper.GenerateInfo(Language.Instance.GetMessageFromKey("NEW_NAME_PET")));
                            Session.SendPacket(Session.Character.GeneratePinit());
                            Session.SendPackets(Session.Character.GeneratePst());
                            Session.SendPackets(Session.Character.GenerateScP());
                            Session.Character.Inventory.RemoveItemAmount(vnumToUse);
                        }
                    }

                    // presentation message
                    if (guriPacket.Argument == 2)
                    {
                        int presentationVNum = Session.Character.Inventory.CountItem(1117) > 0
                            ? 1117
                            : (Session.Character.Inventory.CountItem(9013) > 0 ? 9013 : -1);
                        if (presentationVNum != -1)
                        {
                            string message = "";
                            string[] valuesplit = guriPacket.Value?.Split(' ');
                            if (valuesplit == null)
                            {
                                return;
                            }

                            for (int i = 0; i < valuesplit.Length; i++)
                            {
                                message += valuesplit[i] + "^";
                            }

                            message = message.Substring(0, message.Length - 1); // Remove the last ^
                            message = message.Trim();
                            if (message.Length > 60)
                            {
                                message = message.Substring(0, 60);
                            }

                            Session.Character.Biography = message;
                            Session.SendPacket(
                                Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("INTRODUCTION_SET"),
                                    10));
                            Session.Character.Inventory.RemoveItemAmount(presentationVNum);
                        }
                    }

                    // Speaker
                    if (guriPacket.Argument == 3 && (Session.Character.Inventory.CountItem(speakerVNum) > 0 || Session.Character.Inventory.CountItem(limitedSpeakerVNum) > 0))
                    {
                        string sayPacket = "";
                        string message =
                            $"<{Language.Instance.GetMessageFromKey("SPEAKER")}> [{Session.Character.Name}]:";
                        byte sayItemInventory = 0;
                        short sayItemSlot = 0;
                        int baseLength = message.Length;
                        string[] valuesplit = guriPacket.Value?.Split(' ');
                        if (valuesplit == null)
                        {
                            return;
                        }

                        if (guriPacket.Data == 999 && (valuesplit.Length < 3 || !byte.TryParse(valuesplit[0], out sayItemInventory) || !short.TryParse(valuesplit[1], out sayItemSlot)))
                        {
                            return;
                        }

                        for (int i = 0 + (guriPacket.Data == 999 ? 2 : 0); i < valuesplit.Length; i++)
                        {
                            message += valuesplit[i] + " ";
                        }

                        if (message.Length > 120 + baseLength)
                        {
                            message = message.Substring(0, 120 + baseLength);
                        }

                        message = message.Trim();

                        if (guriPacket.Data == 999)
                        {
                            sayPacket = Session.Character.GenerateSayItem(message, 13, sayItemInventory, sayItemSlot);
                        }
                        else
                        {
                            sayPacket = Session.Character.GenerateSay(message, 13);
                        }

                        if (Session.Character.IsMuted())
                        {
                            Session.SendPacket(
                                Session.Character.GenerateSay(
                                    Language.Instance.GetMessageFromKey("SPEAKER_CANT_BE_USED"), 10));
                            return;
                        }

                        if (Session.Character.Inventory.CountItem(limitedSpeakerVNum) > 0)
                        {
                            Session.Character.Inventory.RemoveItemAmount(limitedSpeakerVNum);
                        }
                        else
                        {
                            Session.Character.Inventory.RemoveItemAmount(speakerVNum);
                        }

                        if (ServerManager.Instance.ChannelId == 51)
                        {
                            ServerManager.Instance.Broadcast(Session, sayPacket, ReceiverType.AllExceptMeAct4);
                            Session.SendPacket(sayPacket);
                        }
                        else
                        {
                            ServerManager.Instance.Broadcast(Session, sayPacket, ReceiverType.All);
                        }
                    }

                    // Bubble

                    if (guriPacket.Argument == 4)
                    {
                        int bubbleVNum = Session.Character.Inventory.CountItem(2174) > 0
                            ? 2174
                            : (Session.Character.Inventory.CountItem(10029) > 0 ? 10029 : -1);
                        if (bubbleVNum != -1)
                        {
                            string message = "";
                            string[] valuesplit = guriPacket.Value?.Split(' ');
                            if (valuesplit == null)
                            {
                                return;
                            }

                            for (int i = 0; i < valuesplit.Length; i++)
                            {
                                message += valuesplit[i] + "^";
                            }

                            message = message.Substring(0, message.Length - 1); // Remove the last ^
                            message = message.Trim();
                            if (message.Length > 60)
                            {
                                message = message.Substring(0, 60);
                            }

                            Session.Character.BubbleMessage = message;
                            Session.Character.BubbleMessageEnd = DateTime.Now.AddMinutes(30);
                            Session.SendPacket($"csp_r {Session.Character.BubbleMessage}");
                            Session.Character.Inventory.RemoveItemAmount(bubbleVNum);
                        }
                    }
                }
                else if (guriPacket.Type == 199 && guriPacket.Argument == 1)
                {
                    if (Session.Character.IsSeal)
                    {
                        return;
                    }
                    short[] listWingOfFriendship = { 2160, 2312, 10048 };
                    short vnumToUse = -1;
                    foreach (short vnum in listWingOfFriendship)
                    {
                        if (Session.Character.Inventory.CountItem(vnum) > 0)
                        {
                            vnumToUse = vnum;
                        }
                    }
                    bool isCouple = Session.Character.IsCoupleOfCharacter(guriPacket.User);
                    if (vnumToUse != -1 || isCouple)
                    {
                        ClientSession session = ServerManager.Instance.GetSessionByCharacterId(guriPacket.User);
                        if (session != null)
                        {
                            if (Session.Character.IsFriendOfCharacter(guriPacket.User))
                            {
                                if (session.CurrentMapInstance?.MapInstanceType == MapInstanceType.BaseMapInstance)
                                {
                                    if (Session.Character.MapInstance.MapInstanceType
                                        != MapInstanceType.BaseMapInstance
                                        || (ServerManager.Instance.ChannelId == 51
                                         && Session.Character.Faction != session.Character.Faction))
                                    {
                                        Session.SendPacket(
                                            Session.Character.GenerateSay(
                                                Language.Instance.GetMessageFromKey("CANT_USE_THAT"), 10));
                                        return;
                                    }
                                }
                                else
                                {
                                    Session.SendPacket(UserInterfaceHelper.GenerateMsg(
                                        Language.Instance.GetMessageFromKey("USER_ON_INSTANCEMAP"), 0));
                                    return;
                                }
                            }
                            else
                            {
                                return;
                            }
                        }
                        else
                        {
                            Session.SendPacket(
                                UserInterfaceHelper.GenerateMsg(
                                    Language.Instance.GetMessageFromKey("USER_NOT_CONNECTED"), 0));
                            return;
                        }
                    }
                    else
                    {
                        Session.SendPacket(
                            Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("NO_WINGS"), 10));
                        return;
                    }
                    if (!Session.Character.IsFriendOfCharacter(guriPacket.User))
                    {
                        Session.SendPacket(Language.Instance.GetMessageFromKey("CHARACTER_NOT_IN_FRIENDLIST"));
                        return;
                    }

                    Session.SendPacket(UserInterfaceHelper.GenerateDelay(3000, 7, $"#guri^199^2^{guriPacket.User}"));
                }
                else if (guriPacket.Type == 201)
                {
                    if (Session.Character.StaticBonusList.Any(s => s.StaticBonusType == StaticBonusType.PetBasket))
                    {
                        Session.SendPacket(Session.Character.GenerateStashAll());
                    }
                }
                else if (guriPacket.Type == 202)
                {
                    //Session.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("PARTNER_BACKPACK"), 10));
                    Session.SendPacket(Session.Character.GeneratePStashAll());
                }
                else if (guriPacket.Type == 208 && guriPacket.Argument == 0)
                {
                    if (short.TryParse(guriPacket.Value, out short mountSlot)
                        && short.TryParse(guriPacket.User.ToString(), out short pearlSlot))
                    {
                        ItemInstance mount = Session.Character.Inventory.LoadBySlotAndType(mountSlot, InventoryType.Main);
                        ItemInstance pearl = Session.Character.Inventory.LoadBySlotAndType(pearlSlot, InventoryType.Equipment);

                        if (mount?.Item == null || pearl?.Item == null)
                        {
                            return;
                        }

                        if (!pearl.Item.IsHolder)
                        {
                            return;
                        }

                        if (pearl.HoldingVNum > 0)
                        {
                            return;
                        }

                        if (pearl.Item.ItemType == ItemType.Box && pearl.Item.ItemSubType == 4)
                        {
                            if (mount.Item.ItemType != ItemType.Special || mount.Item.ItemSubType != 0 || mount.Item.Speed < 1)
                            {
                                return;
                            }

                            Session.Character.Inventory.RemoveItemFromInventory(mount.Id);

                            pearl.HoldingVNum = mount.ItemVNum;

                            Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("MOUNT_SAVED"), 0));
                            Session.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("MOUNT_SAVED"), 10));
                        }
                    }
                }
                else if (guriPacket.Type == 209 && guriPacket.Argument == 0)
                {
                    if (short.TryParse(guriPacket.Value, out short fairySlot)
                        && short.TryParse(guriPacket.User.ToString(), out short pearlSlot))
                    {
                        ItemInstance fairy = Session.Character.Inventory.LoadBySlotAndType(fairySlot, InventoryType.Equipment);
                        ItemInstance pearl = Session.Character.Inventory.LoadBySlotAndType(pearlSlot, InventoryType.Equipment);

                        if (fairy?.Item == null || pearl?.Item == null)
                        {
                            return;
                        }

                        if (!pearl.Item.IsHolder)
                        {
                            return;
                        }

                        if (pearl.HoldingVNum > 0)
                        {
                            return;
                        }

                        if (pearl.Item.ItemType == ItemType.Box && pearl.Item.ItemSubType == 5)
                        {
                            if (fairy.Item.ItemType != ItemType.Jewelery || fairy.Item.ItemSubType != 3 || fairy.Item.IsDroppable)
                            {
                                return;
                            }

                            Session.Character.Inventory.RemoveItemFromInventory(fairy.Id);

                            pearl.HoldingVNum = fairy.ItemVNum;
                            pearl.ElementRate = fairy.ElementRate;

                            Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("FAIRY_SAVED"), 0));
                            Session.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("FAIRY_SAVED"), 10));
                        }
                    }
                }
                else if (guriPacket.Type == 203 && guriPacket.Argument == 0)
                {
                    // SP points initialization
                    int[] listPotionResetVNums = { 1366, 1427, 5115, 9040 };
                    int vnumToUse = -1;
                    foreach (int vnum in listPotionResetVNums)
                    {
                        if (Session.Character.Inventory.CountItem(vnum) > 0)
                        {
                            vnumToUse = vnum;
                        }
                    }

                    if (vnumToUse != -1)
                    {
                        if (Session.Character.UseSp)
                        {
                            ItemInstance specialistInstance =
                                Session.Character.Inventory.LoadBySlotAndType((byte)EquipmentType.Sp,
                                    InventoryType.Wear);
                            if (specialistInstance != null)
                            {
                                specialistInstance.SlDamage = 0;
                                specialistInstance.SlDefence = 0;
                                specialistInstance.SlElement = 0;
                                specialistInstance.SlHP = 0;

                                specialistInstance.DamageMinimum = 0;
                                specialistInstance.DamageMaximum = 0;
                                specialistInstance.HitRate = 0;
                                specialistInstance.CriticalLuckRate = 0;
                                specialistInstance.CriticalRate = 0;
                                specialistInstance.DefenceDodge = 0;
                                specialistInstance.DistanceDefenceDodge = 0;
                                specialistInstance.ElementRate = 0;
                                specialistInstance.DarkResistance = 0;
                                specialistInstance.LightResistance = 0;
                                specialistInstance.FireResistance = 0;
                                specialistInstance.WaterResistance = 0;
                                specialistInstance.CriticalDodge = 0;
                                specialistInstance.CloseDefence = 0;
                                specialistInstance.DistanceDefence = 0;
                                specialistInstance.MagicDefence = 0;
                                specialistInstance.HP = 0;
                                specialistInstance.MP = 0;

                                Session.Character.Inventory.RemoveItemAmount(vnumToUse);
                                Session.Character.Inventory.DeleteFromSlotAndType((byte)EquipmentType.Sp,
                                    InventoryType.Wear);
                                Session.Character.Inventory.AddToInventoryWithSlotAndType(specialistInstance,
                                    InventoryType.Wear, (byte)EquipmentType.Sp);
                                Session.SendPacket(Session.Character.GenerateCond());
                                Session.SendPacket(specialistInstance.GenerateSlInfo(Session));
                                Session.SendPacket(Session.Character.GenerateLev());
                                Session.SendPackets(Session.Character.GenerateStatChar());
                                Session.SendPacket(
                                    UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("POINTS_RESET"),
                                        0));
                            }
                        }
                        else
                        {
                            Session.SendPacket(
                                Session.Character.GenerateSay(
                                    Language.Instance.GetMessageFromKey("TRANSFORMATION_NEEDED"), 10));
                        }
                    }
                    else
                    {
                        Session.SendPacket(
                            Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("NOT_ENOUGH_POINTS"),
                                10));
                    }
                }
            }
        }

        /// <summary>
        /// hero packet
        /// </summary>
        /// <param name="heroPacket"></param>
        public void Hero(HeroPacket heroPacket)
        {
            if (!string.IsNullOrEmpty(heroPacket.Message))
            {
                if (Session.Character.IsReputationHero() >= 3 && Session.Character.Reputation > 5000000)
                {
                    heroPacket.Message = heroPacket.Message.Trim();
                    ServerManager.Instance.Broadcast(Session, $"msg 5 [{Session.Character.Name}]:{heroPacket.Message}",
                        ReceiverType.AllNoHeroBlocked);
                }
                else
                {
                    Session.SendPacket(
                        Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("USER_NOT_HERO"), 11));
                }
            }
        }

        /// <summary>
        /// PreqPacket packet
        /// </summary>
        /// <param name="packet"></param>
        public void Preq(PreqPacket packet)
        {
            if (Session.Character.IsSeal)
            {
                return;
            }

            double currentRunningSeconds = (DateTime.Now - Process.GetCurrentProcess().StartTime.AddSeconds(-50)).TotalSeconds;

            double timeSpanSinceLastPortal = currentRunningSeconds - Session.Character.LastPortal;

            if (Session.Account?.Authority != AuthorityType.Administrator
                && (timeSpanSinceLastPortal < 4 || !Session.HasCurrentMapInstance))
            {
                Session.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("CANT_MOVE"), 10));
                return;
            }

            if (Session.CurrentMapInstance.Portals.Concat(Session.Character.GetExtraPortal())
                    .FirstOrDefault(s => Session.Character.PositionY >= s.SourceY - 1
                        && Session.Character.PositionY <= s.SourceY + 1
                        && Session.Character.PositionX >= s.SourceX - 1
                        && Session.Character.PositionX <= s.SourceX + 1) is Portal portal)
            {
                switch (portal.Type)
                {
                    case (sbyte)PortalType.Miniland:
                    case (sbyte)PortalType.Exit:
                    case (sbyte)PortalType.MapPortal:
                    case (sbyte)PortalType.TSNormal:
                    case (sbyte)PortalType.Open:
                    case (sbyte)PortalType.TSEnd:
                    case (sbyte)PortalType.Effect:
                    case (sbyte)PortalType.ShopTeleport:
                        break;

                    case (sbyte)PortalType.TimeSpace:
                        break;

                    case (sbyte)PortalType.Raid:
                        if (Session.Character.Group?.Raid != null)
                        {
                            if (Session.Character.Group.IsLeader(Session))
                            {
                                Session.SendPacket(
                                    $"qna #mkraid^0^275 {Language.Instance.GetMessageFromKey("RAID_START_QUESTION")}");
                            }
                            else
                            {
                                Session.SendPacket(
                                    Session.Character.GenerateSay(
                                        Language.Instance.GetMessageFromKey("NOT_TEAM_LEADER"), 10));
                            }
                        }
                        else
                        {
                            Session.SendPacket(
                                Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("NEED_TEAM"), 10));
                        }

                        return;

                    case (sbyte)PortalType.BlueRaid:
                    case (sbyte)PortalType.DarkRaid:
                        if (!packet.Parameter.HasValue)
                        {
                            Session.SendPacket($"qna #preq^1 {string.Format(Language.Instance.GetMessageFromKey("ACT4_RAID_ENTER"), Session.Character.Level * 5)}");
                            return;
                        }
                        else
                        {
                            if (packet.Parameter == 1)
                            {
                                if ((int)Session.Character.Faction == portal.Type - 9 && Session.Character.Family?.Act4Raid != null)
                                {
                                    if (Session.Character.Level > 49)
                                    {
                                        if (Session.Character.Reputation > 10000)
                                        {
                                            Session.Character.GetReputation(Session.Character.Level * -5);

                                            Session.Character.LastPortal = currentRunningSeconds;

                                            switch (Session.Character.Family.Act4Raid.MapInstanceType)
                                            {
                                                case MapInstanceType.Act4Morcos:
                                                    ServerManager.Instance.ChangeMapInstance(Session.Character.CharacterId,
                                                        Session.Character.Family.Act4Raid.MapInstanceId, 43, 179);
                                                    break;

                                                case MapInstanceType.Act4Hatus:
                                                    ServerManager.Instance.ChangeMapInstance(Session.Character.CharacterId,
                                                        Session.Character.Family.Act4Raid.MapInstanceId, 15, 9);
                                                    break;

                                                case MapInstanceType.Act4Calvina:
                                                    ServerManager.Instance.ChangeMapInstance(Session.Character.CharacterId,
                                                        Session.Character.Family.Act4Raid.MapInstanceId, 24, 6);
                                                    break;

                                                case MapInstanceType.Act4Berios:
                                                    ServerManager.Instance.ChangeMapInstance(Session.Character.CharacterId,
                                                        Session.Character.Family.Act4Raid.MapInstanceId, 20, 20);
                                                    break;
                                            }
                                        }
                                        else
                                        {
                                            Session.SendPacket(
                                                Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("LOW_REP"),
                                                    10));
                                        }
                                    }
                                    else
                                    {
                                        Session.SendPacket(
                                            Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("LOW_LVL"),
                                                10));
                                    }
                                }
                                else
                                {
                                    Session.SendPacket(
                                        Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("PORTAL_BLOCKED"),
                                            10));
                                }
                            }
                        }

                        return;

                    default:
                        Session.SendPacket(
                            Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("PORTAL_BLOCKED"), 10));
                        return;
                }

                if (Session?.CurrentMapInstance?.MapInstanceType == MapInstanceType.TimeSpaceInstance
                    && Session?.Character?.Timespace != null && !Session.Character.Timespace.InstanceBag.Lock)
                {
                    if (Session.Character.CharacterId == Session.Character.Timespace.InstanceBag.CreatorId)
                    {
                        Session.SendPacket(UserInterfaceHelper.GenerateDialog(
                            $"#rstart^1 rstart {Language.Instance.GetMessageFromKey("FIRST_ROOM_START")}"));
                    }

                    return;
                }

                if (Session?.CurrentMapInstance?.MapInstanceType != MapInstanceType.BaseMapInstance && portal.DestinationMapId == 134)
                {
                    if (!packet.Parameter.HasValue)
                    {
                        Session.SendPacket($"qna #preq^1 {Language.Instance.GetMessageFromKey("ACT4_RAID_EXIT")}");
                        return;
                    }
                }

                portal.OnTraversalEvents.ForEach(e => EventHelper.Instance.RunEvent(e));
                if (portal.DestinationMapInstanceId == default)
                {
                    return;
                }

                if (portal.SourceX == 37 && portal.SourceY == 15 && portal.SourceMapId == 2006)
                {
                    Session.SendPacket("ascr 0 0 0 0 0 0 0 0 -1");
                    Session.Character.CurrentArenaDeath = 0;
                    Session.Character.CurrentArenaKill = 0;
                }

                if (portal.SourceX == 38 && portal.SourceY == 3 && portal.SourceMapId == 2106)
                {
                    Session.SendPacket("ascr 0 0 0 0 0 0 0 0 -1");
                    Session.Character.CurrentArenaDeath = 0;
                    Session.Character.CurrentArenaKill = 0;
                }

                if (ServerManager.Instance.ChannelId == 51)
                {
                    if ((Session.Character.Faction == FactionType.Angel && portal.DestinationMapId == 131)
                        || (Session.Character.Faction == FactionType.Demon && portal.DestinationMapId == 130 ||
                        Session.Character.Faction == FactionType.Angel && portal.SourceX == 135 && portal.SourceY == 171 && portal.DestinationMapId == 134 ||
                        Session.Character.Faction == FactionType.Angel && portal.SourceX == 128 && portal.SourceY == 174 && portal.DestinationMapId == 153 ||
                        Session.Character.Faction == FactionType.Angel && portal.SourceX == 110 && portal.SourceY == 159 && portal.DestinationMapId == 154 ||
                        Session.Character.Faction == FactionType.Demon && portal.SourceX == 46 && portal.SourceY == 171 && portal.DestinationMapId == 134 ||
                        Session.Character.Faction == FactionType.Demon && portal.SourceX == 50 && portal.SourceY == 174 && portal.DestinationMapId == 153 ||
                        Session.Character.Faction == FactionType.Demon && portal.SourceX == 70 && portal.SourceY == 159 && portal.SourceMapId == 153))
                    {
                        Session.SendPacket(
                            Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("PORTAL_BLOCKED"), 10));
                        return;
                    }

                    if (Session.Character.Faction == FactionType.Demon && portal.SourceX == 128 && portal.SourceY == 174 && portal.DestinationMapId == 153)
                    {
                        portal.DestinationX = 110;
                        portal.DestinationY = 159;
                    }

                    if (Session.Character.Faction == FactionType.Demon && portal.SourceX == 140 && portal.SourceY == 11 && portal.DestinationMapId == 153)
                    {
                        portal.DestinationX = 135;
                        portal.DestinationY = 171;
                    }

                    if ((portal.DestinationMapId == 130 || portal.DestinationMapId == 131)
                        && timeSpanSinceLastPortal < 15)
                    {
                        Session.SendPacket(
                            Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("CANT_MOVE"), 10));
                        return;
                    }
                }

                Session.SendPacket(Session.CurrentMapInstance.GenerateRsfn());

                if (ServerManager.GetMapInstance(portal.SourceMapInstanceId).MapInstanceType
                    != MapInstanceType.BaseMapInstance
                    && ServerManager.GetMapInstance(portal.SourceMapInstanceId).MapInstanceType
                    != MapInstanceType.CaligorInstance
                    && ServerManager.GetMapInstance(portal.DestinationMapInstanceId).MapInstanceType
                    == MapInstanceType.BaseMapInstance)
                {
                    ServerManager.Instance.ChangeMap(Session.Character.CharacterId, Session.Character.MapId,
                        Session.Character.MapX, Session.Character.MapY);
                }
                else if (portal.DestinationMapInstanceId == Session.Character.Miniland.MapInstanceId)
                {
                    ServerManager.Instance.JoinMiniland(Session, Session);
                }
                else if (portal.DestinationMapId == 20000)
                {
                    ClientSession sess = ServerManager.Instance.Sessions.FirstOrDefault(s =>
                        s.Character.Miniland.MapInstanceId == portal.DestinationMapInstanceId);
                    if (sess != null)
                    {
                        ServerManager.Instance.JoinMiniland(Session, sess);
                    }
                }
                else
                {
                    if (ServerManager.Instance.ChannelId == 51)
                    {
                        short destinationX = portal.DestinationX;
                        short destinationY = portal.DestinationY;

                        if (portal.DestinationMapInstanceId == CaligorRaid.CaligorMapInstance?.MapInstanceId) /* Caligor Raid Map */
                        {
                            if (!packet.Parameter.HasValue)
                            {
                                Session.SendPacket($"qna #preq^1 {Language.Instance.GetMessageFromKey("CALIGOR_RAID_ENTER")}");
                                return;
                            }
                        }
                        else if (portal.DestinationMapId == 153) /* Unknown Land */
                        {
                            if (Session.Character.MapInstance == CaligorRaid.CaligorMapInstance && !packet.Parameter.HasValue)
                            {
                                Session.SendPacket($"qna #preq^1 {Language.Instance.GetMessageFromKey("CALIGOR_RAID_EXIT")}");
                                return;
                            }
                            else if (Session.Character.MapInstance != CaligorRaid.CaligorMapInstance)
                            {
                                if (destinationX <= 0 && destinationY <= 0)
                                {
                                    switch (Session.Character.Faction)
                                    {
                                        case FactionType.Angel:
                                            destinationX = 50;
                                            destinationY = 172;
                                            break;

                                        case FactionType.Demon:
                                            destinationX = 130;
                                            destinationY = 172;
                                            break;
                                    }
                                }
                            }
                        }

                        ServerManager.Instance.ChangeMapInstance(Session.Character.CharacterId,
                            portal.DestinationMapInstanceId, destinationX, destinationY);
                    }
                    else
                    {
                        ServerManager.Instance.ChangeMapInstance(Session.Character.CharacterId,
                            portal.DestinationMapInstanceId, portal.DestinationX, portal.DestinationY);
                    }
                }
                Session.Character.LastPortal = currentRunningSeconds;
            }
        }

        /// <summary>
        /// pulse packet
        /// </summary>
        /// <param name="pulsepacket"></param>
        public void Pulse(PulsePacket pulsepacket)
        {
            if (Session.Character.LastPulse.AddMilliseconds(80000) >= DateTime.Now
                && DateTime.Now >= Session.Character.LastPulse.AddMilliseconds(40000))
            {
                Session.Character.LastPulse = DateTime.Now;
            }
            else
            {
                Session.Disconnect();
            }

            Session.Character.MuteMessage();
            Session.Character.DeleteTimeout();
            CommunicationServiceClient.Instance.PulseAccount(Session.Account.AccountId);
        }

        /// <summary>
        /// rlPacket packet
        /// </summary>
        /// <param name="rlPacket"></param>
        public void RaidListRegister(RlPacket rlPacket)
        {
            switch (rlPacket.Type)
            {
                case 0: // Show the Raid List
                    if (Session.Character.Group?.IsLeader(Session) == true
                        && Session.Character.Group.GroupType != GroupType.Group
                        && ServerManager.Instance.GroupList.Any(s => s.GroupId == Session.Character.Group.GroupId))
                    {
                        Session.SendPacket(UserInterfaceHelper.GenerateRl(1));
                    }
                    else if (Session.Character.Group != null && Session.Character.Group.GroupType != GroupType.Group
                             && Session.Character.Group.IsLeader(Session))
                    {
                        Session.SendPacket(UserInterfaceHelper.GenerateRl(2));
                    }
                    else if (Session.Character.Group != null)
                    {
                        Session.SendPacket(UserInterfaceHelper.GenerateRl(3));
                    }
                    else
                    {
                        Session.SendPacket(UserInterfaceHelper.GenerateRl(0));
                    }

                    break;

                case 1: // Register a team
                    if (Session.Character.Group != null
                        && Session.Character.Group.GroupType != GroupType.Group
                        && Session.Character.Group.IsLeader(Session)
                        && !ServerManager.Instance.GroupList.Any(s => s.GroupId == Session.Character.Group.GroupId))
                    {
                        ServerManager.Instance.GroupList.Add(Session.Character.Group);
                        Session.SendPacket(UserInterfaceHelper.GenerateRl(1));
                        Session.SendPacket(
                            UserInterfaceHelper.GenerateInfo(Language.Instance.GetMessageFromKey("RAID_REGISTERED")));
                        ServerManager.Instance.Broadcast(Session,
                            $"qnaml 100 #rl {string.Format(Language.Instance.GetMessageFromKey("SEARCH_TEAM_MEMBERS"), Session.Character.Name, Session.Character.Group.Raid?.Label)}",
                            ReceiverType.AllExceptGroup);
                    }

                    break;

                case 2: // Cancel the team registration
                    if (Session.Character.Group != null
                        && Session.Character.Group.GroupType != GroupType.Group
                        && Session.Character.Group.IsLeader(Session)
                        && ServerManager.Instance.GroupList.Any(s => s.GroupId == Session.Character.Group.GroupId))
                    {
                        ServerManager.Instance.GroupList.Remove(Session.Character.Group);
                        Session.SendPacket(UserInterfaceHelper.GenerateRl(2));
                        Session.SendPacket(
                            UserInterfaceHelper.GenerateInfo(Language.Instance.GetMessageFromKey("RAID_UNREGISTERED")));
                    }

                    break;

                case 3: // Become a team member
                    ClientSession targetSession = ServerManager.Instance.GetSessionByCharacterName(rlPacket.CharacterName);

                    ItemInstance amulet = Session.Character.Inventory.LoadBySlotAndType((byte)EquipmentType.Amulet, InventoryType.Wear);
                    if (targetSession.Character.Group.Raid.Label == "Lord Draco")
                    {
                        if (amulet != null)
                        {
                            if (amulet.ItemVNum != 4503)
                            {
                                Session.SendPacket($"info {Language.Instance.GetMessageFromKey("NO_AMULET_DRACO")}");
                                return;
                            }
                        }
                        else
                        {
                            Session.SendPacket($"info {Language.Instance.GetMessageFromKey("NO_AMULET_DRACO")}");
                            return;
                        }
                    }
                    if (targetSession.Character.Group.Raid.Label == "Glacerus the Ice Cold")
                    {
                        if (amulet != null)
                        {
                            if (amulet.ItemVNum != 4504)
                            {
                                Session.SendPacket($"info {Language.Instance.GetMessageFromKey("NO_AMULET_GLACE")}");
                                return;
                            }
                        }
                        else
                        {
                            Session.SendPacket($"info {Language.Instance.GetMessageFromKey("NO_AMULET_GLACE")}");
                            return;
                        }
                    }


                    if (targetSession?.Character?.Group == null)
                    {
                        return;
                    }

                    if (targetSession.Character.CharacterId == Session.Character.CharacterId)
                    {
                        return;
                    }

                    if (!targetSession.Character.Group.IsLeader(targetSession))
                    {
                        return;
                    }

                    if (!ServerManager.Instance.GroupList.Any(group => group.GroupId == targetSession.Character.Group.GroupId))
                    {
                        return;
                    }

                    targetSession.Character.GroupSentRequestCharacterIds.Add(Session.Character.CharacterId);

                    GroupJoin(new PJoinPacket
                    {
                        RequestType = GroupRequestType.Accepted,
                        CharacterId = targetSession.Character.CharacterId
                    });

                    break;
            }
        }

        /// <summary>
        /// rdPacket packet
        /// </summary>
        /// <param name="rdPacket"></param>
        public void RaidManage(RdPacket rdPacket)
        {
            Group grp;
            switch (rdPacket.Type)
            {
                // Join Raid
                case 1:
                    if (Session.CurrentMapInstance.MapInstanceType == MapInstanceType.RaidInstance)
                    {
                        return;
                    }

                    if (rdPacket.Parameter != null)
                    {
                        #region Draco
                        if (rdPacket.Parameter.Value == 16)
                        {
                            ItemInstance amulet = Session.Character.Inventory.LoadBySlotAndType((byte)EquipmentType.Amulet, InventoryType.Wear);
                            if (amulet != null)
                            {
                                if (amulet.ItemVNum != 4503)
                                {
                                    Session.SendPacket($"info {Language.Instance.GetMessageFromKey("NO_AMULET_DRACO")}");
                                    return;
                                }
                            }
                            else
                            {
                                Session.SendPacket($"info {Language.Instance.GetMessageFromKey("NO_AMULET_DRACO")}");
                                return;
                            }
                        }
                        #endregion

                        #region Glace
                        if (rdPacket.Parameter.Value == 17)
                        {
                            ItemInstance amulet = Session.Character.Inventory.LoadBySlotAndType((byte)EquipmentType.Amulet, InventoryType.Wear);
                            if (amulet != null)
                            {
                                if (amulet.ItemVNum != 4504)
                                {
                                    Session.SendPacket($"info {Language.Instance.GetMessageFromKey("NO_AMULET_GLACE")}");
                                    return;
                                }
                            }
                            else
                            {
                                Session.SendPacket($"info {Language.Instance.GetMessageFromKey("NO_AMULET_GLACE")}");
                                return;
                            }
                        }
                        #endregion
                    }

                    ClientSession target = ServerManager.Instance.GetSessionByCharacterId(rdPacket.CharacterId);
                    if (rdPacket.Parameter == null && target?.Character?.Group == null && Session.Character.Group != null && Session.Character.Group.IsLeader(Session) && Session.Character.Group?.Sessions.FirstOrDefault() == Session)
                    {
                        GroupJoin(new PJoinPacket
                        {
                            RequestType = GroupRequestType.Invited,
                            CharacterId = rdPacket.CharacterId
                        });
                    }
                    else if (Session.Character.Group == null)
                    {
                        GroupJoin(new PJoinPacket
                        {
                            RequestType = GroupRequestType.Accepted,
                            CharacterId = rdPacket.CharacterId
                        });
                    }

                    break;

                // Leave Raid
                case 2:
                    if (Session.Character.Group == null)
                    {
                        return;
                    }

                    Session.SendPacket(
                        UserInterfaceHelper.GenerateMsg(string.Format(Language.Instance.GetMessageFromKey("LEFT_RAID")),
                            0));
                    if (Session?.CurrentMapInstance?.MapInstanceType == MapInstanceType.RaidInstance)
                    {
                        ServerManager.Instance.ChangeMap(Session.Character.CharacterId, Session.Character.MapId,
                            Session.Character.MapX, Session.Character.MapY);
                    }

                    grp = Session.Character.Group;
                    Session.SendPacket(Session.Character.GenerateRaid(1, true));
                    Session.SendPacket(Session.Character.GenerateRaid(2, true));
                    if (grp != null)
                    {
                        grp.LeaveGroup(Session);
                        grp.Sessions.ForEach(s =>
                        {
                            s.SendPacket(grp.GenerateRdlst());
                            s.SendPacket(s.Character.GenerateRaid(0));
                        });
                    }
                    break;

                // Kick from Raid
                case 3:
                    if (Session.CurrentMapInstance.MapInstanceType == MapInstanceType.RaidInstance)
                    {
                        return;
                    }

                    if (Session.Character.Group?.IsLeader(Session) == true)
                    {
                        ClientSession chartokick = ServerManager.Instance.GetSessionByCharacterId(rdPacket.CharacterId);
                        if (chartokick.Character?.Group == null)
                        {
                            return;
                        }

                        chartokick.SendPacket(
                            UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("KICK_RAID"), 0));
                        grp = chartokick.Character?.Group;
                        chartokick.SendPacket(chartokick.Character?.GenerateRaid(1, true));
                        chartokick.SendPacket(chartokick.Character?.GenerateRaid(2, true));
                        grp?.LeaveGroup(chartokick);
                        grp?.Sessions.ForEach(s =>
                        {
                            s.SendPacket(grp.GenerateRdlst());
                            s.SendPacket(s.Character.GenerateRaid(0));
                        });
                    }

                    break;

                // Disolve Raid
                case 4:
                    if (Session.CurrentMapInstance.MapInstanceType == MapInstanceType.RaidInstance)
                    {
                        return;
                    }

                    if (Session.Character.Group?.IsLeader(Session) == true)
                    {
                        grp = Session.Character.Group;

                        ClientSession[] grpmembers = new ClientSession[40];
                        grp.Sessions.CopyTo(grpmembers);
                        foreach (ClientSession targetSession in grpmembers)
                        {
                            if (targetSession != null)
                            {
                                targetSession.SendPacket(targetSession.Character.GenerateRaid(1, true));
                                targetSession.SendPacket(targetSession.Character.GenerateRaid(2, true));
                                targetSession.SendPacket(
                                    UserInterfaceHelper.GenerateMsg(
                                        Language.Instance.GetMessageFromKey("RAID_DISOLVED"), 0));
                                grp.LeaveGroup(targetSession);
                            }
                        }

                        ServerManager.Instance.GroupList.RemoveAll(s => s.GroupId == grp.GroupId);
                        ServerManager.Instance.ThreadSafeGroupList.Remove(grp.GroupId);
                    }

                    break;
            }
        }

        public void QtPacket(QtPacket qtPacket)
        {
            switch (qtPacket.Type)
            {
                // On Target Dest
                case 1:
                    Session.Character.IncrementQuests(QuestType.GoTo, Session.CurrentMapInstance.Map.MapId, Session.Character.PositionX, Session.Character.PositionY);
                    break;

                // Give Up Quest
                case 3:
                    CharacterQuest charQuest = Session.Character.Quests?.FirstOrDefault(q => q.QuestNumber == qtPacket.Data);
                    if (charQuest == null || charQuest.IsMainQuest)
                    {
                        return;
                    }
                    Session.Character.RemoveQuest(charQuest.QuestId, true);
                    break;

                // Ask for rewards
                case 4:
                    break;
            }
        }

        /// <summary>
        /// req_info packet
        /// </summary>
        /// <param name="reqInfoPacket"></param>
        public void ReqInfo(ReqInfoPacket reqInfoPacket)
        {
            if (Session.Character != null)
            {
                if (reqInfoPacket.Type == 6)
                {
                    if (reqInfoPacket.MateVNum.HasValue)
                    {
                        Mate mate = Session.CurrentMapInstance?.Sessions?.FirstOrDefault(s => s?.Character?.Mates != null && s.Character.Mates.Any(o => o.MateTransportId == reqInfoPacket.MateVNum.Value))?
                            .Character.Mates.FirstOrDefault(m => m.MateTransportId == reqInfoPacket.MateVNum.Value);

                        if (mate != null)
                        {
                            Session.SendPacket(mate.GenerateEInfo());
                        }
                    }
                }
                else if (reqInfoPacket.Type == 5)
                {
                    NpcMonster npc = ServerManager.GetNpcMonster((short)reqInfoPacket.TargetVNum);

                    if (Session.CurrentMapInstance?.GetMonsterById(Session.Character.LastNpcMonsterId)
                        is MapMonster monster && monster.Monster?.OriginalNpcMonsterVNum == reqInfoPacket.TargetVNum)
                    {
                        npc = ServerManager.GetNpcMonster(monster.Monster.NpcMonsterVNum);
                    }

                    if (npc != null)
                    {
                        Session.SendPacket(npc.GenerateEInfo());
                    }
                }
                else if (reqInfoPacket.Type == 12)
                {
                    if (Session.Character.Inventory != null)
                    {
                        Session.SendPacket(Session.Character.Inventory.LoadBySlotAndType((short)reqInfoPacket.TargetVNum, InventoryType.Equipment)?.GenerateReqInfo());
                    }
                }
                else
                {
                    if (ServerManager.Instance.GetSessionByCharacterId(reqInfoPacket.TargetVNum)?.Character is Character character)
                    {
                        Session.SendPacket(character.GenerateReqInfo());
                    }
                }
            }
        }

        /// <summary>
        /// rest packet
        /// </summary>
        /// <param name="sitpacket"></param>
        public void Rest(SitPacket sitpacket)
        {
            if (Session.Character.MeditationDictionary.Count != 0)
            {
                Session.Character.MeditationDictionary.Clear();
            }

            sitpacket.Users?.ForEach(u =>
            {
                if (u.UserType == 1)
                {
                    Session.Character.Rest();
                }
                else
                {
                    Session.CurrentMapInstance.Broadcast(Session.Character.Mates
                        .Find(s => s.MateTransportId == (int)u.UserId)?.GenerateRest(sitpacket.Users[0] != u));
                }
            });
        }

        /// <summary>
        /// fbPacket packet
        /// </summary>
        /// <param name="fbPacket"></param>
        public void RainbowBattleManage(FbPacket fbPacket)
        {
            Group grp;
            switch (fbPacket.Type)
            {
                // Join BaTeam
                case 1:
                    if (Session.CurrentMapInstance.MapInstanceType == MapInstanceType.RainbowBattleInstance)
                    {
                        return;
                    }

                    ClientSession target = ServerManager.Instance.GetSessionByCharacterId(fbPacket.CharacterId);
                    if (fbPacket.Parameter == null && target?.Character?.Group == null
                        && Session.Character.Group.IsLeader(Session))
                    {
                        GroupJoin(new PJoinPacket
                        {
                            RequestType = GroupRequestType.Invited,
                            CharacterId = fbPacket.CharacterId
                        });
                    }
                    else if (Session.Character.Group == null)
                    {
                        GroupJoin(new PJoinPacket
                        {
                            RequestType = GroupRequestType.Accepted,
                            CharacterId = fbPacket.CharacterId
                        });
                    }

                    break;

                // Leave BaTeam
                case 2:
                    ClientSession sender = ServerManager.Instance.GetSessionByCharacterId(fbPacket.CharacterId);
                    if (sender?.Character?.Group == null)
                    {
                        return;
                    }

                    Session.SendPacket(
                        UserInterfaceHelper.GenerateMsg(string.Format(Language.Instance.GetMessageFromKey("LEFT_TEAM")),
                            0));
                    if (Session?.CurrentMapInstance?.MapInstanceType == MapInstanceType.RainbowBattleInstance)
                    {
                        ServerManager.Instance.ChangeMap(Session.Character.CharacterId, Session.Character.MapId,
                            Session.Character.MapX, Session.Character.MapY);
                    }

                    grp = sender.Character?.Group;
                    Session.SendPacket(Session.Character.GenerateFbt(1, true));
                    Session.SendPacket(Session.Character.GenerateFbt(2, true));

                    grp.Sessions.ForEach(s =>
                    {
                        s.SendPacket(grp.GenerateFblst());
                        //s.SendPacket(grp.GeneraterRaidmbf(s));
                        s.SendPacket(s.Character.GenerateFbt(0));
                    });

                    RainbowBattleMember rainbowbattleTeamMember = ServerManager.Instance.RainbowBattleMembers.Where(s => s.Session == Session).First();
                    if (rainbowbattleTeamMember != null)
                    {
                        ServerManager.Instance.RainbowBattleMembers.Remove(rainbowbattleTeamMember);
                    }

                    RainbowBattleMember rainbowbattleTeamMemberRegistered = ServerManager.Instance.RainbowBattleMembersRegistered.Where(s => s.Session == Session).First();
                    if (rainbowbattleTeamMemberRegistered != null)
                    {
                        ServerManager.Instance.RainbowBattleMembersRegistered.Remove(rainbowbattleTeamMember);
                    }
                    break;

                // Kick from BaTeam
                case 3:
                    if (Session.CurrentMapInstance.MapInstanceType == MapInstanceType.RainbowBattleInstance)
                    {
                        return;
                    }

                    if (Session.Character.Group?.IsLeader(Session) == true)
                    {
                        ClientSession chartokick = ServerManager.Instance.GetSessionByCharacterId(fbPacket.CharacterId);
                        if (chartokick.Character?.Group == null)
                        {
                            return;
                        }

                        RainbowBattleMember rainbowbattleTeamMemberRegisteredd = ServerManager.Instance.RainbowBattleMembersRegistered.Where(s => s.Session == chartokick).First();
                        if (rainbowbattleTeamMemberRegisteredd != null)
                        {
                            Session.SendPacket(
                                UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("CANNOT_KICK_BA"), 0));
                            return;
                        }

                        chartokick.SendPacket(
                            UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("KICK_BA"), 0));
                        grp = chartokick.Character?.Group;
                        chartokick.SendPacket(chartokick.Character?.GenerateFbt(1, true));
                        chartokick.SendPacket(chartokick.Character?.GenerateFbt(2, true));
                        grp?.LeaveGroup(chartokick);
                        grp?.Sessions.ForEach(s =>
                        {
                            s.SendPacket(grp.GenerateRdlst());
                            s.SendPacket(s.Character.GenerateFbt(0));
                        });
                        RainbowBattleMember rainbowbattleTeamMemberr = ServerManager.Instance.RainbowBattleMembers.Where(s => s.Session == chartokick).First();
                        if (rainbowbattleTeamMemberr != null)
                        {
                            ServerManager.Instance.RainbowBattleMembers.Remove(rainbowbattleTeamMemberr);
                        }
                    }

                    break;

                // Disolve BaTeam
                case 4:
                    if (Session.CurrentMapInstance.MapInstanceType == MapInstanceType.RainbowBattleInstance)
                    {
                        return;
                    }

                    if (Session.Character.Group?.IsLeader(Session) == true)
                    {
                        RainbowBattleMember rainbowbattleTeamMemberRegistereddd = ServerManager.Instance.RainbowBattleMembersRegistered?.Where(s => s.Session == Session).First();
                        if (rainbowbattleTeamMemberRegistereddd != null)
                        {
                            Session.SendPacket(
                                UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("CANNOT_DISSOLVE_BA"), 0));
                            return;
                        }

                        grp = Session.Character.Group;

                        ClientSession[] grpmembers = new ClientSession[40];
                        grp.Sessions.CopyTo(grpmembers);
                        foreach (ClientSession targetSession in grpmembers)
                        {
                            if (targetSession != null)
                            {
                                targetSession.SendPacket(targetSession.Character.GenerateFbt(1, true));
                                targetSession.SendPacket(targetSession.Character.GenerateFbt(2, true));
                                targetSession.SendPacket(
                                    UserInterfaceHelper.GenerateMsg(
                                        Language.Instance.GetMessageFromKey("BA_DISOLVED"), 0));
                                grp.LeaveGroup(targetSession);

                                RainbowBattleMember rainbowbattleTeamMemberrr = ServerManager.Instance.RainbowBattleMembers.Where(s => s.Session == targetSession).First();
                                if (rainbowbattleTeamMemberrr != null)
                                {
                                    ServerManager.Instance.RainbowBattleMembers.Remove(rainbowbattleTeamMemberrr);
                                }
                            }
                        }

                        ServerManager.Instance.GroupList.RemoveAll(s => s.GroupId == grp.GroupId);
                        ServerManager.Instance.ThreadSafeGroupList.Remove(grp.GroupId);
                    }

                    break;
            }
        }

        /// <summary>
        /// revival packet
        /// </summary>
        /// <param name="revivalPacket"></param>
        public void Revive(RevivalPacket revivalPacket)
        {
            if (Session.Character.Hp > 0)
            {
                return;
            }

            switch (revivalPacket.Type)
            {
                case 0:
                    switch (Session.CurrentMapInstance.MapInstanceType)
                    {
                        case MapInstanceType.LodInstance:
                            const int saver = 1211;
                            if (Session.Character.Inventory.CountItem(saver) < 1)
                            {
                                Session.SendPacket(
                                    UserInterfaceHelper.GenerateMsg(
                                        Language.Instance.GetMessageFromKey("NOT_ENOUGH_SAVER"), 0));
                                ServerManager.Instance.ReviveFirstPosition(Session.Character.CharacterId);
                            }
                            else
                            {
                                Session.Character.Inventory.RemoveItemAmount(saver);
                                Session.Character.Hp = (int)Session.Character.HPLoad();
                                Session.Character.Mp = (int)Session.Character.MPLoad();
                                Session.CurrentMapInstance?.Broadcast(Session, Session.Character.GenerateRevive());
                                Session.SendPacket(Session.Character.GenerateStat());
                            }

                            break;

                        case MapInstanceType.Act4Berios:
                        case MapInstanceType.Act4Calvina:
                        case MapInstanceType.Act4Hatus:
                        case MapInstanceType.Act4Morcos:
                            if (Session.Character.Reputation < Session.Character.Level * 10)
                            {
                                Session.SendPacket(
                                    UserInterfaceHelper.GenerateMsg(
                                        Language.Instance.GetMessageFromKey("NOT_ENOUGH_REPUT"), 0));
                                ServerManager.Instance.ReviveFirstPosition(Session.Character.CharacterId);
                            }
                            else
                            {
                                Session.Character.GetReputation(Session.Character.Level * -10);
                                Session.Character.Hp = (int)Session.Character.HPLoad();
                                Session.Character.Mp = (int)Session.Character.MPLoad();
                                Session.CurrentMapInstance?.Broadcast(Session, Session.Character.GenerateRevive());
                                Session.SendPacket(Session.Character.GenerateStat());
                            }

                            break;

                        case MapInstanceType.CaligorInstance:
                            Session.Character.Hp = (int)Session.Character.HPLoad();
                            Session.Character.Mp = (int)Session.Character.MPLoad();
                            short x = 0;
                            short y = 0;
                            switch (Session.Character.Faction)
                            {
                                case FactionType.Angel:
                                    x = 50;
                                    y = 172;
                                    break;

                                case FactionType.Demon:
                                    x = 130;
                                    y = 172;
                                    break;
                            }
                            ServerManager.Instance.ChangeMapInstance(Session.Character.CharacterId, Session.Character.MapInstance.MapInstanceId, x, y);
                            Session.Character.AddBuff(new Buff(169, Session.Character.Level), Session.Character.BattleEntity);
                            break;

                        default:
                            const int seed = 1012;
                            if (Session.Character.Inventory.CountItem(seed) < 10 && Session.Character.Level > 20)
                            {
                                Session.SendPacket(UserInterfaceHelper.GenerateMsg(
                                    Language.Instance.GetMessageFromKey("NOT_ENOUGH_POWER_SEED"), 0));
                                ServerManager.Instance.ReviveFirstPosition(Session.Character.CharacterId);
                                Session.SendPacket(
                                    Session.Character.GenerateSay(
                                        Language.Instance.GetMessageFromKey("NOT_ENOUGH_SEED_SAY"), 0));
                            }
                            else
                            {
                                if (Session.Character.Level > 20 && Session.CurrentMapInstance.Map.MapTypes.Any(s => s.MapTypeId != (short)MapTypeEnum.Act61 || s.MapTypeId != (short)MapTypeEnum.Act61a || s.MapTypeId != (short)MapTypeEnum.Act61d || s.MapTypeId != (short)MapTypeEnum.Act71))
                                {
                                    Session.SendPacket(Session.Character.GenerateSay(
                                    string.Format(Language.Instance.GetMessageFromKey("SEED_USED"), 10), 10));
                                    Session.Character.Inventory.RemoveItemAmount(seed, 10);
                                    Session.Character.Hp = (int)Session.Character.HPLoad();
                                    Session.Character.Mp = (int)Session.Character.MPLoad();
                                    Session.Character.AddBuff(new Buff(44, Session.Character.Level), Session.Character.BattleEntity);
                                }
                                else if (Session.Character.HeroLevel > 0 && Session.Character.Level > 20 && Session.Character.HeroXp > 0 && Session.CurrentMapInstance.Map.MapTypes.Any(s => s.MapTypeId == (short)MapTypeEnum.Act61 || s.MapTypeId == (short)MapTypeEnum.Act61a || s.MapTypeId == (short)MapTypeEnum.Act61d || s.MapTypeId == (short)MapTypeEnum.Act71))
                                {
                                    Session.Character.HeroXp -= 60000;
                                    Session.SendPacket(Session.Character.GenerateLev());
                                    Session.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("LOSE_HERO_EXP"), 11));
                                    Session.SendPacket(Session.Character.GenerateSay(
                                    string.Format(Language.Instance.GetMessageFromKey("SEED_USED"), 10), 10));
                                    Session.Character.Inventory.RemoveItemAmount(seed, 10);
                                    Session.Character.Hp = (int)Session.Character.HPLoad();
                                    Session.Character.Mp = (int)Session.Character.MPLoad();
                                    Session.Character.AddBuff(new Buff(44, Session.Character.Level), Session.Character.BattleEntity);
                                }
                                if (Session.Character.HeroXp < 0)
                                {
                                    Session.Character.HeroXp -= 0;
                                }
                                if (Session.Character.Dignity < -1000)
                                {
                                    Session.Character.Dignity = -1000;
                                }
                                else
                                {
                                    Session.Character.Hp = (int)Session.Character.HPLoad();
                                    Session.Character.Mp = (int)Session.Character.MPLoad();
                                }
                                Session.CurrentMapInstance?.Broadcast(Session, Session.Character.GenerateTp());
                                Session.CurrentMapInstance?.Broadcast(Session, Session.Character.GenerateRevive());
                                Session.SendPacket(Session.Character.GenerateFd());
                                Session.SendPacket(Session.Character.GenerateStat());
                                Session.SendPacket(Session.Character.GenerateLev());
                            }
                            break;
                    }

                    break;

                case 1:
                    ServerManager.Instance.ReviveFirstPosition(Session.Character.CharacterId);
                    if (Session.CurrentMapInstance.MapInstanceType == MapInstanceType.BaseMapInstance)
                    {
                        if (Session.Character.Level > 20)
                        {
                            Session.Character.AddBuff(new Buff(44, Session.Character.Level), Session.Character.BattleEntity);
                        }
                    }
                    break;

                case 2:
                    if ((Session.CurrentMapInstance == ServerManager.Instance.ArenaInstance || Session.CurrentMapInstance == ServerManager.Instance.FamilyArenaInstance || Session.CurrentMapInstance == ServerManager.Instance.ArenaInstance2) &&
                        Session.Character.Gold >= 100)
                    {
                        Session.Character.Hp = (int)Session.Character.HPLoad();
                        Session.Character.Mp = (int)Session.Character.MPLoad();
                        Session.CurrentMapInstance?.Broadcast(Session, Session.Character.GenerateTp());
                        Session.CurrentMapInstance?.Broadcast(Session, Session.Character.GenerateRevive());
                        Session.SendPacket(Session.Character.GenerateStat());
                        Session.Character.Gold -= 100;
                        Session.SendPacket(Session.Character.GenerateGold());
                        Session.Character.LastPVPRevive = DateTime.Now;
                        Observable.Timer(TimeSpan.FromSeconds(5)).Subscribe(observer =>
                             Session.SendPacket(Session.Character.GenerateSay("PvP is now available", 10)));
                    }
                    else
                    {
                        ServerManager.Instance.ReviveFirstPosition(Session.Character.CharacterId);
                    }

                    break;
            }
            Session.Character.BattleEntity.SendBuffsPacket();
        }

        /// <summary>
        /// say packet
        /// </summary>
        /// <param name="sayPacket"></param>
        public void Say(SayPacket sayPacket)
        {
            if (string.IsNullOrEmpty(sayPacket.Message))
            {
                return;
            }

            var penalty = Session.Account.PenaltyLogs.OrderByDescending(s => s.DateEnd).FirstOrDefault();
            var message = sayPacket.Message;
            if (Session.Character.IsMuted() && penalty != null)
            {
                if (Session.Character.Gender == GenderType.Female)
                {
                    ConcurrentBag<ArenaTeamMember> member = ServerManager.Instance.ArenaTeams.ToList().FirstOrDefault(s => s.Any(e => e.Session == Session));
                    if (Session.CurrentMapInstance.MapInstanceType == MapInstanceType.TalentArenaMapInstance && member != null)
                    {
                        var member2 = member.FirstOrDefault(o => o.Session == Session);
                        member.Replace(s => member2 != null && s.ArenaTeamType == member2.ArenaTeamType && s != member2).Replace(s => s.ArenaTeamType == member.FirstOrDefault(o => o.Session == Session)?.ArenaTeamType).ToList().ForEach(o => o.Session.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("MUTED_FEMALE"), 1)));
                    }
                    else
                    {
                        Session.CurrentMapInstance?.Broadcast(Session, Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("MUTED_FEMALE"), 1));
                    }

                    Session.SendPacket(Session.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("MUTE_TIME"), (penalty.DateEnd - DateTime.Now).ToString(@"hh\:mm\:ss")), 11));
                    Session.SendPacket(Session.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("MUTE_TIME"), (penalty.DateEnd - DateTime.Now).ToString(@"hh\:mm\:ss")), 12));
                }
                else
                {
                    ConcurrentBag<ArenaTeamMember> member = ServerManager.Instance.ArenaTeams.ToList().FirstOrDefault(s => s.Any(e => e.Session == Session));
                    if (Session.CurrentMapInstance.MapInstanceType == MapInstanceType.TalentArenaMapInstance && member != null)
                    {
                        var member2 = member.FirstOrDefault(o => o.Session == Session);
                        member.Replace(s => member2 != null && s.ArenaTeamType == member2.ArenaTeamType && s != member2).Replace(s => s.ArenaTeamType == member.FirstOrDefault(o => o.Session == Session)?.ArenaTeamType).ToList().ForEach(o => o.Session.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("MUTED_MALE"), 1)));
                    }
                    else
                    {
                        Session.CurrentMapInstance?.Broadcast(Session, Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("MUTED_MALE"), 1));
                    }

                    Session.SendPacket(Session.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("MUTE_TIME"), (penalty.DateEnd - DateTime.Now).ToString(@"hh\:mm\:ss")), 11));
                    Session.SendPacket(Session.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("MUTE_TIME"), (penalty.DateEnd - DateTime.Now).ToString(@"hh\:mm\:ss")), 12));
                }
            }
            else
            {
                byte type = CharacterHelper.AuthorityChatColor(Session.Character.Authority);

                ConcurrentBag<ArenaTeamMember> member = null;
                lock (ServerManager.Instance.ArenaTeams)
                {
                    member = ServerManager.Instance.ArenaTeams.ToList().FirstOrDefault(s => s.Any(e => e.Session == Session));
                }
                if (Session.Character.Authority == AuthorityType.GameSage)
                {
                    type = CharacterHelper.AuthorityChatColor(Session.Character.Authority);
                    if (Session.CurrentMapInstance.MapInstanceType == MapInstanceType.TalentArenaMapInstance && member != null)
                    {
                        ArenaTeamMember member2 = member.FirstOrDefault(o => o.Session == Session);
                        member.Replace(s => member2 != null && s.ArenaTeamType == member2.ArenaTeamType && s != member2).Replace(s => s.ArenaTeamType == member.FirstOrDefault(o => o.Session == Session)?.ArenaTeamType).ToList().ForEach(o => o.Session.SendPacket(Session.Character.GenerateSay(message.Trim(), 1)));
                    }
                    else
                    {
                        Session.CurrentMapInstance?.Broadcast(Session, Session.Character.GenerateSay(message.Trim(), 1), ReceiverType.AllExceptMe);
                    }
                    message = $"[GS {Session.Character.Name}]: " + message;
                }
                if (Session.Character.Authority == AuthorityType.GameMaster)
                {
                    type = CharacterHelper.AuthorityChatColor(Session.Character.Authority);
                    if (Session.CurrentMapInstance.MapInstanceType == MapInstanceType.TalentArenaMapInstance && member != null)
                    {
                        ArenaTeamMember member2 = member.FirstOrDefault(o => o.Session == Session);
                        member.Replace(s => member2 != null && s.ArenaTeamType == member2.ArenaTeamType && s != member2).Replace(s => s.ArenaTeamType == member.FirstOrDefault(o => o.Session == Session)?.ArenaTeamType).ToList().ForEach(o => o.Session.SendPacket(Session.Character.GenerateSay(message.Trim(), 1)));
                    }
                    else
                    {
                        Session.CurrentMapInstance?.Broadcast(Session, Session.Character.GenerateSay(message.Trim(), 1), ReceiverType.AllExceptMe);
                    }
                    message = $"[GM {Session.Character.Name}]: " + message;
                }
                if (Session.Character.Authority == AuthorityType.Administrator)
                {
                    if (Session.CurrentMapInstance.MapInstanceType == MapInstanceType.TalentArenaMapInstance && member != null)
                    {
                        ArenaTeamMember member2 = member.FirstOrDefault(o => o.Session == Session);
                        member.Replace(s => member2 != null && s.ArenaTeamType == member2.ArenaTeamType && s != member2).Replace(s => s.ArenaTeamType == member.FirstOrDefault(o => o.Session == Session)?.ArenaTeamType).ToList().ForEach(o => o.Session.SendPacket(Session.Character.GenerateSay(message.Trim(), 1)));
                    }
                    else
                    {
                        Session.CurrentMapInstance?.Broadcast(Session, Session.Character.GenerateSay(message.Trim(), 1), ReceiverType.AllExceptMe);
                    }
                    message = $"[Administrator {Session.Character.Name}]: " + message;
                }
                if (Session.CurrentMapInstance.MapInstanceType == MapInstanceType.TalentArenaMapInstance && member != null)
                {
                    ArenaTeamMember member2 = member.FirstOrDefault(o => o.Session == Session);
                    member.Where(s => s.ArenaTeamType == member2?.ArenaTeamType && s != member2).ToList().ForEach(o => o.Session.SendPacket(Session.Character.GenerateSay(message.Trim(), type)));
                }
                else
                {
                    Session.CurrentMapInstance?.Broadcast(Session, Session.Character.GenerateSay(message.Trim(), type), ReceiverType.AllExceptMe);
                }
            }
        }

        /// <summary>
        /// pst packet
        /// </summary>
        /// <param name="pstPacket"></param>
        public void SendMail(PstPacket pstPacket)
        {
            if (pstPacket?.Data != null)
            {
                CharacterDTO receiver = DAOFactory.CharacterDAO.LoadByName(pstPacket.Receiver);
                if (receiver != null)
                {
                    string[] datasplit = pstPacket.Data.Split(' ');
                    if (datasplit.Length < 2)
                    {
                        return;
                    }


                    ItemInstance headWearable =
                        Session.Character.Inventory.LoadBySlotAndType((byte)EquipmentType.Hat, InventoryType.Wear);
                    byte color = headWearable?.Item.IsColored == true
                        ? (byte)headWearable.Design
                        : (byte)Session.Character.HairColor;
                    MailDTO mailcopy = new MailDTO
                    {
                        AttachmentAmount = 0,
                        IsOpened = false,
                        Date = DateTime.Now,
                        Title = datasplit[0],
                        Message = datasplit[1],
                        ReceiverId = receiver.CharacterId,
                        SenderId = Session.Character.CharacterId,
                        IsSenderCopy = true,
                        SenderClass = Session.Character.Class,
                        SenderGender = Session.Character.Gender,
                        SenderHairColor = Enum.IsDefined(typeof(HairColorType), color) ? (HairColorType)color : 0,
                        SenderHairStyle = Session.Character.HairStyle,
                        EqPacket = Session.Character.GenerateEqListForPacket(),
                        SenderMorphId = Session.Character.Morph == 0
                            ? (short)-1
                            : (short)(Session.Character.Morph > short.MaxValue ? 0 : Session.Character.Morph)
                    };
                    MailDTO mail = new MailDTO
                    {
                        AttachmentAmount = 0,
                        IsOpened = false,
                        Date = DateTime.Now,
                        Title = datasplit[0],
                        Message = datasplit[1],
                        ReceiverId = receiver.CharacterId,
                        SenderId = Session.Character.CharacterId,
                        IsSenderCopy = false,
                        SenderClass = Session.Character.Class,
                        SenderGender = Session.Character.Gender,
                        SenderHairColor = Enum.IsDefined(typeof(HairColorType), color) ? (HairColorType)color : 0,
                        SenderHairStyle = Session.Character.HairStyle,
                        EqPacket = Session.Character.GenerateEqListForPacket(),
                        SenderMorphId = Session.Character.Morph == 0
                            ? (short)-1
                            : (short)(Session.Character.Morph > short.MaxValue ? 0 : Session.Character.Morph)
                    };

                    MailServiceClient.Instance.SendMail(mailcopy);
                    MailServiceClient.Instance.SendMail(mail);

                    //Session.Character.MailList.Add((Session.Character.MailList.Count > 0 ? Session.Character.MailList.OrderBy(s => s.Key).Last().Key : 0) + 1, mailcopy);
                    Session.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("MAILED"),
                        11));
                    //Session.SendPacket(Session.Character.GeneratePost(mailcopy, 2));
                }
                else
                {
                    Session.SendPacket(
                        Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("USER_NOT_FOUND"), 10));
                }
            }
            else if (pstPacket != null && int.TryParse(pstPacket.Id.ToString(), out int id)
                     && byte.TryParse(pstPacket.Type.ToString(), out byte type))
            {
                if (pstPacket.Argument == 3)
                {
                    if (Session.Character.MailList.ContainsKey(id))
                    {
                        if (!Session.Character.MailList[id].IsOpened)
                        {
                            Session.Character.MailList[id].IsOpened = true;
                            MailDTO mailupdate = Session.Character.MailList[id];
                            DAOFactory.MailDAO.InsertOrUpdate(ref mailupdate);
                        }

                        Session.SendPacket(Session.Character.GeneratePostMessage(Session.Character.MailList[id], type));
                    }
                }
                else if (pstPacket.Argument == 2)
                {
                    if (Session.Character.MailList.ContainsKey(id))
                    {
                        MailDTO mail = Session.Character.MailList[id];
                        Session.SendPacket(
                            Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("MAIL_DELETED"), 11));
                        Session.SendPacket($"post 2 {type} {id}");
                        if (DAOFactory.MailDAO.LoadById(mail.MailId) != null)
                        {
                            DAOFactory.MailDAO.DeleteById(mail.MailId);
                        }

                        if (Session.Character.MailList.ContainsKey(id))
                        {
                            Session.Character.MailList.Remove(id);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// qset packet
        /// </summary>
        /// <param name="qSetPacket"></param>
        public void SetQuicklist(QSetPacket qSetPacket)
        {
            short data1 = 0, data2 = 0, type = qSetPacket.Type, q1 = qSetPacket.Q1, q2 = qSetPacket.Q2;
            if (qSetPacket.Data1.HasValue)
            {
                data1 = qSetPacket.Data1.Value;
            }

            if (qSetPacket.Data2.HasValue)
            {
                data2 = qSetPacket.Data2.Value;
            }

            switch (type)
            {
                case 0:
                case 1:

                    // client says qset 0 1 3 2 6 answer -> qset 1 3 0.2.6.0
                    Session.Character.QuicklistEntries.RemoveAll(n =>
                        n.Q1 == q1 && n.Q2 == q2
                        && (Session.Character.UseSp ? n.Morph == Session.Character.Morph : n.Morph == 0));

                    var morph = Session.Character.Morph;
                    if (Session.Character.Class == ClassType.MartialArtist && Session.Character.Morph == (byte)BrawlerMorphType.Dragon || Session.Character.Morph == (byte)BrawlerMorphType.Normal)
                    {
                        morph = 30;
                    }

                    Session.Character.QuicklistEntries.Add(new QuicklistEntryDTO
                    {
                        CharacterId = Session.Character.CharacterId,
                        Type = type,
                        Q1 = q1,
                        Q2 = q2,
                        Slot = data1,
                        Pos = data2,
                        Morph = (short)(Session.Character.UseSp ? morph : 0)
                    });
                    Session.SendPacket($"qset {q1} {q2} {type}.{data1}.{data2}.0");
                    break;

                case 2:

                    morph = Session.Character.Morph;
                    if (Session.Character.Class == ClassType.MartialArtist && Session.Character.Morph == (byte)BrawlerMorphType.Dragon || Session.Character.Morph == (byte)BrawlerMorphType.Normal)
                    {
                        morph = 30;
                    }

                    // DragDrop / Reorder qset type to1 to2 from1 from2 vars -> q1 q2 data1 data2
                    QuicklistEntryDTO qlFrom = Session.Character.QuicklistEntries.SingleOrDefault(n =>
                        n.Q1 == data1 && n.Q2 == data2
                        && (Session.Character.UseSp ? n.Morph == morph : n.Morph == 0));
                    if (qlFrom != null)
                    {
                        QuicklistEntryDTO qlTo = Session.Character.QuicklistEntries.SingleOrDefault(n =>
                            n.Q1 == q1 && n.Q2 == q2 && (Session.Character.UseSp
                                ? n.Morph == morph
                                : n.Morph == 0));
                        qlFrom.Q1 = q1;
                        qlFrom.Q2 = q2;
                        if (qlTo == null)
                        {
                            // Put 'from' to new position (datax)
                            Session.SendPacket(
                                $"qset {qlFrom.Q1} {qlFrom.Q2} {qlFrom.Type}.{qlFrom.Slot}.{qlFrom.Pos}.0");

                            // old 'from' is now empty.
                            Session.SendPacket($"qset {data1} {data2} 7.7.-1.0");
                        }
                        else
                        {
                            // Put 'from' to new position (datax)
                            Session.SendPacket(
                                $"qset {qlFrom.Q1} {qlFrom.Q2} {qlFrom.Type}.{qlFrom.Slot}.{qlFrom.Pos}.0");

                            // 'from' is now 'to' because they exchanged
                            qlTo.Q1 = data1;
                            qlTo.Q2 = data2;
                            Session.SendPacket($"qset {qlTo.Q1} {qlTo.Q2} {qlTo.Type}.{qlTo.Slot}.{qlTo.Pos}.0");
                        }
                    }

                    break;

                case 3:
                    morph = Session.Character.Morph;
                    if (Session.Character.Class == ClassType.MartialArtist && Session.Character.Morph == (byte)BrawlerMorphType.Dragon || Session.Character.Morph == (byte)BrawlerMorphType.Normal)
                    {
                        morph = 30;
                    }

                    // Remove from Quicklist
                    Session.Character.QuicklistEntries.RemoveAll(n =>
                        n.Q1 == q1 && n.Q2 == q2
                        && (Session.Character.UseSp ? n.Morph == morph : n.Morph == 0));
                    Session.SendPacket($"qset {q1} {q2} 7.7.-1.0");
                    break;

                default:
                    return;
            }
        }

        /// <summary>
        /// game_start packet
        /// </summary>
        /// <param name="gameStartPacket"></param>
        [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
        public void StartGame(GameStartPacket gameStartPacket)
        {

            #region Name's SGM or UP

            switch (Session.Character.Authority)
            {
                case AuthorityType.GameSage:
                    Session.Character.Compliment = 100;
                    break;

                default:
                    break;
            }

            #endregion Name's SGM or UP

            #region System Code Lock

            if (ServerManager.Instance.Configuration.AutoLootEnable)
            {
                if (Session.Character.LockCode != null)
                {
                    #region Lock Code

                    Session.Character.HeroChatBlocked = true;
                    Session.Character.ExchangeBlocked = true;
                    Session.Character.WhisperBlocked = true;
                    Session.Character.Invisible = true;
                    Session.Character.NoAttack = true;
                    Session.Character.NoMove = true;
                    Session.Character.VerifiedLock = false;

                    #endregion Lock Code

                    Session.SendPacket(Session.Character.GenerateSay($"Your account is locked. Please, use $Unlock command.", 12));
                }
                else
                {
                    #region Unlock Code

                    Session.Character.HeroChatBlocked = false;
                    Session.Character.ExchangeBlocked = false;
                    Session.Character.WhisperBlocked = false;
                    Session.Character.Invisible = false;
                    Session.Character.NoAttack = false;
                    Session.Character.NoMove = false;
                    Session.Character.VerifiedLock = true;

                    #endregion Unlock Code

                    Session.SendPacket(Session.Character.GenerateSay($"Your account doesn't have a lock. If you want more security, use $SetLock and a code.", 12));
                }
            }
            else
            {
                Session.Character.VerifiedLock = true;
            }

            #endregion System Code Lock

            if (Session?.Character == null || Session.IsOnMap || !Session.HasSelectedCharacter)
            {
                // character should have been selected in SelectCharacter
                return;
            }

            bool shouldRespawn = false;

            if (Session.Character.MapInstance?.Map?.MapTypes != null)
            {
                if (Session.Character.MapInstance.Map.MapTypes.Any(m => m.MapTypeId == (short)MapTypeEnum.Act4)
                    && ServerManager.Instance.ChannelId != 51)
                {
                    if (ServerManager.Instance.IsAct4Online())
                    {
                        Session.Character.ChangeChannel(ServerManager.Instance.Configuration.Act4IP, ServerManager.Instance.Configuration.Act4Port, 2);
                        return;
                    }

                    shouldRespawn = true;
                }
            }

            Session.CurrentMapInstance = Session.Character.MapInstance;

            if (ServerManager.Instance.Configuration.SceneOnCreate
                && Session.Character.GeneralLogs.CountLinq(s => s.LogType == "Connection") < 2)
            {
                Session.SendPacket("scene 40");
            }


            #region Rates
            int xpRate = ServerManager.Instance.Configuration.RateXP;
            int cxpRate = ServerManager.Instance.Configuration.RateHeroicXP;
            int fairyRate = ServerManager.Instance.Configuration.RateFairyXP;
            int gold = ServerManager.Instance.Configuration.RateGold;
            #endregion

            //if (Session.Character.Authority == AuthorityType.User)
            //{
            //    Session.Disconnect();
            //}

            //bool isOnline = CommunicationServiceClient.Instance.IsCharacterConnected(ServerManager.Instance.ServerGroup, TargetCharacter.CharacterId);

            Session.SendPacket($"msg 3 ========== NosTale DE 1 (Alzanor) ==========");
            Session.SendPacket($"msg 3 EXP: {xpRate} - CXP: {cxpRate} - Fairy: {fairyRate} - Gold: {gold}");
            Session.SendPacket($"msg 3 ==================================");
            Session.Character.LoadSpeed();
            Session.Character.LoadSkills();
            Session.SendPacket(Session.Character.GenerateEvtb());
            Session.SendPacket(Session.Character.GenerateSpPoint());
            Session.SendPacket(Session.Character.GenerateRsfi());
            Session.SendPackets(Session.Character.GenerateQuicklist()); 
            // Fishing
            Session.SendPacket(Session.Character.GenerateFishPacket(FishPacketType.Login, 0, 0));

            // Title
            Session.SendPacket(Session.Character.GenerateTitle());
            Session.SendPacket(Session.Character.GenerateTitInfo());
            Session.Character.GetTitleFromLevel();
            Session.Character.GetEffectFromTitle();

            Session.Character.Quests?.Where(q => q?.Quest?.TargetMap != null).ToList()
                .ForEach(qst => Session.SendPacket(qst.Quest.TargetPacket()));

            if (Session.Character.Hp <= 0 && (!Session.Character.IsSeal || ServerManager.Instance.ChannelId != 51))
            {
                ServerManager.Instance.ReviveFirstPosition(Session.Character.CharacterId);
            }
            else
            {
                if (shouldRespawn)
                {
                    RespawnMapTypeDTO resp = Session.Character.Respawn;
                    short x = (short)(resp.DefaultX + ServerManager.RandomNumber(-3, 3));
                    short y = (short)(resp.DefaultY + ServerManager.RandomNumber(-3, 3));
                    ServerManager.Instance.ChangeMap(Session.Character.CharacterId, resp.DefaultMapId, x, y);
                }
                else
                {
                    ServerManager.Instance.ChangeMap(Session.Character.CharacterId);
                }
            }

           

            Session.SendPacket(Session.Character.GenerateSki());
            Session.SendPacket(
                $"fd {Session.Character.Reputation} 0 {(int)Session.Character.Dignity} {Math.Abs(Session.Character.GetDignityIco())}");
            Session.SendPacket(Session.Character.GenerateFd());
            Session.SendPacket("rage 0 250000");
            Session.SendPacket("rank_cool 0 0 18000");
            ItemInstance specialistInstance = Session.Character.Inventory.LoadBySlotAndType(8, InventoryType.Wear);

            StaticBonusDTO medal = Session.Character.StaticBonusList.Find(s => s.StaticBonusType == StaticBonusType.BazaarMedalGold || s.StaticBonusType == StaticBonusType.BazaarMedalSilver);

            StaticBonusDTO buffs = Session.Character.StaticBonusList.Find(s => s.StaticBonusType == StaticBonusType.MedalOfErenia);

            if (medal != null)
            {
                Session.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("LOGIN_MEDAL"), 12));
            }

            if (Session.Character.StaticBonusList.Any(s => s.StaticBonusType == StaticBonusType.PetBasket))
            {
                Session.SendPacket("ib 1278 1");
            }

            if (Session.Character.MapInstance?.Map?.MapTypes?.Any(m => m.MapTypeId == (short)MapTypeEnum.CleftOfDarkness) == true)
            {
                Session.SendPacket("bc 0 0 0");
            }

            if (specialistInstance != null)
            {
                Session.SendPacket(Session.Character.GenerateSpPoint());
            }

            Session.SendPacket("scr 0 0 0 0 0 0");
            for (int i = 0; i < 10; i++)
            {
                Session.SendPacket($"bn {i} {Language.Instance.GetMessageFromKey($"BN{i}")}");
            }

            Session.SendPacket(Session.Character.GenerateExts());
            Session.SendPacket(Session.Character.GenerateMlinfo());
            Session.SendPacket(UserInterfaceHelper.GeneratePClear());

            Session.SendPacket(Session.Character.GeneratePetskill());

            foreach (var mate in Session.Character.Mates.Where(s => s.IsTeamMember))
            {
                mate.AddTeamMember(true);
            }

            Session.Character.GenerateFmi();
            Session.Character.GenerateFmp();
            Session.SendPacket(Session.Character.GeneratePinit());
            Session.SendPackets(Session.Character.GeneratePst());

            Session.SendPacket("zzim");
            Session.SendPacket(
                $"twk 1 {Session.Character.CharacterId} {Session.Account.Name} {Session.Character.Name} shtmxpdlfeoqkr");

            long? familyId = DAOFactory.FamilyCharacterDAO.LoadByCharacterId(Session.Character.CharacterId)?.FamilyId;
            if (familyId.HasValue)
            {
                Session.Character.Family = ServerManager.Instance.FamilyList[familyId.Value];
            }

            if (Session.Character.Family != null && Session.Character.FamilyCharacter != null)
            {
                if (Session.Character.Faction != (FactionType)Session.Character.Family.FamilyFaction)
                {
                    Session.Character.Faction
                        = (FactionType)Session.Character.Family.FamilyFaction;
                }

                Session.SendPacket(Session.Character.GenerateGInfo());
                Session.SendPackets(Session.Character.GetFamilyHistory());
                Session.SendPacket(Session.Character.GenerateFamilyMember());
                Session.SendPacket(Session.Character.GenerateFamilyMemberMessage());
                Session.SendPacket(Session.Character.GenerateFamilyMemberExp());
                Session.SendPacket($"gcon {Session.Character.CharacterId}|1|0");

                if (!string.IsNullOrWhiteSpace(Session.Character.Family.FamilyMessage))
                {
                    Session.SendPacket(
                        UserInterfaceHelper.GenerateInfo("--- Family Message ---\n" +
                                                         Session.Character.Family.FamilyMessage));
                }
            }

            Session.SendPacket(Session.Character.GetSqst());
            Session.SendPacket("act6");
            Session.SendPacket(Session.Character.GenerateFaction());
            Session.SendPackets(Session.Character.GenerateScP());
            Session.SendPackets(Session.Character.GenerateScN());
#pragma warning disable 618
            Session.Character.GenerateStartupInventory();
#pragma warning restore 618

            Session.SendPacket(Session.Character.GenerateGold());
            Session.SendPackets(Session.Character.GenerateQuicklist());

            string clinit = "clinit";
            string flinit = "flinit";
            string kdlinit = "kdlinit";
            foreach (CharacterDTO character in ServerManager.Instance.TopComplimented)
            {
                clinit +=
                    $" {character.CharacterId}|{character.Level}|{character.HeroLevel}|{character.Compliment}|{character.Name}";
            }

            foreach (CharacterDTO character in ServerManager.Instance.TopReputation)
            {
                flinit +=
                    $" {character.CharacterId}|{character.Level}|{character.HeroLevel}|{character.Reputation}|{character.Name}";
            }

            foreach (CharacterDTO character in ServerManager.Instance.TopPoints)
            {
                kdlinit +=
                    $" {character.CharacterId}|{character.Level}|{character.HeroLevel}|{character.Act4Points}|{character.Name}";
            }

            Session.CurrentMapInstance?.Broadcast(Session.Character.GenerateGidx());

            Session.SendPacket(Session.Character.GenerateFinit());
            Session.SendPacket(Session.Character.GenerateBlinit());
            Session.SendPacket(Session.Character.GenerateFood());
            Session.SendPacket(clinit);
            Session.SendPacket(flinit);
            Session.SendPacket(kdlinit);

            Session.Character.LoadPartnerSkills();

            Session.Character.LastPVPRevive = DateTime.Now;

            IEnumerable<PenaltyLogDTO> warning = DAOFactory.PenaltyLogDAO.LoadByAccount(Session.Character.AccountId)
                .Where(p => p.Penalty == PenaltyType.Warning);
            if (warning.Any())
            {
                Session.SendPacket(UserInterfaceHelper.GenerateInfo(
                    string.Format(Language.Instance.GetMessageFromKey("WARNING_INFO"), warning.Count())));
            }

            // finfo - friends info
            Session.Character.LoadMail();
            Session.Character.LoadSentMail();
            Session.Character.DeleteTimeout();


            if (Session.Character.Quests.Any())
            {
                Session.SendPacket(Session.Character.GenerateQuestsPacket());
            }

            if (Session.Character.IsSeal)
            {
                if (ServerManager.Instance.ChannelId == 51)
                {
                    Session.Character.SetSeal();
                }
                else
                {
                    Session.Character.IsSeal = false;
                }
            }
        }

        /// <summary>
        /// walk packet
        /// </summary>
        /// <param name="walkPacket"></param>
        public void Walk(WalkPacket walkPacket)
        {
            if (Session.Character.CanMove())
            {
                if (Session.Character.MeditationDictionary.Count != 0)
                {
                    Session.Character.MeditationDictionary.Clear();
                }

                if (Session.Character.HasBuff(850))
                {
                    Session.Character.RemoveBuff(850);
                    Session.Character.IsBiting = false;
                    Session.Character.FishingCycle?.Dispose();
                    Session.SendPacket(Session.Character.GenerateSay("Fishing time is over!", 0));
                }

                double currentRunningSeconds =
                    (DateTime.Now - Process.GetCurrentProcess().StartTime.AddSeconds(-50)).TotalSeconds;
                double timeSpanSinceLastPortal = currentRunningSeconds - Session.Character.LastPortal;
                int distance =
                    Map.GetDistance(new MapCell { X = Session.Character.PositionX, Y = Session.Character.PositionY },
                        new MapCell { X = walkPacket.XCoordinate, Y = walkPacket.YCoordinate });

                if (Session.HasCurrentMapInstance
                    && !Session.CurrentMapInstance.Map.IsBlockedZone(walkPacket.XCoordinate, walkPacket.YCoordinate)
                    && !Session.Character.IsChangingMapInstance && !Session.Character.HasShopOpened)
                {
                    Session.Character.PyjamaDead = false;
                    if (!Session.Character.InvisibleGm)
                    {
                        Session.CurrentMapInstance?.Broadcast(StaticPacketHelper.Move(UserType.Player,
                            Session.Character.CharacterId, walkPacket.XCoordinate, walkPacket.YCoordinate,
                            Session.Character.Speed));
                    }
                    Session.SendPacket(Session.Character.GenerateCond());
                    Session.Character.WalkDisposable?.Dispose();
                    walk();
                    int interval = 100 - Session.Character.Speed * 5 + 100 > 0 ? 100 - Session.Character.Speed * 5 + 100 : 0;
                    Session.Character.WalkDisposable = Observable.Interval(TimeSpan.FromMilliseconds(interval)).Subscribe(obs =>
                    {
                        walk();
                    });
                    void walk()
                    {
                        MapCell nextCell = Map.GetNextStep(new MapCell { X = Session.Character.PositionX, Y = Session.Character.PositionY }, new MapCell { X = walkPacket.XCoordinate, Y = walkPacket.YCoordinate }, 1);

                        Session.Character.GetDir(Session.Character.PositionX, Session.Character.PositionY, nextCell.X, nextCell.Y);

                        if (Session.Character.MapInstance.MapInstanceType == MapInstanceType.BaseMapInstance)
                        {
                            Session.Character.MapX = nextCell.X;
                            Session.Character.MapY = nextCell.Y;
                        }

                        Session.Character.PositionX = nextCell.X;
                        Session.Character.PositionY = nextCell.Y;

                        Session.Character.LastMove = DateTime.Now;

                        if (Session.Character.IsVehicled)
                        {
                            Session.Character.Mates.Where(s => s.IsTeamMember || s.IsTemporalMate).ToList().ForEach(s =>
                            {
                                s.PositionX = Session.Character.PositionX;
                                s.PositionY = Session.Character.PositionY;
                            });
                        }

                        if (Session.Character.LastMonsterAggro.AddSeconds(5) > DateTime.Now)
                        {
                            Session.Character.UpdateBushFire();
                        }

                        Session.CurrentMapInstance?.OnAreaEntryEvents
                            ?.Where(s => s.InZone(Session.Character.PositionX, Session.Character.PositionY)).ToList()
                            .ForEach(e => e.Events.ForEach(evt => EventHelper.Instance.RunEvent(evt)));
                        Session.CurrentMapInstance?.OnAreaEntryEvents?.RemoveAll(s =>
                            s.InZone(Session.Character.PositionX, Session.Character.PositionY));

                        Session.CurrentMapInstance?.OnMoveOnMapEvents?.ForEach(e => EventHelper.Instance.RunEvent(e));
                        Session.CurrentMapInstance?.OnMoveOnMapEvents?.RemoveAll(s => s != null);

                        if (Session.Character.PositionX == walkPacket.XCoordinate && Session.Character.PositionY == walkPacket.YCoordinate)
                        {
                            Session.Character.WalkDisposable?.Dispose();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// / packet
        /// </summary>
        /// <param name="whisperPacket"></param>
        public void Whisper(WhisperPacket whisperPacket)
        {
            try
            {
                // TODO: Implement WhisperSupport
                if (string.IsNullOrEmpty(whisperPacket.Message))
                {
                    return;
                }

                string characterName =
                    whisperPacket.Message.Split(' ')[
                            whisperPacket.Message.StartsWith("GM ", StringComparison.CurrentCulture) ? 1 : 0].Replace("[Angel]", "").Replace("[Demon]", "");

                Enum.GetNames(typeof(AuthorityType)).ToList().ForEach(at => characterName = characterName.Replace($"[{at}]", ""));

                string message = "";
                string[] packetsplit = whisperPacket.Message.Split(' ');
                for (int i = packetsplit[0] == "GM" ? 2 : 1; i < packetsplit.Length; i++)
                {
                    message += packetsplit[i] + " ";
                }

                if (message.Length > 60)
                {
                    message = message.Substring(0, 60);
                }

                message = message.Trim();
                Session.SendPacket(Session.Character.GenerateSpk(message, 5));
                CharacterDTO receiver = DAOFactory.CharacterDAO.LoadByName(characterName);
                int? sentChannelId = null;
                if (receiver != null)
                {
                    if (receiver.CharacterId == Session.Character.CharacterId)
                    {
                        return;
                    }

                    if (Session.Character.IsBlockedByCharacter(receiver.CharacterId))
                    {
                        Session.SendPacket(
                            UserInterfaceHelper.GenerateInfo(Language.Instance.GetMessageFromKey("BLACKLIST_BLOCKED")));
                        return;
                    }

                    ClientSession receiverSession =
                        ServerManager.Instance.GetSessionByCharacterId(receiver.CharacterId);
                    if (receiverSession?.CurrentMapInstance?.Map.MapId == Session.CurrentMapInstance?.Map.MapId
                        && Session.Account.Authority >= AuthorityType.GameMaster)
                    {
                        receiverSession?.SendPacket(Session.Character.GenerateSay(message, 2));
                    }

                    sentChannelId = CommunicationServiceClient.Instance.SendMessageToCharacter(new SCSCharacterMessage
                    {
                        DestinationCharacterId = receiver.CharacterId,
                        SourceCharacterId = Session.Character.CharacterId,
                        SourceWorldId = ServerManager.Instance.WorldId,
                        Message = Session.Character.Authority >= AuthorityType.GameMaster
                            ? Session.Character.GenerateSay(
                                $"(whisper)(From {Session.Character.Authority} {Session.Character.Name}):{message}", 11)
                            : Session.Character.GenerateSpk(message,
                                Session.Account.Authority >= AuthorityType.GameMaster ? 15 : 5),
                        Type = Enum.GetNames(typeof(AuthorityType)).Any(a =>
                        {
                            if (a.Equals(packetsplit[0]))
                            {
                                Enum.TryParse(a, out AuthorityType auth);
                                if (auth >= AuthorityType.GameMaster)
                                {
                                    return true;
                                }
                            }
                            return false;
                        })
                        || Session.Account.Authority >= AuthorityType.GameMaster
                        ? MessageType.WhisperGM : MessageType.Whisper
                    });
                }

                if (sentChannelId == null)
                {
                    Session.SendPacket(
                        UserInterfaceHelper.GenerateInfo(Language.Instance.GetMessageFromKey("USER_NOT_CONNECTED")));
                }
            }
            catch (Exception e)
            {
                Logger.Error("Whisper failed.", e);
            }
        }

        /// <summary>
        /// Anti-Cheat Heartbeat Packet
        /// </summary>
        /// <param name="ntcpAcPacket"></param>
        public void NtcpAcPacket(NtcpAcPacket ntcpAcPacket)
        {
            if (Session == null || ntcpAcPacket == null || ntcpAcPacket.Type != 1)
            {
                return;
            }

            #region Packet Manipulation

            if (ntcpAcPacket.TrapValue != 0 || ntcpAcPacket.Crc32?.Length != 8)
            {
                Session.BanForCheatUsing(1);
                return;
            }

            byte[] data = Convert.FromBase64String(ntcpAcPacket.Data);
            byte[] encryptedKey = Convert.FromBase64String(ntcpAcPacket.EncryptedKey);

            if (!AntiCheatHelper.IsValidSignature(ntcpAcPacket.Signature, data, encryptedKey, ntcpAcPacket.Crc32)
                || data.Length != encryptedKey.Length)
            {
                Session.BanForCheatUsing(2);
                return;
            }

            #endregion Packet Manipulation

            #region Crc32

            if (string.IsNullOrEmpty(Session.Crc32))
            {
                Session.Crc32 = ntcpAcPacket.Crc32;
            }
            else if (ntcpAcPacket.Crc32 != Session.Crc32)
            {
                Session.BanForCheatUsing(3);
                return;
            }

            #endregion Crc32

            #region Data

            if (Encoding.UTF8.GetString(data) != AntiCheatHelper.Encrypt(Encoding.UTF8.GetBytes(Session.LastAntiCheatData), encryptedKey))
            {
                Session.BanForCheatUsing(4);
                return;
            }

            #endregion Data

            #region IsAntiCheatAlive

            Session.IsAntiCheatAlive = true;

            #endregion IsAntiCheatAlive
        }

        #endregion Methods
    }
}