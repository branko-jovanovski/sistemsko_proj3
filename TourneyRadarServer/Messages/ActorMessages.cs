using System.Collections.Generic;
using TourneyRadarServer.Models;

namespace TourneyRadarServer.Messages
{
    public interface ITournamentReading
    {

    }

    public sealed class TournamentReading : ITournamentReading
    {
        public TournamentReading(Tournament tournament)
        {
            Tournament = tournament;
        }
        public Tournament Tournament { get; }
    }

    public sealed class TournamentNotAvailable : ITournamentReading
    {
        public static TournamentNotAvailable Instance { get; } = new();
        private TournamentNotAvailable() { }
    }

    public sealed class TournamentActorNotAvailable : ITournamentReading
    {
        public static TournamentActorNotAvailable Instance { get; } = new();
        private TournamentActorNotAvailable() { }
    }

    public sealed class TournamentTimedOut : ITournamentReading
    {
        public static TournamentTimedOut Instance { get; } = new();
        private TournamentTimedOut() { }
    }


    public sealed class ReadTournament
    {
        public ReadTournament(long requestId)
        {
            RequestId = requestId;
        }

        public long RequestId { get; }
    }

    public sealed class RespondTournament
    {
        public RespondTournament(long requestId, ITournamentReading reading)
        {
            RequestId = requestId;
            Reading = reading;
        }

        public long RequestId { get; }
        public ITournamentReading Reading { get; }
    }

    public sealed class RequestAllTournaments
    {
        public RequestAllTournaments(long requestId)
        {
            RequestId = requestId;
        }

        public long RequestId { get; }
    }

    public sealed class RespondAllTournaments
    {
        public RespondAllTournaments(long requestId, IReadOnlyDictionary<TournamentStatus, IReadOnlyList<Tournament>> groupedTournaments)
        {
            RequestId = requestId;
            GroupedTournaments = groupedTournaments;
        }

        public long RequestId { get; }
        public IReadOnlyDictionary<TournamentStatus, IReadOnlyList<Tournament>> GroupedTournaments { get; }
    }

    public sealed class TrackTournament
    {
        public TrackTournament(long requestId, string tournamentId, Tournament tournament)
        {
            RequestId = requestId;
            TournamentId = tournamentId;
            Tournament = tournament;
        }

        public long RequestId { get; }
        public string TournamentId { get; }
        public Tournament Tournament { get; }
    }

    public sealed class TournamentRegistered
    {
        public TournamentRegistered(long requestId, string tournamentId)
        {
            RequestId = requestId;
            TournamentId = tournamentId;
        }

        public long RequestId { get; }
        public string TournamentId { get; }
    }

    public sealed class UpdateTournaments
    {
        public UpdateTournaments(IReadOnlyList<Tournament> tournaments)
        {
            Tournaments = tournaments;
        }

        public IReadOnlyList<Tournament> Tournaments { get; }
    }

}