using System.Windows.Controls;
using System.Windows.Media;
using System.Windows;
using Sumos.Models;
using Sumos.Services;

namespace Sumos.Views
{
    public partial class RatingPage : Page
    {
        private readonly TournamentService _service;

        public RatingPage(TournamentService service)
        {
            InitializeComponent();
            _service = service;
            RatingGrid.ItemsSource = _service.Rating;
        }

        private void RatingGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            RatingRow row = e.Row.Item as RatingRow;
            if (row == null)
                return;

            if (row.Status == "Дисквалифицирована")
            {
                e.Row.Background = new SolidColorBrush(Color.FromRgb(255, 200, 200));
            }
        }
    }
}

