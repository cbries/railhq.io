// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using libUserspace;
using libUserspace.Sbase;
using libUtilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using railyWebIndex.Pages.Auth;
using System.Collections.Generic;
using System.Threading.Tasks;

// ReSharper disable ConvertToPrimaryConstructor

namespace railyWebIndex.Pages
{
    public class NotificationEntry
    {
        public string Title { get; set; }
        public string Message { get; set; }
        public string ShortMessageHtml { get; set; }
        public string FullMessageHtml { get; set; }
        public System.DateTime CreatedAt { get; set; }
        public bool IsHidden { get; set; }
    }

    public class NotificationsModel : PageModelBase
    {
        public NotificationsModel(SupabaseService authService, IMemoryCache cache) : base(authService, cache)
        {
        }

        private List<Notification> Notifications { get; set; } = new();
        [BindProperty(SupportsGet = true)] public List<NotificationEntry> NotificationEntries { get; private set; } = new();


        [BindProperty(SupportsGet = true)] public List<Notification> SystemNotifications { get; private set; } = new();

        public async Task<IActionResult> OnGet()
        {
            // NotificationEntries
            Notifications = await LoadNotifications();
            foreach (var notification in Notifications)
            {
                var (shortHtml, fullHtml, _) = UbbTextTruncator.TruncateAndFormat(notification.Message, 40);
                NotificationEntries.Add(new NotificationEntry
                {
                    Title = notification.Title,
                    CreatedAt = notification.CreatedAt,
                    IsHidden = notification.IsHidden,
                    ShortMessageHtml = shortHtml,
                    FullMessageHtml = fullHtml
                });
            }

            SystemNotifications = await LoadSystemNotifications(true, 100);

            return Page();
        }
    }
}
