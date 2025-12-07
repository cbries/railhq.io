// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: Get.cs

namespace libEsuEcos.Commands
{
    public class Get : Command
    {
        public static string N = "get";
        public override CommandT Type => CommandT.Get;
        public override string Name => N;
    }
}
