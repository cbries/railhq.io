// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

﻿using FluentAssertions;
using libUserspace.Info;

namespace WorkspaceTest
{
    [TestClass]
    public sealed class TestWorkspace
    {
        public const string TestmodelsDirName = "Testmodels";

        [TestMethod]
        [DeploymentItem(TestmodelsDirName)]
        public void TestWorkspaceStateCreation()
        {
            const string uid = "fakeUid";

            var instance = new Workspaces(TestmodelsDirName);
            var res = instance.Update(uid).Result;
            res.Should().BeTrue();
            var workspaces = instance.GetWorkspaceStats(uid).Result;
            workspaces.Should().NotBeNull();
            workspaces.Count.Should().Be(1);

            var workspace = workspaces[0];
            workspace.Should().NotBeNull();
            workspace.Name.Should().Be("20250217_09_55_19-Import_from_Rocrail"); 
            
            workspace.Items.Should().NotBeNull();
            workspace.Items.ItemTypes.Count.Should().Be(15);
            workspace.Items.NoBlocks.Should().Be(5);
            workspace.Items.NoSensors.Should().Be(90);
            workspace.Items.NoSignals.Should().Be(11);
            workspace.Items.NoSwitches.Should().Be(32);

            workspace.Settings.Should().NotBeNull();
            workspace.Settings.NoBlocks.Should().Be(26);
            workspace.Settings.NoSensors.Should().Be(91);
            
            workspace.Routes.Count.Should().Be(0); 
        }
    }
}
