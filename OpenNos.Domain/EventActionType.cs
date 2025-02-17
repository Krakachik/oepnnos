﻿/*
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

namespace OpenNos.Domain
{
    public enum EventActionType
    {
        CHANGEXPRATE,
        CHANGEDROPRATE,
        CLEARMAPMONSTERS,
        SENDPACKET,
        DISPOSEMAP,
        UNSPAWNMONSTERS,
        SPAWNONLASTENTRY,
        CLOCK,
        SPAWNMONSTER,
        SPAWNMONSTERS,
        GIVEITEMS,
        DROPITEMS,
        NPCDIALOG,
        STARTCLOCK,
        SPAWNPORTAL,
        CHANGEPORTALTYPE,
        REFRESHMAPITEMS,
        STOPMAPWAVES,
        NPCSEFFECTCHANGESTATE,
        SPAWNBUTTON,
        STARTMAPCLOCK,
        STOPMAPCLOCK,
        STOPCLOCK,
        MAPCLOCK,
        ADDCLOCKTIME,
        ADDMAPCLOCKTIME,
        REGISTEREVENT,
        SPAWNNPC,
        SPAWNNPCS,
        SCRIPTEND,
        REMOVEBUTTONLOCKER,
        REMOVEMONSTERLOCKER,
        REFRESHRAIDGOAL,
        TELEPORT,
        THROWITEMS,
        STARTACT4RAIDWAVES,
        SETMONSTERLOCKERS,
        SETBUTTONLOCKERS,
        SETAREAENTRY,
        REGISTERWAVE,
        EFFECT,
        MOVE,
        ONTARGET,
        CONTROLEMONSTERINRANGE,
        SENDINSTANTBATTLEWAVEREWARDS,
        ONSTOP,
        ONTIMEOUT,
        REMOVEAFTER,
        REMOVELAURENABUFF,
    }
}