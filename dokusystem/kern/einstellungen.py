"""Einstellungen als lesbare Datei ``einstellungen.json`` im Datenordner.

Bisher nur eine: ``auto_update`` (beim Start nach einer neuen Version fragen, Standard: ja).
Eine kaputte oder fremde Datei führt nie zum Absturz; dann gelten die Standardwerte.
"""

from __future__ import annotations

import json
import os
from typing import Any

from . import pfade

DATEI = "einstellungen.json"
STANDARD: dict[str, Any] = {"auto_update": True}


def lade() -> dict[str, Any]:
    werte = dict(STANDARD)
    try:
        daten = json.loads(pfade.im_datenordner(DATEI).read_text(encoding="utf-8-sig"))
    except (OSError, ValueError):
        return werte
    if isinstance(daten, dict) and isinstance(daten.get("auto_update"), bool):
        werte["auto_update"] = daten["auto_update"]
    return werte


def speichere(werte: dict[str, Any]) -> None:
    ziel = pfade.im_datenordner(DATEI)
    ziel.parent.mkdir(parents=True, exist_ok=True)
    zwischen = ziel.with_name(ziel.name + ".neu")
    inhalt = {k: werte.get(k, v) for k, v in STANDARD.items()}
    zwischen.write_text(json.dumps(inhalt, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    os.replace(zwischen, ziel)  # erst ganz schreiben, dann tauschen: bei Absturz bleibt die alte Datei heil
