namespace MicroGit.HelperFunctions;

public class HistoryQueue
{

    private static List<string> _historyQueue = new List<string>();
    private static int _currentIndex = 0;
    private static int _maxQueueSize = int.MaxValue;
    
    
    public static Thread _keyListenThread;
    public static bool _threadIsRunning = false;
    public static bool _stopThread = false;

    public static void PushCommand(IReadOnlyList<string> args)
    {
        if (_historyQueue.Count() < _maxQueueSize)
        {
            _historyQueue.Add(string.Join(" ", args));
        }
    }

    public static string GetPreviousCommand()
    {
        if (_currentIndex > 0)
        {
            _currentIndex--;
            return _historyQueue[_currentIndex];
        }
        else
        {
            return "";
        }
    }

    public static string GetNextCommand()
    {
        if (_currentIndex < _historyQueue.Count() - 1)
        {
            _currentIndex++;
            return _historyQueue[_currentIndex];
        }
        else
        {
            return "";
        }
    }
    

    public static void DetectArrowKeyPress()
    {
        if (!_threadIsRunning)
        {
            _keyListenThread = new Thread(() =>
            {
                _threadIsRunning = true;
                while (!_stopThread)
                {
                    if (Console.KeyAvailable)
                    {
                        ConsoleKeyInfo keyInfo = Console.ReadKey();
                        var cmd = string.Empty;
                        if (keyInfo.Key == ConsoleKey.UpArrow)
                        {
                            cmd = GetPreviousCommand();
                            Console.Write("\r" + new string(' ', Console.BufferWidth) + "\r");
                            Console.Write(cmd);
                        }
                        else if (keyInfo.Key == ConsoleKey.DownArrow)
                        {
                            cmd = GetNextCommand();
                            Console.Write("\r" + new string(' ', Console.BufferWidth) + "\r");
                            Console.Write(cmd);
                        }
                    }
                }
            });
        }
    }

}