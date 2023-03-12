namespace Blazr.EditContext.Data;

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
