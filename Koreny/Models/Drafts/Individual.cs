using Koreny.Models;

namespace Koreny.Models.Drafts;

/// <summary>In-memory person while composing a tree (not yet a <see cref="GedcomIndividual"/>).</summary>
public class Individual
{
    public string Id { get; set; } = string.Empty;
    public string? GivenName { get; set; }
    public string? Surname { get; set; }
    public SexType Sex { get; set; }
    public int? BirthYear { get; set; }
    public string? BirthPlace { get; set; }
    public int? DeathYear { get; set; }
    public string? DeathPlace { get; set; }
    public string? Note { get; set; }

    public string Label =>
        string.IsNullOrWhiteSpace($"{GivenName} {Surname}".Trim())
            ? Id
            : $"{Id}: {GivenName} {Surname}".Trim();
}
