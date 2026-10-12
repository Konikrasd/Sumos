using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Sumos.Models;
using Sumos.Services;

namespace Sumos.Views
{
    public partial class MatchesPage : Page
    {
        private readonly TournamentService _service;

        public MatchesPage(TournamentService service)
        {
            InitializeComponent();
            _service = service;

            TeamAComboBox.ItemsSource = _service.Teams;
            TeamBComboBox.ItemsSource = _service.Teams;
            MatchesGrid.ItemsSource = _service.Matches;
        }

        private void GenerateScheduleButton_Click(object sender, RoutedEventArgs e)
        {
            if (_service.Teams.Count < 3)
            {
                MessageBox.Show("Для генерации календаря необходимо минимум 3 команды.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _service.GenerateSchedule();
            SetStatus("Календарь спаррингов сгенерирован.");
        }

        private void FinishMatchButton_Click(object sender, RoutedEventArgs e)
        {
            Team teamA = TeamAComboBox.SelectedItem as Team;
            Team teamB = TeamBComboBox.SelectedItem as Team;
            if (teamA == null || teamB == null || teamA.Id == teamB.Id)
            {
                MessageBox.Show("Выберите две разные команды.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (teamA.IsDisqualified || teamB.IsDisqualified)
            {
                MessageBox.Show("Одна из команд дисквалифицирована. Матч невозможен.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int r1A, r1B, r2A, r2B, r3A, r3B;
            if (!TryReadRoundScore(Round1ATextBox, Round1BTextBox, out r1A, out r1B) ||
                !TryReadRoundScore(Round2ATextBox, Round2BTextBox, out r2A, out r2B) ||
                !TryReadRoundScore(Round3ATextBox, Round3BTextBox, out r3A, out r3B))
            {
                MessageBox.Show("Счёт по раундам должен быть 0 или 1.", "Ошибка ввода",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            Match match = _service.GetMatch(teamA.Id, teamB.Id);
            if (match == null)
            {
                match = new Match();
                match.Id = Guid.NewGuid();
                match.TeamAId = teamA.Id;
                match.TeamBId = teamB.Id;
                match.RoundNumber = _service.Matches.Count + 1;
            }

            match.TeamANameSnapshot = teamA.Name;
            match.TeamBNameSnapshot = teamB.Name;

            match.Round1A = r1A;
            match.Round1B = r1B;
            match.Round2A = r2A;
            match.Round2B = r2B;
            match.Round3A = r3A;
            match.Round3B = r3B;

            match.WarningsA = WarningsACheckBox.IsChecked == true;
            match.WarningsB = WarningsBCheckBox.IsChecked == true;
            match.PenaltyToA = PenaltyToACheckBox.IsChecked == true;
            match.PenaltyToB = PenaltyToBCheckBox.IsChecked == true;
            match.DisqualifyA = DisqualifyACheckBox.IsChecked == true;
            match.DisqualifyB = DisqualifyBCheckBox.IsChecked == true;
            match.DisqualificationReason = DisqualificationReasonTextBox.Text == null
                ? string.Empty
                : DisqualificationReasonTextBox.Text.Trim();

            ApplyDecision(match, teamA, teamB);

            if (match.DisqualifyA)
            {
                teamA.IsDisqualified = true;
                if (!string.IsNullOrWhiteSpace(match.DisqualificationReason))
                    teamA.DisqualificationReason = match.DisqualificationReason;
            }
            if (match.DisqualifyB)
            {
                teamB.IsDisqualified = true;
                if (!string.IsNullOrWhiteSpace(match.DisqualificationReason))
                    teamB.DisqualificationReason = match.DisqualificationReason;
            }

            _service.SaveMatch(match);
            ClearMatchInputs();
            MatchesGrid.Items.Refresh();
            SetStatus("Матч сохранён.");
        }

        private bool TryReadRoundScore(TextBox aBox, TextBox bBox, out int a, out int b)
        {
            a = 0;
            b = 0;
            string aText = aBox.Text == null ? string.Empty : aBox.Text.Trim();
            string bText = bBox.Text == null ? string.Empty : bBox.Text.Trim();

            if (!IsValidRoundValue(aText) || !IsValidRoundValue(bText))
                return false;

            a = string.IsNullOrEmpty(aText) ? 0 : int.Parse(aText);
            b = string.IsNullOrEmpty(bText) ? 0 : int.Parse(bText);
            return true;
        }

        private bool IsValidRoundValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return true;
            return value == "0" || value == "1";
        }

        private void ApplyDecision(Match match, Team teamA, Team teamB)
        {
            int winsA = match.Round1A + match.Round2A + match.Round3A;
            int winsB = match.Round1B + match.Round2B + match.Round3B;

            ComboBoxItem item = DecisionComboBox.SelectedItem as ComboBoxItem;
            string decisionText = item != null && item.Content != null
                ? item.Content.ToString()
                : "Авто по раундам";

            MatchOutcome outcome;
            string explanation;

            if (decisionText.StartsWith("Доп. раунд - победа A"))
            {
                outcome = MatchOutcome.TeamAWin;
                explanation = "Победа в дополнительном раунде (A)";
            }
            else if (decisionText.StartsWith("Доп. раунд - победа B"))
            {
                outcome = MatchOutcome.TeamBWin;
                explanation = "Победа в дополнительном раунде (B)";
            }
            else if (decisionText.StartsWith("Решение жюри - победа A"))
            {
                outcome = MatchOutcome.TeamAWin;
                explanation = "Победа по решению жюри (A)";
            }
            else if (decisionText.StartsWith("Решение жюри - победа B"))
            {
                outcome = MatchOutcome.TeamBWin;
                explanation = "Победа по решению жюри (B)";
            }
            else if (decisionText.StartsWith("Ничья"))
            {
                outcome = MatchOutcome.Draw;
                explanation = "Ничья по решению жюри";
            }
            else
            {
                if (winsA > winsB)
                {
                    outcome = MatchOutcome.TeamAWin;
                    explanation = "Победа по результатам раундов";
                }
                else if (winsB > winsA)
                {
                    outcome = MatchOutcome.TeamBWin;
                    explanation = "Победа по результатам раундов";
                }
                else
                {
                    outcome = MatchOutcome.Draw;
                    explanation = "Ничья по результатам раундов";
                }
            }

            match.Outcome = outcome;
            match.DecisionExplanation = explanation;

            match.ScoreText = string.Format("{0}:{1}, {2}:{3}, {4}:{5}",
                match.Round1A, match.Round1B,
                match.Round2A, match.Round2B,
                match.Round3A, match.Round3B);

            switch (outcome)
            {
                case MatchOutcome.TeamAWin:
                    match.ResultText = string.Format("Победа {0} ({1})", teamA.Name, explanation);
                    break;
                case MatchOutcome.TeamBWin:
                    match.ResultText = string.Format("Победа {0} ({1})", teamB.Name, explanation);
                    break;
                case MatchOutcome.Draw:
                    match.ResultText = string.Format("Ничья ({0})", explanation);
                    break;
                default:
                    match.ResultText = "Не сыгран";
                    break;
            }
        }

        private void ClearMatchInputs()
        {
            Round1ATextBox.Text = string.Empty;
            Round1BTextBox.Text = string.Empty;
            Round2ATextBox.Text = string.Empty;
            Round2BTextBox.Text = string.Empty;
            Round3ATextBox.Text = string.Empty;
            Round3BTextBox.Text = string.Empty;

            WarningsACheckBox.IsChecked = false;
            WarningsBCheckBox.IsChecked = false;
            PenaltyToACheckBox.IsChecked = false;
            PenaltyToBCheckBox.IsChecked = false;
            DisqualifyACheckBox.IsChecked = false;
            DisqualifyBCheckBox.IsChecked = false;

            DisqualificationReasonTextBox.Text = string.Empty;
        }

        private void MatchesGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            Match match = e.Row.Item as Match;
            if (match == null)
                return;

            switch (match.Outcome)
            {
                case MatchOutcome.TeamAWin:
                case MatchOutcome.TeamBWin:
                    e.Row.Background = new SolidColorBrush(Color.FromRgb(200, 255, 200));
                    break;
                case MatchOutcome.Draw:
                    e.Row.Background = new SolidColorBrush(Color.FromRgb(220, 220, 220));
                    break;
                default:
                    e.Row.Background = Brushes.White;
                    break;
            }
        }

        private void SetStatus(string text)
        {
            MainWindow window = Application.Current.MainWindow as MainWindow;
            if (window != null)
            {
                window.SetStatus(text);
            }
        }
    }
}

