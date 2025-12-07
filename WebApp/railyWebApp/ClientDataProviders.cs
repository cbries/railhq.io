// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using libShared.DataProvider;
using libShared.Entities;

namespace railyWebApp
{
    /// <summary>
    /// IDataProviders wird von den unterschiedlichen Bibliotheken
    /// die die jeweiligen Datenmodelle kennen zur Verfügung gestellt.
    /// Der Zugriff auf die Entitäten erfolgt über einheitliche Schnittstellen.
    /// Beispielsweise wird ein `libEsuEcos.DataProvider.DataProvider` wissen,
    /// wie die Entitäten für ESU ECoS zu verwalten sind.
    /// `ClientDataProviders` beinhaltet alle `DataProvider` die ein Client
    /// abonnuiert hat, also für die dieser freigeschaltet ist.
    /// </summary>
    public class ClientDataProviders : List<IDataProvider>
    {
        public event EventHandler<IEntityBase> EntityUpdated;
        public event EventHandler<IEntityBase> EntityRemoved;
        public event EventHandler<EntityRemovedEventArgs> EntityRemoved2;
        public event EventHandler<IEntityBase> EntityAdded;

        #region Event Handling Detour

        public new void Add(IDataProvider item)
        {
            if (item == null) return;

            item.EntityUpdated += ItemOnEntityUpdated;
            item.EntityRemoved += ItemOnEntityRemoved;
            item.EntityRemoved2 += ItemOnEntityRemoved2;
            item.EntityAdded += ItemOnEntityAdded;

            base.Add(item);
        }
        
        public new bool Remove(IDataProvider item)
        {
            if (item == null) return false;

            item.EntityUpdated -= ItemOnEntityUpdated;
            item.EntityRemoved -= ItemOnEntityRemoved;
            item.EntityRemoved2 -= ItemOnEntityRemoved2;
            item.EntityAdded -= ItemOnEntityAdded;

            return base.Remove(item);
        }

        private void ItemOnEntityAdded(object sender, IEntityBase e)
        {
            EntityAdded?.Invoke(sender, e);
        }

        private void ItemOnEntityRemoved(object sender, IEntityBase e)
        {
            EntityRemoved?.Invoke(sender, e);
        }

        private void ItemOnEntityRemoved2(object sender, EntityRemovedEventArgs e)
        {
            EntityRemoved2?.Invoke(sender, e);
        }

        private void ItemOnEntityUpdated(object sender, IEntityBase e)
        {
            EntityUpdated?.Invoke(sender, e);
        }

        #endregion

        public bool Has(DataProviderType type)
        {
            return this.Any(p => p.Type == type);
        }

        public IReadOnlyList<IDataProvider> Get(DataProviderType type)
        {
            var res = new List<IDataProvider>();
            foreach (var p in this)
            {
                if (p.Type.HasFlag(type))
                    res.Add(p);
            }

            return res;
        }
    }
}
