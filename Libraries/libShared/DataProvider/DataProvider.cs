// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using libShared.Entities;
using libShared.ExchangeProtocol;

namespace libShared.DataProvider
{
    public enum RemoveType
    {
        Accessory, Locomotive, None
    }

    public enum AddType
    {
        Accessory, Locomotive, LocomotiveFunction, None
    }

    public class EntityRemovedEventArgs(string driverName, int objectId, RemoveType removeType) : EventArgs
    {
        public string DriverName { get; } = driverName;
        public int ObjectId { get; } = objectId;
        public RemoveType RemoveType { get; } = removeType;
    }

    public abstract class DataProvider : IDataProvider
    {
        #region data event handling

        private event EventHandler<IEntityBase> _entityUpdated;
        private event EventHandler<IEntityBase> _entityRemoved;
        private event EventHandler<EntityRemovedEventArgs> _entityRemoved2;
        private event EventHandler<IEntityBase> _entityAdded;

        public event EventHandler<IEntityBase> EntityUpdated
        {
            add => _entityUpdated += value;
            remove => _entityUpdated -= value;
        }

        public event EventHandler<IEntityBase> EntityRemoved
        {
            add => _entityRemoved += value;
            remove => _entityRemoved -= value;
        }

        public event EventHandler<EntityRemovedEventArgs> EntityRemoved2
        {
            add => _entityRemoved2 += value;
            remove => _entityRemoved2 -= value;
        }

        public event EventHandler<IEntityBase> EntityAdded
        {
            add => _entityAdded += value;
            remove => _entityAdded -= value;
        }

        protected virtual void OnEntityUpdated(IEntityBase entity)
        {
            _entityUpdated?.Invoke(this, entity);
        }

        protected virtual void OnEntityRemoved(IEntityBase entity)
        {
            _entityRemoved?.Invoke(this, entity);
        }

        protected virtual void OnEntityRemoved(string driverName, int objectId, RemoveType removeType)
        {
            _entityRemoved2?.Invoke(this, new EntityRemovedEventArgs(driverName, objectId, removeType));
        }

        protected virtual void OnEntityAdded(IEntityBase entity)
        {
            _entityAdded?.Invoke(this, entity);
        }

        #endregion

        public abstract string Name { get; }

        public virtual DataProviderType Type => DataProviderType.None;

        public virtual ICollection Entities { get; } = new ConcurrentBag<IEntityBase>();

        public abstract bool Update(IReadOnlyList<Request> requests, bool isSimulationMode);
    }

    public interface IEntityInverter
    {
        int GetAccessoryState(int objectId);
        int GetAccessoryState(IEntity entity);

        void SetAccessoryState(int objectId, int state);
        void SetAccessoryState(IEntity entity, int state);

        /// <summary>
        /// Invertierte Umschaltung des Schaltartikel
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        int InvertAccessoryState(IEntity entity);

        /// <summary>
        /// Normale Umschaltung (keine Invertierung) des Schaltartikel
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        int ChangeAccessoryState(IEntity entity);
    }
}
