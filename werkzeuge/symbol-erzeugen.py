"""Erzeugt das Programmsymbol src/DokuLine/Bilder/DokuLine.ico aus den beiden SVG-Vorlagen.

Bauplan Schritt 26n: 16, 20, 24 und 32 px aus der kleinen Vorlage (programmsymbol-klein.svg), 40, 48, 64
und 256 px aus der großen (programmsymbol.svg). Ein neues Symbol braucht nur neue SVG-Dateien und einen
neuen Aufruf; die fertige .ico liegt im Repository, der Windows-Build braucht dieses Skript nicht.

Nur die Python-Standardbibliothek (keine weiteren Pakete, keine Lizenzfrage). Das Skript zeichnet die
SVG-Teile, die die Vorlagen benutzen: Rechtecke mit runden Ecken (rect mit rx/ry), Füllung und Rand
(fill, stroke, stroke-width, Deckkraft), radiale Verläufe (radialGradient mit stop-color und
stop-opacity). Andere SVG-Elemente meldet es als Fehler, statt sie stillschweigend wegzulassen.

Aufruf (im Wurzelordner des Repositorys):
    python werkzeuge/symbol-erzeugen.py
    python werkzeuge/symbol-erzeugen.py --vorschau <ordner>   # zusätzlich PNG je Größe zum Ansehen
"""
import math
import os
import struct
import sys
import xml.etree.ElementTree as ET
import zlib

WURZEL = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
BILDER = os.path.join(WURZEL, "src", "DokuLine", "Bilder")
GROSS = os.path.join(BILDER, "programmsymbol.svg")
KLEIN = os.path.join(BILDER, "programmsymbol-klein.svg")
ZIEL = os.path.join(BILDER, "DokuLine.ico")
GROESSEN = [(16, KLEIN), (20, KLEIN), (24, KLEIN), (32, KLEIN), (40, GROSS), (48, GROSS), (64, GROSS), (256, GROSS)]
NS = "{http://www.w3.org/2000/svg}"


def farbe(text):
    text = text.strip()
    if not (text.startswith("#") and len(text) in (4, 7)):
        raise ValueError(f"Farbe nicht unterstützt: {text}")
    if len(text) == 4:
        text = "#" + "".join(z * 2 for z in text[1:])
    return tuple(int(text[i:i + 2], 16) / 255 for i in (1, 3, 5))


def zahl(knoten, name, vorgabe=0.0):
    wert = knoten.get(name)
    return vorgabe if wert is None else float(wert.rstrip("px"))


class Verlauf:
    """radialGradient in Einheiten des Elements (objectBoundingBox, wie in SVG vorgegeben)."""

    def __init__(self, knoten):
        if knoten.get("gradientUnits", "objectBoundingBox") != "objectBoundingBox" or knoten.get("gradientTransform"):
            raise ValueError("radialGradient: nur objectBoundingBox ohne gradientTransform")
        self.cx = zahl(knoten, "cx", 0.5)
        self.cy = zahl(knoten, "cy", 0.5)
        self.r = zahl(knoten, "r", 0.5)
        self.stops = []
        for s in knoten:
            if s.tag != NS + "stop":
                raise ValueError(f"im Verlauf nicht unterstützt: {s.tag}")
            versatz = s.get("offset", "0")
            versatz = float(versatz[:-1]) / 100 if versatz.endswith("%") else float(versatz)
            self.stops.append((versatz, farbe(s.get("stop-color", "#000000")), float(s.get("stop-opacity", "1"))))
        if not self.stops:
            raise ValueError("radialGradient ohne stop")

    def an(self, x, y, kasten):
        bx, by, bw, bh = kasten
        dx = (x - bx) / bw - self.cx
        dy = (y - by) / bh - self.cy
        t = min(1.0, math.hypot(dx, dy) / self.r) if self.r > 0 else 1.0
        if t <= self.stops[0][0]:
            _, f, d = self.stops[0]
            return f, d
        for (o1, f1, d1), (o2, f2, d2) in zip(self.stops, self.stops[1:]):
            if t <= o2:
                a = 0.0 if o2 == o1 else (t - o1) / (o2 - o1)
                return tuple(f1[i] + (f2[i] - f1[i]) * a for i in range(3)), d1 + (d2 - d1) * a
        _, f, d = self.stops[-1]
        return f, d


def im_rundrechteck(x, y, rx0, ry0, rx1, ry1, r):
    """Liegt (x, y) im Rechteck (rx0, ry0)–(rx1, ry1) mit Eckradius r?"""
    if x < rx0 or x > rx1 or y < ry0 or y > ry1:
        return False
    if r <= 0:
        return True
    cx = min(max(x, rx0 + r), rx1 - r)
    cy = min(max(y, ry0 + r), ry1 - r)
    return (x - cx) ** 2 + (y - cy) ** 2 <= r * r


class Rechteck:
    def __init__(self, knoten, verlaeufe):
        for name in ("transform", "ry"):
            if knoten.get(name) and not (name == "ry" and knoten.get("ry") == knoten.get("rx")):
                raise ValueError(f"rect: {name} nicht unterstützt")
        self.x, self.y = zahl(knoten, "x"), zahl(knoten, "y")
        self.b, self.h = zahl(knoten, "width"), zahl(knoten, "height")
        self.r = min(zahl(knoten, "rx"), self.b / 2, self.h / 2)
        deckkraft = float(knoten.get("opacity", "1"))
        self.fuellung = self._farbe(knoten.get("fill", "#000000"), verlaeufe, deckkraft * float(knoten.get("fill-opacity", "1")))
        self.rand = self._farbe(knoten.get("stroke", "none"), verlaeufe, deckkraft * float(knoten.get("stroke-opacity", "1")))
        self.randbreite = zahl(knoten, "stroke-width", 1.0)

    @staticmethod
    def _farbe(text, verlaeufe, deckkraft):
        if text == "none":
            return None
        if text.startswith("url(#"):
            name = text[5:-1]
            if name not in verlaeufe:
                raise ValueError(f"Verlauf {name} fehlt")
            return ("verlauf", verlaeufe[name], deckkraft)
        return ("farbe", farbe(text), deckkraft)

    def kasten(self):
        return self.x, self.y, self.b, self.h

    def malen(self, x, y, pixel):
        """pixel: [r, g, b, a] vormultipliert; malt Füllung, dann Rand darüber."""
        if self.fuellung and im_rundrechteck(x, y, self.x, self.y, self.x + self.b, self.y + self.h, self.r):
            darueber(pixel, self._wert(self.fuellung, x, y))
        if self.rand:
            w = self.randbreite / 2
            aussen = im_rundrechteck(x, y, self.x - w, self.y - w, self.x + self.b + w, self.y + self.h + w, self.r + w)
            innen = im_rundrechteck(x, y, self.x + w, self.y + w, self.x + self.b - w, self.y + self.h - w, max(0.0, self.r - w))
            if aussen and not innen:
                darueber(pixel, self._wert(self.rand, x, y))

    def _wert(self, malung, x, y):
        art, wert, deckkraft = malung
        if art == "farbe":
            return wert, deckkraft
        f, d = wert.an(x, y, self.kasten())
        return f, d * deckkraft


def darueber(pixel, quelle):
    (r, g, b), a = quelle
    rest = 1 - a
    pixel[0] = r * a + pixel[0] * rest
    pixel[1] = g * a + pixel[1] * rest
    pixel[2] = b * a + pixel[2] * rest
    pixel[3] = a + pixel[3] * rest


def lesen(pfad):
    wurzel = ET.parse(pfad).getroot()
    if wurzel.tag != NS + "svg":
        raise ValueError(f"{pfad}: keine SVG-Datei")
    vx, vy, vb, vh = (float(z) for z in wurzel.get("viewBox").replace(",", " ").split())
    verlaeufe, teile = {}, []
    for knoten in wurzel:
        if knoten.tag == NS + "defs":
            for v in knoten:
                if v.tag != NS + "radialGradient":
                    raise ValueError(f"{pfad}: in defs nur radialGradient, nicht {v.tag}")
                verlaeufe[v.get("id")] = Verlauf(v)
        elif knoten.tag == NS + "rect":
            teile.append(knoten)
        else:
            raise ValueError(f"{pfad}: Element {knoten.tag} nicht unterstützt")
    return (vx, vy, vb, vh), [Rechteck(t, verlaeufe) for t in teile]


def zeichnen(pfad, groesse):
    """RGBA-Bytes (nicht vormultipliert), Zeilen von oben; Kantenglättung durch Unterabtastung."""
    (vx, vy, vb, vh), teile = lesen(pfad)
    n = 8 if groesse <= 64 else 3
    sx, sy = vb / groesse, vh / groesse
    ausgabe = bytearray()
    for py in range(groesse):
        for px in range(groesse):
            summe = [0.0, 0.0, 0.0, 0.0]
            for j in range(n):
                y = vy + (py + (j + 0.5) / n) * sy
                for i in range(n):
                    x = vx + (px + (i + 0.5) / n) * sx
                    pixel = [0.0, 0.0, 0.0, 0.0]
                    for t in teile:
                        t.malen(x, y, pixel)
                    for k in range(4):
                        summe[k] += pixel[k]
            a = summe[3] / (n * n)
            if a <= 0:
                ausgabe += b"\0\0\0\0"
                continue
            rgb = [min(255, round(summe[k] / (n * n) / a * 255)) for k in range(3)]
            ausgabe += bytes(rgb + [min(255, round(a * 255))])
    return bytes(ausgabe)


def png(rgba, groesse):
    def block(art, daten):
        return struct.pack(">I", len(daten)) + art + daten + struct.pack(">I", zlib.crc32(art + daten) & 0xFFFFFFFF)
    zeilen = b"".join(b"\0" + rgba[y * groesse * 4:(y + 1) * groesse * 4] for y in range(groesse))
    return (b"\x89PNG\r\n\x1a\n" + block(b"IHDR", struct.pack(">IIBBBBB", groesse, groesse, 8, 6, 0, 0, 0))
            + block(b"IDAT", zlib.compress(zeilen, 9)) + block(b"IEND", b""))


def bmp(rgba, groesse):
    """Klassisches Symbolbild (32 Bit mit Alpha, darunter die Maske) für die kleinen Größen."""
    kopf = struct.pack("<IiiHHIIiiII", 40, groesse, groesse * 2, 1, 32, 0, 0, 0, 0, 0, 0)
    pixel = bytearray()
    for y in reversed(range(groesse)):   # Zeilen von unten nach oben, BGRA
        for x in range(groesse):
            r, g, b, a = rgba[(y * groesse + x) * 4:(y * groesse + x) * 4 + 4]
            pixel += bytes((b, g, r, a))
    zeile = ((groesse + 31) // 32) * 4
    maske = bytearray()
    for y in reversed(range(groesse)):
        bits = bytearray(zeile)
        for x in range(groesse):
            if rgba[(y * groesse + x) * 4 + 3] == 0:
                bits[x // 8] |= 0x80 >> (x % 8)
        maske += bits
    return kopf + bytes(pixel) + bytes(maske)


def ico(bilder):
    kopf = struct.pack("<HHH", 0, 1, len(bilder))
    versatz = 6 + 16 * len(bilder)
    eintraege, daten = b"", b""
    for groesse, inhalt in bilder:
        seite = 0 if groesse >= 256 else groesse
        eintraege += struct.pack("<BBBBHHII", seite, seite, 0, 0, 1, 32, len(inhalt), versatz + len(daten))
        daten += inhalt
    return kopf + eintraege + daten


def main():
    vorschau = None
    if len(sys.argv) == 3 and sys.argv[1] == "--vorschau":
        vorschau = sys.argv[2]
        os.makedirs(vorschau, exist_ok=True)
    elif len(sys.argv) != 1:
        print(__doc__)
        return 2
    bilder = []
    for groesse, vorlage in GROESSEN:
        rgba = zeichnen(vorlage, groesse)
        bilder.append((groesse, png(rgba, groesse) if groesse >= 256 else bmp(rgba, groesse)))
        if vorschau:
            with open(os.path.join(vorschau, f"symbol-{groesse}.png"), "wb") as d:
                d.write(png(rgba, groesse))
        print(f"{groesse:>3} px aus {os.path.basename(vorlage)}")
    with open(ZIEL, "wb") as d:
        d.write(ico(bilder))
    print(f"geschrieben: {os.path.relpath(ZIEL, WURZEL)}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
