"""Oberfläche ohne Bildschirm (Qt „offscreen“)."""

import os
import unittest

os.environ.setdefault("QT_QPA_PLATFORM", "offscreen")

try:
    from PySide6.QtWidgets import QApplication
except ImportError:  # pragma: no cover
    QApplication = None

from tests.hilfen import Datenordner


@unittest.skipIf(QApplication is None, "PySide6 nicht installiert")
class HauptfensterTest(unittest.TestCase):
    def setUp(self):
        from dokusystem.oberflaeche.app import anwendung
        from dokusystem.kern.speicher import Speicher

        self._daten = Datenordner()
        self._daten.__enter__()
        self.app = anwendung()
        self.speicher = Speicher()

    def tearDown(self):
        self.speicher.schliessen()
        self._daten.__exit__(None, None, None)

    def _fenster(self):
        from dokusystem.oberflaeche.hauptfenster import Hauptfenster

        fenster = Hauptfenster(self.speicher)
        self.addCleanup(fenster.close)
        fenster.show()
        self.app.processEvents()
        return fenster

    def test_demo_hinweis_navigation_und_version(self):
        fenster = self._fenster()
        self.assertTrue(fenster.demo_hinweis.isVisible())
        self.assertIn("Demo — ausgedachte Daten", fenster.demo_hinweis.text())
        self.assertIn("Demo", fenster.windowTitle())
        namen = [fenster.navigation.item(i).text() for i in range(fenster.navigation.count())]
        self.assertEqual(namen[0], "Bewohner")
        self.assertRegex(fenster.version.text(), r"^Version \d+\.\d+\.\d+ · ")

    def test_update_haekchen_schreibt_einstellung(self):
        from dokusystem.kern import einstellungen

        fenster = self._fenster()
        self.assertTrue(fenster.auto_update.isChecked())
        fenster.auto_update.setChecked(False)
        self.assertFalse(einstellungen.lade()["auto_update"])
        fenster.auto_update.setChecked(True)
        self.assertTrue(einstellungen.lade()["auto_update"])

    def test_selbsttest(self):
        from dokusystem.oberflaeche.app import selbsttest

        vorher = os.environ.get("DOKUSYSTEM_DATEN")
        self.assertEqual(selbsttest(), 0)
        self.assertEqual(os.environ.get("DOKUSYSTEM_DATEN"), vorher)


if __name__ == "__main__":
    unittest.main()
