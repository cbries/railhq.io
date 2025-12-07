// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using libShared;
using libUserspace;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using railyWebIndex.Pages.Auth;
using railyWebIndex.Pages.Profile.Helper;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using libShared.PODs;
using libUserspace.Sbase;
using libUtilities;
using Newtonsoft.Json.Linq;

// ReSharper disable InconsistentNaming
// ReSharper disable ConvertToPrimaryConstructor

namespace railyWebIndex.Pages.Profile
{
    public class EditModel : PageModelBase
    {
        [BindProperty(SupportsGet = true)]
        public string EMail
        {
            get
            {
                if (RailhqUser == null) return "<invalid>";
                return RailhqUser.Email;
            }
        }

        /// <summary>
        /// This property is used as read only.
        /// The provided information are provided in the value-tags of the html controls.
        /// </summary>
        [BindProperty]
        public User UserDataset { get; set; }

        #region Properties Setter

        [BindProperty] public string Data_Username { get; set; }
        [BindProperty] public string Data_DisplayName { get; set; }

        #endregion

        public EditModel(SupabaseService authService, IMemoryCache cache) : base(authService, cache)
        {
        }

        public async Task<IActionResult> OnGet()
        {
            await QueryUsersData();

            return Page();
        }

        public async Task<IActionResult> OnPost()
        {
            try
            {
                var userData = new UserData
                {
                    Username = Data_Username,
                    DisplayName = Data_DisplayName
                };

                await QueryUsersData();

                if (UserDataset == null)
                {
                    UserDataset = new User { IsNew = true };
                    UserDataset.Apply(userData);
                }
                else
                {
                    //
                    // check if DisplayName or Username changed
                    //
                    var currentUserData = UserDataset.GetUserData();
                    var changedWelcomeName = false;

                    // default value, avoid `null`
                    if (string.IsNullOrEmpty(userData.Username))
                        userData.Username = string.Empty;
                    if (string.IsNullOrEmpty(userData.DisplayName))
                        userData.DisplayName = string.Empty;


                    if (!userData.Username.Equals(currentUserData.Username))
                    {
                        changedWelcomeName = true;
                    }
                    else if (!userData.DisplayName.Equals(currentUserData.DisplayName))
                    {
                        changedWelcomeName = true;
                    }

                    if (changedWelcomeName)
                    {
                        railyWebApp.Controller.Helper.CacheHelper.RemoveCachedValue(Cache,
                            SessionGlobals.SessionWelcomeName, HttpContext);
                    }

                    UserDataset.Apply(userData);
                }

                var res = await UpdateUsersData();
                if (!res)
                {
                    if (ViewData["errorMessage"] == null)
                        ViewData["errorMessage"] = "Aktualisierung deines Profil ist fehlgeschlagen.";

                    return Page();
                }

                return RedirectToPage(Globals.PageProfileEdit);
            }
            catch(Exception ex)
            {
                if (ViewData["errorMessage"] == null)
                    ViewData["errorMessage"] = $"Runtime error: {ex.GetExceptionMessages()}";

                return Page();
            }
        }

        private Task<bool> UpdateUsersData()
        {
            // User data is no longer stored in Supabase
            // For JSON-based auth, profile data could be stored locally
            // This is a placeholder - implement local storage if needed
            return Task.FromResult(true);
        }

        private Task<bool> QueryUsersData()
        {
            // User data is no longer loaded from Supabase
            // For JSON-based auth, profile data could be stored locally
            // This is a placeholder - implement local storage if needed
            
            UserDataset = new User { IsNew = true };
            Data_Username = string.Empty;
            Data_DisplayName = string.Empty;

            return Task.FromResult(true);
        }

        internal static Task<bool> CreateDefaultUser(string uid, SupabaseService sc)
        {
            // User data is no longer stored in Supabase
            // For JSON-based auth, this is a no-op
            return Task.FromResult(true);
        }
    }
}
