using System.Collections.Generic;
using System.Linq;

namespace Client
{
    public static class EnumerableExtensions
    {
        public static IEnumerable<IEnumerable<T>> ByParts<T>(this IEnumerable<T> enumerable, uint partLength)
        {
            do
            {
                IEnumerable<T> takedEumerable = enumerable.Take((int) partLength);
                yield return takedEumerable;
                enumerable = enumerable.Skip((int) partLength);
            } while (enumerable.Any());
        }
    }
}