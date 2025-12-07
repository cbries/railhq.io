// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using libUtilities;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;

// ReSharper disable ConvertToPrimaryConstructor

namespace railyWebApp.Controller.Automation.Dto
{
    public class ApiError
    {
        public int Code { get; set; }
        public string Message { get; set; }

        public string DefaultLanguage { get; set; } = ErrorMessages.DefaultLanguage;

        public ApiError(ErrorCode code)
        {
            Code = (int)code;
            Message = ErrorMessages.GetMessage(code, DefaultLanguage);
        }

        public ApiError(ErrorCode code, Exception ex)
        {
            Code = (int)code;

            var m = ErrorMessages.GetMessage(code, DefaultLanguage);
            if (ex != null)
                m += " | " + ex.GetExceptionMessages();
            Message = m;
        }

        public ApiError(ErrorCode code, string message)
        {
            Code = (int)code;
            Message = message;
        }

        public static string GetLang(ControllerBase ctx)
        {
            var acceptLang = ctx.Request.Headers["Accept-Language"].FirstOrDefault() ?? ErrorMessages.DefaultLanguage;
            var lang = acceptLang.Split(',').FirstOrDefault()?.Split('-').FirstOrDefault()?.ToLower() ?? ErrorMessages.DefaultLanguage;
            return lang;
        }

        public static IActionResult NotFound(ControllerBase ctx, ErrorCode code)
        {
            var lang = GetLang(ctx);
            return ctx.NotFound(new ApiError(code) { DefaultLanguage = lang });
        }

        public static IActionResult StatusCode(
            ControllerBase ctx,
            ErrorCode code = ErrorCode.InternalServerError)
        {
            var lang = GetLang(ctx);
            return ctx.StatusCode(500, new ApiError(code) { DefaultLanguage = lang });
        }

        public static IActionResult StatusCode(
            ControllerBase ctx,
            Exception ex = null)
        {
            var lang = GetLang(ctx);
            return ctx.StatusCode(500, new ApiError(ErrorCode.InternalServerError, ex) { DefaultLanguage = lang });
        }

        public static IActionResult Unauthorized(
            ControllerBase ctx,
            string message = "Authentication required.")
        {
            var lang = GetLang(ctx);
            return ctx.Unauthorized(new ApiError(ErrorCode.Unauthorized, message) { DefaultLanguage = lang });
        }
    }
}
