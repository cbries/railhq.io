// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using libUserspace;
using libUserspace.Auth;
using libUserspace.Sbase;

// ReSharper disable ConvertToPrimaryConstructor

namespace railyWebIndex.Pages.Database
{
    public class DbUser
    {
        public string LastError { get; private set; }

        private readonly SupabaseService _authService;
        private readonly AuthUser _user;

        public string Uid => _user?.Id;

        public string LastLogin
        {
            get
            {
                if (_user == null) return "unknown";
                var dt = _user.LastSignInAt ?? DateTime.UtcNow;
                return dt.ToString("O");
            }
        }

        public DbUser(SupabaseService authService, AuthUser user)
        {
            _authService = authService;
            _user = user;
        }

        public Task<User> GetUser()
        {
            // User data is no longer stored in Supabase
            // For JSON-based auth, we return a basic user object
            var uid = _user?.Id;
            if (uid == null) return Task.FromResult<User>(null);

            // Return a minimal User object with the user's data
            var user = new User
            {
                UserId = Guid.TryParse(uid, out var guid) ? guid : Guid.Empty
            };

            return Task.FromResult(user);
        }

        private string _userAcronym;

        public async Task<string> GetAcronym()
        {
            if (!string.IsNullOrEmpty(_userAcronym)) return _userAcronym;

            var user = await GetUser();
            var userData = user?.GetUserData();
            if (!string.IsNullOrEmpty(userData?.DisplayName))
            {
                var name = userData.DisplayName;
                var words = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                _userAcronym = words.Length > 1 ? $"{words[0][0]}{words[1][0]}".ToUpper() : $"{words[0][0]}".ToUpper();
                return _userAcronym;
            }

            if (!string.IsNullOrEmpty(userData?.Username))
            {
                _userAcronym = userData.Username.Substring(0, 2).ToUpper();
                return _userAcronym;
            }

            if (!string.IsNullOrWhiteSpace(_user?.Email))
            {
                _userAcronym = _user.Email.Substring(0, 2).ToUpper();
                return _userAcronym;
            }

            _userAcronym = "?";

            return _userAcronym;
        }

        private string _welcomeName;

        public async Task<string> GetWelcomeName()
        {
            try
            {
                if (!string.IsNullOrEmpty(_welcomeName)) return _welcomeName;

                var user = await GetUser();
                var userData = user?.GetUserData();
                if (!string.IsNullOrEmpty(userData?.DisplayName))
                {
                    _welcomeName = userData.DisplayName;
                    return _welcomeName;
                }

                if (!string.IsNullOrEmpty(userData?.Username))
                {
                    _welcomeName = userData.Username;
                    return _welcomeName;
                }

                return _user?.Email ?? "?";
            }
            catch
            {
                // ignore
            }

            return "?";
        }
    }
}
