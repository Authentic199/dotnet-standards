# PropertyInfoExtension — canonical source

Name resolution for the pipeline: it turns a client-supplied string such as
`Category.Name` or `Items.Name` into a `PropertyInfo`, and discovers the default
search-field set. Recreate as
`Infrastructure/Facades/Common/Extensions/PropertyInfoExtension.cs`.

**This is the pipeline's slice of the file, not the whole of it.** The real
`PropertyInfoExtension` is a general-purpose utility that `common-extensions`
owns; members no stage here calls are deliberately absent. If the project already
has the file, use the copy that is there and add only what is missing.

**Prerequisites**
- `TypeExtension.IsCollection` — listed in `references/query-expression-extension.md`.
- A marker interface implemented by every entity (`IEntity` below). If the
  project's base entity has no marker interface, point `IsUserDefineType` at the
  base entity type instead.

**Notes for whoever copies this**
- Every lookup binds with `BindingFlags.IgnoreCase`, which is what makes every
  field name in the query string case-insensitive. Keep the flag set.
- The exclusion attributes are passed **in** by the caller. Do not append the
  search attribute inside `GetPropertyRecursiveWithMaxDeep` — that would weld a
  pipeline-specific attribute into a general utility.
- `NotMappedAttribute` is always excluded, without being asked for.
- The skip check is attribute-driven only. A condition naming a concrete type or
  a literal property name does not survive the copy into the next project, and
  `[NotSearchable]` expresses the same intent portably.
- `GetDataHolders` returns null unless the path's head resolves to a generic
  collection of entities **and** a further segment resolves on the element type.
  `acceptPrimitiveType: false` narrows the element to `string` only.

## `PropertyInfoExtension.cs`

```csharp
using Core.Bases;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;

namespace Infrastructure.Facades.Common.Extensions;

public static class PropertyInfoExtension
{
    public const char Point = '.';

    public record DataHolder(PropertyPair PropertyGenericCollection, PropertyPair PropertyElement);

    public record PropertyPair(string PropertyName, PropertyInfo PropertyInfo);

    public static DataHolder? GetDataHolders(this Type baseType, string propertyNames, bool acceptPrimitiveType = true)
    {
        PropertyInfo? propertyInfoGenericCollection = baseType.GetPropertyNameGenericCollection(propertyNames);

        if (propertyInfoGenericCollection is null)
        {
            return null;
        }

        string propertyNameGenericCollection = propertyNames[..(propertyNames.IndexOf(propertyInfoGenericCollection.Name, StringComparison.OrdinalIgnoreCase) + propertyInfoGenericCollection.Name.Length)];
        PropertyPair propertyGenericCollection = new(propertyNameGenericCollection, propertyInfoGenericCollection);

        if (propertyNames.Length <= (propertyNameGenericCollection.Length + 1))
        {
            return null;
        }

        string propertyNameElement = propertyNames[(propertyNameGenericCollection.Length + 1)..];
        Type genericArgumentType = propertyInfoGenericCollection.PropertyType.GetGenericArguments()[0];
        PropertyInfo? propertyInfoElement = genericArgumentType.GetPropertyRecursive(propertyNameElement);

        if (propertyInfoElement is null
            || (propertyInfoElement.PropertyType != typeof(string)
                && (!acceptPrimitiveType
                    || (!propertyInfoElement.PropertyType.IsValueType
                        && !propertyInfoElement.PropertyType.IsEnum
                        )
                    )
                )
            )
        {
            return null;
        }
        PropertyPair propertyElement = new(propertyNameElement, propertyInfoElement);

        return new(propertyGenericCollection, propertyElement);
    }

    public static PropertyInfo? GetPropertyRecursive(this Type baseType, string propertyNames)
    {
        string[] parts = propertyNames.Split('.');

        PropertyInfo? propertyInfo = baseType.GetProperty(parts[0], BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        if (propertyInfo is null)
        {
            return null;
        }

        return parts.Length > 1
            ? propertyInfo.PropertyType.GetPropertyRecursive(parts.Skip(1).Aggregate((a, i) => a + "." + i))
            : baseType.GetProperty(propertyNames, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
    }

    public static List<string> GetPropertyRecursiveWithMaxDeep(this Type baseType, uint level, params Type[] typeIgnores)
    {
        if (typeIgnores.Any(x => !x.IsSubclassOf(typeof(Attribute))))
        {
            throw new ArgumentException("Type ignores has an element that refers to a object, not a attribute");
        }

        return DumpObjectTree(baseType.GetProperties(), level, new List<string>(), string.Empty, new List<string>(), typeIgnores.Append(typeof(NotMappedAttribute)));

        List<string> DumpObjectTree(PropertyInfo[] propertyInfoes, uint level, List<string> result, string path, List<string>? objects, IEnumerable<Type> typeIgnores)
        {
            foreach (PropertyInfo propertyInfo in propertyInfoes)
            {
                if (propertyInfo.CustomAttributes.Any(x => typeIgnores.Contains(x.AttributeType)))
                {
                    continue;
                }

                if (level != 0
                    && (propertyInfo.PropertyType.IsGenericCollection() || !propertyInfo.PropertyType.FullName!.StartsWith("System"))
                    && !objects!.Any(x => x == propertyInfo.Name))
                {
                    objects!.Add(path.Split('.')[^1]);
                    DumpObjectTree(propertyInfo.PropertyType.IsGenericCollection() ? propertyInfo.PropertyType.GetGenericArguments()[0].GetProperties() : propertyInfo.PropertyType.GetProperties(), level - 1, result, !string.IsNullOrEmpty(path) ? string.Format("{0}.{1}", path, propertyInfo.Name) : propertyInfo.Name, objects, typeIgnores);
                }

                if (propertyInfo.PropertyType == typeof(string))
                {
                    result.Add(!string.IsNullOrEmpty(path) ? string.Format("{0}.{1}", path, propertyInfo.Name) : propertyInfo.Name);
                }
            }

            return result;
        }
    }

    public static bool IsUserDefineType(this Type type)
        => type.IsAssignableTo(typeof(IEntity));

    public static bool IsGenericCollection(this Type type)
        => type.IsCollection() && type.GetGenericArguments().FirstOrDefault()?.IsUserDefineType() == true;

    private static PropertyInfo? GetPropertyNameGenericCollection(this Type baseType, string propertyNames)
    {
        string[] parts = propertyNames.Split('.');

        PropertyInfo? propertyInfo = baseType.GetProperty(parts[0], BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

        if (propertyInfo is null)
        {
            return null;
        }

        if (propertyInfo.PropertyType.IsGenericCollection())
        {
            return propertyInfo;
        }

        return GetPropertyNameGenericCollection(propertyInfo.PropertyType, string.Join(Point, parts.Skip(1)));
    }
}
```

## `NotSearchableAttribute.cs`

Marks a property out of the **default** search sweep only. The client can still
filter and sort on it, and a property named explicitly in `SearchFields` is
unaffected — the attribute is consulted only when `ApplySearch` discovers the
field set itself.

```csharp
namespace Infrastructure.Facades.Common.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public class NotSearchableAttribute : Attribute
{
}
```

Older projects carry the same idea as `NotSearchAttribute`, appended *inside*
`GetPropertyRecursiveWithMaxDeep` rather than passed in from `ApplySearch`. When
porting, rename the attribute **and** move the wiring; shipping both spellings in
one project leaves half the annotations inert.

## Deviations from corpus

| Change | Reason |
|---|---|
| Skip condition reduced to the attribute test — a concrete domain type check and a literal property-name check removed | Sanitization; and the same exclusion is expressible as `[NotSearchable]` |
| The `using` for that domain type's namespace removed | Follows from the above |
| Attribute file converted from block-scoped to file-scoped namespace | Formatting only, consistent with the other two files (formatting is analyzer-delegated) |
| `TypeExtension.IsCollection()` not repeated here | Listed once, in `references/query-expression-extension.md` |
| `GetPropertyFromExpression` omitted | No file in this skill calls it, and the lineages disagree on its body — two add an `ExpressionType.Convert` unwrapping branch, four do not. `common-extensions` owns it; shipping a member this skill never calls would invite the two copies to drift |
