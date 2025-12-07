// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

class hqUiClock extends hqUiTile {
    createElement() {
        const el = document.createElement('div');
        el.className = 'data-tile clock-tile';

        el.innerHTML = `
            <div class="clock-face">
                <div class="hand hour-hand"></div>
                <div class="hand minute-hand"></div>
                <div class="hand second-hand"></div>
            </div>
        `;

        // Minuten- und 5-Minuten-Markierungen hinzufügen
        const face = el.querySelector('.clock-face');

        const kurzeStriche = [
            "rotate(6deg) translate(0, -48px)",
            "rotate(13deg) translate(0, -48px)",
            "rotate(20deg) translate(0, -48px)",
            "rotate(27deg) translate(0, -48px)",
            "rotate(39deg) translate(0, -48px)",
            "rotate(45deg) translate(0, -48px)",
            "rotate(51deg) translate(0, -48px)",
            "rotate(57deg) translate(0, -48px)",
            "rotate(69deg) translate(0, -48px)",
            "rotate(75deg) translate(0, -48px)",
            "rotate(82deg) translate(0, -48px)",
            "rotate(88deg) translate(0, -48px)",

            "rotate(100deg) translate(0, -48px)",
            "rotate(106deg) translate(0, -48px)",
            "rotate(112deg) translate(0, -48px)",
            "rotate(117deg) translate(0, -47px)",
            "rotate(128deg) translate(0, -47px)",
            "rotate(134deg) translate(0, -47px)",
            "rotate(140deg) translate(0, -47px)",
            "rotate(145deg) translate(0, -47px)",
            "rotate(157deg) translate(0, -46px)",
            "rotate(162deg) translate(0, -46px)",
            "rotate(168deg) translate(0, -46px)",
            "rotate(174deg) translate(0, -46px)",

            "rotate(185deg) translate(0, -46px)",
            "rotate(190deg) translate(0, -46px)",
            "rotate(196deg) translate(0, -46px)",
            "rotate(202deg) translate(0, -46px)",
            "rotate(213deg) translate(0, -46px)",
            "rotate(219deg) translate(0, -46px)",
            "rotate(225deg) translate(0, -46px)",
            "rotate(231deg) translate(0, -47px)",
            "rotate(242deg) translate(0, -46px)",
            "rotate(247deg) translate(0, -46px)",
            "rotate(253deg) translate(0, -46px)",
            "rotate(259deg) translate(0, -46px)",
            "rotate(272deg) translate(0, -46px)",
            "rotate(278deg) translate(0, -46px)",
            "rotate(284deg) translate(0, -46px)",
            "rotate(290deg) translate(0, -46px)",
            "rotate(303deg) translate(0, -46px)",
            "rotate(309deg) translate(0, -47px)",
            "rotate(315deg) translate(0, -47px)",
            "rotate(322deg) translate(0, -47px)",
            "rotate(336deg) translate(0, -47px)",
            "rotate(342deg) translate(0, -47px)",
            "rotate(348deg) translate(0, -47px)",
            "rotate(354deg) translate(0, -47px)"
        ]

        // Kurze Striche
        for (const deg of kurzeStriche) {
            const tick = document.createElement('div');
            tick.className = 'tick small';
            tick.style.transform = deg;
            face.appendChild(tick);
        }

        const langeStriche = [
            "rotate(0deg) translate(0, -48px)",
            "rotate(30deg) translate(0, -48px)",
            "rotate(60deg) translate(0, -47px)",
            "rotate(90deg) translate(0, -45px)",
            "rotate(120deg) translate(0, -42px)",
            "rotate(150deg) translate(0, -41px)",
            "rotate(180deg) translate(0, -40px)",
            "rotate(210deg) translate(0, -40px)",
            "rotate(240deg) translate(0, -43px)",
            "rotate(270deg) translate(0, -44px)",
            "rotate(300deg) translate(0, -46px)",
            "rotate(330deg) translate(0, -48px)"
        ];

        // Lange Striche
        for (const deg of langeStriche) {
            const tick = document.createElement('div');
            tick.className = 'tick large';
            tick.style.transform = deg;
            face.appendChild(tick);
        }

        // Position setzen (x/y * 32px)
        if (typeof this.config.x === 'number' && typeof this.config.y === 'number') {
            el.style.left = `${this.config.x * 32}px`;
            el.style.top = `${this.config.y * 32}px`;
        }

        if (typeof this.config.draggable === 'boolean') {
            if (this.config.draggable === true) {
                $(el).draggable();
            }
        }

        const container = document.querySelector('#customerUi');
        container?.appendChild(el);

        // Animation starten
        this.startClock(el);

        return el;
    }

    startClock(el) {
        const hourHand = el.querySelector('.hour-hand');
        const minuteHand = el.querySelector('.minute-hand');
        const secondHand = el.querySelector('.second-hand');

        const updateClock = () => {
            const now = new Date();
            const sec = now.getSeconds();
            const min = now.getMinutes();
            const hr = now.getHours();

            const secDeg = sec * 6;
            const minDeg = min * 6 + sec * 0.1;
            const hrDeg = (hr % 12) * 30 + min * 0.5;

            hourHand.style.transform = `rotate(${hrDeg}deg)`;
            minuteHand.style.transform = `rotate(${minDeg}deg)`;
            secondHand.style.transform = `rotate(${secDeg}deg)`;
        };

        this.clockInterval = setInterval(updateClock, 1000);
        updateClock();
    }

    update(value) {
        // Für eine Bahnhofsuhr ist kein externes Update nötig
    }

    destroy() {
        super.destroy();
        clearInterval(this.clockInterval);
    }
}