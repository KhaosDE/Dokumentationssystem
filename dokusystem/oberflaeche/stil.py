"""Farben und Stylesheet an einer Stelle."""

DEMO_HINWEIS = "Demo — ausgedachte Daten"

STYLESHEET = """
QWidget { font-size: 11pt; }
#demoHinweis {
    background: #f9a825; color: #1a1a1a; font-weight: bold; font-size: 12pt;
    padding: 6px 12px;
}
#navigation { background: #eef1f5; border: none; border-right: 1px solid #d0d5dc; }
#navigation QListWidget { background: transparent; border: none; font-size: 12pt; outline: none; }
#navigation QListWidget::item { padding: 10px 14px; }
#navigation QListWidget::item:selected { background: #2f5d8a; color: white; }
#version, #autoUpdate { color: #555; font-size: 9pt; }
#seitenTitel { font-size: 18pt; font-weight: bold; }
#unterTitel { font-size: 13pt; color: #444; }
#klein { font-size: 9pt; color: #666; }
#fehler { color: #b71c1c; background: #ffebee; border: 1px solid #ef9a9a; padding: 6px; }
#kacheln { background: white; border: 1px solid #d0d5dc; }
#kacheln::item { padding: 6px; }
#kacheln::item:hover { background: #e3ecf5; }
#kacheln::item:selected { background: #c9dbee; color: black; }
"""
