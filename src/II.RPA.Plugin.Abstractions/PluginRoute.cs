namespace II.RPA.Plugin.Abstractions;

public sealed class PluginRoute : IEquatable<PluginRoute>
{
    private static readonly StringComparer NameComparer = StringComparer.OrdinalIgnoreCase;

    public PluginRoute(string packageName, string abilityName)
    {
        PackageName = ValidateSegment(packageName, nameof(packageName));
        AbilityName = ValidateSegment(abilityName, nameof(abilityName));
    }

    public string PackageName { get; }

    public string AbilityName { get; }

    public static PluginRoute Parse(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || !string.Equals(value, value.Trim(), StringComparison.Ordinal))
        {
            throw new ArgumentException("The plugin route cannot be empty or contain leading or trailing whitespace.", nameof(value));
        }

        var separatorIndex = value.IndexOf('.');
        if (separatorIndex <= 0 || separatorIndex != value.LastIndexOf('.') || separatorIndex == value.Length - 1)
        {
            throw new ArgumentException("The plugin route must contain one package and one ability segment.", nameof(value));
        }

        return new PluginRoute(value[..separatorIndex], value[(separatorIndex + 1)..]);
    }

    public bool Equals(PluginRoute? other)
    {
        return other is not null
            && NameComparer.Equals(PackageName, other.PackageName)
            && NameComparer.Equals(AbilityName, other.AbilityName);
    }

    public override bool Equals(object? obj) => Equals(obj as PluginRoute);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(PackageName, NameComparer);
        hash.Add(AbilityName, NameComparer);
        return hash.ToHashCode();
    }

    public override string ToString() => $"{PackageName}.{AbilityName}";

    public static bool operator ==(PluginRoute? left, PluginRoute? right) => Equals(left, right);

    public static bool operator !=(PluginRoute? left, PluginRoute? right) => !Equals(left, right);

    private static string ValidateSegment(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value)
            || !string.Equals(value, value.Trim(), StringComparison.Ordinal)
            || value.Contains('.', StringComparison.Ordinal))
        {
            throw new ArgumentException("A route segment cannot be empty, contain a period, or contain leading or trailing whitespace.", parameterName);
        }

        return value;
    }
}
