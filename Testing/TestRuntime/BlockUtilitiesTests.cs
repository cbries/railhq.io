// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using libEsuEcos;
using libEsuEcos.Blocks;

namespace TestRuntime
{
    public class BlockUtilitiesTests
    {
        private readonly List<string> _testInput01 = new()
        {
            "100 state[0x0010]",
            "<END 0 (OK)>",
            "<EVENT 100>",
            "100 state[0x0000]",
            "<END 0 (OK)>",
            "<EVENT 100>",
            "100 state[0x0004]",
            "<END 0 (OK)>",
            "<EVENT 100>",
            "100 state[0x0000]",
            "<END 0 (OK)>",
            "<EVENT 100>",
            "100 state[0x0001]",
            "<END 0 (OK)>",
            "<EVENT 100>",
            "<END 0 (OK)>",
            "<EVENT 100>",
            "100 state[0x0020]",
            "<END 0 (OK)>",
            "<EVENT 100>",
            "100 state[0x0000]",
            "<END 0 (OK)>",
            "<EVENT 100>",
            "100 state[0x0010]",
            "<END 0 (OK)>"
        };

        [Fact]
        public void ExtractBlocks_ShouldExtractEventBlocksCorrectly()
        {
            // Arrange
            // ...

            // Act
            var blocks = BlockUtilities.ExtractBlocks(_testInput01);

            // Assert
            Assert.Equal(8, blocks.Count);
            foreach (var block in blocks)
            {
                Assert.IsType<EventBlock>(block);
                Assert.NotNull(block.NativeBlock);
                Assert.Contains("<EVENT", block.NativeBlock);
                Assert.Contains("<END", block.NativeBlock);
            }

            Assert.Equal(2, _testInput01.Count);
        }

        [Fact]
        public void ExtractBlocks_ShouldExtractEventBlocksCorrectly2()
        {
            // Arrange
            var arr = _testInput01.ToArray();

            // Act
            var blocks = BlockUtilities.ExtractBlocks(arr);

            // Assert
            Assert.Equal(8, blocks.Count);
            foreach (var block in blocks)
            {
                Assert.IsType<EventBlock>(block);
                Assert.NotNull(block.NativeBlock);
                Assert.Contains("<EVENT", block.NativeBlock);
                Assert.Contains("<END", block.NativeBlock);
            }

            Assert.Equal(_testInput01.Count, arr.Length);
        }

        [Fact]
        public void ParseControlledBySomeoneElse()
        {
            // Arrange
            var testInput01 = new List<string>()
            {
                "<REPLY set(1001, speedstep[15])>",
                "<END 25 (controlled by somebody else)>"
            };

            var testInput02 = new List<string>
            {
                "<REPLY get(1001, speed, speedstep)>",
                "1001 speed[0]",
                "1001 speedstep[0]",
                "<END 0 (OK)>"
            };

            // Act
            var blocks1 = BlockUtilities.ExtractBlocks(testInput01);
            var blocks2 = BlockUtilities.ExtractBlocks(testInput02);

            // Assert
            Assert.Single(blocks1);
            Assert.Equal(25, blocks1[0].Result.ErrorCode);
            Assert.Equal("controlled by somebody else", blocks1[0].Result.ErrorMessage);

            Assert.Single(blocks2);
            Assert.Equal(0, blocks2[0].Result.ErrorCode);
            Assert.Equal("OK", blocks2[0].Result.ErrorMessage);
        }
    }
}