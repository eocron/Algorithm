using System.Collections.Immutable;
using Roaring.Net.CRoaring;

namespace Eocron.Indexation;

public sealed class InvertedIndex<TEntity>(
    TEntity[] orderedEntities, 
    ImmutableDictionary<string, ImmutableDictionary<object, Roaring32Bitmap>> propertyIndexes) 
    : IInvertedIndex<TEntity>, IDisposable
{
    public IEnumerable<TEntity> Search(SearchFilterInfo filterInfo)
    {
        var includes = filterInfo.Include.Select(x => GetAllBitmaps(x.Key, x.Value));
        var excludes = filterInfo.Exclude.Select(x => GetAllBitmaps(x.Key, x.Value));
        
        Roaring32Bitmap? result = null;
        try
        {
            foreach (var include in includes)
            {
                var tmpOr = new Roaring32Bitmap((uint)orderedEntities.Length);

                foreach (var includeBitmap in include)
                {
                    tmpOr.IOr(includeBitmap);
                }

                if (result == null)
                {
                    result = tmpOr;
                }
                else
                {
                    result.IAnd(tmpOr);
                    tmpOr.Dispose();
                }

                if (result.IsEmpty)
                {
                    yield break;
                }
            }
            
            foreach (var exclude in excludes)
            {
                var tmpOr = new Roaring32Bitmap((uint)orderedEntities.Length);

                foreach (var excludeBitmap in exclude)
                {
                    tmpOr.IAndNot(excludeBitmap);
                }

                if (result == null)
                {
                    result = tmpOr;
                }
                else
                {
                    result.IAnd(tmpOr);
                    tmpOr.Dispose();
                }

                if (result.IsEmpty)
                {
                    yield break;
                }
            }

            if (result == null)
            {
                yield break;
            }
            foreach (var i in result.Values)
            {
                yield return orderedEntities[i];
            }
        }
        finally
        {
            result?.Dispose();
        }
    }

    private IEnumerable<Roaring32Bitmap> GetAllBitmaps(
        string propertyName,
        List<object> propertyValues)
    {
        if (!propertyIndexes.TryGetValue(propertyName, out var propertyIndex))
        {
            yield break;
        }
        foreach (var value in propertyValues)
        {
            if (propertyIndex.TryGetValue(value, out var bitmap))
            {
                yield return bitmap;
            }
        }
    }

    public void Dispose()
    {
        foreach (var bitmap in propertyIndexes.SelectMany(x=> x.Value.Values))
        {
            bitmap.Dispose();
        }
    }
}