using Akka.Actor;
using Akka.Event;
using System;
using System.Collections.Generic;
using TourneyRadarServer.Messages;

namespace TourneyRadarServer.Actors
{
    public class TournamentManagerActor : UntypedActor
    {
        protected ILoggingAdapter Log { get; } = Context.GetLogger();

        private Dictionary<string, IActorRef> tournamentIdToActor = new();
        private Dictionary<IActorRef, string> actorToTournamentId = new();

        protected override void PreStart() => Log.Info("TournamentManager started");
        protected override void PostStop() => Log.Info("TournamentManager stopped");

        protected override void OnReceive(object message)
        {
            switch (message)
            {
                case UpdateTournaments updateMsg:
                    Log.Info($"Received batch update from Rx.NET with {updateMsg.Tournaments.Count} tournaments.");
                    foreach (var tournament in updateMsg.Tournaments)
                    {
                        Self.Tell(new TrackTournament(0, tournament.Id, tournament));
                    }
                    break;
                case TrackTournament trackMsg:
                    if (tournamentIdToActor.TryGetValue(trackMsg.TournamentId, out var actorRef))
                    {
                        actorRef.Forward(trackMsg);
                    }
                    else
                    {
                        Log.Info($"Creating actor for new tournament: {trackMsg.TournamentId}");
                        var tournamentActor = Context.ActorOf(TournamentActor.Props(trackMsg.TournamentId), $"tournament-{trackMsg.TournamentId}");

                        Context.Watch(tournamentActor);
                        tournamentIdToActor.Add(trackMsg.TournamentId, tournamentActor);
                        actorToTournamentId.Add(tournamentActor, trackMsg.TournamentId);

                        tournamentActor.Forward(trackMsg);
                    }
                    break;

                case Terminated t:
                    var tId = actorToTournamentId[t.ActorRef];
                    Log.Info($"Tournament actor {tId} has been terminated");
                    actorToTournamentId.Remove(t.ActorRef);
                    tournamentIdToActor.Remove(tId);
                    break;

                case RequestAllTournaments r:
                    Context.ActorOf(TournamentQueryActor.Props(
                        new Dictionary<IActorRef, string>(actorToTournamentId),
                        r.RequestId,
                        Sender,
                        TimeSpan.FromSeconds(3)
                    ));
                    break;
            }
        }

        public static Props Props() =>
    Akka.Actor.Props.Create<TournamentManagerActor>()
                    .WithDispatcher("tourney-custom-dispatcher");
    }
}
