// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using libShared.DataProvider;
using railyWebApp.Controller.Automation.Dto.Entity;
using System.Collections.Generic;
using System.Threading.Tasks;

// ReSharper disable PossibleMultipleEnumeration
// ReSharper disable ConvertToPrimaryConstructor

namespace railyWebApp.Controller.Automation.Services.Impl
{
    public class FeedbackService : AbstractBaseService, IFeedbackService
    {
        public FeedbackService(string uid) : base(uid)
        {
            // ignore
        }

        #region IFeedbackService

        public async Task<IEnumerable<Feedback>> GetAll()
        {
            var dps = DataProviderManager.Apply(Uid);
            if (dps == null) return new List<Feedback>();

            var res = new List<Feedback>();

            foreach (var itDp in dps)
            {
                var feedbackDp = itDp as IDataProviderFeedback;
                if (feedbackDp == null) continue;

                foreach (var itFeedback in feedbackDp.Ports)
                {
                    var driverName = itDp.Name;
                    var moduleIdx = itFeedback.Key;
                    var moduleData = itFeedback.Value;
                    var binaryState = moduleData.BinaryState;

                    var data = new Feedback
                    {
                        DriverName = driverName,
                        Module = moduleIdx,
                        BinaryState = binaryState
                    };

                    res.Add(data);
                }
            }

            return res;
        }

        public async Task<Feedback> Get(string driverName, int module)
        {
            var fb = GetFeedbackEntity(driverName, module);
            if (fb == null) return null;

            return new Feedback
            {
                DriverName = driverName,
                Module = module,
                BinaryState = fb.BinaryState
            };
        }

        #endregion
    }
}
