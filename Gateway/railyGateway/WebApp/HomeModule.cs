// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

﻿using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using libShared.Web;
using Microsoft.AspNetCore.Http;

namespace railyGateway.WebApp
{
    public class HomeModule
    {
        public static string DefaultFile = "index.html";

        public static IReadOnlyList<string> FilesForSubstitution = new List<string>()
        {
            DefaultFile,
            "index.js"
        };

        public static async Task HandleFiles(HttpContext context)
        {
            var filepath = context?.Request?.Path.Value?.TrimStart('/');
            if (string.IsNullOrEmpty(filepath)) filepath = DefaultFile;

            var fullPath = Path.Combine(Globals.HttpRootDir, filepath);
            var infoPath = new FileInfo(fullPath);

            if (infoPath.Exists)
            {
                byte[] fileBytes;

                if (libUtilities.Filesystem.IsSubst(infoPath.FullName, FilesForSubstitution))
                {
                    var subst = new Dictionary<string, string> {
                            { "##QRCODE##", QrCodeGenerator.Base64QrCode},
                            { "{{Author}}", "riesolution - Dr. Christian Benjamin Ries" },
                            { "{{ServiceName}}", "railhq.io" },
                            { "{{ApplicationName}}", "railhq.io - Gateway" },
                            { "{{ApplicationDescription}}", "This is railhq.io Gateway!" },
                            { "{{GENERATE_NET_HOST}}", QrCodeGenerator.InternetIpAddress},
                            { "\"##GENERATE_NET_PORT##\"", $"{QrCodeGenerator.InternetIpPort}"},
                            { "(##GENERATE_NET_PORT##)", $"{QrCodeGenerator.InternetIpPort}"}
                        };

                    var cnt = await File.ReadAllTextAsync(infoPath.FullName, Encoding.UTF8);

                    foreach (var it in subst)
                        cnt = cnt.Replace(it.Key, it.Value);

                    fileBytes = Encoding.UTF8.GetBytes(cnt);
                }
                else
                {
                    fileBytes = await File.ReadAllBytesAsync(infoPath.FullName);
                }

                var fileExtension = Path.GetExtension(infoPath.FullName);
                var contentType = ContentType.GetContentType(fileExtension);

                context.Response.ContentType = contentType;
                context.Response.Headers["Content-Type"] = $"{contentType}; charset=utf-8";
                await context.Response.Body.WriteAsync(fileBytes);
            }
            else
            {
                context.Response.StatusCode = 404;
                await context.Response.WriteAsync($"File not found: {filepath}");
            }
        }
    }
}
