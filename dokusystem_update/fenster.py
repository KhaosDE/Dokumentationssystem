"""Kleines Fenster mit Fortschrittsbalken, solange die neue Version geladen wird."""

from __future__ import annotations

import sys

from PySide6.QtCore import Qt
from PySide6.QtWidgets import QApplication, QProgressDialog

SCHRITTE = 1000


class Fortschrittsfenster:
    def __init__(self) -> None:
        self.app = QApplication.instance() or QApplication(sys.argv)
        self.dialog = QProgressDialog("Neue Version wird geladen …", "Abbrechen und alte Version starten",
                                      0, SCHRITTE)
        self.dialog.setWindowTitle("Dokumentationssystem – Update")
        self.dialog.setWindowModality(Qt.WindowModality.ApplicationModal)
        self.dialog.setMinimumDuration(0)
        self.dialog.setMinimumWidth(460)
        self.dialog.setAutoClose(False)
        self.dialog.setAutoReset(False)
        self.dialog.show()
        self.app.processEvents()

    def zeige(self, text: str, anteil: float | None) -> bool:
        """Text und Balken setzen. False, wenn abgebrochen oder das Fenster geschlossen wurde."""
        self.dialog.setLabelText(text)
        if anteil is None:
            self.dialog.setRange(0, 0)  # unbekannte Größe: laufender Balken
        else:
            self.dialog.setRange(0, SCHRITTE)
            self.dialog.setValue(int(max(0.0, min(1.0, anteil)) * SCHRITTE))
        self.app.processEvents()
        return not self.dialog.wasCanceled()

    def schliessen(self) -> None:
        self.dialog.close()
        self.dialog.deleteLater()
        self.app.processEvents()
