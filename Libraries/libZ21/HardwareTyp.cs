// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

// ReSharper disable InconsistentNaming

namespace libZ21
{
    public enum HardwareTyp
    {
        Z21_OLD, // „schwarze Z21” (Hardware-Variante ab 2012),
        Z21_NEW, // „schwarze Z21”(Hardware-Variante ab 2013)
        SMARTRAIL, // SmartRail (ab 2012)
        z21_SMALL, // „weiße z21” Starterset-Variante (ab 2013)
        z21_START, // „z21 start” Starterset-Variante (ab 2016)
        SINGLE_BOOSTER, // 10806 „Z21 Single Booster” (zLink)
        DUAL_BOOSTER, // 10807 „Z21 Dual Booster” (zLink)
        Z21_XL, // 10870 „Z21 XL Series” (ab 2020)
        XL_BOOSTER, // 10869 „Z21 XL Booster” (ab 2021, zLink)
        Z21_SWITCH_DECODER, // 10836 „Z21 SwitchDecoder” (zLink)
        Z21_SIGNAL_DECODER, // 10836 „Z21 SignalDecoder” (zLink)
        None
    }
}