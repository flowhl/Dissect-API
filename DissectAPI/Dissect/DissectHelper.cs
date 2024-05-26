using DissectAPI.Translation;
using Newtonsoft.Json;
using Serilog;
using System.Data;
using System.Diagnostics;
using System.IO;

namespace DissectAPI.Dissect
{
    public static class DissectHelper
    {
        public static Dissect.MatchReplay CurrentReplay { get; set; }

        public static async Task RetrieveReplayAsync(string path)
        {
            CurrentReplay = await GetReplayAsync(path);
        }

        public static void ExportCurrentReplayAsCustomCSV(string path)
        {
            if (path.IsNullOrEmpty()) return;
            if (!Path.GetExtension(path).Contains("csv")) return;
        }

        public static void CleanUpFolders()
        {
            var dir = Directory.GetDirectories("UploadedFiles");
            foreach (var folder in dir)
            {
                Log.Debug("Cleaning up folder: " + folder);
                Directory.Delete(folder, true);
            }
        }

        public static void CleanUpFolder(string path)
        {
            if (Directory.Exists(path))
            {
                Log.Debug("Cleaning up folder: " + path);
                Directory.Delete(path, true);
            }
        }

        public static async Task<Dissect.MatchReplay> GetReplayAsync(string path)
        {
            try
            {
                if (path.IsNullOrEmpty()) return null;
                if (!Directory.Exists(path)) return null;

                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = Globals.DissectPath,
                    //Arguments = $" --version",
                    Arguments = $" \"{path}\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    WorkingDirectory = Path.Combine(Globals.XStratInstallPath, "External"),
                    //WorkingDirectory = "D:\\Github_repos\\xstrat\\Enterprise\\xstrat-client\\xstrat\\bin\\Debug\\External",
                    CreateNoWindow = true
                };
                Process process = new Process();
                process.StartInfo = startInfo;
                process.Start();

                var output = await process.StandardOutput.ReadToEndAsync();

                process.Dispose();

                MatchReplay matchReplay = JsonConvert.DeserializeObject<MatchReplay>(output);
                if (matchReplay != null)
                {
                    //path
                    matchReplay.FolderPath = path;

                    //title
                    string teamA = matchReplay.Rounds.FirstOrDefault()?.Teams[0].Name;
                    string teamB = matchReplay.Rounds.FirstOrDefault()?.Teams[1].Name;
                    string time = matchReplay.Rounds.FirstOrDefault().Timestamp.Date.ToString();
                    //matchReplay.Title = time + "_" + teamA + "_" + teamB;
                    matchReplay.Title = Path.GetFileName(path);

                    //mapping fields
                    foreach (var round in matchReplay.Rounds)
                    {
                        try
                        {
                            round.RoundNr = round.RoundNumber + 1;
                            round.MapName = round.Map.Name;
                            round.TeamA = round.Teams[0].Name;
                            round.RoundScoreTeamA = round.Teams[0].Score;
                            round.TeamA_Role = round.Teams[0].Role;
                            round.TeamA_Player1_Pick = round.Players.Where(x => x.TeamIndex == 0).ToArray()[0].Operator.Name;
                            round.TeamA_Player2_Pick = round.Players.Where(x => x.TeamIndex == 0).ToArray()[1].Operator.Name;
                            round.TeamA_Player3_Pick = round.Players.Where(x => x.TeamIndex == 0).ToArray()[2].Operator.Name;
                            round.TeamA_Player4_Pick = round.Players.Where(x => x.TeamIndex == 0).ToArray()[3].Operator.Name;
                            round.TeamA_Player5_Pick = round.Players.Where(x => x.TeamIndex == 0).ToArray()[4].Operator.Name;
                            round.TeamA_Player1_Name = round.Players.Where(x => x.TeamIndex == 0).ToArray()[0].Username;
                            round.TeamA_Player2_Name = round.Players.Where(x => x.TeamIndex == 0).ToArray()[1].Username;
                            round.TeamA_Player3_Name = round.Players.Where(x => x.TeamIndex == 0).ToArray()[2].Username;
                            round.TeamA_Player4_Name = round.Players.Where(x => x.TeamIndex == 0).ToArray()[3].Username;
                            round.TeamA_Player5_Name = round.Players.Where(x => x.TeamIndex == 0).ToArray()[4].Username;
                            round.TeamB = round.Teams[1].Name;
                            round.RoundScoreTeamB = round.Teams[1].Score;
                            round.TeamB_Role = round.Teams[1].Role;
                            round.TeamB_Player1_Pick = round.Players.Where(x => x.TeamIndex == 1).ToArray()[0].Operator.Name;
                            round.TeamB_Player2_Pick = round.Players.Where(x => x.TeamIndex == 1).ToArray()[1].Operator.Name;
                            round.TeamB_Player3_Pick = round.Players.Where(x => x.TeamIndex == 1).ToArray()[2].Operator.Name;
                            round.TeamB_Player4_Pick = round.Players.Where(x => x.TeamIndex == 1).ToArray()[3].Operator.Name;
                            round.TeamB_Player5_Pick = round.Players.Where(x => x.TeamIndex == 1).ToArray()[4].Operator.Name;
                            round.TeamB_Player1_Name = round.Players.Where(x => x.TeamIndex == 1).ToArray()[0].Username;
                            round.TeamB_Player2_Name = round.Players.Where(x => x.TeamIndex == 1).ToArray()[1].Username;
                            round.TeamB_Player3_Name = round.Players.Where(x => x.TeamIndex == 1).ToArray()[2].Username;
                            round.TeamB_Player4_Name = round.Players.Where(x => x.TeamIndex == 1).ToArray()[3].Username;
                            round.TeamB_Player5_Name = round.Players.Where(x => x.TeamIndex == 1).ToArray()[4].Username;

                            var index = matchReplay.Rounds.IndexOf(round);
                            Dissect.Round previous = null;
                            if (index > 0)
                            {
                                previous = matchReplay.Rounds[index - 1];
                            }
                            SetWinCondition(round, previous);
                        }
                        catch (Exception ex)
                        {
                            Log.Error($"Could not Map field of replay: {path}, round: {round.RoundNr}, error: {ex.Message}", ex);
                            throw ex;
                        }
                    }
                }
                return matchReplay;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message + ex.StackTrace);
                throw ex;
            }
        }

        public static void ExportReplay(string path, string exportFile)
        {
            try
            {
                if (path.IsNullOrEmpty())
                {
                    Log.Error("Error: replay path cannot be empty");
                    throw new Exception("Error: replay path cannot be empty");
                    return;
                }
                if (exportFile.IsNullOrEmpty())
                {
                    Log.Error("Error: export path cannot be empty");
                    throw new Exception("Error: export path cannot be empty");
                    return;
                }

                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = Globals.DissectPath,
                    Arguments = $" \"{path}\" -x {Path.GetFileName(exportFile)}",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    WorkingDirectory = Path.GetDirectoryName(exportFile),
                    CreateNoWindow = true
                };
                Process process = new Process();
                process.StartInfo = startInfo;
                process.Start();
                process.WaitForExit();
                if (process.ExitCode != 0)
                {
                    Log.Debug($"Error exporting file: {process.StandardOutput}");
                    throw new Exception($"Error exporting file: {process.StandardOutput}");
                    return;
                }
                Log.Debug($"Exported into {exportFile}");

            }
            catch (Exception ex)
            {
                Log.Error(ex.Message + ex.StackTrace);
                throw ex;
            }
        }

        public static void SetWinCondition(Dissect.Round round, Dissect.Round previousRound)
        {
            //manually fix won flag
            //first round
            //FW: 22.03.2024: this is not needed anymore, as the dissect tool now sets the won flag correctly
            //if(previousRound == null)
            //{
            //    int AScore = round.Teams[0].Score;
            //    int BScore = round.Teams[1].Score;

            //    if(AScore > BScore)
            //    {
            //        round.Teams[0].Won = true;
            //        round.Teams[1].Won = false;
            //    }
            //    else
            //    {
            //        round.Teams[0].Won = false;
            //        round.Teams[1].Won = true;
            //    }
            //}
            ////other rounds
            //else
            //{
            //    int AScorediff = round.Teams[0].Score - previousRound.Teams[0].Score;
            //    int BScorediff = round.Teams[1].Score - previousRound.Teams[1].Score;

            //    if( AScorediff > BScorediff )
            //    {
            //        round.Teams[0].Won = true;
            //        round.Teams[1].Won = false;
            //    }
            //    else
            //    {
            //        round.Teams[0].Won = false;
            //        round.Teams[1].Won = true;
            //    }
            //}

            //set left players dead manually
            var leftPlayers = round.MatchFeedback.Where(x => x.Type.Name == "PlayerLeave").Select(x => x.Username).ToList();
            foreach (var leftPlayer in leftPlayers)
            {
                var statrow = round.Stats.Where(x => x.Username == leftPlayer).FirstOrDefault();
                if (statrow != null) statrow.Died = true;
            }

            string winConditionA = "";
            string winConditionB = "";

            //team A
            string prefixA;
            string prefixB;
            if (round.Teams.First().Won)
            {
                prefixA = "Win by ";
                prefixB = "Lose by ";
            }
            else
            {
                prefixA = "Lose by ";
                prefixB = "Win by ";
            }

            var winningTeam = round.Teams.Where(x => x.Won).FirstOrDefault();
            string condition = "";

            Dissect.Team attTeam = round.Teams.Where(x => x.Role == "Attack").First();
            int attTeamIndex = round.Teams.IndexOf(attTeam);

            var attTeamPlayers = round.Players.Where(x => x.TeamIndex == attTeamIndex);
            var defTeamPlayers = round.Players.Where(x => x.TeamIndex != attTeamIndex);
            var attTeamStats = round.Stats.Where(x => attTeamPlayers.Any(y => y.Username == x.Username));
            var defTeamStats = round.Stats.Where(x => defTeamPlayers.Any(y => y.Username == x.Username));

            bool attTeamAllDied = !attTeamStats.Any(x => x.Died == false);
            bool defTeamAllDied = !defTeamStats.Any(x => x.Died == false);

            bool plantStarted = round.MatchFeedback.Any(x => x.Type.Name == "DefuserPlantStart");
            bool plantComplete = round.MatchFeedback.Any(x => x.Type.Name == "DefuserPlantComplete");
            bool disableStarted = round.MatchFeedback.Any(x => x.Type.Name == "DefuserDisableStart");
            bool disableComplete = round.MatchFeedback.Any(x => x.Type.Name == "DefuserDisableComplete");


            int attTeamKills = attTeamStats.Sum(x => x.Kills);
            int defTeamKills = defTeamStats.Sum(x => x.Kills);

            if (plantComplete)
            {
                condition = "Disable";

                ////do wincheck manually as dissect is currently wrong:
                ////def team won
                //if (disableComplete)
                //{
                //    if (attTeamIndex == 0)
                //    {
                //        prefixA = "Lose by ";
                //        prefixB = "Win by ";
                //    }
                //    else
                //    {
                //        prefixA = "Win by ";
                //        prefixB = "Lose by ";
                //    }
                //}
                ////att team won
                //else
                //{
                //    if (attTeamIndex == 0)
                //    {
                //        prefixA = "Win by ";
                //        prefixB = "Lose by ";
                //    }
                //    else
                //    {
                //        prefixA = "Lose by ";
                //        prefixB = "Win by ";
                //    }

                //}
            }
            else if (attTeamAllDied || defTeamAllDied)
            {
                condition = "Kills";
            }
            else
            {
                condition = "Time";
            }

            winConditionA = prefixA + condition;
            winConditionB = prefixB + condition;

            round.TeamA_WinCondition = winConditionA;
            round.TeamB_WinCondition = winConditionB;
        }

        public static bool ContainsReplayFiles(string path)
        {
            if (path.IsNullOrEmpty()) throw new ArgumentNullException("path");
            if (!Directory.Exists(path)) throw new DirectoryNotFoundException(path);

            var files = Directory.GetFiles(path, "*.rec");
            return files.Count() > 0;
        }

        public static string GetDissectCSV(MatchReplay replay)
        {
            //translate fields + order players
            ApplyTranslation(replay);

            // Define the data to write in CSV format

            var dt = new DataTable();
            dt.Columns.Add("RoundScoreTeamA");
            dt.Columns.Add("TeamA");
            dt.Columns.Add("Round1");
            dt.Columns.Add("RoleTeamA");
            dt.Columns.Add("WinConditionTeamA");
            dt.Columns.Add("BombSite1");
            dt.Columns.Add("TeamA_Player1_Pick");
            dt.Columns.Add("TeamA_Player2_Pick");
            dt.Columns.Add("TeamA_Player3_Pick");
            dt.Columns.Add("TeamA_Player4_Pick");
            dt.Columns.Add("TeamA_Player5_Pick");
            dt.Columns.Add("TeamA_Player1_Name");
            dt.Columns.Add("TeamA_Player2_Name");
            dt.Columns.Add("TeamA_Player3_Name");
            dt.Columns.Add("TeamA_Player4_Name");
            dt.Columns.Add("TeamA_Player5_Name");

            //split
            dt.Columns.Add("");

            dt.Columns.Add("RoundScoreTeamB");
            dt.Columns.Add("TeamB");
            dt.Columns.Add("Round2");
            dt.Columns.Add("RoleTeamB");
            dt.Columns.Add("WinConditionTeamB");
            dt.Columns.Add("BombSite2");
            dt.Columns.Add("TeamB_Player1_Pick");
            dt.Columns.Add("TeamB_Player2_Pick");
            dt.Columns.Add("TeamB_Player3_Pick");
            dt.Columns.Add("TeamB_Player4_Pick");
            dt.Columns.Add("TeamB_Player5_Pick");
            dt.Columns.Add("TeamB_Player1_Name");
            dt.Columns.Add("TeamB_Player2_Name");
            dt.Columns.Add("TeamB_Player3_Name");
            dt.Columns.Add("TeamB_Player4_Name");
            dt.Columns.Add("TeamB_Player5_Name");

            foreach (var round in replay.Rounds)
            {
                dt.Rows.Add(new string[]
                {
                    round.RoundScoreTeamA.ToString(),
                    round.Translated_TeamA,
                    round.RoundNr.ToString(),
                    round.Translated_TeamA_Role,
                    round.Translated_TeamA_WinCondition,
                    round.Translated_Site,
                    round.Ordered_TeamA_Player1_Pick,
                    round.Ordered_TeamA_Player2_Pick,
                    round.Ordered_TeamA_Player3_Pick,
                    round.Ordered_TeamA_Player4_Pick,
                    round.Ordered_TeamA_Player5_Pick,
                    round.Ordered_TeamA_Player1_Name,
                    round.Ordered_TeamA_Player2_Name,
                    round.Ordered_TeamA_Player3_Name,
                    round.Ordered_TeamA_Player4_Name,
                    round.Ordered_TeamA_Player5_Name,
                    "",
                    round.RoundScoreTeamB.ToString(),
                    round.Translated_TeamB,
                    round.RoundNr.ToString(),
                    round.Translated_TeamB_Role,
                    round.Translated_TeamB_WinCondition,
                    round.Translated_Site,
                    round.Ordered_TeamB_Player1_Pick,
                    round.Ordered_TeamB_Player2_Pick,
                    round.Ordered_TeamB_Player3_Pick,
                    round.Ordered_TeamB_Player4_Pick,
                    round.Ordered_TeamB_Player5_Pick,
                    round.Ordered_TeamB_Player1_Name,
                    round.Ordered_TeamB_Player2_Name,
                    round.Ordered_TeamB_Player3_Name,
                    round.Ordered_TeamB_Player4_Name,
                    round.Ordered_TeamB_Player5_Name
                });
            }

            //Save File
            return CSVHelper.ConvertDatatableToCSV(dt);
        }

        public static Dictionary<string, string> GetDissectColumnTitles()
        {
            var dict = new Dictionary<string, string>();
            dict.Add("RoundScoreTeamA", "Score Team A");
            dict.Add("TeamA", "Team A");
            dict.Add("Round1", "Round Number A");
            dict.Add("RoleTeamA", "Role Team A");
            dict.Add("WinConditionTeamA", "Win Condition Team A");
            dict.Add("BombSite1", "Bomb Site");
            dict.Add("TeamA_Player1_Pick", "Team A Player 1 Pick");
            dict.Add("TeamA_Player2_Pick", "Team A Player 2 Pick");
            dict.Add("TeamA_Player3_Pick", "Team A Player 3 Pick");
            dict.Add("TeamA_Player4_Pick", "Team A Player 4 Pick");
            dict.Add("TeamA_Player5_Pick", "Team A Player 5 Pick");
            dict.Add("TeamA_Player1_Name", "Team A Player 1 Name");
            dict.Add("TeamA_Player2_Name", "Team A Player 2 Name");
            dict.Add("TeamA_Player3_Name", "Team A Player 3 Name");
            dict.Add("TeamA_Player4_Name", "Team A Player 4 Name");
            dict.Add("TeamA_Player5_Name", "Team A Player 5 Name");
            dict.Add("", "Divider");
            dict.Add("RoundScoreTeamB", "Score Team B");
            dict.Add("TeamB", "Team B");
            dict.Add("Round2", "Round Number B");
            dict.Add("RoleTeamB", "Role Team B");
            dict.Add("WinConditionTeamB", "Win Condition Team B");
            dict.Add("BombSite2", "Bomb Site");
            dict.Add("TeamB_Player1_Pick", "Team B Player 1 Pick");
            dict.Add("TeamB_Player2_Pick", "Team B Player 2 Pick");
            dict.Add("TeamB_Player3_Pick", "Team B Player 3 Pick");
            dict.Add("TeamB_Player4_Pick", "Team B Player 4 Pick");
            dict.Add("TeamB_Player5_Pick", "Team B Player 5 Pick");
            dict.Add("TeamB_Player1_Name", "Team B Player 1 Name");
            dict.Add("TeamB_Player2_Name", "Team B Player 2 Name");
            dict.Add("TeamB_Player3_Name", "Team B Player 3 Name");
            dict.Add("TeamB_Player4_Name", "Team B Player 4 Name");
            dict.Add("TeamB_Player5_Name", "Team B Player 5 Name");
            return dict;
        }

        private static void ApplyTranslation(MatchReplay replay)
        {
            if (replay == null || replay.Rounds == null) return;
            replay.Rounds.ForEach(x => Translate(x));
        }

        private static void Translate(Dissect.Round round)
        {
            //TODO: Translate fields
            round.Translated_TeamA = TranslationHandler.DissectTranslate(round.TeamA);
            round.Translated_TeamB = TranslationHandler.DissectTranslate(round.TeamB);
            round.Translated_MapName = TranslationHandler.DissectTranslate(round.Map.Name);
            round.Translated_Site = TranslationHandler.DissectTranslate(round.Site);
            round.Translated_TeamA_WinCondition = TranslationHandler.DissectTranslate(round.TeamA_WinCondition);
            round.Translated_TeamB_WinCondition = TranslationHandler.DissectTranslate(round.TeamB_WinCondition);
            round.Translated_TeamA_Role = TranslationHandler.DissectTranslate(round.TeamA_Role);
            round.Translated_TeamB_Role = TranslationHandler.DissectTranslate(round.TeamB_Role);

            round.Translated_TeamA_Player1_Pick = TranslationHandler.DissectTranslate(round.TeamA_Player1_Pick);
            round.Translated_TeamA_Player2_Pick = TranslationHandler.DissectTranslate(round.TeamA_Player2_Pick);
            round.Translated_TeamA_Player3_Pick = TranslationHandler.DissectTranslate(round.TeamA_Player3_Pick);
            round.Translated_TeamA_Player4_Pick = TranslationHandler.DissectTranslate(round.TeamA_Player4_Pick);
            round.Translated_TeamA_Player5_Pick = TranslationHandler.DissectTranslate(round.TeamA_Player5_Pick);
            round.Translated_TeamB_Player1_Pick = TranslationHandler.DissectTranslate(round.TeamB_Player1_Pick);
            round.Translated_TeamB_Player2_Pick = TranslationHandler.DissectTranslate(round.TeamB_Player2_Pick);
            round.Translated_TeamB_Player3_Pick = TranslationHandler.DissectTranslate(round.TeamB_Player3_Pick);
            round.Translated_TeamB_Player4_Pick = TranslationHandler.DissectTranslate(round.TeamB_Player4_Pick);
            round.Translated_TeamB_Player5_Pick = TranslationHandler.DissectTranslate(round.TeamB_Player5_Pick);

            round.Translated_TeamA_Player1_Name = TranslationHandler.DissectTranslate(round.TeamA_Player1_Name);
            round.Translated_TeamA_Player2_Name = TranslationHandler.DissectTranslate(round.TeamA_Player2_Name);
            round.Translated_TeamA_Player3_Name = TranslationHandler.DissectTranslate(round.TeamA_Player3_Name);
            round.Translated_TeamA_Player4_Name = TranslationHandler.DissectTranslate(round.TeamA_Player4_Name);
            round.Translated_TeamA_Player5_Name = TranslationHandler.DissectTranslate(round.TeamA_Player5_Name);
            round.Translated_TeamB_Player1_Name = TranslationHandler.DissectTranslate(round.TeamB_Player1_Name);
            round.Translated_TeamB_Player2_Name = TranslationHandler.DissectTranslate(round.TeamB_Player2_Name);
            round.Translated_TeamB_Player3_Name = TranslationHandler.DissectTranslate(round.TeamB_Player3_Name);
            round.Translated_TeamB_Player4_Name = TranslationHandler.DissectTranslate(round.TeamB_Player4_Name);
            round.Translated_TeamB_Player5_Name = TranslationHandler.DissectTranslate(round.TeamB_Player5_Name);



            #region Order Players Alphabetically

            List<Tuple<string, string>> TeamAPlayers = new List<Tuple<string, string>>();
            List<Tuple<string, string>> TeamBPlayers = new List<Tuple<string, string>>();

            TeamAPlayers.Add(new Tuple<string, string>(round.Translated_TeamA_Player1_Name ?? "", round.Translated_TeamA_Player1_Pick ?? ""));
            TeamAPlayers.Add(new Tuple<string, string>(round.Translated_TeamA_Player2_Name ?? "", round.Translated_TeamA_Player2_Pick ?? ""));
            TeamAPlayers.Add(new Tuple<string, string>(round.Translated_TeamA_Player3_Name ?? "", round.Translated_TeamA_Player3_Pick ?? ""));
            TeamAPlayers.Add(new Tuple<string, string>(round.Translated_TeamA_Player4_Name ?? "", round.Translated_TeamA_Player4_Pick ?? ""));
            TeamAPlayers.Add(new Tuple<string, string>(round.Translated_TeamA_Player5_Name ?? "", round.Translated_TeamA_Player5_Pick ?? ""));

            TeamBPlayers.Add(new Tuple<string, string>(round.Translated_TeamB_Player1_Name ?? "", round.Translated_TeamB_Player1_Pick ?? ""));
            TeamBPlayers.Add(new Tuple<string, string>(round.Translated_TeamB_Player2_Name ?? "", round.Translated_TeamB_Player2_Pick ?? ""));
            TeamBPlayers.Add(new Tuple<string, string>(round.Translated_TeamB_Player3_Name ?? "", round.Translated_TeamB_Player3_Pick ?? ""));
            TeamBPlayers.Add(new Tuple<string, string>(round.Translated_TeamB_Player4_Name ?? "", round.Translated_TeamB_Player4_Pick ?? ""));
            TeamBPlayers.Add(new Tuple<string, string>(round.Translated_TeamB_Player5_Name ?? "", round.Translated_TeamB_Player5_Pick ?? ""));

            List<Tuple<string, string>> OrderedTeamAPlayers = TeamAPlayers.OrderBy(t => t.Item1).ToList();
            List<Tuple<string, string>> OrderedTeamBPlayers = TeamBPlayers.OrderBy(t => t.Item1).ToList();

            round.Ordered_TeamA_Player1_Name = OrderedTeamAPlayers[0].Item1;
            round.Ordered_TeamA_Player1_Pick = OrderedTeamAPlayers[0].Item2;

            round.Ordered_TeamA_Player2_Name = OrderedTeamAPlayers[1].Item1;
            round.Ordered_TeamA_Player2_Pick = OrderedTeamAPlayers[1].Item2;

            round.Ordered_TeamA_Player3_Name = OrderedTeamAPlayers[2].Item1;
            round.Ordered_TeamA_Player3_Pick = OrderedTeamAPlayers[2].Item2;

            round.Ordered_TeamA_Player4_Name = OrderedTeamAPlayers[3].Item1;
            round.Ordered_TeamA_Player4_Pick = OrderedTeamAPlayers[3].Item2;

            round.Ordered_TeamA_Player5_Name = OrderedTeamAPlayers[4].Item1;
            round.Ordered_TeamA_Player5_Pick = OrderedTeamAPlayers[4].Item2;


            round.Ordered_TeamB_Player1_Name = OrderedTeamBPlayers[0].Item1;
            round.Ordered_TeamB_Player1_Pick = OrderedTeamBPlayers[0].Item2;

            round.Ordered_TeamB_Player2_Name = OrderedTeamBPlayers[1].Item1;
            round.Ordered_TeamB_Player2_Pick = OrderedTeamBPlayers[1].Item2;

            round.Ordered_TeamB_Player3_Name = OrderedTeamBPlayers[2].Item1;
            round.Ordered_TeamB_Player3_Pick = OrderedTeamBPlayers[2].Item2;

            round.Ordered_TeamB_Player4_Name = OrderedTeamBPlayers[3].Item1;
            round.Ordered_TeamB_Player4_Pick = OrderedTeamBPlayers[3].Item2;

            round.Ordered_TeamB_Player5_Name = OrderedTeamBPlayers[4].Item1;
            round.Ordered_TeamB_Player5_Pick = OrderedTeamBPlayers[4].Item2;
            #endregion
        }
    }
}
