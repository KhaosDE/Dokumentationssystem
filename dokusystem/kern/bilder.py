"""Bewohnerbilder im Datenordner (Unterordner ``bilder``), nie im Repository.

Die Datenbank merkt sich nur den Dateinamen. Ob eine Datei wirklich ein Bild ist, prüft die
Oberfläche beim Auswählen (Qt kann das, der Kern kommt ohne Qt aus).
"""

from __future__ import annotations

import shutil
import uuid
from pathlib import Path

from . import pfade

ORDNER = "bilder"
ENDUNGEN = (".png", ".jpg", ".jpeg", ".bmp", ".gif")


def bildordner() -> Path:
    return pfade.im_datenordner(ORDNER)


def uebernehmen(quelle: Path) -> str:
    """Kopiert ein Bild in den Datenordner und gibt den neuen Dateinamen zurück."""
    quelle = Path(quelle)
    endung = quelle.suffix.lower()
    if endung not in ENDUNGEN:
        raise ValueError(f"Bilder bitte als {', '.join(ENDUNGEN)}.")
    name = uuid.uuid4().hex + endung
    ziel = pfade.im_datenordner(ORDNER, name)
    ziel.parent.mkdir(parents=True, exist_ok=True)
    shutil.copyfile(quelle, ziel)
    return name


def pfad(name: str | None) -> Path | None:
    """Pfad zum hinterlegten Bild – oder None, wenn keins da ist."""
    if not name:
        return None
    try:
        datei = pfade.im_datenordner(ORDNER, name)
    except ValueError:
        return None
    return datei if datei.is_file() else None


def loeschen(name: str | None) -> None:
    datei = pfad(name)
    if datei:
        try:
            datei.unlink()
        except OSError:
            pass  # z. B. gerade geöffnet – bleibt liegen, stört nicht


def alle_loeschen() -> None:
    ordner = bildordner()
    if ordner.is_dir():
        for datei in ordner.iterdir():
            if datei.is_file():
                try:
                    datei.unlink()
                except OSError:
                    pass
