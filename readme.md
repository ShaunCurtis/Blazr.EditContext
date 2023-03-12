The purpose of this repository is to demonstrate how to track state in an edit form whwn using the standard Blazor implementation i.e. `EditContext` and `EditForm`.  The code also demonstrates using the .Net7.0 `NavigationLocker` component to manage navigation.

`EditContext` implements the field state concept to provide rudimentary tracking of individual field state.  `InputBase` components call the `NotifyFieldChanged` method whenever their value changes.  `EditContext` provides an `OnFieldChanged` event, and `IsModified` and `MarkAsModified` methods.

However, you would soon hit problems if you used these the detect and manage state on an edit object.  Neither `EditContext` or the individual components track the original state of the `Model`: they only know that a value has changed since the last time it was changed.  You could change the Temperature from 5 to 10 and then back to 5 and the `EditContext` would still believe it was dirty.

## Tracking State

In order to track atate we need to know what the original state is.  If we're editing our model class then we need to keep a copy of it's initisl state.  Not that easy with classes.

### The Record

The solutiuon is to create a `record` object of our base class.  Here's the `WeatherForecastRecord`.

```csharp
public record WeatherForecastRecord
{
    public DateOnly Date { get; init; }
    public int TemperatureC { get; init; }
    public string? Summary { get; init; }

    public WeatherForecastRecord() { }

    public WeatherForecastRecord(WeatherForecast record)
    {
        this.Date = record.Date;
        this.Summary = record.Summary;
        this.TemperatureC = record.TemperatureC;
    }
}
```

Making a copy is now as simple as:

```csharp
var record = new WeatherForecastRecord(new());
var copy = record with {};
```

### The Record Edit Context

This is the *Model*.  It tracks changes to individual properties, manages the state and raising `Actions`.  It creates a record whwn initialized to capture the initial state. 

```csharp
public class WeatherForecastEditContext : IRecordEditContext
{
    private WeatherForecastRecord _baseRecord = new();
    private WeatherForecast _baseClass = new();

    private DateOnly _date;
    public DateOnly Date
    {
        get => _date;
        set => this.Set(value, ref _date, _baseRecord.Date, "Date");
    }

    private int _temperatureC;
    public int TemperatureC
    {
        get => _temperatureC;
        set => this.Set(value, ref _temperatureC, _baseRecord.TemperatureC, "TemperatureC");
    }

    private string? _summary;
    public string? Summary
    {
        get => _summary;
        set => this.Set(value, ref _summary, _baseRecord.Summary, "Summary");
    }

    // gets a new record based on the current edit state
    public WeatherForecastRecord AsRecord =>
        new()
        {
            TemperatureC = this.TemperatureC,
            Date = this.Date,
            Summary = this.Summary
        };

    // compares the current state with the base record
    public bool IsDirty => _baseRecord != this.AsRecord;

    // Actions mapped within the UI Components to the Edit Context
    public Action<FieldIdentifier>? SetAsDirty { get; set; }
    public Action<FieldIdentifier?>? SetAsClean { get; set; }

    public WeatherForecastEditContext(WeatherForecast record)
        => this.LoadFromClass(record);

    public WeatherForecast UpdateClass(WeatherForecast? item = null)
    {
        item = item ?? _baseClass;
        item.Date = _date;
        item.Summary = _summary;
        item.TemperatureC = _temperatureC;
        return item;
    }

    // Resets the context back to base
    public void Reset()
    {
        _temperatureC = _baseRecord.TemperatureC;
        _date = _baseRecord.Date;
        _summary = _baseRecord.Summary;
        this.SetAsClean?.Invoke(null);
    }

    // Loads the context from the data class
    public void LoadFromClass(WeatherForecast record)
    {
        _baseClass = record;
        _baseRecord = new(record);
        _temperatureC = record.TemperatureC;
        _date = record.Date;
        _summary = record.Summary;
    }

    // Method called by the property setters to set the values,
    // make state decisions and raise actions
    private void Set<T>(T value, ref T setter, T baseValue, string fieldName)
    {
        setter = value;
        var isModified = (value is not null && baseValue is null)
            || value is not null && !value.Equals(baseValue);

        var fi = new FieldIdentifier(this, fieldName);

        if (isModified)
            this.SetAsDirty?.Invoke(fi);
        else
            this.SetAsClean?.Invoke(fi);
    }
}
```
The context implements the `IRecordEditContext` which is used in the components.

```csharp
public interface IRecordEditContext
{
    public bool IsDirty { get; }

    public Action<FieldIdentifier>? SetAsDirty { get; set; }
    public Action<FieldIdentifier?>? SetAsClean { get; set; }
}
```

### The RecordEditContextTracker Component

This is the component added within the `EditForm` which hooks up the RecordEditContext with the EditContext.

```csharp
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
```

### The Edit Form

The `EditContext` and `RecordEditContext` are set up in `OnInitialized` to ensure they have values before any rendering takes place.

```csharp
@page "/"

<PageTitle>Index</PageTitle>

<h1>Hello, Weather!</h1>

<div class=" p-2 mb-3">
    <button class="btn btn-success" @onclick=this.SetAsBalmy>Set As Balmy</button>
    <button class="btn btn-primary" @onclick=this.SetAsCold>Set As Cold</button>
</div>

<EditForm EditContext=this.editContext>

    <RecordEditContextTracker RecordEditContext=this.recordContext />

    <div class="container-fluid">
        <div class="row">
            <div class="col-lg-4">
                <div class="form-floating mb-3">
                    <InputDate class="form-control" placeholder="Enter a Date" @bind-Value=this.recordContext.Date />
                    <label>Date</label>
                </div>
            </div>

            <div class="col-lg-4">
                <div class="form-floating mb-3">
                    <InputNumber class="form-control" placeholder="enter a temperature" @bind-Value=this.recordContext.TemperatureC />
                    <label>Temperature &deg;C</label>
                </div>
            </div>

            <div class="col-12 col-lg-4">
                <div class="form-floating mb-3">
                    <InputText class="form-control" placeholder="enter a summary" @bind-Value=this.recordContext.Summary />
                    <label>Summary</label>
                </div>
            </div>

            <div class=" p-2 mb-3 text-end">
                <button hidden="@(!this.recordContext.IsDirty)" class="btn btn-warning" @onclick=this.Reset>Reset</button>
                <button disabled="@(!this.recordContext.IsDirty)" class="btn btn-success">Save</button>
                <button hidden="@(!this.recordContext.IsDirty)" class="btn btn-danger">Exit without Saving</button>
                <button disabled="@(this.recordContext.IsDirty)" class="btn btn-dark">Exit</button>
            </div>

        </div>
    </div>

</EditForm>

<NavigationLock OnBeforeInternalNavigation=this.OnLocationChanging ConfirmExternalNavigation=this.recordContext.IsDirty />

@code {
    // we set these to default to override the compiler null checking
    // They must be set before any attempt to render
    private EditContext editContext = default!;
    private WeatherForecastEditContext recordContext = default!;

    // create some dummy data
    private WeatherForecast model = new() { Date = DateOnly.FromDateTime(DateTime.Now), Summary = "Hot", TemperatureC = 25 };


    public override Task SetParametersAsync(ParameterView parameters)
    {
        parameters.SetParameterProperties(this);
        // Do any initial async data access here
        // Ensures the contexts are set and have data before any rendering takes place.
        return base.SetParametersAsync(ParameterView.Empty);
    }

    protected override void OnInitialized()
    {
        // Set these before any rendering takes place
        recordContext = new WeatherForecastEditContext(model);
        editContext = new EditContext(recordContext);
    }

    protected override Task OnInitializedAsync()
    {
        //Do not do async coding here to populate the edit contexts
        // I
        ArgumentNullException.ThrowIfNull(this.editContext);
        ArgumentNullException.ThrowIfNull(this.recordContext);

        return base.OnInitializedAsync();
    }

    private void SetAsBalmy()
    {
        this.recordContext.Summary = "Balmy";
        this.recordContext.TemperatureC = 20;
    }

    private void SetAsCold()
    {
        this.recordContext.Summary = "Cold";
        this.recordContext.TemperatureC = 5;
    }

    private void Reset()
        => this.recordContext.Reset();

    private void OnLocationChanging(LocationChangingContext context)
    {
        if (this.recordContext.IsDirty)
            context.PreventNavigation();
    }
}
```