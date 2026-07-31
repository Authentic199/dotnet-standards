# RandomExtensions

**When:** generating a one-time code, a verification or referral code, a temporary identifier, a
nonce, or any short human-facing string that must mix character classes.

**Why:** one seeded `Random` and one set of generators keeps every generated code in a project
drawing from the same alphabets. The member that earns the file is `RandomAlphabetOrSymbols`,
which draws **non-repeating type slots** so a requested mix of classes is actually produced
rather than statistically hoped for.

> **Corrected canon — read this before grepping.** The corpus carries this file misspelled, as
> `RamdomExtentions.cs` with type `RamdomExtentions`, in **all six** projects. The canonical name
> going forward is `RandomExtensions.cs` / `RandomExtensions`, used below.
>
> **The type name is corrected but nothing inside it is, and that is not an inconsistency:** a
> recreator chooses the file and type name fresh, whereas every member name is already load-bearing
> public API in the projects that carry this file — correcting one of those in a single project
> breaks its call sites and breaks the cross-project grep at the same time.
>
> - **In an existing project, grep both spellings** before concluding the file is absent. Do not
>   rename it in place unless you also fix its callers — one other base file takes a `using
>   static` on the type (see `references/password-extension.md`).
> - **Member names, parameter names and the thrown message are NOT corrected.** `lenght`,
>   `RandomLowwerCase` and the `ArgumentException` text ship exactly as written. The exception
>   text is the string a developer greps for when it shows up in a log, so it is API surface for
>   the same reason the member names are.
> - Four XML doc comments on `CharacterType` were written in the team's spoken language and are
>   translated to English here. No code changed.
>
> **Merged members.** `RandomAlphaNumericUpperCase` comes from the canonical project;
> `RandomPercentage` from a second project in the corpus. Both are reproduced at their corpus
> insertion points, and each has a live call site in its home project. Everything else is the
> byte-identical core shared by four projects. The RNG, its seeding and every algorithm are
> transcribed unchanged.

```csharp
using System.Text;

namespace Infrastructure.Facades.Common.Extensions;

// CORRECTED CANON: type renamed from the corpus spelling RamdomExtentions.
public static class RandomExtensions
{
    public const string Symbols = "!@#$%^&*()_-";
    public const string FullSymbols = @"!""#$%&'()*+,-./:;<=>?@[\]^_`{|}~";
    public static readonly Random Random = new(Environment.TickCount);

    public enum CharacterType
    {
        /// <summary>
        /// Upper-case letter.
        /// </summary>
        Upper = 1,

        /// <summary>
        /// Lower-case letter.
        /// </summary>
        Lower = 2,

        /// <summary>
        /// Special character.
        /// </summary>
        Symbol = 3,

        /// <summary>
        /// Digit.
        /// </summary>
        Digit = 4,
    }

    // MERGED from a second project in the corpus.
    public static double RandomPercentage()
    {
        return Random.NextDouble() * 100;
    }

    public static string RandomString(int length, string chars)
    {
        StringBuilder build = new();
        build.Clear();
        for (int i = 0; i < length; i++)
        {
            build.Append(chars[Random.Next(0, chars.Length)]);
        }

        return build.ToString();
    }

    public static string RandomDigit(int lenght = 1)
    {
        if (lenght == 0)
        {
            return string.Empty;
        }

        StringBuilder builder = new();
        for (int i = 0; i < lenght; i++)
        {
            builder.Append(Convert.ToChar((int)Math.Floor((10 * Random.NextDouble()) + 48)));
        }

        return builder.ToString();
    }

    public static string RandomUpperCase(int lenght = 1)
    {
        if (lenght == 0)
        {
            return string.Empty;
        }

        StringBuilder builder = new();
        for (int i = 0; i < lenght; i++)
        {
            builder.Append(Convert.ToChar((int)Math.Floor((26 * Random.NextDouble()) + 65)));
        }

        return builder.ToString();
    }

    // MERGED from the canonical project.
    public static string RandomAlphaNumericUpperCase(int lenght = 1)
        => RandomString(lenght, "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789");

    public static string RandomLowwerCase(int lenght = 1)
        => RandomUpperCase(lenght).ToLower();

    public static string RandomSymbols(int lenght = 1, string symbols = Symbols)
    {
        if (lenght == 0)
        {
            return string.Empty;
        }

        StringBuilder builder = new();
        for (int i = 0; i < lenght; i++)
        {
            builder.Append(symbols[Random.Next(symbols.Length)]);
        }

        return builder.ToString();
    }

    public static string RandomAlphabetOrSymbols(int lenght = 1, string symbols = Symbols, params CharacterType[] types)
    {
        if (lenght == 0)
        {
            return string.Empty;
        }

        if (!types.Any())
        {
            throw new ArgumentException("The type of character when perform random cant not be emty");
        }

        StringBuilder builder = new();

        // Non-repeating slot draw: every requested type is consumed once before any repeats,
        // so a requested mix is produced, not merely made probable.
        foreach (int value in RandomRangeNotRepeat(lenght, 0, types.Length))
        {
            builder.Append(types[value] switch
            {
                CharacterType.Digit => RandomDigit(),
                CharacterType.Upper => RandomUpperCase(),
                CharacterType.Lower => RandomLowwerCase(),
                CharacterType.Symbol => RandomSymbols(symbols: symbols),
                _ => throw new NotImplementedException(),
            });
        }

        return builder.ToString();
    }

    /// <summary>
    /// <returns>Integer that is greater than or equal to min value and less than max value.</returns>
    /// </summary>
    private static IEnumerable<int> RandomRangeNotRepeat(int lenght, int minValue = 0, int maxValue = int.MaxValue)
    {
        IEnumerable<int> numer = Enumerable.Range(minValue, maxValue - minValue).OrderBy(_ => Random.Next());
        int numLenght = numer.Count();

        if (numLenght >= lenght)
        {
            return numer.Take(lenght);
        }

        return numer.Take(numLenght).Concat(RandomRangeNotRepeat(lenght - numLenght, minValue, maxValue));
    }
}
```

## Notes

- **Not cryptographically secure, and not to be used where that matters.** `Random` is a
  pseudo-random generator seeded from `Environment.TickCount`; two processes started in the same
  tick produce the same sequence. Use this for codes a user reads aloud — never for a password
  reset token, a session identifier, an API key, or anything an attacker benefits from guessing.
  For those, reach for a cryptographic generator instead.
- **`Random` is a public static field and is shared process-wide.** Concurrent requests draw from
  the same instance. Do not assign to it and do not hand it to code that expects exclusive
  ownership.
- **`RandomAlphabetOrSymbols` distributes evenly across character *classes*, not characters.**
  Twelve characters over `Upper, Lower` yields exactly six of each, because the type slots are
  exhausted and re-shuffled rather than drawn independently. When you want a uniform draw over a
  single alphabet, call `RandomString`; call this one when the caller needs at least one of each
  requested class.
- **`RandomAlphabetOrSymbols` ignores the `symbols` argument unless `CharacterType.Symbol` is in
  `types`.** Passing a custom symbol set without that type silently has no effect. Passing an
  empty `types` throws `ArgumentException` — including when a caller builds the array from flags
  and every flag happened to be off.
- **`RandomRangeNotRepeat` recurses once per exhausted pass.** Asking for a length far larger than
  `types.Length` produces one stack frame per pass. It is sized for short codes.
- **`RandomDigit` and the other single-character helpers return `string`, not `char`.** Callers
  that need a `char` convert, and that conversion only works because the default `lenght` is 1 —
  see the fill loop in `references/password-extension.md`.
- **`RandomLowwerCase` goes through `ToLower()`**, which is culture-sensitive. On a
  Turkish-culture thread the ASCII `I` does not lower-case to `i`. Set the culture explicitly at
  application start if generated codes must be stable across environments.

## Dependencies and registration

- `System.Text` only. No package reference.
- Static class — **no DI registration**.
- `Symbols` is the keyboard-safe default; `FullSymbols` must be passed explicitly.
- `references/password-extension.md` depends on this file. Recreate this one first.
