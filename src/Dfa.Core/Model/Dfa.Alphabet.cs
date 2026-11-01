namespace Dfa.Core.Model;

public sealed partial class Dfa
{
    public string? SymbolOf(char c) => throw new NotImplementedException();

    public OpResult AddSymbol(string symbol)
    {
        if (symbol.Length != 1)
        {
            return OpResult.Fail("A simple symbol must have exactly one character. Use a symbol class for groups.");
        }

        if (_alphabet.Contains(symbol))
        {
            return OpResult.Fail($"Symbol '{symbol}' is already in the alphabet.");
        }

        _alphabet.Add(symbol);
        return OpResult.Success;
    }

    public OpResult AddSymbolClass(SymbolClass symbolClass) => throw new NotImplementedException();

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

    private static OpResult SymbolNotInAlphabet(string symbol) =>
        OpResult.Fail($"Symbol '{symbol}' is not in the alphabet.");
}
