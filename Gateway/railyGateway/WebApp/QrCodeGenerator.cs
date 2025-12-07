// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

﻿using System;
using System.IO;
using libUtilities;
using QRCoder;

namespace railyGateway.WebApp
{
    public class QrCodeGenerator
    {
        public static byte[] Generate(string message)
        {
            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(message, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new PngByteQRCode(qrCodeData);
            var qrCodeImage = qrCode.GetGraphic(20);
            return qrCodeImage;
        }

        public static void SaveAs(string filePath, string message)
        {
            try
            {
                var qrCodeImage = Generate(message);
                File.WriteAllBytes(filePath, qrCodeImage);
            }
            catch (Exception ex)
            {
                Logging.ExceptionLog(ex);
            }
        }

        public static string Base64QrCode { get; private set; } = string.Empty;
        public static string InternetIpAddress { get; private set; } = string.Empty;
        public static int InternetIpPort { get; private set; } = -1;

        public static string GenerateHostQrCode(
            string internetIpAddress,
            int listenPort
            )
        {
            if (!string.IsNullOrEmpty(Base64QrCode)) return Base64QrCode;

            InternetIpAddress = internetIpAddress;
            InternetIpPort = listenPort;

            var tmpPath = Path.GetTempFileName();
            var pathToQrPng = Path.Combine(tmpPath + ".png");
            try
            {
                SaveAs(pathToQrPng, $"http://{internetIpAddress}:{listenPort}?qrcode=true");
                Base64QrCode = ConvertImageToBase64(pathToQrPng);
                return Base64QrCode;
            }
            catch
            {
                // ignore
            }
            finally
            {
                DeleteTmpFile(tmpPath);
                DeleteTmpFile(pathToQrPng);
            }

            return string.Empty;
        }

        private static void DeleteTmpFile(string path)
        {
            if (string.IsNullOrEmpty(path)) return;
            try
            {
                if(File.Exists(path))
                    File.Delete(path);
            }
            catch
            {
                // ignore
            }
        }

        private static string ConvertImageToBase64(string imagePath)
        {
            byte[] imageBytes = File.ReadAllBytes(imagePath);
            return Convert.ToBase64String(imageBytes);
        }
    }
}
