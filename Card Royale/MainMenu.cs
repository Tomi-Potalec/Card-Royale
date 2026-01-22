using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using WMPLib;



namespace Card_Royale
{
    public partial class MainMenu : Form
    {
        private Panel panelDifficulty;
        private string selectedDifficulty = "Beginner";

        private Button btnBeginner, btnEasy, btnModerate, btnExpert, btnPro, btnMaster;
        private Button btnPlayDifficulty, btnDifficultyBack;

        private Panel panelDarkBackground;

        private bool isOnSolitairePanel = false;

        private SolitairePanel solitairePanel;

        private Button btnCloseOverlay;

        private Panel selectorBar;

        private Rectangle btnSolitaireOriginalRect;
        private Rectangle btnExitOriginalRect;
        private Size formOriginalSize;

        private Button[] gameButtons;
        private string[] gameNames;
        private Size gameBtnSize;
        private Font gameFont;
        private int padding = 25;

        private Button btnSettings;
        private Rectangle btnSettingsOriginalRect;


        private Panel panelSettings;
        private Panel panelSettingsDark;
        private Button btnSettingsClose;
        private ComboBox cbTheme;
        private ComboBox cbSelectMusic;
        private TrackBar sliderVolume;
        private ComboBox cbTableBackground;
        private ComboBox cbCardBack;

        private WindowsMediaPlayer musicPlayer = new WindowsMediaPlayer();

        private bool difficultySelected = false;


        private Dictionary<Button, Rectangle> originalButtonRects = new Dictionary<Button, Rectangle>();

        public MainMenu()
        {
            InitializeComponent();

            // Inicijaliziranje nizova i veličina tipki za igru
            gameButtons = new Button[] { btnSolitaire, btnSpider, btnKlondike, btnFreeCell, btnPyramid, btnHearts };
            gameNames = new string[] { "", "Spider", "Klondike", "FreeCell", "Pyramid", "Hearts" };
            gameBtnSize = new Size(150, 170);
            gameFont = new Font("Comic Sans MS", 10, FontStyle.Bold);
            padding = 25;

            SetupGameButtons(); // sada koristi ova polja na razini klase

            panelMainMenu.Controls.Add(selectorBar);
            selectorBar.BringToFront();

            this.BackColor = ColorTranslator.FromHtml("#00512c");

            SetupSolitaireButton();
            SetupExitButton();
            SetupSelectorBar();

            btnSolitaire.Click += BtnSolitaire_Click;
            btnExit.Click += BtnExit_Click;

            panelMainMenu.BringToFront();

            panelDarkBackground = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = ColorTranslator.FromHtml("#00512c"),
                Visible = false
            };
            this.Controls.Add(panelDarkBackground);
            panelDarkBackground.BringToFront();

            SetupDifficultyOverlay();
        }

        private void MainMenu_Load(object sender, EventArgs e)
        {
            this.Icon = new Icon("card_royale.ico");
            this.Text = "Card Royale";

            // Prvo učitajte postavke
            SettingsManager.Instance.Load();
            PlaySelectedMusic(SettingsManager.Instance.SelectMusic);


            formOriginalSize = this.Size;
            btnSolitaireOriginalRect = new Rectangle(btnSolitaire.Location, btnSolitaire.Size);
            btnExitOriginalRect = new Rectangle(btnExit.Location, btnExit.Size);

            PositionExitButton();
            SetupSettingsPanel(); // sada će ovo ispravno unaprijed odabrati TableBackground i CardBack

            // Učitaj druge kontrole korisničkog sučelja
            cbTheme.SelectedItem = SettingsManager.Instance.Theme;
            cbSelectMusic.SelectedItem = SettingsManager.Instance.SelectMusic;
            sliderVolume.Value = SettingsManager.Instance.MusicVolume;
            musicPlayer.settings.volume = SettingsManager.Instance.MusicVolume;



            this.Resize += MainMenu_Resize;
            this.Resize += (s, ev) => PositionSelectorBar();
            ResizeGameButtons();
        }

        private void MainMenu_Resize(object sender, EventArgs e)
        {
            foreach (var kvp in originalButtonRects)
                ResizeControl(kvp.Value, kvp.Key);

            PositionSelectorBar();
            ResizeGameButtons();
            PositionExitButton(); // drži gumb za izlaz usidrenim

            if (panelSettings != null)
            {
                panelSettings.Left = (this.ClientSize.Width - panelSettings.Width) / 2;
                panelSettings.Top = (this.ClientSize.Height - panelSettings.Height) / 2;
            }

        }

        private void ResizeControl(Rectangle originalRect, Control control)
        {
            float xRatio = (float)this.Width / formOriginalSize.Width;
            float yRatio = (float)this.Height / formOriginalSize.Height;

            int newX = (int)(originalRect.X * xRatio);
            int newY = (int)(originalRect.Y * yRatio);
            int newWidth = (int)(originalRect.Width * xRatio);
            int newHeight = (int)(originalRect.Height * yRatio);

            CenterDifficultyOverlay();

            control.Location = new Point(newX, newY);
            control.Size = new Size(newWidth, newHeight);

            
        }

        private void PositionExitButton()
        {
            float xRatio = (float)this.ClientSize.Width / formOriginalSize.Width;
            float yRatio = (float)this.ClientSize.Height / formOriginalSize.Height;

            int newWidth = (int)(btnExitOriginalRect.Width * xRatio);
            int newHeight = (int)(btnExitOriginalRect.Height * yRatio);

            btnExit.Size = new Size(newWidth, newHeight);
            btnSettings.Size = new Size(newWidth, newHeight); // iste veličine

            btnExit.Location = new Point(
                panelMainMenu.ClientSize.Width - btnExit.Width - 20,
                panelMainMenu.ClientSize.Height - btnExit.Height - 20
            );

            btnSettings.Location = new Point(
                btnExit.Left - btnSettings.Width - 15,
                btnExit.Top
            );
        }

        private void PositionSelectorBar()
        {
            if (selectorBar == null || btnSolitaire == null) return;

            // Izračunaj koordinate u odnosu na klijentsko područje scrollPanelGamesa
            // btnSolitaire.Location je relativan u odnosu na scrollPanelGames jer je njegov podređeni dio
            selectorBar.Width = btnSolitaire.Width;
            selectorBar.Height = 6;
            selectorBar.Left = btnSolitaire.Left;
            selectorBar.Top = btnSolitaire.Bottom + 4;
            selectorBar.Visible = false;

            // ažuriraj zaobljene kutove nakon promjene veličine
            SetRoundedRegion(selectorBar, 4);
        }

        private void SetupSelectorBar()
        {
            if (selectorBar != null) return;

            selectorBar = new Panel
            {
                Size = new Size(btnSolitaire.Width, 6),
                BackColor = Color.LightGreen,
                Visible = true
            };

            SetRoundedRegion(selectorBar, 4);

            // Dodaj samo u scrollPanelGames
            scrollPanelGames.Controls.Add(selectorBar);
            selectorBar.BringToFront();
        }
        
        private void SetRoundedRegion(Control ctrl, int radius)
        {
            if (ctrl.Width <= 0 || ctrl.Height <= 0) return;
            using (var path = new System.Drawing.Drawing2D.GraphicsPath())
            {
                int r = Math.Max(0, radius);
                path.AddArc(0, 0, r * 2, r * 2, 180, 90);
                path.AddArc(ctrl.Width - r * 2, 0, r * 2, r * 2, 270, 90);
                path.AddArc(ctrl.Width - r * 2, ctrl.Height - r * 2, r * 2, r * 2, 0, 90);
                path.AddArc(0, ctrl.Height - r * 2, r * 2, r * 2, 90, 90);
                path.CloseFigure();
                ctrl.Region = new Region(path);
            }
        }

        private void SetupGameButtons()
        {
            scrollPanelGames.Controls.Clear();
            int x = padding;
            int y = 25;

            for (int i = 0; i < gameButtons.Length; i++)
            {
                Button btn = gameButtons[i];
                btn.Text = gameNames[i];
                btn.Size = gameBtnSize;
                btn.Location = new Point(x, y);
                btn.FlatStyle = FlatStyle.Flat;
                btn.FlatAppearance.BorderSize = 0;
                btn.BackColor = Color.FromArgb(10, 150, 70);
                btn.ForeColor = Color.White;
                btn.Font = gameFont;
                btn.Cursor = Cursors.Hand;
                btn.Visible = true;

                btn.Click -= BtnSolitaire_Click;
                btn.Click -= TempGameClick;

                if (btn == btnSolitaire)
                    btn.Click += BtnSolitaire_Click;
                else
                    btn.Click += TempGameClick;

                if (!originalButtonRects.ContainsKey(btn))
                    originalButtonRects[btn] = new Rectangle(btn.Location, btn.Size);

                scrollPanelGames.Controls.Add(btn);
                x += btn.Width + padding;
            }

            scrollPanelGames.AutoScroll = true;
            scrollPanelGames.HorizontalScroll.Enabled = true;
            scrollPanelGames.HorizontalScroll.Visible = true;
            scrollPanelGames.AutoScrollMinSize = new Size(x, 0);

            PositionSelectorBar();
        }

        private void TempGameClick(object sender, EventArgs e)
        {
            var b = sender as Button;
            if (b != null)
                MessageBox.Show($"{b.Text} igra je u razvoju.", "Upozorenje", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnSolitaire_Click(object sender, EventArgs e)
        {
            panelSolitaire.BringToFront();
            panelDarkBackground.Visible = true;
            panelDifficulty.Visible = true;
            panelDifficulty.BringToFront();
        }
        
        private void BtnExit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void SetupDifficultyOverlay()
        {
            panelDifficulty = new Panel
            {
                Size = new Size(400, 400),
                Location = new Point((this.ClientSize.Width - 400) / 2, (this.ClientSize.Height - 400) / 2),
                BackColor = Color.FromArgb(100, 0, 0, 0),
                Visible = false
            };
            this.Controls.Add(panelDifficulty);
            panelDifficulty.BringToFront();

            // Gumbi za težine
            btnBeginner = new Button { Text = "Beginner", Width = 150, Height = 30, Top = 20, Left = 125, Font = gameFont };
            btnEasy = new Button { Text = "Easy", Width = 150, Height = 30, Top = 60, Left = 125, Font = gameFont };
            btnModerate = new Button { Text = "Moderate", Width = 150, Height = 30, Top = 100, Left = 125, Font = gameFont };
            btnExpert = new Button { Text = "Expert", Width = 150, Height = 30, Top = 140, Left = 125, Font = gameFont };
            btnPro = new Button { Text = "Pro", Width = 150, Height = 30, Top = 180, Left = 125, Font = gameFont };
            btnMaster = new Button { Text = "Master", Width = 150, Height = 30, Top = 220, Left = 125, Font = gameFont };

            btnDifficultyBack = new Button { Text = "Back to Menu", Width = 150, Height = 30, Top = 270, Left = 40, Font = gameFont };
            btnPlayDifficulty = new Button { Text = "Play", Width = 150, Height = 30, Top = 270, Left = 210, Font = gameFont };
            btnPlayDifficulty.Enabled = false;

            panelDifficulty.Controls.AddRange(new Control[]
            {
        btnBeginner, btnEasy, btnModerate, btnExpert, btnPro, btnMaster, btnDifficultyBack, btnPlayDifficulty
            });


            // Označite odabir
            Action<Button> highlightSelected = (btn) =>
            {
                foreach (Control c in panelDifficulty.Controls)
                    if (c is Button b && b.Text != "Back to Menu" && b.Text != "Play")
                        b.BackColor = SystemColors.Control;

                btn.BackColor = Color.LightGreen;

                difficultySelected = true;
                btnPlayDifficulty.Enabled = true;   // ✅ Odobri "play"
            };

            // Gumb za zatvaranje preklapanja koristi manji font (može zadržati original)
            btnCloseOverlay = new Button
            {
                Text = "X",
                ForeColor = Color.Black,
                BackColor = ColorTranslator.FromHtml("#00512c"),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Arial", 10, FontStyle.Bold),
                Size = new Size(25, 25),
                Location = new Point(panelDifficulty.Width - 30, 5),
                Visible = false
            };
            btnCloseOverlay.FlatAppearance.BorderSize = 0;
            btnCloseOverlay.Click += BtnCloseOverlay_Click;
            panelDifficulty.Controls.Add(btnCloseOverlay);
            btnCloseOverlay.BringToFront();

            // Rukovatelji klikovima gumba
            btnBeginner.Click += (s, e) => { selectedDifficulty = "Beginner"; highlightSelected(btnBeginner); };
            btnEasy.Click += (s, e) => { selectedDifficulty = "Easy"; highlightSelected(btnEasy); };
            btnModerate.Click += (s, e) => { selectedDifficulty = "Moderate"; highlightSelected(btnModerate); };
            btnExpert.Click += (s, e) => { selectedDifficulty = "Expert"; highlightSelected(btnExpert); };
            btnPro.Click += (s, e) => { selectedDifficulty = "Pro"; highlightSelected(btnPro); };
            btnMaster.Click += (s, e) => { selectedDifficulty = "Master"; highlightSelected(btnMaster); };

            btnDifficultyBack.Click += (s, e) =>
            {
                panelDifficulty.Visible = false;
                panelDarkBackground.Visible = false;
                panelMainMenu.BringToFront();
            };

            btnPlayDifficulty.Click += (s, e) =>
            {
                if (!difficultySelected)
                {
                    MessageBox.Show(
                        "Please select a difficulty.",
                        "Difficulty Required",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning
                    );
                    return;
                }

                panelDifficulty.Visible = false;
                panelDarkBackground.Visible = false;
                StartSolitaire(selectedDifficulty);
            };

        }

        private void BtnCloseOverlay_Click(object sender, EventArgs e)
        {
            panelDifficulty.Visible = false;
            panelDarkBackground.Visible = false;

            //Nastavi timer ako SOlitairePanel učitan i aktivan
            var sp = panelSolitaire.Controls["solitairePanel"] as SolitairePanel;
            sp?.ResumeTimer();
        }

        private void ShowDifficultyPanel(bool fromNewDraft = false)
        {
            panelDarkBackground.Visible = true;
            panelDifficulty.Visible = true;
            panelDifficulty.BringToFront();

            //Postavi flag za indikaciju konteksta
            btnCloseOverlay.Visible = isOnSolitairePanel;
        }

        private void StartSolitaire(string difficulty)
        {
            if (solitairePanel == null)
            {
                solitairePanel = new SolitairePanel { Name = "solitairePanel", Dock = DockStyle.Fill };
                panelSolitaire.Controls.Add(solitairePanel);

                solitairePanel.ReturnToMenuRequested += () =>
                {
                    panelDifficulty.Visible = false;
                    panelDarkBackground.Visible = false;
                    panelMainMenu.BringToFront();
                    panelSolitaire.SendToBack();
                    isOnSolitairePanel = false;
                };

                solitairePanel.NewDraftRequested += () =>
                {
                    isOnSolitairePanel = true;
                    ShowDifficultyPanel(true);
                    solitairePanel.PauseTimer();
                };
            }

            panelSolitaire.Visible = true;             // ✅ učiniti ploču vidljivom
            panelSolitaire.BringToFront();             // ✅ postaviti samu ploču naprijed
            solitairePanel.BringToFront();             // ✅ postaviti unutarnju ploču naprijed

            solitairePanel.StartGame(difficulty);      // ✅ pokrenuti/resetirati igru
            solitairePanel.RestartTimer();
            isOnSolitairePanel = true;
        }
        
        private void SetupSolitaireButton()
        {
            btnSolitaire.Size = new Size(150, 150); // fixirane veličine
            btnSolitaire.FlatStyle = FlatStyle.Flat;
            btnSolitaire.FlatAppearance.BorderSize = 0;
            btnSolitaire.BackColor = Color.Transparent;
            btnSolitaire.BackgroundImageLayout = ImageLayout.Stretch;

            try
            {
                btnSolitaire.BackgroundImage = Properties.Resources.play_solitaire;
            }
            catch
            {
                btnSolitaire.Text = "Play Solitaire";
                btnSolitaire.BackColor = Color.LightGray;
            }
        }

        private void CenterDifficultyOverlay()
        {
            if (panelDifficulty != null)
            {
                panelDifficulty.Left = (this.ClientSize.Width - panelDifficulty.Width) / 2;
                panelDifficulty.Top = (this.ClientSize.Height - panelDifficulty.Height) / 2;
            }
        }

        private void SetupExitButton()
        {
            btnExit.FlatStyle = FlatStyle.Flat;
            btnExit.FlatAppearance.BorderSize = 0;
            btnExit.BackColor = Color.Transparent;
            btnExit.Size = new Size(100, 100); // skala s prozorom kasnije
            btnExit.BackgroundImageLayout = ImageLayout.Zoom;
            btnExit.BackgroundImage = Properties.Resources.exit;
            bool isHovering = false;



            try
            {
                btnExit.BackgroundImage = Properties.Resources.exit; // provjerite je li ovo u vašim resursima
            }
            catch
            {
                btnExit.Text = ""; // ✅ ispravna rezervna oznaka

            }

            // ✅ Efekt lebdenja – suptilno prekrivanje nijansom
            btnExit.MouseEnter += (s, e) =>
            {
                isHovering = true;
                btnExit.Invalidate(); // tjera na ponovno farbanje
            };

            btnExit.MouseLeave += (s, e) =>
            {
                isHovering = false;
                btnExit.Invalidate();

            };
            btnExit.Paint += (s, e) =>
            {
                if (isHovering)
                {
                    using (Pen pen = new Pen(Color.LightGreen, 3)) // boja i debljina
                    {
                        e.Graphics.DrawRectangle(pen, 0, 0, btnExit.Width - 1, btnExit.Height - 1);
                    }
                }
            };


            btnSettings = new Button
            {
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0 },
                BackColor = Color.Transparent,
                Size = new Size(100, 100),
                BackgroundImageLayout = ImageLayout.Zoom,
                Cursor = Cursors.Hand
            };

            try
            {
               btnSettings.BackgroundImage = Properties.Resources.settings; 
            }
            catch
            {
                btnSettings.Text = "Settings";
                btnSettings.BackColor = Color.LightGray;
            }

            btnSettings.Click += BtnSettings_Click;

            panelMainMenu.Controls.Add(btnSettings);

            // Spremi izvorni pravokutnik za promjenu veličine
            btnSettingsOriginalRect = new Rectangle(btnSettings.Location, btnSettings.Size);

        }

        private void BtnSettings_Click(object sender, EventArgs e)
        {
            OpenSettingsPanel();
        }

        private void SetupSettingsPanel()
        {
            // Tamni sloj
            panelSettingsDark = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(150, 0, 0, 0),
                Visible = false
            };
            this.Controls.Add(panelSettingsDark);
            panelSettingsDark.BringToFront();

            // Glavni panel
            panelSettings = new Panel
            {
                Size = new Size(450, 430),
                BackColor = Color.FromArgb(240, 240, 240),
                Visible = false
            };
            panelSettingsDark.Controls.Add(panelSettings);

            // naslov
            Label lblTitle = new Label
            {
                Text = "Settings",
                Font = new Font("Arial", 16, FontStyle.Bold),
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Height = 50
            };
            panelSettings.Controls.Add(lblTitle);

            // gumb zatvori
            btnSettingsClose = new Button
            {
                Text = "X",
                BackColor = Color.Transparent,
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.Black,
                Font = new Font("Arial", 12, FontStyle.Bold),
                Size = new Size(35, 35),
                Location = new Point(panelSettings.Width - 45, 10)
            };
            btnSettingsClose.FlatAppearance.BorderSize = 0;
            btnSettingsClose.Click += (s, e) => CloseSettingsPanel();
            panelSettings.Controls.Add(btnSettingsClose);
            btnSettingsClose.BringToFront();

            int labelX = 40;
            int comboX = 150;

            // ================= THEME =================
            panelSettings.Controls.Add(new Label { Text = "Theme:", Location = new Point(labelX, 90), AutoSize = true, Font = new Font("Arial", 12) });

            cbTheme = new ComboBox { Location = new Point(comboX, 85), Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };
            cbTheme.Items.AddRange(new string[] { "Light", "Dark" });
            panelSettings.Controls.Add(cbTheme);

            // ================= MUSIC SELECT =================
            panelSettings.Controls.Add(new Label { Text = "Select Music:", Location = new Point(labelX, 140), AutoSize = true, Font = new Font("Arial", 12) });

            cbSelectMusic = new ComboBox { Location = new Point(comboX, 135), Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };
            panelSettings.Controls.Add(cbSelectMusic);

            string musicFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Music");
            if (Directory.Exists(musicFolder))
            {
                foreach (var file in Directory.GetFiles(musicFolder, "*.mp3"))
                    cbSelectMusic.Items.Add(Path.GetFileNameWithoutExtension(file));
            }

            cbSelectMusic.SelectedIndexChanged += (s, e) =>
            {
                if (cbSelectMusic.SelectedItem == null) return;

                string selectedSong = cbSelectMusic.SelectedItem.ToString();
                SettingsManager.Instance.SelectMusic = selectedSong;
                SettingsManager.Instance.Save();
                PlaySelectedMusic(selectedSong);
            };

            if (!string.IsNullOrEmpty(SettingsManager.Instance.SelectMusic) &&
                cbSelectMusic.Items.Contains(SettingsManager.Instance.SelectMusic))
                cbSelectMusic.SelectedItem = SettingsManager.Instance.SelectMusic;
            else if (cbSelectMusic.Items.Count > 0)
                cbSelectMusic.SelectedIndex = 0;

            // ================= MUSIC VOLUME =================
            panelSettings.Controls.Add(new Label { Text = "Music Volume:", Location = new Point(labelX, 190), AutoSize = true, Font = new Font("Arial", 12) });

            sliderVolume = new TrackBar
            {
                Location = new Point(comboX, 180),
                Width = 200,
                Minimum = 0,
                Maximum = 100,
                TickFrequency = 10,
                Value = SettingsManager.Instance.MusicVolume
            };
            panelSettings.Controls.Add(sliderVolume);

            sliderVolume.Scroll += (s, e) =>
            {
                musicPlayer.settings.volume = sliderVolume.Value;
                SettingsManager.Instance.MusicVolume = sliderVolume.Value;
                SettingsManager.Instance.Save();
            };

            // ================= SFX VOLUME =================
            panelSettings.Controls.Add(new Label { Text = "Sound Effects:", Location = new Point(labelX, 240), AutoSize = true, Font = new Font("Arial", 12) });

            TrackBar sliderSfx = new TrackBar
            {
                Location = new Point(comboX, 230),
                Width = 200,
                Minimum = 0,
                Maximum = 100,
                TickFrequency = 10,
                Value = SettingsManager.Instance.SfxVolume
            };
            panelSettings.Controls.Add(sliderSfx);

            sliderSfx.Scroll += (s, e) =>
            {
                SettingsManager.Instance.SfxVolume = sliderSfx.Value;
                SettingsManager.Instance.Save();
            };


            // ================= CARD BACK =================
            panelSettings.Controls.Add(new Label { Text = "Card Back:", Location = new Point(labelX, 285), AutoSize = true, Font = new Font("Arial", 12) });

            cbCardBack = new ComboBox { Location = new Point(comboX, 285), Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };
            cbCardBack.Items.AddRange(new string[] { "Black", "Red", "Blue", "Orange", "Purple", "Green" });
            panelSettings.Controls.Add(cbCardBack);

            // ================= LOAD SAVED VALUES =================
            cbTheme.SelectedItem = SettingsManager.Instance.Theme;
            
            cbCardBack.SelectedItem = SettingsManager.Instance.CardBack;
        }

        private void OpenSettingsPanel()
        {
            panelSettingsDark.Visible = true;
            panelSettings.Visible = true;

            panelSettings.Left = (this.ClientSize.Width - panelSettings.Width) / 2;
            panelSettings.Top = (this.ClientSize.Height - panelSettings.Height) / 2;

            panelSettings.BringToFront();
            panelSettingsDark.BringToFront();
        }

        private void CloseSettingsPanel()
        {
            SettingsManager.Instance.Theme = cbTheme.SelectedItem?.ToString() ?? "Light";
            SettingsManager.Instance.SelectMusic = cbSelectMusic.SelectedItem?.ToString() ?? "Default";

            SettingsManager.Instance.MusicVolume = sliderVolume.Value;
            
            SettingsManager.Instance.CardBack = cbCardBack.SelectedItem?.ToString() ?? "Black";

            SettingsManager.Instance.Save();

            panelSettings.Visible = false;
            panelSettingsDark.Visible = false;
        }

        private void ResizeGameButtons()
        {
            if (scrollPanelGames.Controls.Count == 0) return;

            int panelWidth = scrollPanelGames.ClientSize.Width;
            int panelHeight = scrollPanelGames.ClientSize.Height;
            int padding = 20;

            int totalButtons = scrollPanelGames.Controls.Count;
            int buttonWidth = 150;  // fiksna širina
            int buttonHeight = 170; // fiksna viša visina

            int x = padding;
            int y = (panelHeight - buttonHeight) / 2;

            foreach (Control c in scrollPanelGames.Controls)
            {
                if (c is Button btn && c != selectorBar)
                {
                    btn.Size = new Size(buttonWidth, buttonHeight);
                    btn.Location = new Point(x, y);
                    x += buttonWidth + padding;
                }
            }

            // Ažurirajte traku za pomicanje
            scrollPanelGames.AutoScrollMinSize = new Size(x, 0);

            PositionSelectorBar();
        }

        private void PlaySelectedMusic(string songName)
        {
            if (string.IsNullOrEmpty(songName))
                return;

            string songPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Resources",
                "Music",
                songName + ".mp3"
            );

            if (!File.Exists(songPath))
                return;

            // Spriječi ponovno pokretanje iste pjesme
            if (musicPlayer.URL == songPath)
                return;

            musicPlayer.settings.autoStart = false;
            musicPlayer.settings.setMode("loop", true);
            musicPlayer.URL = songPath;
            musicPlayer.controls.play();
        }




    }
}
