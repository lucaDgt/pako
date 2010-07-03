﻿/* * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * *
 * Pako Jabber-bot. Bbodio's Lab.                                                *
 * Copyright. All rights reserved © 2007-2008 by Klichuk Bogdan (Bbodio's Lab)   *
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
using Core.Plugins;
using Core.Kernel;
using Core.Conference;
using agsXMPP;
using agsXMPP.protocol.client;

namespace Plugin
{
 
    public class Main : IPlugin
    {
        SessionHandler _session = null;

        public string File
        {
            get
            {
                return "Admin.dll";
            }
        }


        public bool MucOnly
        {
            get
            {
                return false;
            }
        }


        public string Name
        {
            get
            {
                return "Admin";
            }
        }

        public string Comment
        {
            get
            {
                return "A control panel for an adminisrator" ;
            }
        }

        public SessionHandler Session
        {
            get
            {
                return _session;
            }
            set
            {
                _session = value;
            }
        } 

        public bool SubscribePresence 
        { 
            get
            {
                return false;
            }
        }

        public bool SubscribeMessages 
        { 
            get
            {
                return false;
            }
        }
        
        public bool SubscribeIq 
        { 
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Handle a command insede of the plug-in
        /// </summary>
        /// <param name="d"></param>
        public void PerformAction(IPluginData d)
        {

        	if (d.r.AccessType == AccessType.None)
            { 
        		if (d.r.Access >= 100)
        		{
        			ConfigHandler ph = new ConfigHandler(d.r);
        		}
                   else
                   d.r.Reply(d.r.f("access_not_enough","100"));
        	}else
        	{
        		ConfigHandler ph = new ConfigHandler(d.r);
        	}

        }

        // IPlugin implementation

        // Plugin initialization and shut down
        public void Start(SessionHandler sh)
        {
        }

        public void Stop()
        {
        }

        // Handlers
        public void CommandHandler(agsXMPP.protocol.client.Message msg, SessionHandler s, Message emulation, CmdhState signed, int level)
        {
        }

        public void PresenceHandler(Presence m_pres, SessionHandler sh)
        {
        }

        public void IqHandler(IQ iq, XmppClientConnection Con)
        {
        }
    }



}