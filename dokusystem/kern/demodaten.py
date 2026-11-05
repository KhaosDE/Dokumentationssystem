"""Demo-Daten aus ``daten/demo/bewohner.json`` (lesbare Datei im Repository).

Alle Angaben dort sind ausgedacht. Sie werden beim ersten Start geladen und lassen sich über
„Demo-Daten zurücksetzen“ wiederherstellen.
"""

from __future__ import annotations

import json

from . import pfade
from .bewohner import Bewohner, aus_eingabe

DATEI = "bewohner.json"


def bewohner() -> list[Bewohner]:
    pfad = pfade.programmdaten() / "demo" / DATEI
    daten = json.loads(pfad.read_text(encoding="utf-8"))
    return [aus_eingabe(eintrag) for eintrag in daten["bewohner"]]
