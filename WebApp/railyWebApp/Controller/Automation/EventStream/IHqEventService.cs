// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading;

namespace railyWebApp.Controller.Automation.EventStream;

public interface IHqEventService
{
    IAsyncEnumerable<HqEvent> SubscribeAsync(string uid, CancellationToken ct);
    void PublishToUser(string uid, HqEvent evt);
    public void PublishToAll(HqEvent evt);
}