using Koreny.Models;

namespace Koreny.Services;

/// <summary>Singleton document for the single-page editor; not persisted.</summary>
public class AppState
{
    public GedcomDocument? Document { get; set; }
}
