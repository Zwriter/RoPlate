namespace Dfa.Tests.Core;

using Dfa.Core.Model;
using Shouldly;

public class SymbolClassTests
{
    // Σ = {L, C, _}, with _ holding the space character (the RoPlate alphabet)
    private static Dfa PlateAlphabet()
    {
        var dfa = new Dfa();
        dfa.AddSymbolClass(SymbolClass.FromRanges("L", "A-Z"));
        dfa.AddSymbolClass(SymbolClass.FromRanges("C", "0-9"));
        dfa.AddSymbolClass(new SymbolClass("_", " "));
        return dfa;
    }

    [Theory]
    [InlineData("A-Z", "ABCDEFGHIJKLMNOPQRSTUVWXYZ")]
    [InlineData("0-9", "0123456789")]
    [InlineData("a-cx", "abcx")]
    [InlineData("c-a", "abc")]
    [InlineData("-a-b-", "-ab")]
    [InlineData("aab", "ab")]
    public void FromRanges_ExpandsRangesAndKeepsLiterals(string ranges, string expected)
    {
        SymbolClass.FromRanges("X", ranges).Characters.ShouldBe(expected);
    }

    [Theory]
    [InlineData('7', "C")]
    [InlineData('K', "L")]
    [InlineData(' ', "_")]
    [InlineData('_', null)]
    [InlineData('k', null)]
    [InlineData('#', null)]
    public void SymbolOf_MapsCharactersToClasses(char c, string? expected)
    {
        PlateAlphabet().SymbolOf(c).ShouldBe(expected);
    }

    [Fact]
    public void SymbolOf_SimpleSymbol_MapsToItself()
    {
        var dfa = PlateAlphabet();
        dfa.AddSymbol("-");

        dfa.SymbolOf('-').ShouldBe("-");
    }

    [Fact]
    public void AddSymbolClass_AddsNameToAlphabet()
    {
        PlateAlphabet().Alphabet.ShouldBe(["L", "C", "_"]);
    }

    [Fact]
    public void AddSymbolClass_OverlappingClass_IsRefused()
    {
        var dfa = PlateAlphabet();

        dfa.AddSymbolClass(new SymbolClass("V", "AEIOU")).Error.ShouldBe("Character 'A' already belongs to class 'L'.");
        dfa.Alphabet.ShouldNotContain("V");
    }

    [Fact]
    public void AddSymbolClass_CharacterIsSimpleSymbol_IsRefused()
    {
        var dfa = new Dfa();
        dfa.AddSymbol("b");

        dfa.AddSymbolClass(SymbolClass.FromRanges("ab", "a-c")).Error.ShouldBe("Symbol 'b' is already in the alphabet.");
    }

    [Fact]
    public void AddSymbolClass_DuplicateName_IsRefused()
    {
        PlateAlphabet().AddSymbolClass(new SymbolClass("C", "#")).Error.ShouldBe("Symbol 'C' is already in the alphabet.");
    }

    [Fact]
    public void AddSymbol_CharacterOfAClass_IsRefused()
    {
        PlateAlphabet().AddSymbol("7").Error.ShouldBe("Character '7' already belongs to class 'C'.");
    }

    [Fact]
    public void RemoveSymbol_Class_RemovesTheClass()
    {
        var dfa = PlateAlphabet();

        dfa.RemoveSymbol("C").Ok.ShouldBeTrue();

        dfa.SymbolClasses.Select(k => k.Name).ShouldBe(["L", "_"]);
        dfa.SymbolOf('7').ShouldBeNull();
    }
}
