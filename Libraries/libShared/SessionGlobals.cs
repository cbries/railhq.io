// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace libShared
{
    public class SessionGlobals
    {
        public static string ProtectionKeyName = "RailHQ";

        public static string AuthUserSession = "UserSession";
        public static string AuthUserRefreshToken = "RefreshToken";
        public static string AuthEmail = "Email";
        public static string AuthUid = "Uid";
        public static string SessionWelcomeName = "WelcomeName";
        public static string SessionAcronym = "Acronym";
        public static string SessionWorkspaceName = "WorkspaceName";

        public static List<string> AllKeys => new List<string>
        {
            AuthUserSession,
            AuthUserRefreshToken,
            AuthEmail,
            AuthUid,
            SessionWelcomeName,
            SessionAcronym
        };
    }
}
