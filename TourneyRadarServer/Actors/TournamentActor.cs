using Akka.Actor;
using Akka.Event;
using TourneyRadarServer.Messages;
using TourneyRadarServer.Models;

namespace TourneyRadarServer.Actors
{
    public class TournamentActor : UntypedActor
    {
        protected ILoggingAdapter Log { get; } = Context.GetLogger();

        private Tournament? _tournament = null;

        protected string TournamentId { get; }

        public TournamentActor(string tournamentId)
        {
            TournamentId = tournamentId;
        }

        protected override void PreStart() => Log.Info($"Tournament actor {TournamentId} started");
        protected override void PostStop() => Log.Info($"Tournament actor {TournamentId} stopped");

        protected override void OnReceive(object message)
        {
            switch (message)
            {
                case TrackTournament trackMsg when trackMsg.TournamentId.Equals(TournamentId):

                    Log.Info($"[Actor {TournamentId}] Updating internal tournament state.");
                    _tournament = trackMsg.Tournament;

                    if (Sender != ActorRefs.NoSender)
                    {
                        Sender.Tell(new TournamentRegistered(trackMsg.RequestId, TournamentId));
                    }
                    break;

                case TrackTournament trackMsg:
                    Log.Warning($"Ignoring request for {trackMsg.TournamentId}. Actor is responsible for {TournamentId}.");
                    break;

                case ReadTournament readMsg:

                    Log.Info($"[Actor {TournamentId}] Received request to read state (RequestId: {readMsg.RequestId}).");

                    ITournamentReading reading;
                    if (_tournament == null)
                    {
                        Log.Warning($"[Actor {TournamentId}] Tournament data is not yet available.");
                        reading = TournamentNotAvailable.Instance;
                    }
                    else
                    {
                        Log.Info($"[Actor {TournamentId}] Returning successful tournament state.");
                        reading = new TournamentReading(_tournament);
                    }
                    Sender.Tell(new RespondTournament(readMsg.RequestId, reading));
                    break;
            }
        }

        public static Props Props(string tournamentId) =>
    Akka.Actor.Props.Create(() => new TournamentActor(tournamentId))
                    .WithDispatcher("tourney-custom-dispatcher");
    }
}