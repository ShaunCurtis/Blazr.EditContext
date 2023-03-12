using Microsoft.AspNetCore.Components.Forms;

namespace Blazr.EditContext.Data;

public interface IRecordEditContext
{
    public bool IsDirty { get; }

    public Action<FieldIdentifier>? SetAsDirty { get; set; }
    public Action<FieldIdentifier?>? SetAsClean { get; set; }
}
