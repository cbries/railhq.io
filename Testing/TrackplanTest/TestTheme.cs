// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

﻿using System.Text;
using FluentAssertions;
using HtmlTags;
using libTrackplan.Theming;
using libUtilities;
using Newtonsoft.Json;

namespace TrackplanTest
{
    [TestClass]
    public class TestTheme
    {
        public TestContext TestContext { get; set; }
        private string BaseDir => TestContext.TestRunDirectory;
        private string FindSolutionDirectory()
        {
            return Filesystem.FindSolutionDirectory(BaseDir);
        }

        [TestMethod]
        public void TestIconsRendering()
        {
            var path = System.IO.Path.Combine(FindSolutionDirectory(), @"resources", "theme", "RailwayEssential.json");
            var pathToFiles = System.IO.Path.Combine(FindSolutionDirectory(), @"resources", "theme", @"RailwayEssential");
            var targetHtmlFile = Path.Combine(Path.GetDirectoryName(path), "RailwayEssential.Test.html");
            File.Exists(path).Should().BeTrue();

            var json = File.ReadAllText(path, Encoding.UTF8);
            var itm = JsonConvert.DeserializeObject<Theme>(json);
            itm.ThemeItems.Count.Should().Be(6);

            var body = new HtmlTag("body");

            foreach (var it in itm.ThemeItems)
            {
                var table = body.Add("table");
                var tableBodyTag = table.Add("tbody");

                var titleTag = tableBodyTag.Add("tr/td")
                    .Attr("colspan", 4)
                    .Text(it.Category);
                titleTag.Style("font-weight", "bold");
                titleTag.Style("font-size", "1.25em");

                foreach (var itt in it.Objects)
                {
                    if (itt.Id <= -1) continue;
                    var activeName = itt.BaseName;
                    var pathToFile = Path.Combine(pathToFiles, activeName + ".png");

                    var trTag = tableBodyTag.Add("tr");
                    var trTag2 = tableBodyTag.Add("tr");

                    var wfactor = 1;
                    var hfactor = 1;
                    var noOfRoutes = 1;

                    if (itt.Dimensions != null && itt.Dimensions.Count > 0)
                    {
                        wfactor = itt.Dimensions[0].W;
                        hfactor = itt.Dimensions[0].H;
                    }

                    if (itt.Routes != null && itt.Routes.Count > 0)
                        noOfRoutes = itt.Routes.Count;

                    var width = 32 * wfactor;
                    var height = 32 * hfactor;

                    const int rotDegreeStep = 90;

                    for (var i = 0; i < noOfRoutes; ++i)
                    {
                        var rotDegree = i * rotDegreeStep;

                        var imgTag = new HtmlTag("img");
                        imgTag.Attr("width", width);
                        imgTag.Attr("height", height);
                        imgTag.Attr("src", pathToFile);

                        if (wfactor == 1 && hfactor == 1)
                            imgTag.Style("transform-origin", "center center");
                        else
                            // https://css-tricks.com/almanac/properties/t/transform-origin/
                            imgTag.Style("transform-origin", "top left");
                        imgTag.Style("transform", $"rotate({rotDegree}deg)");
                        imgTag.Style("border", "1px solid rgba(0,0,0,0.2)");

                        var tdTag = trTag.Add("td");

                        tdTag
                            .Attr("height", width > height ? width : height)
                            .Attr("width", width > height ? width : height)
                            .Style("padding", "50px")
                            //.Style("border", "1px solid black")
                            .Append(imgTag);

                        trTag2.Add("td")
                            .Style("text-align", "center")
                            .Text(itt.Name + "\n" + itt.Id);
                    }
                }
            }

            var html = body.ToString();

            File.WriteAllText(targetHtmlFile, html, Encoding.UTF8);
        }
    }
}
