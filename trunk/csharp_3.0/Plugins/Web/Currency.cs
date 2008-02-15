﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Net;
using System.IO;
using Core.Client;
using agsXMPP.Xml.Dom;


namespace www
{
    class Currency
    {
        public Currency()
        {

        }



        public string GetList()
        {
            HttpWebRequest wr = (HttpWebRequest)HttpWebRequest.CreateDefault(new System.Uri("http://www.xe.com/ucc/full.php"));
            wr.Method = "GET";
            wr.ContentType = "application/x-www-form-urlencoded";
            WebResponse _wp = wr.GetResponse();
            StreamReader sr = new StreamReader(_wp.GetResponseStream());
            Document doc = new Document();
            doc.LoadXml("<Currency>" + sr.ReadToEnd().GetValue("<select(.*)/select>", true) + "</Currency>");

            string data = "";

            foreach (Element el in doc.RootElement.SelectSingleElement("select").SelectElements("option"))
            {
                data += el.Value + "\n";
            }


            return data;
            


        }

        public string Find(string tip)
        {
            HttpWebRequest wr = (HttpWebRequest)HttpWebRequest.CreateDefault(new System.Uri("http://www.xe.com/ucc/full.php"));
            wr.Method = "GET";
            wr.ContentType = "application/x-www-form-urlencoded";
            WebResponse _wp = wr.GetResponse();
            StreamReader sr = new StreamReader(_wp.GetResponseStream());
            Document doc = new Document();
            doc.LoadXml("<Currency>" + Utils.GetValue(sr.ReadToEnd(), "<select(.*)/select>", true) + "</Currency>");

          //  string data = "";

            foreach (Element el in doc.RootElement.SelectSingleElement("select").SelectElements("option"))
            {
                if (el.Value.ToLower().IndexOf(tip.ToLower()) > -1)
                    return el.Value;
            }


            return null;



        }
        public string Handle(string from, string to, int amount)
        {
            HttpWebRequest wr = (HttpWebRequest)HttpWebRequest.CreateDefault(new System.Uri("http://www.xe.com/ucc/convert.cgi?Amount="+amount+"&From="+from.ToUpper()+"&To="+to.ToUpper()));
            wr.Method = "GET";
            wr.ContentType = "application/x-www-form-urlencoded";
            WebResponse _wp = wr.GetResponse();
            StreamReader sr = new StreamReader(_wp.GetResponseStream());

            string data = "";
            Regex reg = new Regex("<h2 class=\"XE\">(.*)<!--");
            MatchCollection mc = reg.Matches(sr.ReadToEnd());
            data = Utils.GetValue(mc[0].ToString(), "<h2 class=\"XE\">(.*)<!--");
            data += " = " + Utils.GetValue(mc[1].ToString(), "<h2 class=\"XE\">(.*)<!--");
            return data;
        }



    }


}
