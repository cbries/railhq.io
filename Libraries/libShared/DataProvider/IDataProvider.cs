// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using libShared.ExchangeProtocol;
using libShared.Entities;

namespace libShared.DataProvider
{
    [Flags]
    public enum DataProviderType
    {
        None = 0,
        ECoS50210 = 1,
        S88Feedback = 2,
        S88FeedbackSimulator = 4,
        Demo = 8,
        Z21 = 16
    }

    public interface IDataProvider
    {
        event EventHandler<IEntityBase> EntityUpdated;
        event EventHandler<IEntityBase> EntityRemoved;
        event EventHandler<EntityRemovedEventArgs> EntityRemoved2;
        event EventHandler<IEntityBase> EntityAdded;

        string Name { get; }

        DataProviderType Type { get; }

        ICollection Entities { get; }

        bool Update(IReadOnlyList<Request> requests, bool isSimulationMode);
    }

    public interface IDataProviderDemoExtension
    {
        void TriggerEntityUpdate(IEntity entity);
    }
}
