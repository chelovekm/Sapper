using System.IO;
using System.Windows;

namespace Sapper;

public partial class HighScoresWindow : Window
{
    private readonly string _saveFolder;

    public HighScoresWindow(string saveFolder)
    {
        InitializeComponent();
        _saveFolder = saveFolder;
        LoadHighScores();
    }

    private void LoadHighScores()
    {
        string highScoresFile = Path.Combine(_saveFolder, "highscores.txt");

        if (File.Exists(highScoresFile))
        {
            var scores = File.ReadAllLines(highScoresFile);
            if (scores.Length > 0 && !string.IsNullOrWhiteSpace(scores[0]))
            {
                lbHighScores.ItemsSource = scores;
            }
            else
            {
                lbHighScores.ItemsSource = new string[] { "Топ лидеров пуст!" };
            }
        }
        else
        {
            lbHighScores.ItemsSource = new string[] { "Топ лидеров пуст!" };
        }
    }

    private void btnClose_Click(object sender, RoutedEventArgs e)
    {
        this.Close();
    }
}
