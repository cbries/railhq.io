// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

﻿namespace railyWebIndex.Pages
{
    public static class HelperTime
    {
        public static string FormatTime(int seconds)
        {
            int hours = seconds / 3600;
            int minutes = (seconds % 3600) / 60;

            if (hours > 0)
                return minutes > 0 ? $"{hours} h {minutes} Min." : $"{hours} h";
            if (minutes > 0)
                return $"{minutes} Min.";

            return $"{seconds} Sek.";
        }


        public static string FormatDays(double days)
        {
            if (days >= 1) return $"{days:F2} Tage";
            double hours = days * 24;
            if (hours >= 1) return $"{hours:F1} Std.";
            double minutes = hours * 60;
            if (minutes >= 1) return $"{minutes:F0} Min.";
            double seconds = minutes * 60;
            return $"{seconds:F0} Sek.";
        }
    }

}
