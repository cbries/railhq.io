// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

namespace libZ21.EntitiesPredefined
{
    public enum EntityFileType
    {
        None,
        Locomotive,
        Accessory
    }

    public interface IPredefinedEntity
    {
        string Name { get; set; }
        EntityFileType EntityType { get; }
    }
}
