// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace libShared.Entities
{
    public interface IEntityBase
    {
        /// <summary>
        /// This method is used to provide data for this entity.
        /// </summary>
        /// <param name="data">
        /// The object with data to apply to this entity.
        /// In most cases the data object should be a JObject.
        /// </param>
        /// <returns>true when the data has been applied successfully</returns>
        bool ParseData(object data);

        /// <summary>
        /// This property contains the name of the driver which is
        /// repsonsible to control this entity. E.g. "ecos" is the
        /// unique name of the ESU ECoS hardware driver, all commands,
        /// accessories, etc. are within this namespace and can be
        /// individually controlled.
        /// </summary>
        [JsonProperty("driverName")] string DriverName { get; set; }

        JObject ToJsonObject();
    }
}
