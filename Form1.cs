using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using PasswordCracker.Classes;

namespace PasswordCracker
{
    public partial class MainForm : Form
    {
        private PasswordManager _passwordManager;
        private BruteForceEngine _engine;
        private PerformanceLogger _logger;

        private System.Windows.Forms.Timer _uiTimer;
        private DateTime _startTime;
        private bool _isRunning = false;

        // ── Controls ──
        private GroupBox grpPassword, grpAttack, grpResults;
        private Label lblPasswordInfo, lblHashDisplay, lblThreadCount, lblAttempts, lblElapsed;
        private TextBox txtCustomPassword;
        private Button btnGeneratePassword, btnSetCustom, btnStartStop, btnRunComparison, btnSaveLog, btnClear;
        private ProgressBar progressBar;
        private RichTextBox txtResults;

        public MainForm()
        {
            InitializeComponent();
            BuildUI();
            InitWorkers();
        }

        private void BuildUI()
        {
            this.Text = "Password Cracker - Brute Force Demo";
            this.Size = new Size(720, 700);
            this.MinimumSize = new Size(700, 650);
            this.BackColor = Color.FromArgb(30, 30, 40);
            this.ForeColor = Color.White;
            this.Font = new Font("Segoe UI", 9f);
            this.StartPosition = FormStartPosition.CenterScreen;

            // ── Password Group ──
            grpPassword = MakeGroup("Password Setup", 10, 10, 685, 130);

            lblPasswordInfo = MakeLabel("No password set. Generate one or type your own (4-5 chars).", 10, 25, 660, 20);
            lblPasswordInfo.ForeColor = Color.LightYellow;

            lblHashDisplay = MakeLabel("SHA256 Hash: —", 10, 50, 660, 20);
            lblHashDisplay.ForeColor = Color.FromArgb(100, 200, 100);
            lblHashDisplay.Font = new Font("Consolas", 8f);

            txtCustomPassword = new TextBox
            {
                Left = 10,
                Top = 78,
                Width = 200,
                MaxLength = 5,
                BackColor = Color.FromArgb(50, 50, 65),
                ForeColor = Color.White,
            };

            btnSetCustom = MakeButton("Use Custom", 220, 76, 130, 28, Color.FromArgb(70, 100, 160));
            btnSetCustom.Click += BtnSetCustom_Click;

            btnGeneratePassword = MakeButton("Generate Random", 360, 76, 150, 28, Color.FromArgb(70, 130, 80));
            btnGeneratePassword.Click += BtnGeneratePassword_Click;

            grpPassword.Controls.AddRange(new Control[] {
                lblPasswordInfo, lblHashDisplay, txtCustomPassword, btnSetCustom, btnGeneratePassword
            });

            // ── Attack Group ──
            grpAttack = MakeGroup("Brute Force Attack", 10, 150, 685, 160);

            lblThreadCount = MakeLabel("Threads: —", 10, 25, 500, 18);
            lblThreadCount.ForeColor = Color.FromArgb(150, 200, 255);

            btnStartStop = MakeButton("START ATTACK", 10, 50, 180, 36, Color.FromArgb(40, 160, 80));
            btnStartStop.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            btnStartStop.Click += BtnStartStop_Click;

            lblAttempts = MakeLabel("Attempts: 0", 205, 60, 220, 18);
            lblAttempts.ForeColor = Color.Orange;
            lblAttempts.Font = new Font("Consolas", 9f);

            lblElapsed = MakeLabel("Elapsed: 00:00:00", 440, 60, 200, 18);
            lblElapsed.ForeColor = Color.FromArgb(200, 200, 100);
            lblElapsed.Font = new Font("Consolas", 9f);

            progressBar = new ProgressBar
            {
                Left = 10,
                Top = 100,
                Width = 660,
                Height = 22,
                Style = ProgressBarStyle.Marquee,
                MarqueeAnimationSpeed = 0
            };

            grpAttack.Controls.AddRange(new Control[] {
                lblThreadCount, btnStartStop, lblAttempts, lblElapsed, progressBar
            });

            // ── Results Group ──
            grpResults = MakeGroup("Results and Performance Log", 10, 320, 685, 320);

            txtResults = new RichTextBox
            {
                Left = 10,
                Top = 25,
                Width = 660,
                Height = 230,
                BackColor = Color.FromArgb(20, 20, 30),
                ForeColor = Color.LightGreen,
                Font = new Font("Consolas", 8.5f),
                ReadOnly = true,
                ScrollBars = RichTextBoxScrollBars.Vertical
            };
            txtResults.AppendText("Welcome! Generate a password above then click START ATTACK.\r\n");

            btnRunComparison = MakeButton("Run Comparison", 10, 265, 150, 28, Color.FromArgb(120, 60, 160));
            btnRunComparison.Click += BtnRunComparison_Click;

            btnSaveLog = MakeButton("Save Log", 170, 265, 110, 28, Color.FromArgb(60, 100, 140));
            btnSaveLog.Click += (s, e) => { _logger.SaveToFile(); Log("Log saved to Desktop."); };

            btnClear = MakeButton("Clear", 290, 265, 90, 28, Color.FromArgb(130, 50, 50));
            btnClear.Click += (s, e) => { txtResults.Clear(); _logger.Clear(); };

            grpResults.Controls.AddRange(new Control[] {
                txtResults, btnRunComparison, btnSaveLog, btnClear
            });

            this.Controls.AddRange(new Control[] { grpPassword, grpAttack, grpResults });
        }

        private void InitWorkers()
        {
            _passwordManager = new PasswordManager();
            _logger = new PerformanceLogger();

            _uiTimer = new System.Windows.Forms.Timer { Interval = 500 };
            _uiTimer.Tick += (s, e) =>
            {
                lblElapsed.Text = $"Elapsed: {DateTime.Now - _startTime:hh\\:mm\\:ss}";
                lblAttempts.Text = $"Attempts: {_engine?.TotalAttempts:N0}";
            };

            int threads = Math.Max(1, Environment.ProcessorCount - 1);
            lblThreadCount.Text = $"Using {threads} thread(s)  |  CPU cores: {Environment.ProcessorCount}  |  Max = cores - 1";
        }

        // ── Button Handlers ──

        private void BtnGeneratePassword_Click(object sender, EventArgs e)
        {
            if (_isRunning) return;
            _passwordManager.GenerateNewPassword();
            UpdatePasswordDisplay();
            Log($"Generated password: '{_passwordManager.PlainPassword}' (length {_passwordManager.PasswordLength})");
        }

        private void BtnSetCustom_Click(object sender, EventArgs e)
        {
            if (_isRunning) return;
            if (_passwordManager.SetCustomPassword(txtCustomPassword.Text.Trim()))
            {
                UpdatePasswordDisplay();
                Log($"Custom password set: '{_passwordManager.PlainPassword}'");
            }
            else
            {
                MessageBox.Show("Password must be 4-5 characters, letters and digits only.",
                    "Invalid", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void BtnStartStop_Click(object sender, EventArgs e)
        {
            if (_passwordManager.HashedPassword == null)
            {
                MessageBox.Show("Please generate or set a password first!",
                    "No Password", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (_isRunning)
            {
                _engine?.Stop();
                StopUI("Attack stopped by user.");
            }
            else
            {
                StartAttack();
            }
        }

        private void BtnRunComparison_Click(object sender, EventArgs e)
        {
            if (_passwordManager.HashedPassword == null || _isRunning) return;

            Log("Running single-thread comparison, please wait...");
            btnRunComparison.Enabled = false;
            string hash = _passwordManager.HashedPassword;
            string plain = _passwordManager.PlainPassword;

            Thread t = new Thread(() =>
            {
                var tempEngine = new BruteForceEngine(hash, PasswordManager.GetCharset());
                TimeSpan singleTime = tempEngine.RunSingleThreaded(hash);
                Invoke(new Action(() =>
                {
                    _logger.LogSingleThread(plain, singleTime, 0);
                    Log($"Single-thread finished in {singleTime.TotalSeconds:F3}s");
                    Log(_logger.GetComparisonReport());
                    btnRunComparison.Enabled = true;
                }));
            });
            t.IsBackground = true;
            t.Start();
        }

        // ── Attack Logic ──

        private void StartAttack()
        {
            _isRunning = true;
            _startTime = DateTime.Now;

            _engine = new BruteForceEngine(_passwordManager.HashedPassword, PasswordManager.GetCharset());
            _engine.OnProgressUpdate += attempts =>
                Invoke(new Action(() => lblAttempts.Text = $"Attempts: {attempts:N0}"));
            _engine.OnPasswordFound += (pwd, elapsed) =>
                Invoke(new Action(() =>
                {
                    _logger.LogMultiThread(pwd, elapsed, _engine.TotalAttempts, _engine.ThreadCount);
                    StopUI($"FOUND: '{pwd}'  in {elapsed.TotalSeconds:F3}s");
                    Log($"PASSWORD FOUND: '{pwd}'");
                    Log($"Time: {elapsed.TotalSeconds:F3}s  |  Attempts: {_engine.TotalAttempts:N0}  |  Threads: {_engine.ThreadCount}");
                    Log(_logger.GetComparisonReport());
                }));
            _engine.OnStopped += () => Invoke(new Action(() => StopUI("Attack stopped.")));

            btnStartStop.Text = "STOP ATTACK";
            btnStartStop.BackColor = Color.FromArgb(180, 50, 50);
            progressBar.MarqueeAnimationSpeed = 30;
            _uiTimer.Start();

            Log($"Attack started! Searching from length 1 to 6 using {_engine.ThreadCount} thread(s)...");
            _engine.StartMultiThreaded();
        }

        private void StopUI(string message)
        {
            _isRunning = false;
            _uiTimer.Stop();
            btnStartStop.Text = "START ATTACK";
            btnStartStop.BackColor = Color.FromArgb(40, 160, 80);
            progressBar.MarqueeAnimationSpeed = 0;
            Log(message);
        }

        private void UpdatePasswordDisplay()
        {
            lblPasswordInfo.Text = $"Password: '{_passwordManager.PlainPassword}'  |  Length: {_passwordManager.PasswordLength}  |  (Engine does NOT know the length!)";
            lblHashDisplay.Text = $"SHA256 Hash: {_passwordManager.HashedPassword}";
        }

        private void Log(string message)
        {
            if (InvokeRequired) { Invoke(new Action(() => Log(message))); return; }
            txtResults.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\r\n");
            txtResults.ScrollToCaret();
        }

        // ── Control Factories ──

        private GroupBox MakeGroup(string title, int x, int y, int w, int h) =>
            new GroupBox
            {
                Text = title,
                Left = x,
                Top = y,
                Width = w,
                Height = h,
                ForeColor = Color.FromArgb(150, 200, 255),
                BackColor = Color.FromArgb(38, 38, 52),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };

        private Label MakeLabel(string text, int x, int y, int w, int h) =>
            new Label
            {
                Text = text,
                Left = x,
                Top = y,
                Width = w,
                Height = h,
                ForeColor = Color.White,
                AutoSize = false
            };

        private Button MakeButton(string text, int x, int y, int w, int h, Color bg) =>
            new Button
            {
                Text = text,
                Left = x,
                Top = y,
                Width = w,
                Height = h,
                BackColor = bg,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
    }
}