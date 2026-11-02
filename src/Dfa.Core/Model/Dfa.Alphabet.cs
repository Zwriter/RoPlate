namespace Dfa.Core.Model;

public sealed partial class Dfa
{
    public string? SymbolOf(char c)
    {
        var symbolClass = ClassContaining(c);
        if (symbolClass is not null)
        {
            return symbolClass.Name;
        }

        var symbol = c.ToString();
        return _alphabet.Contains(symbol) && !IsClassName(symbol) ? symbol : null;
    }

    public OpResult AddSymbol(string symbol)
    {
        if (symbol.Length != 1)
        {
            return OpResult.Fail("A simple symbol must have exactly one character. Use a symbol class for groups.");
        }

        if (_alphabet.Contains(symbol))
        {
            return SymbolAlreadyInAlphabet(symbol);
        }

        var owner = ClassContaining(symbol[0]);
        if (owner is not null)
        {
            return CharacterInClass(symbol[0], owner);
        }

        _alphabet.Add(symbol);
        return OpResult.Success;
    }

    public OpResult AddSymbolClass(SymbolClass symbolClass)
    {
        if (_alphabet.Contains(symbolClass.Name))
        {
            return SymbolAlreadyInAlphabet(symbolClass.Name);
        }

        foreach (var c in symbolClass.Characters)
        {
            var owner = ClassContaining(c);
            if (owner is not null)
            {
                return CharacterInClass(c, owner);
            }

            if (_alphabet.Contains(c.ToString()))
            {
                return SymbolAlreadyInAlphabet(c.ToString());
            }
        }

        _alphabet.Add(symbolClass.Name);
        _symbolClasses.Add(symbolClass);
        return OpResult.Success;
    }

    public OpResult RemoveSymbol(string symbol)
    {
        if (!_alphabet.Contains(symbol))
        {
            return SymbolNotInAlphabet(symbol);
        }

        var uses = _transitions.Keys.Count(k => k.Symbol == symbol);
        if (uses > 0)
        {
            return OpResult.Fail($"Symbol '{symbol}' is used in {uses} transitions. Remove them first.");
        }

        _alphabet.Remove(symbol);
        _symbolClasses.RemoveAll(c => c.Name == symbol);
        return OpResult.Success;
    }

    private SymbolClass? ClassContaining(char c) => _symbolClasses.FirstOrDefault(k => k.Contains(c));

    private bool IsClassName(string symbol) => _symbolClasses.Any(k => k.Name == symbol);

    private static OpResult SymbolNotInAlphabet(string symbol) =>
        OpResult.Fail($"Symbol '{symbol}' is not in the alphabet.");

    private static OpResult SymbolAlreadyInAlphabet(string symbol) =>
        OpResult.Fail($"Symbol '{symbol}' is already in the alphabet.");

    private static OpResult CharacterInClass(char c, SymbolClass owner) =>
        OpResult.Fail($"Character '{c}' already belongs to class '{owner.Name}'.");
}
