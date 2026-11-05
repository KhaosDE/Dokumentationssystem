"""Selbst-Update der Windows-Programmdatei (Vorbild: KhaosDE/N3XTBYT3, Paket n3xtbyt3_update).

Nur dieses Paket darf ins Netz – und nur zu den Releases dieses Repositorys auf GitHub:

1. Beim Start der .exe fragt es
   https://api.github.com/repos/KhaosDE/Dokumentationssystem/releases/latest,
   ob es einen neueren Build gibt (Zeitlimit wenige Sekunden; ohne Netz geht es einfach weiter).
2. Wenn ja: Meldung „Neue Version wird geladen“, dann lädt es ``Dokumentationssystem.exe`` und
   die Prüfsumme ``Dokumentationssystem.exe.sha256`` und vergleicht beide.
3. Es benennt die laufende .exe in ``….exe.alt`` um, legt die neue an ihren Platz, startet sie
   und beendet sich. Die neue Version löscht beim Start die alte Datei.

Jeder Schritt und jeder Fehler steht in ``update.log`` im Datenordner. Bei einem Fehler startet
das Programm ganz normal in der alten Version.
Abschalten: Häkchen „Beim Start nach Updates suchen“ im Programm (``"auto_update": false`` in
``einstellungen.json``) oder Startparameter ``--kein-update``.

Das Programm selbst (Paket ``dokusystem``) geht nie ins Netz; ein Test prüft das.
"""

from .aktualisierung import beim_start

__all__ = ["beim_start"]
