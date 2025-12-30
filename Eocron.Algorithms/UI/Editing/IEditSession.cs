using System.ComponentModel.DataAnnotations;

namespace Eocron.Algorithms.UI.Editing;

public interface IEditSession<out TDocument>
{
    TDocument Source { get; }
        
    TDocument Draft { get; }
        
    ValidationResult[] Validation { get; }
        
    bool IsEditing { get; }
        
    bool CanRedo { get; }
        
    bool CanUndo { get; }

    void BeginEdit();
        
    void Apply(IEditSessionChange<TDocument> change);
        
    bool TryCommit();

    void Rollback();
        
    void Undo();

    void Redo();
}