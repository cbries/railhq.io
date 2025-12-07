// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace z21Pendelzug
{
    public class HoldBool
    {
        private bool _state;
        private DateTime _lastTrueTime;
        private readonly TimeSpan _holdDuration;

        public HoldBool(TimeSpan holdDuration)
        {
            _holdDuration = holdDuration;
        }

        public bool Value
        {
            get
            {
                if (_state && (DateTime.Now - _lastTrueTime) < _holdDuration)
                    return true;

                // Haltezeit abgelaufen? Dann auf false setzen
                _state = false;
                return false;
            }
            set
            {
                if (value)
                {
                    _state = true;
                    _lastTrueTime = DateTime.Now;
                }
                else if ((DateTime.Now - _lastTrueTime) >= _holdDuration)
                {
                    _state = false;
                }
                // sonst ignorieren: Haltezeit noch nicht abgelaufen
            }
        }
    }

}
