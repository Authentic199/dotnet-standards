# Content extensions and property flattening

Full source for the three content builders — `ToStringContent`, `ToFormDataContent`,
`ToFormUrlEncodedContent` — and the flattening machinery beneath them.

## Contents

| File | Project location | What it is |
|---|---|---|
| [HttpClientExtensions.cs](#httpclientextensionscs) | `Infrastructure/Facades/Common/HttpClients/` | The three content builders |
| [HttpPropertyFlattener.cs](#httppropertyflattenercs) | `Infrastructure/Facades/Common/HttpClients/` | Flattener subclass that honours `[FormName]` |
| [FormNameAttribute.cs](#formnameattributecs) | `Infrastructure/Facades/Common/Attributes/` | Wire-name override attribute |
| [PropertyFlatten.cs](#propertyflattencs) | `Core/Helpers/PropertyFlatten/` | The flattened-property record |
| [PropertyFlattenOptions.cs](#propertyflattenoptionscs) | `Core/Helpers/PropertyFlatten/` | Delimiter and depth options |
| [PropertyFlattener.cs](#propertyflattenercs) | `Core/Helpers/PropertyFlatten/` | The reflection walker |
| [ReflectionHelper.cs](#reflectionhelpercs) | `Core/Helpers/` | `GetElementTypeOfCollection` / `IsCollection` |

**Filenames.** Create these as `HttpClientExtensions.cs` and `FormNameAttribute.cs`. Existing
projects may spell the files `HttpClientExtentions.cs` and `FormNameAtrribute.cs`; the class
names inside are already correct and nothing breaks either way. Match whatever the project you
are in already has — do not rename as a drive-by.

**Package dependencies.** `Humanizer` (2.14.1) for the `Underscore()` call in the snake-case
overload — note the reference sits on the `Core` project in the reference solution while the
call site is in `Infrastructure`, so it resolves transitively there; a project recreating only
the `Infrastructure` half may need the reference added where the call actually lives.
`Microsoft.AspNetCore.Http` for `IFormFile`. Everything else is BCL. Versions for the whole
facade are tabulated in `sender-and-result.md`.

## How the pieces fit

`ToFormDataContent` / `ToFormUrlEncodedContent` hand the request object to
`HttpPropertyFlattener`, which walks it by reflection and returns a flat list of
`PropertyFlatten(Path, Value, PropertyInfo, Depth, Index)`. `Path` is the wire field name:
nested properties become `Parent.Child` (the default `Dots` delimiter), and `[FormName("x")]`
replaces the segment entirely. Properties marked `[JsonIgnore]` are skipped.

The flattener treats `string`, primitives, enums, `DateTime`, `DateTimeOffset`, `TimeSpan`,
`Guid` and `byte[]` as leaves; everything else is recursed into unless `TypeAcceptFiller`
says otherwise.

`PropertyFlattener`'s constructor is `protected` — it is not meant to be instantiated
directly. `HttpPropertyFlattener` is the entry point, and it exists solely to override
`GetName` so `[FormName]` wins over the C# property name.

`TypeAcceptFiller` is how `ToFormDataContent` tells the walker to treat `IFormFile` as a leaf
value rather than an object to descend into. Without it the walker would flatten the file's own
properties.

A flattener instance accumulates into an instance list and is never cleared, so it is
single-use. The content builders construct a fresh one per call — do the same if you call
the flattener directly.

The two `ToFormUrlEncodedContent` overloads differ by one `bool` and produce **different wire
names** for the same request: `Parent.Child` from the no-argument form, `parent_child` from
the snake-case form. Pick one per integration and use it consistently.

## HttpClientExtensions.cs

```csharp
using Core.Helpers;
using Core.Helpers.PropertyFlatten;
using Humanizer;
using Microsoft.AspNetCore.Http;
using System.Collections;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;
using System.Text.Json;

namespace Infrastructure.Facades.Common.HttpClients;

/// <summary>
/// Extension methods for working with System.Net.Http.HttpClient.
/// </summary>
public static class HttpClientExtensions
{
    /// <summary>
    /// Converts data to StringContent using System.Text.Json.JsonSerializer with default encoding as Encoding.UTF8.
    /// </summary>
    /// <typeparam name="T">The type of the data. Should be a class or record type, not abstract or interface.</typeparam>
    /// <param name="request">The data to be serialized.</param>
    /// <param name="encoding">The encoding to be used for the content. Defaults to Encoding.UTF8.</param>
    /// <param name="mediatype">The media type of the content. Defaults to "application/json".</param>
    /// <param name="options">Options for customizing the serialization process.</param>
    /// <returns>StringContent representing the serialized data.</returns>
    public static StringContent ToStringContent<T>(this T request, Encoding? encoding = null, string mediatype = MediaTypeNames.Application.Json, JsonSerializerOptions? options = null)
        where T : class
        => new(JsonSerializer.Serialize(request, options), encoding, mediatype);

    /// <summary>
    /// Converts data to MultipartFormDataContent.
    /// </summary>
    /// <typeparam name="T">The type of the data. Should be a class or record type, not abstract or interface.</typeparam>
    /// <param name="request">The data to be converted to MultipartFormDataContent.</param>
    /// <exception cref="ArgumentNullException"><paramref name="request"/> is <c>null</c>.</exception>
    /// <exception cref="NotSupportedException">Not supported for IEnumerable types.</exception>
    /// <returns>MultipartFormDataContent representing the data.</returns>
    public static MultipartFormDataContent ToFormDataContent<T>(this T request)
        where T : class
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (request.GetType().IsAssignableTo(typeof(IEnumerable)))
        {
            throw new NotSupportedException($"Type based {nameof(IEnumerable)} does not support conversion.");
        }

        HttpPropertyFlattener flattener = new()
        {
            TypeAcceptFiller = x => typeof(IFormFile).IsAssignableFrom(x),
        };
        IEnumerable<PropertyFlatten> propertyFlattens = flattener.FlattenData(request);
        MultipartFormDataContent content = new();

        foreach (var property in propertyFlattens)
        {
            object? value = property.Value;
            if (value == null)
            {
                continue;
            }

            Type valueType = value.GetType();
            Type? elementType = valueType.GetElementTypeOfCollection();

            // If the property is a file, add it to the content as a StreamContent carrying its own content type.
            if (value is IFormFile file)
            {
                StreamContent stream = new(file.OpenReadStream());
                stream.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);

                content.Add(stream, property.Path, file.FileName);
            }

            // If the property is a collection, handle each item individually.
            else if (elementType != null && value is IEnumerable items)
            {
                foreach (var item in items)
                {
                    if (item is IFormFile fileInCollection)
                    {
                        StreamContent stream = new(fileInCollection.OpenReadStream());
                        stream.Headers.ContentType = new MediaTypeHeaderValue(fileInCollection.ContentType);

                        content.Add(stream, property.Path, fileInCollection.FileName);
                    }
                    else
                    {
                        content.Add(new StringContent(item?.ToString() ?? string.Empty, Encoding.UTF8), property.Path);
                    }
                }
            }

            // If the property is a simple type, add it to the content as StringContent.
            else
            {
                content.Add(new StringContent(value.ToString()!, Encoding.UTF8), property.Path);
            }
        }

        return content;
    }

    /// <summary>
    /// Converts data to FormUrlEncodedContent.
    /// </summary>
    /// <typeparam name="T">The type of the data. Should be a class or record type, not abstract or interface.</typeparam>
    /// <param name="request">The data to be converted to FormUrlEncodedContent.</param>
    /// <exception cref="ArgumentNullException"><paramref name="request"/> is <c>null</c>.</exception>
    /// <returns>FormUrlEncodedContent representing the data.</returns>
    public static FormUrlEncodedContent ToFormUrlEncodedContent<T>(this T request)
        where T : class
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (request.GetType().IsAssignableTo(typeof(IEnumerable)))
        {
            throw new NotSupportedException($"Type based {nameof(IEnumerable)} does not support conversion.");
        }

        HttpPropertyFlattener flattener = new();
        IEnumerable<PropertyFlatten> propertyFlattens = flattener.FlattenData(request);
        List<KeyValuePair<string, string>> formData = new();
        foreach (var property in propertyFlattens)
        {
            formData.Add(new KeyValuePair<string, string>(property.Path, property.Value?.ToString() ?? string.Empty));
        }

        return new FormUrlEncodedContent(formData);
    }

    /// <summary>
    /// Converts data to FormUrlEncodedContent, optionally rewriting each flattened path to snake_case.
    /// </summary>
    /// <typeparam name="T">The type of the data. Should be a class or record type, not abstract or interface.</typeparam>
    /// <param name="request">The data to be converted to FormUrlEncodedContent.</param>
    /// <param name="useSnakeCase">If true, each flattened path is rewritten to snake_case.</param>
    /// <exception cref="ArgumentNullException"><paramref name="request"/> is <c>null</c>.</exception>
    /// <returns>FormUrlEncodedContent representing the data.</returns>
    public static FormUrlEncodedContent ToFormUrlEncodedContent<T>(this T request, bool useSnakeCase)
        where T : class
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (request.GetType().IsAssignableTo(typeof(IEnumerable)))
        {
            throw new NotSupportedException($"Type based {nameof(IEnumerable)} does not support conversion.");
        }

        HttpPropertyFlattener flattener = new();
        IEnumerable<PropertyFlatten> propertyFlattens = flattener.FlattenData(request);
        List<KeyValuePair<string, string>> formData = new();
        foreach (var property in propertyFlattens)
        {
            string key = useSnakeCase ? property.Path.Underscore() : property.Path;
            formData.Add(new KeyValuePair<string, string>(key, property.Value?.ToString() ?? string.Empty));
        }

        return new FormUrlEncodedContent(formData);
    }
}
```

## HttpPropertyFlattener.cs

```csharp
using Core.Helpers.PropertyFlatten;
using Infrastructure.Facades.Common.Attributes;
using System.Reflection;

namespace Infrastructure.Facades.Common.HttpClients
{
    public class HttpPropertyFlattener : PropertyFlattener
    {
        public HttpPropertyFlattener(PropertyFlattenOptions? options = null)
            : base(options)
        {
        }

        public override string GetName(PropertyInfo propertyInfo)
        {
            return propertyInfo.GetCustomAttribute<FormNameAttribute>()?.Name ?? base.GetName(propertyInfo);
        }
    }
}
```

## FormNameAttribute.cs

```csharp
namespace Infrastructure.Facades.Common.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class FormNameAttribute : Attribute
    {
        public FormNameAttribute(string name)
        {
            Name = name;
        }

        public string Name { get; set; }
    }
}
```

Usage — the property keeps its C# name, the wire gets the third party's:

```csharp
public class UpsertEntityRequest
{
    [FormName("entity_code")]
    public string Code { get; set; } = default!;
}
```

## PropertyFlatten.cs

```csharp
using System.Reflection;

namespace Core.Helpers.PropertyFlatten
{
    public record PropertyFlatten(string Path, object? Value, PropertyInfo PropertyInfo, int Depth, int? Index);
}
```

## PropertyFlattenOptions.cs

```csharp
namespace Core.Helpers.PropertyFlatten;

/// <summary>
/// Options for flattening properties with specific delimiters.
/// </summary>
public class PropertyFlattenOptions
{
    /// <summary>
    /// Gets or sets the delimiter used for property flattening.
    /// </summary>
    public PropertyFlattenDelimiter Delimiter { get; set; } = PropertyFlattenDelimiter.Dots;

    /// <summary>
    /// Gets or sets the maximum depth for property flattening.
    /// If set, the flattening process will only traverse properties up to the specified depth.
    /// A depth of null or not set indicates no depth limit.
    /// </summary>
    public int? MaxDepth { get; set; }
}

/// <summary>
/// Enumeration representing different delimiters for property flattening.
/// </summary>
public enum PropertyFlattenDelimiter
{
    /// <summary>
    /// Represents don't have the delimiter.
    /// </summary>
    None = 0,

    /// <summary>
    /// Represents the dot (.) as the delimiter.
    /// </summary>
    Dots = 1,

    /// <summary>
    /// Represents square brackets [] as the delimiter.
    /// </summary>
    SquareBrackets = 2,

    /// <summary>
    /// Represents underscore (_) as the delimiter.
    /// </summary>
    Underscore = 3,

    /// <summary>
    /// Represents parentheses () as the delimiter.
    /// </summary>
    Parentheses = 4,

    /// <summary>
    /// Represents hyphen (-) as the delimiter.
    /// </summary>
    Hyphen = 5,

    /// <summary>
    /// Represents colons (:) as the delimiter.
    /// </summary>
    Colons = 6,

    /// <summary>
    /// Represents commas (,) as the delimiter.
    /// </summary>
    Commas = 7,

    /// <summary>
    /// Represents slash (/) as the delimiter.
    /// </summary>
    Slash = 8,
}
```

## PropertyFlattener.cs

```csharp
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;

namespace Core.Helpers.PropertyFlatten;

public class PropertyFlattener
{
    private readonly List<PropertyFlatten> propertyFlattens = new();
    private readonly PropertyFlattenOptions options;

    protected PropertyFlattener(PropertyFlattenOptions? options = null)
    {
        this.options = options ?? new();
    }

    /// <summary>
    /// Gets or sets a function that defines custom criteria for accepting or rejecting types.
    /// The function should take a Type as input and return true if the type is acceptable; otherwise, false.
    /// This property allows users to provide a custom filtering mechanism for types in addition to the default criteria
    /// defined such as: String, Primitive Type, Enum, TimeSpan, DateTimeOffset, DateTime, Guid, byte[]
    /// </summary>
    public Func<Type, bool>? TypeAcceptFiller { get; set; }

    /// <summary>
    /// Flattens the properties of the given object.
    /// </summary>
    /// <param name="data">The object whose properties need to be flattened.</param>
    /// <returns>A collection of flattened properties.</returns>
    public IReadOnlyCollection<PropertyFlatten> FlattenData(object data)
    {
        FlattenPropertyInfo(string.Empty, data);
        return propertyFlattens;
    }

    /// <summary>
    /// Gets the name of the property.
    /// </summary>
    /// <param name="propertyInfo">The property information.</param>
    /// <returns>The name of the property.</returns>
    public virtual string GetName(PropertyInfo propertyInfo) => propertyInfo.Name;

    /// <summary>
    /// Recursively flattens the properties of the parent object.
    /// </summary>
    /// <param name="path">The current path in the object hierarchy.</param>
    /// <param name="parent">The parent object whose properties are being flattened.</param>
    /// <param name="depth">The current depth in the object hierarchy.</param>
    private void FlattenPropertyInfo(string path, object parent, int depth = 0)
    {
        if (parent == null)
        {
            return;
        }

        Type parentType = parent.GetType();
        Type? enumerableType = parentType.GetElementTypeOfCollection();

        if (enumerableType != null)
        {
            parentType = enumerableType;
        }

        PropertyInfo[] propertyInfos = parentType.GetProperties().Where(x => x.GetCustomAttribute<JsonIgnoreAttribute>() == null).ToArray();
        PropertyInfo[] acceptProperties = GetAcceptProperties(propertyInfos);
        PropertyInfo[] deeperProperties = propertyInfos.Except(acceptProperties).ToArray();
        propertyFlattens.AddRange(acceptProperties.SelectMany(x => CreateFlattenProperties(x, path, parent, enumerableType, depth)));
        depth++;

        if (options.MaxDepth.HasValue && depth > options.MaxDepth.Value)
        {
            return;
        }

        foreach (PropertyInfo property in deeperProperties)
        {
            Type? elementType = property.PropertyType.GetElementTypeOfCollection();
            if (elementType != null)
            {
                string enumerableTypeName = GetPath(path, property);

                if (IsAcceptType(elementType))
                {
                    propertyFlattens.AddRange(CreateFlattenProperties(property, path, parent, enumerableType, depth));
                }
                else
                {
                    FlattenPropertyInfo(enumerableTypeName, property.GetValue(parent)!, depth);
                }
            }
            else
            {
                FlattenPropertyInfo(GetPath(path, property), property.GetValue(parent)!, depth);
            }
        }
    }

    /// <summary>
    /// Gets the properties that should be accepted based on type criteria.
    /// </summary>
    /// <param name="propertyInfos">An array of PropertyInfo objects.</param>
    /// <returns>An array of PropertyInfo objects that meet the acceptance criteria.</returns>
    private PropertyInfo[] GetAcceptProperties(PropertyInfo[] propertyInfos)
        => propertyInfos.Where(propertyInfo => IsAcceptType(propertyInfo.PropertyType)).ToArray();

    /// <summary>
    /// Checks if a given type is acceptable based on specified criteria.
    /// </summary>
    /// <param name="type">The Type to be checked.</param>
    /// <returns>True if the type is acceptable; otherwise, false.</returns>
    private bool IsAcceptType(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        return type == typeof(string) || type.IsPrimitive || type.IsEnum || type == typeof(DateTimeOffset) ||
               type == typeof(DateTime) || type == typeof(TimeSpan) || type == typeof(Guid) ||
               type == typeof(byte[])
               || TypeAcceptFiller?.Invoke(type) == true;
    }

    /// <summary>
    /// Creates flattened properties based on the provided property information and parent object.
    /// </summary>
    /// <param name="propertyInfo">The PropertyInfo of the property.</param>
    /// <param name="path">The current path in the object hierarchy.</param>
    /// <param name="parent">The parent object whose property is being flattened.</param>
    /// <param name="enumerableType">The element type of the property if it is enumerable; otherwise, null.</param>
    /// <param name="depth">The current depth in the object hierarchy.</param>
    /// <returns>An IEnumerable of PropertyFlatten objects representing the flattened properties.</returns>
    private IEnumerable<PropertyFlatten> CreateFlattenProperties(PropertyInfo propertyInfo, string path, object parent, Type? enumerableType, int depth)
    {
        string selectedTreeName = GetPath(path, propertyInfo);

        if (enumerableType != null)
        {
            dynamic enumerable = ((dynamic)parent).ToArray();
            int length = enumerable.Length;
            for (int i = 0; i < length; i++)
            {
                yield return new PropertyFlatten(selectedTreeName, propertyInfo.GetValue(enumerable[i]), propertyInfo, depth, i);
            }
        }
        else
        {
            yield return new PropertyFlatten(selectedTreeName, propertyInfo.GetValue(parent), propertyInfo, depth, null);
        }
    }

    /// <summary>
    /// Gets the path for a property based on the specified delimiter.
    /// </summary>
    /// <param name="path">The current path.</param>
    /// <param name="propertyInfo">The property information.</param>
    /// <returns>The path with the specified delimiter.</returns>
    private string GetPath(string path, PropertyInfo propertyInfo)
    {
        string propertyName = GetName(propertyInfo);
        StringBuilder pathBuilder = new();
        if (string.IsNullOrEmpty(path))
        {
            return pathBuilder.Append(propertyName).ToString();
        }
        else
        {
            pathBuilder.Append(path);
        }

        switch (options.Delimiter)
        {
            case PropertyFlattenDelimiter.None:
                break;

            case PropertyFlattenDelimiter.Dots:
                pathBuilder.Append('.').Append(propertyName);
                break;

            case PropertyFlattenDelimiter.SquareBrackets:
                pathBuilder.Append('[').Append(propertyName).Append(']');
                break;

            case PropertyFlattenDelimiter.Parentheses:
                pathBuilder.Append('(').Append(propertyName).Append(')');
                break;

            case PropertyFlattenDelimiter.Hyphen:
                pathBuilder.Append('-').Append(propertyName);
                break;

            case PropertyFlattenDelimiter.Colons:
                pathBuilder.Append(':').Append(propertyName);
                break;

            case PropertyFlattenDelimiter.Commas:
                pathBuilder.Append(',').Append(propertyName);
                break;

            case PropertyFlattenDelimiter.Slash:
                pathBuilder.Append('/').Append(propertyName);
                break;

            case PropertyFlattenDelimiter.Underscore:
                pathBuilder.Append('_').Append(propertyName);
                break;
        }

        return pathBuilder.ToString();
    }
}
```

## ReflectionHelper.cs

The flattener and `ToFormDataContent` both call `GetElementTypeOfCollection`. A project may
already have a shared reflection helper — **extend that one, do not add a second**. Create this
file only if nothing equivalent exists. See `common-extensions` for the general case.

```csharp
using System.Collections;

namespace Core.Helpers
{
    public static class ReflectionHelper
    {
        /// <summary>
        /// Gets the element type of a collection type.
        /// </summary>
        /// <param name="type">The collection type.</param>
        /// <returns>The element type if the type is a collection; otherwise, null.</returns>
        public static Type? GetElementTypeOfCollection(this Type type)
        {
            if (IsCollection(type))
            {
                return type.IsArray ? type.GetElementType() : type.GetGenericArguments().FirstOrDefault();
            }

            return null;
        }

        public static bool IsCollection(this Type type) => type != typeof(string) && typeof(IEnumerable).IsAssignableFrom(type);
    }
}
```
