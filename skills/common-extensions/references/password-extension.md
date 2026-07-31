# PasswordExtension

**When:** generating a password on the server — seeding an initial account, issuing a temporary
password after a reset, creating credentials for an imported user.

**Why:** a password built by drawing N characters from one pool routinely fails the very
complexity policy the application enforces on user input, which is why the naive version is
usually a draw-and-retry loop. This generator inverts the order: it **places one character of
each required class first**, then fills, so the policy holds by construction.

> **Corrected canon.** Transcribed from three corpus projects — two byte-identical, the third
> differing only in `new()` versus `[]` on the two list declarations, where `[]` is shipped. The
> algorithm, the ordering and the defaults are unchanged. The one deviation is the name of the
> dependency: the corpus type is misspelled `RamdomExtentions` and is referred to here by its
> canonical name `RandomExtensions` (see `references/random-extensions.md`). **If the project you
> are dropping this into already carries that file under the old spelling, use the old name here
> rather than renaming that file.**
>
> `PasswordOptions` is **project-defined** — it is not a framework type, and it ships in this same
> file exactly as the corpus has it.
>
> **Recreate `references/random-extensions.md` first. This file does not compile without it.**

```csharp
// CORRECTED CANON: canonical type name; the corpus spells it RamdomExtentions.
using static Infrastructure.Facades.Common.Extensions.RandomExtensions;

namespace Infrastructure.Facades.Common.Extensions;

public static class PasswordExtension
{
    public static string Generate(PasswordOptions? opt = null)
    {
        opt ??= new();
        List<char> chars = [];
        List<CharacterType> types = [];

        // One character of each required class is placed up front, so the result satisfies the
        // policy by construction. `types` then constrains the fill to those same classes.
        if (opt.RequireDigit)
        {
            types.Add(CharacterType.Digit);
            chars.Insert(
                RandomExtensions.Random.Next(0, chars.Count),
                Convert.ToChar(RandomDigit()));
        }

        if (opt.RequireLowercase)
        {
            types.Add(CharacterType.Lower);
            chars.Insert(
                RandomExtensions.Random.Next(0, chars.Count),
                Convert.ToChar(RandomLowwerCase()));
        }

        if (opt.RequireUppercase)
        {
            types.Add(CharacterType.Upper);
            chars.Insert(
                RandomExtensions.Random.Next(0, chars.Count),
                Convert.ToChar(RandomUpperCase()));
        }

        if (opt.RequireSymbols)
        {
            types.Add(CharacterType.Symbol);
            chars.Insert(
                RandomExtensions.Random.Next(0, chars.Count),
                Convert.ToChar(RandomSymbols()));
        }

        // Fill until BOTH targets are met — total length and distinct-character count.
        for (int i = chars.Count; i < opt.RequiredLength
            || chars.Distinct().Count() < opt.RequiredUniqueChars; i++)
        {
            chars.Insert(
                RandomExtensions.Random.Next(0, chars.Count),
                Convert.ToChar(RandomAlphabetOrSymbols(1, Symbols, types.ToArray())));
        }

        return string.Concat(chars.OrderBy(_ => RandomExtensions.Random.Next()));
    }
}

public class PasswordOptions
{
    /// <summary>
    /// Default value is 8
    /// </summary>
    public int RequiredLength { get; set; } = 8;

    /// <summary>
    /// Default value is 4
    /// </summary>
    public int RequiredUniqueChars { get; set; } = 4;

    /// <summary>
    /// Default value is true
    /// </summary>
    public bool RequireDigit { get; set; } = true;

    /// <summary>
    /// Default value is true
    /// </summary>
    public bool RequireLowercase { get; set; } = true;

    /// <summary>
    /// Default value is true
    /// </summary>
    public bool RequireUppercase { get; set; } = true;

    /// <summary>
    /// Default value is true
    /// </summary>
    public bool RequireSymbols { get; set; } = true;
}
```

## Notes

- **Inherits the security posture of `RandomExtensions`, which is not cryptographic.** Everything
  here draws from a shared `Random` seeded with `Environment.TickCount`. Acceptable for a
  temporary password that is delivered out of band and changed on first login; **not** acceptable
  as a long-lived credential or as a reset token.
- **`RequiredUniqueChars` larger than the reachable alphabet never terminates.** With only
  `RequireDigit` set, the fill draws from ten characters, so any `RequiredUniqueChars` above ten
  spins forever. Keep it below the reachable alphabet size, and validate it if it comes from
  configuration.
- **The result can be longer than `RequiredLength`.** The loop continues while *either* target is
  unmet, so a high `RequiredUniqueChars` keeps appending past the length target. Do not size a
  database column to `RequiredLength`.
- **All four `Require*` flags set to `false` throws.** `types` is empty and
  `RandomAlphabetOrSymbols` rejects an empty type array with `ArgumentException`. There is no
  "no constraints" mode.
- **`chars.Distinct().Count()` runs on every iteration**, so the fill is quadratic in the output
  length. Irrelevant at the default 8; do not reuse this to generate long strings.
- **Keep both randomizations — do not simplify the tail into `string.Concat(chars)`.** Characters
  are inserted at a random position *and* the finished list is shuffled again. Both steps are in
  the corpus algorithm and this file's contract is to reproduce it.
- **`PasswordOptions` is declared here and is a collision-prone name.** A near-identically shaped
  type ships with the framework's identity stack; a file importing both namespaces gets an
  ambiguous-reference error. Alias it at the use site — do not delete this one.

## Dependencies and registration

- **`references/random-extensions.md` — recreate that file first.** This file takes a `using
  static` on the type and calls `RandomDigit`, `RandomLowwerCase`, `RandomUpperCase`,
  `RandomSymbols`, `RandomAlphabetOrSymbols`, the `Symbols` constant, the `CharacterType` enum and
  the shared `Random` instance.
- No package reference.
- Static class — **no DI registration**.
