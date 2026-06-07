using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace BruteForceApp
{
    public partial class MainWindow : Window
    {
        private readonly PasswordManager _passwordManager;
        private readonly CombinationGenerator _generator;
        private readonly PerformanceLogger _logger;
        private BruteForceEngine _engine;

        private string _currentHash;
        private string _currentPlaintext;

        private DispatcherTimer _uiTimer;
        private Stopwatch _attackStopwatch;
        private long _lastChecked;
        private DateTime _lastSpeedSample;

        public MainWindow()
        {
            InitializeComponent();
            _passwordManager = new PasswordManager();
            _generator = new CombinationGenerator(_passwordManager.Charset, maxLength: 6);
            _logger = new PerformanceLogger();

            TxtThreads.Text = $"{Math.Max(1, Environment.ProcessorCount - 1)}";
            InitTimer();
            Log("Application started. Generate or enter a password to begin.");
            Log($"CPU cores: {Environment.ProcessorCount}  →  Attack threads: {Math.Max(1, Environment.ProcessorCount - 1)}");
            Log($"Search space: {_generator.TotalCombinations():N0} combinations (length 1-6)");
        }

        // ──────────────────────────────────────────────────────────
        //  UI TIMER
        // ──────────────────────────────────────────────────────────

        private void InitTimer()
        {
            _uiTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
            _uiTimer.Tick += UiTimer_Tick;
        }

        private void UiTimer_Tick(object sender, EventArgs e)
        {
            if (_engine == null || _attackStopwatch == null) return;

            // Elapsed time
            var elapsed = _attackStopwatch.Elapsed;
            TxtElapsed.Text = $"{(int)elapsed.TotalMinutes:D2}:{elapsed.Seconds:D2}.{elapsed.Milliseconds:D3}";

            // Progress
            long checked_ = _engine.CheckedCount;
            long total = _engine.TotalCombinations;
            TxtChecked.Text = checked_.ToString("N0");

            double pct = total > 0 ? (double)checked_ / total * 100.0 : 0;
            ProgressBar.Value = Math.Min(100, pct);
            TxtProgressPct.Text = $"{pct:F2}%";

            // Speed
            double dt = (DateTime.Now - _lastSpeedSample).TotalSeconds;
            if (dt >= 0.5)
            {
                long delta = checked_ - _lastChecked;
                double kps = delta / dt / 1000.0;
                TxtSpeed.Text = $"{kps:F1}";
                _lastChecked = checked_;
                _lastSpeedSample = DateTime.Now;
            }
        }

        // ──────────────────────────────────────────────────────────
        //  BUTTON HANDLERS
        // ──────────────────────────────────────────────────────────

        private void BtnGenerate_Click(object sender, RoutedEventArgs e)
        {
            _currentPlaintext = _passwordManager.GeneratePassword();
            _currentHash = _passwordManager.HashPassword(_currentPlaintext);
            TxtPlainPassword.Text = _currentPlaintext;
            TxtHash.Text = _currentHash[..16] + "...";
            TxtHash.ToolTip = _currentHash;
            TxtFoundPassword.Text = "Searching...";
            TxtFoundPassword.Foreground = System.Windows.Media.Brushes.Gray;
            Log($"Generated password: '{_currentPlaintext}' (length {_currentPlaintext.Length})");
            Log($"SHA256 hash: {_currentHash}");
        }

        private void BtnCustom_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CustomPasswordDialog();
            if (dialog.ShowDialog() == true && !string.IsNullOrEmpty(dialog.Password))
            {
                _currentPlaintext = dialog.Password;
                _currentHash = _passwordManager.HashPassword(_currentPlaintext);
                TxtPlainPassword.Text = $"(custom - {_currentPlaintext.Length} chars)";
                TxtHash.Text = _currentHash[..16] + "...";
                TxtHash.ToolTip = _currentHash;
                TxtFoundPassword.Text = "Searching...";
                TxtFoundPassword.Foreground = System.Windows.Media.Brushes.Gray;
                Log($"Custom password set (length {_currentPlaintext.Length}). Hash: {_currentHash[..16]}...");
            }
        }

        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_currentHash))
            {
                MessageBox.Show("Please generate or enter a password first.", "No Password", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _engine = new BruteForceEngine(_passwordManager, _generator);
            _engine.ProgressUpdated += OnProgressUpdated;
            _engine.PasswordFound += OnPasswordFound;
            _engine.LogMessage += Log;

            BtnStart.IsEnabled = false;
            BtnStop.IsEnabled = true;
            BtnBenchmark.IsEnabled = false;
            TxtFoundPassword.Text = "Cracking...";
            TxtFoundPassword.Foreground = System.Windows.Media.Brushes.Orange;
            ProgressBar.Value = 0;

            _attackStopwatch = Stopwatch.StartNew();
            _lastChecked = 0;
            _lastSpeedSample = DateTime.Now;
            _uiTimer.Start();

            int threads = Math.Max(1, Environment.ProcessorCount - 1);
            Log($"═══ ATTACK STARTED (Multi-Thread, {threads} threads) ═══");
            _engine.StartMultiThreaded(_currentHash);
        }

        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            _engine?.Stop();
            _attackStopwatch?.Stop();
            _uiTimer.Stop();
            BtnStart.IsEnabled = true;
            BtnStop.IsEnabled = false;
            BtnBenchmark.IsEnabled = true;
        }

        private void BtnBenchmark_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_currentHash))
            {
                MessageBox.Show("Please generate or enter a password first.", "No Password", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            BtnStart.IsEnabled = false;
            BtnBenchmark.IsEnabled = false;
            BtnStop.IsEnabled = false;
            TxtFoundPassword.Text = "Benchmarking...";
            TxtFoundPassword.Foreground = System.Windows.Media.Brushes.Yellow;

            string hashSnapshot = _currentHash;
            string plain = _currentPlaintext;

            Task.Run(() =>
            {
                // ── Single-thread run ──
                Dispatcher.Invoke(() => Log("═══ BENCHMARK: Single-thread run... ═══"));
                var singleEngine = new BruteForceEngine(_passwordManager, _generator);
                var (pwd1, t1) = singleEngine.RunSingleThreaded(hashSnapshot);
                _logger.LogSingleThread(t1, pwd1 ?? "?");
                Dispatcher.Invoke(() => Log($"[Single] Found '{pwd1}' in {t1.TotalMilliseconds:F0} ms"));

                // ── Multi-thread run ──
                int threads = Math.Max(1, Environment.ProcessorCount - 1);
                Dispatcher.Invoke(() => Log($"═══ BENCHMARK: Multi-thread run ({threads} threads)... ═══"));

                var multiEngine = new BruteForceEngine(_passwordManager, _generator);
                TimeSpan t2 = TimeSpan.Zero;
                string pwd2 = null;
                using var cts = new CancellationTokenSource();

                multiEngine.PasswordFound += (p, t) => { pwd2 = p; t2 = t; cts.Cancel(); };
                multiEngine.StartMultiThreaded(hashSnapshot);

                // Wait for completion
                while (!cts.IsCancellationRequested && multiEngine.IsRunning)
                    Thread.Sleep(50);
                Thread.Sleep(200); // let events fire

                _logger.LogMultiThread(t2, pwd2 ?? "?", threads);

                string report = _logger.GetComparisonReport();
                _logger.SaveToFile("performance_log.txt");

                Dispatcher.Invoke(() =>
                {
                    Log(report);
                    TxtFoundPassword.Text = pwd2 ?? pwd1 ?? "Not found";
                    TxtFoundPassword.Foreground = System.Windows.Media.Brushes.LightGreen;
                    BtnStart.IsEnabled = true;
                    BtnBenchmark.IsEnabled = true;
                });
            });
        }

        // ──────────────────────────────────────────────────────────
        //  ENGINE CALLBACKS
        // ──────────────────────────────────────────────────────────

        private void OnProgressUpdated(long checked_, long total)
        {
            // Already handled by timer; nothing needed here for heavy UI ops
        }

        private void OnPasswordFound(string password, TimeSpan elapsed)
        {
            _attackStopwatch?.Stop();
            _uiTimer.Stop();

            int threads = Math.Max(1, Environment.ProcessorCount - 1);
            _logger.LogMultiThread(elapsed, password, threads);

            Dispatcher.Invoke(() =>
            {
                TxtFoundPassword.Text = $"✓  \"{password}\"";
                TxtFoundPassword.Foreground = System.Windows.Media.Brushes.LightGreen;
                ProgressBar.Value = 100;
                TxtProgressPct.Text = "100%";
                BtnStart.IsEnabled = true;
                BtnStop.IsEnabled = false;
                BtnBenchmark.IsEnabled = true;
                Log($"╔══════════════════════════════╗");
                Log($"║  PASSWORD FOUND: '{password}'");
                Log($"║  Time: {elapsed.TotalMilliseconds:F0} ms  |  Threads: {threads}");
                Log($"╚══════════════════════════════╝");
            });
        }

        // ──────────────────────────────────────────────────────────
        //  LOG HELPER
        // ──────────────────────────────────────────────────────────

        private void Log(string message)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(() => Log(message));
                return;
            }
            TxtLog.AppendText($"[{DateTime.Now:HH:mm:ss.fff}] {message}\n");
            LogScroller.ScrollToBottom();
        }
    }
}
