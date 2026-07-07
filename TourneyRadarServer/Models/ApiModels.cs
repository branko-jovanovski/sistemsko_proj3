using Newtonsoft.Json;
using System.Collections.Generic;

namespace TourneyRadarServer.Models
{
    public class TournamentApiResponse
    {
        [JsonProperty("data")]
        public List<TournamentDto> Data { get; set; } = new List<TournamentDto>();
    }

    public class TournamentDto
    {
        [JsonProperty("id")]
        public string Id { get; set; } = null!;

        [JsonProperty("name")]
        public string Name { get; set; } = null!;

        [JsonProperty("date")]
        public string Date { get; set; } = null!;

        [JsonProperty("end_date")]
        public string EndDate { get; set; } = null!;
    }
}