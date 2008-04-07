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
using System.IO;
using System.Diagnostics;
using Core.Conference;
using Core.Kernel;
using System.Reflection;

namespace Core.Other
{

    /// <summary>
    /// Represents current OSes, which can support C# .NET/Mono
    /// </summary>
    public enum Platform
    {
        Windows,
        Mac,
        Unix,
        Linux
    }



    static public class Utils
    {
    	
    	
    	static object obj = new object();
    	static object obj2 = new object();
    	static SessionHandler session;
    	static string m_dir;
    	
    	
        /// <summary>
        /// Directory builder for any OS
        /// </summary>
        /// <param name="parts"></param>
        /// <returns></returns>
        static public string dir(params string[] parts)
        {
            string data = "";
            int i = 0;
            foreach (string part in parts)
            {
                i++;
                if (i != 1)
                    data += Path.DirectorySeparatorChar + part;
                else
                    data += part;
            }
            return data;
        }

        /// <summary>
        /// Current directory of the process
        /// </summary>
        static public string CD
        {
            get { lock (obj2) { return m_dir; } }
            set { lock (obj2) { m_dir = value; } }
        }

        /// <summary>
        /// The platform-specific sign, used to separate directory/path units
        /// </summary>
        static public char d
        {
            get
            {
                return System.IO.Path.DirectorySeparatorChar;
            }
        }
        /// <summary>
        /// Gets current machine's Operating System's base
        /// </summary>
        static public Platform OS
        {
            get
            {
                string os = Environment.OSVersion.ToString().ToLower();
                return
                   (os.IndexOf("unix") > -1) ?
                    Platform.Unix :
                   (os.IndexOf("windows") > -1) ?
                    Platform.Windows :
                   (os.IndexOf("linux") > -1) ?
                    Platform.Linux :
                   (os.IndexOf("mac") > -1) ?
                   Platform.Mac :
                   Platform.Unix;

            }
        }


        /// <summary>
        ///  Gets the substring in "Source" - the first entering of string, specified in "Pattern"( It's has to contain (.*) which will be used like regular expression to find the needed part)
        /// You can specify as an argument true/false - if you want to get all the pattern or just the matched by "Pattern" word, which was found for (.*)
        /// </summary>
        /// <param name="Source"></param>
        /// <param name="Pattern"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        static public string GetValue(string Source, string Pattern, params object[] args)
        {
            string part1 = Pattern.Substring(0, Pattern.IndexOf("(.*)"));
            string part2 = Pattern.Substring(Pattern.IndexOf("(.*)") + 4);
            bool include;
            if (args.Length == 0)
                include = false;
            else
                try   { include = (bool)args[0]; }
                catch { include = false; };
            int posb;
            int pose;
            if (part1 == "")
                posb = 0;
            else
            {
                posb = include ? Source.IndexOf(part1) : Source.IndexOf(part1) + part1.Length;
                if (posb == -1)
                    return "";
            }

            if (part2 == "")
                pose = Source.Length;
            else
            {
                pose = include ? Source.IndexOf(part2, posb + 1) + part2.Length : Source.IndexOf(part2, posb);
                if (pose == -1)
                    return "";
            }
            if (pose >= posb)
                return Source.Substring(posb, pose - posb);
            else
                return "";

        }

        /// <summary>
        /// Removes in string "Source" the first entering of string, specified in "Pattern"( It's has to contain (.*) which will be used like regular expression to find the needed part)
        /// You can specify as an argument true/false - if you want to remove al the pattern or just the matched by "Pattern" word, which was found for (.*)
        /// </summary>
        /// <example>
        /// string data = "Hello, my job is [programmer - ingeneer]
        /// RemoveValue(data, "[(.*)]", false)
        /// or
        /// RemoveValue(data, "[(.*)]") will return a string 
        /// "Hello, my job is []"
        /// RemoveValue(data, "[(.*)]", true) will return a string 
        /// "Hello, my job is "
        /// </example>
        /// <param name="Source"></param>
        /// <param name="Pattern"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        static public string RemoveValue(string Source, string Pattern, params object[] args)
        {

            string part1 = Pattern.Substring(0, Pattern.IndexOf("(.*)"));
            string part2 = Pattern.Substring(Pattern.IndexOf("(.*)") + 4);
            string value = GetValue(Source, Pattern, args);
            if ((value.StartsWith(part1)) && (value.EndsWith(part2)))
                return Source.Remove(Source.IndexOf(value), value.Length);
            return Source.Remove(Source.IndexOf(part1 + value) + 1, value.Length);
        }


        /// <summary>
        /// Singularily replaces first entering of string "From" in string "Source" with "To" 
        /// </summary>
        /// <param name="Source"></param>
        /// <param name="From"></param>
        /// <param name="To"></param>
        /// <returns></returns>
        static public string ReplaceOnce(string Source, string From, string To)
        {
            string result = Source;
            if (string.IsNullOrEmpty(result))
                return "";
            if ((string.IsNullOrEmpty(To)) || (string.IsNullOrEmpty(To)))
                return result;
            int posb = result.IndexOf(From);
            if (posb == -1)
                return result;
            result = result.Remove(posb, From.Length).Insert(posb, To);
            return result;

        }

        /// <summary>
        /// This is SplitEx -> split words by empty-space, specifiyng maximum parts.
        /// </summary>
        /// <example>
        /// Let's take a look on this phrase:
        /// string data = "Hello, my job is programmer - ingeneer";
        /// A ussual split will return a string[6] massive with each part,delimited with emptyspace.
        /// When we use ParseWords(data,3) it will return string[3] massive, which parts will be:
        /// "Hello," "my" "job" "is programmer - ingeneer"
        /// For ParseWords(data, 2):
        /// "Hello," "my" "job is programmer - ingeneer"
        /// </example>
        /// <param name="Source"></param>
        /// <param name="Fixed"></param>
        /// <returns></returns>
        static public string[] SplitEx(string Source, int Fixed)
        {
            string[] ss = Source.Trim().Split(' ');
            int i = 0;
            string[] res;
            string _source = Source;
            foreach (string s in ss)
            {
                if (s != "")
                {
                    if (i + 1 <= Fixed)
                    {
                        i++;
                        _source = _source.Remove(0, _source.IndexOf(s) + s.Length);
                    }

                    else
                    {
                        string rr;
                        i++;
                        rr = _source.Substring(_source.IndexOf(s)).Trim();
                        break;
                    }
                }

            }
            _source = Source.Trim();
            res = new string[i];
            i = 0;
            foreach (string s in ss)
            {
                if (s != "")
                {
                    if (i + 1 <= Fixed)
                    {
                        res[i] = s.Trim();
                        i++;
                        _source = _source.Remove(0, _source.IndexOf(s) + s.Length);
                    }

                    else
                    {
                        string rr;

                        rr = _source.Substring(_source.IndexOf(s)).Trim();
                        res[i] = rr;
                        i++;
                        return res;

                    }
                }

            }

            return res;
        }


        /// <summary>
        /// A formatter for a TimeSpan, which converts ticks count into a real data/time.
        /// </summary>
        /// <param name="Ticks"></param>
        /// <param name="r"></param>
        /// <returns></returns>
        static public string FormatTimeSpan(long Ticks, Response r)
        {
            TimeSpan ts = new TimeSpan(Ticks);
            double m = System.Math.Truncate((double)ts.Days / 30);
            @out.exe(Ticks.ToString());
            return
                                      (ts.Days > 30 ? Convert.ToString(m) + " "+r.f("month") + " " : "") +
                                      (ts.Days-m*30 > 0 ? (ts.Days - m*30).ToString() + " " + r.f("day") + " " : "") +
                                      (ts.Hours > 0 ? ts.Hours.ToString() + " " + r.f("hour") + " " : "") +
                                      (ts.Minutes > 0 ? ts.Minutes.ToString() + " " + r.f("minute") + " " : "") +
                                      (ts.Seconds > 0 ? ts.Seconds.ToString() + " " + r.f("second") + " " : "");

        }

        /// <summary>
        /// Formats escape sequences into phrases.
        /// </summary>
        /// <param name="Source"></param>
        /// <param name="muc"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        static public string FormatEnvironmentVariables(string Source, MUC muc, MUser user)
        {
            if (Source == null)
                return null;
            string res = Source
                         .Replace("{NICK}", user.Nick)
                         .Replace("{MYSERVER}", muc.Connection.Server)
                         .Replace("{ROOM}", muc.Jid.ToString())
                         .Replace("{JID}", user.Jid.Bare)
                         .Replace("{STATUS}", user.Status)
                         .Replace("{LANG}", user.Language)
                         .Replace("{ACCESS}", user.Access.ToString())
                         .Replace("{DATE}", DateTime.Now.Date.ToString("d"))
                         .Replace("{TIME}", DateTime.Now.ToString("HH:mm:ss"))
                         .Replace("{DAY}", DateTime.Now.ToString("dd"))
                         .Replace("{MINUTE}", DateTime.Now.ToString("mm"))
                         .Replace("{SECOND}", DateTime.Now.ToString("ss"))
                         .Replace("{YEAR}", DateTime.Now.ToString("yyyy"))
                         .Replace("{MONTH}", DateTime.Now.ToString("MM"))
                         .Replace("{DAYOFWEEK}", DateTime.Now.ToString("dddd"))
                         .Replace("{DATETIME}", DateTime.Now.ToString())
                         .Replace("{MYNICK}", muc.MyNick);
            if (muc.Subject != null)
                res = res.Replace("{SUBJECT}", muc.Subject);
            return res;


        }
    }
}