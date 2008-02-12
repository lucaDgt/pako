﻿using System;
using System.Collections.Generic;
using System.Text;
using agsXMPP;
using agsXMPP.protocol.iq.time;
using agsXMPP.protocol.client;
using Core.Client;

namespace Plugin
{
    public class TimeCB
    {
        Response m_r;
        Jid m_jid;



        public TimeCB(Response r, Jid Jid)
        {
            m_r = r;
            m_jid = Jid;
            TimeIq ti = new TimeIq();
            ti.Type = IqType.get;
            ti.To = m_jid;
            ti.GenerateId();
            m_r.Connection.IqGrabber.SendIq(ti, new IqCB(TimeExtractor), null);

        }




        private void TimeExtractor(object obj, IQ iq, object arg)
        {
           
                Console.WriteLine(" before translate  =>  ");
                agsXMPP.protocol.iq.time.Time vi = iq.Query as agsXMPP.protocol.iq.time.Time;
                Console.WriteLine(" after translate  =>  ");

                string answer;
                string jid = m_jid.ToString();
                bool muc = m_r.MUC != null;
                if (muc)
                {
                    muc = m_jid.Resource != null;
                    if (muc)
                    {
                        muc = m_r.MUC.UserExists(m_jid.Resource);
                        if (muc)
                        jid = m_jid.Resource;
                    }
                }
                if (vi != null)
                {
                    if (iq.Type == IqType.error)
                    {
                        if (iq.Error.HasTag("feature-not-implemented"))
                        {
                            if (m_r.Msg.From.ToString() != m_jid.ToString())
                                answer = m_r.FormatPattern("iq_not_implemented", jid);
                            else
                                answer = m_r.FormatPattern("iq_not_implemented_self");
                        }
                        else
                            answer = m_r.FormatPattern("version_error", jid);
                    }
                    else
                    {

                        string Tz = vi.Tz;
                        Tz = String.IsNullOrEmpty(Tz) ? "" : Tz;

                        string display = vi.Display;
                        display = String.IsNullOrEmpty(display) ? "" : display;

                        string utc = vi.Utc;
                        utc = String.IsNullOrEmpty(utc) ? " unknown " : utc;

     
                        string full = display + " " + Tz;
                        if (full.TrimStart() == "")
                            full = utc;

                        if (muc)
                        {
                            // Console.WriteLine(" time muc  =>  ");
                            if (m_r.Msg.From.ToString() != m_jid.ToString())
                                answer = m_r.FormatPattern("time_muc", jid) + " " + full;
                            else
                                answer = m_r.FormatPattern("time_muc_self") + " " + full;
                        }
                        else
                        {
                            // Console.WriteLine(" version  muc =>  ");
                            answer = m_r.FormatPattern("time_server", jid) + " " + full;
                        }
                    }
                }
                else
                {
                    Console.WriteLine(" version null  =>  ");
                    answer = m_r.FormatPattern("version_error", jid);
                }

                m_r.Reply(answer);
            }
        




    }
}
