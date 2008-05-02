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
using System.Collections.Specialized;
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

            if (r.MUser == null)
            {
                r.Reply(r.f("muconly"));
                return;
            }


            if (ws.Length < 2)
            {
                r.Reply(r.f("volume_info", n, d + n.ToLower() + " list"));
                return;
            }


            self = ws[0] + " " + ws[1];
            Handle();
        }






        public void Handle()
        {



            int? myaccess = m_r.MUC.GetUser(m_r.MUC.MyNick).Access;
            string cmd = ws[1];
            string rs = null;




            switch (cmd)
            {
                case "cmdaccess":
                    {

                        string _cmd = Utils.GetValue(m_b, "[(.*)]").Trim();
                        m_b = Utils.RemoveValue(m_b, "[(.*)]", true);
                        ws = Utils.SplitEx(m_b, 3);

                        if (ws.Length > 2)
                        {
                            if (ws[2] == "show")
                            {
                                string acc = m_r.f("access");
                                rs = "\n[========" + acc + "========]";
                                ListDictionary list = Sh.S.GetMUC(m_r.MUC.Jid).AccessManager.GetCommands();
                                foreach (string cm in list.Keys)
                                {
                                    string _access = ((int)list[cm]).ToString();
                                    rs += "\n" + "[" + cm + "]";
                                    for (int i = 1; i <= 28 - (2 + cm.Length) - _access.Length; i++)
                                        rs += i == 1 || i == 28 - (2 + cm.Length) - _access.Length ? " " : i % 2 == 1 ? " " : ".";
                                    rs += _access;
                                }
                                rs = list.Count == 0 ? m_r.f("access_list_empty") : rs + "\n[=================================]";
                                break;
                            }
                            else
                                if (ws[2] == "del")
                                {

                                    if (ws.Length == 3 && _cmd != "")
                                    {
                                        Sh.S.GetMUC(m_r.MUC.Jid).AccessManager.DelCommand(_cmd);
                                        rs = m_r.Agree();
                                    }
                                    else
                                        syntax_error = true;
                                    break;
                                }
                                else
                                {

                                    if (_cmd != "")
                                    {

                                        if (m_r.Access < 100 && Utils.SplitEx(_cmd, 2)[0].ToLower() == "admin")
                                        {
                                            rs = m_r.f("admin_volume_alias_restricted");
                                            break;
                                        }

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
                                        syntax_error = true;
                                }


                        }
                        else
                            if (_cmd != "")
                            {
                                int? access;
                                access = Sh.S.GetMUC(m_r.MUC.Jid).AccessManager.GetAccess(_cmd);

                                if (access == null)
                                {
                                    string a = "", b = "";
                                    _cmd = m_r.MUC.GetAlias(_cmd, ref a, ref b);
                                    access = Sh.S.AccessManager.GetAccess(_cmd);
                                }

                                access = Utils.SplitEx(_cmd, 2)[0].ToLower() == "admin" && access == null ? 100 : access;
                                rs = (access ?? 0).ToString();
                            }
                            else
                                syntax_error = true;

                   
                        break;
                    }

                case "moderator":
                    {
                        if (ws.Length > 2)
                        {
                            if (myaccess >= 60)
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
                                    m_r.MUC.Moderator(m_r, ws[2], reason);
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
                            m_r.MUC.Kick(m_r, ws[2].Trim(), reason);

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
                            if (ws[2].Trim() == "show")
                            {
                                Sh.S.Tempdb.CleanAutoKick(m_r.MUC.Jid);
                                string data = Sh.S.Tempdb.GetAutoKickList(m_r.MUC.Jid, "{1}) {3}      ({2}, {4}, '{5}')", m_r);
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
                                        // *muc avisitor $15h jid-exp ^[0-9]+\@\s*$ || bad jid..
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
                                            ws = Utils.SplitEx(m_b.Trim(), 2);
                                            determiner = "AKICK_JID";
                                            MUser user = m_r.MUC.GetUser(ws[2]);
                                            if (user != null)
                                                if (user.Jid.Bare != m_r.Msg.From.Bare)
                                                    value = user.Jid.Bare;
                                                else
                                                    value = ws[2].ToLower();
                                            else
                                                value = ws[2].ToLower();

                                            if (!Sh.S.JidValid(value))
                                            {
                                                m_r.Reply(m_r.f("jid_not_valid", value));
                                                return;
                                            }
                                        }
                                        if (ws.Length > 3)
                                        {
                                            value = ws[ws.Length - 1].Trim();
                                            bool time_span_setted = false;
                                            bool type_setted = false;
                                            int time_pos = -1;
                                            int det_pos = -1;
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
                                                                time_pos = -1;
                                                                time_span_setted = false;
                                                            }
                                                            if (time_span_setted)
                                                            {
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
                                                                            ticks += (_time * 600000000);
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
                                                        time_pos = i;
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
                                                            case "nick":
                                                                determiner = "AKICK_NICK";
                                                                break;
                                                            default:
                                                                det_pos = -1;
                                                                type_setted = false;
                                                                break;

                                                        }
                                                        det_pos = i;

                                                    }


                                                }
                                            }





                                            if (!type_setted)
                                            {
                                                if ((time_span_setted) && (det_pos == 2) && (time_pos != -1))
                                                {
                                                    m_r.Reply(m_r.f("akick_wrong_parameter"));
                                                    return;
                                                }

                                            }
                                            else
                                            {
                                                m_b = m_b.Remove(m_b.IndexOf(ws[det_pos]), ws[det_pos].Length);
                                            }

                                            if (!time_span_setted)
                                            {
                                                if ((type_setted) && (time_pos == 2) && (det_pos != -1))
                                                {
                                                    m_r.Reply(m_r.f("akick_wrong_parameter"));
                                                    return;
                                                }
                                            }
                                            else
                                            {
                                                m_b = m_b.Remove(m_b.IndexOf(ws[time_pos]), ws[time_pos].Length);
                                            }
                                        }

                                        ws = Utils.SplitEx(m_b, 2);
                                        value = ws[2].Trim();
                                        if (determiner == "AKICK_JID")
                                        {
                                            MUser _user = m_r.MUC.GetUser(ws[2]);
                                            if (_user != null)
                                                if (_user.Jid.Bare != m_r.Msg.From.Bare)
                                                    value = _user.Jid.Bare;
                                                else
                                                    value = ws[2].ToLower();
                                            else
                                                value = ws[2].ToLower();
                                        }

                                        if (Sh.S.Tempdb.AddAutoKick(value, m_r.MUC.Jid, determiner, reason, ticks))
                                        {
                                            rs = m_r.Agree();
                                        }
                                        else
                                            rs = rs = m_r.f("akick_already_exsists");



                                        foreach (MUser user in m_r.MUC.Users.Values)
                                        {
                                            string ak = Sh.S.Tempdb.IsAutoKick(user.Jid, user.Nick, m_r.MUC.Jid, Sh);
                                            if (ak != null)
                                                if (m_r.MUC.KickableForCensored(user))
                                                    m_r.MUC.Kick(null, user.Nick, ak);
                                        }







                                    }

                        }
                        else
                            syntax_error = true;
                        break;
                    }

                case "avisitor":
                    {
                        ws = Utils.SplitEx(m_b.Trim(), 3);
                        if (ws.Length > 2)
                        {
                            if (ws[2].Trim() == "show")
                            {
                                Sh.S.Tempdb.CleanAutoVisitor(m_r.MUC.Jid);
                                string data = Sh.S.Tempdb.GetAutoVisitorList(m_r.MUC.Jid, "{1}) {3}      ({2}, {4}, '{5}')", m_r);
                                m_r.Reply(data != null ? data : m_r.f("avisitor_list_empty"));
                                return;
                            }
                            else
                                if (ws[2].Trim() == "clear")
                                {
                                    Sh.S.Tempdb.ClearAutoVisitor(m_r.MUC.Jid);
                                    m_r.Reply(m_r.f("avisitor_list_cleared"));
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
                                            if (Sh.S.Tempdb.DelAutoVisitor(m_r.MUC.Jid, num))
                                                rs = m_r.Agree();
                                            else
                                                rs = m_r.f("avisitor_not_existing");
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
                                        // *muc avisitor $15h jid-exp ^[0-9]+\@\s*$ || bad jid..
                                        if (m_b.IndexOf(" ||") > -1)
                                        {
                                            reason = m_b.Substring(m_b.IndexOf(" ||") + 3).Trim() == "" ? null : m_b.Substring(m_b.IndexOf(" ||") + 3).Trim();

                                            if (reason != null)
                                                m_b = m_b.Remove(m_b.IndexOf(" ||"));

                                        }
                                        if (reason == null)
                                            reason = m_r.f("devoice_reason");
                                        string determiner = "AKICK_JID";
                                        string value = "";
                                        long ticks = 0;
                                        ws = Utils.SplitEx(m_b.Trim(), 4);
                                        if (ws.Length == 3)
                                        {
                                            ws = Utils.SplitEx(m_b.Trim(), 2);
                                            determiner = "AKICK_JID";
                                            MUser user = m_r.MUC.GetUser(ws[2]);
                                            if (user != null)
                                                if (user.Jid.Bare != m_r.Msg.From.Bare)
                                                    value = user.Jid.Bare;
                                                else
                                                    value = ws[2].ToLower();
                                            else
                                                value = ws[2].ToLower();

                                            if (!Sh.S.JidValid(value))
                                            {
                                                m_r.Reply(m_r.f("jid_not_valid", value));
                                                return;
                                            }
                                        }
                                        if (ws.Length > 3)
                                        {
                                            value = ws[ws.Length - 1].Trim();
                                            bool time_span_setted = false;
                                            bool type_setted = false;
                                            int time_pos = -1;
                                            int det_pos = -1;
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
                                                                time_span_setted = false;
                                                            }
                                                            if (time_span_setted)
                                                            {
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
                                                                            ticks += (_time * 600000000);
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
                                                                            m_r.Reply(m_r.f("avisitor_wrong_parameter"));
                                                                            return;
                                                                    }

                                                                }

                                                            }
                                                        }
                                                        time_pos = i;
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
                                                            case "nick":
                                                                determiner = "AKICK_NICK";
                                                                break;
                                                            default:
                                                                type_setted = false;
                                                                break;

                                                        }
                                                        det_pos = i;
                                                    }

                                                }
                                            }



                                            if (!type_setted)
                                            {
                                                if ((time_span_setted) && (det_pos == 2) && (time_pos != -1))
                                                {
                                                    m_r.Reply(m_r.f("avisitor_wrong_parameter"));
                                                    return;
                                                }
                                            }
                                            else
                                            {
                                                m_b = m_b.Remove(m_b.IndexOf(ws[det_pos]), ws[det_pos].Length);
                                            }

                                            if (!time_span_setted)
                                            {
                                                if ((type_setted) && (time_pos == 2) && (det_pos != -1))
                                                {
                                                    m_r.Reply(m_r.f("avisitor_wrong_parameter"));
                                                    return;
                                                }
                                            }
                                            else
                                            {
                                                m_b = m_b.Remove(m_b.IndexOf(ws[time_pos]), ws[time_pos].Length);
                                            }
                                        }

                                        ws = Utils.SplitEx(m_b, 2);
                                        value = ws[2].Trim();
                                        if (determiner == "AKICK_JID")
                                        {
                                            MUser _user = m_r.MUC.GetUser(ws[2]);
                                            if (_user != null)
                                                if (_user.Jid.Bare != m_r.Msg.From.Bare)
                                                    value = _user.Jid.Bare;
                                                else
                                                    value = ws[2].ToLower();
                                            else
                                                value = ws[2].ToLower();
                                        }

                                        if (Sh.S.Tempdb.AddAutoVisitor(value, m_r.MUC.Jid, determiner, reason, ticks))
                                        {
                                            rs = m_r.Agree();
                                        }
                                        else
                                            rs = rs = m_r.f("avisitor_already_exsists");



                                        foreach (MUser user in m_r.MUC.Users.Values)
                                        {
                                            string ak = Sh.S.Tempdb.IsAutoVisitor(user.Jid, user.Nick, m_r.MUC.Jid, Sh);
                                            if (ak != null)
                                            {
                                                if (m_r.MUC.KickableForCensored(user))
                                                    m_r.MUC.Devoice(null, user.Nick, Utils.FormatEnvironmentVariables(ak, m_r.MUC, user));

                                            }
                                        }




                                    }

                        }
                        else
                            syntax_error = true;
                        break;
                    }



                case "vipaccess":
                    {
                        ws = Utils.SplitEx(m_b, 3);
                        if (ws.Length > 2)
                        {
                            if (ws[2] == "del")
                            {

                                if (ws.Length > 3)
                                {
                                    try
                                    {
                                        if (Sh.S.GetMUC(m_r.MUC.Jid).VipAccess.DelVip(Convert.ToInt32(ws[3])))
                                            rs = m_r.f("vip_deleted");
                                        else
                                            rs = m_r.f("vip_not_found", ws[3]);
                                    }
                                    catch
                                    {
                                        if (Sh.S.GetMUC(m_r.MUC.Jid).VipAccess.DelVip(new Jid(ws[3])))
                                            rs = m_r.f("vip_deleted");
                                        else
                                            rs = m_r.f("vip_not_found", ws[3]);
                                    }
                                }
                                else
                                    syntax_error = true;
                            }
                            else
                                if (ws[2] == "clear")
                                {
                                    if (ws.Length == 3)
                                    {
                                        Sh.S.GetMUC(m_r.MUC.Jid).VipAccess.Clear();
                                        rs = m_r.f("vip_list_cleared");
                                    }
                                    else
                                        syntax_error = true;

                                }
                                else
                                    if (ws[2] == "show")
                                    {
                                        if (ws.Length == 3)
                                        {
                                            rs = Sh.S.GetMUC(m_r.MUC.Jid).VipAccess.GetAllVips("{1}) {2} : {3}");
                                            if (rs == null)
                                                rs = m_r.f("vip_list_empty");
                                        }
                                        else
                                        {
                                            if (!Sh.S.JidValid(ws[3]))
                                            {
                                                rs = m_r.f("jid_not_valid", ws[3]);
                                                break;
                                            }
                                            else
                                            {
                                                int? acc = Sh.S.GetMUC(m_r.MUC.Jid).VipAccess.GetAccess(new Jid(ws[3]));
                                                rs = acc == null ? m_r.f("vip_not_found", ws[3]) : acc.ToString();
                                            }
                                        }

                                    }
                                    else
                                        if (ws[2] == "count")
                                        {
                                            if (ws.Length == 3)
                                            {
                                                rs = Sh.S.GetMUC(m_r.MUC.Jid).VipAccess.Count().ToString();
                                            }
                                            else
                                                syntax_error = true;

                                        }
                                        else
                                        {
                                            if (ws.Length > 3)
                                            {
                                                Jid Jid;
                                                MUser user = m_r.MUC.GetUser(ws[2]);
                                                if (user != null)
                                                    if (user.Jid.Bare != m_r.Msg.From.Bare)
                                                        Jid = user.Jid;
                                                    else
                                                        Jid = new Jid(ws[2]);
                                                else
                                                    Jid = new Jid(ws[2]);

                                                if (!Sh.S.JidValid(Jid))
                                                {
                                                    rs = m_r.f("jid_not_valid", Jid.ToString());
                                                    break;
                                                }
                                                else
                                                {
                                                    try
                                                    {
                                                        int ac = Convert.ToInt32(ws[3]);
                                                        if ((ac > 100) || (ac < 0))
                                                        {
                                                            syntax_error = true;
                                                            break;
                                                        }
                                                        if (m_r.Access < 100)
                                                            if (ac > m_r.Access - 10)
                                                            {
                                                                rs = m_r.f("muc_vipaccess_access_not_enough");
                                                                break;
                                                            }

                                                        Sh.S.GetMUC(m_r.MUC.Jid).VipAccess.AddVip(Jid, ac);
                                                        rs = m_r.Agree();
                                                    }
                                                    catch
                                                    {
                                                        syntax_error = true;
                                                    }
                                                }

                                            }
                                            else
                                            {
                                                syntax_error = true;
                                            }




                                        }
                        }
                        else
                            syntax_error = true;
                        break;
                    }



                case "viplang":
                    {
                        ws = Utils.SplitEx(m_b, 3);
                        if (ws.Length > 2)
                        {

                            if (ws[2] == "del")
                            {

                                if (ws.Length > 3)
                                {
                                    try
                                    {
                                        if (Sh.S.GetMUC(m_r.MUC.Jid).VipLang.DelVip(Convert.ToInt32(ws[3])))
                                            rs = m_r.f("vip_deleted");
                                        else
                                            rs = m_r.f("vip_not_found", ws[3]);
                                    }
                                    catch
                                    {
                                        if (Sh.S.GetMUC(m_r.MUC.Jid).VipLang.DelVip(new Jid(ws[3])))
                                            rs = m_r.f("vip_deleted");
                                        else
                                            rs = m_r.f("vip_not_found", ws[3]);
                                    }
                                }
                                else
                                    syntax_error = true;
                            }
                            else
                                if (ws[2] == "clear")
                                {
                                    if (ws.Length == 3)
                                    {
                                        Sh.S.GetMUC(m_r.MUC.Jid).VipLang.Clear();
                                        rs = m_r.f("vip_list_cleared");
                                    }
                                    else
                                        syntax_error = true;

                                }
                                else
                                    if (ws[2] == "show")
                                    {
                                        if (ws.Length == 3)
                                        {
                                            rs = Sh.S.GetMUC(m_r.MUC.Jid).VipLang.GetAllVips("{1}) {2} : {3}");
                                            if (rs == null)
                                                rs = m_r.f("vip_list_empty");
                                        }
                                        else
                                        {
                                            if (!Sh.S.JidValid(ws[3]))
                                            {
                                                rs = m_r.f("jid_not_valid", ws[3]);
                                                break;
                                            }
                                            else
                                            {
                                                rs = Sh.S.GetMUC(m_r.MUC.Jid).VipLang.GetLang(new Jid(ws[3]));
                                                if (rs == null)
                                                    rs = m_r.f("vip_not_found", ws[3]);
                                            }
                                        }

                                    }
                                    else
                                        if (ws[2] == "count")
                                        {
                                            if (ws.Length == 3)
                                            {
                                                rs = Sh.S.GetMUC(m_r.MUC.Jid).VipLang.Count().ToString();
                                            }
                                            else
                                                syntax_error = true;

                                        }
                                        else
                                        {

                                            if (ws.Length > 3)
                                            {
                                                Jid Jid;
                                                MUser user = m_r.MUC.GetUser(ws[2]);
                                                if (user != null)
                                                    if (user.Jid.Bare != m_r.Msg.From.Bare)
                                                        Jid = user.Jid;
                                                    else
                                                        Jid = new Jid(ws[2]);
                                                else
                                                    Jid = new Jid(ws[2]);

                                                if (!Sh.S.JidValid(Jid))
                                                {
                                                    rs = m_r.f("jid_not_valid", Jid.ToString()).ToString();
                                                    break;
                                                }
                                                else
                                                {

                                                    if (Sh.S.Rg.GetResponse(ws[3]) != null)
                                                    {

                                                        Sh.S.GetMUC(m_r.MUC.Jid).VipLang.AddVip(Jid, ws[3]);
                                                        rs = m_r.Agree();
                                                    }
                                                    else
                                                        rs = m_r.f("lang_pack_not_found", ws[3]);

                                                }

                                            }
                                            else
                                            {
                                                syntax_error = true;
                                            }


                                        }
                        }
                        else
                            syntax_error = true;
                        break;
                    }






                case "setsubject":
                    {
                        if (myaccess >= 60)
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
                        if (ws.Length > 2)
                        {
                            if (ws[2] == "del")
                            {
                                if (ws.Length > 3)
                                {
                                    if (m_r.MUC.DelRoomCensor(ws[3]))
                                        rs = m_r.f("censor_deleted");
                                    else
                                        rs = m_r.f("censor_not_existing");
                                }
                                else
                                    syntax_error = true;
                            }
                            else
                                if (ws[2] == "show")
                                {
                                    if ((ws.Length == 3))
                                    {
                                        string data = m_r.MUC.GetRoomCensorList("{1}) {2}   =>   \"{3}\"");
                                        rs = data != null ? data : m_r.f("censor_list_empty");
                                    }
                                    else
                                        syntax_error = true;
                                }
                                else
                                    if (ws[2] == "clear")
                                    {
                                        if ((ws.Length == 3))
                                        {
                                            m_r.MUC.ClearCensor();
                                            rs = m_r.f("censor_list_cleared");
                                        }
                                        else
                                            syntax_error = true;
                                    }
                                    else
                                    {

                                        string reason = null;
                                        if (m_b.IndexOf(" ||") > -1)
                                        {
                                            reason = m_b.Substring(m_b.IndexOf(" ||") + 3).Trim() == "" ? null : m_b.Substring(m_b.IndexOf(" ||") + 3).Trim();

                                            if (reason != null)
                                                m_b = m_b.Remove(m_b.IndexOf(" ||"));

                                        }
                                        if (reason == null)
                                            reason = m_r.f("kick_censored_reason");
                                        ws = Utils.SplitEx(m_b, 2);
                                        m_r.MUC.AddRoomCensor(ws[2].Trim(), reason);
                                        rs = m_r.Agree();
                                    }
                        }
                        else
                            syntax_error = true;
                        break;
                    }

                case "greet":
                    {
                        Jid Room = m_r.MUC.Jid;
                        ws = Utils.SplitEx(m_b, 3);
                        if (ws.Length > 2)
                        {

                            if (ws[2] == "del")
                            {

                                if (ws.Length > 3)
                                {

                                    try
                                    {
                                        if (Sh.S.Tempdb.DelGreet(Room, Convert.ToInt32(ws[3])))
                                            rs = m_r.f("greet_deleted");
                                        else
                                            rs = m_r.f("greet_not_existing");
                                    }
                                    catch
                                    {
                                        Jid Jid = new Jid(ws[3]);
                                        if (Jid.ToString().IndexOf("@") <= 0)
                                        {
                                            m_r.Reply(m_r.f("jid_not_valid"));
                                            return;
                                        }

                                        if (Sh.S.Tempdb.DelGreet(Jid, Room))
                                            rs = m_r.f("greet_deleted");
                                        else
                                            rs = m_r.f("greet_not_existing");
                                    }
                                    break;
                                }
                                else
                                    syntax_error = true;

                                break;
                            }
                            else
                                if (ws[2] == "show")
                                {
                                    if (ws.Length == 3)
                                    {
                                        rs = Sh.S.Tempdb.GetGreetList(Room, "{1}) {2} : {3}");
                                        if (rs == null)
                                            rs = m_r.f("greet_list_empty");
                                    }
                                    else
                                        syntax_error = true;
                                }
                                else
                                    if (ws[2] == "clear")
                                    {
                                        if (ws.Length == 3)
                                        {
                                            Sh.S.Tempdb.ClearGreet(Room);
                                            rs = m_r.f("greet_list_cleared");
                                        }
                                        else
                                            syntax_error = true;
                                    }
                                    else
                                    {
                                        Jid Jid;
                                        MUser user = m_r.MUC.GetUser(ws[2]);
                                        if (user != null)
                                            if (user.Jid.Bare != m_r.Msg.From.Bare)
                                                Jid = user.Jid;
                                            else
                                                Jid = new Jid(ws[2]);
                                        else
                                            Jid = new Jid(ws[2]);


                                        if (!Sh.S.JidValid(Jid))
                                        {
                                            m_r.Reply(m_r.f("jid_not_valid"));
                                            return;
                                        }

                                        if (Sh.S.Tempdb.AddGreet(Jid, Room, ws[3]))
                                            rs = m_r.Agree();
                                        else
                                            rs = m_r.f("greet_already");
                                        break;
                                    }
                        }
                        else
                            syntax_error = true;
                        break;
                    }

                case "amoderator":
                    {
                        ws = Utils.SplitEx(m_b, 3);
                        long period = 0;
                        if (ws.Length > 2)
                        {
                            if (ws[2] == "del")
                            {
                                if (ws.Length > 3)
                                {
                                    try
                                    {
                                        if (Sh.S.Tempdb.DelAutoModerator(m_r.MUC.Jid, Convert.ToInt32(ws[3])))
                                            rs = m_r.f("amoderator_deleted");
                                        else
                                            rs = m_r.f("amoderator_not_existing", ws[3]);
                                    }
                                    catch
                                    {
                                        syntax_error = true;
                                        break;
                                    }

                                }
                                else
                                    syntax_error = true;
                            }
                            else
                                if (ws[2] == "show")
                                {
                                    if ((ws.Length == 3))
                                    {
                                        Sh.S.Tempdb.ClearAutoModerator(m_r.MUC.Jid);
                                        string data = Sh.S.Tempdb.GetAutoModeratorList(m_r.MUC.Jid, "{1}) {3} {2}", m_r);
                                        rs = data != null ? data : m_r.f("amoderator_list_empty");
                                    }
                                    else
                                        syntax_error = true;
                                }
                                else
                                    if (ws[2] == "clear")
                                    {
                                        if ((ws.Length == 3))
                                        {
                                            Sh.S.Tempdb.CleanAutoModerator(m_r.MUC.Jid);
                                            rs = m_r.f("amoderator_list_cleared");
                                        }
                                        else
                                            syntax_error = true;
                                    }
                                    else
                                    {
                                        if (ws[2].StartsWith("$"))
                                        {
                                            bool is_time = false;
                                            Regex reg = new Regex("[0-9]+[s|m|h|d|M]");

                                            if (reg.IsMatch(ws[2]))
                                            {
                                                is_time = true;
                                            }

                                            foreach (Match m in reg.Matches(ws[2]))
                                            {
                                                string full = m.ToString().Trim();
                                                string time_span = full.Substring(0, full.Length - 1);
                                                if (time_span == "&")
                                                    break;
                                                long _time = Convert.ToInt64(time_span);
                                                char type = full[full.Length - 1];
                                                switch (type)
                                                {
                                                    case 's':
                                                        period += _time * 10000000;
                                                        break;
                                                    case 'm':
                                                        period += (_time * 600000000);
                                                        break;
                                                    case 'h':
                                                        period += _time * 36000000000;
                                                        break;
                                                    case 'd':
                                                        period += _time * 864000000000;
                                                        break;
                                                    case 'M':
                                                        period += _time * 25920000000000;
                                                        break;
                                                    default:
                                                        is_time = false;
                                                        break;
                                                }
                                            }
                                            if (is_time)
                                                m_b = m_b.Remove(m_b.IndexOf(ws[2]), ws[2].Length);

                                        }
                                        @out.exe("amoderator_starting_setting");
                                        ws = Utils.SplitEx(m_b, 2);
                                        Jid Jid;
                                        MUser user = m_r.MUC.GetUser(ws[2]);
                                        if (user != null)
                                            if (user.Jid.Bare != m_r.Msg.From.Bare)
                                                Jid = user.Jid;
                                            else
                                                Jid = new Jid(ws[2]);
                                        else
                                            Jid = new Jid(ws[2]);
                                        if (!Sh.S.JidValid(Jid))
                                        {
                                            m_r.Reply(m_r.f("jid_not_valid", Jid.ToString()));
                                            return;
                                        }



                                        @out.exe("amoderator_starting_moderate");
                                        if (Sh.S.Tempdb.AddAutoModerator(Jid, m_r.MUC.Jid, period))
                                        {
                                            rs = m_r.Agree();
                                            foreach (MUser _user in m_r.MUC.Users.Values)
                                            {
                                                if (Sh.S.Tempdb.AutoModerator(_user.Jid, m_r.MUC.Jid))
                                                    m_r.MUC.Moderator(null, _user.Nick, null);
                                            }
                                        }
                                        else
                                            rs = m_r.f("amoderator_already_exsists");
                                        @out.exe("amoderator_finished_moderate");
                                    }
                        }
                        else
                            syntax_error = true;
                        break;
                    }

                case "tryme":
                    {
                        if (myaccess >= 60)
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
                                                reason = m_b.Substring(m_b.IndexOf(" ||") + 3).Trim() == "" ? null : m_b.Substring(m_b.IndexOf(" ||") + 3).Trim();

                                                if (reason != null)
                                                    m_b = m_b.Remove(m_b.IndexOf(" ||"));

                                            }
                                            if (reason == null)
                                                reason = m_r.f("tryme_reason");
                                            ws = Utils.SplitEx(m_b.Trim(), 2);
                                            m_r.MUC.Kick(null, m_r.MUser.Nick, reason);
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
                                Sh.S.GetMUC(m_r.MUC.Jid).MyStatus = status;
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

                        if (ws.Length < 3)
                        {
                            string data = "";
                            short index = 1;
                            foreach (MUC m in Sh.S.MUCs.Values)
                            {
                                data += "\n" + index++.ToString() + ") <" + m.Jid.ToString() + "/" + m.MyNick + ">    (" + m.Users.Count + ")\n     " +
                                    m.Language + " | " + m.Me.Affiliation + "/" + m.Me.Role + "\n     " +
                                    m.MyShow.ToString().Replace("NONE", "Online") + " (" + m.MyStatus + ")";
                            }
                            rs = m_r.f("mucs_list") + data + "\n-- " + Sh.S.MUCs.Count.ToString() + " --";
                        }
                        else
                        {
                            Jid room = Sh.S.GetMUCJid(ws[2]);
                            if (room != null)
                            {
                                MUC m = Sh.S.GetMUC(room);
                                rs = "\n<" + m.Jid.ToString() + "/" + m.MyNick + ">    (" + m.Users.Count + ")\n     " +
                                                                   m.Language + " | " + m.Me.Affiliation + "/" + m.Me.Role + "\n     " +
                                                                   m.MyShow.ToString().Replace("NONE", "Online") + " (" + m.MyStatus + ")";
                            }
                            else
                                rs = m_r.f("muc_not_in");
                        }
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
                        if (myaccess >= 60)
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
                                if (m_r.MUC.UserExists(ws[2]))
                                {
                                    if (m_r.MUC.GetUser(ws[2]).Access == 100)
                                    {
                                        m_r.Reply(m_r.Deny());
                                        return;
                                    }
                                }
                                int? acc = Sh.S.GetAccess(m_r.Msg, ws[2], m_r.MUC);
                                if (m_r.Access > acc || (m_r.Access == 100 && acc == 100) || (acc == 80 && m_r.Access == 80))
                                    m_r.MUC.Ban(m_r, ws[2], reason);
                                else
                                    rs = m_r.Deny();
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
                                int? acc = Sh.S.GetAccess(m_r.Msg, ws[2], m_r.MUC);
                                if (m_r.Access > acc || (m_r.Access == 100 && acc == 100) || (acc == 80 && m_r.Access == 80))
                                    m_r.MUC.Admin(m_r, ws[2], reason);
                                else
                                    rs = m_r.Deny();
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

                                int? acc = Sh.S.GetAccess(m_r.Msg, ws[2], m_r.MUC);
                                if (m_r.Access > acc || (m_r.Access == 100 && acc == 100) || (acc == 80 && m_r.Access == 80))
                                    m_r.MUC.Owner(m_r, ws[2], reason);
                                else
                                    rs = m_r.Deny();
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
                        if (myaccess >= 40)
                        {
                            @out.exe("victim_access: " + Sh.S.GetAccess(m_r.Msg, ws[2], m_r.MUC).ToString());
                            if (ws.Length > 2)
                            {
                                int? acc = Sh.S.GetAccess(m_r.Msg, ws[2], m_r.MUC);
                                if (m_r.Access > acc || (m_r.Access == 100 && acc == 100) || (Sh.S.Self(m_r.MUC, m_r.MUser, ws[2])) || (acc == 80 && m_r.Access == 80))
                                    m_r.MUC.Participant(m_r, ws[2]);
                                else
                                    rs = m_r.Deny();

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
                            clm.Body = m_r.f("clean_up_unit");
                            m_r.Connection.Send(clm);
                            Thread.Sleep(1500);

                        }
                        break;

                    }
                case "member":
                    {
                        if (myaccess >= 60)
                        {
                            if (ws.Length > 2)
                            {
                                int? acc = Sh.S.GetAccess(m_r.Msg, ws[2], m_r.MUC);
                                @out.exe("self: " + Sh.S.Self(m_r.MUC, m_r.MUser, ws[2]).ToString());
                                if (m_r.Access > acc || (m_r.Access == 100 && acc == 100) || (Sh.S.Self(m_r.MUC, m_r.MUser, ws[2])) || (acc == 80 && m_r.Access == 80))
                                    m_r.MUC.MemberShip(m_r, ws[2]);
                                else
                                    rs = m_r.Deny();

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
                        if (myaccess >= 40)
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
                                    int? acc = Sh.S.GetAccess(m_r.Msg, ws[2], m_r.MUC);
                                    if (m_r.Access > acc || (m_r.Access == 100 && acc == 100) || (acc == 80 && m_r.Access == 80))
                                        m_r.MUC.Voice(m_r, ws[2], reason);
                                    else
                                        rs = m_r.Deny();
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
                        if (myaccess >= 40)
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
                                    if (m_r.MUC.GetUser(ws[2]).Access == 100)
                                    {
                                        m_r.Reply(m_r.Deny());
                                        return;
                                    }
                                    int? acc = Sh.S.GetAccess(m_r.Msg, ws[2], m_r.MUC);
                                    if (m_r.Access > acc || (m_r.Access == 100 && acc == 100) || (acc == 80 && m_r.Access == 80))
                                        m_r.MUC.Devoice(m_r, ws[2], reason);
                                    else
                                        rs = m_r.Deny();
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
                            if (m_r.Msg.Type == MessageType.groupchat)
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
                            msg.Body = Utils.FormatEnvironmentVariables(ws[2], m_r.MUC, m_r.MUser);
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


                        rs = m_user.Show.ToString().Replace("NONE", "Online") + " (" + m_user.Status + ")";
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

                            string vl = Sh.S.VipLang.GetLang(m_user.Jid);
                            if (vl == null)
                                vl = m_r.MUC.VipLang.GetLang(m_user.Jid);
                            string lng = vl != null ?
                                               vl :
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

                            int? access = Sh.S.GetAccess(msg, m_user, m_r.MUC);
                            if ((m_r.Emulation != null) && (ws.Length == 2))
                                access = 100;
                            rs = m_r.f("muc_user_info",
                                   m_user.Nick,
                                   m_user.Affiliation + "/" + m_user.Role,
                                   lng,
                                   (access ?? 0).ToString(),
                                   m_user.Show.ToString().Replace("NONE", "Online") + " (" + m_user.Status + ")",
                                   m_user.EnterTime,
                                   ShowJid);
                        }
                        break;
                    }

                case "list":
                    {
                        if (ws.Length == 2)
                        {
                            rs = m_r.f("volume_list", n) + "\nlist, show, disco, owner, admin, amoderator, moderator, member, voice, devoice, none, avisitor, kick, akick, ban, tryme, cmdaccess, vipaccess, viplang, subject, setsubject, censor, mylang, mystatus, mynick, jid, nicks, name, greet, tell, echo, status, entered, role, info, me, clean";
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