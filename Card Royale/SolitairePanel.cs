    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Windows.Forms;
    using WMPLib;
    using System.IO;

namespace Card_Royale
{
    public partial class SolitairePanel : UserControl
    {
        private Deck deck;
        public List<Panel> tableauColumns;
        public List<Panel> foundations;
        private Panel stockPile;
        private Panel wastePile;

        private List<Card> wasteCards = new List<Card>();
        private List<PictureBox> wasteCardPanels = new List<PictureBox>();

        private TaskCompletionSource<bool> skipAnimation;

        private Button btnReturnToMenu;
        private Button btnNewDraft;
        private Button btnUndoMove;

        private Point mouseDragOffset;

        private Timer gameTimer;
        private int elapsedSeconds = 0;
        private Label lblTimer;

        private DateTime startTime;

        private bool timerRestarted = false;

        private readonly int maxVisibleWasteCards = 3;
        private readonly int wasteCardOffsetX = 20;

        private int cardWidth;
        private int cardHeight;
        private int tableauSpacing;

        private readonly int defaultTableauSpacing = 15;

        private Panel bottomNavPanel;

        private Font gameFont;

        private Stack<Action> undoStack = new Stack<Action>();

        public List<MoveRecord> moveRecords = new List<MoveRecord>();

        private bool cancelWinAnimation = false;
        public Panel WastePile => wastePile;

        public MovingCardsManager moveManager;

        public event Action ReturnToMenuRequested;
        public event Action NewDraftRequested;

        private TimeSpan elapsedTime = TimeSpan.Zero;

        private System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();

        private List<AnimatedCard> animatedCards;
        private System.Windows.Forms.Timer animationTimer;

        private ImageList navImageList;

        private WindowsMediaPlayer sfxPlayer = new WindowsMediaPlayer();


        public SolitairePanel()
        {
            InitializeComponent();
            this.BackColor = ColorTranslator.FromHtml("#00512c");
            gameFont = new Font("Comic Sans MS", 10, FontStyle.Bold);
            tableauColumns = new List<Panel>();
            foundations = new List<Panel>();

            gameTimer = new Timer { Interval = 1000 };
            gameTimer.Tick += GameTimer_Tick;

            lblTimer = new Label
            {
                Text = "Time: 00:00",
                ForeColor = Color.White,
                Font = new Font("Arial", 14, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(20, 10)
            };

            this.Controls.Add(lblTimer);
            lblTimer.BringToFront();
            this.Resize += SolitairePanel_Resize;
            this.Resize += (s, e) =>
                {
                    var img = stockPile?.Controls.OfType<PictureBox>()
                        .FirstOrDefault(pb => pb.Name == "deckImage");
                    if (img != null)
                        CenterDeckImageInStock(img);
                };


            RebuildWastePile();
            ScaleElements();

        }

        private void SolitairePanel_Resize(object sender, EventArgs e)
        {
            // Spriječite prekid rasporeda tijekom vrlo malih veličina prozora
            if (this.Width < 400 || this.Height < 400)
                return;


            // Promijeni skalu svih postojećih PictureBoxova na kartici
            foreach (var column in tableauColumns)
            {
                foreach (PictureBox pb in column.Controls.OfType<PictureBox>())
                {
                    pb.Width = cardWidth;
                    pb.Height = cardHeight;
                    ApplyRoundedCorners(pb, 5);
                }
            }

            foreach (var foundation in foundations)
            {
                foreach (PictureBox pb in foundation.Controls.OfType<PictureBox>())
                {
                    pb.Width = cardWidth;
                    pb.Height = cardHeight;
                    ApplyRoundedCorners(pb, 5);
                }
            }

            if (wastePile != null)
            {
                foreach (PictureBox pb in wastePile.Controls.OfType<PictureBox>())
                {
                    pb.Width = cardWidth;
                    pb.Height = cardHeight;
                    ApplyRoundedCorners(pb, 5);
                }
            }

            if (stockPile != null)
            {
                foreach (PictureBox pb in stockPile.Controls.OfType<PictureBox>())
                {
                    pb.Width = cardWidth;
                    pb.Height = cardHeight;
                    ApplyRoundedCorners(pb, 5);
                }
            }
            ScaleElements();
            RedrawAllCards();

        }

        private void SolitairePanel_Load(object sender, EventArgs e)
        {
            RedrawAllCards();
        }

        private void RedrawAllCards()
        {
            foreach (var pb in this.Controls.OfType<PictureBox>())
            {
                Card card = pb.Tag as Card;
                if (card != null && pb.BackColor == Color.White)
                {
                    pb.Image = CreateCardImage(card);
                }
            }
        }

        public void SetupBottomNavigation()
        {
            navImageList = new ImageList();
            navImageList.ImageSize = new Size(62, 62);
            navImageList.ColorDepth = ColorDepth.Depth32Bit;

            // Dodajte ikone na ImageList (pod pretpostavkom da ih imate kao resurse)
            navImageList.Images.Add(Properties.Resources.exit);
            navImageList.Images.Add(Properties.Resources.newdraft);
            navImageList.Images.Add(Properties.Resources.undo);


            bottomNavPanel = new Panel
            {
                Height = 50,
                Dock = DockStyle.Bottom,
                BackColor = Color.FromArgb(50, 0, 0, 0),
            };

            this.Controls.Add(bottomNavPanel);
            bottomNavPanel.BringToFront();

            btnReturnToMenu = new Button
            {
                ImageList = navImageList,
                ImageIndex = 0,
                Text = "",
                Width = 60,
                Height = 60,
                Left = 20,
                Top = 0,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                FlatAppearance = { BorderSize = 0 },
                ImageAlign = ContentAlignment.MiddleCenter,
            };
            btnReturnToMenu.Click += BtnReturnToMenu_Click;

            btnNewDraft = new Button
            {
                ImageList = navImageList,
                ImageIndex = 1,
                Text = "",
                Width = 60,
                Height = 60,
                Left = btnReturnToMenu.Right + 20,
                Top = 0,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                FlatAppearance = { BorderSize = 0 },
                ImageAlign = ContentAlignment.MiddleCenter,
            };
            btnNewDraft.Click += BtnNewDraft_Click;

            btnUndoMove = new Button
            {
                ImageList = navImageList,
                ImageIndex = 2,
                Text = "",
                Width = 60,
                Height = 60,
                Left = btnNewDraft.Right + 20,
                Top = 0,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                FlatAppearance = { BorderSize = 0 },
                ImageAlign = ContentAlignment.MiddleCenter,
            };
            btnUndoMove.Click += BtnUndoMove_Click;

            bottomNavPanel.Controls.Add(btnReturnToMenu);
            bottomNavPanel.Controls.Add(btnNewDraft);
            bottomNavPanel.Controls.Add(btnUndoMove);
        }

        public void StartGame(string difficulty)
        {

            SettingsManager.Instance.Load();


            PlayResetSound();
            // Očisti staro stanje igre
            this.Controls.Clear();
            tableauColumns.Clear();
            foundations.Clear();
            wasteCards.Clear();
            wasteCardPanels.Clear();

            // Timer i oznaka vremena
            this.Controls.Add(lblTimer);
            lblTimer.BringToFront();
            startTime = DateTime.Now;
            gameTimer.Start();

            RestartTimer();

            if (!timerRestarted)
            {
                elapsedSeconds = 0; //Resetiraj samo ako stvarno #elimo svje#u igru
            }

            timerRestarted = false;

            // iniciliziraj deck
            deck = new Deck();
            deck.Shuffle();


            ClearMoveHistory();

            Console.WriteLine("SolitairePanel instance hashcode: " + this.GetHashCode());

            // 1️. Postavi zalihu i otpad najprije, kako bi stockPile i wastePile postojali
            SetupStockAndWaste();

            // 2️. Postavi donju navigacijsku traku
            SetupBottomNavigation();

            // 3. Postavi temelje
            SetupFoundations();

            // 4️. Stvori MovingCardsManager NAKON što postoje zaliha, otpad i temelji

            moveManager = new MovingCardsManager(this, tableauColumns, foundations, cardWidth);

            RebuildWastePile();

            ScaleElements();

            // 5️. Postavi stupce tabloa (karte dodane u stupce)
            SetupTableauColumns();

            // 6️. Postavi sliku špila NAKON što postoji moveManager
            SetupDeckImage();

            SolitairePanel_Resize(this, EventArgs.Empty);

            Console.WriteLine("Game started successfully. MovingCardsManager hashcode: " + moveManager.GetHashCode());

           

        }

        private void SetupStockAndWaste()
        {
            stockPile = new Panel
            {
                Width = cardWidth,
                Height = cardHeight,
                Left = 20,
                Top = 50,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = ColorTranslator.FromHtml("#02b05f")
            };
            this.Controls.Add(stockPile);
            stockPile.BringToFront(); // Osigurajte da je hrpa zaliha dostupna

            wastePile = new Panel
            {
                Width = cardWidth,
                Height = cardHeight,
                Left = stockPile.Right + 40,
                Top = 50,
                BackColor = ColorTranslator.FromHtml("#00512c")
            };
            this.Controls.Add(wastePile);
            wastePile.BringToFront(); // Osigurajte pristup hrpi otpada

            stockPile.Click += (s, e) => StockPile_Click();
            List<Card> allCards = new List<Card>(deck.GetAllCards());
            int totalTableauCards = 0;
            for (int i = 1; i <= 7; i++) totalTableauCards += i;
            allCards.RemoveRange(0, totalTableauCards);
            stockPile.Tag = allCards;
        }

        public void AddMoveRecord(MoveRecord move)
        {
            moveRecords.Add(move);
        }

        private void SetupDeckImage()
        {
            // Prvo uklonite sve postojeće slike palube
            var existing = stockPile.Controls.OfType<PictureBox>().FirstOrDefault(pb => pb.Name == "deckImage");
            if (existing != null)
            {
                stockPile.Controls.Remove(existing);
                existing.Dispose();
            }

            PictureBox deckImage = new PictureBox
            {
                Name = "deckImage",
                Image = Properties.Resources.refresh, // ikona za osvježavanje
                SizeMode = PictureBoxSizeMode.Zoom,   // zadrži aspekt, prilagodi skali
                BackColor = Color.Transparent
            };

            // Dodaj u stockPile, a zatim prilagodi veličinu i centriraj pomoću stockPile.ClientSize
            stockPile.Controls.Add(deckImage);
            CenterDeckImageInStock(deckImage);
            deckImage.BringToFront();

            // Click handler
            deckImage.Click += (s, e) => StockPile_Click();
        }

        private void CenterDeckImageInStock(PictureBox deckImage)
        {
            if (stockPile == null || deckImage == null) return;

            // Koristite proporcionalno skaliranje kako bi izgledalo ispravno
            deckImage.Width = Math.Max(20, cardWidth);
            deckImage.Height = Math.Max(20, cardHeight);
            deckImage.SizeMode = PictureBoxSizeMode.Zoom;

            // Sredina unutar stockPile
            deckImage.Left = (stockPile.ClientSize.Width - deckImage.Width) / 2;
            deckImage.Top = (stockPile.ClientSize.Height - deckImage.Height) / 2;
        }

        private void SetupFoundations()
        {
            int startX = wastePile.Right + 100; // povećana udaljenost od hrpe otpada
            int top = 50;

            for (int i = 0; i < 4; i++)
            {
                Panel foundation = new Panel
                {
                    Width = cardWidth + 10,               // mala vodoravna podstava
                    Height = cardHeight + (cardHeight / 2), // dodatna visina za smještaj naslaganih kartica
                    Left = startX + i * (cardWidth + 40),
                    Top = top,         // malo podignite radi simetrije
                    BorderStyle = BorderStyle.FixedSingle,
                    BackColor = ColorTranslator.FromHtml("#355E3B")        // Izgleda čišće od LightGraya
                };
                foundations.Add(foundation);
                this.Controls.Add(foundation);
                foundation.BringToFront();
            }
        }

        public void UndoLastMove()
        {
            if (moveRecords == null || moveRecords.Count == 0)
            {
                Console.WriteLine("No moves to undo.");
                return;
            }

            var lastMove = moveRecords.Last();
            moveRecords.RemoveAt(moveRecords.Count - 1);

            Console.WriteLine($"Undoing move: {lastMove.CardDescription} from {lastMove.Source} to {lastMove.Destination}");

            if (lastMove.IsFlip)
            {
                UndoFlip(lastMove);
            }
            else
            {
                UndoCardMove(lastMove);
            }
        }

        private void UndoCardMove(MoveRecord move)
        {
            Panel sourcePanel = GetPanelFromLabel(move.Source);
            Panel destPanel = GetPanelFromLabel(move.Destination);
            if (sourcePanel == null || destPanel == null) return;

            // Pronađite gornju premještenu karticu PictureBox u odredištu
            PictureBox topMovedCard = destPanel.Controls
                .Cast<PictureBox>()
                .LastOrDefault(pb => moveManager.GetCardDescription(pb) == move.CardDescription);

            if (topMovedCard == null) return;

            // Sakupite sve karte ispod topMovedCard kako biste ih zajedno vratili
            var destCardsOrdered = destPanel.Controls.Cast<PictureBox>().OrderBy(pb => pb.Top).ToList();
            int startIndex = destCardsOrdered.IndexOf(topMovedCard);
            List<PictureBox> cardsToRestore = destCardsOrdered.Skip(startIndex).ToList();

            // Ukloni sve kartice s odredišne ​​ploče
            foreach (var card in cardsToRestore)
                destPanel.Controls.Remove(card);

            // Ako je izvor hrpa otpada, postupajte u skladu s tim (npr. vratite na popis WasteCards)
            if (sourcePanel == wastePile)
            {
                foreach (var cardPb in cardsToRestore)
                    wasteCards.Add(cardPb.Tag as Card);
                RebuildWastePile();
            }
            else
            {
                // Dodaj sve kartice natrag na izvornu ploču složene s pomakom
                int baseTop = sourcePanel.Controls.Count > 0
                    ? sourcePanel.Controls.Cast<PictureBox>().Max(pb => pb.Top) + 30
                    : 0;

                foreach (var cardPb in cardsToRestore)
                {
                    cardPb.Parent = sourcePanel;
                    cardPb.Left = 0;
                    cardPb.Top = baseTop;
                    cardPb.BringToFront();
                    baseTop += 30;
                }
            }

            UpdateFaceDownCardsClickable(sourcePanel);
            MoveCompleted();
        }

        private void BtnUndoMove_Click(object sender, EventArgs e)
        {
            UndoLastMove();
        }

        private void BtnReturnToMenu_Click(object sender, EventArgs e)
        {
            PauseTimer();
            ReturnToMenuRequested?.Invoke();
        }

        private void BtnNewDraft_Click(object sender, EventArgs e)
        {
            NewDraftRequested?.Invoke();

            ClearMoveHistory();

        }

        private void UndoFlip(MoveRecord move)
        {
            Panel panelOfCard = GetPanelFromLabel(move.Source);
            if (panelOfCard == null) return;

            PictureBox cardPb = panelOfCard.Controls
                .Cast<PictureBox>()
                .LastOrDefault(pb => moveManager.GetCardDescription(pb) == move.CardDescription);

            if (cardPb != null)
            {
                // Vratite licem prema dolje
                cardPb.BackColor = Color.DarkGreen; // ili koja god vam je boja licem prema dolje
                cardPb.Image = GetCardBackImage(); // upotrijebite sliku poleđine svoje kartice

                UpdateFaceDownCardsClickable(panelOfCard);
                MoveCompleted();
            }
        }

        private Panel GetPanelFromLabel(string label)
        {
            if (label.StartsWith("T"))
            {
                int idx = int.Parse(label.Substring(1)) - 1;
                if (idx >= 0 && idx < tableauColumns.Count) return tableauColumns[idx];
            }
            else if (label.StartsWith("A"))
            {
                int idx = int.Parse(label.Substring(1)) - 1;
                if (idx >= 0 && idx < foundations.Count) return foundations[idx];
            }
            else if (label.StartsWith("W"))
            {
                return wastePile;
            }
            // Dodajte zalihe ili druge po potrebi
            return null;
        }

        private void SetupTableauColumns()
        {
            int startX = 20;
            int startY = 200;
            tableauColumns.Clear();

            // Makni prijašno stvorene stupce
            for (int i = this.Controls.Count - 1; i >= 0; i--)
            {
                if (this.Controls[i] is Panel p && p.BackColor == Color.Transparent)
                    this.Controls.Remove(p);
            }

            // Tableau karte: prvih 28 karata
            List<Card> tableauCards = deck.GetAllCards().Take(28).ToList();
            int index = 0;

            for (int colIdx = 0; colIdx < 7; colIdx++)
            {
                Panel column = new Panel
                {
                    Width = cardWidth,
                    Height = this.Height - startY - 20,
                    Left = startX + colIdx * (cardWidth + 10),
                    Top = startY,
                    BackColor = Color.Transparent
                };
                tableauColumns.Add(column);
                this.Controls.Add(column);

                int numCards = colIdx + 1;
                for (int row = 0; row < numCards; row++)
                {
                    Card card = tableauCards[index++];
                    bool faceUp = (row == numCards - 1);
                    PictureBox cardBox = CreateCardPictureBox(card, faceUp);

                    cardBox.Top = row * 30;
                    column.Controls.Add(cardBox);
                    cardBox.BringToFront();

                    if (faceUp)
                        moveManager.MakeCardDraggable(cardBox);
                }

                //Ažuriraj preokrenute karte clickablea nakon namještavanja stupca
                UpdateFaceDownCardsClickable(column);
            }
        }

        private void StockPile_Click()
        {
            var stockCards = stockPile.Tag as List<Card>;
            if (stockCards == null) return;

            if (stockCards.Count == 0)
            {
                if (wasteCards.Count == 0) return;

                stockCards.AddRange(wasteCards);
                wasteCards.Clear();
                stockPile.Tag = stockCards;


                RebuildWastePile();
                return;
            }

            // Izvuci gornju kartu
            Card nextCard = stockCards[0];
            stockCards.RemoveAt(0);
            stockPile.Tag = stockCards;

            // Dodaj u hrpu otpada
            wasteCards.Add(nextCard);

            RebuildWastePile();

        }

        public bool IsGameWon()
        {
            foreach (var col in tableauColumns)
            {
                foreach (PictureBox pb in col.Controls)
                {
                    if (pb.BackColor != Color.White) // i dalje licem prema dolje
                        return false;
                }
            }
            return true;
        }

        public PictureBox CreateCardPictureBox(Card card, bool faceUp, bool makeDraggableImmediately = false)
        {
            PictureBox pb = new PictureBox
            {
                Width = cardWidth,
                Height = cardHeight,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = faceUp ? Color.White : Color.Transparent, // faceup počinje bijelo
                SizeMode = PictureBoxSizeMode.StretchImage,
                Tag = card
            };

            // Set image
            pb.Image = faceUp ? CreateCardImage(card) : GetCardBackImage();


            // Nacrtajte ikonu malog odijela ako je licem prema gore
            pb.Paint += (s, e) =>
            {
                if (pb.BackColor == Color.White && card != null)
                {
                    int iconSize = Math.Max(12, cardWidth / 8);
                    Bitmap suitBmp = GetSuitIcon(card.Suit, iconSize);
                    if (suitBmp != null)
                        e.Graphics.DrawImage(suitBmp, 4, pb.Height - iconSize - 4, iconSize, iconSize);
                }
            };

            // Kliknite za okretanje karte licem prema dolje
            pb.Click += (s, e) =>
            {
                if (pb.BackColor != Color.White && !HasCardBelow(pb))
                {
                    pb.BackColor = Color.White;
                    pb.Image = CreateCardImage(card);
                    moveManager.MakeCardDraggable(pb);

                    PlayCardFlipSound();

                    // Dodaj move record
                    Panel parentPanel = pb.Parent as Panel;
                    if (parentPanel != null)
                    {
                        AddMoveRecord(new MoveRecord(
                            moveManager.GetCardDescription(pb),
                            GetPanelLabel(parentPanel),  // source
                            GetPanelLabel(parentPanel),  // destination (isto, jer flip ne mijenja panel)
                            null,                        // beneathDescription
                            true                         // flip
                        ));
                    }

                    if (parentPanel != null)
                        UpdateFaceDownCardsClickable(parentPanel);
                }
            };

            // Odmah omogućite povlačenje ako se zatraži (npr. gornja karta tablice ili karta otpada)
            if (faceUp || makeDraggableImmediately)
                moveManager.MakeCardDraggable(pb);

            ApplyRoundedCorners(pb, 5);
            return pb;
        }

        private Image GetCardBackImage()
        {
            switch (SettingsManager.Instance.CardBack)
            {
                case "Black":
                    return Properties.Resources.card_back;
                case "Red":
                    return Properties.Resources.card_back_red;
                case "Blue":
                    return Properties.Resources.card_back_blue;
                case "Green":
                    return Properties.Resources.card_back_green;
                case "Purple":
                    return Properties.Resources.card_back_purple;
                case "Orange":
                    return Properties.Resources.card_back_orange;
                default:
                    return Properties.Resources.card_back; // fallback
            }
        }

        private string GetPanelLabel(Panel panel)
        {
            if (tableauColumns.Contains(panel))
                return "T" + (tableauColumns.IndexOf(panel) + 1);
            if (foundations.Contains(panel))
                return "A" + (foundations.IndexOf(panel) + 1);
            if (panel == wastePile)
                return "W";
            return "Unknown";
        }

        private Bitmap GetSuitIcon(Suit suit, int size)
        {
            Bitmap baseIcon = null;

            switch (suit)
            {
                case Suit.Hearts:
                    baseIcon = Properties.Resources.heart;
                    break;

                case Suit.Diamonds:
                    baseIcon = Properties.Resources.diamond;
                    break;

                case Suit.Clubs:
                    baseIcon = Properties.Resources.clover;
                    break;

                case Suit.Spades:
                    baseIcon = Properties.Resources.spade;
                    break;
            }

            if (baseIcon == null)
                return null;

            Bitmap resized = new Bitmap(baseIcon, size, size);

            // Prebojite crna odijela u tamnoj temi
            if (SettingsManager.Instance.Theme == "Dark" &&
                (suit == Suit.Clubs || suit == Suit.Spades))
            {
                return RecolorBitmap(resized, Color.White);
            }

            return resized;
        }

        private Bitmap RecolorBitmap(Bitmap original, Color newColor)
        {
            Bitmap recolored = new Bitmap(original.Width, original.Height);

            for (int x = 0; x < original.Width; x++)
            {
                for (int y = 0; y < original.Height; y++)
                {
                    Color pixel = original.GetPixel(x, y);

                    if (pixel.A == 0)
                        recolored.SetPixel(x, y, Color.Transparent);
                    else
                        recolored.SetPixel(x, y, Color.FromArgb(pixel.A, newColor));
                }
            }

            return recolored;
        }

        public bool HasCardBelow(PictureBox cardPb)
        {
            if (!(cardPb.Parent is Panel parent)) return false;

            var cards = parent.Controls.Cast<PictureBox>()
                .OrderBy(pb => pb.Top)
                .ToList();

            int idx = cards.IndexOf(cardPb);
            if (idx == -1) return false;

            //Vrati istinito ako ima karta ispod over
            return idx < cards.Count - 1;
        }

        public void UpdateFaceDownCardsClickable(Panel tableau)
        {
            var faceDownCards = tableau.Controls.Cast<PictureBox>()
                    .Where(pb => pb.BackColor != Color.White)
                    .OrderBy(pb => pb.Top)
                    .ToList();

            for (int i = 0; i < faceDownCards.Count; i++)
            {
                faceDownCards[i].Enabled = (i == faceDownCards.Count - 1); // omogućena je samo gornja kartica
            }
        }

        public void ApplyRoundedCorners(PictureBox pb, int radius)
        {
            var bounds = new Rectangle(0, 0, pb.Width - 1, pb.Height - 1); // -1 sprječava rezanje donjeg desnog kuta
            using (var path = new System.Drawing.Drawing2D.GraphicsPath())
            {
                int diameter = radius * 2;
                path.StartFigure();
                path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
                path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
                path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
                path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
                path.CloseFigure();
                pb.Region = new Region(path);
            }

            // Dodaj tanki crni rub
            pb.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using (Pen pen = new Pen(Color.Black, 1))
                {
                    pen.Alignment = System.Drawing.Drawing2D.PenAlignment.Inset;

                    // koristimo iste granice kao gore (-1) da rub ne izađe van
                    var rect = new Rectangle(0, 0, pb.Width - 1, pb.Height - 1);
                    using (var borderPath = new System.Drawing.Drawing2D.GraphicsPath())
                    {
                        int diameter = radius * 2;
                        borderPath.StartFigure();
                        borderPath.AddArc(rect.Left, rect.Top, diameter, diameter, 180, 90);
                        borderPath.AddArc(rect.Right - diameter, rect.Top, diameter, diameter, 270, 90);
                        borderPath.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
                        borderPath.AddArc(rect.Left, rect.Bottom - diameter, diameter, diameter, 90, 90);
                        borderPath.CloseFigure();
                        e.Graphics.DrawPath(pen, borderPath);
                    }
                }
            };
        }

        public bool IsAlternateColor(Card c1, Card c2)
        {
            if (c1 == null || c2 == null) return false;

            bool c1IsRed = (c1.Suit == SolitairePanel.Suit.Hearts || c1.Suit == SolitairePanel.Suit.Diamonds);
            bool c2IsRed = (c2.Suit == SolitairePanel.Suit.Hearts || c2.Suit == SolitairePanel.Suit.Diamonds);

            return c1IsRed != c2IsRed;
        }

        public Card GetCardFromPictureBox(PictureBox pb)
        {
            if (pb == null) return null;
            return pb.Tag as Card;
        }

        private string GetCardRankText(string rank)
        {
            switch (rank)
            {
                case "A": return "A";
                case "J": return "J";
                case "Q": return "Q";
                case "K": return "K";
                default: return rank;
            }
        }

        private void GameTimer_Tick(object sender, EventArgs e)
        {
            TimeSpan totalElapsed = elapsedTime + (DateTime.Now - startTime);
            lblTimer.Text = $"Time: {totalElapsed.Minutes:D2}:{totalElapsed.Seconds:D2}";
        }

        public Bitmap CreateCardImage(Card card)
        {
            Color rankColor;

            if (card.Suit == Suit.Hearts || card.Suit == Suit.Diamonds)
            {
                rankColor = Color.Red;
            }
            else
            {
                rankColor = SettingsManager.Instance.Theme == "Dark"
                    ? Color.White
                    : Color.Black;
            }

            // Koristite sigurne veličine za stvaranje bitmape
            int safeWidth = Math.Max(10, cardWidth);
            int safeHeight = Math.Max(14, cardHeight);

            Bitmap bmp = new Bitmap(safeWidth, safeHeight);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.Clear(SettingsManager.Instance.Theme == "Dark"
                    ? Color.Black
                    : Color.White);

                string rankText = GetCardRankText(card.Rank);
                // veličina fonta proporcionalna visini kartice
                float fontSize = Math.Max(8f, safeHeight * 0.17f);
                using (Font font = new Font("Arial", fontSize, FontStyle.Bold))
                using (Brush textBrush = new SolidBrush(rankColor))
                {
                    float padX = Math.Max(3f, safeWidth * 0.03f);
                    float padY = Math.Max(2f, safeHeight * 0.02f);
                    g.DrawString(rankText, font, textBrush, new PointF(padX, padY));
                }

                // Nacrtajte ikonu odijela u sredini koristeći sigurne veličine
                Image suitImg = GetCenterSuitImage(card.Suit);
                if (suitImg != null)
                {
                    int iconW = Math.Max(8, safeWidth / 2);
                    int iconH = Math.Max(8, safeHeight / 2);
                    int iconX = (safeWidth - iconW) / 2;
                    int iconY = (safeHeight - iconH) / 2;
                    g.DrawImage(suitImg, iconX, iconY, iconW, iconH);
                }
            }

            return bmp;
        }

        private Image GetCenterSuitImage(Suit suit)
        {
            bool dark = SettingsManager.Instance.Theme == "Dark";

            switch (suit)
            {
                case Suit.Spades:
                    return dark
                        ? Properties.Resources.spade_white
                        : Properties.Resources.spade;

                case Suit.Clubs:
                    return dark
                        ? Properties.Resources.clover_white
                        : Properties.Resources.clover;

                case Suit.Hearts:
                    return Properties.Resources.heart;

                case Suit.Diamonds:
                    return Properties.Resources.diamond;
            }

            return null;
        }

        public void RemoveFromWaste(PictureBox pb)
        {
            if (pb == null) return;

            Card c = pb.Tag as Card;
            if (c != null)
                wasteCards.Remove(c);

            RebuildWastePile();
        }

        public void RebuildWastePile()
        {
            if (wastePile == null) return;

            wastePile.Controls.Clear();
            wasteCardPanels.Clear();

            int startIndex = Math.Max(0, wasteCards.Count - maxVisibleWasteCards);
            var visibleCards = wasteCards.Skip(startIndex).ToList();

            // Izračunajte razmak tako da je rang u gornjem lijevom kutu potpuno vidljiv
            int minRankSpacing = (int)(cardWidth * 0.25f); // prostor za rang
            int cardSpacing = Math.Max(wasteCardOffsetX, minRankSpacing);

            // Dinamičko postavljanje širine hrpe otpada
            wastePile.Width = cardWidth + (visibleCards.Count - 1) * cardSpacing;
            wastePile.Height = cardHeight;

            for (int i = 0; i < visibleCards.Count; i++)
            {
                Card card = visibleCards[i];

                PictureBox pb = new PictureBox
                {
                    Width = cardWidth,
                    Height = cardHeight,
                    Left = i * cardSpacing,
                    Top = 0,
                    BorderStyle = BorderStyle.FixedSingle,
                    BackColor = Color.White,
                    SizeMode = PictureBoxSizeMode.StretchImage,
                    Tag = card,
                    Name = $"WasteCard_{i}"
                };

                // Nacrtajte cijelu sliku kartice
                pb.Image = CreateCardImage(card);

                // Nanesite zaobljene kutove
                ApplyRoundedCorners(pb, 5);

                // Nacrtajte malu ikonu odijela dolje lijevo
                pb.Paint += (s, e) =>
                {
                    int iconSize = Math.Max(12, cardWidth / 8); // manja ikona
                    Bitmap suitBmp = GetSuitIcon(card.Suit, iconSize);
                    if (suitBmp != null)
                        e.Graphics.DrawImage(suitBmp, 4, pb.Height - iconSize - 4, iconSize, iconSize);
                };

                wastePile.Controls.Add(pb);
                pb.BringToFront();
                wasteCardPanels.Add(pb);

                pb.Enabled = true;
                moveManager.MakeCardDraggable(pb);
            }

            wastePile.Visible = wasteCardPanels.Count > 0;
            RedrawAllCards();
        }

        public void UpdateTimerLabel()
        {
            int minutes = elapsedSeconds / 60;
            int seconds = elapsedSeconds % 60;
            lblTimer.Text = $"Time: {minutes:D2}:{seconds:D2}";
        }

        public void RestartTimer()
        {
            gameTimer.Stop();
            elapsedSeconds = 0;
            elapsedTime = TimeSpan.Zero;   //  resetiraj protečeno vrijeme
            startTime = DateTime.Now;      //  počni svježe
            UpdateTimerLabel();
            timerRestarted = true;
            gameTimer.Start();
        }

        public void PauseTimer()
        {
            if (gameTimer != null && gameTimer.Enabled)
            {
                elapsedTime += DateTime.Now - startTime; //spajanje protečenog
                gameTimer.Stop();
            }
        }

        public void ResumeTimer()
        {
            if (gameTimer != null && !gameTimer.Enabled)
            {
                startTime = DateTime.Now; //novi početak za ovu sesiju
                gameTimer.Start();
            }
        }

        public async void StartWinSequence()
        {
            cancelWinAnimation = false;

            moveManager.DisableDragging();
            stockPile.Enabled = false;
            wastePile.Enabled = false;

            // 1️. Izradite oznaku "Čestitamo"
            Label lblCongrats = new Label
            {
                Text = "Congratulations!",
                ForeColor = Color.Gold,
                Font = new Font("Segoe UI", 36, FontStyle.Bold),
                AutoSize = true,
                BackColor = Color.Transparent
            };

            this.Controls.Add(lblCongrats);
            lblCongrats.BringToFront();

            // 2️ Postavite naljepnicu ispod temelja i iznad radne površine
            if (foundations.Count > 0 && tableauColumns.Count > 0)
            {
                int leftMost = foundations.Min(f => f.Left);
                int rightMost = foundations.Max(f => f.Right);
                lblCongrats.Left = (leftMost + rightMost - lblCongrats.Width) / 2;

                int topMostFoundation = foundations.Min(f => f.Top);
                int bottomMostFoundation = foundations.Max(f => f.Bottom);

                int topMostTableau = tableauColumns.Min(t => t.Top);

                // Postavite naljepnicu malo ispod temelja, ali iznad radne površine
                lblCongrats.Top = bottomMostFoundation + (topMostTableau - bottomMostFoundation) / 2 - lblCongrats.Height / 2;
            }
            else
            {
                // fallback: center on panel
                lblCongrats.Left = (this.ClientSize.Width - lblCongrats.Width) / 2;
                lblCongrats.Top = 100;
            }

            // Neobavezno: efekt postupnog pojavljivanja
            for (int i = 0; i <= 100; i += 5)
            {
                lblCongrats.ForeColor = Color.FromArgb(i * 255 / 100, Color.Gold);
                await Task.Delay(10);
            }

            await Task.Delay(1500); // vidljivo trajanje

            // Opcionalno: efekt blijeđenja
            for (int i = 100; i >= 0; i -= 5)
            {
                lblCongrats.ForeColor = Color.FromArgb(i * 255 / 100, Color.Gold);
                await Task.Delay(10);
            }

            this.Controls.Remove(lblCongrats);
            lblCongrats.Dispose();

            // Pokreni novu skicu
            if (!cancelWinAnimation)
                NewDraftRequested?.Invoke();

            ClearMoveHistory();
        }

        private void ScaleElements()
        {
            int screenWidth = this.Width;
            int screenHeight = this.Height;

            // 🧠 Izračun omjera ekrana (manje povećanje na većim ekranima)
            float widthFactor = 0.00022f * screenWidth + 0.45f;
            float heightFactor = 0.0003f * screenHeight + 0.35f;

            // 📏 Kombiniraj oba faktora i ograniči maksimalnu vrijednost
            float scaleFactor = (widthFactor + heightFactor) / 2f;
            scaleFactor = Math.Min(scaleFactor, 0.95f);

            // 🃏 Izračun veličine karata (manje rastu pri većim prozorima)
            cardWidth = (int)Math.Max(90, (screenWidth / 16f) * scaleFactor);
            cardHeight = (int)(cardWidth * 1.3f);

            // 📐 Margine i razmak
            int marginX = screenWidth / 65;
            int marginY = screenHeight / 50;

            // 🧱 Veličina gumbi i ostalih elemenata
            int buttonWidth = Math.Max(100, screenWidth / 10);
            int buttonHeight = Math.Max(40, screenHeight / 18);
            int labelFontSize = Math.Max(12, screenWidth / 90);

            // 🎯 Ažuriraj fontove i gumbiće
            foreach (Control control in this.Controls)
            {
                if (control is Button)
                {
                    control.Font = new Font("Segoe UI", labelFontSize, FontStyle.Bold);
                    control.Size = new Size(buttonWidth, buttonHeight);
                }
                else if (control is Label)
                {
                    control.Font = new Font("Segoe UI", labelFontSize, FontStyle.Regular);
                }
            }


            // 🟩 Hrpa zaliha (gornji lijevi kut)
            if (stockPile != null)
            {
                stockPile.Width = cardWidth;
                stockPile.Height = cardHeight;
                stockPile.Left = marginX;
                stockPile.Top = marginY;

                var deckImage = stockPile.Controls.OfType<PictureBox>()
                                    .FirstOrDefault(pb => pb.Name == "deckImage");
                if (deckImage != null)
                    CenterDeckImageInStock(deckImage);
            }

            // 🟩 Hrpa otpada (desno od zaliha)
            if (wastePile != null && stockPile != null)
            {
                // Izračunajte razmak na temelju širine kartice za potpunu vidljivost ranga
                int rankPadding = (int)(cardWidth * 0.25f);
                int visibleCardsCount = Math.Min(maxVisibleWasteCards, wasteCards.Count);
                int cardSpacing = Math.Max(wasteCardOffsetX, rankPadding);

                wastePile.Width = cardWidth + (visibleCardsCount - 1) * cardSpacing;
                wastePile.Height = cardHeight;
                wastePile.Left = stockPile.Right + (int)(cardWidth * 0.7f);
                wastePile.Top = stockPile.Top;

                // Obnovite hrpu otpada kako biste osigurali ispravno pozicioniranje i stanje vučenja
                RebuildWastePile();
            }

            // 🟥 Temelji (gore desno)
            if (foundations.Count == 4 && wastePile != null)
            {
                int startX = wastePile.Right + (int)(cardWidth * 1.5f);
                int top = stockPile.Top;

                for (int i = 0; i < foundations.Count; i++)
                {
                    foundations[i].Width = cardWidth;
                    foundations[i].Height = cardHeight;
                    foundations[i].Left = startX + i * (cardWidth + (int)(cardWidth * 0.5f));
                    foundations[i].Top = top;
                }
            }

            // 🟦 Tableau stupci (ispod zaliha/otpada/temeljnih elemenata)
            if (tableauColumns.Count == 7)
            {
                int startX = marginX;
                float lowerFactor = 1.5f; // 1 = odmah ispod hrpe zaliha, više = dalje dolje
                int startY = stockPile.Bottom + (int)(cardHeight * lowerFactor);
                tableauSpacing = defaultTableauSpacing;

                for (int i = 0; i < tableauColumns.Count; i++)
                {
                    tableauColumns[i].Width = cardWidth;
                    tableauColumns[i].Height = screenHeight - startY - (screenHeight / 15);
                    tableauColumns[i].Left = startX + i * (cardWidth + tableauSpacing);
                    tableauColumns[i].Top = startY;
                }
            }

            // 🟨 Donja navigacijska ploča
            if (bottomNavPanel != null)
            {
                bottomNavPanel.Height = Math.Max(40, screenHeight / 15);
                bottomNavPanel.Dock = DockStyle.Bottom;
            }

            // 🟦 Navigacijski gumbi
            if (btnReturnToMenu != null && bottomNavPanel != null)
            {
                btnReturnToMenu.Width = screenWidth / 10;
                btnReturnToMenu.Height = bottomNavPanel.Height - 12;
                btnReturnToMenu.Left = marginX;
                btnReturnToMenu.Top = (bottomNavPanel.Height - btnReturnToMenu.Height) / 2;

                btnNewDraft.Width = btnReturnToMenu.Width;
                btnNewDraft.Height = btnReturnToMenu.Height;
                btnNewDraft.Left = btnReturnToMenu.Right + (int)(cardWidth * 0.3f);
                btnNewDraft.Top = btnReturnToMenu.Top;

                btnUndoMove.Width = btnReturnToMenu.Width;
                btnUndoMove.Height = btnReturnToMenu.Height;
                btnUndoMove.Left = btnNewDraft.Right + (int)(cardWidth * 0.3f);
                btnUndoMove.Top = btnReturnToMenu.Top;
            }

            // 🕒 Timer label
            if (lblTimer != null)
            {
                lblTimer.Font = new Font("Arial", Math.Max(12, screenHeight / 50), FontStyle.Bold);
                lblTimer.Left = marginX;

                if (bottomNavPanel != null)
                    lblTimer.Top = bottomNavPanel.Top - lblTimer.Height - 10;
                else
                    lblTimer.Top = 10;
            }
        }

        public void MoveCompleted()
        {
            Console.WriteLine("Current move history:");
            Console.WriteLine(GetMoveHistory());

            if (IsGameWon())
            {
                PlayVictorySound();
                StartWinSequence();
            }
                
        }

        private void PlayResetSound()
        {
            try
            {
                string sfxPath = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "Resources",
                    "Sounds",
                    "reset.mp3"
                );

                if (!File.Exists(sfxPath))
                {
                    MessageBox.Show("SFX NOT FOUND:\n" + sfxPath);
                    return;
                }

                sfxPlayer.controls.stop(); // ✅ force restart sound
                sfxPlayer.settings.volume = Math.Max(10, SettingsManager.Instance.SfxVolume);
                sfxPlayer.URL = sfxPath;
                sfxPlayer.controls.play();
            }
            catch (Exception ex)
            {
                MessageBox.Show("SFX ERROR:\n" + ex.Message);
            }
        }

        private void PlayCardFlipSound()
        {
            try
            {
                string sfxPath = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "Resources",
                    "Sounds",
                    "card_flip.mp3"
                );

                if (!File.Exists(sfxPath))
                {
                    MessageBox.Show("SFX NOT FOUND:\n" + sfxPath);
                    return;
                }

                sfxPlayer.controls.stop(); // ✅ force restart sound
                sfxPlayer.settings.volume = Math.Max(10, SettingsManager.Instance.SfxVolume);
                sfxPlayer.URL = sfxPath;
                sfxPlayer.controls.play();
            }
            catch (Exception ex)
            {
                MessageBox.Show("SFX ERROR:\n" + ex.Message);
            }
        }

        private void PlayVictorySound()
        {
            try
            {
                string sfxPath = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "Resources",
                    "Sounds",
                    "victory.mp3"
                );

                if (!File.Exists(sfxPath))
                {
                    MessageBox.Show("SFX NOT FOUND:\n" + sfxPath);
                    return;
                }

                sfxPlayer.controls.stop(); // ✅ force restart sound
                sfxPlayer.settings.volume = Math.Max(10, SettingsManager.Instance.SfxVolume);
                sfxPlayer.URL = sfxPath;
                sfxPlayer.controls.play();
            }
            catch (Exception ex)
            {
                MessageBox.Show("SFX ERROR:\n" + ex.Message);
            }
        }

        public enum Suit
        {
            Hearts,
            Diamonds,
            Clubs,
            Spades
        }


    }

    public class AnimatedCard
    {
        public Image Image;
        public float X, Y;
        public float DX, DY;
    }

    public partial class SolitairePanel : UserControl
    {
        private StringBuilder moveHistory = new StringBuilder();

        // Pozovi ovo na početku nove igre/drafta
        public void RecordMove(string moveNotation)
        {
            if (moveHistory.Length > 0)
                moveHistory.AppendLine();

            moveHistory.Append(moveNotation);
        }

        // Pozovite ovo kada želite cijelu povijest kao niz znakova
        public string GetMoveHistory()
        {
            return moveHistory.ToString();
        }

        // sve ovo za resetiranje povijesti u novoj igri
        public void ClearMoveHistory()
        {
            moveHistory.Clear();
        }

        // Pozovite ClearMoveHistory() na početku novog nacrta u BtnNewDraftClick ili ekvivalentu
    }

}
