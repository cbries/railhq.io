// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using railyWebApp.Controller.Automation.Dto.Entity;

// ReSharper disable PossibleMultipleEnumeration
// ReSharper disable ConvertToPrimaryConstructor

namespace railyWebApp.Controller.Automation.Services.Impl
{
    public class BlockService : AbstractBaseService, IBlockService
    {
        public BlockService(string uid) : base(uid)
        {
            // ignore
        }

        private static string GetCleanedBlockName(string input)
        {
            var blockName = input
                .Trim()
                .Replace("[+]", string.Empty)
                .Replace("[-]", string.Empty);

            return blockName;
        }

        private static string GetDirectionSuffix(string identifier)
        {
            if (identifier.Contains("[+]")) return "+";
            if (identifier.Contains("[-]")) return "-";
            return string.Empty;
        }

        private static BlockSensor TryResolveSensor(string sensorName, Dictionary<string, BlockSensor> sensorMap)
        {
            if (string.IsNullOrWhiteSpace(sensorName)) return null;
            return sensorMap.GetValueOrDefault(sensorName);
        }

        private Dictionary<string, BlockSensor> GetBlockSensors(libMetamodel.Settings.ISettings metamodel)
        {
            return metamodel.Sensors
                .Where(s => !string.IsNullOrEmpty(s.Key.Name))
                .ToDictionary(s => s.Key.Name, s => new BlockSensor
                {
                    Name = s.Key.Name,
                    Provider = s.Key.Provider,
                    Address = s.Key.Address
                }, StringComparer.OrdinalIgnoreCase);
        }

        private Dictionary<string, BlockLocomotive> GetBlockLocomotives(libMetamodel.Settings.ISettings metamodel)
        {
            return metamodel.Locomotives
                .Where(l => !string.IsNullOrWhiteSpace(l.Key.AssignedToBlock))
                .ToDictionary(
                    l => l.Key.AssignedToBlock.Trim(),
                    l => new BlockLocomotive
                    {
                        ObjectId = l.Key.ObjectId,
                        DriverName = l.Key.DriverName,
                        EnterSide = l.Key.EnterSide.ToString(),
                        // ggf. mehr übernehmen
                    },
                    StringComparer.OrdinalIgnoreCase
                );
        }

        #region IBlockService

        public async Task<IEnumerable<Block>> GetAll()
        {
            var res = Globals.UserWorkspaces.TryGetValue(Uid, out var workspace);
            if (!res) throw new Exception("Workspace not loaded");

            var metamodel = workspace.Metamodel?.Settings;
            if (metamodel == null) throw new Exception("Workspace model not loaded");

            var sensorMap = GetBlockSensors(metamodel);
            var locoMap = GetBlockLocomotives(metamodel);

            var blockDict = new Dictionary<string, Block>(StringComparer.OrdinalIgnoreCase);

            foreach (var itBlock in metamodel.BlockSensors)
            {
                var blockData = itBlock.Key;
                if (blockData == null) continue;

                var cleanedName = GetCleanedBlockName(blockData.Identifier);
                var direction = GetDirectionSuffix(blockData.Identifier); // "+" oder "-"

                if (!blockDict.TryGetValue(cleanedName, out var blockEntity))
                {
                    blockEntity = new Block { Name = cleanedName };
                    blockDict[cleanedName] = blockEntity;
                }

                var directional = new BlockDirectional
                {
                    IsEnabled = blockData.IsEnabled,
                    IsLocked = blockData.IsLocked,
                    Length = blockData.Length,
                    FeedbackEnter = TryResolveSensor(blockData.SensorEnter, sensorMap),
                    FeedbackOcc = TryResolveSensor(blockData.SensorOcc, sensorMap),
                    FeedbackIn = TryResolveSensor(blockData.SensorIn, sensorMap),
                    AssignedLocomotive = locoMap.GetValueOrDefault(cleanedName),

                    //StartDelay = blockData.StartDelay,
                    //SignalsToRedDelay = blockData.SignalsToRedDelay,
                    //IsCommuterAllowed = direction == "+" ? blockData.IsCommuterAllowedPlus : blockData.IsCommuterAllowedMinus,
                    //Signal = blockData.Signal,
                    //Vorsignal = blockData.Vorsignal
                };

                if (direction == "+")
                    blockEntity.Plus = directional;
                else if (direction == "-")
                    blockEntity.Minus = directional;
            }

            return blockDict.Values;
        }

        public async Task<Block> Get(string blockName)
        {
            var res = Globals.UserWorkspaces.TryGetValue(Uid, out var workspace);
            if (!res) throw new Exception("Workspace not loaded");

            var metamodel = workspace.Metamodel?.Settings;
            if (metamodel == null) throw new Exception("Workspace model not loaded");

            var sensorMap = GetBlockSensors(metamodel);
            var locoMap = GetBlockLocomotives(metamodel);

            var result = new Block { Name = blockName };

            foreach (var itBlock in metamodel.BlockSensors)
            {
                var blockData = itBlock.Key;
                if (blockData == null) continue;

                var cleanedName = GetCleanedBlockName(blockData.Identifier);

                if (!string.Equals(cleanedName, blockName, StringComparison.OrdinalIgnoreCase))
                    continue;

                var direction = GetDirectionSuffix(blockData.Identifier); // "+" oder "-"

                var directionalData = new BlockDirectional
                {
                    IsEnabled = blockData.IsEnabled,
                    IsLocked = blockData.IsLocked,
                    Length = blockData.Length,
                    FeedbackEnter = TryResolveSensor(blockData.SensorEnter, sensorMap),
                    FeedbackOcc = TryResolveSensor(blockData.SensorOcc, sensorMap),
                    FeedbackIn = TryResolveSensor(blockData.SensorIn, sensorMap),
                    AssignedLocomotive = locoMap.GetValueOrDefault(cleanedName),

                    //StartDelay = blockData.StartDelay,
                    //SignalsToRedDelay = blockData.SignalsToRedDelay,
                    //IsCommuterAllowed = direction == "+" ? blockData.IsCommuterAllowedPlus : blockData.IsCommuterAllowedMinus,
                    //Signal = blockData.Signal,
                    //Vorsignal = blockData.Vorsignal
                };

                if (direction == "+")
                    result.Plus = directionalData;
                else if (direction == "-")
                    result.Minus = directionalData;
            }

            if (result.Plus == null && result.Minus == null)
                throw new Exception("Block not found");

            return result;
        }

        #endregion
    }
}
