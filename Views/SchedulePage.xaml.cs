using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Sumos.Models;
using Sumos.Services;

namespace Sumos.Views
{
    public partial class SchedulePage : Page
    {
        private readonly TournamentService _service;

        public SchedulePage(TournamentService service)
        {
            InitializeComponent();
            _service = service;
        }

        private void ToursItemsControl_Loaded(object sender, RoutedEventArgs e)
        {
            BuildTours();
        }

        private void BuildTours()
        {
            var byRound = _service.Matches
                .OrderBy(m => m.RoundNumber)
                .GroupBy(m => m.RoundNumber)
                .OrderBy(g => g.Key)
                .ToList();

            var blocks = new List<ScheduleTourBlock>();
            foreach (var group in byRound)
            {
                blocks.Add(new ScheduleTourBlock
                {
                    TourNumber = group.Key,
                    Pairings = group
                        .Select(m => m.TeamANameSnapshot + " - " + m.TeamBNameSnapshot)
                        .ToList()
                });
            }

            ToursItemsControl.ItemsSource = blocks;
        }
    }
}
