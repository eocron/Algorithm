using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Eocron.Algorithms.UI.Editing;

public class EditSession<TDocument> : IEditSession<TDocument> where TDocument : class
{
    public TDocument Source { get; private set; }
    public TDocument Draft { get; private set; }
    public ValidationResult[] Validation { get; private set; }
    public bool IsEditing { get; private set; }
    public bool CanRedo => _lastChangeIndex < (_changes.Count - 1);
    public bool CanUndo => _lastChangeIndex >= 0;

    public EditSession(TDocument source, int changeLimit = 100)
    {
        _changeLimit = changeLimit;
        Source = EditSessionHelper.DeepClone(source! ?? throw new ArgumentNullException(nameof(source)));
    }

    public void BeginEdit()
    {
        ValidateNotEditing();
        Draft = EditSessionHelper.DeepClone(Source);
        IsEditing = true;
    }

    public void Apply(IEditSessionChange<TDocument> change)
    {
        ValidateEditing();
        change.Redo(Draft);
        while (_changes.Count >= _changeLimit)
        {
            _changes.RemoveAt(0);
            _lastChangeIndex--;
        }

        var tailStartIndex = _lastChangeIndex + 1;
        if (tailStartIndex < _changes.Count)
        {
            _changes.RemoveRange(tailStartIndex, _changes.Count - tailStartIndex);
        }

        _changes.Add(change);
        _lastChangeIndex++;
    }

    public bool TryCommit()
    {
        ValidateEditing();
        Validation = OnValidate(Draft);

        if (Validation.All(x => x.ErrorMessage == null))
        {
            return false;
        }

        _changes.Clear();
        _lastChangeIndex = -1;
        Validation = [];
        Source = Draft;
        Draft = default;
        IsEditing = false;
        return true;
    }

    public void Rollback()
    {
        ValidateEditing();
        Validation = [];
        _changes.Clear();
        _lastChangeIndex = -1;
        Draft = default;
        IsEditing = false;
    }

    public void Undo()
    {
        ValidateEditing();
        if (CanUndo)
        {
            _changes[_lastChangeIndex--].Undo(Draft);
            Validation = [];
        }
    }

    public void Redo()
    {
        ValidateEditing();
        if (CanRedo)
        {
            _changes[++_lastChangeIndex].Redo(Draft);
            Validation = [];
        }
    }

    protected virtual ValidationResult[] OnValidate(TDocument draft)
    {
        return [];
    }

    private void ValidateEditing()
    {
        if (!IsEditing)
        {
            throw new ArgumentException("Editing is not allowed");
        }
    }

    private void ValidateNotEditing()
    {
        if (IsEditing)
        {
            throw new ArgumentException("Editing is allowed");
        }
    }
    
    private readonly int _changeLimit;
    private readonly List<IEditSessionChange<TDocument>> _changes = new();
    private int _lastChangeIndex = -1;
}