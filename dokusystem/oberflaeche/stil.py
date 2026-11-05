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
"""
