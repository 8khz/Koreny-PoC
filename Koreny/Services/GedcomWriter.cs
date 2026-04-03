using System.Globalization;
using System.Text;
using Koreny.Models;

namespace Koreny.Services;

public static class GedcomWriter
{
    /// <summary>Serialises a document to GEDCOM 5.5.1 lines (UTF-8 string; caller handles file encoding).</summary>
    public static string Write(GedcomDocument doc)
    {
        var sb = new StringBuilder();
        WriteHead(sb);

        foreach (var ind in doc.Individuals.OrderBy(i => i.Id, StringComparer.Ordinal))
        {
            WriteIndividual(sb, ind);
        }

        foreach (var fam in doc.Families.OrderBy(f => f.Id, StringComparer.Ordinal))
        {
            WriteFamily(sb, fam);
        }

        WriteLine(sb, 0, "TRLR");
        return sb.ToString();
    }

    private static void WriteHead(StringBuilder sb)
    {
        WriteLine(sb, 0, "HEAD");
        WriteLine(sb, 1, "SOUR", "Koreny");
        WriteLine(sb, 1, "GEDC");
        WriteLine(sb, 2, "VERS", "5.5.1");
        // FORM Lineage-Linked is required when INDI/FAM use lineage-linked structures.
        WriteLine(sb, 2, "FORM", "Lineage-Linked");
        WriteLine(sb, 1, "CHAR", "UTF-8");
    }

    private static void WriteIndividual(StringBuilder sb, GedcomIndividual ind)
    {
        WriteLine(sb, 0, $"@{ind.Id}@", "INDI");
        if (ind.Name is not null)
        {
            WriteLine(sb, 1, "NAME", ind.Name.Raw.Trim());
        }

        if (!string.IsNullOrEmpty(ind.Sex))
        {
            WriteLine(sb, 1, "SEX", ind.Sex);
        }

        WriteEvent(sb, "BIRT", ind.Birth);
        WriteEvent(sb, "DEAT", ind.Death);

        foreach (var note in ind.Notes)
        {
            if (note.Length > 0)
            {
                WriteLine(sb, 1, "NOTE", note);
            }
        }
    }

    private static void WriteFamily(StringBuilder sb, GedcomFamily fam)
    {
        WriteLine(sb, 0, $"@{fam.Id}@", "FAM");
        if (!string.IsNullOrEmpty(fam.HusbandId))
        {
            WriteLine(sb, 1, "HUSB", $"@{fam.HusbandId}@");
        }

        if (!string.IsNullOrEmpty(fam.WifeId))
        {
            WriteLine(sb, 1, "WIFE", $"@{fam.WifeId}@");
        }

        foreach (var ch in fam.ChildrenIds)
        {
            if (!string.IsNullOrEmpty(ch))
            {
                WriteLine(sb, 1, "CHIL", $"@{ch}@");
            }
        }

        if (fam.Marriage is not null && (fam.Marriage.Date is not null || fam.Marriage.Place is not null))
        {
            WriteLine(sb, 1, "MARR");
            if (fam.Marriage.Date is not null)
            {
                WriteLine(sb, 2, "DATE", fam.Marriage.Date);
            }

            if (fam.Marriage.Place is not null)
            {
                WriteLine(sb, 2, "PLAC", fam.Marriage.Place);
            }
        }
    }

    private static void WriteEvent(StringBuilder sb, string tag, GedcomEvent? ev)
    {
        if (ev is null)
        {
            return;
        }

        if (ev.Date is null && ev.Place is null)
        {
            return;
        }

        WriteLine(sb, 1, tag);
        if (ev.Date is not null)
        {
            WriteLine(sb, 2, "DATE", ev.Date);
        }

        if (ev.Place is not null)
        {
            WriteLine(sb, 2, "PLAC", ev.Place);
        }
    }

    private static void WriteLine(StringBuilder sb, int level, string rest)
    {
        sb.Append(level.ToString(CultureInfo.InvariantCulture));
        sb.Append(' ');
        sb.Append(rest);
        sb.Append('\n');
    }

    private static void WriteLine(StringBuilder sb, int level, string tag, string value)
    {
        sb.Append(level.ToString(CultureInfo.InvariantCulture));
        sb.Append(' ');
        sb.Append(tag);
        if (value.Length > 0)
        {
            sb.Append(' ');
            sb.Append(value);
        }

        sb.Append('\n');
    }
}
