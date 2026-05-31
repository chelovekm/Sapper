using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Model.Core;
using Model.Data;

namespace Sapper;

public partial class GameWindow : Window
{
    private GameLogic _gameLogic = null!;
    private Button[,] _buttons = null!;
    private DispatcherTimer _timer = null!;
    private string _saveFolder;
    private string _saveFormat;
    private string _fileName;

    public GameWindow(int rows, int cols, double difficulty, string saveFolder, string saveFormat, string fileName)
    {
        InitializeComponent();
        _saveFolder = saveFolder;
        _saveFormat = saveFormat;
        _fileName = fileName;
        
        _gameLogic = new GameLogic(rows, cols, difficulty);
        
        // Устанавливаем размер окна в зависимости от количества клеток
        SetWindowSize();
        
        InitializeGame();
    }

    public GameWindow(string saveFolder, string saveFormat, string fileName)
    {
        InitializeComponent();
        _saveFolder = saveFolder;
        _saveFormat = saveFormat;
        _fileName = fileName;
        
        LoadGame();
    }

    private void SetWindowSize()
    {
        // Фиксированный размер окна для всех полей
        // Viewbox автоматически масштабирует клетки под этот размер
        double windowWidth = 700;
        double windowHeight = 770;

        // Устанавливаем размер окна
        this.Width = windowWidth;
        this.Height = windowHeight;
    }

    private void InitializeGame()
    {
        _buttons = new Button[_gameLogic.Field.Rows, _gameLogic.Field.Cols];
        
        CreateGrid();
        InitializeTimer();
        UpdateUI();
    }

    private void LoadGame()
    {
        string saveFile = Path.Combine(_saveFolder, $"{_fileName}.{_saveFormat}");
        ISerializer<GameField> serializer = _saveFormat == "json"
            ? new GameJsonSerializer()
            : new GameXmlSerializer();

        var field = serializer.Deserialize(saveFile);
        if (field != null)
        {
            _gameLogic = new GameLogic(field);
            
            // Устанавливаем сохраненное время
            if (field.ElapsedTime > 0)
            {
                _gameLogic.SetElapsedTime(field.ElapsedTime);
            }
            
            // Устанавливаем размер окна в зависимости от количества клеток
            SetWindowSize();
            
            InitializeGame();
            UpdateAllButtons();
        }
        else
        {
            MessageBox.Show("Ошибка загрузки сохранения игры: Не удалось загрузить файл сохранения!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            this.Close();
        }
    }

    private void InitializeTimer()
    {
        _timer = new DispatcherTimer();
        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.Tick += Timer_Tick;
        _timer.Start();
    }

    private void Timer_Tick(object sender, EventArgs e)
    {
        tbTime.Text = _gameLogic.RemainingTime.ToString();
        
        if (_gameLogic.IsTimeUp)
        {
            _timer.Stop();
            _gameLogic.IsGameOver = true;
            _gameLogic.RevealAllMines();
            _gameLogic.RemoveAllFlags();
            UpdateAllButtons();
            // Показываем изначальное количество мин при проигрыше
            tbMines.Text = _gameLogic.Field.TotalMines.ToString();
            tbStatus.Text = "Время вышло! Игра окончена!";
            tbStatus.Foreground = Brushes.Red;
            DeleteSaveFile();
        }
    }

    private void CreateGrid()
    {
        gameGrid.Rows = _gameLogic.Field.Rows;
        gameGrid.Columns = _gameLogic.Field.Cols;
        gameGrid.Children.Clear();

        // Базовый размер клетки для Viewbox
        double cellSize = 20;

        for (int i = 0; i < _gameLogic.Field.Rows; i++)
        {
            for (int j = 0; j < _gameLogic.Field.Cols; j++)
            {
                var button = new Button
                {
                    FontSize = Math.Max(12, cellSize / 2),
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(0),
                    Padding = new Thickness(0),
                    Background = Brushes.LightGray,
                    Tag = new Tuple<int, int>(i, j),
                    Width = cellSize,
                    Height = cellSize,
                    HorizontalContentAlignment = HorizontalAlignment.Center,
                    VerticalContentAlignment = VerticalAlignment.Center
                };

                button.Click += Button_Click;
                button.MouseRightButtonDown += Button_MouseRightButtonDown;

                _buttons[i, j] = button;
                gameGrid.Children.Add(button);
            }
        }
    }

    private double CalculateCellSize()
    {
        // Используем фиксированный размер клетки для обеспечения квадратной формы
        return 40;
    }

    private void Button_Click(object sender, RoutedEventArgs e)
    {
        if (_gameLogic.IsGameOver || _gameLogic.IsGameWon) return;

        var button = sender as Button;
        var (row, col) = (Tuple<int, int>)button!.Tag;

        _gameLogic.StartTimer();
        bool survived = _gameLogic.RevealCell(row, col);
        
        // Обновляем все кнопки на поле после первого клика
        UpdateAllButtons();
        UpdateUI();

        if (!survived)
        {
            _timer.Stop();
            _gameLogic.RevealAllMines();
            _gameLogic.RemoveAllFlags();
            UpdateAllButtons();
            // Показываем изначальное количество мин при проигрыше
            tbMines.Text = _gameLogic.Field.TotalMines.ToString();
            tbStatus.Text = "Игра окончена!";
            tbStatus.Foreground = Brushes.Red;

            // Удаляем файл сохранения при проигрыше
            DeleteSaveFile();

            MessageBox.Show("Игра окончена! Вы наткнулись на мину.", "Игра окончена", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        else if (_gameLogic.IsGameWon)
        {
            _timer.Stop();
            tbStatus.Text = "Вы победили!";
            tbStatus.Foreground = Brushes.Green;

            // Вычисляем очки
            int score = CalculateScore();
            
            // Сохраняем результат в таблицу лидеров сразу при победе
            SaveHighScore();

            // Используем Dispatcher для показа MessageBox на UI потоке
            Dispatcher.Invoke(() =>
            {
                MessageBox.Show($"Поздравляем! Вы победили!\nВаши очки: {score}", "Победа", MessageBoxButton.OK, MessageBoxImage.Information);
            });
        }
    }

    private void Button_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_gameLogic.IsGameOver || _gameLogic.IsGameWon) return;

        // Запрещаем ставить флаги, пока не открыто ни одной клетки
        if (!_gameLogic.MinesGenerated) return;

        var button = sender as Button;
        var (row, col) = (Tuple<int, int>)button!.Tag;

        _gameLogic.ToggleFlag(row, col);
        UpdateButton(row, col);
        UpdateUI();
        
        // Проверяем победу после постановки флага
        if (_gameLogic.IsGameWon)
        {
            _timer.Stop();
            tbStatus.Text = "Вы победили!";
            tbStatus.Foreground = Brushes.Green;
            
            // Вычисляем очки
            int score = CalculateScore();
            
            // Сохраняем результат в таблицу лидеров сразу при победе
            SaveHighScore();

            // Используем Dispatcher для показа MessageBox на UI потоке
            Dispatcher.Invoke(() =>
            {
                MessageBox.Show($"Поздравляем! Вы победили!\nВаши очки: {score}", "Победа", MessageBoxButton.OK, MessageBoxImage.Information);
            });
        }
    }

    private void UpdateButton(int row, int col)
    {
        var cell = _gameLogic.Field.GetCell(row, col);
        var button = _buttons[row, col];

        if (cell.IsFlagged)
        {
            button.Content = "🚩";
            button.Background = Brushes.LightGray;
            button.IsEnabled = true;
        }
        else if (cell.IsRevealed)
        {
            if (cell is MineCell)
            {
                button.Content = "💣";
                button.Background = Brushes.Red;
            }
            else if (cell.NeighborMines > 0)
            {
                button.Content = cell.NeighborMines.ToString();
                button.Background = Brushes.White;
                button.Foreground = GetNumberColor(cell.NeighborMines);
            }
            else
            {
                button.Content = "";
                button.Background = Brushes.White;
            }
            button.IsEnabled = false;
        }
        else
        {
            button.Content = "";
            button.Background = Brushes.LightGray;
            button.IsEnabled = true;
        }
    }

    private void UpdateAllButtons()
    {
        for (int i = 0; i < _gameLogic.Field.Rows; i++)
        {
            for (int j = 0; j < _gameLogic.Field.Cols; j++)
            {
                UpdateButton(i, j);
            }
        }
    }

    private Brush GetNumberColor(int number)
    {
        return number switch
        {
            1 => Brushes.Blue,
            2 => Brushes.Green,
            3 => Brushes.Red,
            4 => Brushes.DarkBlue,
            5 => Brushes.DarkRed,
            6 => Brushes.DarkCyan,
            7 => Brushes.Black,
            8 => Brushes.Gray,
            _ => Brushes.Black
        };
    }

    private void RevealAllMines()
    {
        for (int i = 0; i < _gameLogic.Field.Rows; i++)
        {
            for (int j = 0; j < _gameLogic.Field.Cols; j++)
            {
                UpdateButton(i, j);
            }
        }
    }

    private void UpdateUI()
    {
        int flaggedCount = 0;
        for (int i = 0; i < _gameLogic.Field.Rows; i++)
        {
            for (int j = 0; j < _gameLogic.Field.Cols; j++)
            {
                if (_gameLogic.Field.GetCell(i, j).IsFlagged)
                    flaggedCount++;
            }
        }
        
        tbMines.Text = (_gameLogic.Field.TotalMines - flaggedCount).ToString();
        tbGridSize.Text = $"{_gameLogic.Field.Rows} x {_gameLogic.Field.Cols}";
    }

    private void SaveGame()
    {
        try
        {
            // Убеждаемся, что папка существует
            if (!Directory.Exists(_saveFolder))
            {
                Directory.CreateDirectory(_saveFolder);
            }

            string saveFile = Path.Combine(_saveFolder, $"{_fileName}.{_saveFormat}");
            
            // Сохраняем текущее время и статус генерации мин в GameField перед сериализацией
            _gameLogic.Field.ElapsedTime = _gameLogic.ElapsedTime;
            _gameLogic.Field.MinesGenerated = _gameLogic.MinesGenerated;
            
            ISerializer<GameField> serializer = _saveFormat == "json"
                ? new GameJsonSerializer()
                : new GameXmlSerializer();

            serializer.Serialize(_gameLogic.Field, saveFile);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка сохранения игры: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void DeleteSaveFile()
    {
        try
        {
            string saveFile = Path.Combine(_saveFolder, $"{_fileName}.{_saveFormat}");
            if (File.Exists(saveFile))
            {
                File.Delete(saveFile);
            }
        }
        catch
        {
            // Игнорируем ошибки при удалении файла
        }
    }

    private void SaveHighScore()
    {
        try
        {
            // Убеждаемся, что папка существует
            if (!Directory.Exists(_saveFolder))
            {
                Directory.CreateDirectory(_saveFolder);
            }

            int score = CalculateScore();
            string highScoresFile = Path.Combine(_saveFolder, "highscores.txt");

            var scores = new List<string>();
            if (File.Exists(highScoresFile))
            {
                scores = File.ReadAllLines(highScoresFile).Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
            }

            // Проверяем, нужно ли добавлять новый результат
            bool shouldAdd = false;
            if (scores.Count < 10)
            {
                // Если меньше 10 записей, добавляем
                shouldAdd = true;
            }
            else
            {
                // Если уже 10 записей, проверяем, превышает ли новый результат минимальный
                var validScores = scores.Where(s => s.Contains('-') && s.Split('-').Length >= 3)
                                      .Select(s =>
                                      {
                                          var parts = s.Split('-')[2].Split(' ');
                                          if (parts.Length > 0 && int.TryParse(parts[0], out int num))
                                              return num;
                                          return 0;
                                      })
                                      .ToList();
                
                if (validScores.Count > 0)
                {
                    int minScore = validScores.Min();
                    if (score > minScore)
                    {
                        shouldAdd = true;
                    }
                }
            }

            if (shouldAdd)
            {
                scores.Add($"{_fileName} - {_gameLogic.Field.Rows}x{_gameLogic.Field.Cols} - {score} очков");
                scores = scores.Where(s => s.Contains('-') && s.Split('-').Length >= 3)
                              .OrderByDescending(s => 
                              {
                                  var parts = s.Split('-')[2].Split(' ');
                                  if (parts.Length > 0 && int.TryParse(parts[0], out int num))
                                      return num;
                                  return 0;
                              })
                              .Take(10).ToList();

                File.WriteAllLines(highScoresFile, scores);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка сохранения результата: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private int CalculateScore()
    {
        int baseScore = _gameLogic.Field.Rows * _gameLogic.Field.Cols * 10;
        int difficultyBonus = (int)(_gameLogic.Field.MinePercentage * 100);
        int timeBonus = Math.Max(0, 300 - _gameLogic.ElapsedTime);
        
        return baseScore + difficultyBonus + timeBonus;
    }

    private void btnBackToMenu_Click(object sender, RoutedEventArgs e)
    {
        if (_gameLogic.IsGameOver)
        {
            DeleteSaveFile();
        }
        else if (_gameLogic.IsGameWon)
        {
            DeleteSaveFile();
        }
        else
        {
            SaveGame();
        }
        _timer.Stop();

        var mainWindow = new MainWindow();
        mainWindow.Show();
        this.Close();
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        if (_gameLogic.IsGameOver)
        {
            DeleteSaveFile();
        }
        else if (_gameLogic.IsGameWon)
        {
            DeleteSaveFile();
        }
        else
        {
            SaveGame();
        }
    }
}
