// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using libUtilities;
using Newtonsoft.Json;

namespace libUserspace.Auth
{
    /// <summary>
    /// JSON file-based authentication service.
    /// Users are stored in a JSON file and validated against it.
    /// </summary>
    public class JsonAuthService : IAuthService
    {
        private readonly string _usersFilePath;
        private readonly object _fileLock = new object();
        
        // In-memory cache of active sessions (token -> user)
        private readonly ConcurrentDictionary<string, AuthSession> _activeSessions = new();
        
        // Token expiration time (default: 24 hours)
        private readonly TimeSpan _tokenExpiration = TimeSpan.FromHours(24);

        public AuthUser CurrentUser { get; private set; }

        public JsonAuthService(string usersFilePath)
        {
            _usersFilePath = usersFilePath;
            EnsureUsersFileExists();
        }

        private void EnsureUsersFileExists()
        {
            if (!File.Exists(_usersFilePath))
            {
                var container = new UsersContainer { Users = new List<AuthUser>() };
                SaveUsers(container);
            }
        }

        private UsersContainer LoadUsers()
        {
            lock (_fileLock)
            {
                try
                {
                    var json = File.ReadAllText(_usersFilePath, Encoding.UTF8);
                    return JsonConvert.DeserializeObject<UsersContainer>(json) ?? new UsersContainer();
                }
                catch (Exception ex)
                {
                    Logging.ExceptionLog(ex);
                    return new UsersContainer();
                }
            }
        }

        private void SaveUsers(UsersContainer container)
        {
            lock (_fileLock)
            {
                try
                {
                    var json = JsonConvert.SerializeObject(container, Formatting.Indented);
                    File.WriteAllText(_usersFilePath, json, Encoding.UTF8);
                }
                catch (Exception ex)
                {
                    Logging.ExceptionLog(ex);
                }
            }
        }

        public async Task<AuthSession> Register(string email, string password)
        {
            var container = LoadUsers();

            // Check if user already exists
            if (container.Users.Any(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase)))
            {
                throw new Exception("User with this email already exists");
            }

            var user = new AuthUser
            {
                Id = Guid.NewGuid().ToString("D"),
                Email = email,
                PasswordHash = HashPassword(password),
                CreatedAt = DateTime.UtcNow,
                EmailConfirmedAt = DateTime.UtcNow, // Auto-confirm for JSON auth
                ConfirmedAt = DateTime.UtcNow,
                Role = "user"
            };

            container.Users.Add(user);
            SaveUsers(container);

            return await Task.FromResult(CreateSession(user));
        }

        public async Task<AuthSession> Login(string email, string password, bool remember = false)
        {
            var container = LoadUsers();

            var user = container.Users.FirstOrDefault(u => 
                u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));

            if (user == null)
            {
                Logging.Log.Warn($"Login failed: User not found for email {email}");
                return null;
            }

            if (!VerifyPassword(password, user.PasswordHash))
            {
                Logging.Log.Warn($"Login failed: Invalid password for email {email}");
                return null;
            }

            // Update last sign in time
            user.LastSignInAt = DateTime.UtcNow;
            SaveUsers(container);

            CurrentUser = user;

            var session = CreateSession(user, remember ? TimeSpan.FromDays(30) : _tokenExpiration);
            return await Task.FromResult(session);
        }

        public Task Logout()
        {
            CurrentUser = null;
            return Task.CompletedTask;
        }

        public Task ResetPassword(string email)
        {
            // For JSON-based auth, password reset would require manual intervention
            // or additional implementation (e.g., sending emails)
            Logging.Log.Info($"Password reset requested for: {email}");
            return Task.CompletedTask;
        }

        public async Task<AuthUser> Validate(string jwt)
        {
            return await GetUser(jwt);
        }

        public Task<AuthUser> GetUser(string jwt)
        {
            if (string.IsNullOrEmpty(jwt))
                return Task.FromResult<AuthUser>(null);

            // Check if token exists in active sessions
            if (_activeSessions.TryGetValue(jwt, out var session))
            {
                // Check if session has expired
                if (session.ExpiresAt > DateTime.UtcNow)
                {
                    return Task.FromResult(session.User);
                }

                // Remove expired session
                _activeSessions.TryRemove(jwt, out _);
            }

            // Try to decode the token (simple base64 encoded user ID)
            try
            {
                var decoded = DecodeToken(jwt);
                if (decoded == null)
                    return Task.FromResult<AuthUser>(null);

                var container = LoadUsers();
                var user = container.Users.FirstOrDefault(u => u.Id == decoded.UserId);

                if (user != null && decoded.ExpiresAt > DateTime.UtcNow)
                {
                    // Re-add to active sessions
                    var newSession = new AuthSession
                    {
                        AccessToken = jwt,
                        User = user,
                        ExpiresAt = decoded.ExpiresAt
                    };
                    _activeSessions.TryAdd(jwt, newSession);
                    return Task.FromResult(user);
                }
            }
            catch (Exception ex)
            {
                Logging.ExceptionLog(ex);
            }

            return Task.FromResult<AuthUser>(null);
        }

        public void LoadSession()
        {
            // No-op for JSON auth - sessions are managed in memory
        }

        public async Task<bool> DeleteUser(Guid userGuid)
        {
            var container = LoadUsers();
            var userIdString = userGuid.ToString("D");
            
            var user = container.Users.FirstOrDefault(u => u.Id == userIdString);
            if (user == null)
                return false;

            container.Users.Remove(user);
            SaveUsers(container);

            // Remove any active sessions for this user
            var sessionsToRemove = _activeSessions
                .Where(kvp => kvp.Value.User.Id == userIdString)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var token in sessionsToRemove)
            {
                _activeSessions.TryRemove(token, out _);
            }

            return await Task.FromResult(true);
        }

        /// <summary>
        /// Gets all users (admin operation)
        /// </summary>
        public List<AuthUser> GetAllUsers()
        {
            var container = LoadUsers();
            // Return users without password hashes for security
            return container.Users.Select(u => new AuthUser
            {
                Id = u.Id,
                Email = u.Email,
                EmailConfirmedAt = u.EmailConfirmedAt,
                ConfirmationSentAt = u.ConfirmationSentAt,
                ConfirmedAt = u.ConfirmedAt,
                CreatedAt = u.CreatedAt,
                LastSignInAt = u.LastSignInAt,
                RecoverySentAt = u.RecoverySentAt,
                UpdatedAt = u.UpdatedAt,
                BannedUntil = u.BannedUntil,
                Role = u.Role
            }).ToList();
        }

        /// <summary>
        /// Creates a new user directly (admin operation)
        /// </summary>
        public AuthUser CreateUser(string email, string password, string role = "user")
        {
            var container = LoadUsers();

            if (container.Users.Any(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase)))
            {
                throw new Exception("User with this email already exists");
            }

            var user = new AuthUser
            {
                Id = Guid.NewGuid().ToString("D"),
                Email = email,
                PasswordHash = HashPassword(password),
                CreatedAt = DateTime.UtcNow,
                EmailConfirmedAt = DateTime.UtcNow,
                ConfirmedAt = DateTime.UtcNow,
                Role = role
            };

            container.Users.Add(user);
            SaveUsers(container);

            return user;
        }

        /// <summary>
        /// Updates a user's password
        /// </summary>
        public bool UpdatePassword(string userId, string newPassword)
        {
            var container = LoadUsers();
            var user = container.Users.FirstOrDefault(u => u.Id == userId);
            
            if (user == null)
                return false;

            user.PasswordHash = HashPassword(newPassword);
            user.UpdatedAt = DateTime.UtcNow;
            SaveUsers(container);

            return true;
        }

        /// <summary>
        /// Updates a user's email address
        /// </summary>
        /// <param name="userId">The user ID</param>
        /// <param name="newEmail">The new email address</param>
        /// <returns>True if successful, false otherwise</returns>
        public bool UpdateEmail(string userId, string newEmail)
        {
            var container = LoadUsers();
            var user = container.Users.FirstOrDefault(u => u.Id == userId);
            
            if (user == null)
                return false;

            // Check if new email is already in use by another user
            if (container.Users.Any(u => u.Id != userId && u.Email.Equals(newEmail, StringComparison.OrdinalIgnoreCase)))
            {
                throw new Exception("Diese E-Mail-Adresse wird bereits von einem anderen Benutzer verwendet.");
            }

            user.Email = newEmail;
            user.UpdatedAt = DateTime.UtcNow;
            SaveUsers(container);

            return true;
        }

        #region Helper Methods

        private AuthSession CreateSession(AuthUser user, TimeSpan? expiration = null)
        {
            var expiresAt = DateTime.UtcNow.Add(expiration ?? _tokenExpiration);
            var token = GenerateToken(user.Id, expiresAt);

            var session = new AuthSession
            {
                AccessToken = token,
                RefreshToken = GenerateRefreshToken(),
                ExpiresIn = (long)(expiration ?? _tokenExpiration).TotalSeconds,
                ExpiresAt = expiresAt,
                User = user,
                TokenType = "Bearer"
            };

            _activeSessions.TryAdd(token, session);

            return session;
        }

        private string GenerateToken(string userId, DateTime expiresAt)
        {
            var tokenData = new TokenData
            {
                UserId = userId,
                ExpiresAt = expiresAt,
                Nonce = Guid.NewGuid().ToString("N")
            };

            var json = JsonConvert.SerializeObject(tokenData);
            var bytes = Encoding.UTF8.GetBytes(json);
            return Convert.ToBase64String(bytes);
        }

        private TokenData DecodeToken(string token)
        {
            try
            {
                var bytes = Convert.FromBase64String(token);
                var json = Encoding.UTF8.GetString(bytes);
                return JsonConvert.DeserializeObject<TokenData>(json);
            }
            catch
            {
                return null;
            }
        }

        private string GenerateRefreshToken()
        {
            var randomBytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
            }
            return Convert.ToBase64String(randomBytes);
        }

        private string HashPassword(string password)
        {
            // Simple SHA256 hash with salt
            // For production, consider using BCrypt or similar
            var salt = Guid.NewGuid().ToString("N");
            var saltedPassword = salt + password;
            
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(saltedPassword));
                var hash = Convert.ToBase64String(hashedBytes);
                return $"{salt}:{hash}";
            }
        }

        private bool VerifyPassword(string password, string storedHash)
        {
            if (string.IsNullOrEmpty(storedHash))
                return false;

            var parts = storedHash.Split(':');
            if (parts.Length != 2)
                return false;

            var salt = parts[0];
            var hash = parts[1];
            var saltedPassword = salt + password;

            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(saltedPassword));
                var computedHash = Convert.ToBase64String(hashedBytes);
                return hash == computedHash;
            }
        }

        #endregion

        private class TokenData
        {
            public string UserId { get; set; }
            public DateTime ExpiresAt { get; set; }
            public string Nonce { get; set; }
        }
    }
}
