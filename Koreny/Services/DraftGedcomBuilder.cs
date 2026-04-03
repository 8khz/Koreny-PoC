using Koreny.Models;
using Koreny.Models.Drafts;

namespace Koreny.Services;

public static class DraftGedcomBuilder
{
    public static GedcomDocument ToGedcomDocument(IReadOnlyList<Individual> people, IReadOnlyList<Family> families)
    {
        var doc = new GedcomDocument();
        foreach (var p in people)
        {
            doc.Individuals.Add(MapIndividual(p));
        }

        foreach (var f in families)
        {
            doc.Families.Add(MapFamily(f));
        }

        return doc;
    }

    private static GedcomIndividual MapIndividual(Individual p)
    {
        var ind = new GedcomIndividual { Id = p.Id };
        var sur = p.Surname?.Trim() ?? string.Empty;
        var giv = p.GivenName?.Trim() ?? string.Empty;
        ind.Name = new GedcomName
        {
            Surname = string.IsNullOrEmpty(sur) ? null : sur,
            GivenName = string.IsNullOrEmpty(giv) ? null : giv,
            Raw = $"/{sur}/ {giv}".Trim(),
        };

        ind.Sex = p.Sex switch
        {
            SexType.Male => "M",
            SexType.Female => "F",
            SexType.Unknown => null,
            _ => null,
        };

        if (p.BirthYear is not null || !string.IsNullOrWhiteSpace(p.BirthPlace))
        {
            ind.Birth = new GedcomEvent
            {
                Date = p.BirthYear?.ToString(),
                Place = string.IsNullOrWhiteSpace(p.BirthPlace) ? null : p.BirthPlace!.Trim(),
            };
        }

        if (p.DeathYear is not null || !string.IsNullOrWhiteSpace(p.DeathPlace))
        {
            ind.Death = new GedcomEvent
            {
                Date = p.DeathYear?.ToString(),
                Place = string.IsNullOrWhiteSpace(p.DeathPlace) ? null : p.DeathPlace!.Trim(),
            };
        }

        if (!string.IsNullOrWhiteSpace(p.Note))
        {
            ind.Notes.Add(p.Note.Trim());
        }

        return ind;
    }

    private static GedcomFamily MapFamily(Family f)
    {
        var fam = new GedcomFamily { Id = f.Id };
        fam.HusbandId = f.HusbandId;
        fam.WifeId = f.WifeId;
        foreach (var c in f.ChildrenIds)
        {
            fam.ChildrenIds.Add(c);
        }

        if (f.MarriageYear is not null || !string.IsNullOrWhiteSpace(f.MarriagePlace))
        {
            fam.Marriage = new GedcomEvent
            {
                Date = f.MarriageYear?.ToString(),
                Place = string.IsNullOrWhiteSpace(f.MarriagePlace) ? null : f.MarriagePlace!.Trim(),
            };
        }

        return fam;
    }
}
