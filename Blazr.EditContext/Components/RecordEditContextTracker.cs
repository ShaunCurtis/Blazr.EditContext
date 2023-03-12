using Blazr.EditContext.Data;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace Blazr.EditContext.Components;

public class RecordEditContextTracker : ComponentBase
{
    [CascadingParameter] private Microsoft.AspNetCore.Components.Forms.EditContext _editContext { get; set; } = default!;
    [Parameter, EditorRequired] public IRecordEditContext RecordEditContext { get; set; } = default!;

    protected override void OnInitialized()
    {
        ArgumentNullException.ThrowIfNull(this._editContext);
        ArgumentNullException.ThrowIfNull(this.RecordEditContext);

        this.RecordEditContext.SetAsClean = this.SetAsClean;
        this.RecordEditContext.SetAsDirty = this.SetAsDirty;
    }

    private void SetAsDirty(FieldIdentifier fi)
        => _editContext.NotifyFieldChanged(fi);

    private void SetAsClean(FieldIdentifier? fi)
    {
        if (fi is null)
            _editContext.MarkAsUnmodified();
        else
            _editContext.MarkAsUnmodified(fi ?? new FieldIdentifier());
    }
}
