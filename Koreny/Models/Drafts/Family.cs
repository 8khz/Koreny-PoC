namespace Koreny.Models.Drafts;

/// <summary>In-memory family while composing a tree (not yet a <see cref="GedcomFamily"/>).</summary>
public class Family
{
    public string Id { get; set; } = string.Empty;
    public string? HusbandId { get; set; }
    public string? WifeId { get; set; }
    public List<string> ChildrenIds { get; } = new();
    public int? MarriageYear { get; set; }
    public string? MarriagePlace { get; set; }
}
