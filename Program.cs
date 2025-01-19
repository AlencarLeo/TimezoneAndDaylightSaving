var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

var events = new List<Event>();

// POST EXAMPLE
// {
//     "info": "Meeting with client",
//     "localDate": "2030-12-12T22:00:00",
//     "timeZone": "America/Sao_Paulo"
// }

app.MapPost("/events", (CreateEventRequest request) =>
{
    if (string.IsNullOrEmpty(request.Info) || string.IsNullOrEmpty(request.TimeZone))
    {
        return Results.BadRequest("Info and TimeZone are required.");
    }

    try
    {
        var timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(request.TimeZone);
        var originalLocalDate = request.LocalDate;
        var utcDate = TimeZoneInfo.ConvertTimeToUtc(originalLocalDate, timeZoneInfo);

        var newEvent = new Event
        {
            Id = request.Id,
            Info = request.Info, 
            UtcDate = new DateTimeOffset(utcDate),
            OriginalLocalDate = originalLocalDate,
            TimeZone = request.TimeZone
        };

        events.Add(newEvent);

        return Results.Created($"/events/{newEvent.Id}", newEvent);
    }
    catch (Exception ex)
    {
        return Results.BadRequest($"Error: {ex.Message}");
    }
})
.WithName("CreateEvent")
.WithTags("Event Management")
.Produces<Event>(201)
.Produces<string>(400)
.Produces(500);

app.MapGet("/events/{id}", (int id, int? simulatedOffsetHours) =>
{
    var evt = events.FirstOrDefault(e => e.Id == id);
    if (evt == null)
    {
        return Results.NotFound("Event not found.");
    }

    var timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(evt.TimeZone);

    TimeSpan offset;
    if (simulatedOffsetHours.HasValue)
    {
        offset = TimeSpan.FromHours(simulatedOffsetHours.Value);
    }
    else
    {
        offset = timeZoneInfo.GetUtcOffset(evt.UtcDate.UtcDateTime);
    }

    var reconstructedLocalDate = evt.UtcDate.UtcDateTime.Add(offset);

    return Results.Ok(new
    {
        evt.Id,
        evt.Info,
        UtcDate = evt.UtcDate.UtcDateTime,
        OriginalLocalDate = evt.OriginalLocalDate,
        ReconstructedLocalDate = reconstructedLocalDate,
        evt.TimeZone,
        SimulatedOffset = simulatedOffsetHours.HasValue ? $"{simulatedOffsetHours.Value} hours" : "No simulation"
    });
})
.WithName("GetEvent")
.WithTags("Event Management")
.Produces<Event>(200)
.Produces<string>(404);

app.Run();

public class CreateEventRequest
{
    public int Id { get; set; } 
    public string Info { get; set; }
    public DateTime LocalDate { get; set; }
    public string TimeZone { get; set; }
}

public class Event
{
    public int Id { get; set; } 
    public string Info { get; set; }
    public DateTimeOffset UtcDate { get; set; }
    public DateTimeOffset OriginalLocalDate { get; set; }
    public string TimeZone { get; set; }
}