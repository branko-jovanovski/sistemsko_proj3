using Akka.Actor;
using Akka.Event;
using System;
using System.Collections.Generic;
using TourneyRadarServer.Messages;
using TourneyRadarServer.Models;

namespace TourneyRadarServer.Actors
{
    public sealed class CollectionTimeout
    {
        public static CollectionTimeout Instance { get; } = new();
        private CollectionTimeout() { }
    }

    public class TournamentQueryActor : UntypedActor
    {
        protected ILoggingAdapter Log { get; } = Context.GetLogger();
        private readonly ICancelable queryTimeoutTimer;
        public Dictionary<IActorRef, string> ActorToTournamentId { get; }
        public long RequestId { get; }
        public IActorRef Requester { get; }
        public TimeSpan Timeout { get; }

        public TournamentQueryActor(Dictionary<IActorRef, string> actorToTournamentId, long requestId, IActorRef requester, TimeSpan timeout)
        {
            ActorToTournamentId = actorToTournamentId;
            RequestId = requestId;
            Requester = requester;
            Timeout = timeout;

            queryTimeoutTimer = Context.System.Scheduler.ScheduleTellOnceCancelable(timeout, Self, CollectionTimeout.Instance, Self);

            Become(WaitingForReplies(new Dictionary<string, ITournamentReading>(), new HashSet<IActorRef>(ActorToTournamentId.Keys)));
        }

        protected override void PreStart()
        {
            foreach (var tournamentActor in ActorToTournamentId.Keys)
            {
                Context.Watch(tournamentActor);
                tournamentActor.Tell(new ReadTournament(0));
            }
        }

        protected override void PostStop() => queryTimeoutTimer.Cancel();

        public UntypedReceive WaitingForReplies(Dictionary<string, ITournamentReading> repliesSoFar, HashSet<IActorRef> stillWaiting)
        {
            return message =>
            {
                switch (message)
                {
                    case RespondTournament response when response.RequestId == 0:
                        ReceivedResponse(Sender, response.Reading, stillWaiting, repliesSoFar);
                        break;
                    case Terminated t:
                        ReceivedResponse(t.ActorRef, TournamentActorNotAvailable.Instance, stillWaiting, repliesSoFar);
                        break;
                    case CollectionTimeout _:
                        var replies = new Dictionary<string, ITournamentReading>(repliesSoFar);
                        foreach (var actor in stillWaiting)
                        {
                            replies.Add(ActorToTournamentId[actor], TournamentTimedOut.Instance);
                        }
                        SendResponseAndStop(replies);
                        break;
                }
            };
        }

        public void ReceivedResponse(IActorRef tournamentActor, ITournamentReading reading, HashSet<IActorRef> stillWaiting, Dictionary<string, ITournamentReading> repliesSoFar)
        {
            Context.Unwatch(tournamentActor);
            stillWaiting.Remove(tournamentActor);
            repliesSoFar.Add(ActorToTournamentId[tournamentActor], reading);

            if (stillWaiting.Count == 0) SendResponseAndStop(repliesSoFar);
            else Context.Become(WaitingForReplies(repliesSoFar, stillWaiting));
        }

        private void SendResponseAndStop(Dictionary<string, ITournamentReading> replies)
        {
            var grouped = new Dictionary<TournamentStatus, List<Tournament>>
    {
        { TournamentStatus.Upcoming, new List<Tournament>() },
        { TournamentStatus.Active, new List<Tournament>() },
        { TournamentStatus.Completed, new List<Tournament>() }
    };

            foreach (var reading in replies.Values)
            {
                if (reading is TournamentReading tr)
                {
                    grouped[tr.Tournament.Status].Add(tr.Tournament);
                }
            }

            var readOnlyGrouped = new Dictionary<TournamentStatus, IReadOnlyList<Tournament>>();
            foreach (var kvp in grouped)
            {
                readOnlyGrouped[kvp.Key] = kvp.Value.AsReadOnly();
            }

            Requester.Tell(new RespondAllTournaments(RequestId, readOnlyGrouped));
            Context.Stop(Self);
        }

        protected override void OnReceive(object message) { }

        public static Props Props(Dictionary<IActorRef, string> actorToTournamentId, long requestId, IActorRef requester, TimeSpan timeout) =>
    Akka.Actor.Props.Create(() => new TournamentQueryActor(actorToTournamentId, requestId, requester, timeout))
                    .WithDispatcher("tourney-custom-dispatcher");
    }
}
