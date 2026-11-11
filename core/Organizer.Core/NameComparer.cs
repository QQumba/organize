using System.Globalization;

namespace Organizer.Core;

/// <summary>
/// The one comparison for Category and Tag names: equality and order, invariant culture, case-insensitive,
/// canonically equivalent forms equal. It lives in C# because SQLite's NOCASE folds ASCII only; the database
/// still guards ASCII duplicates, but uniqueness for non-ASCII names (Cyrillic) is enforced here.
/// </summary>
public static class NameComparer
{
    private const CompareOptions Options = CompareOptions.IgnoreCase;

    private static readonly CompareInfo Info = CultureInfo.InvariantCulture.CompareInfo;

    public static readonly IComparer<string> Order = new OrderComparer();

    public static readonly IEqualityComparer<string> Equality = new EqualityComparerImpl();

    public static bool Same(string a, string b) => Info.Compare(a, b, Options) == 0;

    /// <summary>The first item (callers pass items in ascending Id order) whose name equals <paramref name="name"/>.</summary>
    public static T? Find<T>(IEnumerable<T> items, Func<T, string> nameOf, string name) where T : class =>
        items.FirstOrDefault(i => Same(nameOf(i), name));

    private sealed class OrderComparer : IComparer<string>
    {
        public int Compare(string? x, string? y) => Info.Compare(x, y, Options);
    }

    private sealed class EqualityComparerImpl : IEqualityComparer<string>
    {
        public bool Equals(string? x, string? y) => Info.Compare(x, y, Options) == 0;

        public int GetHashCode(string s) => Info.GetHashCode(s, Options);
    }
}
