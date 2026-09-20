using System.Collections.Immutable;
using Roaring.Net.CRoaring;

namespace Eocron.Indexation;

public sealed class InvertedIndexBuilder<TEntity>
{
    public void WithBind<T>(
        string name,
        Func<TEntity, IEnumerable<T>?> entityPropertySelector,
        IEqualityComparer<object>? equalityComparer = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nameof(name));
        ArgumentNullException.ThrowIfNull(entityPropertySelector);
        equalityComparer ??= EqualityComparer<object>.Default;
        _bindings.Add((name, x=> entityPropertySelector(x)?.Cast<object>(), equalityComparer));
    }

    public void WithEntityOrdering(Func<IEnumerable<TEntity>, IEnumerable<TEntity>> entityOrderingSelector)
    {
        ArgumentNullException.ThrowIfNull(entityOrderingSelector);
        _entityOrderingSelector = entityOrderingSelector;
    }

    public IInvertedIndex<TEntity> BuildFrom(IEnumerable<TEntity> entities)
    {
        ArgumentNullException.ThrowIfNull(entities);
        var orderedEntities = (_entityOrderingSelector == null ? entities : _entityOrderingSelector(entities)).ToArray();
        var indexes = new List<Dictionary<object, Roaring32Bitmap>>();
        try
        {
            foreach (var binding in _bindings)
            {
                var index = new Dictionary<object, Roaring32Bitmap>(binding.Item3);
                for (uint i = 0; i < orderedEntities.Length; i++)
                {
                    var entity = orderedEntities[i];
                    var entityPropSet = binding.Item2(entity); //TODO: can throw errors
                    if (entityPropSet == null)
                    {
                        continue;
                    }

                    foreach (var p in entityPropSet)
                    {
                        if (!index.TryGetValue(p, out var bitmap))
                        {
                            bitmap = new Roaring32Bitmap((uint)orderedEntities.Length);
                            index.Add(p, bitmap);
                        }

                        bitmap.Add(i);
                    }
                }

                indexes.Add(index);
            }
        }
        catch
        {
            foreach (var bitmap in indexes.SelectMany(x=> x.Values))
            {
                bitmap.Dispose();
            }
            throw;
        }

        return new InvertedIndex<TEntity>(orderedEntities,
            indexes
                .Zip(_bindings)
                .Select(x => (
                    x.Second.Item1,
                    x.First
                        .ToImmutableDictionary(
                            y => y.Key,
                            y =>
                            {
                                y.Value.Optimize();
                                return y.Value;
                            },
                            x.Second.Item3)))
                .ToImmutableDictionary(x => x.Item1, x => x.Item2));
    }

    private readonly List<(string, Func<TEntity, IEnumerable<object>?>, IEqualityComparer<object>)> _bindings = new();
    private Func<IEnumerable<TEntity>, IEnumerable<TEntity>>? _entityOrderingSelector;
}