using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Sumos.Models;
using Sumos.Services;

namespace Sumos.Views
{
    public partial class TeamsPage : Page
    {
        private readonly TournamentService _service;

        public TeamsPage(TournamentService service)
        {
            InitializeComponent();
            _service = service;
            TeamsGrid.ItemsSource = _service.Teams;
        }

        /// <summary>
        /// При выборе строки в таблице подставляем данные команды в поля формы для редактирования.
        /// </summary>
        private void TeamsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Team selected = TeamsGrid.SelectedItem as Team;
            if (selected == null)
            {
                ClearTeamInputs();
                return;
            }

            TeamNameTextBox.Text = selected.Name ?? string.Empty;
            ParticipantTextBox.Text = selected.Participant ?? string.Empty;
            WeightTextBox.Text = selected.RobotWeight.ToString();
            ConstructorComboBox.SelectedIndex = -1;
            for (int i = 0; i < ConstructorComboBox.Items.Count; i++)
            {
                ComboBoxItem item = ConstructorComboBox.Items[i] as ComboBoxItem;
                if (item != null && item.Content != null &&
                    string.Equals(item.Content.ToString(), selected.Constructor, StringComparison.OrdinalIgnoreCase))
                {
                    ConstructorComboBox.SelectedIndex = i;
                    break;
                }
            }
            DisqualifyCheckBox.IsChecked = selected.IsDisqualified;
            DisqualificationReasonTextBox.Text = selected.DisqualificationReason ?? string.Empty;
        }

        private void AddTeamButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Team input = ReadTeamFromInputs();
                if (_service.Teams.Any(t => string.Equals(t.Name, input.Name, StringComparison.OrdinalIgnoreCase)))
                {
                    MessageBox.Show("Команда с таким названием уже существует.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                _service.CreateTeam(input.Name, input.Participant, input.Constructor, input.RobotWeight);
                ClearTeamInputs();
                SetStatus("Команда добавлена.");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EditTeamButton_Click(object sender, RoutedEventArgs e)
        {
            Team selected = TeamsGrid.SelectedItem as Team;
            if (selected == null)
            {
                MessageBox.Show("Выберите команду для редактирования.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                Team updated = ReadTeamFromInputs();

                if (_service.Teams.Any(t => t.Id != selected.Id &&
                                            string.Equals(t.Name, updated.Name, StringComparison.OrdinalIgnoreCase)))
                {
                    MessageBox.Show("Команда с таким названием уже существует.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                bool isDisq = DisqualifyCheckBox.IsChecked == true;
                string reason = DisqualificationReasonTextBox.Text == null ? string.Empty : DisqualificationReasonTextBox.Text.Trim();
                _service.UpdateTeam(selected, updated.Name, updated.Participant, updated.Constructor, updated.RobotWeight, isDisq, reason);
                TeamsGrid.Items.Refresh();
                SetStatus("Команда обновлена.");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteTeamButton_Click(object sender, RoutedEventArgs e)
        {
            Team selected = TeamsGrid.SelectedItem as Team;
            if (selected == null)
            {
                MessageBox.Show("Выберите команду для удаления.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (MessageBox.Show("Удалить выбранную команду? Связанные матчи будут также удалены.",
                                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            {
                return;
            }

            _service.DeleteTeam(selected);
            SetStatus("Команда и её матчи удалены.");
        }

        private Team ReadTeamFromInputs()
        {
            string name = TeamNameTextBox.Text == null ? string.Empty : TeamNameTextBox.Text.Trim();
            string participant = ParticipantTextBox.Text == null ? string.Empty : ParticipantTextBox.Text.Trim();
            string constructor = string.Empty;
            ComboBoxItem selectedItem = ConstructorComboBox.SelectedItem as ComboBoxItem;
            if (selectedItem != null && selectedItem.Content != null)
                constructor = selectedItem.Content.ToString();

            string weightText = WeightTextBox.Text == null ? string.Empty : WeightTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(name))
                throw new InvalidOperationException("Введите название команды.");
            if (string.IsNullOrWhiteSpace(participant))
                throw new InvalidOperationException("Введите ФИО участника.");

            int weight;
            if (!int.TryParse(weightText, out weight) || weight < 1 || weight > 1000)
                throw new InvalidOperationException("Масса робота должна быть целым числом от 1 до 1000 г.");

            if (string.IsNullOrWhiteSpace(constructor))
                constructor = "Другое";

            Team team = new Team();
            team.Id = Guid.NewGuid();
            team.Name = name;
            team.Participant = participant;
            team.RobotWeight = weight;
            team.Constructor = constructor;
            return team;
        }

        private void ClearTeamInputs()
        {
            TeamNameTextBox.Text = string.Empty;
            ParticipantTextBox.Text = string.Empty;
            WeightTextBox.Text = string.Empty;
            ConstructorComboBox.SelectedIndex = -1;
            DisqualifyCheckBox.IsChecked = false;
            DisqualificationReasonTextBox.Text = string.Empty;
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

