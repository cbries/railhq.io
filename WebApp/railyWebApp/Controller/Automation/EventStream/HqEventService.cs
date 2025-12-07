// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using libUtilities;

namespace railyWebApp.Controller.Automation.EventStream;

public class HqEventService : IHqEventService
{
    // Key: uid, Value: Channel für diese User-Session
    private readonly ConcurrentDictionary<string, Channel<HqEvent>> _userChannels = new();

    private const int MaxMessagesProChannel = 1;

    public HqEventService()
    {
        Logging.Log.Debug($"*** construct HqEventService() -> 0x" + GetHashCode());
    }

    /// <summary>
    /// Registriert einen neuen Event-Stream für einen Nutzer (uid).
    /// </summary>
    public IAsyncEnumerable<HqEvent> SubscribeAsync(string uid, CancellationToken ct)
    {
        var channel = Channel.CreateBounded<HqEvent>(new BoundedChannelOptions(MaxMessagesProChannel)
        {
            FullMode = BoundedChannelFullMode.DropOldest
        });

        // Neuen Channel anlegen / ersetzen
        _userChannels[uid] = channel;

        // Wenn Client disconnectet: Channel schließen und entfernen
        ct.Register(() =>
        {
            _userChannels.TryRemove(uid, out var _);

            channel.Writer.TryComplete();
        });

        return ReadAllAsync(channel.Reader, ct);
    }

    private async IAsyncEnumerable<HqEvent> ReadAllAsync(ChannelReader<HqEvent> reader, [EnumeratorCancellation] CancellationToken ct)
    {
        while (await reader.WaitToReadAsync(ct))
        {
            while (reader.TryRead(out var evt))
                yield return evt;
        }
    }

    /// <summary>
    /// Event an einen bestimmten Nutzer schicken.
    /// </summary>
    public void PublishToUser(string uid, HqEvent evt)
    {
        if (_userChannels.TryGetValue(uid, out var channel))
        {
            channel.Writer.TryWrite(evt);
        }
    }

    /// <summary>
    /// Event an alle Nutzer schicken.
    /// </summary>
    public void PublishToAll(HqEvent evt)
    {
        foreach (var channel in _userChannels.Values)
        {
            channel.Writer.TryWrite(evt);
        }
    }
}