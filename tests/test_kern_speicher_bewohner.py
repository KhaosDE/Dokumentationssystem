"""Bewohner speichern, Bilder im Datenordner, Demo-Daten laden und zurücksetzen."""

import datetime as dt
import json
import sqlite3
import tempfile
import unittest
from pathlib import Path

from dokusystem.kern import bewohner as bw
from dokusystem.kern import bilder, demodaten, pfade
from dokusystem.kern.speicher import Speicher

from tests.hilfen import Datenordner

WURZEL = Path(__file__).resolve().parents[1]


def neuer(**aenderungen):
    werte = {"vorname": "Test", "nachname": "Tanne", "zimmer": "201",
             "geburtsdatum": "01.01.1970", "aufnahmedatum": "01.01.2020"}
    werte.update(aenderungen)
    return bw.aus_eingabe(werte)


class SpeicherTestBasis(unittest.TestCase):
    def setUp(self):
        self._daten = Datenordner()
        self._daten.__enter__()
        self.s = Speicher()
        self.tmp = tempfile.TemporaryDirectory()

    def tearDown(self):
        self.s.schliessen()
        self.tmp.cleanup()
        self._daten.__exit__(None, None, None)

    def bilddatei(self, name="foto.png", inhalt=b"\x89PNG kein echtes Bild, reicht hier"):
        datei = Path(self.tmp.name) / name
        datei.write_bytes(inhalt)
        return datei


class BewohnerSpeichernTest(SpeicherTestBasis):
    def test_anlegen_lesen_aendern(self):
        b = self.s.bewohner_speichern(neuer(allergie="eingetragen", allergien="Erdnüsse",
                                            beschluss="ende", beschlussende="31.12.2027"))
        self.assertIsNotNone(b.id)
        gelesen = self.s.bewohner(b.id)
        self.assertEqual(gelesen, b)
        self.assertEqual(gelesen.beschlussende, dt.date(2027, 12, 31))
        geaendert = self.s.bewohner_speichern(bw.aus_eingabe(
            {**bw.als_eingabe(gelesen), "zimmer": "202", "allergie": "keine_bekannt"}, id=b.id))
        self.assertEqual(self.s.bewohner(b.id).zimmer, "202")
        self.assertEqual(self.s.bewohner(b.id).allergien, "")
        self.assertEqual(geaendert.id, b.id)
        self.assertEqual(len(self.s.alle_bewohner()), 1)

    def test_ungueltiges_wird_nicht_gespeichert(self):
        b = neuer()
        b.aufnahmedatum = dt.date(1960, 1, 1)
        with self.assertRaises(bw.Eingabefehler):
            self.s.bewohner_speichern(b)
        self.assertEqual(self.s.alle_bewohner(), [])

    def test_datenbank_kennt_nur_die_drei_zustaende(self):
        with self.assertRaises(sqlite3.IntegrityError):
            with self.s._db:
                self.s._db.execute(
                    "INSERT INTO bewohner (vorname, nachname, zimmer, geburtsdatum, aufnahmedatum, allergie) "
                    "VALUES ('a', 'b', '1', '1970-01-01', '2020-01-01', '')")
        with self.s._db:
            self.s._db.execute(
                "INSERT INTO bewohner (vorname, nachname, zimmer, geburtsdatum, aufnahmedatum) "
                "VALUES ('a', 'b', '1', '1970-01-01', '2020-01-01')")
        b = self.s.alle_bewohner()[0]
        self.assertEqual((b.allergie, b.beschluss), (bw.NICHT_ERFASST, bw.NICHT_ERFASST))

    def test_sortiert_nach_nachname(self):
        for nachname in ("Zeder", "ahorn", "Mohn"):
            self.s.bewohner_speichern(neuer(nachname=nachname))
        self.assertEqual([b.nachname for b in self.s.alle_bewohner()], ["ahorn", "Mohn", "Zeder"])


class BilderTest(SpeicherTestBasis):
    def test_bild_hinterlegen_ersetzen_entfernen(self):
        b = self.s.bewohner_speichern(neuer(), neues_bild=self.bilddatei())
        erstes = bilder.pfad(b.bild)
        self.assertIsNotNone(erstes)
        self.assertEqual(erstes.parent, pfade.im_datenordner("bilder"))
        b = self.s.bewohner_speichern(b, neues_bild=self.bilddatei("zwei.JPG"))
        self.assertTrue(b.bild.endswith(".jpg"))
        self.assertFalse(erstes.exists())  # altes Bild ist weg
        zweites = bilder.pfad(b.bild)
        self.s.bewohner_speichern(b)  # ohne Bildangabe: Bild bleibt
        self.assertTrue(zweites.exists())
        b = self.s.bewohner_speichern(b, bild_entfernen=True)
        self.assertIsNone(b.bild)
        self.assertFalse(zweites.exists())

    def test_nur_bildendungen_und_kein_ausbruch(self):
        with self.assertRaises(ValueError):
            bilder.uebernehmen(self.bilddatei("text.txt"))
        self.assertIsNone(bilder.pfad("../../etwas.png"))
        self.assertIsNone(bilder.pfad(None))


class DemoDatenTest(SpeicherTestBasis):
    def test_demo_datei_ist_gueltig_und_gemischt(self):
        alle = demodaten.bewohner()
        self.assertGreaterEqual(len(alle), 10)
        self.assertEqual({b.allergie for b in alle}, set(bw.ALLERGIE_ZUSTAENDE))
        self.assertEqual({b.beschluss for b in alle}, set(bw.BESCHLUSS_ZUSTAENDE))
        self.assertTrue(all(b.bild is None for b in alle))  # Regel 2: nur Platzhalter
        self.assertEqual(len({b.zimmer for b in alle}), len(alle))

    def test_demo_datei_ohne_bilder(self):
        self.assertEqual([p.name for p in (WURZEL / "daten" / "demo").iterdir()], ["bewohner.json"])
        daten = json.loads((WURZEL / "daten" / "demo" / "bewohner.json").read_text(encoding="utf-8"))
        self.assertTrue(all("bild" not in e for e in daten["bewohner"]))

    def test_erster_start_und_zuruecksetzen(self):
        self.assertTrue(self.s.demo_beim_ersten_start())
        anzahl = len(self.s.alle_bewohner())
        self.assertGreaterEqual(anzahl, 10)
        self.assertFalse(self.s.demo_beim_ersten_start())  # nur einmal
        b = self.s.bewohner_speichern(neuer(), neues_bild=self.bilddatei())
        self.s.bewohner_speichern(b, neues_bild=self.bilddatei())
        self.s.demo_zuruecksetzen()
        self.assertEqual(len(self.s.alle_bewohner()), anzahl)
        self.assertEqual(list(bilder.bildordner().iterdir()), [])

    def test_alles_geloescht_bleibt_geloescht(self):
        self.s.demo_beim_ersten_start()
        with self.s._db:
            self.s._db.execute("DELETE FROM bewohner")
        self.s.schliessen()
        self.s = Speicher()
        self.assertFalse(self.s.demo_beim_ersten_start())
        self.assertEqual(self.s.alle_bewohner(), [])


if __name__ == "__main__":
    unittest.main()
