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

        private System.Windows.Forms.Timer _timer;
        private DateTime _startTime;
        private bool _isRunning = false;

        // controls
        private Label lblPassword, lblHash, lblThreads, lblAttempts, lblElapsed, lblStatus;
        private TextBox txtCustomPassword, txtLog;
        private Button btnGenerate, btnCustom, btnStart, btnStop, btnCompare, btnClear;
        private ProgressBar progressBar;

        public MainForm()
        {
            InitializeComponent();
            BuildUI();
            Setup();
        }

        private void BuildUI()
        {
            this.Text = "Password Cracker";
            this.Size = new Size(600, 620);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;

            // --- password section ---
            Label lblSection1 = new Label();
            lblSection1.Text = "--- Password Setup ---";
            lblSection1.Left = 10;
            lblSection1.Top = 10;
            lblSection1.Width = 560;
            lblSection1.Font = new Font("Arial", 10, FontStyle.Bold);
            this.Controls.Add(lblSection1);

            lblPassword = new Label();
            lblPassword.Text = "Password: not set";
            lblPassword.Left = 10;
            lblPassword.Top = 35;
            lblPassword.Width = 560;
            this.Controls.Add(lblPassword);

            lblHash = new Label();
            lblHash.Text = "Hash: -";
            lblHash.Left = 10;
            lblHash.Top = 55;
            lblHash.Width = 560;
            lblHash.Font = new Font("Courier New", 7);
            this.Controls.Add(lblHash);

            Label lblCustom = new Label();
            lblCustom.Text = "Custom password (4-5 chars):";
            lblCustom.Left = 10;
            lblCustom.Top = 80;
            lblCustom.Width = 200;
            this.Controls.Add(lblCustom);

            txtCustomPassword = new TextBox();
            txtCustomPassword.Left = 210;
            txtCustomPassword.Top = 78;
            txtCustomPassword.Width = 100;
            txtCustomPassword.MaxLength = 5;
            this.Controls.Add(txtCustomPassword);

            btnCustom = new Button();
            btnCustom.Text = "Set Password";
            btnCustom.Left = 320;
            btnCustom.Top = 76;
            btnCustom.Width = 110;
            btnCustom.Click += BtnCustom_Click;
            this.Controls.Add(btnCustom);

            btnGenerate = new Button();
            btnGenerate.Text = "Generate Random";
            btnGenerate.Left = 440;
            btnGenerate.Top = 76;
            btnGenerate.Width = 130;
            btnGenerate.Click += BtnGenerate_Click;
            this.Controls.Add(btnGenerate);

            // --- attack section ---
            Label lblSection2 = new Label();
            lblSection2.Text = "--- Brute Force Attack ---";
            lblSection2.Left = 10;
            lblSection2.Top = 115;
            lblSection2.Width = 560;
            lblSection2.Font = new Font("Arial", 10, FontStyle.Bold);
            this.Controls.Add(lblSection2);

            lblThreads = new Label();
            lblThreads.Text = "Threads: -";
            lblThreads.Left = 10;
            lblThreads.Top = 140;
            lblThreads.Width = 400;
            this.Controls.Add(lblThreads);

            btnStart = new Button();
            btnStart.Text = "Start Attack";
            btnStart.Left = 10;
            btnStart.Top = 165;
            btnStart.Width = 120;
            btnStart.BackColor = Color.LightGreen;
            btnStart.Click += BtnStart_Click;
            this.Controls.Add(btnStart);

            btnStop = new Button();
            btnStop.Text = "Stop Attack";
            btnStop.Left = 140;
            btnStop.Top = 165;
            btnStop.Width = 120;
            btnStop.BackColor = Color.LightCoral;
            btnStop.Enabled = false;
            btnStop.Click += BtnStop_Click;
            this.Controls.Add(btnStop);

            lblAttempts = new Label();
            lblAttempts.Text = "Attempts: 0";
            lblAttempts.Left = 10;
            lblAttempts.Top = 200;
            lblAttempts.Width = 200;
            this.Controls.Add(lblAttempts);

            lblElapsed = new Label();
            lblElapsed.Text = "Time: 00:00:00";
            lblElapsed.Left = 220;
            lblElapsed.Top = 200;
            lblElapsed.Width = 200;
            this.Controls.Add(lblElapsed);

            progressBar = new ProgressBar();
            progressBar.Left = 10;
            progressBar.Top = 225;
            progressBar.Width = 560;
            progressBar.Height = 20;
            progressBar.Style = ProgressBarStyle.Marquee;
            progressBar.MarqueeAnimationSpeed = 0;
            this.Controls.Add(progressBar);

            lblStatus = new Label();
            lblStatus.Text = "Status: Ready";
            lblStatus.Left = 10;
            lblStatus.Top = 250;
            lblStatus.Width = 560;
            lblStatus.ForeColor = Color.Blue;
            this.Controls.Add(lblStatus);

            // --- results section ---
            Label lblSection3 = new Label();
            lblSection3.Text = "--- Results and Log ---";
            lblSection3.Left = 10;
            lblSection3.Top = 275;
            lblSection3.Width = 560;
            lblSection3.Font = new Font("Arial", 10, FontStyle.Bold);
            this.Controls.Add(lblSection3);

            txtLog = new TextBox();
            txtLog.Left = 10;
            txtLog.Top = 300;
            txtLog.Width = 560;
            txtLog.Height = 220;
            txtLog.Multiline = true;
            txtLog.ScrollBars = ScrollBars.Vertical;
            txtLog.ReadOnly = true;
            txtLog.BackColor = Color.White;
            txtLog.Font = new Font("Courier New", 8);
            txtLog.Text = "Welcome! Generate a password and click Start Attack.\r\n";
            this.Controls.Add(txtLog);

            btnCompare = new Button();
            btnCompare.Text = "Run Comparison";
            btnCompare.Left = 10;
            btnCompare.Top = 530;
            btnCompare.Width = 130;
            btnCompare.Click += BtnCompare_Click;
            this.Controls.Add(btnCompare);

            btnClear = new Button();
            btnClear.Text = "Clear Log";
            btnClear.Left = 150;
            btnClear.Top = 530;
            btnClear.Width = 100;
            btnClear.Click += (s, e) => { txtLog.Clear(); _logger.Clear(); };
            this.Controls.Add(btnClear);
        }

        private void Setup()
        {
            _passwordManager = new PasswordManager();
            _logger = new PerformanceLogger();

            _timer = new System.Windows.Forms.Timer();
            _timer.Interval = 500;
            _timer.Tick += (s, e) =>
            {
                lblElapsed.Text = "Time: " + (DateTime.Now - _startTime).ToString(@"hh\:mm\:ss");
                lblAttempts.Text = "Attempts: " + _engine?.TotalAttempts.ToString("N0");
            };

            int threads = Environment.ProcessorCount - 1;
            if (threads < 1) threads = 1;
            lblThreads.Text = "Threads: " + threads + " (CPU cores: " + Environment.ProcessorCount + ", using cores - 1)";
        }

        private void BtnGenerate_Click(object sender, EventArgs e)
        {
            if (_isRunning) return;
            _passwordManager.GenerateNewPassword();
            lblPassword.Text = "Password: '" + _passwordManager.PlainPassword + "' (length " + _passwordManager.PasswordLength + ")";
            lblHash.Text = "Hash: " + _passwordManager.HashedPassword;
            Log("Generated password: '" + _passwordManager.PlainPassword + "'");
        }

        private void BtnCustom_Click(object sender, EventArgs e)
        {
            if (_isRunning) return;
            bool ok = _passwordManager.SetCustomPassword(txtCustomPassword.Text.Trim());
            if (ok)
            {
                lblPassword.Text = "Password: '" + _passwordManager.PlainPassword + "' (length " + _passwordManager.PasswordLength + ")";
                lblHash.Text = "Hash: " + _passwordManager.HashedPassword;
                Log("Custom password set: '" + _passwordManager.PlainPassword + "'");
            }
            else
            {
                MessageBox.Show("Password must be 4-5 characters, letters and digits only.");
            }
        }

        private void BtnStart_Click(object sender, EventArgs e)
        {
            if (_passwordManager.HashedPassword == null)
            {
                MessageBox.Show("Please generate or set a password first!");
                return;
            }

            _isRunning = true;
            _startTime = DateTime.Now;
            btnStart.Enabled = false;
            btnStop.Enabled = true;
            progressBar.MarqueeAnimationSpeed = 30;
            lblStatus.Text = "Status: Attacking...";
            lblStatus.ForeColor = Color.Red;

            _engine = new BruteForceEngine(_passwordManager.HashedPassword, PasswordManager.GetCharset());

            _engine.OnProgressUpdate += (attempts) =>
            {
                Invoke(new Action(() =>
                {
                    lblAttempts.Text = "Attempts: " + attempts.ToString("N0");
                }));
            };

            _engine.OnPasswordFound += (pwd, elapsed) =>
            {
                Invoke(new Action(() =>
                {
                    _logger.LogMultiThread(pwd, elapsed, _engine.TotalAttempts, _engine.ThreadCount);
                    Log("PASSWORD FOUND: '" + pwd + "'");
                    Log("Time: " + elapsed.TotalSeconds.ToString("F3") + "s | Attempts: " + _engine.TotalAttempts.ToString("N0"));
                    Log(_logger.GetComparisonReport());
                    StopUI("Done! Password found.");
                }));
            };

            _engine.OnStopped += () =>
            {
                Invoke(new Action(() => StopUI("Stopped.")));
            };

            _timer.Start();
            Log("Attack started! Using " + _engine.ThreadCount + " threads, searching length 1 to 6...");
            _engine.StartMultiThreaded();
        }

        private void BtnStop_Click(object sender, EventArgs e)
        {
            _engine?.Stop();
            StopUI("Stopped by user.");
        }

        private void BtnCompare_Click(object sender, EventArgs e)
        {
            if (_passwordManager.HashedPassword == null || _isRunning) return;

            Log("Running single-thread test, please wait...");
            btnCompare.Enabled = false;

            string hash = _passwordManager.HashedPassword;
            string plain = _passwordManager.PlainPassword;

            Thread t = new Thread(() =>
            {
                BruteForceEngine temp = new BruteForceEngine(hash, PasswordManager.GetCharset());
                TimeSpan time = temp.RunSingleThreaded(hash);

                Invoke(new Action(() =>
                {
                    _logger.LogSingleThread(plain, time, 0);
                    Log("Single-thread time: " + time.TotalSeconds.ToString("F3") + "s");
                    Log(_logger.GetComparisonReport());
                    btnCompare.Enabled = true;
                }));
            });
            t.IsBackground = true;
            t.Start();
        }

        private void StopUI(string message)
        {
            _isRunning = false;
            _timer.Stop();
            btnStart.Enabled = true;
            btnStop.Enabled = false;
            progressBar.MarqueeAnimationSpeed = 0;
            lblStatus.Text = "Status: " + message;
            lblStatus.ForeColor = Color.Blue;
            Log(message);
        }

        private void Log(string message)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => Log(message)));
                return;
            }
            txtLog.AppendText("[" + DateTime.Now.ToString("HH:mm:ss") + "] " + message + "\r\n");
            txtLog.ScrollToCaret();
        }
    }
}