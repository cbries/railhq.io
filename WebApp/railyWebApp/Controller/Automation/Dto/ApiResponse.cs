// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

// ReSharper disable ConvertToPrimaryConstructor

namespace railyWebApp.Controller.Automation.Dto
{
    public class ApiResponse<T>
    {
        public string Code { get; set; }
        public string Message { get; set; }
        public T Data { get; set; }

        public ApiResponse(string message, T data)
        {
            Code = "Success";
            Message = message;
            Data = data;
        }
    }

    public static class ApiResponse
    {
        public static ApiResponse<object> Success(string message, object data)
            => new(message, data);

        public static ApiResponse<object> Success(string message)
            => new(message, new { });
    }
}