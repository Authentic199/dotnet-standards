# RegexExtension

**When:** the project matches or scrubs strings against a fixed pattern — a validator rule, a filename sanitiser, a config redactor.

**Why one file:** a pattern written at its call site is re-parsed on every call and cannot be reused, and the same pattern drifts into three slightly different spellings across modules. Holding every pattern here makes `[GeneratedRegex]` possible (the engine is compiled at build time, not at first use) and gives every rule method one name to consume.

Three layers, and the accessibility follows from the layer:

| Layer | Accessibility | Purpose |
|---|---|---|
| `Regex` fields | **public** | the handles rule methods and callers consume |
| pattern-template strings | **private** | the only place literal pattern text is composed at runtime |
| `[GeneratedRegex]` partials | **private** | the compiled backing for each field |

**The one exception to "patterns are compile-time constants":** two patterns take a caller-supplied accepted character set, so they cannot be fixed at compile time. They are built by substituting into a private template, never by assembling pattern text at the call site. `ReplaceSpecialCharacters` and `AcceptedCharactersPattern` are the only two members allowed to do this.

> **Corrected canon.** This file is the corpus form with four deviations, each marked at its code site:
> 1. `NonWhitespace`, `Digits` and `NumberOnly` were regex literals inlined in rule methods in `ValidatorExtension`. Hoisted to fields.
> 2. The "letters, digits and an optional accepted set" pattern was composed inline inside `NotSpecialCharacter`. Hoisted to the `AcceptedCharacters` template plus the public `AcceptedCharactersPattern(...)` builder, matching the mechanism `ReplaceSpecialCharacters` already uses.
> 3. `SpecialCharactersRg`, `IdentifierNumber` and `ColorCode` are merged in from a second project in the corpus; the canonical project lacks them.
> 4. Two members constructed a `Regex` where a field already existed or belonged — see the two `// Corrected canon:` comments in the code.
>
> A vehicle-registration pattern and its formatter exist in the canonical project and are deliberately not reproduced: single-project, business-specific.

```csharp
using System.Text.RegularExpressions;

namespace Infrastructure.Facades.Common.Extensions;

public static partial class RegexExtension
{
    public static readonly Regex NistPassword = NistPasswordRegex();
    public static readonly Regex Whitespace = WhiteSpaceRegex();
    public static readonly Regex VnPhoneNumber = VnPhoneNumberRegex();
    public static readonly Regex VnCountryCode = VnCountryCodeRegex();

    // Merged in from a second project.
    public static readonly Regex SpecialCharactersRg = SpecialCharactersRegex();
    public static readonly Regex IdentifierNumber = IdentifierNumberRegex();
    public static readonly Regex ColorCode = ColorCodeRegex();

    // Hoisted: these three were regex literals inlined in ValidatorExtension rule methods.
    public static readonly Regex NonWhitespace = NonWhitespaceRegex();
    public static readonly Regex Digits = DigitsRegex();
    public static readonly Regex NumberOnly = NumberOnlyRegex();

    // Corrected canon: SpecialCharacterRemoving allocated `new Regex("[^A-Za-z0-9_.]+")` on every call.
    public static readonly Regex FileNameSpecialCharacters = FileNameSpecialCharactersRegex();

    private const string TextToReplace = nameof(TextToReplace);
    private const string Seperator = ";";
    private static readonly string SpecialCharacters = $"[^a-zA-Z0-9{TextToReplace}]+";
    private static readonly string EqualSeperator = $@"{TextToReplace}(\s*)=(\s*)[^;]+{Seperator}";

    // Hoisted: NotSpecialCharacter composed this pattern inline in ValidatorExtension.
    private static readonly string AcceptedCharacters = $"^[A-Za-z0-9{TextToReplace}]*$";

    public static string ReplaceWhitespace(this string input, string replacement)
    {
        return Whitespace.Replace(input, replacement);
    }

    public static string ReplaceSpecialCharacters(this string input, string replacement, string? acceptCharacters = null)
    {
        string template = SpecialCharacters.Replace(TextToReplace, acceptCharacters, StringComparison.Ordinal);
        return new Regex(template).Replace(input, replacement);
    }

    public static string ReplaceByEqualSeperator(this string input, string replacement, params string[] props)
    {
        foreach (string key in props)
        {
            string pattern = EqualSeperator.Replace(TextToReplace, key, StringComparison.OrdinalIgnoreCase);
            input = Regex.Replace(input, pattern, replacement);
        }

        return input;
    }

    public static string ReplaceVnCountryCode(this string input, string replacement)
    {
        // Corrected canon: was `Regex.Replace(input, VnCountryCode.ToString(), replacement)`.
        // Rendering a compiled Regex back to a string re-parses the pattern on every call
        // and discards the generated engine entirely.
        return VnCountryCode.Replace(input, replacement);
    }

    /// <summary>
    /// Builds the "letters, digits and an optional accepted set" pattern.
    /// Returns a string because FluentValidation's Matches(string) caches the compiled
    /// Regex per pattern internally, so the caller does not need a Regex instance.
    /// </summary>
    public static string AcceptedCharactersPattern(string? acceptCharacters = null)
    {
        string escaped = string.IsNullOrEmpty(acceptCharacters)
            ? string.Empty
            : Regex.Escape(acceptCharacters);

        return AcceptedCharacters.Replace(TextToReplace, escaped, StringComparison.Ordinal);
    }

    /// <summary>
    /// Strips characters that are unsafe in a file name and appends a timestamp,
    /// so two uploads of the same name do not collide.
    /// </summary>
    public static string SpecialCharacterRemoving(this string input)
    {
        string name = Path.GetFileNameWithoutExtension(input);
        string extension = Path.GetExtension(input);

        // Corrected canon: was `Regex regex = new("[^A-Za-z0-9_.]+");` constructed here on every call.
        return $"{FileNameSpecialCharacters.Replace(name, string.Empty)}.{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}{extension}";
    }

    /// <summary>
    /// Checks whether the given string is a valid NIST password based on specific criteria.
    /// </summary>
    /// <param name="password">The input string to check for NIST password validity.</param>
    /// <returns>true if the string is a valid NIST password; otherwise, false.</returns>
    public static bool IsNISTPassword(this string password)
    {
        return NistPassword.IsMatch(password);
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhiteSpaceRegex();

    [GeneratedRegex(@"^[\S]*$")]
    private static partial Regex NonWhitespaceRegex();

    [GeneratedRegex(@"^(?=.*[A-Za-z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$")]
    private static partial Regex NistPasswordRegex();

    [GeneratedRegex(@"^((((\+?)84)(0{0,1})|0)(3|5|7|8|9)\d{8})$")]
    private static partial Regex VnPhoneNumberRegex();

    [GeneratedRegex("^(\\+?)84")]
    private static partial Regex VnCountryCodeRegex();

    [GeneratedRegex(@"[^a-zA-Z0-9]+")]
    private static partial Regex SpecialCharactersRegex();

    [GeneratedRegex(@"[^A-Za-z0-9_.]+")]
    private static partial Regex FileNameSpecialCharactersRegex();

    [GeneratedRegex(@"^(?:\d{8}\s?\d{1}|\d{12})$")]
    private static partial Regex IdentifierNumberRegex();

    [GeneratedRegex(@"^#([A-Fa-f0-9]{6})$")]
    private static partial Regex ColorCodeRegex();

    [GeneratedRegex(@"^[\d]*$")]
    private static partial Regex DigitsRegex();

    [GeneratedRegex(@"^[0-9]*$")]
    private static partial Regex NumberOnlyRegex();
}
```

## Notes

- **`Digits` vs `NumberOnly` are not duplicates.** `\d` matches Unicode decimal digits from any script; `[0-9]` matches ASCII only. Keep both; pick `NumberOnly` when the value is later parsed or stored as ASCII digits.
- **`Whitespace` vs `NonWhitespace` are unrelated despite the names.** `Whitespace` (`\s+`) is an unanchored scrub pattern for `Replace`. `NonWhitespace` (`^[\S]*$`) is an anchored whole-string assertion for a rule method. Every pattern here is anchored except the scrub patterns — if you add a pattern for a rule method, anchor it, or it will pass any string that merely *contains* a match.
- **`SpecialCharactersRg` carries the `Rg` suffix** because the private template `SpecialCharacters` already owns the unsuffixed name in the same class.
- **`Seperator` and `IsNISTPassword` keep their original spelling.** Both are public surface; renaming them is a breaking change for no behavioural gain.
- **`SpecialCharacterRemoving`'s timestamp is appended before the extension**, so a scrubbed name reads `name.<unix-seconds>.ext` — collisions within the same second still collide.
- **The phone-number and country-code patterns are locale-specific.** Swap the pattern text for your locale; the field names and the `ReplaceVnCountryCode` shape carry over unchanged.

> Documentation-derived: `Regex.Escape` is intended for escaping text used *outside* a character class. It does not escape `]` or `-`, both of which are meaningful inside `[...]`. `AcceptedCharactersPattern` substitutes into a character class, so an accepted set containing `]` or `-` can close the class early or form an unintended range. Keep accepted sets to ordinary punctuation such as `.@_`.

## Dependencies and registration

- `System.Text.RegularExpressions`; `System.IO` for `Path` (both covered by implicit usings in a default SDK project).
- `[GeneratedRegex]` requires **.NET 7 or later** and the class must be `partial`.
- **Older framework fallback:** replace each generated partial with a field initialiser — `public static readonly Regex Whitespace = new(@"\s+", RegexOptions.Compiled);` — and drop `partial`. The public surface is identical; only the compile-time-vs-first-use tradeoff changes.
- Static class, no DI registration.
