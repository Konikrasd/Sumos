using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using Sumos.Models;

namespace Sumos.Services
{
    /// <summary>
    /// Сервис, хранящий состояние турнира и реализующий бизнес-логику.
    /// Не зависит от WPF-контролов.
    /// </summary>
    public class TournamentService
    {
        private const string SaveFileName = "tournament.json";

        public ObservableCollection<Team> Teams { get; private set; }
        public ObservableCollection<Match> Matches { get; private set; }
        public ObservableCollection<RatingRow> Rating { get; private set; }
        public ObservableCollection<MatchReportRow> Reports { get; private set; }

        public TournamentService()
        {
            Teams = new ObservableCollection<Team>();
            Matches = new ObservableCollection<Match>();
            Rating = new ObservableCollection<RatingRow>();
            Reports = new ObservableCollection<MatchReportRow>();

            LoadStateIfExists();
            RefreshAll();
        }

        public Team CreateTeam(string name, string participant, string constructor, int weight)
        {
            var team = new Team
            {
                Id = Guid.NewGuid(),
                Name = name,
                Participant = participant,
                RobotWeight = weight,
                Constructor = string.IsNullOrWhiteSpace(constructor) ? "Другое" : constructor
            };

            Teams.Add(team);
            RefreshAll();
            return team;
        }

        public void UpdateTeam(Team team, string name, string participant, string constructor, int weight,
            bool isDisqualified = false, string disqualificationReason = null)
        {
            team.Name = name;
            team.Participant = participant;
            team.RobotWeight = weight;
            team.Constructor = string.IsNullOrWhiteSpace(constructor) ? "Другое" : constructor;
            team.IsDisqualified = isDisqualified;
            team.DisqualificationReason = isDisqualified ? (disqualificationReason ?? string.Empty) : string.Empty;
            RefreshAll();
        }

        public void DeleteTeam(Team team)
        {
            var toRemove = Matches.Where(m => m.TeamAId == team.Id || m.TeamBId == team.Id).ToList();
            foreach (var m in toRemove)
            {
                Matches.Remove(m);
            }

            Teams.Remove(team);
            RefreshAll();
        }

        /// <summary>
        /// Генерирует календарь по турам: в каждом туре все команды играют по одному матчу
        /// (в туре матчей = половина от числа команд), за все туры каждая с каждой встречается ровно раз (круговая ротация).
        /// </summary>
        public void GenerateSchedule()
        {
            Matches.Clear();

            var activeTeams = Teams.Where(t => !t.IsDisqualified).ToList();
            int N = activeTeams.Count;
            if (N < 2) return;

            int numRounds;
            int matchesPerRound;
            if (N % 2 == 0)
            {
                numRounds = N - 1;
                matchesPerRound = N / 2;
            }
            else
            {
                numRounds = N;
                matchesPerRound = (N - 1) / 2;
            }

            for (int round = 0; round < numRounds; round++)
            {
                int[] order;
                if (N % 2 == 0)
                {
                    order = new int[N];
                    order[0] = 0;
                    for (int i = 1; i < N; i++)
                        order[i] = ((round + i - 1) % (N - 1)) + 1;
                }
                else
                {
                    order = new int[N];
                    int mid = (N - 1) / 2;
                    order[mid] = round;
                    var list = new List<int>();
                    for (int i = 0; i < N; i++)
                        if (i != round)
                            list.Add(i);
                    for (int i = 0; i < mid; i++)
                        order[i] = list[i];
                    for (int i = mid + 1; i < N; i++)
                        order[i] = list[i - 1];
                }

                for (int i = 0; i < matchesPerRound; i++)
                {
                    int idxB = N % 2 == 0 ? (N - 1 - i) : (N - 2 - i);
                    if (i >= idxB) continue;
                    var t1 = activeTeams[order[i]];
                    var t2 = activeTeams[order[idxB]];
                    Matches.Add(new Match
                    {
                        Id = Guid.NewGuid(),
                        TeamAId = t1.Id,
                        TeamBId = t2.Id,
                        RoundNumber = round + 1,
                        TeamANameSnapshot = t1.Name,
                        TeamBNameSnapshot = t2.Name,
                        Outcome = MatchOutcome.NotPlayed
                    });
                }
            }

            RefreshAll();
        }

        public Match GetMatch(Guid teamAId, Guid teamBId)
        {
            return Matches.FirstOrDefault(m => m.TeamAId == teamAId && m.TeamBId == teamBId);
        }

        public void SaveMatch(Match match)
        {
            if (!Matches.Contains(match))
            {
                Matches.Add(match);
            }

            RefreshAll();
        }

        public void RebuildRating()
        {
            Rating.Clear();

            var statsByTeam = new Dictionary<Guid, RatingRowBuilder>();

            foreach (var team in Teams)
            {
                statsByTeam[team.Id] = new RatingRowBuilder
                {
                    TeamId = team.Id,
                    TeamName = team.Name,
                    RobotWeight = team.RobotWeight,
                    IsDisqualified = team.IsDisqualified
                };
            }

            foreach (var match in Matches)
            {
                if (match.Outcome == MatchOutcome.NotPlayed)
                    continue;

                RatingRowBuilder a;
                RatingRowBuilder b;
                if (!statsByTeam.TryGetValue(match.TeamAId, out a) ||
                    !statsByTeam.TryGetValue(match.TeamBId, out b))
                    continue;

                a.Matches++;
                b.Matches++;

                int roundsA = match.Round1A + match.Round2A + match.Round3A;
                int roundsB = match.Round1B + match.Round2B + match.Round3B;

                a.RoundsScored += roundsA;
                a.RoundsConceded += roundsB;
                b.RoundsScored += roundsB;
                b.RoundsConceded += roundsA;

                switch (match.Outcome)
                {
                    case MatchOutcome.TeamAWin:
                        a.Wins++;
                        b.Losses++;
                        a.Points += 3;
                        break;
                    case MatchOutcome.TeamBWin:
                        b.Wins++;
                        a.Losses++;
                        b.Points += 3;
                        break;
                    case MatchOutcome.Draw:
                        a.Draws++;
                        b.Draws++;
                        a.Points += 1;
                        b.Points += 1;
                        break;
                }
            }

            var ordered = statsByTeam.Values
                .OrderByDescending(s => s.IsDisqualified ? 0 : 1)
                .ThenByDescending(s => s.Points)
                .ThenByDescending(s => s.RoundDifference)
                .ThenByDescending(s => s.RoundsScored)
                .ThenBy(s => s.RobotWeight)
                .ToList();

            int position = 1;
            foreach (var s in ordered)
            {
                Rating.Add(new RatingRow
                {
                    Position = s.IsDisqualified ? (int?)null : position++,
                    TeamName = s.TeamName,
                    Matches = s.Matches,
                    Wins = s.Wins,
                    Draws = s.Draws,
                    Losses = s.Losses,
                    Points = s.Points,
                    RoundDifference = s.RoundDifference,
                    RobotWeight = s.RobotWeight,
                    Status = s.IsDisqualified ? "Дисквалифицирована" : "Активна"
                });
            }
        }

        public void RebuildReports()
        {
            Reports.Clear();

            foreach (var match in Matches.Where(m => m.Outcome != MatchOutcome.NotPlayed))
            {
                var teamAName = Teams.FirstOrDefault(t => t.Id == match.TeamAId)?.Name ?? match.TeamANameSnapshot;
                var teamBName = Teams.FirstOrDefault(t => t.Id == match.TeamBId)?.Name ?? match.TeamBNameSnapshot;

                var sb = new StringBuilder();

                sb.AppendLine("Счёт по раундам: " + string.Format("{0}:{1}, {2}:{3}, {4}:{5}",
                    match.Round1A, match.Round1B,
                    match.Round2A, match.Round2B,
                    match.Round3A, match.Round3B));

                if (match.WarningsA || match.WarningsB)
                {
                    sb.Append("Предупреждения: ");
                    if (match.WarningsA) sb.Append("команда A (" + teamAName + "); ");
                    if (match.WarningsB) sb.Append("команда B (" + teamBName + "); ");
                    sb.AppendLine();
                }

                if (match.PenaltyToA || match.PenaltyToB)
                {
                    sb.Append("Дополнительные баллы сопернику: ");
                    if (match.PenaltyToA) sb.Append("команде A (баллы в пользу " + teamBName + "); ");
                    if (match.PenaltyToB) sb.Append("команде B (баллы в пользу " + teamAName + "); ");
                    sb.AppendLine();
                }

                if (match.DisqualifyA || match.DisqualifyB)
                {
                    sb.Append("Дисквалификация по решению жюри: ");
                    string reason = string.IsNullOrEmpty(match.DisqualificationReason) ? "причина не указана" : match.DisqualificationReason;
                    if (match.DisqualifyA) sb.AppendFormat("команда A ({0}). Причина: {1}. ", teamAName, reason);
                    if (match.DisqualifyB) sb.AppendFormat("команда B ({0}). Причина: {1}. ", teamBName, reason);
                    sb.AppendLine();
                }

                string outcomeText = BuildOutcomeText(match, teamAName, teamBName);
                sb.AppendLine("Итог матча: " + outcomeText);
                sb.AppendLine("Победитель: " + GetWinnerSummary(match, teamAName, teamBName));

                Reports.Add(new MatchReportRow
                {
                    MatchSummary = string.Format("{0} - {1}", teamAName, teamBName),
                    Details = sb.ToString().Trim()
                });
            }
        }

        /// <summary>
        /// Формирует итоговую фразу матча: победа A/Б, по раундам/по решению жюри/из-за дисквалификации, ничья.
        /// </summary>
        private static string BuildOutcomeText(Match match, string teamAName, string teamBName)
        {
            string reason = string.IsNullOrEmpty(match.DisqualificationReason) ? "по решению жюри" : match.DisqualificationReason;

            if (match.Outcome == MatchOutcome.Draw)
                return "Ничья. " + (match.DecisionExplanation ?? "по результатам раундов / по решению жюри");

            if (match.Outcome == MatchOutcome.TeamAWin)
            {
                if (match.DisqualifyB)
                    return string.Format("Победа команды A ({0}), т.к. команда B ({1}) дисквалифицирована по решению жюри ({2}).", teamAName, teamBName, reason);
                if (match.DecisionExplanation != null && match.DecisionExplanation.Contains("жюри"))
                    return string.Format("Победа команды A ({0}) по решению жюри.", teamAName);
                if (match.DecisionExplanation != null && match.DecisionExplanation.Contains("дополнительн"))
                    return string.Format("Победа команды A ({0}) в дополнительном раунде.", teamAName);
                return string.Format("Победа команды A ({0}). {1}", teamAName, match.DecisionExplanation ?? "По результатам раундов.");
            }

            if (match.Outcome == MatchOutcome.TeamBWin)
            {
                if (match.DisqualifyA)
                    return string.Format("Победа команды B ({0}), т.к. команда A ({1}) дисквалифицирована по решению жюри ({2}).", teamBName, teamAName, reason);
                if (match.DecisionExplanation != null && match.DecisionExplanation.Contains("жюри"))
                    return string.Format("Победа команды B ({0}) по решению жюри.", teamBName);
                if (match.DecisionExplanation != null && match.DecisionExplanation.Contains("дополнительн"))
                    return string.Format("Победа команды B ({0}) в дополнительном раунде.", teamBName);
                return string.Format("Победа команды B ({0}). {1}", teamBName, match.DecisionExplanation ?? "По результатам раундов.");
            }

            return match.ResultText ?? "—";
        }

        private static string GetWinnerSummary(Match match, string teamAName, string teamBName)
        {
            if (match.Outcome == MatchOutcome.TeamAWin) return "команда A (" + teamAName + ")";
            if (match.Outcome == MatchOutcome.TeamBWin) return "команда B (" + teamBName + ")";
            return "ничья";
        }

        public void RefreshAll()
        {
            RebuildRating();
            RebuildReports();
            SaveState();
        }

        public void SaveState()
        {
            try
            {
                var state = BuildState();
                using (var stream = File.Create(SaveFileName))
                {
                    var serializer = new DataContractJsonSerializer(typeof(TournamentState));
                    serializer.WriteObject(stream, state);
                }
            }
            catch
            {
                // игнорируем проблемы сохранения
            }
        }

        private TournamentState BuildState()
        {
            var state = new TournamentState();

            foreach (var t in Teams)
            {
                state.Teams.Add(new TeamDto
                {
                    Id = t.Id,
                    Name = t.Name,
                    Participant = t.Participant,
                    RobotWeight = t.RobotWeight,
                    Constructor = t.Constructor,
                    IsDisqualified = t.IsDisqualified,
                    DisqualificationReason = t.DisqualificationReason
                });
            }

            foreach (var m in Matches)
            {
                state.Matches.Add(new MatchDto
                {
                    Id = m.Id,
                    TeamAId = m.TeamAId,
                    TeamBId = m.TeamBId,
                    RoundNumber = m.RoundNumber,
                    Round1A = m.Round1A,
                    Round1B = m.Round1B,
                    Round2A = m.Round2A,
                    Round2B = m.Round2B,
                    Round3A = m.Round3A,
                    Round3B = m.Round3B,
                    WarningsA = m.WarningsA,
                    WarningsB = m.WarningsB,
                    PenaltyToA = m.PenaltyToA,
                    PenaltyToB = m.PenaltyToB,
                    DisqualifyA = m.DisqualifyA,
                    DisqualifyB = m.DisqualifyB,
                    DisqualificationReason = m.DisqualificationReason,
                    Outcome = m.Outcome,
                    DecisionExplanation = m.DecisionExplanation,
                    ScoreText = m.ScoreText,
                    ResultText = m.ResultText,
                    TeamANameSnapshot = m.TeamANameSnapshot,
                    TeamBNameSnapshot = m.TeamBNameSnapshot
                });
            }

            return state;
        }

        private void LoadStateIfExists()
        {
            if (!File.Exists(SaveFileName))
                return;

            try
            {
                using (var stream = File.OpenRead(SaveFileName))
                {
                    var serializer = new DataContractJsonSerializer(typeof(TournamentState));
                    var obj = serializer.ReadObject(stream) as TournamentState;
                    if (obj == null)
                        return;

                    Teams.Clear();
                    foreach (var t in obj.Teams)
                    {
                        Teams.Add(new Team
                        {
                            Id = t.Id,
                            Name = t.Name,
                            Participant = t.Participant,
                            RobotWeight = t.RobotWeight,
                            Constructor = t.Constructor,
                            IsDisqualified = t.IsDisqualified,
                            DisqualificationReason = t.DisqualificationReason
                        });
                    }

                    Matches.Clear();
                    foreach (var m in obj.Matches)
                    {
                        Matches.Add(new Match
                        {
                            Id = m.Id,
                            TeamAId = m.TeamAId,
                            TeamBId = m.TeamBId,
                            RoundNumber = m.RoundNumber,
                            Round1A = m.Round1A,
                            Round1B = m.Round1B,
                            Round2A = m.Round2A,
                            Round2B = m.Round2B,
                            Round3A = m.Round3A,
                            Round3B = m.Round3B,
                            WarningsA = m.WarningsA,
                            WarningsB = m.WarningsB,
                            PenaltyToA = m.PenaltyToA,
                            PenaltyToB = m.PenaltyToB,
                            DisqualifyA = m.DisqualifyA,
                            DisqualifyB = m.DisqualifyB,
                            DisqualificationReason = m.DisqualificationReason,
                            Outcome = m.Outcome,
                            DecisionExplanation = m.DecisionExplanation,
                            ScoreText = m.ScoreText,
                            ResultText = m.ResultText,
                            TeamANameSnapshot = m.TeamANameSnapshot,
                            TeamBNameSnapshot = m.TeamBNameSnapshot
                        });
                    }
                }
            }
            catch
            {
                // если не удалось загрузить, просто начинаем с пустого состояния
            }
        }
    }
}

