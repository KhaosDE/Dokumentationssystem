"""Programmstart der Oberfläche und Selbsttest der .exe."""

from __future__ import annotations

import os
import sys
import tempfile
import traceback

from PySide6.QtWidgets import QApplication, QMessageBox

from dokusystem.kern import pfade
from dokusystem.kern.speicher import Speicher, SpeicherFehler

from .hauptfenster import Hauptfenster
from .stil import STYLESHEET


def anwendung() -> QApplication:
    """Die eine QApplication (das Update-Fenster legt sie eventuell schon vorher an)."""
    app = QApplication.instance() or QApplication(sys.argv)
    app.setApplicationName("Dokumentationssystem")
    app.setStyleSheet(STYLESHEET)
    return app


def main(argv: list[str] | None = None) -> int:
    argv = sys.argv[1:] if argv is None else argv
    if "--selbsttest" in argv:
        return selbsttest()
    app = anwendung()
    try:
        speicher = Speicher()
    except SpeicherFehler as e:
        QMessageBox.critical(None, "Dokumentationssystem", str(e))
        return 1
    fenster = Hauptfenster(speicher)
    fenster.show()
    ergebnis = app.exec()
    speicher.schliessen()
    return ergebnis


def selbsttest() -> int:
    """Startet alles einmal in einem leeren Datenordner, ohne Fenster zu zeigen.

    Für die Prüfung der fertigen .exe in GitHub Actions: Fehlt etwas im Paket
    (Qt-Bausteine, Demo-Daten), endet der Selbsttest mit Exit-Code 1.
    """
    vorher = os.environ.get(pfade.UMLENKUNG)
    with tempfile.TemporaryDirectory() as ordner:
        os.environ[pfade.UMLENKUNG] = ordner
        try:
            app = anwendung()
            speicher = Speicher()
            fenster = Hauptfenster(speicher)
            app.processEvents()
            fenster.close()
            speicher.schliessen()
            return 0
        except Exception:  # noqa: BLE001 – jeder Fehler heißt: Selbsttest nicht bestanden
            with open("selbsttest.log", "w", encoding="utf-8") as f:
                f.write(traceback.format_exc())
            return 1
        finally:
            if vorher is None:
                os.environ.pop(pfade.UMLENKUNG, None)
            else:
                os.environ[pfade.UMLENKUNG] = vorher
