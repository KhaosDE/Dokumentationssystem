"""Gemeinsame Hilfen für die Tests."""

import os
import tempfile

from dokusystem.kern import pfade


class Datenordner:
    """Lenkt den Datenordner für die Dauer eines Tests in einen leeren Ordner um."""

    def __enter__(self):
        self._tmp = tempfile.TemporaryDirectory()
        self._alt = os.environ.get(pfade.UMLENKUNG)
        os.environ[pfade.UMLENKUNG] = self._tmp.name
        return self._tmp.name

    def __exit__(self, *exc):
        if self._alt is None:
            os.environ.pop(pfade.UMLENKUNG, None)
        else:
            os.environ[pfade.UMLENKUNG] = self._alt
        self._tmp.cleanup()
