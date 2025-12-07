// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

﻿using libTrackplan.Plan;
using System;
using System.Collections.Generic;
using System.Xml;

namespace Converter.Rocrail.Xml
{
    public class Section
    {
        public TrackElement Owner { get; set; }

        public string Id { get; set; } = string.Empty;
        public string FbId { get; set; } = string.Empty;
        public string FbIdOcc { get; set; } = string.Empty;
        public int Length { get; set; } = 0;
        public int Index { get; set; } = 0;

        public bool Parse(XmlNode node)
        {
            if (node == null) return false;
            Id = node.Attributes.GetString("id");
            FbId = node.Attributes.GetString("fbid");
            FbIdOcc = node.Attributes.GetString("fbidocc");
            Length = node.Attributes.GetInt("len", 0);
            Index = node.Attributes.GetInt("idx", 0);
            return true;
        }
    }

    /*
    
    <sb ori="west" id="AB_2" x="30" y="5" z="0" reserved="false" entering="false" state="open" totallength="444" totalsections="4" prev_id="AB_2" desc="" slen="111" gap="10" fbenterid="fb_Enter_AB_2" entersignal="" exitsignal="" randomrate="10" departdelay="0" minocc="0" minoccsec="0" waitmode="random" minwaittime="5" maxwaittime="15" waittime="5" exitspeed="cruise" stopspeed="mid" speedpercent="20" exitspeedpercent="80" suitswell="true" inatlen="true" usewd="true" wdsleep="1" movetimeout="0" electrified="false" smallsymbol="false" stopspeedtolastsection="false" vmintofirstsection="false" typeperm="" engine="" era="0" class="" maxlen="0" minlen="65" allowchgdir="false" exitstate="closed" locid="" cleanstamp="0">
      <section id="AB_2_Sektion_1" fbid="fb_in1_AB_2" fbidocc="fb_be1_AB_2" lcid="BR_74_854" len="111" nr="0" idx="0"/>
      <section id="AB_2_Sektion_2" fbid="fb_in2_AB_2" fbidocc="fb_be2_AB_2" lcid="BR_75_057" len="111" nr="1" idx="1"/>
      <section id="AB_2_Sektion_3" fbid="fb_in3_AB_2" fbidocc="fb_be3_AB_2" lcid="BR_18_128" len="111" nr="2" idx="2"/>
      <section id="AB_2_Sektion_4" fbid="fb_in4_AB_2" fbidocc="fb_be4_AB_2" lcid="BR_216_025_7" len="111" nr="3" idx="3"/>
      <fbevent id="fb_Enter2_AB2" action="in" from="all-reverse"/>
      <fbevent id="fb_Enter2_AB2" action="enter" from="all"/>
    </sb>

     */

    public class StagingElement : TrackElement
    {
        public List<Section> Sections { get; set; } = new();
        public List<FbEvent> FbEvents { get; } = new();

        public StagingElement()
        {
            ElementType = PlanItemT.StagingBlock;
        }

        public string Id { get; private set; } = string.Empty;

        public override bool ParseXmlNode(XmlNode node)
        {
            var res = base.ParseXmlNode(node);
            if (!res) return false;

            var attr = node.Attributes;
            if (attr == null) return false;

            Id = attr.GetString("id");
            
            if (node.HasChildNodes)
            {
                foreach (var itChild in node.ChildNodes)
                {
                    if (itChild is not XmlNode nodeChild) continue;
                    if (string.IsNullOrEmpty(nodeChild.Name)) continue;
                    if (!nodeChild.Name.Equals("fbevent", StringComparison.OrdinalIgnoreCase)) continue;

                    var instance = new FbEvent();
                    if (instance.Parse(nodeChild))
                    {
                        instance.Owner = this;
                        FbEvents.Add(instance);
                    }
                }

                foreach (var itChild in node.ChildNodes)
                {
                    if (itChild is not XmlNode nodeChild) continue;
                    if (string.IsNullOrEmpty(nodeChild.Name)) continue;
                    if (!nodeChild.Name.Equals("section", StringComparison.OrdinalIgnoreCase)) continue;

                    var instance = new Section();
                    if (instance.Parse(nodeChild))
                    {
                        instance.Owner = this;
                        Sections.Add(instance);
                    }
                }
            }

            return true;
        }
    }
}
