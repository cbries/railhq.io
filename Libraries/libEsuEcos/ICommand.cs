// Copyright (c) 2024 Dr. Christian Benjamin Ries
// Licensed under the MIT License

using System.Collections.Generic;

namespace libEsuEcos
{
    public interface ICommand
    {
        CommandT Type { get; }
        string Name { get; }
        string NativeCommand { get; set; }
        int ObjectId { get; }
        List<CommandArgument> Arguments { get; set; }

        bool Parse(bool keepQuotes = false);
        bool ArgumentsHas(string name);
    }
}
