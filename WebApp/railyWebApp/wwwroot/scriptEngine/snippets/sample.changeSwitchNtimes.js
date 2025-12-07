// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

const snippetMetainformation = {
    label: "hqSample.toggleSwitchRepeatedly",
    description: "Schaltet eine Weiche mehrmals zwischen Geradeaus und Abzweig hin und her."
};

const driverName = "ecos";
const objectId = 20001;
const N = 10;

const Turn = 1;
const Straight = 0;

// Schaltet eine Weiche N mal.
for(let i = 0; i < N; ++i){
   let acc0 = await hqAccessory.getStatus(driverName, objectId);
    if(acc0.currentState == Straight) {
       await hqAccessory.switch(driverName, objectId, Turn);
    }  else {
       await hqAccessory.switch(driverName, objectId, Straight); 
    }

    await hqHelper.sleep1Sec();
}
