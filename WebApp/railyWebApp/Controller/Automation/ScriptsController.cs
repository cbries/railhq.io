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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

// ReSharper disable ConvertToPrimaryConstructor

namespace railyWebApp.Controller.Automation
{
    [TypeFilter(typeof(RailhqAuthenticationFilter))]
    [ApiController]
    [Route("api/v1/automation/[controller]")]
    public class ScriptsController : RailhqAutomationBase
    {
        private string _storageIndexList { get; set; }

        /// <summary>
        /// Erfragt den WorkspaceName der aktuellen Session.
        /// </summary>
        private string WorkspaceName
        {
            get
            {
                var wsName = GetCachedValue(SessionGlobals.SessionWorkspaceName, HttpContext);
                return wsName;
            }
        }

        public ScriptsController(SupabaseService authService, IMemoryCache cache) : base(authService, cache)
        {
            // ignore
        }

        /// <summary>
        /// Gibt alle gespeicherten Skripte zurück, sortiert nach Reihenfolge (absteigend) und Name.
        /// </summary>
        /// <returns>Liste aller Skripte</returns>
        /// <response code="200">Skripte erfolgreich geladen</response>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<IEnumerable<ScriptModel>> GetAll()
        {
            var scripts = LoadScripts();

            var sortedScripts = scripts
                .OrderByDescending(s => s.Order)   // zuerst nach Order absteigend
                .ThenBy(s => s.Name)              // dann nach Name aufsteigend
                .ToList();

            return Ok(sortedScripts);
        }

        /// <summary>
        /// Gibt ein einzelnes Skript anhand der ID zurück.
        /// </summary>
        /// <param name="id">Die ID des Skripts</param>
        /// <returns>Ein Skript oder 404</returns>
        /// <response code="200">Skript gefunden</response>
        /// <response code="404">Skript nicht gefunden</response>
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<ScriptModel> GetById(int id)
        {
            var script = LoadScripts().FirstOrDefault(s => s.Id == id);
            return script == null ? NotFound() : Ok(script);
        }

        /// <summary>
        /// Benennt ein Skript um.
        /// </summary>
        /// <param name="id">Die ID des Skripts</param>
        /// <param name="request">Neuer Name</param>
        /// <response code="204">Erfolgreich umbenannt</response>
        /// <response code="404">Skript nicht gefunden</response>
        [HttpPut("{id}/rename")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult Rename(int id, [FromBody] RenameRequest request)
        {
            var scripts = LoadScripts();
            var script = scripts.FirstOrDefault(s => s.Id == id);
            if (script == null)
                return NotFound();

            script.Name = request.NewName;
            SaveScripts(scripts);
            return NoContent();
        }

        /// <summary>
        /// Aktualisiert den Code eines Skripts.
        /// </summary>
        /// <param name="id">Die ID des Skripts</param>
        /// <param name="request">Neuer Code</param>
        /// <response code="204">Erfolgreich aktualisiert</response>
        /// <response code="404">Skript nicht gefunden</response>
        [HttpPut("{id}/content")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult UpdateCode(int id, [FromBody] CodeUpdateRequest request)
        {
            var scripts = LoadScripts();
            var script = scripts.FirstOrDefault(s => s.Id == id);
            if (script == null)
                return NotFound();

            script.Code = request.Code;
            SaveScripts(scripts);
            return NoContent();
        }

        /// <summary>
        /// Ändert die Reihenfolge mehrerer Skripte.
        /// </summary>
        /// <param name="updates">Liste mit Skript-IDs und neuer Reihenfolge</param>
        /// <response code="204">Reihenfolge aktualisiert</response>
        [HttpPut("reorder")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public IActionResult Reorder([FromBody] List<OrderUpdate> updates)
        {
            var scripts = LoadScripts();
            foreach (var update in updates)
            {
                var script = scripts.FirstOrDefault(s => s.Id == update.Id);
                if (script != null)
                {
                    script.Order = update.Order;
                }
            }

            SaveScripts(scripts);
            return NoContent();
        }

        /// <summary>
        /// Löscht ein Skript.
        /// </summary>
        /// <param name="id">Die ID des Skripts</param>
        /// <response code="204">Erfolgreich gelöscht</response>
        /// <response code="404">Skript nicht gefunden</response>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult Delete(int id)
        {
            var scripts = LoadScripts();
            var script = scripts.FirstOrDefault(s => s.Id == id);
            if (script == null)
                return NotFound();

            scripts.Remove(script);
            SaveScripts(scripts);
            return NoContent();
        }

        /// <summary>
        /// Aktiviert ein Skript.
        /// </summary>
        /// <param name="id">Die ID des Skripts</param>
        /// <response code="204">Erfolgreich aktiviert</response>
        /// <response code="404">Skript nicht gefunden</response>
        [HttpPost("{id}/activate")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult Activate(int id)
        {
            var scripts = LoadScripts();
            var script = scripts.FirstOrDefault(s => s.Id == id);
            if (script == null)
                return NotFound();

            script.IsActive = true;
            SaveScripts(scripts);
            return NoContent();
        }

        /// <summary>
        /// Deaktiviert ein Skript.
        /// </summary>
        /// <param name="id">Die ID des Skripts</param>
        /// <response code="204">Erfolgreich deaktiviert</response>
        /// <response code="404">Skript nicht gefunden</response>
        [HttpPost("{id}/deactivate")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult Deactivate(int id)
        {
            var scripts = LoadScripts();
            var script = scripts.FirstOrDefault(s => s.Id == id);
            if (script == null)
                return NotFound();

            script.IsActive = false;
            SaveScripts(scripts);
            return NoContent();
        }

        /// <summary>
        /// Erstellt ein neues Skript.
        /// </summary>
        /// <param name="request">Skriptname und Code</param>
        /// <returns>Das neu erstellte Skript</returns>
        /// <response code="201">Skript erfolgreich erstellt</response>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public IActionResult Create([FromBody] CreateRequest request)
        {
            var scripts = LoadScripts();
            int newId = scripts.Count > 0 ? scripts.Max(s => s.Id) + 1 : 1;

            var newScript = new ScriptModel
            {
                Id = newId,
                Name = request.Name,
                IsActive = false,
                Code = request.Code
            };

            scripts.Add(newScript);
            SaveScripts(scripts);
            return CreatedAtAction(nameof(GetAll), new { id = newId }, newScript);
        }

        private object _internalFileLock = new();

        private List<ScriptModel> LoadScripts()
        {
            var baseName = libUserspace.Filesystem.WorkspacesBaseDir;
            
            // Prüfe ob Uid und WorkspaceName gültig sind
            if (string.IsNullOrEmpty(Uid) || string.IsNullOrEmpty(WorkspaceName))
            {
                Logging.Log.Warn("LoadScripts: Uid or WorkspaceName is null or empty");
                return new List<ScriptModel>();
            }
            
            var workspaceDir = Path.Combine(baseName, Uid, WorkspaceName);
            _storageIndexList = Path.Combine(workspaceDir, "scripts.json");
            
            // Prüfe ob das Workspace-Verzeichnis existiert
            if (!Directory.Exists(workspaceDir))
            {
                Logging.Log.Warn($"LoadScripts: Workspace directory does not exist: {workspaceDir}");
                return new List<ScriptModel>();
            }
            
            if (!System.IO.File.Exists(_storageIndexList))
            {
                try
                {
                    System.IO.File.WriteAllText(_storageIndexList, "[]", Encoding.UTF8);
                }
                catch
                {
                    // ignore
                }
            }
            lock (_internalFileLock)
            {
                // Nochmals prüfen, ob die Datei jetzt existiert
                if (!System.IO.File.Exists(_storageIndexList))
                {
                    Logging.Log.Warn($"LoadScripts: Scripts file does not exist and could not be created: {_storageIndexList}");
                    return new List<ScriptModel>();
                }
                
                using (var stream =
                       new FileStream(_storageIndexList, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var reader = new StreamReader(stream))
                {
                    var json = reader.ReadToEnd();
                    return JsonSerializer.Deserialize<List<ScriptModel>>(json) ?? new List<ScriptModel>();
                }
            }
        }

        private void SaveScripts(List<ScriptModel> scripts)
        {
            if (string.IsNullOrEmpty(_storageIndexList))
            {
                Logging.Log.Warn("SaveScripts: Storage path is not initialized");
                return;
            }
            
            // Stelle sicher, dass das Verzeichnis existiert
            var directory = Path.GetDirectoryName(_storageIndexList);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Logging.Log.Warn($"SaveScripts: Directory does not exist: {directory}");
                return;
            }
            
            lock (_internalFileLock)
            {
                var json = JsonSerializer.Serialize(scripts, new JsonSerializerOptions { WriteIndented = true });
                System.IO.File.WriteAllText(_storageIndexList, json);
            }
        }
    }

    // Modelle

    public class ScriptModel
    {
        [JsonProperty("id")] public int Id { get; set; }
        [JsonProperty("name")] public string Name { get; set; }
        [JsonProperty("description")] public string Description { get; set; }
        [JsonProperty("isActive")] public bool IsActive { get; set; }
        [JsonProperty("code")] public string Code { get; set; } = "";

        /// <summary>
        /// higher number is more on top
        /// </summary>
        [JsonProperty("order")] public int Order { get; set; } = 0;
    }

    public class RenameRequest
    {
        public string NewName { get; set; }
    }

    public class CreateRequest
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Code { get; set; }
    }

    public class CodeUpdateRequest
    {
        public string Code { get; set; }
    }

    public class OrderUpdate
    {
        public int Id { get; set; }
        public int Order { get; set; }
    }
}
