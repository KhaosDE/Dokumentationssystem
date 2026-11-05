"""Startdatei für Doppelklick und für PyInstaller (.exe).

In der .exe fragt zuerst das Selbst-Update (Paket dokusystem_update), ob es eine neuere Version
gibt. Ist eine geladen und gestartet, endet dieses Programm; sonst startet das Programm normal.
"""

import sys

if __name__ == "__main__":
    from dokusystem_update import beim_start

    if beim_start(sys.argv[1:]):
        sys.exit(0)
    from dokusystem.oberflaeche.app import main

    sys.exit(main(sys.argv[1:]))
