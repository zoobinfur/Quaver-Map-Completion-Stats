using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Quaver_Map_Completion
{
    internal class Program
    {
        static double MaxDifficulty = 50;
        static double DifficultyRangeWidth = 1;
        static readonly HttpClient client = new HttpClient();
        struct Map
        {
            public string Id; //Is actually the MD5 hash of the map, but serves the same purpose as an ID
            public string Title;
            public double Difficulty;

            public Map(string id, string title, double difficulty)
            {
                Id = id;
                Title = title;
                Difficulty = difficulty;
            }
        }
        static async Task<string> CallAPI(string url)
        {
            try
            {
                HttpResponseMessage response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();
                return responseBody;
            }
            catch (HttpRequestException e)
            {
                if (e.Message.Contains("429"))
                {
                    Console.WriteLine("\nAPI Rate limit reached.");
                }
                else
                {
                    Console.WriteLine("\nException Caught!");
                    Console.WriteLine("Message: {0} ", e.Message);
                }

                int timer = 30;
                while (timer > 0)
                {
                    Console.SetCursorPosition(0, Console.CursorTop);
                    Console.Write("Waiting " + timer + " seconds before retrying...   ");
                    Thread.Sleep(1000);
                    timer--;
                } Console.WriteLine();
                return await CallAPI(url);
            }
        }
        static string ParseString(string json, string target)
        {
            int targetIndex = json.IndexOf("\"" + target + "\":"); // Find location of target data
            json = json.Substring(targetIndex + target.Length + 3); // Remove everything before the target
            int endIndex = json.IndexOf(","); // Find end of target data (won't work if the target is the last line and hence doesn't have a comma at the end)
            string data = json.Substring(0, endIndex + 1).Replace("\"", "").Replace(",", ""); // Remove any quotes and commas
            return data;
        }
        static int ParseInt(string json, string target)
        {
            int targetIndex = json.IndexOf("\"" + target + "\":"); // Find location of target data
            json = json.Substring(targetIndex + target.Length + 3); // Remove everything before the target
            int endIndex = json.IndexOf(","); // Find end of target data (won't work if the target is the last line and hence doesn't have a comma at the end)
            int data = int.Parse(json.Substring(0, endIndex));
            return data;
        }
        static double ParseDouble(string json, string target)
        {
            int targetIndex = json.IndexOf("\"" + target + "\":"); // Find location of target data
            json = json.Substring(targetIndex + target.Length + 3); // Remove everything before the target
            int endIndex = json.IndexOf(","); // Find end of target data (won't work if the target is the last line and hence doesn't have a comma at the end)
            double data = double.Parse(json.Substring(0, endIndex));
            return data;
        }
        static string RequestUserID()
        {
            Console.WriteLine("Enter User ID: ");
            Console.Write("(e.g. in 'https://quavergame.com/user/1241', '1241' is the user ID for Zoobin4)");
            Console.SetCursorPosition(15, Console.CursorTop - 1);

            string user_id = Console.ReadLine(); // Too lazy to check if the user ID is valid
            Console.SetCursorPosition(0, Console.CursorTop + 1);
            return user_id;
        }
        static async Task<string> GetUserInfo(string user_id)
        {
            string url = "https://api.quavergame.com/v2/user/" + user_id;
            return await CallAPI(url);
        }
        static async Task DisplayUserStats(string user_id)
        {
            string user_info = await GetUserInfo(user_id);
            string username = ParseString(user_info, "username");
            int rank = ParseInt(user_info, "global");

            Console.WriteLine("\nUser Selected: " + username);
            Console.WriteLine("Rank: " + rank + "\n");
        }
        static async Task Main()
        {
            string user_id = RequestUserID();
            await DisplayUserStats(user_id);

            List<Map> maps = await GetMapList();

            // Most definitely does not guarantee no duplicate maps, but I am too lazy to change the array to a set
            CheckForDuplicateMaps(maps);

            Console.WriteLine("\n" + maps.Count + " total maps\n" +
                "Estimated time to complete: " + (maps.Count / 100) + " minutes");
            await GetScoresAsync(maps, user_id);

            Console.WriteLine("\n\nPress ESCAPE to exit... ");
            while (Console.ReadKey().Key != ConsoleKey.Escape) { }
        }
        static bool CheckForDuplicateMaps(List<Map> maps)
        {
            bool hasDuplicateMaps = false;
            for (int i = 1; i < maps.Count; i++)
            {
                if (maps[i].Id == maps[i - 1].Id)
                {
                    Console.WriteLine("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA DUPLICATE MAP AAAAAAAAAAAAAAAAAAAAAAAAAAA" +
                    maps[i].Difficulty);
                    hasDuplicateMaps = true;
                }
            }
            return hasDuplicateMaps;
        }
        static async Task<List<Map>> GetMapList()
        {
            List<Map> maps = new List<Map>();
            double difficultyIncrement = 0.1;
            double min_difficulty_rating = 0;
            double max_difficulty_rating = min_difficulty_rating + difficultyIncrement;

            while (min_difficulty_rating < MaxDifficulty)
            {
                string url = "https://api.quavergame.com/v2/mapset/search?ranked_status=2&mode=1&" +
                    "min_difficulty_rating=" + Math.Round(min_difficulty_rating, 2) +
                    "&max_difficulty_rating=" + Math.Round(max_difficulty_rating, 2);

                string map_info = await CallAPI(url);
                List<Map> partialMaps = ParseMaps(map_info);
                if (partialMaps.Count >= 49)
                {
                    difficultyIncrement *= 0.5;
                    max_difficulty_rating -= difficultyIncrement;
                    continue;
                }

                maps.AddRange(partialMaps);

                Console.WriteLine("Difficulty " +
                    min_difficulty_rating.ToString("0.##") + "-" +
                    max_difficulty_rating.ToString("0.##") + ": " +
                    partialMaps.Count + " maps");

                min_difficulty_rating = max_difficulty_rating;
                max_difficulty_rating += difficultyIncrement;

                if (partialMaps.Count <= 24) 
                { difficultyIncrement *= 1.5; }
            }

            return maps;
        }
        static List<Map> ParseMaps(string json)
        {
            List<Map> maps = new List<Map>();
            int md5Index = json.IndexOf("\"md5\":");
            while (md5Index != -1)
            {
                json = json.Substring(md5Index + 6);
                int endIndex = json.IndexOf("\",");
                string mapId = json.Substring(1, endIndex - 1);

                string mapTitle = ParseString(json, "title");
                double mapDifficulty = ParseDouble(json, "difficulty_rating");

                maps.Add(new Map(mapId, mapTitle, mapDifficulty));

                md5Index = json.IndexOf("\"md5\":");
            }
            return maps;
        }

        static async Task GetScoresAsync(List<Map> maps, string user_id)
        {
            List<Map> completedMaps = new List<Map>();
            List<Map> incompleteMaps = new List<Map>();
            int index = 0;

            while (index < maps.Count)
            {
                Map map = maps[index];
                string url = "https://api.quavergame.com/v2/scores/" + map.Id + "/" + user_id + "/global";
                string score_info = await CallAPI(url);

                if (score_info.IndexOf("\"id\":") == -1) // If score has no map ID (i.e. because the score does not exist)
                { incompleteMaps.Add(map); }
                else
                { completedMaps.Add(map); }

                Console.WriteLine(completedMaps.Count + "/" + (completedMaps.Count + incompleteMaps.Count));
                index++;
            }

            PrintStats(completedMaps, incompleteMaps);
        }

        static void PrintStats(List<Map> completedMaps, List<Map> incompleteMaps)
        {
            int totalMapCount = completedMaps.Count + incompleteMaps.Count;
            Console.WriteLine("Completed " + completedMaps.Count + "/" + totalMapCount +
                " maps (" + (100 * (float)completedMaps.Count / totalMapCount).ToString("0.##") + "%)");

            List<Map>[] completedDifficulties = SeparateDifficultyRanges(completedMaps);
            List<Map>[] incompleteDifficulties = SeparateDifficultyRanges(incompleteMaps);

            for (int i = 0; i < completedDifficulties.Length; i++)
            {
                int total = completedDifficulties[i].Count + incompleteDifficulties[i].Count;
                double percent = 100 * (double)completedDifficulties[i].Count / total;
                double difficulty = i * DifficultyRangeWidth;

                Console.Write(difficulty.ToString("0.##") + " - " +
                    (difficulty + DifficultyRangeWidth).ToString("0.##") +
                    ": " + completedDifficulties[i].Count + "/" +
                    total + " (");
                SetConsoleColour(percent);
                Console.Write(percent.ToString("0.##") + "%");
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine(")");

                if (incompleteDifficulties[i].Count != 0 && incompleteDifficulties[i].Count <= 5)
                {
                    for (int n = 0; n < incompleteDifficulties[i].Count; n++)
                    {
                        Console.WriteLine("   " + incompleteDifficulties[i][n].Title +
                            ", (" + incompleteDifficulties[i][n].Difficulty.ToString("0.##") + ")");
                    }
                }
            }
        }

        public static void SetConsoleColour(double percent)
        {
            ConsoleColor colour = ConsoleColor.Blue; // 100%
            if (percent < 100) // 80-100%
            { colour = ConsoleColor.Green; }
            if (percent < 80) // 60-80%
            { colour = ConsoleColor.Yellow; }
            if (percent < 50) // 25-50%
            { colour = ConsoleColor.DarkYellow; }
            if (percent < 25) // 0-25%
            { colour = ConsoleColor.Red; }
            if (percent == 0) // 0%
            { colour = ConsoleColor.DarkRed; }
            if (double.IsNaN(percent)) // No maps in range
            { colour = ConsoleColor.DarkGray; }

            Console.ForegroundColor = colour;
        }

        static List<Map>[] SeparateDifficultyRanges(List<Map> maps)
        {
            int totalDifficultyRanges = (int)Math.Ceiling(MaxDifficulty / DifficultyRangeWidth);
            List<Map>[] separatedMaps = new List<Map>[totalDifficultyRanges];
            InitialiseMapListArray(ref separatedMaps);

            foreach (Map map in maps)
            {
                int difficultyRangeIndex = (int)Math.Ceiling(map.Difficulty / DifficultyRangeWidth) - 1;
                separatedMaps[difficultyRangeIndex].Add(map);
            }
            return separatedMaps;
        }

        static void InitialiseMapListArray(ref List<Map>[] mapListArray)
        {
            for (int i = 0; i < mapListArray.Length; i++)
            { mapListArray[i] = new List<Map>(); }
        }
    }
}
