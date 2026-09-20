namespace Eocron.Indexation;

public interface IInvertedIndex<out TEntity>
{
    IEnumerable<TEntity> Search(SearchFilterInfo filterInfo);
}

public class SearchFilterInfo
{
    public Dictionary<string, List<object>> Include { get; set; } = new();

    public Dictionary<string, List<object>> Exclude { get; set; } = new();
}