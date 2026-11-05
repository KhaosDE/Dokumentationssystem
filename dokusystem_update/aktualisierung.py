"""Prüfen, laden, Prüfsumme vergleichen, austauschen, neu starten. Netz nur über die Funktionen hier."""

from __future__ import annotations

import datetime as _dt
import hashlib
import json
import os
import re
import subprocess
import sys
import urllib.request
from dataclasses import dataclass
from pathlib import Path
from typing import Callable

API_NEUESTE = "https://api.github.com/repos/KhaosDE/Dokumentationssystem/releases/latest"
DOWNLOAD_PRAEFIX = "https://github.com/KhaosDE/Dokumentationssystem/releases/download/"
DATEI = "Dokumentationssystem.exe"
PRUEFSUMME = "Dokumentationssystem.exe.sha256"
LOG = "update.log"
LOG_ZEILEN = 300           # so viele Zeilen behält update.log
ZEITLIMIT_PRUEFEN = 4      # Sekunden: so lange darf die Frage nach einer neuen Version dauern
ZEITLIMIT_LADEN = 30       # Sekunden ohne Daten, dann Abbruch
FRISCH = "DOKUSYSTEM_FRISCH_AKTUALISIERT"  # gesetzt für die gerade gestartete neue Version
_TAG = re.compile(r"^build-(\d+)$")


class UpdateFehler(Exception):
    """Etwas hat nicht geklappt – das Programm startet dann in der alten Version."""


@dataclass
class Angebot:
    build: int
    url: str
    pruefsumme_url: str
    groesse: int


# --------------------------------------------------------------------------- Netz (nur hier)

def _anfrage(url: str) -> urllib.request.Request:
    if not (url == API_NEUESTE or url.startswith(DOWNLOAD_PRAEFIX)):
        raise UpdateFehler(f"Adresse nicht erlaubt: {url}")
    return urllib.request.Request(url, headers={"User-Agent": "Dokumentationssystem-Update",
                                                "Accept": "application/vnd.github+json"})


def hole_json(url: str) -> dict:
    with urllib.request.urlopen(_anfrage(url), timeout=ZEITLIMIT_PRUEFEN) as antwort:
        return json.loads(antwort.read().decode("utf-8"))


def hole_datei(url: str, ziel: Path, fortschritt: Callable[[int, int], bool] | None = None) -> None:
    """Lädt in eine Datei. ``fortschritt(gelesen, gesamt)`` darf False liefern = abbrechen."""
    with urllib.request.urlopen(_anfrage(url), timeout=ZEITLIMIT_LADEN) as antwort, open(ziel, "wb") as f:
        gesamt = int(antwort.headers.get("Content-Length") or 0)
        gelesen = 0
        while True:
            stueck = antwort.read(64 * 1024)
            if not stueck:
                break
            f.write(stueck)
            gelesen += len(stueck)
            if fortschritt and fortschritt(gelesen, gesamt) is False:
                raise UpdateFehler("Vom Benutzer abgebrochen.")


# --------------------------------------------------------------------------- Logik (ohne Netz testbar)

def build_aus_tag(tag: object) -> int | None:
    treffer = _TAG.match(tag) if isinstance(tag, str) else None
    return int(treffer.group(1)) if treffer else None


def angebot_aus_release(release: dict, eigener_build: int) -> Angebot | None:
    """Neuerer Build im Release? Dann Download-Adressen; sonst None."""
    build = build_aus_tag(release.get("tag_name"))
    if build is None or build <= eigener_build:
        return None
    dateien = {a.get("name"): a for a in release.get("assets", []) if isinstance(a, dict)}
    exe, summe = dateien.get(DATEI), dateien.get(PRUEFSUMME)
    if not exe or not summe:
        raise UpdateFehler(f"Release build-{build} ohne {DATEI} oder {PRUEFSUMME}.")
    for a in (exe, summe):
        url = a.get("browser_download_url", "")
        if not isinstance(url, str) or not url.startswith(DOWNLOAD_PRAEFIX):
            raise UpdateFehler(f"Unerwartete Download-Adresse: {url}")
    return Angebot(build, exe["browser_download_url"], summe["browser_download_url"], int(exe.get("size") or 0))


def sha256(pfad: Path) -> str:
    h = hashlib.sha256()
    with open(pfad, "rb") as f:
        for stueck in iter(lambda: f.read(1024 * 1024), b""):
            h.update(stueck)
    return h.hexdigest()


def pruefsumme_lesen(text: str) -> str:
    """Erstes Wort der .sha256-Datei (Format „<hash>  Dokumentationssystem.exe“)."""
    wort = text.strip().split()[0].lower() if text.strip() else ""
    if not re.fullmatch(r"[0-9a-f]{64}", wort):
        raise UpdateFehler("Prüfsummen-Datei unlesbar.")
    return wort


def austauschen(exe: Path, neu: Path) -> Path:
    """Laufende exe → .alt umbenennen (geht unter Windows auch im Betrieb), neue an ihren Platz."""
    alt = exe.with_name(exe.name + ".alt")
    if alt.exists():
        alt.unlink()
    exe.rename(alt)
    try:
        neu.rename(exe)
    except OSError:
        alt.rename(exe)  # zurück, damit das Programm weiter startbar bleibt
        raise
    return alt


def aufraeumen(exe: Path) -> None:
    """Reste eines früheren Updates löschen (die alte Datei ist jetzt nicht mehr in Benutzung)."""
    for rest in (exe.with_name(exe.name + ".alt"), exe.with_name(exe.name + ".neu"),
                 exe.with_name(exe.name + ".sha256")):
        try:
            if rest.exists():
                rest.unlink()
        except OSError:
            pass  # läuft vielleicht noch kurz – nächstes Mal


def starte(exe: Path) -> None:
    """Startet die neue Version als eigenständiges Programm (Hinweis aus der PyInstaller-Doku)."""
    umgebung = {**os.environ, "PYINSTALLER_RESET_ENVIRONMENT": "1", FRISCH: "1"}
    subprocess.Popen([str(exe)], env=umgebung, close_fds=True)


# --------------------------------------------------------------------------- Protokoll

def log(text: str) -> None:
    """Eine Zeile an update.log im Datenordner anhängen; nur die letzten LOG_ZEILEN bleiben."""
    try:
        from dokusystem.kern import pfade
        datei = pfade.im_datenordner(LOG)
        datei.parent.mkdir(parents=True, exist_ok=True)
        alt = datei.read_text(encoding="utf-8", errors="replace").splitlines() if datei.exists() else []
        zeit = _dt.datetime.now().replace(microsecond=0).strftime("%d.%m.%Y %H:%M:%S")
        zeilen = (alt + [f"{zeit}  {text}"])[-LOG_ZEILEN:]
        datei.write_text("\n".join(zeilen) + "\n", encoding="utf-8")
    except Exception:  # noqa: BLE001 – das Protokoll darf den Start nie verhindern
        pass


# --------------------------------------------------------------------------- Ablauf

def grund_ohne_update(argv: list[str], als_windows_exe: bool | None = None) -> str | None:
    """Warum diesmal nicht nachgefragt wird – oder None, wenn nachgefragt werden soll."""
    if als_windows_exe is None:
        als_windows_exe = bool(getattr(sys, "frozen", False)) and os.name == "nt"
    if "--kein-update" in argv:
        return "gestartet mit --kein-update"
    if "--selbsttest" in argv:
        return "Selbsttest"
    if os.environ.get(FRISCH):
        return "gerade aktualisiert"
    if not als_windows_exe:
        return "nicht als Windows-Programm gestartet"
    try:
        from dokusystem.kern import einstellungen
        if not einstellungen.lade()["auto_update"]:
            return "in den Einstellungen abgeschaltet"
    except Exception:  # noqa: BLE001
        pass
    return None


def aktualisieren(exe: Path, eigener_build: int, holen_json=hole_json, holen_datei=hole_datei,
                  fenster_machen=None, starten=starte) -> bool:
    """Ein Update-Durchgang. True = neue Version gestartet (aufrufendes Programm soll sich beenden).

    ``fenster_machen`` erzeugt das Fortschrittsfenster – erst, wenn es wirklich etwas zu laden gibt.
    """
    log(f"Build {eigener_build}: frage nach neuer Version.")
    angebot = angebot_aus_release(holen_json(API_NEUESTE), eigener_build)
    if angebot is None:
        log(f"Build {eigener_build} ist aktuell.")
        return False
    log(f"Neue Version gefunden: Build {angebot.build}. Lade …")
    fenster = fenster_machen() if fenster_machen else None
    try:
        return _laden_und_tauschen(exe, eigener_build, angebot, holen_datei, fenster, starten)
    finally:
        if fenster:
            fenster.schliessen()


def _laden_und_tauschen(exe: Path, eigener_build: int, angebot: Angebot, holen_datei, fenster, starten) -> bool:
    neu = exe.with_name(exe.name + ".neu")
    summe = exe.with_name(exe.name + ".sha256")
    text = f"Neue Version wird geladen (Build {eigener_build} → Build {angebot.build}) …"

    def fortschritt(gelesen: int, gesamt: int) -> bool:
        gesamt = gesamt or angebot.groesse
        return fenster.zeige(text, gelesen / gesamt if gesamt else None) if fenster else True

    try:
        if fenster:
            fenster.zeige(text, 0.0)
        holen_datei(angebot.pruefsumme_url, summe)
        erwartet = pruefsumme_lesen(summe.read_text(encoding="utf-8", errors="replace"))
        holen_datei(angebot.url, neu, fortschritt)
        if sha256(neu) != erwartet:
            raise UpdateFehler("Prüfsumme stimmt nicht – Download beschädigt.")
        log("Prüfsumme stimmt.")
        if fenster:
            fenster.zeige(f"Build {angebot.build} wird gestartet …", 1.0)
        austauschen(exe, neu)
    finally:
        for rest in (neu, summe):
            try:
                if rest.exists():
                    rest.unlink()
            except OSError:
                pass
    starten(exe)
    log(f"Ausgetauscht: Build {eigener_build} → Build {angebot.build}. Neustart.")
    return True


def beim_start(argv: list[str]) -> bool:
    """Wird von der Startdatei aufgerufen. True = neue Version läuft, dieses Programm soll enden."""
    from dokusystem._build import BUILD

    als_exe = getattr(sys, "frozen", False)
    if als_exe:
        aufraeumen(Path(sys.executable))
    grund = grund_ohne_update(argv)
    if grund or not BUILD:
        if als_exe and "--selbsttest" not in argv:
            log(f"Build {BUILD} gestartet, keine Nachfrage ({grund or 'Entwicklerversion'}).")
        return False
    try:
        from .fenster import Fortschrittsfenster
        return aktualisieren(Path(sys.executable), BUILD, fenster_machen=Fortschrittsfenster)
    except Exception as e:  # noqa: BLE001 – ohne Update weiterstarten, nie hängen bleiben
        log(f"Kein Update, Build {BUILD} läuft weiter: {e}")
        return False
