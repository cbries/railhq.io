# railyHsi88Usb

The driver to open the HSI-88-USB device and to communicate with it. 

## Addressing

ESU ECoS feedback device object ids starts at "100" HSI-88-USB sends "1"-based port ids => hsiDeviceId.

In general we have:
- Device 1:0..15
- Device 2:0..15
- ...
- Device 31:0..15

The maximum amount of devices is 31.

## Official Information

https://www.ldt-infocenter.com/dokuwiki/doku.php?id=de:hsi-88-usb

- Schnelle (1.1/2.0 Full-Speed USB-Anschluss), galvanisch getrennte Verbindung zum Computer über USB. Alle USB-Geräte benötigen sog. USB-Gerätetreiber, die sie im Bereich Downloads für das HSI-88-USB herunterladen können.
- Durch 3 Rückmeldestränge verdreifacht sich außerdem die Lesegeschwindigkeit des s88-Rückmeldebusses.
- 3 Rückmeldestränge bedeutet aber auch, einfachere Anordnung der Rückmeldemodule unter Ihrer Anlage.
- Es können maximal 31*16 Rückmeldekontakte überwacht werden. Pro Busstrang maximal 31*16, jedoch können in der Summe über alle drei Stränge nicht mehr als 31*16 Kontakte eingelesen werden.
- Neben allen Standardrückmeldemodulen wie s88 von Märklin oder unseren RM-88-N / RM-DEC-88, können Sie natürlich auch unsere Rückmeldemodule mit Optokopplereingängen RM-88-N-O / RM-DEC-88-Opto und unsere Rückmeldemodule mit integrierten Gleisbelegtmeldern RM-GB-8-N / RM-GB-8 am HSI-88 betreiben.
