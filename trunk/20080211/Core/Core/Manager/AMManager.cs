using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using agsXMPP;
using agsXMPP.protocol.client;
using agsXMPP.Xml.Dom;
using agsXMPP.protocol.x.muc;
using Core.Plugins;
using Core.Conference;
using Core.Special;

namespace Core.Client
{
    public class AMManager : XMLContainer
    {

        int m_count;

        public AMManager(string AMFile)
        {
            
            m_count = 0;
            Open(AMFile,10);

            foreach (Element el in Document.RootElement.SelectSingleElement("rooms").SelectElements("room"))
            {
                m_count++;
            }

        }



        public bool SetLanguage(Jid Room, string lang)
        {
            lock (Document)
            {
                foreach (Element el in Document.RootElement.SelectSingleElement("rooms").SelectElements("room"))
                {
                    if (el.GetAttribute("jid") == Room.Bare)
                    {
                        el.SetAttribute("lang", lang);
                        Save();
                        return true;
                    }
                }
                return false;
            }
        }

        public bool SetStatus(Jid Room, string status)
        {
            lock (Document)
            {
                foreach (Element el in Document.RootElement.SelectSingleElement("rooms").SelectElements("room"))
                {
                    if (el.GetAttribute("jid") == Room.Bare)
                    {
                        el.SetAttribute("status", status);
                        Save();
                        return true;
                    }
                }
                return false;
            }
        }

        public bool AddMuc(Jid Room, string nick, string status, string language)
        {
            lock (Document)
            {
                foreach (AutoMuc am in GetAMList())
                {
                    if (am.Jid.ToString() == Room.ToString())
                        return false;
                }

                Document.RootElement.SelectSingleElement("rooms").AddTag("room");
                foreach (Element el in Document.RootElement.SelectSingleElement("rooms").SelectElements("room"))
                {
                    if (!el.HasAttribute("jid"))
                    {
                        el.SetAttribute("status", status);
                        el.SetAttribute("jid",    Room.ToString());
                        el.SetAttribute("nick",   nick);
                        el.SetAttribute("lang",   language);
                        Count++;
                        Save();
                        return true;
                    }
                }
                return false;
            }
        }


        public bool DelMuc(Jid Room)
        {
            lock (Document)
            {
               
                foreach (Element el in Document.RootElement.SelectSingleElement("rooms").SelectElements("room"))
                {
                    if (el.GetAttribute("jid") == Room.ToString())
                    {
                        el.Remove();
                        Count--;
                        Save();
                        return true;
                    }
                }
                return true;
            }
        }



        public bool SetNick(Jid Room, string nick)
        {
            lock (Document)
            {
                foreach (Element el in Document.RootElement.SelectSingleElement("rooms").SelectElements("room"))
                {
                    if (el.GetAttribute("jid") == Room.Bare)
                    {
                        el.SetAttribute("nick", nick);
                        Save();
                        return true;
                    }
                }
                return false;
            }
        }




        public int Count
        {
            get { lock (aso[3]) { return m_count; } }
            set { lock (aso[3]) { m_count = value; } }
        }

        public bool Exists(Jid Room)
        {
            lock (Document)
            {
                foreach (AutoMuc am in GetAMList())
                {
                    if (am.Jid.ToString() == Room.ToString())
                        return true;
                }
                return false;
       
            }
        }

        public AutoMuc[] GetAMList()
        {
            lock (Document)
            {
                AutoMuc[] am;
                int i = 0;
                am = new AutoMuc[Document.RootElement.SelectSingleElement("rooms").SelectElements("room").Count];
                foreach (Element el in Document.RootElement.SelectSingleElement("rooms").SelectElements("room"))
                {

                    am[i] = new AutoMuc(new Jid(el.GetAttribute("jid")),
                        el.GetAttribute("nick"),
                        el.GetAttribute("status"),
                        el.GetAttribute("lang"));

                    i++;
                }
                return am;
            }
        }
    }
}
