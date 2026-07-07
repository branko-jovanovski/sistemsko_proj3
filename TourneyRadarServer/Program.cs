using Akka.Actor;
using Akka.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TourneyRadarServer.Actors;
using TourneyRadarServer.Messages;
using TourneyRadarServer.Services;

var hoconConfig = @"
    tourney-custom-dispatcher {
        type = Dispatcher
        executor = ""thread-pool-executor""
        thread-pool-executor {
            core-pool-size-min = 2
            core-pool-size-factor = 2.0
            core-pool-size-max = 8
        }
        throughput = 10
    }
";

var config = ConfigurationFactory.ParseString(hoconConfig);

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var actorSystem = ActorSystem.Create("TourneyRadarSystem", config);

var managerActor = actorSystem.ActorOf(
    TournamentManagerActor.Props(),
    "tournament-manager"
);

var tournamentService = new TournamentService();
var requestIdCounter = 1L;

var rxSubscription = tournamentService.GetTournamentsStream().Subscribe(tournaments =>
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine($"\n[Rx.NET] Received {tournaments.Count} tournaments. Routing batch update to TournamentManager on thread ID: {Environment.CurrentManagedThreadId}...");
    Console.ResetColor();

    managerActor.Tell(new UpdateTournaments(tournaments));
});

Thread shutdownThread = new Thread(() =>
{
    Console.WriteLine("[Shutdown-Thread] Press any key in console to shut down the server...");
    Console.ReadKey();

    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("\n[Shutdown-Thread] Graceful server shutdown initiated...");
    Console.ResetColor();

    Console.WriteLine("[Shutdown-Thread] Disposing Rx.NET subscription...");
    rxSubscription.Dispose();
    Console.WriteLine("[Shutdown-Thread] Rx.NET subscription successfully disposed.");

    Console.WriteLine("[Shutdown-Thread] Shutting down Akka.NET ActorSystem...");
    try
    {
        actorSystem.Terminate().Wait(TimeSpan.FromSeconds(5));
        Console.WriteLine("[Shutdown-Thread] Akka.NET ActorSystem successfully terminated.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Shutdown-Thread Warning] Error during Akka system shutdown: {ex.Message}");
    }

    Console.WriteLine("[Shutdown-Thread] Stopping Web Server...");
    app.StopAsync().Wait();
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("[Shutdown-Thread] Web server successfully shut down. Goodbye.");
    Console.ResetColor();
})
{
    IsBackground = true,
    Name = "Shutdown-Thread"
};
shutdownThread.Start();

app.MapGet("/", async (HttpContext context) =>
{
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine($"\n[Web Server] Received HTTP GET request from: {context.Connection.RemoteIpAddress} at {DateTime.Now:HH:mm:ss}");
    Console.ResetColor();

    try
    {
        var currentRequestId = Interlocked.Increment(ref requestIdCounter);

        var response = await managerActor.Ask<RespondAllTournaments>(
            new RequestAllTournaments(currentRequestId),
            TimeSpan.FromSeconds(5)
        );

        var htmlBuilder = new StringBuilder();
        htmlBuilder.Append("<html><head><style>body{font-family:Arial; margin:40px;} h2{color:#2c3e50;} li{margin-bottom:5px;}</style></head><body>");
        htmlBuilder.Append("<h1>TourneyRadar - Esports Tournament Dashboard</h1>");

        foreach (var group in response.GroupedTournaments)
        {
            htmlBuilder.Append($"<h2>Status: {group.Key} ({group.Value.Count} tournaments)</h2><ul>");

            foreach (var t in group.Value)
            {
                htmlBuilder.Append($"<li>{t.ToString()}</li>");
            }

            htmlBuilder.Append("</ul>");
        }
        htmlBuilder.Append("</body></html>");

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"[Web Server] Request ID {currentRequestId} successfully processed. Returning response to client.");
        Console.ResetColor();

        context.Response.ContentType = "text/html; charset=utf-8";
        await context.Response.WriteAsync(htmlBuilder.ToString());
    }
    catch (Exception ex)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"[Web Server] ERROR processing request: {ex.Message}");
        Console.ResetColor();

        context.Response.StatusCode = 500;
        await context.Response.WriteAsync("An error occurred on the server while retrieving tournament data.");
    }
});

app.Run();