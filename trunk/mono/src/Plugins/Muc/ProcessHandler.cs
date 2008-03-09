/* * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * *
 * Pako Jabber-bot. Bbodio's Lab.                                                *
 * Copyright. All rights reserved � 2007-2008 by Klichuk Bogdan (Bbodio's Lab)   *
 * Contact information is here: http://code.google.com/p/pako                    *
 *                                                                               *
 * Pako is under GNU GPL v3 license:                                             *
 * YOU CAN SHARE THIS SOFTWARE WITH YOUR FRIEND, MAKE CHANGES, REDISTRIBUTE,     *
 * CHANGE THE SOFTWARE TO SUIT YOUR NEEDS, THE GNU GENERAL PUBLIC LICENSE IS     *
 * FREE, COPYLEFT LICENSE FOR SOFTWARE AND OTHER KINDS OF WORKS.                 *
 *                                                                               *
 * Visit http://www.gnu.org/licenses/gpl.html for more information about         *
 * GNU General Public License v3 license                                         *
 *                                                                               *
 * Download source code: http://pako.googlecode.com/svn/trunk                    *
 * See the general information here:                                             *
 * http://code.google.com/p/pako.                                                *
 * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * */

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using agsXMPP;
using agsXMPP.protocol.client;
using agsXMPP.Xml.Dom;
using System.Threading;
using System.IO;
using Core.Kernel;
using Core.Other;
using Core.Conference;
using Core.Xml;
using agsXMPP.protocol.x.muc;
using agsXMPP.protocol.x.muc.iq.admin;
using agsXMPP.protocol.x.muc.iq.owner;
using Mono.Data.SqliteClient;


namespace Plugin
{
    public class MucHandler
    {
        string[] ws;
        Message m_msg;
        Response m_r;
        string self;
        bool syntax_error = false;
        Jid s_jid;
        string s_nick;
        string d;
        string n;
        string m_b;
        SessionHandler Sh;

        public MucHandler(Response r, string Name)
        {
           
            Sh = r.Sh;

            m_b = r.Msg.Body;
            ws = Utils.SplitEx(m_b, 2);
            m_msg = r.Msg;
            m_r = r;
            s_jid = r.Msg.From;
            s_nick = r.Msg.From.Resource;
            d = r.Delimiter;
            n = Name;
         
            if (ws.Length < 2)
            {
                r.Reply(r.f("volume_info", n, d + n.ToLower() + " list"));
                return;
            }

            if ((ws[1] != "join") && (ws[1] != "leave"))
            if (r.MUser == null)
            {
                r.Reply(r.f("muconly"));
                return;
            }

            self = ws[0] + " " + ws[1];
            Handle();
        }




 

        public void Handle()
        {


       
            int myaccess = m_r.MUC.GetUser(m_r.MUC.MyNick).Access;
            string cmd = ws[1];
            string rs = null;




            switch (cmd)
            {
                case "cmdaccess":
                    {
                        string _cmd = Utils.GetValue(m_b, "[(.*)]");
                        m_b = Utils.RemoveValue(m_b, "[(.*)]", true);
                        ws = Utils.SplitEx(m_b, 3);
                        if ((ws.Length > 2) && (_cmd != ""))
                        {
                            try
                            {
                                Sh.S.GetMUC(m_r.MUC.Jid).AccessManager.SetAccess(_cmd, Convert.ToInt32(ws[2]));
                                rs = m_r.Agree();
                            }
                            catch
                            {
                                syntax_error = true;
                            }
                        }
                        else
                        {
                            int access;

                            if (m_r.MUC != null)
                            {
                                access = Sh.S.GetMUC(m_r.MUC.Jid).AccessManager.GetAccess(_cmd);

                                if (access == -1)
                                {
                                    string a = "", b = "";
                                    _cmd = m_r.MUC.GetAlias(_cmd, ref a, ref b);
                                    access = Sh.S.AccessManager.GetAccess(_cmd);
                                }

                            }
                            else
                                access = Sh.S.AccessManager.GetAccess(_cmd);
                            rs = access.ToString();
                        }
                        break;
                    }

                case "moderator":
                    {
                        if (ws.Length > 2)
                        {
                            if (myaccess >= 70)
                            {

                                string reason = null;
                                if (m_b.IndexOf(" ||") > -1)
                                {
                                    reason = m_b.Substring(m_b.IndexOf(" ||") + 3).Trim() == "" ? null : m_b.Substring(m_b.IndexOf(" ||") + 3).Trim();

                                    if (reason != null)
                                        m_b = m_b.Remove(m_b.IndexOf(" ||"));
                                }
                                if (reason == null)
                                    reason = m_r.f("moderator_reason");
                                ws = Utils.SplitEx(m_b.Trim(), 2);
                                if (m_r.MUC.UserExists(ws[2]))
                                {
                                    m_r.MUC.Moderator(ws[2], reason);
                                    rs = m_r.Agree();
                                }
                                else
                                    rs = m_r.f("user_not_found", ws[2]);
                            }
                            else
                                rs = m_r.Deny();
                        }
                        else
                            syntax_error = true;
                        break;
                    }

                case "kick":
                    {
                        if (ws.Length > 2)
                        {
                            string reason = null;
                            if (m_b.IndexOf(" ||") > -1)
                            {
                                reason = m_b.Substring(m_b.IndexOf(" ||") + 3).Trim() == "" ? null : m_b.Substring(m_b.IndexOf(" ||") + 3).Trim();
                    
                                if (reason != null)
                               m_b = m_b.Remove(m_b.IndexOf(" ||"));
                
                            }
                           if (reason == null)
                               reason = m_r.f("kick_reason");
                            ws = Utils.SplitEx(m_b, 2);
                            switch (m_r.MUC.Kick(ws[2].Trim(), reason))
                            {
                                case ActionResult.Done:
                                    rs = m_r.Agree();
                                    break;
                                case ActionResult.NotAble:
                                    rs = m_r.Deny();
                                    break;
                                case ActionResult.UserNotFound:
                                    rs = m_r.f("user_not_found", ws[2]);
                                    break;
                            }

                        }
                        else
                            syntax_error = true;
                        break;
                    }

                case "akick":
                    {
                        ws = Utils.SplitEx(m_b.Trim(), 3);
                        if (ws.Length > 2)
                        {
                            if (ws[2].Trim() == "list")
                            {
                                string data = Sh.S.Tempdb.GetAutoKickList(m_r.MUC.Jid,"{1}) {3}      ({2}, {4}, '{5}')", m_r);
                                m_r.Reply(data != null ? data : m_r.f("akick_list_empty"));
                                return;
                            }
                            else
                                if (ws[2].Trim() == "clear")
                                {
                                    Sh.S.Tempdb.ClearAutoKick(m_r.MUC.Jid);
                                    m_r.Reply(m_r.f("akick_list_cleared"));
                                    return;
                                }
                                else
                                if (ws[2].Trim() == "del")
                                {
                                    if (ws.Length == 4)
                                    {
                                        int num = 0;
                                        try
                                        {
                                            num = Convert.ToInt32(ws[3]);
                                        }
                                        catch
                                        {
                                            syntax_error = true;
                                            break;
                                        }
                                        if (Sh.S.Tempdb.DelAutoKick(m_r.MUC.Jid, num))
                                            rs = m_r.Agree();
                                        else
                                            rs = m_r.f("akick_not_existing");
                                    }
                                    else
                                    {
                                        syntax_error = true;
                                        break;
                                    }
                                }
                                else
                                {
                                    string reason = null;
                                    // *muc akick $15h|jid-exp ^[0-9]+\@\s*$ || bad jid..
                                    if (m_b.IndexOf(" ||") > -1)
                                    {
                                        reason = m_b.Substring(m_b.IndexOf(" ||") + 3).Trim() == "" ? null : m_b.Substring(m_b.IndexOf(" ||") + 3).Trim();

                                        if (reason != null)
                                            m_b = m_b.Remove(m_b.IndexOf(" ||"));

                                    }
                                    if (reason == null)
                                        reason = m_r.f("kick_reason");
                                    string determiner = "AKICK_JID";
                                    string value = "";
                                    long ticks = 0;
                                    ws = Utils.SplitEx(m_b.Trim(), 4);
                                    if (ws.Length == 3)
                                    {
                                        
                                            determiner = "AKICK_JID";
                                            MUser user = m_r.MUC.GetUser(ws[2]);
                                            if (user != null)
                                                if (user.Jid.Bare != m_r.Msg.From.Bare)
                                                    value = user.Jid.Bare;
                                                else
                                                    value = ws[2].ToLower();
                                            else
                                                value = ws[2].ToLower();
                                        
                                        if (value.IndexOf("@") == -1)
                                        {
                                            m_r.Reply(m_r.f("jid_not_valid"));
                                            return;
                                        }
                                    }
                                    if (ws.Length > 3)
                                    {
                                        value = ws[ws.Length - 1].Trim();
                                        string[] ps = ws[2].Split('|');
                                        bool time_span_setted = false;
                                        bool type_setted = false;
                                        for (int i = 2; i < ws.Length - 1; i++)
                                        {

                                            string p = ws[i];
                                            if (p.StartsWith("$"))
                                            {
                                                if (time_span_setted == false)
                                                {
                                                    time_span_setted = true;
                                                    if (p != "$&")
                                                    {
                                                        Regex reg = new Regex("[0-9]+[s|m|h|d|M]");
                                                        if (!reg.IsMatch(p))
                                                        {
                                                            m_r.Reply(m_r.f("akick_wrong_parameter"));
                                                            return;
                                                        }
                                                        foreach (Match m in reg.Matches(p))
                                                        {
                                                            string full = m.ToString().Trim();
                                                            string time_span = full.Substring(0, full.Length - 1);
                                                            long _time = Convert.ToInt64(time_span);
                                                            char type = full[full.Length - 1];
                                                            switch (type)
                                                            {
                                                                case 's':
                                                                    ticks += _time * 10000000;
                                                                    break;
                                                                case 'm':
                                                                    ticks = ticks + (_time * 600000000);
                                                                    break;
                                                                case 'h':
                                                                    ticks += _time * 36000000000;
                                                                    break;
                                                                case 'd':
                                                                    ticks += _time * 864000000000;
                                                                    break;
                                                                case 'M':
                                                                    ticks += _time * 25920000000000;
                                                                    break;
                                                                default:
                                                                    m_r.Reply(m_r.f("akick_wrong_parameter"));
                                                                    return;
                                                            }
                                                        }

                                                    }
                                                }
                                                else
                                                {
                                                    m_r.Reply(m_r.f("akick_wrong_parameter"));
                                                    return;


                                                }
                                            }
                                            else
                                            {
                                                string _det = p;
                                                if (type_setted == false)
                                                {
                                                    type_setted = true;
                                                    switch (_det)
                                                    {
                                                        case "jid-exp":
                                                            determiner = "AKICK_JID_REGEXP";
                                                            break;
                                                        case "nick-exp":
                                                            determiner = "AKICK_NICK_REGEXP";
                                                            break;
                                                        case "jid":
                                                            determiner = "AKICK_JID";
                                                            break;
                                                        default:
                                                            m_r.Reply(m_r.f("akick_wrong_parameter"));
                                                            return;

                                                    }
                                                }
                                                else
                                                {
                                                    m_r.Reply(m_r.f("akick_wrong_parameter"));
                                                    return;
                                                }
                                            }
                                        }


                                    }

                                    if (Sh.S.Tempdb.AddAutoKick(value, m_r.MUC.Jid, determiner, reason, ticks))
                                    {
                                        rs = m_r.Agree();
                                    }
                                    else
                                        rs = m_r.Deny();



                                    foreach (MUser user in m_r.MUC.Users.Values)
                                    {
                                        string ak = Sh.S.Tempdb.IsAutoKick(user.Jid, user.Nick, m_r.MUC.Jid, Sh);
                                        if (ak != null)
                                            if (m_r.MUC.KickableForCensored(user))
                                                m_r.MUC.Kick(user.Nick, ak);
                                    }




                                }

                        }
                        else
                            syntax_error = true;
                        break;
                    }
                case "setsubject":
                    {
                        if (myaccess >= 50)
                        {
                            if (ws.Length > 2)
                            {
                                m_r.MUC.ChangeSubject(ws[2]);
                                rs = m_r.Agree();
                            }
                            else
                                rs = m_r.MUC.Subject;
                        }
                        else
                            rs = m_r.Deny();
                        break;
                    }

                case "nicks":
                    {
                        string data = "\n";
                        foreach (string nick in m_r.MUC.Users.Keys)
                        {
                            data += nick + ", ";
                        }

                        rs = m_r.f("nicks_list") + data + "\n-- " + m_r.MUC.Users.Count.ToString() + " --";

                        break;
                    }

                case "censor":
                    {
                        ws = Utils.SplitEx(m_b, 3);
                        if ((ws.Length > 2))
                        {
                            string reason = m_r.f("kick_censored_reason");
                            if (ws.Length == 4)
                                reason = ws[3];
                            m_r.MUC.AddRoomCensor(ws[2],reason);
                            rs = m_r.Agree();
                        }
                        else
                            syntax_error = true;
                        break;
                    }
                case "allcensor":
                    {

                        if ((ws.Length == 2))
                        {
                            string data = m_r.MUC.GetRoomCensorList("{1}) {2} => \"{3}\"");
                            rs = data != null ? data : m_r.f("censor_list_empty");
                        }
                        else
                            syntax_error = true;
                        break;
                    }

                case "uncensor":
                    {

                        if ((ws.Length > 2))
                        {
                            if (m_r.MUC.DelRoomCensor(ws[2]))
                                rs = m_r.Agree();
                            else
                                rs = m_r.Deny();
                        }
                        else
                            syntax_error = true;
                        break;
                    }


                case "tryme":
                    {
                        if (myaccess >= 50)
                        {
                            if (m_r.Access < myaccess)
                            {
                                Random rand = new Random();
                                switch (rand.Next(2))
                                {
                                    case 0:
                                        {
                                            rs = m_r.f("tryme_fail");
                                            break;
                                        }
                                    default:
                                        {
                                            string reason = null;
                                            if (m_b.IndexOf(" ||") > -1)
                                            {
                                                reason = m_b.Substring(m_b.IndexOf(" ||")+3).Trim() == "" ? null : m_b.Substring(m_b.IndexOf(" ||") + 3).Trim();

                                                if (reason != null)
                                                    m_b = m_b.Remove(m_b.IndexOf(" ||"));

                                            }
                                            if (reason == null)
                                                reason = m_r.f("tryme_reason");
                                            ws = Utils.SplitEx(m_b.Trim(), 2);
                                            m_r.MUC.Kick(m_r.MUser.Nick, reason);
                                            Message msg = new Message();
                                            msg.To = new Jid(m_r.Msg.From.Bare);
                                            msg.Type = MessageType.groupchat;
                                            msg.Body = m_r.f("tryme_done");
                                            m_r.Connection.Send(msg);
                                            break;
                                        }
                                }

                            }
                            else
                                rs = m_r.f("tryme_fail");
                        }
                        else
                            rs = m_r.f("tryme_fail");
                        break;
                    }


                case "me":
                    {
                        rs = m_r.f("self");
                        break;
                    }

                case "mylang":
                    {

                        if (ws.Length > 2)
                        {
                            string lang = ws[2].Trim();
                            if (Sh.S.Rg.GetResponse(lang) != null)
                            {
                                if (Sh.S.AutoMucManager.SetLanguage(m_r.MUC.Jid, lang))
                                {
                                    Sh.S.GetMUC(m_r.MUC.Jid).Language = lang;
                                    m_r.Document = Sh.S.Rg.GetResponse(lang).Document;
                                    m_r.Language = lang;
                                    rs = m_r.Agree();
                                }
                                else
                                    syntax_error = true;
                            }
                            else
                                rs = m_r.f("lang_pack_not_found", lang);

                        }
                        else
                            rs = m_r.MUC.Language;

                        break;
                    }

                case "mystatus":
                    {

                        if (ws.Length > 2)
                        {
                            string status = ws[2].Trim();
                            if (Sh.S.AutoMucManager.SetStatus(m_r.MUC.Jid, status))
                            {
                                Presence pres = new Presence();
                                pres.To = m_r.MUC.Jid;
                                pres.Status = status;
                                pres.Show = m_r.MUC.MyShow;
                                m_r.Connection.Send(pres);
                                rs = m_r.Agree();
                            }
                            else
                                syntax_error = true;

                        }
                        else
                            rs = m_r.MUC.MyStatus;

                        break;
                    }


                case "mynick":
                    {
                        if (ws.Length > 2)
                        {
                            string nick = ws[2].Trim();
                            if (Sh.S.AutoMucManager.SetNick(m_r.MUC.Jid, nick))
                            {
                                Sh.S.GetMUC(m_r.MUC.Jid).ChangeNick(ws[2]);
                                rs = m_r.Agree();
                            }
                            else
                                syntax_error = true;

                        }
                        else
                            rs = m_r.MUC.MyNick;
                        break;
                    }


                case "subject":
                    {
                        if (ws.Length == 2)
                        {
                            if (m_r.MUC.Subject != null)
                                rs = m_r.MUC.Subject;
                            else
                                rs = m_r.f("muc_no_subject");
                        }
                        else
                            syntax_error = true;
                        break;

                    }

                case "show":
                    {

                        string data = "";
                        foreach (Jid j in Sh.S.MUCs.Keys)
                        {
                            data += "\n" + j.ToString() + ",";
                        }

                        rs = m_r.f("mucs_list") + data + "\n-- " + Sh.S.MUCs.Count.ToString() + " --";

                        break;
                    }

                case "join":
                    {

                        ws = Utils.SplitEx(m_b, 3);

                        if (ws.Length == 3)
                        {
                            if (!Sh.S.AutoMucManager.Exists(new Jid(ws[2].Trim())))
                            {
                                MUC m = new MUC(Sh.S.C, new Jid(ws[2].Trim()), Sh.S.Config.Nick, Sh.S.Config.Status, Sh.S.Config.Language, ShowType.NONE, Sh.S.Censor.SQLiteConnection);
                                Sh.S.MUCs.Add(new Jid(ws[2].Trim()), m);
                                MucActivityController mac = new MucActivityController(m_r, m);
                                return;
                            }
                            else
                                rs = m_r.f("muc_already_in");

                        }
                        else
                            if (ws.Length == 4)
                            {
                                if (!Sh.S.AutoMucManager.Exists(new Jid(ws[2].Trim())))
                                {
                                    MUC m = new MUC(Sh.S.C, new Jid(ws[2].Trim()), ws[3].Trim(), Sh.S.Config.Status, Sh.S.Config.Language, ShowType.NONE, Sh.S.Censor.SQLiteConnection);
                                    Sh.S.MUCs.Add(new Jid(ws[2].Trim()), m);
                                    MucActivityController mac = new MucActivityController(m_r, m);
                                    return;
                                   
                                }
                                else
                                    rs = m_r.f("muc_already_in");

                            }
                            else
                                syntax_error = true;
                        break;
                    }

                case "leave":
                    {
                        if (ws.Length > 2)
                        {
                            Jid room = new Jid(ws[2].Trim());

                            if (Sh.S.AutoMucManager.Exists(room))
                            {


                                    m_r.Reply(m_r.Agree());
                                    if (Sh.S.GetMUC(room) != null)
                                    {
                                        Message msg = new Message();
                                        msg.To = room;
                                        msg.Body = m_r.f("muc_leave");
                                        msg.Type = MessageType.groupchat;
                                        m_r.Connection.Send(msg);
                                        Presence pr = new Presence();
                                        pr.To = room;
                                        pr.Type = PresenceType.unavailable;
                                        m_r.Connection.Send(pr);
                                    }

                                    Sh.S.AutoMucManager.DelMuc(room);
                                   
                            }
                            else
                                rs = m_r.f("muc_not_in");
                        }
                        else
                            syntax_error = true;
                        break;
                    }




                case "tell":
                    {
                        string word = Utils.GetValue(m_b, "[(.*)]").Trim();
                        m_b = Utils.RemoveValue(m_b, "[(.*)]", true);
                        ws = Utils.SplitEx(m_b, 2);


                        if ((ws.Length > 2) && (word != ""))
                        {
                            Jid sender = m_r.Msg.From;
                            Jid tell_jid = new Jid(m_r.MUC.Jid.Bare + "/" + word);


                            if (m_r.Msg.From.ToString() == tell_jid.ToString())
                            {
                                rs = m_r.f("tell_fail_yourself");
                                break;
                            }

                            if (m_r.MUC.UserExists(word))
                            {

                                rs = m_r.f("tell_fail_nick_here", word);
                                break;
                            }

                            if (Sh.S.Tempdb.AddTell(tell_jid, Utils.FormatEnvironmentVariables(ws[2], m_r.MUC, m_r.MUser), sender))
                                rs = m_r.Agree();
                            else
                                rs = m_r.f("tell_count_overflow");

                        }
                        else
                            syntax_error = true;
                        break;
                    }


                case "ban":
                    {
                        if (myaccess >= 70)
                        {
                            if (ws.Length > 2)
                            {
                                string reason = null;
                                if (m_b.IndexOf(" ||") > -1)
                                {
                                    reason = m_b.Substring(m_b.IndexOf(" ||") + 3).Trim() == "" ? null : m_b.Substring(m_b.IndexOf(" ||") + 3).Trim();

                                    if (reason != null)
                                        m_b = m_b.Remove(m_b.IndexOf(" ||"));

                                }
                                if (reason == null)
                                    reason = m_r.f("ban_reason");
                                ws = Utils.SplitEx(m_b.Trim(), 2);
                                m_r.MUC.Ban(ws[2], reason);
                                rs = m_r.Agree();
                            }
                            else
                                syntax_error = true;
                        }

                        else
                            rs = m_r.Deny();
                        break;
                    }

                case "admin":
                    {
                        if (myaccess >= 80)
                        {
                            if (ws.Length > 2)
                            {
                                string reason = null;
                                if (m_b.IndexOf(" ||") > -1)
                                {
                                    reason = m_b.Substring(m_b.IndexOf(" ||") + 3).Trim() == "" ? null : m_b.Substring(m_b.IndexOf(" ||") + 3).Trim();

                                    if (reason != null)
                                        m_b = m_b.Remove(m_b.IndexOf(" ||"));

                                }
                                if (reason == null)
                                    reason = m_r.f("admin_reason");
                                ws = Utils.SplitEx(m_b.Trim(), 2);
                                m_r.MUC.Admin(ws[2], reason);
                                rs = m_r.Agree();
                            }
                            else
                                syntax_error = true;
                        }
                        else
                            rs = m_r.Deny();
                        break;
                    }

                case "owner":
                    {
                        if (myaccess >= 80)
                        {
                            if (ws.Length > 2)
                            {
                                string reason = null;
                                if (m_b.IndexOf(" ||") > -1)
                                {
                                    reason = m_b.Substring(m_b.IndexOf(" ||") + 3).Trim() == "" ? null : m_b.Substring(m_b.IndexOf(" ||") + 3).Trim();

                                    if (reason != null)
                                        m_b = m_b.Remove(m_b.IndexOf(" ||"));

                                }
                                if (reason == null)
                                    reason = m_r.f("owner_reason");
                                ws = Utils.SplitEx(m_b.Trim(), 2);
                                m_r.MUC.Owner(ws[2], reason);
                                rs = m_r.Agree();
                            }
                            else
                                syntax_error = true;
                        }
                        else
                            rs = m_r.Deny();
                        break;
                    }

                case "none":
                    {
                        if (myaccess >= 60)
                        {
                            if (ws.Length > 2)
                            {
                                m_r.MUC.Participant(ws[2]);
                                rs = m_r.Agree();
                            }
                            else
                                syntax_error = true;
                        }
                        else
                            rs = m_r.Deny();
                        break;
                    }
                case "clean":
                    {
                        int number = 20;
                        rs = m_r.f("clean_up_done");
                        if (ws.Length > 2)
                        {
                            try
                            {
                                number = Convert.ToInt32(ws[2]);

                            }
                            catch
                            {
                                syntax_error = true;
                                break;
                            }
                        }

                        for (int i = 0; i < number; i++)
                        {
                            Message clm = new Message();
                            clm.To = m_r.MUC.Jid;
                            clm.Type = MessageType.groupchat;
                            clm.Body = "~";
                            m_r.Connection.Send(clm);
                            Thread.Sleep(1500);

                        }
                        break;

                    }
                case "member":
                    {
                        if (myaccess >= 70)
                        {
                            if (ws.Length > 2)
                            {

                                    m_r.MUC.MemberShip(ws[2]);
                                    rs = m_r.Agree();
                            }
                            else
                                syntax_error = true;

                        }
                        else
                            rs = m_r.Deny();
                        break;
                    }

                case "voice":
                    {
                        if (myaccess >= 50)
                        {
                            if (ws.Length > 2)
                            {
                                if (m_r.MUC.UserExists(ws[2]))
                                {
                                    string reason = null;
                                    if (m_b.IndexOf(" ||") > -1)
                                    {
                                        reason = m_b.Substring(m_b.IndexOf(" ||") + 3).Trim() == "" ? null : m_b.Substring(m_b.IndexOf(" ||") + 3).Trim();

                                        if (reason != null)
                                            m_b = m_b.Remove(m_b.IndexOf(" ||"));

                                    }
                                    if (reason == null)
                                        reason = m_r.f("voice_reason");
                                    ws = Utils.SplitEx(m_b.Trim(), 2);
                                    m_r.MUC.Voice(ws[2], reason);
                                    rs = m_r.Agree();
                                }
                                else
                                {
                                    rs = m_r.f("user_not_found", ws[2]);
                                    break;
                                }

                            }
                            else
                                syntax_error = true;
                        }
                        else
                            rs = m_r.Deny();
                        break;
                    }


                case "devoice":
                    {
                        if (myaccess >= 50)
                        {
                            if (ws.Length > 2)
                            {
                                if (m_r.MUC.UserExists(ws[2]))
                                {
                                    string reason = null;
                                    if (m_b.IndexOf(" ||") > -1)
                                    {
                                        reason = m_b.Substring(m_b.IndexOf(" ||") + 3).Trim() == "" ? null : m_b.Substring(m_b.IndexOf(" ||") + 3).Trim();

                                        if (reason != null)
                                            m_b = m_b.Remove(m_b.IndexOf(" ||"));

                                    }
                                    if (reason == null)
                                        reason = m_r.f("devoice_reason");
                                    ws = Utils.SplitEx(m_b.Trim(), 2);
                                    m_r.MUC.Devoice(ws[2], reason);
                                    rs = m_r.Agree();
                                }
                                else
                                {
                                    rs = m_r.f("user_not_found", ws[2]);
                                    break;
                                }
                            }
                            else
                                syntax_error = true;
                        }
                        else
                            rs = m_r.Deny();
                        break;
                    }

                case "name":
                    {
                        if (ws.Length == 2)
                            rs = m_r.MUC.Name;
                        else
                            syntax_error = true;
                        break;
                    }

                case "jid":
                    {

                        MUser m_user = null;
                        if (ws.Length > 2)
                        {
                            if (m_r.MUC.UserExists(ws[2]))
                            {
                                m_user = m_r.MUC.GetUser(ws[2]);
                            }
                            else
                            {
                                rs = m_r.f("user_not_found", ws[2]);
                                break;
                            }
                        }
                        else
                            m_user = m_r.MUser;

                        if (m_user != null)
                        {
                            m_r.Reply(m_r.f("private_notify"));
                            m_r.Msg.Type = MessageType.chat;
                            rs = m_r.f("real_jid", m_user.Nick, m_user.Jid.ToString());
                        }
                        break;
                    }

                case "echo":
                    {
                        if (ws.Length > 2)
                        {
                            Message msg = new Message();
                            msg.To = m_r.MUC.Jid;
                            msg.Type = MessageType.groupchat;
                            msg.Body = Utils.FormatEnvironmentVariables(ws[2], m_r.MUC,m_r.MUser);
                            m_r.Connection.Send(msg);
                            if (m_msg.Type == MessageType.chat)
                                rs = m_r.Agree();
                        }
                        else
                            syntax_error = true;
                        break;
                    }

                case "status":
                    {
                        MUser m_user = null;
                        if (ws.Length > 2)
                        {
                            if (m_r.MUC.UserExists(ws[2]))
                            {
                                m_user = m_r.MUC.GetUser(ws[2]);
                            }
                            else
                            {
                                rs = m_r.f("user_not_found", ws[2]);
                                break;
                            }

                        }
                        else
                            m_user = m_r.MUser;


                        rs = m_user.Status;
                        break;
                    }

                case "entered":
                    {
                        MUser m_user = null;
                        if (ws.Length > 2)
                        {
                            if (m_r.MUC.UserExists(ws[2]))
                            {
                                m_user = m_r.MUC.GetUser(ws[2]);
                            }
                            else
                            {
                                rs = m_r.f("user_not_found", ws[2]);
                                break;
                            }

                        }
                        else
                            m_user = m_r.MUser;


                        rs = m_user.EnterTime;
                        break;
                    }


                case "role":
                    {
                        MUser m_user = null;
                        if (ws.Length > 2)
                        {
                            if (m_r.MUC.UserExists(ws[2]))
                            {
                                m_user = m_r.MUC.GetUser(ws[2]);
                            }
                            else
                            {
                                rs = m_r.f("user_not_found", ws[2]);
                                break;
                            }
                        }
                        else
                            m_user = m_r.MUser;
                        rs = m_user.Affiliation + "/" + m_user.Role;
                        break;
                    }

                case "disco":
                    {
                        if (ws.Length > 2)
                        {
                            Jid Room = new Jid(ws[2]);
                            if (Room.ToString().IndexOf("@") < 0)
                            {
                                syntax_error = true;
                                break;
                            }
                            DiscoCB vcb = new DiscoCB(m_r, Room);
                        }
                        else
                        {
                            syntax_error = true;
                            break;
                        }
                      
                        return;
                    }

                case "info":
                    {
                        MUser m_user = null;
                        if (ws.Length > 2)
                        {
                            if (m_r.MUC.UserExists(ws[2]))
                            {
                                m_user = m_r.MUC.GetUser(ws[2]);
                            }
                            else
                            {
                                rs = m_r.f("user_not_found", ws[2]);
                                break;
                            }
                        }
                        else
                            m_user = m_r.MUser;
                        if (m_user != null)
                        {
                            Vipuser ud = Sh.S.VipManager.GetUserData(m_user.Jid);
                            bool ud_lang = ud != null;
                            if (ud_lang)
                                ud_lang = ud.Language != null;
                            string lng = ud_lang ?
                                               ud.Language :
                                               m_user.Language;

                            MUser user = m_r.MUser;
                            Message msg = new Message();
                            msg.From = m_user.Jid;
                            msg.To = m_msg.To;
                            msg.Body = m_msg.Body;
                            msg.Type = m_msg.Type;

                            string ShowJid = m_user.Jid.ToString();
                            if (m_r.Msg.Type == MessageType.groupchat)
                                ShowJid = "???";
                            else
                            if (m_r.Access <= 50)
                                    ShowJid = "???";

                            int access = Sh.S.GetAccess(msg, m_user);
                            rs = m_r.f("muc_user_info",
                                   m_user.Nick,
                                   m_user.Affiliation + "/" + m_user.Role,
                                   lng,
                                   access.ToString(),
                                   m_user.Show + " (" + m_user.Status + ")",
                                   m_user.EnterTime,
                                   ShowJid);
                        }
                        break;
                    }

                case "list":
                    {
                        if (ws.Length == 2)
                        {
                            rs = m_r.f("volume_list", n) + "\nlist, mynick, kick, ban, moderator, admin, tell, owner, tryme, member, voice, devoice, name,  status, role, entered, me, info, nicks, subject, censor, uncensor, allcensor, setsubject, join, leave, mystatus, mylang, show , disco";
                        }
                        break;
                    }

                default:
                    {
                        rs = m_r.f("volume_cmd_not_found", n, ws[1], d + n.ToLower() + " list");
                        break;
                    }

            }

            if (syntax_error)
                m_r.se(self);
            else

            if (rs != null)
                m_r.Reply(rs);
               
        }

    }
}
