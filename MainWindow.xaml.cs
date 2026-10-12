using System;
using System.Windows;
using System.Windows.Controls;
using Sumos.Services;
using Sumos.Views;

namespace Sumos
{
    public partial class MainWindow : Window
    {
        private readonly TournamentService _service;

        public MainWindow()
        {
            InitializeComponent();
            _service = new TournamentService();
            MainFrame.Navigate(new TeamsPage(_service));
        }

        private void OpenTeamsPage_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new TeamsPage(_service));
        }

        private void OpenMatchesPage_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new MatchesPage(_service));
        }

        private void OpenRatingPage_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new RatingPage(_service));
        }

        private void OpenSchedulePage_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new SchedulePage(_service));
        }

        private void OpenReportsPage_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new ReportsPage(_service));
        }

        public void SetStatus(string text)
        {
            StatusTextBlock.Text = text;
        }
    }
}
