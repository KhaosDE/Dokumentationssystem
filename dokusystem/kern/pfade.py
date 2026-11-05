"""Wo das Programm seine Dateien findet und speichert.

* Programmdaten (mitgelieferte Demo-Daten): Ordner ``daten`` neben dem Paket,
  in der .exe im entpackten Programmordner.
* Datenordner (Datenbank, Bilder, Einstellungen, update.log):
  Windows ``%APPDATA%\\Dokumentationssystem``, sonst ``~/.local/share/dokumentationssystem``.
  Mit der Umgebungsvariable ``DOKUSYSTEM_DATEN`` lässt er sich umlenken (Tests, Selbsttest).

Das Programm schreibt nur in den Datenordner.
"""

from __future__ import annotations

import os
import sys
from pathlib import Path

UMLENKUNG = "DOKUSYSTEM_DATEN"


def programmordner() -> Path:
    if getattr(sys, "frozen", False) and hasattr(sys, "_MEIPASS"):
        return Path(sys._MEIPASS)  # type: ignore[attr-defined]
    return Path(__file__).resolve().parents[2]


def programmdaten() -> Path:
    return programmordner() / "daten"


def datenordner() -> Path:
    umgelenkt = os.environ.get(UMLENKUNG)
    if umgelenkt:
        return Path(umgelenkt)
    if os.name == "nt":
        basis = os.environ.get("APPDATA")
        if basis:
            return Path(basis) / "Dokumentationssystem"
        return Path.home() / "AppData" / "Roaming" / "Dokumentationssystem"
    return Path.home() / ".local" / "share" / "dokumentationssystem"


def im_datenordner(*teile: str) -> Path:
    """Pfad im Datenordner. Verhindert, dass ein Pfad aus dem Ordner herausführt."""
    basis = datenordner().resolve()
    ziel = basis.joinpath(*teile).resolve()
    if ziel != basis and basis not in ziel.parents:
        raise ValueError(f"Pfad liegt außerhalb des Datenordners: {ziel}")
    return ziel
