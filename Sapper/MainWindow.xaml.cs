using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Model.Core;
using Model.Data;

namespace Sapper;

public partial class MainWindow : Window
{
    private string _saveFolder = Path.Combine(Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..")), "Saves");
    private string _saveFormat = "json";

    public MainWindow()
    {
        InitializeComponent();
        tbSaveFolder.Text = _saveFolder;
        
        // Создаем папку Saves, если она не существует
        if (!Directory.Exists(_saveFolder))
        {
            Directory.CreateDirectory(_saveFolder);
        }
        
        // Создаем файл highscores.txt, если он не существует
        string highScoresFile = Path.Combine(_saveFolder, "highscores.txt");
        if (!File.Exists(highScoresFile))
        {
            File.WriteAllText(highScoresFile, "");
        }
        
        CheckExistingSave();
    }

    private void CheckExistingSave()
    {
        try
        {
            if (!Directory.Exists(_saveFolder))
            {
                Directory.CreateDirectory(_saveFolder);
            }

            string fileName = tbFileName.Text;
            if (string.IsNullOrWhiteSpace(fileName))
            {
                btnContinueGame.IsEnabled = false;
                return;
            }

            // Проверяем оба формата
            string jsonFile = Path.Combine(_saveFolder, $"{fileName}.json");
            string xmlFile = Path.Combine(_saveFolder, $"{fileName}.xml");

            bool jsonExists = File.Exists(jsonFile);
            bool xmlExists = File.Exists(xmlFile);

            bool jsonValid = false;
            bool xmlValid = false;

            if (jsonExists)
            {
                var jsonSerializer = new GameJsonSerializer();
                jsonValid = jsonSerializer.IsValidFile(jsonFile);
            }

            if (xmlExists)
            {
                var xmlSerializer = new GameXmlSerializer();
                xmlValid = xmlSerializer.IsValidFile(xmlFile);
            }

            // Активируем кнопку, если есть валидный файл в любом формате
            btnContinueGame.IsEnabled = jsonValid || xmlValid;
        }
        catch
        {
            btnContinueGame.IsEnabled = false;
        }
    }

    private void btnNewGame_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!int.TryParse(tbFieldRows.Text, out int rows) || rows < 5 || rows > 20)
            {
                MessageBox.Show("Пожалуйста, введите корректное количество строк (5-20)!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(tbFieldCols.Text, out int cols) || cols < 5 || cols > 20)
            {
                MessageBox.Show("Пожалуйста, введите корректное количество столбцов (5-20)!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(tbFileName.Text))
            {
                MessageBox.Show("Пожалуйста, введите имя файла!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var diffItem = cbDifficulty.SelectedItem as ComboBoxItem;
            double difficulty = double.Parse(diffItem?.Tag?.ToString() ?? "0.3", CultureInfo.InvariantCulture);

            var formatItem = cbSaveFormat.SelectedItem as ComboBoxItem;
            string newFormat = formatItem?.Tag?.ToString() ?? "json";
            _saveFolder = tbSaveFolder.Text;

            // Конвертируем файл сохранения, если формат изменился
            ConvertSaveFileIfNeeded(newFormat);

            _saveFormat = newFormat;

            var gameWindow = new GameWindow(rows, cols, difficulty, _saveFolder, _saveFormat, tbFileName.Text);
            gameWindow.Show();
            this.Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при запуске новой игры: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void btnContinueGame_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!btnContinueGame.IsEnabled)
            {
                MessageBox.Show("Невозможно продолжить игру: Не найден валидный файл сохранения.", "Невозможно продолжить", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (string.IsNullOrWhiteSpace(tbFileName.Text))
            {
                MessageBox.Show("Пожалуйста, введите имя файла!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var formatItem = cbSaveFormat.SelectedItem as ComboBoxItem;
            string newFormat = formatItem?.Tag?.ToString() ?? "json";
            _saveFolder = tbSaveFolder.Text;

            // Конвертируем файл сохранения, если формат изменился
            ConvertSaveFileIfNeeded(newFormat);

            _saveFormat = newFormat;

            var gameWindow = new GameWindow(_saveFolder, _saveFormat, tbFileName.Text);
            gameWindow.Show();
            this.Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка загрузки игры: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void btnBrowseFolder_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Выбрать папку сохранений",
            InitialDirectory = _saveFolder
        };

        if (dialog.ShowDialog() == true)
        {
            _saveFolder = dialog.FolderName;
            tbSaveFolder.Text = _saveFolder;
            CheckExistingSave();
        }
    }

    private void cbSaveFormat_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        var selectedItem = cbSaveFormat.SelectedItem as ComboBoxItem;
        _saveFormat = selectedItem?.Tag?.ToString() ?? "json";
        CheckExistingSave();
    }

    private void ConvertSaveFileIfNeeded(string newFormat)
    {
        string fileName = tbFileName.Text;
        if (string.IsNullOrWhiteSpace(fileName))
            return;

        string jsonFile = Path.Combine(_saveFolder, $"{fileName}.json");
        string xmlFile = Path.Combine(_saveFolder, $"{fileName}.xml");

        bool jsonExists = File.Exists(jsonFile);
        bool xmlExists = File.Exists(xmlFile);

        // Если файл уже существует в нужном формате, ничего не делаем
        if (newFormat == "json" && jsonExists)
            return;
        if (newFormat == "xml" && xmlExists)
            return;

        // Определяем, какой файл нужно конвертировать
        string sourceFile = null;
        string sourceFormat = null;

        if (newFormat == "json" && xmlExists)
        {
            sourceFile = xmlFile;
            sourceFormat = "xml";
        }
        else if (newFormat == "xml" && jsonExists)
        {
            sourceFile = jsonFile;
            sourceFormat = "json";
        }

        if (sourceFile != null)
        {
            try
            {
                // Десериализуем из исходного формата
                ISerializer<GameField> sourceSerializer = sourceFormat == "json"
                    ? new GameJsonSerializer()
                    : new GameXmlSerializer();

                var field = sourceSerializer.Deserialize(sourceFile);

                if (field != null)
                {
                    // Сериализуем в новый формат
                    ISerializer<GameField> targetSerializer = newFormat == "json"
                        ? new GameJsonSerializer()
                        : new GameXmlSerializer();

                    string targetFile = Path.Combine(_saveFolder, $"{fileName}.{newFormat}");
                    targetSerializer.Serialize(field, targetFile);

                    // Удаляем старый файл
                    File.Delete(sourceFile);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка конвертации файла сохранения: {ex.Message}", "Ошибка конвертации", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }

    private void btnHighScores_Click(object sender, RoutedEventArgs e)
    {
        var highScoresWindow = new HighScoresWindow(_saveFolder);
        highScoresWindow.ShowDialog();
    }

    private void tbFileName_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        CheckExistingSave();
    }
}