using Microsoft.AspNetCore.Components.Forms;

namespace Blazr.EditContext.Data;

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
