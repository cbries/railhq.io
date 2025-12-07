// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

﻿namespace TestRuntime
{
    public class Z21HelperTests
    {
        [Fact]
        public void RmBusAddress()
        {
            // arrange
            const byte gruppenIndex = 1;
            byte[] rmStatus = [0x01, 0x00, 0xC5, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00];

            // act
            /*
               Rückmelder 11, Eingang 1
               Rückmelder 13, Eingang 1
               Rückmelder 13, Eingang 3
               Rückmelder 13, Eingang 7
               Rückmelder 13, Eingang 8
             */
            var aktive = libZ21.RmBusHelper.GetActiveFeedbacks(gruppenIndex, rmStatus);

            // assert
            Assert.Equal(11, aktive[0].module);
            Assert.Equal(1, aktive[0].pin);

            Assert.Equal(13, aktive[1].module);
            Assert.Equal(1, aktive[1].pin);

            Assert.Equal(13, aktive[2].module);
            Assert.Equal(3, aktive[2].pin);

            Assert.Equal(13, aktive[3].module);
            Assert.Equal(7, aktive[3].pin);

            Assert.Equal(13, aktive[4].module);
            Assert.Equal(8, aktive[4].pin);
        }
    }
}
