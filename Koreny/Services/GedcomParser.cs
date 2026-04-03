using Koreny.Models;

namespace Koreny.Services;

public class GedcomParser
{
    private readonly Stack<ContextFrame> _stack = new();
    private GedcomDocument? _doc;

    public GedcomDocument Parse(string content)
    {
        _doc = new GedcomDocument();
        _stack.Clear();

        foreach (var rawLine in SplitLines(content))
        {
            if (!TryParseLine(rawLine, out var level, out var tag, out var value, out var xref))
            {
                continue;
            }

            PopUntil(level);
            ProcessLine(level, tag, value, xref);
        }

        return _doc;
    }

    private void PopUntil(int lineLevel)
    {
        while (_stack.Count > 0 && _stack.Peek().Level >= lineLevel)
        {
            _stack.Pop();
        }
    }

    private void ProcessLine(int level, string tag, string value, string? xref)
    {
        if (_doc is null)
        {
            return;
        }

        if (level == 0)
        {
            if (tag == "INDI" && xref is not null)
            {
                var indi = new GedcomIndividual { Id = xref };
                _doc.Individuals.Add(indi);
                _stack.Push(new ContextFrame(level, ContextKind.Individual, indi, null, null));
                return;
            }

            if (tag == "FAM" && xref is not null)
            {
                var fam = new GedcomFamily { Id = xref };
                _doc.Families.Add(fam);
                _stack.Push(new ContextFrame(level, ContextKind.Family, null, fam, null));
                return;
            }

            // HEAD, TRLR, OBJE, etc.: absorb subtree so nested lines do not attach to a prior record.
            _stack.Push(new ContextFrame(level, ContextKind.Skip, null, null, null));
            return;
        }

        if (_stack.Count == 0)
        {
            return;
        }

        var parent = _stack.Peek();

        switch (parent.Kind)
        {
            case ContextKind.Individual:
                HandleIndividualLine(parent.Individual!, level, tag, value);
                return;
            case ContextKind.Family:
                HandleFamilyLine(parent.Family!, level, tag, value);
                return;
            case ContextKind.Birth:
            case ContextKind.Death:
                HandleLifeEventLine(parent, level, tag, value);
                return;
            case ContextKind.Marriage:
                HandleMarriageLine(parent.Event!, level, tag, value);
                return;
            case ContextKind.Skip:
                // Nested line under ignored block; deeper levels stay under Skip until a sibling pops it.
                _stack.Push(new ContextFrame(level, ContextKind.Skip, null, null, null));
                return;
        }
    }

    private void HandleIndividualLine(GedcomIndividual indi, int level, string tag, string value)
    {
        if (tag == "NAME")
        {
            indi.Name = GedcomNameParser.Parse(value);
            return;
        }

        if (tag == "SEX")
        {
            indi.Sex = value.Trim();
            return;
        }

        if (tag == "BIRT")
        {
            var e = new GedcomEvent();
            indi.Birth = e;
            _stack.Push(new ContextFrame(level, ContextKind.Birth, indi, null, e));
            return;
        }

        if (tag == "DEAT")
        {
            var e = new GedcomEvent();
            indi.Death = e;
            _stack.Push(new ContextFrame(level, ContextKind.Death, indi, null, e));
            return;
        }

        if (tag == "NOTE")
        {
            indi.Notes.Add(value);
            return;
        }

        _stack.Push(new ContextFrame(level, ContextKind.Skip, null, null, null));
    }

    private void HandleLifeEventLine(ContextFrame parent, int level, string tag, string value)
    {
        var ev = parent.Event!;
        if (tag == "DATE")
        {
            ev.Date = value.Trim();
            return;
        }

        if (tag == "PLAC")
        {
            ev.Place = value.Trim();
            return;
        }

        _stack.Push(new ContextFrame(level, ContextKind.Skip, null, null, null));
    }

    private void HandleFamilyLine(GedcomFamily fam, int level, string tag, string value)
    {
        if (tag == "HUSB")
        {
            fam.HusbandId = StripXref(value);
            return;
        }

        if (tag == "WIFE")
        {
            fam.WifeId = StripXref(value);
            return;
        }

        if (tag == "CHIL")
        {
            var id = StripXref(value);
            if (id is not null)
            {
                fam.ChildrenIds.Add(id);
            }

            return;
        }

        if (tag == "MARR")
        {
            var e = new GedcomEvent();
            fam.Marriage = e;
            _stack.Push(new ContextFrame(level, ContextKind.Marriage, null, fam, e));
            return;
        }

        if (tag == "NOTE")
        {
            fam.Notes.Add(value);
            return;
        }

        _stack.Push(new ContextFrame(level, ContextKind.Skip, null, null, null));
    }

    private void HandleMarriageLine(GedcomEvent ev, int level, string tag, string value)
    {
        if (tag == "DATE")
        {
            ev.Date = value.Trim();
            return;
        }

        if (tag == "PLAC")
        {
            ev.Place = value.Trim();
            return;
        }

        // Unknown subtags under MARR (e.g. TYPE, OBJE): push Skip so nested levels stay out of sibling records.
        _stack.Push(new ContextFrame(level, ContextKind.Skip, null, null, null));
    }

    private static IEnumerable<string> SplitLines(string content)
    {
        using var reader = new StringReader(content);
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            yield return line;
        }
    }

    private static bool TryParseLine(string line, out int level, out string tag, out string value, out string? xref)
    {
        level = 0;
        tag = string.Empty;
        value = string.Empty;
        xref = null;

        line = line.Trim();
        if (line.Length == 0)
        {
            return false;
        }

        var i = 0;
        while (i < line.Length && char.IsDigit(line[i]))
        {
            i++;
        }

        if (i == 0)
        {
            return false;
        }

        level = int.Parse(line.AsSpan(0, i), System.Globalization.NumberStyles.Integer, null);

        if (i >= line.Length || (line[i] != ' ' && line[i] != '\t'))
        {
            return false;
        }

        while (i < line.Length && (line[i] == ' ' || line[i] == '\t'))
        {
            i++;
        }

        if (i >= line.Length)
        {
            return false;
        }

        var rest = line.AsSpan(i);
        if (rest[0] == '@')
        {
            var end = -1;
            for (var k = 1; k < rest.Length; k++)
            {
                if (rest[k] == '@')
                {
                    end = k;
                    break;
                }
            }

            if (end < 0)
            {
                return false;
            }

            xref = rest[1..end].ToString();
            rest = rest[(end + 1)..].TrimStart();
            if (rest.Length == 0)
            {
                return false;
            }
        }

        var restStr = rest.ToString();
        var split = restStr.AsSpan();
        var sep = -1;
        for (var j = 0; j < split.Length; j++)
        {
            if (split[j] == ' ' || split[j] == '\t')
            {
                sep = j;
                break;
            }
        }

        if (sep < 0)
        {
            tag = restStr;
            value = string.Empty;
            return true;
        }

        tag = restStr[..sep];
        value = restStr[sep..].TrimStart();
        return true;
    }

    private static string? StripXref(string raw)
    {
        var t = raw.Trim();
        if (t.Length >= 2 && t[0] == '@' && t[^1] == '@')
        {
            return t[1..^1];
        }

        return t.Length > 0 ? t : null;
    }

    private enum ContextKind
    {
        Skip,
        Individual,
        Family,
        Birth,
        Death,
        Marriage,
    }

    private readonly struct ContextFrame
    {
        public ContextFrame(int level, ContextKind kind, GedcomIndividual? individual, GedcomFamily? family, GedcomEvent? ev)
        {
            Level = level;
            Kind = kind;
            Individual = individual;
            Family = family;
            Event = ev;
        }

        public int Level { get; }
        public ContextKind Kind { get; }
        public GedcomIndividual? Individual { get; }
        public GedcomFamily? Family { get; }
        public GedcomEvent? Event { get; }
    }
}

internal static class GedcomNameParser
{
    internal static GedcomName Parse(string raw)
    {
        var name = new GedcomName { Raw = raw };
        var s = raw.Trim();
        var first = s.IndexOf('/');
        if (first < 0)
        {
            name.GivenName = s.Length > 0 ? s : null;
            return name;
        }

        var second = s.IndexOf('/', first + 1);
        if (second < 0)
        {
            name.GivenName = s.Length > 0 ? s : null;
            return name;
        }

        // Slash-delimited surname is the standard GEDCOM convention; avoids guessing on proprietary NAME extensions.
        name.Surname = s.Substring(first + 1, second - first - 1).Trim();
        var given = (s[..first] + " " + s[(second + 1)..]).Trim();
        name.GivenName = given.Length > 0 ? given : null;
        return name;
    }
}
