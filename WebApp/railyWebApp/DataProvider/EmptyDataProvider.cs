// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using libShared.DataProvider;
using libShared.Entities;
using libShared.ExchangeProtocol;

namespace railyWebApp.DataProvider;

// ReSharper disable UnusedMember.Global

/// <summary>
/// Empty DataProvider to minimize null pointer exceptions.
/// </summary>
public class EmptyDataProvider(DataProviderType type) : IDataProvider
{
    public static readonly IDataProvider Empty = new EmptyDataProvider(DataProviderType.None);

    #region implementation not valid

#pragma warning disable CS0067
    public event EventHandler<IEntityBase> EntityUpdated;
    public event EventHandler<IEntityBase> EntityRemoved;
    public event EventHandler<EntityRemovedEventArgs> EntityRemoved2;
    public event EventHandler<IEntityBase> EntityAdded;
#pragma warning restore CS0067

    public DataProviderType Type { get; private set; } = type;
    public string Name => string.Empty;
    public ICollection Entities => null;
    public ICollection EntitiesS88 => null;

    public bool Update(IReadOnlyList<Request> requests, bool _)
    {
        throw new NotImplementedException();
    }

    #endregion
}