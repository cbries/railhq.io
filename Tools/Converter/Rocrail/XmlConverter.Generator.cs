// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: XmlConverter.Generator.cs

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Converter.Rocrail.Xml;
using libTrackplan.Plan;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Converter.Rocrail
{
    public class SensorItem
    {
        [JsonProperty("name")] public string Name { get; set; } = string.Empty;
        [JsonProperty("provider")] public string Provider { get; set; } = "ecos";
        [JsonProperty("address")] public string Address { get; set; } = "-1";
    }

    public partial class XmlConverter
    {
        private bool GenerateMetamodel(out string errorMessage)
        {
            var planfield = new JObject();

            var allElements = new List<TrackElement>();
            allElements.AddRange(TrackElements);
            allElements.AddRange(SignalElements);
            allElements.AddRange(FeedbackElements);
            allElements.AddRange(SwitchElements);
            allElements.AddRange(BlockElements);
            allElements.AddRange(StagingElements);
            allElements.AddRange(TextElements);
            allElements.AddRange(CoElements);

            var connectorCounter = 0;
            var trackCounter = 0;
            var signalCounter = 0;
            var feedbackCounter = 0;
            var switchCounter = 0;
            var blockCounter = 0;
            var textCounter = 0;
            var coCounter = 0;

            var sensorList = new List<SensorItem>();

            foreach (var it in allElements)
            {
                var targetKey = $"{it.X}x{it.Y}";

                var o = new JObject();
                var themeId = 0;
                string suffix;

                if (it.ElementType == PlanItemT.Tk)
                {
                    themeId = GetThemeIdByType(it);
                    if (themeId == 17) // connector
                    {
                        suffix = "Connector_";
                        o["identifier"] = $"{suffix}{connectorCounter}";
                        ++connectorCounter;
                    }
                    else
                    {
                        suffix = "TE_";
                        o["identifier"] = $"{suffix}{trackCounter}";
                        ++trackCounter;
                    }
                }
                else if (it.ElementType == PlanItemT.Fb)
                {
                    themeId = GetThemeIdByType(it);
                    suffix = "FB_";
                    o["identifier"] = $"{suffix}{feedbackCounter}";
                    ++feedbackCounter;
                }
                else if (it.ElementType == PlanItemT.Sg)
                {
                    themeId = GetThemeIdByType(it);
                    suffix = "SE_";
                    o["identifier"] = $"{suffix}{signalCounter}";
                    ++signalCounter;
                }
                else if (it.ElementType == PlanItemT.Sw)
                {
                    themeId = GetThemeIdByType(it);
                    suffix = "SW_";
                    o["identifier"] = $"{suffix}{switchCounter}";
                    ++switchCounter;
                }
                else if (it.ElementType == PlanItemT.Bk)
                {
                    themeId = GetThemeIdByType(it);
                    suffix = "BK_";
                    o["identifier"] = $"{suffix}{blockCounter}";
                    ++blockCounter;
                }
                else if (it.ElementType == PlanItemT.StagingBlock)
                {
                    themeId = GetThemeIdByType(it);
                    suffix = "SB_";
                    o["identifier"] = $"{suffix}{blockCounter}";
                    ++blockCounter;
                }
                else if (it.ElementType == PlanItemT.Tx)
                {
                    themeId = GetThemeIdByType(it);
                    suffix = "TX_";
                    o["identifier"] = $"{suffix}{textCounter}";
                    ++textCounter;
                }
                else if (it.ElementType == PlanItemT.Co)
                {
                    themeId = GetThemeIdByType(it);
                    suffix = "CO_";
                    o["identifier"] = $"{suffix}{coCounter}";
                    ++coCounter;
                }
                else
                {
                    Console.WriteLine($"Unknown element type: {it.ElementType}");
                }

                if (themeId == 0) continue;

                if (!string.IsNullOrEmpty(it.Identifier))
                    o["identifier"] = it.Identifier.Trim();

                o["coord"] = new JObject
                {
                    ["x"] = it.X,
                    ["y"] = it.Y
                };

                if (it.ElementType == PlanItemT.Fb)
                {
                    if (it is FeedbackElement itFb)
                    {
                        var fbIdentifier = itFb.Identifier;
                        var s88Idx = itFb.Address.Addr;

                        sensorList.Add(new SensorItem
                        {
                            Name = fbIdentifier,
                            Address = $"{s88Idx}"
                        });
                    }
                }

                var themeDimIdx = GetRotationBy(it);
                var hackToFixThemeDimIdx = new List<int>
                {
                    10, 200, 255
                };
                if (hackToFixThemeDimIdx.Contains(themeId))
                {
                    if (themeDimIdx == 2) themeDimIdx = 0;
                    else if (themeDimIdx == 3) themeDimIdx = 1;
                }

                var editor = new JObject
                {
                    ["themeId"] = themeId,
                    ["themeDimIdx"] = themeDimIdx
                };

                if (it is TextElement txtEl)
                {
                    var txt = txtEl.Text;
                    if (txtEl.IsBold) txt = "<b>" + txt + "</b>";
                    if (txtEl.IsItalic) txt = "<i>" + txt + "</i>";
                    if (txtEl.IsUnderline) txt = "<u>" + txt + "</u>";

                    editor["innerHtml"] = txt;
                    editor["fontSize"] = "14px;";
                }
                if (it.ConnectorId > 1)
                {
                    editor["connectorId"] = it.ConnectorId;
                }
                o["editor"] = editor;

                planfield[targetKey] = o;
            }

            var settingsJson = new JObject
            {
                ["blockSensors"] = GenerateFbEvents(out errorMessage),
                ["sensors"] = JArray.FromObject(sensorList),
                ["accessories"] = GenerateAccessories(planfield, out errorMessage)
            };

            var targetDir = System.IO.Path.Combine(_outputDirectory, GenerateFolderName());
            try
            {
                System.IO.Directory.CreateDirectory(targetDir);
            }
            catch
            {
                // ignore
            }

            var planPath = System.IO.Path.Combine(targetDir, "planfield.json");
            var settingsPath = System.IO.Path.Combine(targetDir, "settings.json");
            var routesPath = System.IO.Path.Combine(targetDir, "routes.json");

            var strJson = planfield.ToString(Formatting.Indented);
            strJson.FixBomIfNeeded();
            var r1 = StringUtilities.WriteAllTextNoBom(planPath, strJson, out errorMessage);

            var strJson2 = settingsJson.ToString(Formatting.Indented);
            strJson2.FixBomIfNeeded();
            var r2 = StringUtilities.WriteAllTextNoBom(settingsPath, strJson2, out errorMessage);

            // dummy creation
            var r3 = StringUtilities.WriteAllTextNoBom(routesPath, "[]", out errorMessage);

            return r1 && r2 && r3;
        }

        public static string GenerateFolderName()
        {
            return $"{DateTime.Now:yyyyMMdd_HH_mm_ss}-Import";
        }

        private string GetPlanfieldItemId(JObject planfield, int x, int y)
        {
            foreach (var it in planfield)
            {
                var targetKey = $"{x}x{y}";
                if (it.Key.Equals(targetKey, StringComparison.OrdinalIgnoreCase))
                {
                    return (it.Value as JObject)?["identifier"]?.ToString() ?? string.Empty;
                }
            }

            return string.Empty;
        }

        public JArray GenerateAccessories(JObject planfield, out string errorMessage)
        {
            errorMessage = string.Empty;
            var accs = new JArray();

            foreach (var it in SwitchElements)
            {
                var info = new JObject
                {
                    ["accessoryDriver"] = "ecos",
                    ["accessoryIdentifier"] = it.Identifier,
                    ["planfieldControlIdentifier"] = GetPlanfieldItemId(planfield, it.X, it.Y)
                };

                accs.Add(info);
            }

            foreach (var it in SignalElements)
            {
                var info = new JObject
                {
                    ["accessoryDriver"] = "ecos",
                    ["accessoryIdentifier"] = it.Identifier,
                    ["planfieldControlIdentifier"] = GetPlanfieldItemId(planfield, it.X, it.Y)
                };

                accs.Add(info);
            }

            return accs;
        }

        public JArray GenerateFbEvents(out string errorMessage)
        {
            errorMessage = string.Empty;
            var events = new JArray();

            foreach (var itBlock in BlockElements)
            {
                if (itBlock == null) continue;

                var fromFbs = new List<FbEvent>();
                var toFbs = new List<FbEvent>();

                foreach (var itFbEvent in itBlock.FbEvents)
                {
                    if (itFbEvent == null) continue;

                    var blockId = (itFbEvent.Owner as BlockElement)?.Id ?? string.Empty;
                    var from = itFbEvent.From;

                    if (string.IsNullOrEmpty(blockId)) continue;
                    if (string.IsNullOrEmpty(from)) continue;

                    if (from.Equals("all", StringComparison.OrdinalIgnoreCase))
                        fromFbs.Add(itFbEvent);
                    else if (from.Equals("all-reverse", StringComparison.OrdinalIgnoreCase))
                        toFbs.Add(itFbEvent);
                }

                var oplus = new JObject
                {
                    ["identifier"] = itBlock.Id + "[+]",
                    ["isEnabled"] = true,
                    ["isLocked"] = false
                };
                var ominus = new JObject
                {
                    ["identifier"] = itBlock.Id + "[-]",
                    ["isEnabled"] = true,
                    ["isLocked"] = false
                };

                if (fromFbs.Count == 2 && toFbs.Count == 2)
                {
                    if (fromFbs[0].Action.Equals("enter", StringComparison.OrdinalIgnoreCase))
                    {
                        oplus["sensorEnter"] = fromFbs[0].FbId;
                        oplus["sensorIn"] = fromFbs[1].FbId;
                    }
                    else
                    {
                        oplus["sensorIn"] = fromFbs[0].FbId;
                        oplus["sensorEnter"] = fromFbs[1].FbId;
                    }

                    if (toFbs[0].Action.Equals("in", StringComparison.OrdinalIgnoreCase))
                    {
                        ominus["sensorIn"] = toFbs[0].FbId;
                        ominus["sensorEnter"] = toFbs[1].FbId;
                    }
                    else
                    {
                        ominus["sensorEnter"] = toFbs[0].FbId;
                        ominus["sensorIn"] = toFbs[1].FbId;
                    }
                }

                events.Add(oplus);
                events.Add(ominus);
            }

            return events;
        }
    }
}
