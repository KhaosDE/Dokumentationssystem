"""Speicher: eine SQLite-Datei im Datenordner.

Der Aufbau der Datenbank wächst mit dem Programm. Jede Erweiterung ist ein Schritt in
``_SCHRITTE``; die Datei merkt sich in ``PRAGMA user_version``, wie viele Schritte sie schon
hat. Beim Öffnen werden fehlende Schritte nachgeholt – jeder ganz oder gar nicht.
"""

from __future__ import annotations

import dataclasses
import datetime as _dt
import sqlite3
from pathlib import Path

from . import bilder, demodaten, pfade
from .bewohner import Bewohner, Eingabefehler, pruefen, sortierschluessel

DATEI = "dokusystem.sqlite3"
DEMO_GELADEN = "demo_geladen"

_SCHRITTE: list[str] = [
    # 1 – Grundgerüst: kleine Ablage für Verwaltungsangaben
    """
    CREATE TABLE meta (
        schluessel TEXT PRIMARY KEY,
        wert       TEXT NOT NULL
    );
    """,
    # 2 – Bewohnerakte. Zustände mit CHECK, damit nie ein leerer Wert wie „keine“ aussieht.
    """
    CREATE TABLE bewohner (
        id            INTEGER PRIMARY KEY,
        vorname       TEXT NOT NULL,
        nachname      TEXT NOT NULL,
        zimmer        TEXT NOT NULL,
        geburtsdatum  TEXT NOT NULL,              -- JJJJ-MM-TT
        aufnahmedatum TEXT NOT NULL,              -- JJJJ-MM-TT
        allergie      TEXT NOT NULL DEFAULT 'nicht_erfasst'
                      CHECK (allergie IN ('eingetragen', 'keine_bekannt', 'nicht_erfasst')),
        allergien     TEXT NOT NULL DEFAULT '',
        beschluss     TEXT NOT NULL DEFAULT 'nicht_erfasst'
                      CHECK (beschluss IN ('ende', 'kein_beschluss', 'nicht_erfasst')),
        beschlussende TEXT,                       -- JJJJ-MM-TT, nur bei beschluss = 'ende'
        bild          TEXT                        -- Dateiname im Ordner „bilder“
    );
    """,
]

_SPALTEN = ("vorname", "nachname", "zimmer", "geburtsdatum", "aufnahmedatum",
            "allergie", "allergien", "beschluss", "beschlussende", "bild")

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

    # ------------------------------------------------------------------ Bewohner

    def alle_bewohner(self) -> list[Bewohner]:
        zeilen = self._db.execute(f"SELECT id, {', '.join(_SPALTEN)} FROM bewohner").fetchall()
        return sorted((_aus_zeile(z) for z in zeilen), key=sortierschluessel)

    def bewohner(self, id: int) -> Bewohner | None:
        zeile = self._db.execute(f"SELECT id, {', '.join(_SPALTEN)} FROM bewohner WHERE id = ?",
                                 (id,)).fetchone()
        return _aus_zeile(zeile) if zeile else None

    def bewohner_speichern(self, b: Bewohner, neues_bild: Path | None = None,
                           bild_entfernen: bool = False) -> Bewohner:
        """Legt an (ohne id) oder ändert (mit id). Gibt den gespeicherten Bewohner zurück.

        ``neues_bild`` wird in den Datenordner kopiert; das bisherige Bild wird danach gelöscht.
        """
        fehler = pruefen(b)
        if fehler:
            raise Eingabefehler(fehler)
        vorher = self.bewohner(b.id) if b.id is not None else None
        if b.id is not None and vorher is None:
            raise Eingabefehler(["Diesen Bewohner gibt es nicht mehr."])
        bild = vorher.bild if vorher else b.bild
        if neues_bild is not None:
            bild = bilder.uebernehmen(neues_bild)
        elif bild_entfernen:
            bild = None
        b = dataclasses.replace(b, bild=bild)
        werte = _zu_zeile(b)
        try:
            with self._db:
                if b.id is None:
                    cursor = self._db.execute(
                        f"INSERT INTO bewohner ({', '.join(_SPALTEN)}) VALUES ({', '.join('?' * len(_SPALTEN))})",
                        werte)
                    b = dataclasses.replace(b, id=cursor.lastrowid)
                else:
                    self._db.execute(
                        f"UPDATE bewohner SET {', '.join(s + ' = ?' for s in _SPALTEN)} WHERE id = ?",
                        (*werte, b.id))
        except sqlite3.Error:
            if neues_bild is not None:
                bilder.loeschen(bild)
            raise
        if vorher and vorher.bild and vorher.bild != b.bild:
            bilder.loeschen(vorher.bild)
        return b

    # ------------------------------------------------------------------ Demo-Daten

    def demo_beim_ersten_start(self) -> bool:
        """Lädt die Demo-Daten, wenn das noch nie geschehen ist. True = gerade geladen."""
        if self.meta(DEMO_GELADEN):
            return False
        self.demo_zuruecksetzen()
        return True

    def demo_zuruecksetzen(self) -> None:
        """Alle Bewohner und Bilder weg, Demo-Daten neu laden – alles oder nichts."""
        demo = demodaten.bewohner()  # erst lesen: ist die Datei kaputt, bleibt alles, wie es war
        with self._db:
            self._db.execute("DELETE FROM bewohner")
            self._db.executemany(
                f"INSERT INTO bewohner ({', '.join(_SPALTEN)}) VALUES ({', '.join('?' * len(_SPALTEN))})",
                [_zu_zeile(b) for b in demo])
            self._db.execute(
                "INSERT INTO meta (schluessel, wert) VALUES (?, '1') "
                "ON CONFLICT(schluessel) DO UPDATE SET wert = excluded.wert", (DEMO_GELADEN,))
        bilder.alle_loeschen()

    def schliessen(self) -> None:
        self._db.close()


def _datum(text: str | None) -> _dt.date | None:
    return _dt.date.fromisoformat(text) if text else None


def _aus_zeile(z: tuple) -> Bewohner:
    werte = dict(zip(("id", *_SPALTEN), z))
    for feld in ("geburtsdatum", "aufnahmedatum", "beschlussende"):
        werte[feld] = _datum(werte[feld])
    return Bewohner(**werte)


def _zu_zeile(b: Bewohner) -> tuple:
    werte = []
    for spalte in _SPALTEN:
        wert = getattr(b, spalte)
        werte.append(wert.isoformat() if isinstance(wert, _dt.date) else wert)
    return tuple(werte)
