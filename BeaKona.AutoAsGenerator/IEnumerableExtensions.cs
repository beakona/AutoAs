namespace BeaKona.AutoAsGenerator;
internal static class IEnumerableExtensions
{
    public static HashSet<T> ToHashSet<T>(this IEnumerable<T> @this)
    {
        return @this != null ? new HashSet<T>(@this) : [];
    }
}
