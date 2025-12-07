// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: MessageEventArgs.cs

using System;

namespace railyEsuEcos.Network
{
    public class MessageEventArgs(string msg, System.Exception ex = null) : System.EventArgs
    {
        public string Message { get; } = msg;
        public System.Exception Exception { get; } = ex;

        public MessageEventArgs(string msg, params object[] args) 
            : this(string.Format(msg, args), default(Exception))
        {
        }
    }
}
