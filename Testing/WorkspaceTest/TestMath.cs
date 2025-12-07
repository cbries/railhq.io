// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using System.Collections;
using System.Net.NetworkInformation;
using System.Security.Cryptography.Xml;
using FluentAssertions;

namespace WorkspaceTest;

[TestClass]
public class TestMath
{
    public class InOut
    {
        public int InputPin { get; set; }
        public int OutputPort { get; set; }
        public int OutputPin { get; set; }
    }

    [TestMethod]
    public void TestS88PortPin()
    {
        var data = new List<InOut>
        {
            new InOut { InputPin = 1, OutputPort = 1, OutputPin = 1 },
            new InOut { InputPin = 15, OutputPort = 1, OutputPin = 15 },
            new InOut { InputPin = 16, OutputPort = 1, OutputPin = 16 },
            new InOut { InputPin = 17, OutputPort = 2, OutputPin = 1 },
            new InOut { InputPin = 31, OutputPort = 2, OutputPin = 15 },
            new InOut { InputPin = 32, OutputPort = 2, OutputPin = 16 },
            new InOut { InputPin = 33, OutputPort = 3, OutputPin = 1 },
        };

        foreach (var it in data)
        {
            var res = libUtilities.S88Math.HandleInput(it.InputPin);

            res.InputPin.Should().Be(it.InputPin);
            res.OutputPort.Should().Be(it.OutputPort);
            res.OutputPin.Should().Be(it.OutputPin);
        }
    }
}
