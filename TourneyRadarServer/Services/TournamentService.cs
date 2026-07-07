using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TourneyRadarServer.Models;

namespace TourneyRadarServer.Services
{
    public class TournamentService
    {
        private readonly HttpClient _httpClient = new HttpClient();

        private const string ApiUrl = "https://tourneyradar-api.vercel.app/v1/tournaments?limit=50";

        public IObservable<List<Tournament>> GetTournamentsStream()
        {
            return Observable.Timer(TimeSpan.Zero, TimeSpan.FromSeconds(10))

                .ObserveOn(TaskPoolScheduler.Default)

                .SelectMany(_ => Observable.FromAsync(() => FetchAndMapTournamentsAsync()).Catch((Exception ex) =>
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"[Rx.NET Pipe Error] Recovered from pipeline exception: {ex.Message}");
                        Console.ResetColor();
                        return Observable.Return(new List<Tournament>());
                    }))

                .Where(list => list != null && list.Count > 0);
        }

        private async Task<List<Tournament>> FetchAndMapTournamentsAsync()
        {
            try
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"[Rx.NET] [{DateTime.Now:HH:mm:ss}] Fetching data from API on thread ID: {Environment.CurrentManagedThreadId}...");
                Console.ResetColor();

                using var httpResponse = await _httpClient.GetAsync(ApiUrl);

                if (!httpResponse.IsSuccessStatusCode)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    switch (httpResponse.StatusCode)
                    {
                        case System.Net.HttpStatusCode.BadRequest:
                            Console.WriteLine($"[Rx.NET HTTP Error] Bad Request (400)");
                            break;
                        case System.Net.HttpStatusCode.Unauthorized:
                            Console.WriteLine($"[Rx.NET HTTP Error] Unauthorized (401)");
                            break;
                        case System.Net.HttpStatusCode.Forbidden:
                            Console.WriteLine($"[Rx.NET HTTP Error] Forbidden (403)");
                            break;
                        case System.Net.HttpStatusCode.NotFound:
                            Console.WriteLine($"[Rx.NET HTTP Error] Not Found (404)");
                            break;
                        case System.Net.HttpStatusCode.InternalServerError:
                            Console.WriteLine($"[Rx.NET HTTP Error] Internal Server Error (500)");
                            break;
                        default:
                            Console.WriteLine($"[Rx.NET HTTP Error] Status Code: {(int)httpResponse.StatusCode} - {httpResponse.ReasonPhrase}");
                            break;
                    }
                    Console.ResetColor();
                    return new List<Tournament>();
                }

                var json = await httpResponse.Content.ReadAsStringAsync();

                var response = JsonConvert.DeserializeObject<TournamentApiResponse>(json);

                var result = new List<Tournament>();
                var today = DateTime.Today;

                if (response?.Data != null)
                {
                    foreach (var dto in response.Data)
                    {
                        if (DateTime.TryParse(dto.Date, out DateTime startDate) &&
                            DateTime.TryParse(dto.EndDate, out DateTime endDate))
                        {
                            TournamentStatus status;
                            if (today < startDate.Date)
                                status = TournamentStatus.Upcoming;
                            else if (today > endDate.Date)
                                status = TournamentStatus.Completed;
                            else
                                status = TournamentStatus.Active;

                            result.Add(new Tournament(dto.Id, dto.Name, startDate, endDate, status));
                        }
                    }
                }

                Console.WriteLine($"[Rx.NET] Successfully downloaded and mapped {result.Count} tournaments.");
                return result;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[Rx.NET Error] Failed to fetch data: {ex.Message}");
                Console.ResetColor();
                return new List<Tournament>();
            }
        }
    }
}
