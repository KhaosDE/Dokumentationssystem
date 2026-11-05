"""Programmstart der Oberfläche und Selbsttest der .exe."""

from __future__ import annotations

import os
import sys
import tempfile
import traceback

import shiboken6
from PySide6.QtGui import QImageReader
from PySide6.QtWidgets import QApplication, QMessageBox, QWidget

from dokusystem.kern import bilder, pfade
from dokusystem.kern.speicher import Speicher, SpeicherFehler

from .hauptfenster import Hauptfenster
from .stil import STYLESHEET


def anwendung() -> QApplication:
    """Die eine QApplication (das Update-Fenster legt sie eventuell schon vorher an)."""
    app = QApplication.instance() or QApplication(sys.argv)
    app.setApplicationName("Dokumentationssystem")
    app.setStyleSheet(STYLESHEET)
    return app


def abbauen(fenster: QWidget) -> None:
    """Fenster sofort löschen, solange die QApplication noch lebt.

    Signal-Verbindungen halten Fenster über Referenzkreise am Leben; ohne diesen Schritt
    räumt PySide sie erst beim Python-Ende ab – und das kann abstürzen.
    """
    if shiboken6.isValid(fenster):
        fenster.close()
        shiboken6.delete(fenster)


def main(argv: list[str] | None = None) -> int:
    argv = sys.argv[1:] if argv is None else argv
    if "--selbsttest" in argv:
        return selbsttest()
    app = anwendung()
    try:
        speicher = Speicher()
        speicher.demo_beim_ersten_start()
    except SpeicherFehler as e:
        QMessageBox.critical(None, "Dokumentationssystem", str(e))
        return 1
    fenster = Hauptfenster(speicher)
    fenster.show()
    ergebnis = app.exec()
    abbauen(fenster)
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
            formate = {bytes(f).decode() for f in QImageReader.supportedImageFormats()}
            fehlend = {e.lstrip(".") for e in bilder.ENDUNGEN} - formate
            if fehlend:
                raise RuntimeError(f"Bildformate fehlen: {sorted(fehlend)}")
            speicher = Speicher()
            if not speicher.demo_beim_ersten_start() or not speicher.alle_bewohner():
                raise RuntimeError("Demo-Daten fehlen.")
            fenster = Hauptfenster(speicher)
            fenster.bewohner.akte_zeigen(speicher.alle_bewohner()[0].id)
            app.processEvents()
            abbauen(fenster)
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
