using System;

namespace TourneyRadarServer.Models
{
    public enum TournamentStatus
    {
        Upcoming,
        Active,
        Completed
    }

    public class Tournament
    {
        public string Id { get; }
        public string Name { get; }
        public DateTime StartDate { get; }
        public DateTime EndDate { get; }
        public TournamentStatus Status { get; }

        public Tournament(string id, string name, DateTime startDate, DateTime endDate, TournamentStatus status)
        {
            Id = id;
            Name = name;
            StartDate = startDate;
            EndDate = endDate;
            Status = status;
        }

        public override string ToString()
        {
            return $"{Name} ({StartDate:dd.MM.yyyy} - {EndDate:dd.MM.yyyy}) [{Status}]";
        }
    }
}