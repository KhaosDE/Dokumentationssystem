"""Datenordner, Einstellungen und SQLite-Speicher."""

import sqlite3
import unittest
from pathlib import Path

from dokusystem.kern import einstellungen, pfade
from dokusystem.kern.speicher import AKTUELLER_STAND, Speicher, SpeicherFehler

from tests.hilfen import Datenordner


class PfadeTest(unittest.TestCase):
    def test_umlenkung_und_schutz_vor_ausbruch(self):
        with Datenordner() as ordner:
            self.assertEqual(pfade.datenordner(), Path(ordner))
            self.assertEqual(pfade.im_datenordner("bilder", "a.png").parent.name, "bilder")
            with self.assertRaises(ValueError):
                pfade.im_datenordner("..", "anderswo.txt")

    def test_programmdaten_liegen_im_repository(self):
        self.assertTrue((pfade.programmdaten() / "demo").is_dir())


class EinstellungenTest(unittest.TestCase):
    def test_standard_speichern_und_kaputte_datei(self):
        with Datenordner():
            self.assertTrue(einstellungen.lade()["auto_update"])
            einstellungen.speichere({"auto_update": False})
            self.assertFalse(einstellungen.lade()["auto_update"])
            pfade.im_datenordner(einstellungen.DATEI).write_text("{kaputt", encoding="utf-8")
            self.assertTrue(einstellungen.lade()["auto_update"])
            pfade.im_datenordner(einstellungen.DATEI).write_text('{"auto_update": "nein"}', encoding="utf-8")
            self.assertTrue(einstellungen.lade()["auto_update"])


class SpeicherTest(unittest.TestCase):
    def test_legt_datei_im_datenordner_an_und_baut_auf(self):
        with Datenordner() as ordner:
            s = Speicher()
            self.assertEqual(s.pfad.parent, Path(ordner).resolve())
            self.assertEqual(s.stand(), AKTUELLER_STAND)
            s.setze_meta("probe", "1")
            s.setze_meta("probe", "2")
            s.schliessen()
            s = Speicher()  # zweites Öffnen: nichts doppelt, Daten noch da
            self.assertEqual(s.meta("probe"), "2")
            self.assertIsNone(s.meta("gibt-es-nicht"))
            s.schliessen()

    def test_daten_aus_neuerer_version_werden_nicht_angefasst(self):
        with Datenordner():
            s = Speicher()
            pfad = s.pfad
            s.schliessen()
            db = sqlite3.connect(pfad)
            db.execute(f"PRAGMA user_version = {AKTUELLER_STAND + 1}")
            db.close()
            with self.assertRaises(SpeicherFehler):
                Speicher()

    def test_kaputte_datei_gibt_verstaendlichen_fehler(self):
        with Datenordner():
            pfad = pfade.im_datenordner("dokusystem.sqlite3")
            pfad.write_bytes(b"das ist keine Datenbank" * 100)
            with self.assertRaises(SpeicherFehler):
                Speicher()


if __name__ == "__main__":
    unittest.main()
