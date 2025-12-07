// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using libShared.DataProvider;
using libShared.Entities;
using Newtonsoft.Json.Linq;
// ReSharper disable DuplicatedStatements

namespace railyWebApp.Playground;

public class PgDpHelper
{
    internal static JObject __getEntityFromDp(IDataProvider dp, int objectId)
    {
        //
        // z21
        //
        if (dp is libZ21.DataProvider.DataProvider dpZ21)
        {
            foreach (var it in dpZ21.LocomotivesEntities)
            {
                var entity = it as IEntity;
                if (entity == null) continue;
                if (entity.ObjectId < 0) continue;
                if (entity.ObjectId == objectId)
                {
                    var obj = __getObject(entity);
                    if (obj == null) continue;
                    return obj;
                }
            }

            foreach (var it in dpZ21.AccessoriesEntities)
            {
                var entity = it as IEntity;
                if (entity == null) continue;
                if (entity.ObjectId < 0) continue;
                if (entity.ObjectId == objectId)
                {
                    var obj = __getObject(entity);
                    if (obj == null) continue;
                    return obj;
                }
            }

            foreach (var it in dpZ21.GeneralEntities)
            {
                var entity = it as IEntity;
                if (entity == null) continue;
                if (entity.ObjectId < 0) continue;
                if (entity.ObjectId == objectId)
                {
                    var obj = __getObject(entity);
                    if (obj == null) continue;
                    return obj;
                }
            }
        }
        else
        {
            //
            // ECoS and Demo
            //
            foreach (var it in dp.Entities)
            {
                var entity = it as IEntity;
                if (entity == null) continue;
                if (entity.ObjectId < 0) continue;
                if (entity.ObjectId != objectId) continue;

                var obj = __getObject(entity);
                if (obj == null) continue;
                return obj;
            }
        }

        return null;
    }

    internal static JObject __getDataFromDp(IDataProvider dp)
    {
        var data = new JObject();

        if (dp.Type.HasFlag(DataProviderType.Z21))
        {
            //
            // z21
            //
            var arrLocomotives = new JArray();
            var arrAccessories = new JArray();

            if (dp is libZ21.DataProvider.DataProvider dpZ21)
            {
                foreach (var it in dpZ21.LocomotivesEntities)
                {
                    if (it is not IEntity entity) continue;
                    if (entity.ObjectId < 0) continue;
                    var jsonObj = __getObject(entity);
                    if (jsonObj == null) continue;

                    arrLocomotives.Add(jsonObj);
                }

                foreach (var it in dpZ21.AccessoriesEntities)
                {
                    if (it is not IEntity entity) continue;
                    if (entity.ObjectId < 0) continue;
                    var jsonObj = __getObject(entity);
                    if (jsonObj == null) continue;

                    arrAccessories.Add(jsonObj);
                }

                foreach (var it in dpZ21.GeneralEntities)
                {
                    if (it is not IEntity entity) continue;
                    if (entity.ObjectId < 0) continue;
                    var jsonObj = __getObject(entity);
                    if (jsonObj == null) continue;

                    if (entity.Type == EntityType.Z21Station)
                    {
                        data.Add("z21base", jsonObj);
                        break;
                    }
                }
            }

            if (arrLocomotives.Count > 0) data.Add("locomotives", arrLocomotives);
            if (arrAccessories.Count > 0) data.Add("accessories", arrAccessories);
        }
        else
        {
            //
            // ECoS and Demo
            //
            var arrLocomotives = new JArray();
            var arrAccessories = new JArray();

            foreach (var it in dp.Entities)
            {
                if (it is not IEntity entity) continue;
                if(entity.ObjectId < 0) continue;
                var jsonObj = __getObject(entity);
                if (jsonObj == null) continue;

                switch (entity.Type)
                {
                    case EntityType.Locomotive:
                        arrLocomotives.Add(jsonObj);
                        break;
                    case EntityType.Accessory:
                        arrAccessories.Add(jsonObj);
                        break;
                    case EntityType.Ecos2:
                        data.Add("ecosbase", jsonObj);
                        break;
                }
            }

            if (arrLocomotives.Count > 0) data.Add("locomotives", arrLocomotives);
            if (arrAccessories.Count > 0) data.Add("accessories", arrAccessories);
        }
        
        return data;
    }

    private static JObject __getObject(IEntity entity)
    {
        return entity?.ToJsonObject();
    }
}