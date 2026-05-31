using System.Diagnostics;

namespace Model.Core;

public partial class GameLogic
{
    private Stopwatch _stopwatch = null!;
    private int _timeLimit = 300; // 5 минут по умолчанию
    private bool _timerStarted = false;
    private int _savedElapsedTime = 0; // Сохраненное время для загрузки

    public int ElapsedTime => _timerStarted ? (_savedElapsedTime + (int)_stopwatch.Elapsed.TotalSeconds) : _savedElapsedTime;
    public int RemainingTime => Math.Max(0, _timeLimit - ElapsedTime);
    public bool IsTimeUp => RemainingTime <= 0;

    public void SetTimeLimit(int seconds)
    {
        _timeLimit = seconds;
    }

    public void StartTimer()
    {
        if (!_timerStarted)
        {
            _stopwatch = Stopwatch.StartNew();
            _timerStarted = true;
        }
    }

    public void StopTimer()
    {
        if (_timerStarted)
        {
            _stopwatch.Stop();
            _savedElapsedTime += (int)_stopwatch.Elapsed.TotalSeconds;
            _timerStarted = false;
        }
    }

    public void ResetTimer()
    {
        StopTimer();
        _savedElapsedTime = 0;
        _timerStarted = false;
    }

    public void SetElapsedTime(int seconds)
    {
        StopTimer();
        _savedElapsedTime = seconds;
        _timerStarted = true;
        _stopwatch = Stopwatch.StartNew();
    }
}
