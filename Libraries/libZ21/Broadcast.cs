// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

namespace libZ21
{
    public class Broadcast
    {
        public static Flags GetFlagConfig()
        {
            var flags = new Flags(0)
            {
                AllRailcom = true,
                AllLocomotiveInfo = true,
                Fahren_Schalten = true,
                LOCONET_Basic = true,
                LOCONET_Detect = true,
                LOCONET_Lok = true,
                LOCONET_Weichen = true,
                Railcom = true,
                RM_Bus = true,
                System_Status = true,
                CAN_Detect = true
            };

//#if DEBUG
//            flags = new Flags(0)
//            {
//                AllRailcom = false,
//                AllLocomotiveInfo = false,
//                Fahren_Schalten = false,
//                LOCONET_Basic = false,
//                LOCONET_Detect = false,
//                LOCONET_Lok = false,
//                LOCONET_Weichen = false,
//                Railcom = false,
//                RM_Bus = true,
//                System_Status = false,
//                CAN_Detect = false
//            };
//#endif

            return flags;
        }
    }
}
