using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Sumos.Models;
using Sumos.Services;

namespace Sumos.Views
{
    public partial class ReportsPage : Page
    {
        private readonly TournamentService _service;

        public ReportsPage(TournamentService service)
        {
            InitializeComponent();
            _service = service;
            ReportsGrid.ItemsSource = _service.Reports;
        }

        /// <summary>
        /// Экспорт в JSON того, что отображается в окне «Отчёты» (протоколы матчей).
        /// </summary>
        private void ExportJsonButton_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Filter = "JSON файлы (*.json)|*.json";
            dialog.FileName = "отчёты_матчей.json";
            if (dialog.ShowDialog() != true)
                return;

            try
            {
                List<ReportExportItem> list = _service.Reports
                    .Select(r => new ReportExportItem { MatchSummary = r.MatchSummary, Details = r.Details })
                    .ToList();

                using (Stream stream = File.Create(dialog.FileName))
                {
                    var serializer = new DataContractJsonSerializer(typeof(List<ReportExportItem>));
                    serializer.WriteObject(stream, list);
                }

                SetStatus("Отчёты (протоколы матчей) экспортированы в JSON.");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка экспорта", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Экспорт в CSV того, что отображается в окне «Отчёты» (протоколы матчей).
        /// </summary>
        private void ExportCsvButton_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Filter = "CSV файлы (*.csv)|*.csv";
            dialog.FileName = "отчёты_матчей.csv";
            if (dialog.ShowDialog() != true)
                return;

            try
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("Матч;Протокол");

                foreach (MatchReportRow r in _service.Reports)
                {
                    string summary = r.MatchSummary ?? "";
                    string details = (r.Details ?? "").Replace("\"", "\"\"");
                    sb.Append(summary);
                    sb.Append(";\"");
                    sb.Append(details);
                    sb.AppendLine("\"");
                }

                File.WriteAllText(dialog.FileName, sb.ToString(), Encoding.UTF8);
                SetStatus("Отчёты (протоколы матчей) экспортированы в CSV.");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка экспорта", MessageBoxButton.OK, MessageBoxImage.Error);
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

