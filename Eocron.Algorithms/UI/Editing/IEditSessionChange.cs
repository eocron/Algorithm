namespace Eocron.Algorithms.UI.Editing
{
    public interface IEditSessionChange<in TDocument>
    {
        void Redo(TDocument document);
        void Undo(TDocument document);
    }
}