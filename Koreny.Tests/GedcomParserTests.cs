using Koreny.Models;
using Koreny.Services;

namespace Koreny.Tests;

public class GedcomParserTests
{
    private readonly GedcomParser _parser = new();

    [Fact]
    public void Parse_MinimalIndiAndFam_PopulatesBirthDeathAndFamily()
    {
        const string gedcom = """
            0 HEAD
            1 GEDC
            2 VERS 5.5.1
            0 @I1@ INDI
            1 NAME /Novák/ Jan
            1 SEX M
            1 BIRT
            2 DATE 10 JAN 1900
            2 PLAC Praha
            1 DEAT
            2 DATE 1 MAR 1980
            2 PLAC Brno
            1 NOTE Poznámka osoby
            0 @F1@ FAM
            1 HUSB @I1@
            1 WIFE @I2@
            1 CHIL @I3@
            1 MARR
            2 DATE 15 JUN 1925
            2 PLAC Karlín
            1 NOTE Poznámka rodiny
            0 TRLR
            """;

        var doc = _parser.Parse(gedcom);

        Assert.Single(doc.Individuals);
        Assert.Single(doc.Families);

        var i = doc.Individuals[0];
        Assert.Equal("I1", i.Id);
        Assert.Equal("M", i.Sex);
        Assert.NotNull(i.Birth);
        Assert.Equal("10 JAN 1900", i.Birth!.Date);
        Assert.Equal("Praha", i.Birth.Place);
        Assert.NotNull(i.Death);
        Assert.Equal("1 MAR 1980", i.Death!.Date);
        Assert.Equal("Brno", i.Death.Place);
        Assert.Single(i.Notes);
        Assert.Equal("Poznámka osoby", i.Notes[0]);

        var f = doc.Families[0];
        Assert.Equal("F1", f.Id);
        Assert.Equal("I1", f.HusbandId);
        Assert.Equal("I2", f.WifeId);
        Assert.Single(f.ChildrenIds);
        Assert.Equal("I3", f.ChildrenIds[0]);
        Assert.NotNull(f.Marriage);
        Assert.Equal("15 JUN 1925", f.Marriage!.Date);
        Assert.Equal("Karlín", f.Marriage.Place);
        Assert.Single(f.Notes);
        Assert.Equal("Poznámka rodiny", f.Notes[0]);
    }

    [Fact]
    public void Parse_NameSlashFormat_SplitsSurnameAndGiven()
    {
        const string gedcom = """
            0 @I9@ INDI
            1 NAME /Dvořák/ Anna Marie
            0 TRLR
            """;

        var doc = _parser.Parse(gedcom);
        var n = doc.Individuals[0].Name;

        Assert.NotNull(n);
        Assert.Equal("/Dvořák/ Anna Marie", n!.Raw);
        Assert.Equal("Dvořák", n.Surname);
        Assert.Equal("Anna Marie", n.GivenName);
    }

    [Fact]
    public void Parse_UnknownTags_DoesNotThrow()
    {
        const string gedcom = """
            0 @I1@ INDI
            1 NAME /X/ Y
            1 _MYHERITAGE_UNIQUE_ID 12345
            1 OBJE
            2 FORM jpeg
            2 FILE photo.jpg
            1 BIRT
            2 DATE 1 JAN 1900
            2 _FOO proprietary
            3 MORE junk
            1 SEX M
            0 @F1@ FAM
            1 _ANCESTRY proprietary
            1 HUSB @I1@
            0 TRLR
            """;

        var ex = Record.Exception(() => _parser.Parse(gedcom));
        Assert.Null(ex);

        var doc = _parser.Parse(gedcom);
        Assert.Equal("I1", doc.Individuals[0].Id);
        Assert.Equal("1 JAN 1900", doc.Individuals[0].Birth!.Date);
        Assert.Equal("I1", doc.Families[0].HusbandId);
    }
}
