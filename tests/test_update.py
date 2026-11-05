"""Tests für das Selbst-Update – ohne echtes Netz (GitHub wird nachgebaut)."""

import hashlib
import os
import re
import tempfile
import unittest
from pathlib import Path

from dokusystem.kern import einstellungen, pfade
from dokusystem_update import aktualisierung as up

from tests.hilfen import Datenordner

WURZEL = Path(__file__).resolve().parents[1]
INHALT = b"neue Version"
SUMME = hashlib.sha256(INHALT).hexdigest()


def release(tag="build-30", exe_url=None, mit_summe=True):
    assets = [{"name": up.DATEI, "size": len(INHALT),
               "browser_download_url": exe_url or up.DOWNLOAD_PRAEFIX + tag + "/" + up.DATEI}]
    if mit_summe:
        assets.append({"name": up.PRUEFSUMME, "size": 80,
                       "browser_download_url": up.DOWNLOAD_PRAEFIX + tag + "/" + up.PRUEFSUMME})
    return {"tag_name": tag, "assets": assets}


class AngebotTest(unittest.TestCase):
    def test_nur_neuere_builds(self):
        self.assertIsNone(up.angebot_aus_release(release("build-30"), 30))
        self.assertIsNone(up.angebot_aus_release(release("build-29"), 30))
        self.assertIsNone(up.angebot_aus_release({"tag_name": "v1.0"}, 30))
        angebot = up.angebot_aus_release(release("build-31"), 30)
        self.assertEqual(angebot.build, 31)
        self.assertTrue(angebot.url.endswith("/build-31/Dokumentationssystem.exe"))

    def test_fremde_adressen_und_fehlende_pruefsumme(self):
        with self.assertRaises(up.UpdateFehler):
            up.angebot_aus_release(release("build-31", exe_url="https://example.com/x.exe"), 30)
        with self.assertRaises(up.UpdateFehler):
            up.angebot_aus_release(release("build-31", mit_summe=False), 30)
        with self.assertRaises(up.UpdateFehler):
            up._anfrage("https://example.com/")

    def test_pruefsumme_lesen(self):
        self.assertEqual(up.pruefsumme_lesen(f"{SUMME.upper()}  {up.DATEI}\n"), SUMME)
        with self.assertRaises(up.UpdateFehler):
            up.pruefsumme_lesen("kaputt")


class AblaufTest(unittest.TestCase):
    def setUp(self):
        self._daten = Datenordner()
        self._daten.__enter__()
        self.tmp = tempfile.TemporaryDirectory()
        self.exe = Path(self.tmp.name) / up.DATEI
        self.exe.write_bytes(b"alte Version")
        self.gestartet = []

    def tearDown(self):
        self.tmp.cleanup()
        self._daten.__exit__(None, None, None)

    def _holen_datei(self, inhalt=INHALT):
        def holen(url, ziel, fortschritt=None):
            if url.endswith(".sha256"):
                ziel.write_text(f"{SUMME}  {up.DATEI}\n", encoding="utf-8")
            else:
                ziel.write_bytes(inhalt)
                if fortschritt:
                    fortschritt(len(inhalt), len(inhalt))
        return holen

    def _log(self):
        return pfade.im_datenordner(up.LOG).read_text(encoding="utf-8")

    def test_aktualisiert_tauscht_und_startet(self):
        ok = up.aktualisieren(self.exe, 30, holen_json=lambda url: release("build-31"),
                              holen_datei=self._holen_datei(), starten=self.gestartet.append)
        self.assertTrue(ok)
        self.assertEqual(self.exe.read_bytes(), INHALT)
        self.assertEqual(self.exe.with_name(up.DATEI + ".alt").read_bytes(), b"alte Version")
        self.assertEqual(self.gestartet, [self.exe])
        up.aufraeumen(self.exe)
        self.assertEqual(sorted(p.name for p in Path(self.tmp.name).iterdir()), [up.DATEI])
        log = self._log()
        self.assertIn("Neue Version gefunden: Build 31", log)
        self.assertIn("Prüfsumme stimmt", log)
        self.assertIn("Build 30 → Build 31", log)

    def test_aktuell_nichts_zu_tun(self):
        ok = up.aktualisieren(self.exe, 31, holen_json=lambda url: release("build-31"),
                              holen_datei=self._holen_datei(), starten=self.gestartet.append)
        self.assertFalse(ok)
        self.assertEqual(self.exe.read_bytes(), b"alte Version")
        self.assertIn("Build 31 ist aktuell", self._log())

    def test_falsche_pruefsumme_laesst_alte_version_stehen(self):
        with self.assertRaises(up.UpdateFehler):
            up.aktualisieren(self.exe, 30, holen_json=lambda url: release("build-31"),
                             holen_datei=self._holen_datei(b"beschaedigt"), starten=self.gestartet.append)
        self.assertEqual(self.exe.read_bytes(), b"alte Version")
        self.assertEqual(self.gestartet, [])
        self.assertEqual(sorted(p.name for p in Path(self.tmp.name).iterdir()), [up.DATEI])

    def test_abbrechen_im_fenster_laesst_alte_version_stehen(self):
        class Fenster:
            geschlossen = False

            def zeige(self, text, anteil):
                return anteil == 0.0  # erster Aufruf ja, beim Laden: abbrechen

            def schliessen(self):
                Fenster.geschlossen = True

        def holen(url, ziel, fortschritt=None):
            ziel.write_text(f"{SUMME}  {up.DATEI}\n", encoding="utf-8") if url.endswith(".sha256") else None
            if fortschritt and fortschritt(1, 10) is False:
                raise up.UpdateFehler("Vom Benutzer abgebrochen.")

        with self.assertRaises(up.UpdateFehler):
            up.aktualisieren(self.exe, 30, holen_json=lambda url: release("build-31"), holen_datei=holen,
                             fenster_machen=Fenster, starten=self.gestartet.append)
        self.assertTrue(Fenster.geschlossen)
        self.assertEqual(self.exe.read_bytes(), b"alte Version")
        self.assertEqual(self.gestartet, [])

    def test_log_bleibt_kurz(self):
        for i in range(up.LOG_ZEILEN + 20):
            up.log(f"Zeile {i}")
        zeilen = self._log().splitlines()
        self.assertEqual(len(zeilen), up.LOG_ZEILEN)
        self.assertTrue(zeilen[-1].endswith(f"Zeile {up.LOG_ZEILEN + 19}"))


class AbschaltenTest(unittest.TestCase):
    def test_gruende_ohne_update(self):
        with Datenordner():
            self.assertEqual(up.grund_ohne_update(["--kein-update"]), "gestartet mit --kein-update")
            self.assertEqual(up.grund_ohne_update(["--selbsttest"]), "Selbsttest")
            # Aus dem Quelltext gestartet: nie ins Netz
            self.assertEqual(up.grund_ohne_update([]), "nicht als Windows-Programm gestartet")
            self.assertFalse(up.beim_start([]))

    def test_einstellung_schaltet_ab(self):
        with Datenordner():
            self.assertIsNone(up.grund_ohne_update([], als_windows_exe=True))
            einstellungen.speichere({"auto_update": False})
            self.assertEqual(up.grund_ohne_update([], als_windows_exe=True), "in den Einstellungen abgeschaltet")


class NetzNurImUpdateTest(unittest.TestCase):
    def test_update_spricht_nur_mit_den_releases_dieses_repositorys(self):
        adressen = set()
        for datei in (WURZEL / "dokusystem_update").rglob("*.py"):
            adressen |= set(re.findall(r"https?://[^\s\"'),]+", datei.read_text(encoding="utf-8")))
        erlaubt = ("https://api.github.com/repos/KhaosDE/Dokumentationssystem/releases/latest",
                   "https://github.com/KhaosDE/Dokumentationssystem/releases/download/")
        self.assertTrue(adressen)
        self.assertTrue(all(a.startswith(erlaubt) for a in adressen), adressen)

    def test_programm_selbst_geht_nie_ins_netz(self):
        verboten = re.compile(r"\b(urllib|http\.client|socket|requests|QtNetwork|dokusystem_update)\b")
        funde = [f"{d.relative_to(WURZEL)}: {m.group(0)}"
                 for d in (WURZEL / "dokusystem").rglob("*.py")
                 for m in verboten.finditer(d.read_text(encoding="utf-8"))]
        self.assertEqual(funde, [])


class FensterTest(unittest.TestCase):
    def test_fortschrittsfenster(self):
        os.environ.setdefault("QT_QPA_PLATFORM", "offscreen")
        try:
            import PySide6  # noqa: F401
        except ImportError:  # pragma: no cover
            self.skipTest("PySide6 nicht installiert")
        from dokusystem_update.fenster import Fortschrittsfenster

        f = Fortschrittsfenster()
        self.assertTrue(f.zeige("Neue Version wird geladen (Build 30 → Build 31) …", 0.4))
        self.assertTrue(f.zeige("Build 31 wird gestartet …", None))
        f.dialog.cancel()
        self.assertFalse(f.zeige("…", 0.5))
        f.schliessen()


if __name__ == "__main__":
    unittest.main()
