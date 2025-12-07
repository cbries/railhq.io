// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using Newtonsoft.Json;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace libUtilities
{
    public class JsonLoader<T>
    {
        private readonly string _cdnUrl;
        private readonly string _localFileName;
        private readonly Action<string> _errorCallback;

        // ReSharper disable once ConvertToPrimaryConstructor
        public JsonLoader(
            string cdnUrl, 
            string localFileName,
            Action<string> errorCallback = null)
        {
            _cdnUrl = cdnUrl;
            _localFileName = localFileName;
            _errorCallback = errorCallback;
        }

        public async Task<T> LoadAsync()
        {
            string jsonContent = await TryLoadFromCdnAsync();
            if (string.IsNullOrWhiteSpace(jsonContent))
            {
                jsonContent = TryLoadFromFile();
            }

            if (!string.IsNullOrWhiteSpace(jsonContent))
            {
                return TryDeserialize(jsonContent);
            }

            return default;
        }

        private async Task<string> TryLoadFromCdnAsync()
        {
            if (string.IsNullOrWhiteSpace(_cdnUrl))
                return null;

            try
            {
                using var httpClient = new HttpClient();
                var response = await httpClient.GetAsync(_cdnUrl);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync();
                }
                else
                {
                    _errorCallback?.Invoke($"CDN antwortete mit Status: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                _errorCallback?.Invoke($"Fehler beim Laden der Datei vom CDN: {ex.Message}");
            }

            return null;
        }

        private string TryLoadFromFile()
        {
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", _localFileName);

            if (!File.Exists(filePath))
            {
                _errorCallback?.Invoke("Die Datei wurde weder über das CDN noch lokal gefunden.");
                return null;
            }

            try
            {
                return File.ReadAllText(filePath);
            }
            catch (Exception ex)
            {
                _errorCallback?.Invoke($"Fehler beim Lesen der lokalen Datei: {ex.Message}");
                return null;
            }
        }

        private T TryDeserialize(string jsonContent)
        {
            try
            {
                return JsonConvert.DeserializeObject<T>(jsonContent);
            }
            catch (Exception ex)
            {
                _errorCallback?.Invoke($"Fehler beim Parsen der JSON-Daten: {ex.Message}");
                return default;
            }
        }
    }
}
