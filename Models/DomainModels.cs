using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Sumos.Models
{
    /// <summary>
    /// Итог матча.
    /// </summary>
    public enum MatchOutcome
    {
        NotPlayed = 0,
        TeamAWin = 1,
        TeamBWin = 2,
        Draw = 3
    }

    /// <summary>
    /// Команда турнира.
    /// </summary>
    public class Team
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Participant { get; set; }
        public int RobotWeight { get; set; }
        public string Constructor { get; set; }
        public bool IsDisqualified { get; set; }
        public string DisqualificationReason { get; set; }

        public string StatusText
        {
            get { return IsDisqualified ? "Дисквалифицирована" : "Активна"; }
        }
    }

    /// <summary>
    /// Матч между двумя командами.
    /// </summary>
    public class Match
    {
        public Guid Id { get; set; }
        public Guid TeamAId { get; set; }
        public Guid TeamBId { get; set; }
        public int RoundNumber { get; set; }

        public int Round1A { get; set; }
        public int Round1B { get; set; }
        public int Round2A { get; set; }
        public int Round2B { get; set; }
        public int Round3A { get; set; }
        public int Round3B { get; set; }

        public bool WarningsA { get; set; }
        public bool WarningsB { get; set; }
        public bool PenaltyToA { get; set; }
        public bool PenaltyToB { get; set; }
        public bool DisqualifyA { get; set; }
        public bool DisqualifyB { get; set; }
        public string DisqualificationReason { get; set; }

        public MatchOutcome Outcome { get; set; }
        public string DecisionExplanation { get; set; }

        public string ScoreText { get; set; }
        public string ResultText { get; set; }

        // Снимок имён команд на момент матча для отчётов
        public string TeamANameSnapshot { get; set; }
        public string TeamBNameSnapshot { get; set; }
    }

    /// <summary>
    /// Сериализуемое состояние турнира для сохранения/загрузки.
    /// </summary>
    [DataContract]
    public class TournamentState
    {
        [DataMember]
        public List<TeamDto> Teams { get; set; }

        [DataMember]
        public List<MatchDto> Matches { get; set; }

        public TournamentState()
        {
            Teams = new List<TeamDto>();
            Matches = new List<MatchDto>();
        }
    }

    [DataContract]
    public class TeamDto
    {
        [DataMember] public Guid Id { get; set; }
        [DataMember] public string Name { get; set; }
        [DataMember] public string Participant { get; set; }
        [DataMember] public int RobotWeight { get; set; }
        [DataMember] public string Constructor { get; set; }
        [DataMember] public bool IsDisqualified { get; set; }
        [DataMember] public string DisqualificationReason { get; set; }
    }

    [DataContract]
    public class MatchDto
    {
        [DataMember] public Guid Id { get; set; }
        [DataMember] public Guid TeamAId { get; set; }
        [DataMember] public Guid TeamBId { get; set; }
        [DataMember] public int RoundNumber { get; set; }

        [DataMember] public int Round1A { get; set; }
        [DataMember] public int Round1B { get; set; }
        [DataMember] public int Round2A { get; set; }
        [DataMember] public int Round2B { get; set; }
        [DataMember] public int Round3A { get; set; }
        [DataMember] public int Round3B { get; set; }

        [DataMember] public bool WarningsA { get; set; }
        [DataMember] public bool WarningsB { get; set; }
        [DataMember] public bool PenaltyToA { get; set; }
        [DataMember] public bool PenaltyToB { get; set; }
        [DataMember] public bool DisqualifyA { get; set; }
        [DataMember] public bool DisqualifyB { get; set; }
        [DataMember] public string DisqualificationReason { get; set; }

        [DataMember] public MatchOutcome Outcome { get; set; }
        [DataMember] public string DecisionExplanation { get; set; }

        [DataMember] public string ScoreText { get; set; }
        [DataMember] public string ResultText { get; set; }

        [DataMember] public string TeamANameSnapshot { get; set; }
        [DataMember] public string TeamBNameSnapshot { get; set; }
    }

    /// <summary>
    /// Строка рейтинговой таблицы.
    /// </summary>
    public class RatingRow
    {
        public int? Position { get; set; }
        public string TeamName { get; set; }
        public int Matches { get; set; }
        public int Wins { get; set; }
        public int Draws { get; set; }
        public int Losses { get; set; }
        public int Points { get; set; }
        public int RoundDifference { get; set; }
        public int RobotWeight { get; set; }
        public string Status { get; set; }
    }

    /// <summary>
    /// Вспомогательная модель для построения рейтинга.
    /// </summary>
    internal class RatingRowBuilder
    {
        public Guid TeamId { get; set; }
        public string TeamName { get; set; }
        public int Matches { get; set; }
        public int Wins { get; set; }
        public int Draws { get; set; }
        public int Losses { get; set; }
        public int Points { get; set; }
        public int RoundsScored { get; set; }
        public int RoundsConceded { get; set; }
        public int RobotWeight { get; set; }
        public bool IsDisqualified { get; set; }

        public int RoundDifference
        {
            get { return RoundsScored - RoundsConceded; }
        }
    }

    /// <summary>
    /// Строка отчёта по матчу.
    /// </summary>
    public class MatchReportRow
    {
        public string MatchSummary { get; set; }
        public string Details { get; set; }
    }

    /// <summary>
    /// Элемент экспорта отчётов (то, что отображается в окне «Отчёты»).
    /// </summary>
    [DataContract]
    public class ReportExportItem
    {
        [DataMember(Name = "матч")]
        public string MatchSummary { get; set; }
        [DataMember(Name = "протокол")]
        public string Details { get; set; }
    }

    /// <summary>
    /// Блок тура для отображения спаррингов: номер тура и список пар «Команда A — Команда B».
    /// </summary>
    public class ScheduleTourBlock
    {
        public int TourNumber { get; set; }
        public List<string> Pairings { get; set; }
    }
}

