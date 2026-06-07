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
        private GroupBox grpPassword, grpAttack, grpResults;
        private Label lblInfo, lblHash, lblThreads, lblAttempts, lblElapsed;
        private TextBox txtPassword;
        private Button btnGenerate, btnCustom, btnStartStop, btnCompare, btnSave, btnClear;
        private ProgressBar progressBar;
        private RichTextBox txtLog;

        public MainForm()
        {
            InitializeComponent();
            BuildUI();
            Setup();
        }

        private void BuildUI()
        {
            this.Text = "Password Cracker";
            this.Size = new Size(720, 700);
            this.BackColor = Color.FromArgb(30, 30, 40);
            this.ForeColor = Color.White;
            this.Font = new Font("Segoe UI", 9f);
            this.StartPosition = FormStartPosition.CenterScreen;

            // password group
            grpPassword = MakeGroup("Password Setup", 10, 10, 685, 130);

            lblInfo = MakeLabel("No password set yet.", 10, 25, 660, 20);
            lblInfo.ForeColor = Color.LightYellow;

            lblHash = MakeLabel("Hash: —", 10, 50, 660, 20);
            lblHash.ForeColor = Color.LightGreen;
            lblHash.Font = new Font("Consolas", 8f);

            txtPassword = new TextBox
            {
                Left = 10,
                Top = 78,
                Width = 200,
                MaxLength = 5,
                BackColor = Color.FromArgb(50, 50, 65),
                ForeColor = Color.White
            };

            btnCustom = MakeButton("Use Custom", 220, 76, 120, 28, Color.FromArgb(70, 100, 160));
            btnCustom.Click += BtnCustom_Click;

            btnGenerate = MakeButton("Generate Random", 350, 76, 150, 28, Color.FromArgb(70, 130, 80));
            btnGenerate.Click += BtnGenerate_Click;

            grpPassword.Controls.AddRange(new Control[] {
                lblInfo, lblHash, txtPassword, btnCustom, btnGenerate
            });

            // attack group
            grpAttack = MakeGroup("Brute Force Attack", 10, 150, 685, 160);

            lblThreads = MakeLabel("Threads: —", 10, 25, 500, 18);
            lblThreads.ForeColor = Color.LightBlue;

            btnStartStop = MakeButton("START ATTACK", 10, 50, 180, 36, Color.FromArgb(40, 160, 80));
            btnStartStop.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            btnStartStop.Click += BtnStartStop_Click;

            lblAttempts = MakeLabel("Attempts: 0", 205, 60, 220, 18);
            lblAttempts.ForeColor = Color.Orange;
            lblAttempts.Font = new Font("Consolas", 9f);

            lblElapsed = MakeLabel("Time: 00:00:00", 440, 60, 200, 18);
            lblElapsed.ForeColor = Color.Yellow;
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
                lblThreads, btnStartStop, lblAttempts, lblElapsed, progressBar
            });

            // results group
            grpResults = MakeGroup("Results", 10, 320, 685, 320);

            txtLog = new RichTextBox
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
            txtLog.AppendText("Ready. Generate a password and click START ATTACK.\r\n");

            btnCompare = MakeButton("Run Comparison", 10, 265, 150, 28, Color.FromArgb(120, 60, 160));
            btnCompare.Click += BtnCompare_Click;

            btnSave = MakeButton("Save Log", 170, 265, 110, 28, Color.FromArgb(60, 100, 140));
            btnSave.Click += (s, e) => { _logger.SaveToFile(); Log("Log saved."); };

            btnClear = MakeButton("Clear", 290, 265, 90, 28, Color.FromArgb(130, 50, 50));
            btnClear.Click += (s, e) => { txtLog.Clear(); _logger.Clear(); };

            grpResults.Controls.AddRange(new Control[] {
                txtLog, btnCompare, btnSave, btnClear
            });

            this.Controls.AddRange(new Control[] { grpPassword, grpAttack, grpResults });
        }

        private void Setup()
        {
            _passwordManager = new PasswordManager();
            _logger = new PerformanceLogger();

            // timer updates the elapsed time every 500ms
            _timer = new System.Windows.Forms.Timer();
            _timer.Interval = 500;
            _timer.Tick += (s, e) =>
            {
                lblElapsed.Text = "Time: " + (DateTime.Now - _startTime).ToString(@"hh\:mm\:ss");
                lblAttempts.Text = "Attempts: " + _engine?.TotalAttempts.ToString("N0");
            };

            int threads = Environment.ProcessorCount - 1;
            if (threads < 1) threads = 1;
            lblThreads.Text = "Using " + threads + " thread(s)  |  CPU cores: " + Environment.ProcessorCount;
        }

        // generate random password button
        private void BtnGenerate_Click(object sender, EventArgs e)
        {
            if (_isRunning) return;

            _passwordManager.GenerateNewPassword();
            lblInfo.Text = "Password: '" + _passwordManager.PlainPassword + "'  |  Length: " + _passwordManager.PasswordLength;
            lblHash.Text = "Hash: " + _passwordManager.HashedPassword;
            Log("Generated password: '" + _passwordManager.PlainPassword + "'");
        }

        // set custom password button
        private void BtnCustom_Click(object sender, EventArgs e)
        {
            if (_isRunning) return;

            bool ok = _passwordManager.SetCustomPassword(txtPassword.Text.Trim());
            if (ok)
            {
                lblInfo.Text = "Password: '" + _passwordManager.PlainPassword + "'  |  Length: " + _passwordManager.PasswordLength;
                lblHash.Text = "Hash: " + _passwordManager.HashedPassword;
                Log("Custom password set: '" + _passwordManager.PlainPassword + "'");
            }
            else
            {
                MessageBox.Show("Password must be 4-5 characters, letters and digits only.");
            }
        }

        // start or stop the attack
        private void BtnStartStop_Click(object sender, EventArgs e)
        {
            if (_passwordManager.HashedPassword == null)
            {
                MessageBox.Show("Please generate a password first!");
                return;
            }

            if (_isRunning)
            {
                _engine.Stop();
                StopAttack("Stopped by user.");
            }
            else
            {
                StartAttack();
            }
        }

        // run single vs multi thread comparison
        private void BtnCompare_Click(object sender, EventArgs e)
        {
            if (_passwordManager.HashedPassword == null || _isRunning) return;

            Log("Running single-thread test, please wait...");
            btnCompare.Enabled = false;

            string hash = _passwordManager.HashedPassword;
            string plain = _passwordManager.PlainPassword;

            // run in background so GUI doesnt freeze
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

        private void StartAttack()
        {
            _isRunning = true;
            _startTime = DateTime.Now;

            _engine = new BruteForceEngine(_passwordManager.HashedPassword, PasswordManager.GetCharset());

            // when progress updates, show it in the label
            _engine.OnProgressUpdate += (attempts) =>
            {
                Invoke(new Action(() =>
                {
                    lblAttempts.Text = "Attempts: " + attempts.ToString("N0");
                }));
            };

            // when password is found
            _engine.OnPasswordFound += (pwd, elapsed) =>
            {
                Invoke(new Action(() =>
                {
                    _logger.LogMultiThread(pwd, elapsed, _engine.TotalAttempts, _engine.ThreadCount);
                    Log("PASSWORD FOUND: '" + pwd + "'");
                    Log("Time: " + elapsed.TotalSeconds.ToString("F3") + "s  |  Attempts: " + _engine.TotalAttempts.ToString("N0"));
                    Log(_logger.GetComparisonReport());
                    StopAttack("Attack finished!");
                }));
            };

            // when stopped without finding
            _engine.OnStopped += () =>
            {
                Invoke(new Action(() => StopAttack("Stopped.")));
            };

            btnStartStop.Text = "STOP ATTACK";
            btnStartStop.BackColor = Color.FromArgb(180, 50, 50);
            progressBar.MarqueeAnimationSpeed = 30;
            _timer.Start();

            Log("Attack started! Using " + _engine.ThreadCount + " threads, searching length 1 to 6...");
            _engine.StartMultiThreaded();
        }

        private void StopAttack(string message)
        {
            _isRunning = false;
            _timer.Stop();
            btnStartStop.Text = "START ATTACK";
            btnStartStop.BackColor = Color.FromArgb(40, 160, 80);
            progressBar.MarqueeAnimationSpeed = 0;
            Log(message);
        }

        // add a line to the log box
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

        // helper to create a group box
        private GroupBox MakeGroup(string title, int x, int y, int w, int h)
        {
            return new GroupBox
            {
                Text = title,
                Left = x,
                Top = y,
                Width = w,
                Height = h,
                ForeColor = Color.LightBlue,
                BackColor = Color.FromArgb(38, 38, 52),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };
        }

        // helper to create a label
        private Label MakeLabel(string text, int x, int y, int w, int h)
        {
            return new Label
            {
                Text = text,
                Left = x,
                Top = y,
                Width = w,
                Height = h,
                ForeColor = Color.White,
                AutoSize = false
            };
        }

        // helper to create a button
        private Button MakeButton(string text, int x, int y, int w, int h, Color bg)
        {
            return new Button
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
}