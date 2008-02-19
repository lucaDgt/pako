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
using agsXMPP;
using agsXMPP.protocol.client;
using agsXMPP.Xml.Dom;
using System.IO;
using Core.Client;
using Core.Conference;
using Core.Manager;

namespace Plugin
{
    public class AliasHandler
    {
        string[] ws;
        Message m_msg;
        Response m_r;
        bool syntax_error = false;
        string self;
        string d;
        Jid s_jid;
        string m_b;
        string n;
        SessionHandler Sh;

        public AliasHandler(Response r, string Name)
        {
            Sh = r.Sh;
            m_r = r;
            m_b = m_r.Msg.Body;
            ws = Utils.SplitEx(m_b, 2);
            m_msg = m_r.Msg;
            n = Name;

            d = r.Delimiter;
            s_jid = m_r.Msg.From;

            if (ws.Length < 2)
            {
                r.Reply(r.FormatPattern("volume_info", n, d + n.ToLower() + " list"));
                return;
            }

            self = ws[0] + " " + ws[1];
            Handle();



        }



        public void Handle()
        {

            string cmd = ws[1];
            string rs = null;
            switch (cmd)
            {
                case "add":
                    {
                        //*alias add $50 ff=trash
                        //*alias add ff=trash
                        ///*alias add $4j ff=trash
                       
                        ws = Utils.SplitEx(m_b, 3);
                        if (ws.Length > 2)
                        {
                           
                                bool spec = false;
                                int access = 0;

                                if ((ws[2].StartsWith("$")) && (ws[2].Length > 1))
                                {


                                    
                                    try
                                    {
                                        access = Convert.ToInt32(ws[2].Substring(1));
                                           @out.exe(">"+m_b);
                                        int index = 0;
                                        m_b = "";
                                           foreach (string word in ws)
                                           {
                                               if (index != 2)
                                               {
                                                   m_b += word + " ";
                                               }
                                                   index++;
                                           }
                                           @out.exe(">"+m_b);
                                     
                                        @out.exe("?"+access.ToString());
                                        spec = true;
                                    }
                                    catch
                                    {
                                    
                                    }
                                }

                                ws = Utils.SplitEx(m_b.Trim(), 2);
                                @out.exe(">"+m_b);
                                if (ws[2].IndexOf("=") > -1)
                                {
                                    string alias = ws[2].Substring(0, ws[2].IndexOf("=")).Trim();
                                    string value = ws[2].Substring(ws[2].IndexOf("=") + 1).Trim();
                                    if ((value != "") && (alias != ""))
                                    {
                                        if (spec)
                                            Sh.S.AccessManager.SetAccess(alias, access);

                                        if (Sh.S.GetMUC(s_jid).AddAlias(alias, value))
                                            rs = m_r.FormatPattern("alias_added");
                                        else
                                            rs = m_r.FormatPattern("alias_already_exists");

                                    }
                                    else
                                        syntax_error = true;
                                }
                                else
                                    syntax_error = true;
                        }
                        else
                            syntax_error = true;
                        break;
                    }

                case "list":
                    {
                        if (ws.Length == 2)
                        {
                            rs = m_r.FormatPattern("volume_list", n) + "\nlist, show, add, del, count, clear";
                        }
                        break;
                    }

                case "show":
                    {
                        if (ws.Length == 2)
                        {
                            rs = m_r.MUC.GetAliasList("{0}) {1} = {2}\n");
                        }
                        break;
                    }


                case "count":
                    {
                        if (ws.Length == 2)
                        {
                            rs = m_r.MUC.AliasCount().ToString();
                        }
                        break;
                    }
                case "clear":
                    {
                        if (ws.Length == 2)
                        {
                            Sh.S.GetMUC(s_jid).ClearAliases();
                            rs = m_r.FormatPattern("aliases_cleared");
                        }
                        break;
                    }
                case "del":
                    {
                        if (ws.Length > 2)
                        {

                            if (Sh.S.GetMUC(s_jid).DelAlias(ws[2]))
                                rs = m_r.FormatPattern("alias_deleted");
                            else
                                rs = m_r.FormatPattern("alias_not_existing");
                        }
                        break;
                    }

                default:
                    {
                        rs = m_r.FormatPattern("volume_cmd_not_found", n, ws[1], d + n.ToLower() + " list");
                        break;
                    }




            }

            if (syntax_error)
            {
                m_r.se(self);
            }
            else

                if (rs != null)
                    m_r.Reply(rs);

        }

    }
}