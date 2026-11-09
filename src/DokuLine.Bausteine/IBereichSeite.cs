namespace DokuLine;

// Eine Seite der Seitenleiste: Beim Wechsel dorthin liest sie neu; Kopfzeile und Aktionsleiste nennt sie selbst.
public interface IBereichSeite
{
    string Titel { get; }
    string KopfText { get; }
    IReadOnlyList<Button> Knoepfe { get; }
    void Zeigen(bool filterBehalten);
}
