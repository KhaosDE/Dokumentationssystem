"""Speicher: eine SQLite-Datei im Datenordner.

Der Aufbau der Datenbank wächst mit dem Programm. Jede Erweiterung ist ein Schritt in
``_SCHRITTE``; die Datei merkt sich in ``PRAGMA user_version``, wie viele Schritte sie schon
hat. Beim Öffnen werden fehlende Schritte nachgeholt – jeder ganz oder gar nicht.
"""

from __future__ import annotations

import sqlite3
from pathlib import Path

from . import pfade

DATEI = "dokusystem.sqlite3"

_SCHRITTE: list[str] = [
    # 1 – Grundgerüst: kleine Ablage für Verwaltungsangaben
    """
    CREATE TABLE meta (
        schluessel TEXT PRIMARY KEY,
        wert       TEXT NOT NULL
    );
    """,
]

AKTUELLER_STAND = len(_SCHRITTE)


class SpeicherFehler(Exception):
    """Die Datenbank lässt sich nicht verwenden. Der Text ist für Menschen gedacht."""


class Speicher:
    def __init__(self, pfad: Path | None = None) -> None:
        self.pfad = Path(pfad) if pfad else pfade.im_datenordner(DATEI)
        self.pfad.parent.mkdir(parents=True, exist_ok=True)
        self._db = sqlite3.connect(self.pfad)
        try:
            self._db.execute("PRAGMA foreign_keys = ON")
            self._aufbauen()
        except (sqlite3.DatabaseError, SpeicherFehler) as e:
            self._db.close()  # sonst bleibt die Datei unter Windows gesperrt
            if isinstance(e, SpeicherFehler):
                raise
            raise SpeicherFehler(f"Die Datenbank {self.pfad} lässt sich nicht öffnen: {e}") from e

    # ------------------------------------------------------------------ Aufbau

    def stand(self) -> int:
        return self._db.execute("PRAGMA user_version").fetchone()[0]

    def _aufbauen(self) -> None:
        stand = self.stand()
        if stand > AKTUELLER_STAND:
            raise SpeicherFehler(
                "Die Daten stammen aus einer neueren Version des Programms. "
                "Bitte die neueste Version verwenden."
            )
        for nummer in range(stand, AKTUELLER_STAND):
            try:
                self._db.executescript(
                    "BEGIN;\n" + _SCHRITTE[nummer] + f"\nPRAGMA user_version = {nummer + 1};\nCOMMIT;"
                )
            except sqlite3.Error:
                if self._db.in_transaction:
                    self._db.rollback()
                raise

    # ------------------------------------------------------------------ Verwaltungsangaben

    def meta(self, schluessel: str) -> str | None:
        zeile = self._db.execute("SELECT wert FROM meta WHERE schluessel = ?", (schluessel,)).fetchone()
        return zeile[0] if zeile else None

    def setze_meta(self, schluessel: str, wert: str) -> None:
        with self._db:
            self._db.execute(
                "INSERT INTO meta (schluessel, wert) VALUES (?, ?) "
                "ON CONFLICT(schluessel) DO UPDATE SET wert = excluded.wert",
                (schluessel, wert),
            )

    def schliessen(self) -> None:
        self._db.close()
