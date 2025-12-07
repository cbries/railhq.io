// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: FeedbackElement.cs

using System;
using System.Xml;
using libTrackplan.Plan;

namespace Converter.Rocrail.Xml
{
    /*
    <fb accnr="4" id="fb+_BK_9_kurz" x="16" y="18" z="0" state="false" identifier="" regval="0" gpssid="0" clone="false" shortcut="false" counter="23" wheelcount="0" carcount="0" countedcars="0" bididir="0" val="0" load="0" maxload="0" prev_id="fb+_BK_9_kurz" desc="" decid="" show="true" showid="true" road="false" curve="false" ignoresamestate="false" blockid="BK_9_kurz" routeids="[BK_Wendel_Aussen-]-[BK_Wendel_Innen-],[BK_Wendel_Innen-]-[BK_Wendel_Aussen-],[BK_Wendel_Aussen-]-[BK_9+],[BK_Wendel_Aussen-]-[BK_9_kurz+],[BK_9+]-[BK_Wendel_Aussen-],[BK_9_kurz+]-[BK_Wendel_Aussen-]" timer="0" zerocodedelay="0" operable="true" generated="false" iid="ecos" uidname="" rfid="0" bus="0" addr="93" reg0="0" reg1="0" reg2="0" reg3="0" reg4="0" reg5="0" reg6="0" reg7="0" threshold="1" offset="" baseaddr="0" cutoutbus="0" cutoutaddr="0" subscribe="" fbtype="0" regtrigger="0" activelow="false" resetwc="false" ctciid="" ctcbus="0" ctcaddr="0" ctcport="0" ctcgate="0" ctcasswitch="false" ctcOutput="" ctcColorOn="0" ctcColorOff="0" gpsx="0" gpsy="0" gpsz="0" gpstolx="0" gpstoly="0" gpstolz="0" actor=""/>
     */

    public class FeedbackElement : TrackElement
    {
        public int Val { get; set; }
        public int Load { get; set; }
        public int Regval { get; set; }
        public int Maxload { get; set; }
        public int Baseaddr { get; set; }
        public int Offset { get; set; }
        public PortAddress Address { get; set; } = new();
        //public new string BlockId { get; set; }
        public int Bididir { get; set; }
        public string State { get; set; } // true, false
        public bool IsCurve { get; set; } = false;

        public FeedbackElement()
        {
            ElementType = PlanItemT.Fb;
        }

        public override bool ParseXmlNode(XmlNode node)
        {
            var res = base.ParseXmlNode(node);
            if (!res) return false;

            var attr = node.Attributes;
            if (attr == null) return false;

            if (attr["val"] != null)
            {
                var v = attr["val"].Value;
                if(!string.IsNullOrEmpty(v))
                    Val = int.Parse(v);
            }

            if (attr["load"] != null)
            {
                var v = attr["load"].Value;
                if (!string.IsNullOrEmpty(v))
                    Load = int.Parse(v);
            }
            
            if (attr["regval"] != null)
            {
                var v = attr["regval"].Value;
                if (!string.IsNullOrEmpty(v)) 
                    Regval = int.Parse(v);
            }
            
            if (attr["maxload"] != null)
            {
                var v = attr["maxload"].Value;
                if (!string.IsNullOrEmpty(v)) 
                    Maxload = int.Parse(v);
            }
            
            if (attr["baseaddr"] != null)
            {
                var v = attr["baseaddr"].Value;
                if (!string.IsNullOrEmpty(v)) 
                    Baseaddr = int.Parse(v);
            }

            if (attr["offset"] != null)
            {
                var v = attr["offset"].Value;
                if(!string.IsNullOrEmpty(v))
                    Offset = int.Parse(v);
            }

            Address.Parse(attr);

            BlockId = attr["blockid"]?.Value ?? string.Empty;
            
            if (attr["bididir"] != null)
                // ReSharper disable once ConstantNullCoalescingCondition
                Bididir = int.Parse(attr["bididir"]?.Value ?? "0");

            State = attr["state"]?.Value;

            if(attr["curve"] != null)
                IsCurve = attr["curve"].Value.Equals("true", StringComparison.OrdinalIgnoreCase);

            return true;
        }
    }
}
