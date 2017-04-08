using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace HommStat
{
    class Program
    {
        static void Main(string[] args)
        {
            List<Game> games = new List<Game>();
            GamesPage page = null;
            HttpClient httpClient = new HttpClient();
            int currentPage = 0;
            do
            {
                var task =
                    httpClient.GetAsync($"http://homm.ulearn.me/Games/Find?Page={currentPage}").ContinueWith(async t =>
                    {
                        var content = await t.Result.Content.ReadAsStringAsync();
                        page = JsonConvert.DeserializeObject<GamesPage>(content);
                    });
                task.Wait();
                if (page?.games?.Any() == false) break;
                Console.WriteLine($"parsed {page.games.Count} games (page {currentPage})");
                games.AddRange(page.games);
                currentPage++;
            } while (true);

            //var json = JsonConvert.SerializeObject(games);
            //File.WriteAllText("all games.txt", json);

            //var json = File.ReadAllText("all games.txt");
            //games = JsonConvert.DeserializeObject<List<Game>>(json);


            var filteredGames = games
                .Where(x => x.TeamGameResults.Count == 2)
                .Where(x => x.GameName == "homm level3")
                .Where(x => x.TeamGameResults[0].Team.TeamId != x.TeamGameResults[1].Team.TeamId)
                .Where(x => x.TeamGameResults.All(z => z.Results.First(y => y.ScoresType == "Main").Scores > 50))
                .SelectMany(x => new[] {x.TeamGameResults[0], x.TeamGameResults[1]})
                .GroupBy(x => x.Team.TeamId)
                .Select(x => new
                {
                    Id = x.Key,
                    Name = x.First().Team.Name,
                    GamesPlayed = x.Count(),
                    MaxPoints = x.Max(z => z.Results.First(y => y.ScoresType == "Main").Scores),
                    MinPoints = x.Min(z => z.Results.First(y => y.ScoresType == "Main").Scores),
                    AveragePoints = x.Average(z => z.Results.First(y => y.ScoresType == "Main").Scores)
                })
                .OrderByDescending(x => x.AveragePoints)
                .ThenByDescending(x => x.GamesPlayed)
                //.GroupBy(x => x.FirstPlayer.Team.TeamId)
                .ToList();
            Console.WriteLine($"Id\tName\tGamesPlayed\tMaxPoints\tMinPoints\tAveragePoints\t");
            filteredGames.ForEach(x => Console.WriteLine($"{x.Id,3}\t{x.Name,16}\t{x.GamesPlayed,2}\t{x.MaxPoints,3}\t{x.MinPoints,3}\t{x.AveragePoints:000}"));
            Console.ReadLine();
        }
    }


    public class Team
    {
        public int TeamId { get; set; }
        public string Name { get; set; }
        public string OwnerId { get; set; }
        public string CvarcTag { get; set; }
        public object LinkToImage { get; set; }
        public object Members { get; set; }
        public int MaxSize { get; set; }
        public bool CanOwnerLeave { get; set; }
    }

    public class Result
    {
        public int ResultId { get; set; }
        public int Scores { get; set; }
        public string ScoresType { get; set; }
    }

    public class TeamGameResult
    {
        public int TeamGameResultId { get; set; }
        public Team Team { get; set; }
        public string Role { get; set; }
        public List<Result> Results { get; set; }
    }

    public class Game
    {
        public int GameId { get; set; }
        public string GameName { get; set; }
        public object PathToLog { get; set; }
        public List<TeamGameResult> TeamGameResults { get; set; }
    }

    public class GamesPage
    {
        public List<Game> games { get; set; }
        public int total { get; set; }
    }

}
