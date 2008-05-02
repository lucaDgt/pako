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

using System.Collections.Generic;
using System.Collections;
using System.Diagnostics;
using System.Text;
using System;
using agsXMPP;
using System.Web;
using agsXMPP.protocol.client;
using agsXMPP.Xml.Dom;
using System.IO;
using System.Net;
using Core.Kernel;
using Core.Conference;
using Core.Plugins;
using System.Threading;
using Core.Xml;
using Core.Other;

namespace Core.Kernel
{

    public class CommandHandler
    {

        #region Variables

        Message m_msg;
        bool alias_exists;
        Jid s_jid;
        string s_nick;
        MUser m_user;
        string original;
        Jid m_jid;
        MUC m_muc;
        SessionHandler Sh;
        Message emulate;

        #endregion;



        public CommandHandler(agsXMPP.protocol.client.Message msg, SessionHandler s, Message emulation)
        {
            m_msg = msg;

            s_jid = msg.From;
            Sh = s;
            if (s_jid.Bare == Sh.S.C.MyJID.Bare)
                return;
            emulate = emulation;
            Thread thr = new Thread(new ThreadStart(Handle));
            thr.Start();

        }

        public void Handle()
        {
            // _Handle();
            try
            {
                _Handle();
            }
            catch (Exception ex)
            {

                string clrf = "\n";
                string msg_source = m_msg.ToString();

                if (Utils.OS == Platform.Windows)
                {
                    clrf = "\r\n";
                    msg_source = m_msg.ToString().Replace("\n", "\r\n");

                }
                string data = "====== [" + DateTime.Now.ToString() + "] ===================================================>" + clrf +
                                        "   Stanza:" + clrf + msg_source + clrf + clrf + ex.ToString() + clrf + clrf + clrf;

                Sh.S.ErrorLoger.Write(data);
                Message _msg = new Message();
                _msg.To = Sh.S.Config.Administartion()[0];
                _msg.Type = MessageType.chat;
                _msg.Body = "ERROR:   " + ex.ToString();
                Sh.S.C.Send(_msg);
            }
        }


        public void _Handle()
        {



            if (m_msg.Subject != null)
            {
                Sh.S.GetMUC(s_jid).Subject = m_msg.Subject;
            }

            m_muc = Sh.S.GetMUC(s_jid);
            m_user = null;

            if (m_muc != null)
            {
                if (s_jid.Resource == null)
                    return;
                m_user = m_muc.GetUser(s_jid.Resource);

            }

            string d = Sh.S.Config.Delimiter;

            s_nick = s_jid.Resource;
            msgType m_type = Sh.S.GetTypeOfMsg(m_msg, m_user);
            bool is_muser = m_type == msgType.MUC;
            m_jid = is_muser ? m_user.Jid : s_jid;

            if ((m_type == msgType.MUC) || (m_type == msgType.Roster))
            {

                m_msg.Body = m_msg.Body.TrimStart(' ');
                original = m_msg.Body;
                string m_body = m_msg.Body;

                string vl = Sh.S.VipLang.GetLang(m_jid);
                if ((vl == null) && (is_muser))
                    vl = m_muc.VipLang.GetLang(m_jid);

                Response r = new Response(Sh.S.Rg.GetResponse(
                              vl != null ?
                              vl :
                              is_muser ?
                              m_user.Language :
                              Sh.S.Config.Language
                         ));


                int? access = Sh.S.GetAccess(m_msg, m_user, m_muc);
                r.Access = access;
                r.Msg = m_msg;
                r.MSGLimit = Sh.S.Config.MSGLimit;
                string aliasb = String.Empty;
                string aliase = String.Empty;

                r.MUC = m_muc;
                r.MUser = m_user;
                r.Delimiter = d;
                r.Sh = Sh;

                int? naccess = null;

                if (is_muser)
                {
                    m_user.Access = access;
                    @out.exe("[" + s_jid.User + "] " + s_jid.Resource + "> " + m_msg.Body);
                    if (emulate == null)
                    {
                        @out.exe("emulate_non_existing");
                        if (m_user.Nick == m_muc.MyNick)
                            return;
                    }
                    else
                    {
                        @out.exe("emulate_existing");
                        r.Access = 100;
                        m_user.Access = 100;
                        r.Emulation = emulate;
                    }
                    @out.exe("emulate_extistence_determining_finished");
                    string found_censored = Sh.S.GetMUC(s_jid).IsCensored(m_msg.Body);

                    if (found_censored != null)
                    {
                        @out.exe("censored_caught");
                        if (m_muc.KickableForCensored(m_user))
                        {
                            @out.exe("censored_ready_to_kick");
                            m_muc.Kick(null, m_user.Nick, Utils.FormatEnvironmentVariables(found_censored, m_muc, m_user));
                            return;
                        }
                        else
                        {
                            @out.exe("censored_ready_to_notify");
                            MessageType original_type = r.Msg.Type;
                            r.Msg.Type = MessageType.groupchat;
                            r.Reply(found_censored);
                            r.Msg.Type = original_type;
                            Thread.Sleep(1500);
                        }

                    }
                    else
                        @out.exe("censored_not_found");
                    @out.exe("censored_check_finished");

                    if (!m_muc.HasAlias(m_body))
                    {
                        @out.exe("alias_not_found");
                        if (!m_msg.Body.StartsWith(d))
                            return;
                        else
                        {
                            m_msg.Body = m_msg.Body.Substring(d.Length);

                            m_body = m_msg.Body;
                            if (m_msg.Body.Trim() == "")
                                return;
                        }

                    }
                    else
                        @out.exe("alias_found");

                }
                else
                {
                    @out.exe("roster_user");
                    if (!m_msg.Body.StartsWith(d))
                        return;
                    else
                    {
                        m_msg.Body = m_msg.Body.Substring(d.Length);

                        m_body = m_msg.Body;
                        if (m_msg.Body.Trim() == "")
                            return;
                    }

                }



                if (is_muser)
                {
                    @out.exe("cmd_muc_access_setting_started");
                    naccess = m_muc.AccessManager.GetAccess(m_body);
                    r.AccessType = AccessType.SetByMuc;
                    alias_exists = m_muc.HasAlias(m_body);
                    r.HasAlias = alias_exists;
                    @out.exe("cmd_muc_access_setting_finished");
                    while (m_muc.HasAlias(m_body))
                    {
                        m_body = m_muc.GetAlias(m_body, ref aliasb, ref aliase);
                        @out.exe("alias_launched");
                    }
                    if (alias_exists)
                        m_body = Utils.FormatEnvironmentVariables(m_body, m_muc, m_user);

                    m_msg.Body = m_body;
                    r.Alias = aliasb;
                }

                string[] args = Utils.SplitEx(m_body, 2);

                @out.exe("words_splitted");


                string m_cmd = args[0];

                string m_retort = null;

                @out.exe("<<body>>: " + m_body);


                if ((Sh.S.PluginHandler.Handles(m_cmd)) || (alias_exists))
                {


                    if (naccess == null)
                    {
                        @out.exe("cmd_global_access_setting_started");
                        r.AccessType = AccessType.SetByAdmin;
                        naccess = Sh.S.AccessManager.GetAccess(m_body);
                    }

                    if (naccess == null)
                    {
                        r.AccessType = AccessType.None;
                        @out.exe("cmd_no_access_notifies_found_access=0");
                    }
                    if (access < naccess)
                    {
                        @out.exe("access_not_enough");
                        r.Reply(r.f("access_not_enough", naccess.ToString()));
                        return;
                    }
                }





                if (Sh.S.PluginHandler.Handles(m_cmd))
                {
                        object obj = Sh.S.PluginHandler.Execute(m_cmd);
                        if (obj != null)
                        {
                            IPlugin plugin = (IPlugin)obj;
                            PluginTransfer pt = new PluginTransfer(r);
                            @out.exe("EXE");
                            plugin.PerformAction(pt);

                        }
                    return;
                }



                switch (m_cmd)
                {
                    case "help":
                        {
                            args = Utils.SplitEx(m_body, 1);

                            if (args.Length > 1)
                            {
                                string data = args[1].Trim();
                                while (data.IndexOf("  ") > -1)
                                    data = data.Replace("  ", " ");
                                m_retort = r.GetHelp(data);
                            }
                            else
                                m_retort = r.f("help_info", Sh.S.PluginHandler.GetPluginsList(), d + "<volume> <command>");
                            break;
                        }


                }



                if (m_retort != null)
                    r.Reply(m_retort);
                else
                    if (alias_exists)
                    {
                        r.Reply(aliase);
                    }
            }








        }

    }
}