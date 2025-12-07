// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

﻿namespace railyWebApp.DataProvider
{
    using Newtonsoft.Json;

    #region Interfaces

    public interface IPayloadS88
    {
        [JsonProperty("event")] IEvent Event { get; set; }

        [JsonProperty("info")] IInfo Info { get; set; }
    }

    public interface IEvent
    {
        [JsonProperty("port")] int Port { get; set; }

        [JsonProperty("state")] IState State { get; set; }
    }

    public interface IState
    {
        [JsonProperty("hex")] string Hex { get; set; }

        [JsonProperty("binary")] string Binary { get; set; }
    }

    public interface IInfo
    {
        [JsonProperty("left")] int Left { get; set; }

        [JsonProperty("middle")] int Middle { get; set; }

        [JsonProperty("right")] int Right { get; set; }
    }

    #endregion

    public class PayloadS88 : IPayloadS88
    {
        public IEvent Event { get; set; } = new Event();
        public IInfo Info { get; set; } = new Info();
    }

    public class Event : IEvent
    {
        public int Port { get; set; } = 0;
        public IState State { get; set; } = new State();
    }

    public class State : IState
    {
        public string Hex { get; set; } = string.Empty;
        public string Binary { get; set; } = string.Empty;
    }

    public class Info : IInfo
    {
        public int Left { get; set; } = 0;
        public int Middle { get; set; } = 0;
        public int Right { get; set; } = 0;
    }

}
