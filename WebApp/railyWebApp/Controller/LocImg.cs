// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using libShared;
using libUserspace;
using libUtilities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using railyWebApp.Controller.Helper;
using SkiaSharp;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
// ReSharper disable RedundantNameQualifier
// ReSharper disable ConvertToPrimaryConstructor

#pragma warning disable CS0618 // Type or member is obsolete

namespace railyWebApp.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class LocImg : RailhqControllerBase
    {
        public const string SharedName = "Shared";
        public const string LocomotivesSubdirName = "Locomotives";
        public const int MaxFileUploadSizeKb = 512; // 512 kB Limit
        public const string DefaultFileExt = ".avif";
        public const string DefaultFileExt2 = ".png";

        public LocImg(SupabaseService authService, IMemoryCache cache) : base(authService, cache)
        {
            // ignore
        }

        private async Task<string> QueryUserDirectoryForImages()
        {
            try
            {
                var user = await ValideRequestBearer(HttpContext);
                return Path.Combine(libUserspace.Filesystem.UserImagesBaseDir, user?.Id ?? SharedName);
            }
            catch (Exception)
            {
                // ignore
            }

            //
            // only shared images are accessible
            //
            return Path.Combine(libUserspace.Filesystem.UserImagesBaseDir, SharedName);
        }

        private string CleanupFilename(IFormFile file)
        {
            if (file == null) return string.Empty;
            if (string.IsNullOrEmpty(file.FileName)) return string.Empty;
            var name = file.FileName;
            return FileNameHelper.ToWebSafeFileName(name);
        }

        private string GetExt(IFormFile file)
        {
            if (file == null) return string.Empty;
            if (string.IsNullOrEmpty(file.FileName)) return string.Empty;
            var name = file.FileName;
            return System.IO.Path.GetExtension(name);
        }

        private FileContentResult GetFallbackImage()
        {
            var data = Helpers.GetFallbackImage();
            var bytes = data.ToArray();
            data.Dispose();
            return File(bytes, "image/png");
        }
        
        static JObject GetImageInfo(string imagePath)
        {
            try
            {
                if (RailEnvironment.IsRunningInContainer())
                {
                    var fileInfo = new FileInfo(imagePath);

                    using var input = System.IO.File.OpenRead(imagePath);
                    using var codec = SKCodec.Create(input);
                    var info = codec.Info;

                    return new JObject
                    {
                        ["fileSize"] = fileInfo.Length,
                        ["width"] = info.Width,
                        ["height"] = info.Height,
                        ["format"] = codec.EncodedFormat.ToString()
                    };
                }
                else
                {
                    var fileInfo = new FileInfo(imagePath);
                    using var image = System.Drawing.Image.FromFile(imagePath);

                    return new JObject
                    {
                        ["fileSize"] = fileInfo.Length,
                        ["width"] = image.Width,
                        ["height"] = image.Height,
                        ["format"] = image.RawFormat.ToString()
                    };
                }
            }
            catch (Exception ex)
            {
                Logging.ExceptionLog(ex);
            }

            return [];
        }

        [HttpGet("sharedImageList/{filename?}")]
        public async Task<IActionResult> GetSharedImageList(string filename)
        {
            try
            {
                var entries = new JArray();
                var pathToSharedLocomotives = Path.Combine(libUserspace.Filesystem.UserImagesBaseDir, "Shared", "Locomotives");
                if (!Directory.Exists(pathToSharedLocomotives))
                    return Ok(entries.ToString(Formatting.None));

                //
                // Prüfe ob `filename` gesetzt ist, wenn ja, dann soll ein Bild aus
                // dem Shared-Bereich geladen und dem Anwender übergeben werden.
                //
                if (!string.IsNullOrEmpty(filename))
                {
                    var fileToLoad = Path.Combine(pathToSharedLocomotives, filename);
                    if (!fileToLoad.EndsWith(".png")) fileToLoad += ".png";
                    if (!System.IO.File.Exists(fileToLoad))
                    {
                        return GetFallbackImage();
                    }

                    var imageBytes = await System.IO.File.ReadAllBytesAsync(fileToLoad);
                    var mimeType = Helpers.GetMimeType(fileToLoad);
                    return File(imageBytes, mimeType);
                }
                else
                {

                    var itRunner = Directory.EnumerateFileSystemEntries(pathToSharedLocomotives, "*.png", SearchOption.TopDirectoryOnly);
                    foreach (var itEntry in itRunner)
                    {
                        try
                        {
                            var imageBytes = await System.IO.File.ReadAllBytesAsync(itEntry);
                            var base64 = Convert.ToBase64String(imageBytes);
                            if (string.IsNullOrEmpty(base64)) continue;
                            var base64WithPrefix = "data:image/png;base64," + base64;
                            var info = GetImageInfo(itEntry);
                            var name = Path.GetFileNameWithoutExtension(itEntry);
                            var entry = new JObject
                            {
                                { "name", name },
                                { "info", info },
                                { "data", base64WithPrefix }
                            };

                            entries.Add(entry);
                        }
                        catch
                        {
                            // ignore
                        }
                    }

                    return Ok(entries.ToString(Formatting.None));
                }
            }
            catch (Exception ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
        }

        [HttpGet("file/{filename}")]
        public async Task<IActionResult> GetImage(
            string filename,
            [FromQuery] string fallback = null)
        {
            if (string.IsNullOrEmpty(filename))
                return GetFallbackImage();

            try
            {
                // set fallback when not provided by app
                if (string.IsNullOrEmpty(fallback))
                {
                    var cleanname = FilesystemHelper.RemoveKnownExtension(filename, out _);
                    fallback = Crypto.GenerateSHA256Hash(cleanname);
                }

                var dirpath = string.Empty;
                var uid = GetCachedValue(SessionGlobals.AuthUid, HttpContext);
                if (string.IsNullOrEmpty(uid))
                {
                    // Wenn leer, dann prüfen wir ob in der URL diese
                    // direkt übergeben wurde; nicht ideal für die
                    // Sicherheit, aber das können wir später noch 
                    // anpassen -- kritisch ist es allemal nicht.
                    var queryUid = HttpContext.Request.Query["uid"];
                    if (!string.IsNullOrEmpty(queryUid))
                    {
                        uid = queryUid;
                    }
                }
                if (!string.IsNullOrEmpty(uid))
                    dirpath = Path.Combine(libUserspace.Filesystem.UserImagesBaseDir, uid);
                if (string.IsNullOrEmpty(dirpath))
                    dirpath = await QueryUserDirectoryForImages();
                
                var ext = Path.GetExtension(filename);
                if (string.IsNullOrEmpty(ext)) filename += DefaultFileExt;
                var filepath = Path.Combine(dirpath, LocomotivesSubdirName, filename);
                
                //
                // prüfe ob die Bilddatei existiert, wenn nicht
                // dann versuchen wir es nochmal mit dem Fallback
                // und im allerletzten Fall rufen wir ein Dummy auf
                //
                if (!System.IO.File.Exists(filepath)
                    && !string.IsNullOrEmpty(fallback))
                {
                    if (!fallback.EndsWith(ext))
                        fallback += ext;

                    filepath = Path.Combine(dirpath, LocomotivesSubdirName, fallback);
                }
                
                if (!System.IO.File.Exists(filepath))
                {
                    // .avif does not exist
                    // check for .png
                    filename = filename.ReplaceFileExtension(DefaultFileExt, DefaultFileExt2);
                    filepath = Path.Combine(dirpath, LocomotivesSubdirName, filename);
                    if (!System.IO.File.Exists(filepath))
                        return GetFallbackImage();
                }

                var imageBytes = await System.IO.File.ReadAllBytesAsync(filepath);
                var mimeType = Helpers.GetMimeType(filepath);
                return File(imageBytes, mimeType);
            }
            catch (Exception ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
        }

        [RequestSizeLimit(MaxFileUploadSizeKb * 1024)] // 512 KB Limit
        [HttpPost("locup")]
        public async Task<IActionResult> UploadLocomotiveImage([FromForm] IFormFile file, [FromForm] string fileName)
        {
            try
            {
                if (file == null || file.Length == 0)
                    return BadRequest(new { error = "no file uploaded" });

                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                var fileExtension = Path.GetExtension(file.FileName).ToLower();
                if (!allowedExtensions.Contains(fileExtension)) return BadRequest("file format not supported, allowed types are: JPG, PNG");
                if (!file.ContentType.StartsWith("image/")) return BadRequest("file format not supported, allowed types are: JPG, PNG");
                if (file.Length > MaxFileUploadSizeKb * 1024) return BadRequest($"file size limit reached: {MaxFileUploadSizeKb} kB");

                //
                // check path, no images can be uploaded to "Shared"
                //
                var dirpath = await QueryUserDirectoryForImages();
                if (dirpath.EndsWith(SharedName))
                    throw new Exception($"your session does not allow file upload to locomotive image store");

                var uploadPath = Path.Combine(dirpath, "Locomotives");
                if (!Directory.Exists(uploadPath))
                    Directory.CreateDirectory(uploadPath);

                var urlPathDefault = string.Empty;
                var urlPathHash = string.Empty;

                try
                {
                    urlPathDefault = await SaveFile(file, fileName, uploadPath);
                }
                catch (Exception)
                {
                    // ignore
                }

                try
                {
                    urlPathHash = await SaveFile(file, fileName, uploadPath, true);
                }
                catch (Exception)
                {
                    // ignore
                }

                return Ok(new
                {
                    message = "success",
                    urlPathHash
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message, details = ex.Message });
            }
        }

        private async Task<string> SaveFile(IFormFile file, string fileName, string uploadPath, bool hash = false)
        {
            string targetFilename;

            if (!string.IsNullOrEmpty(fileName))
            {
                if (hash)
                {
                    var cleanname = FilesystemHelper.RemoveKnownExtension(fileName, out var extHash);
                    var fileNameHash = Crypto.GenerateSHA256Hash(cleanname);
                    if (string.IsNullOrEmpty(extHash))
                        targetFilename = fileNameHash + GetExt(file);
                    else
                        targetFilename = fileNameHash + extHash;
                }
                else
                {
                    targetFilename = fileName;
                    var ext = Path.GetExtension(targetFilename);
                    if (string.IsNullOrEmpty(ext))
                        targetFilename += GetExt(file);
                }
            }
            else
            {
                var cleanedFilename = CleanupFilename(file);
                if (string.IsNullOrEmpty(cleanedFilename))
                    throw new Exception("uploaded file does not support a valid filename");
                targetFilename = cleanedFilename;
            }

            var targetFilepath = Path.Combine(uploadPath, targetFilename);
            using (var stream = new FileStream(targetFilepath, FileMode.Create))
                await file.CopyToAsync(stream);

            return $"{Request.Scheme}://{Request.Host}/api/locimg/file/{targetFilename}";
        }
    }
}
