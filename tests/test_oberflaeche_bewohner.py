"""Bereich „Bewohner“ ohne Bildschirm (Qt „offscreen“): Übersicht, Akte, Formular, Zurücksetzen."""

import os
import tempfile
import unittest
from pathlib import Path

os.environ.setdefault("QT_QPA_PLATFORM", "offscreen")

try:
    from PySide6.QtCore import Qt
    from PySide6.QtGui import QColor, QImage
except ImportError:  # pragma: no cover
    Qt = None

from tests.hilfen import Datenordner


@unittest.skipIf(Qt is None, "PySide6 nicht installiert")
class BewohnerOberflaecheTest(unittest.TestCase):
    def setUp(self):
        from dokusystem.kern.speicher import Speicher
        from dokusystem.oberflaeche.app import anwendung
        from dokusystem.oberflaeche.hauptfenster import Hauptfenster

        self._daten = Datenordner()
        self._daten.__enter__()
        self.tmp = tempfile.TemporaryDirectory()
        self.app = anwendung()
        self.speicher = Speicher()
        self.speicher.demo_beim_ersten_start()
        self.fenster = Hauptfenster(self.speicher)
        self.fenster.show()
        self.app.processEvents()
        self.bereich = self.fenster.bewohner

    def tearDown(self):
        from dokusystem.oberflaeche.app import abbauen

        abbauen(self.fenster)
        self.speicher.schliessen()
        self.tmp.cleanup()
        self._daten.__exit__(None, None, None)

    def kacheln(self):
        liste = self.bereich.uebersicht.liste
        return [liste.item(i) for i in range(liste.count())]

    def demo(self, nachname):
        return next(b for b in self.speicher.alle_bewohner() if b.nachname == nachname)

    def test_uebersicht_zeigt_alle_mit_name_und_zimmer(self):
        kacheln = self.kacheln()
        self.assertEqual(len(kacheln), len(self.speicher.alle_bewohner()))
        self.assertEqual(kacheln[0].text(), "Ahorn, Anton\nZimmer 101")
        self.assertFalse(kacheln[0].icon().isNull())  # Platzhalter mit Initialen

    def test_klick_oeffnet_akte_und_zurueck(self):
        liste = self.bereich.uebersicht.liste
        liste.itemClicked.emit(self.kacheln()[1])
        self.assertIs(self.bereich.currentWidget(), self.bereich.akte)
        self.assertEqual(self.bereich.akte.name.text(), "Berta Birke")
        self.assertEqual(self.bereich.akte.zimmer.text(), "Zimmer 102")
        self.assertEqual(self.bereich.akte.geburtsdatum.text(), "22.11.1958")
        self.bereich.akte.zurueck.emit()
        self.assertIs(self.bereich.currentWidget(), self.bereich.uebersicht)

    def test_allergiehinweis_drei_zustaende_sichtbar_verschieden(self):
        akte = self.bereich.akte
        gesehen = {}
        for nachname in ("Ahorn", "Birke", "Clematis"):
            self.bereich.akte_zeigen(self.demo(nachname).id)
            self.app.processEvents()
            self.assertTrue(akte.allergie.isVisible())
            gesehen[akte.allergie.text()] = akte.allergie.styleSheet()
        self.assertEqual(set(gesehen), {"Allergien: Haselnüsse", "Allergien: keine bekannt",
                                        "Allergien: nicht erfasst – bitte klären"})
        self.assertEqual(len(set(gesehen.values())), 3)

    def test_beschluss_in_der_akte(self):
        self.bereich.akte_zeigen(self.demo("Ahorn").id)
        self.assertEqual(self.bereich.akte.beschlussende.text(), "30.06.2027")
        self.bereich.akte_zeigen(self.demo("Birke").id)
        self.assertEqual(self.bereich.akte.beschlussende.text(), "kein Beschluss")
        self.bereich.akte_zeigen(self.demo("Ginster").id)
        self.assertEqual(self.bereich.akte.beschlussende.text(), "nicht erfasst")

    def test_neuer_bewohner_steht_auf_nicht_erfasst_und_prueft(self):
        from dokusystem.kern import bewohner as bw

        f = self.bereich.formular(None)
        self.assertEqual(f.allergie.zustand(), bw.NICHT_ERFASST)
        self.assertEqual(f.beschluss.zustand(), bw.NICHT_ERFASST)
        f.vorname.setText("Paul")
        f.nachname.setText("Pappel")
        f.zimmer.setText("111")
        f.geburtsdatum.setText("01.01.1990")
        f.aufnahmedatum.setText("01.01.1989")
        self.assertFalse(f.speichern())
        self.assertIn("Aufnahmedatum liegt vor dem Geburtsdatum", f.fehler.text())
        self.assertEqual(len(self.speicher.alle_bewohner()), 10)
        f.aufnahmedatum.setText("1.2.2026")
        self.assertTrue(f.speichern())
        neu = f.gespeichert
        self.assertEqual((neu.allergie, neu.beschluss), (bw.NICHT_ERFASST, bw.NICHT_ERFASST))
        self.bereich.uebersicht_zeigen()
        self.assertIn("Pappel, Paul\nZimmer 111", [k.text() for k in self.kacheln()])

    def test_bearbeiten_mit_bild(self):
        from dokusystem.kern import bilder

        b = self.demo("Distel")
        f = self.bereich.formular(b)
        self.assertEqual(f.vorname.text(), "Dieter")
        self.assertEqual(f.allergie.feld.text(), "Wespen- und Bienenstiche")
        kein_bild = Path(self.tmp.name) / "kein.png"
        kein_bild.write_text("kein Bild", encoding="utf-8")
        self.assertFalse(f.bild_setzen(kein_bild))
        bild = Path(self.tmp.name) / "probe.png"
        img = QImage(40, 30, QImage.Format.Format_RGB32)
        img.fill(QColor("#336699"))
        self.assertTrue(img.save(str(bild)))
        self.assertTrue(f.bild_setzen(bild))
        f.allergie.setze("keine_bekannt", "")
        f.zimmer.setText("204")
        self.assertTrue(f.speichern())
        gespeichert = self.speicher.bewohner(b.id)
        self.assertEqual(gespeichert.zimmer, "204")
        self.assertEqual(gespeichert.allergien, "")
        self.assertIsNotNone(bilder.pfad(gespeichert.bild))
        self.bereich.akte_zeigen(b.id)
        self.assertEqual(self.bereich.akte.allergie.text(), "Allergien: keine bekannt")

    def test_demo_daten_zuruecksetzen(self):
        f = self.bereich.formular(self.demo("Eiche"))
        f.zimmer.setText("999")
        self.assertTrue(f.speichern())
        self.bereich.akte_zeigen(self.demo("Eiche").id)
        self.assertTrue(self.fenster.demo_zuruecksetzen(nachfragen=False))
        self.assertIs(self.bereich.currentWidget(), self.bereich.uebersicht)
        self.assertIn("Eiche, Emil\nZimmer 105", [k.text() for k in self.kacheln()])
        self.assertEqual(len(self.kacheln()), 10)


if __name__ == "__main__":
    unittest.main()
