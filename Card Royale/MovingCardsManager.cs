using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Card_Royale
{
    public class MovingCardsManager

    {
        private SolitairePanel panel;
        private List<Panel> tableauColumns;
        public List<Panel> foundations;
        private int cardWidth;
        private List<PictureBox> draggableCards = new List<PictureBox>();
        private bool isDragging = false;
        private Point lastMousePosition = Point.Empty;
        private List<PictureBox> dragStack = new List<PictureBox>();
        private Control dragOrigin = null;



        public MovingCardsManager(SolitairePanel panel, List<Panel> tableauColumns, List<Panel> foundations, int cardWidth)
        {
            this.panel = panel;
            this.tableauColumns = tableauColumns;
            this.foundations = foundations;
            this.cardWidth = cardWidth;
        }

        private void ReturnStackToOrigin(List<PictureBox> stack, Control origin)
        {
            Panel panelOrigin = origin as Panel;
            if (panelOrigin == null) return;

            stack = stack.OrderBy(c => c.Top).ToList();

            if (panelOrigin == panel.WastePile)
            {
                foreach (var c in stack)
                {
                    c.Parent = panelOrigin;
                    c.BringToFront();
                }
                Console.WriteLine("Returning to waste pile, rebuilding...");
                panel.RebuildWastePile();
            }
            else
            {
                var cards = panelOrigin.Controls.Cast<PictureBox>().OrderBy(c => c.Top).ToList();
                int baseTop = (cards.Count > 0) ? cards.Last().Top + 30 : 0;

                foreach (var c in stack)
                {
                    c.Parent = panelOrigin;
                    c.Left = 0;
                    c.Top = baseTop;
                    c.BringToFront();
                    baseTop += 30;
                }

                FlipTopCardFaceUp(panelOrigin);
                panel.UpdateFaceDownCardsClickable(panelOrigin);
            }

            panel.MoveCompleted();
        }

        public void RecordCardFlip(PictureBox cardPb)
        {
            string cardDesc = GetCardDescription(cardPb);
            string source = GetTableauColumnName(cardPb.Parent);
            string moveNotation = $"{cardDesc} flipped at {source}";
            panel.RecordMove(moveNotation);
            panel.MoveCompleted();
            Console.WriteLine($"Move recorded: {moveNotation}");
        }

        private void FlipTopCardFaceUp(Panel tableau)
        {
            var cards = tableau.Controls.Cast<PictureBox>().OrderBy(c => c.Top).ToList();
            foreach (var cardPb in cards)
            {
                if (cardPb.BackColor != Color.White && !HasCardBelow(cardPb))
                {
                    cardPb.BackColor = Color.White;
                    Card card = panel.GetCardFromPictureBox(cardPb);
                    if (card != null)
                    {
                        cardPb.Image = panel.CreateCardImage(card);
                        MakeCardDraggable(cardPb);

                        // Snimite okretanje ovdje
                        RecordCardFlip(cardPb);
                    }
                }
            }
            panel.MoveCompleted();
            panel.UpdateFaceDownCardsClickable(tableau);
        }

        public bool HasCardBelow(PictureBox cardPb)
        {
            if (!(cardPb.Parent is Panel parent)) return false;
            var cards = parent.Controls.Cast<PictureBox>().OrderBy(c => c.Top).ToList();
            int idx = cards.IndexOf(cardPb);
            return idx < cards.Count - 1;
        }

        public void MakeCardDraggable(PictureBox pb)
        {
            if (pb == null) return;

            pb.Enabled = true;


            // Uklonite prethodne rukovatelje kako biste spriječili duplikate
            pb.MouseDown -= Card_MouseDown;
            pb.MouseMove -= Card_MouseMove;
            pb.MouseUp -= Card_MouseUp;

            // Attach handlers
            pb.MouseDown += Card_MouseDown;
            pb.MouseMove += Card_MouseMove;
            pb.MouseUp += Card_MouseUp;

            if (!draggableCards.Contains(pb))
                draggableCards.Add(pb);

            Console.WriteLine($"MakeCardDraggable called for card {pb.Tag}, Enabled: {pb.Enabled}, Name: {pb.Name}, Parent: {pb.Parent?.Name}");
        }

        public void DisableDragging()
        {
            foreach (var pb in draggableCards)
            {
                pb.MouseDown -= Card_MouseDown;
                pb.MouseMove -= Card_MouseMove;
                pb.MouseUp -= Card_MouseUp;
            }
        }

        private void Panel_MouseMove(object sender, MouseEventArgs e)
        {
            if (!isDragging || dragStack == null || dragStack.Count == 0) return;

            Point currentMousePos = Control.MousePosition;
            int dx = currentMousePos.X - lastMousePosition.X;
            int dy = currentMousePos.Y - lastMousePosition.Y;

            foreach (var c in dragStack)
            {
                c.Left += dx;
                c.Top += dy;
            }

            lastMousePosition = currentMousePos;
        }

        private void Panel_MouseUp(object sender, MouseEventArgs e)
        {
            // Pozivamo isti završetak kao prije (možeš refaktorirati zajedničku logiku)
            if (!isDragging || dragStack == null || dragStack.Count == 0) return;

            Console.WriteLine("Panel_MouseUp fired");

            isDragging = false;

            // Odjavi globalne evente odmah
            panel.MouseMove -= Panel_MouseMove;
            panel.MouseUp -= Panel_MouseUp;

            // Središte gornje karte
            Point centerPoint = new Point(
                dragStack[0].Left + dragStack[0].Width / 2,
                dragStack[0].Top + dragStack[0].Height / 2);

            // Pronađi foundation
            Panel nearestFoundation = FindNearestFoundation(centerPoint);
            if (nearestFoundation != null)
            {
                if (dragStack.Count == 1 && !HasCardBelow(dragStack[0]) &&
                    CanPlaceOnFoundation(dragStack[0], nearestFoundation))
                {
                    PlaceStackOnFoundation(new List<PictureBox> { dragStack[0] }, nearestFoundation, dragOrigin);

                    if (dragOrigin == panel.WastePile)
                        panel.RemoveFromWaste(dragStack[0]);
                }
                else
                {
                    ReturnStackToOrigin(dragStack, dragOrigin);
                }

                dragStack.Clear();
                return;
            }

            // Ako nije foundation, pokušaj tableau
            Panel nearestTableau = FindNearestColumn(centerPoint, dragStack[0]);
            if (nearestTableau != null && CanPlaceOnTableau(dragStack[0], nearestTableau))
            {
                PlaceStackInTableau(dragStack, nearestTableau, dragOrigin);

                if (dragOrigin == panel.WastePile)
                {
                    foreach (var c in dragStack)
                        panel.RemoveFromWaste(c);
                }

                dragStack.Clear();
                return;
            }

            panel.MoveCompleted();
            ReturnStackToOrigin(dragStack, dragOrigin);
            dragStack.Clear();
        }

        private void Card_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;

            PictureBox draggedCard = sender as PictureBox;
            if (draggedCard == null) return;

            Console.WriteLine($"Card_MouseDown fired for {draggedCard.Name}, Card: {draggedCard.Tag}, Enabled: {draggedCard.Enabled}");

            isDragging = true;
            lastMousePosition = Control.MousePosition;

            dragStack = GetStackFromControl(draggedCard);
            dragOrigin = draggedCard.Parent;

            foreach (var c in dragStack)
            {
                if (c.Parent == null) continue;

                Point screenPos = c.Parent.PointToScreen(c.Location);
                panel.Controls.Add(c);
                Point clientPos = panel.PointToClient(screenPos);
                c.Location = clientPos;
                c.BringToFront();
                c.Cursor = Cursors.Hand;
            }

            panel.MouseMove += Panel_MouseMove;
            panel.MouseUp += Panel_MouseUp;
        }

        public void Card_MouseUp(object sender, MouseEventArgs e)
        {
            if (!isDragging || dragStack.Count == 0) return;

            isDragging = false;

            Console.WriteLine($"Card_MouseUp fired for {dragStack[0].Name}, Card: {dragStack[0].Tag}");

            foreach (var c in dragStack)
                c.Capture = false;

            Point centerPoint = new Point(
                dragStack[0].Left + dragStack[0].Width / 2,
                dragStack[0].Top + dragStack[0].Height / 2);

            Panel nearestFoundation = FindNearestFoundation(centerPoint);
            if (nearestFoundation != null)
            {
                if (dragStack.Count == 1 && !HasCardBelow(dragStack[0]) &&
                    CanPlaceOnFoundation(dragStack[0], nearestFoundation))
                {
                    PlaceStackOnFoundation(new List<PictureBox> { dragStack[0] }, nearestFoundation, dragOrigin);
                    if (dragOrigin == panel.WastePile)
                        panel.RemoveFromWaste(dragStack[0]);
                }
                else
                {
                    ReturnStackToOrigin(dragStack, dragOrigin);
                }
                dragStack.Clear();
                return;
            }

            Panel nearestTableau = FindNearestColumn(centerPoint, dragStack[0]);
            if (nearestTableau != null && CanPlaceOnTableau(dragStack[0], nearestTableau))
            {
                PlaceStackInTableau(dragStack, nearestTableau, dragOrigin);
                if (dragOrigin == panel.WastePile)
                {
                    foreach (var c in dragStack)
                        panel.RemoveFromWaste(c);
                }
                dragStack.Clear();
                return;
            }

            panel.MoveCompleted();
            ReturnStackToOrigin(dragStack, dragOrigin);
            dragStack.Clear();
        }

        public void Card_MouseMove(object sender, MouseEventArgs e)
        {
            if (!isDragging || dragStack.Count == 0) return;

            Point currentMousePos = Control.MousePosition;
            int dx = currentMousePos.X - lastMousePosition.X;
            int dy = currentMousePos.Y - lastMousePosition.Y;

            foreach (var c in dragStack)
            {
                c.Left += dx;
                c.Top += dy;
            }

            lastMousePosition = currentMousePos;
            
        }

        private List<PictureBox> GetStackFromControl(PictureBox start)
        {
            var stack = new List<PictureBox>();
            if (!(start.Parent is Panel parent)) return stack;

            // Waste pile: only one card allowed
            if (parent == panel.WastePile)
            {
                stack.Add(start);
                return stack;
            }

            // Skupi sve karte od kliknute prema dolje (samo licem prema gore)
            var orderedCards = parent.Controls.Cast<PictureBox>().OrderBy(c => c.Top).ToList();
            bool add = false;

            foreach (var pb in orderedCards)
            {
                if (pb == start) add = true;
                if (add && pb.BackColor == Color.White)
                {
                    stack.Add(pb);
                }
            }

            // Ako povlačite na temelj, ograničite se samo na gornju karticu
            Point mousePos = Control.MousePosition;
            foreach (var foundation in panel.foundations)
            {
                Rectangle rect = foundation.RectangleToScreen(foundation.ClientRectangle);
                if (rect.Contains(mousePos))
                    return new List<PictureBox> { start };
            }

            return stack;
        }

        private Panel FindNearestFoundation(Point point)
        {
            foreach (var foundation in foundations)
            {
                // 🔹 Nevidljivi "hitbox" — proširenje područja za lakše prepoznavanje
                Rectangle hitbox = new Rectangle(
                    foundation.Left - 15,
                    foundation.Top - 15,
                    foundation.Width + 30,
                    foundation.Height + 30
                );

                if (hitbox.Contains(point))
                    return foundation;
            }

            return null;
        }

        private Panel FindNearestColumn(Point point, PictureBox draggedCard)
        {
            Panel nearest = null;
            int minDist = int.MaxValue;
            int threshold = cardWidth + 20;

            foreach (var col in tableauColumns)
            {
                if (!CanPlaceOnTableau(draggedCard, col)) continue;

                int colCenter = col.Left + col.Width / 2;
                int dx = Math.Abs(colCenter - point.X);
                if (dx < minDist)
                {
                    minDist = dx;
                    nearest = col;
                }
            }

            if (minDist < threshold) return nearest;

            if (nearest == null)
            {
                if (point.X < tableauColumns.First().Left + cardWidth &&
                    CanPlaceOnTableau(draggedCard, tableauColumns.First()))
                    return tableauColumns.First();

                if (point.X > tableauColumns.Last().Right - cardWidth &&
                    CanPlaceOnTableau(draggedCard, tableauColumns.Last()))
                    return tableauColumns.Last();
            }

            return nearest;
        }

        private bool CanPlaceOnFoundation(PictureBox cardPb, Panel foundation)
        {
            Card card = panel.GetCardFromPictureBox(cardPb);
            if (card == null) return false;

            var cardsInFoundation = foundation.Controls.Cast<PictureBox>()
                                        .OrderBy(c => c.Top)
                                        .ToList();

            if (HasCardBelow(cardPb)) return false;

            // Prazan temelj: može se položiti samo as
            if (cardsInFoundation.Count == 0)
                return card.Rank == "A";

            // Neprazan temelj: mora biti ista boja i točno 1 rang viša od gornje karte
            Card topCard = panel.GetCardFromPictureBox(cardsInFoundation.Last());
            if (topCard == null) return false;

            return card.Suit == topCard.Suit && card.GetRankValue() == topCard.GetRankValue() + 1;
   
        }

        private void PlaceStackInTableau(List<PictureBox> stack, Panel tableau, Control sourcePile)
        {
            int startTop = 0;
            if (tableau.Controls.Count > 0)
            {
                var lastCard = tableau.Controls.Cast<PictureBox>().OrderBy(c => c.Top).Last();
                startTop = lastCard.Top + 30;
            }

            stack = stack.OrderBy(c => c.Top).ToList();

            foreach (var c in stack)
            {
                if (c.Parent == panel.WastePile)
                {
                    panel.RemoveFromWaste(c);
                }

                c.Parent = tableau;
                c.Left = 0;
                c.Top = startTop;
                c.BringToFront();
                startTop += 30;
            }

            // Koristite dragOrigin snimljen na početku povlačenja kao izvor
            string source;

            if (sourcePile == panel.WastePile)
            {
                source = GetWasteCardSourceDescription(stack[0]);
            }
            else
            {
                source = GetTableauColumnName(sourcePile);
            }

            string destination = GetTableauColumnName(tableau);
            string cardDesc = GetCardDescription(stack[0]);
            string beneathDesc = GetBeneathStackDescription(stack); 

            string moveNotation = $"{cardDesc} from {source} to {destination}";
            if (!string.IsNullOrEmpty(beneathDesc))
            {
                moveNotation += $" above {beneathDesc}";
            }
            panel.RecordMove(moveNotation);


            FlipTopCardFaceUp(tableau);

            var moveRecord = new MoveRecord(
                    cardDesc,
                    source,
                    destination,
                    beneathDesc,
                    false
                )
            {
                OriginalTop = stack[0].Top // pohrani trenutni Y položaj
            };
            panel.moveRecords.Add(moveRecord);


            panel.UpdateFaceDownCardsClickable(tableau);
            panel.MoveCompleted();
        }

        private void PlaceStackOnFoundation(List<PictureBox> stack, Panel foundation, Control sourcePile)
        {
            if (stack.Count == 0) return;

            PictureBox movedCard = stack[0];

            if (dragOrigin == panel.WastePile)
                panel.RemoveFromWaste(movedCard);

            int top = 0;
            if (foundation.Controls.Count > 0)
            {
                var lastCard = foundation.Controls.Cast<PictureBox>().OrderBy(c => c.Top).Last();
                top = lastCard.Top + 1;
            }

            movedCard.Parent = foundation;
            movedCard.Left = 0;
            movedCard.Top = top;
            movedCard.BringToFront();

            Card cardData = panel.GetCardFromPictureBox(movedCard);
            if (cardData != null)
            {
                movedCard.BackColor = Color.White;
                movedCard.Image = panel.CreateCardImage(cardData);
                MakeCardDraggable(movedCard);
            }

            // Koristite dragOrigin kao izvor za ispravno snimanje poteza
            string source;

            if (sourcePile == panel.WastePile)
            {
                source = GetWasteCardSourceDescription(stack[0]);
            }
            else
            {
                source = GetTableauColumnName(sourcePile);
            }

            string destination = GetFoundationName(foundation);
            string cardDesc = GetCardDescription(stack[0]);
            string beneathDesc = GetBeneathStackDescription(stack); // ako želite više od jedne kartice ispod

            string moveNotation = $"{cardDesc} from {source} to {destination}";
            if (!string.IsNullOrEmpty(beneathDesc))
            {
                moveNotation += $" above {beneathDesc}";
            }
            panel.RecordMove(moveNotation);

            var moveRecord = new MoveRecord(
                cardDesc,
                source,
                destination,
                beneathDesc,
                false
            )
            {
                OriginalTop = stack[0].Top // pohrani trenutni Y položaj
            };
            panel.moveRecords.Add(moveRecord);

            panel.UpdateFaceDownCardsClickable(foundation);
            panel.MoveCompleted();
        }

        private bool CanPlaceOnTableau(PictureBox cardPb, Panel tableau)
        {
            var cardsInTableau = tableau.Controls.Cast<PictureBox>().OrderBy(c => c.Top).ToList();
            Card card = panel.GetCardFromPictureBox(cardPb);
            if (card == null) return false;

            if (cardsInTableau.Count == 0)
                return string.Equals(card.Rank, "K", StringComparison.OrdinalIgnoreCase);

            Card topCard = panel.GetCardFromPictureBox(cardsInTableau.Last());
            if (topCard == null) return false;

            if (cardsInTableau.Last().BackColor != Color.White)
                return false;


            return panel.IsAlternateColor(card, topCard) &&
                   card.GetRankValue() == topCard.GetRankValue() - 1;
            

        }

        private string GetTableauColumnName(Control col)
        {
            if (col == null) return "?";

            if (panel.foundations.Contains(col))
            {
                int foundationIdx = panel.foundations.IndexOf(col as Panel);
                return $"A{foundationIdx + 1}";
            }

            Panel panelCol = col as Panel;
            if (panelCol == null) return "T?";
            int idx = panel.tableauColumns.IndexOf(panelCol);
            if (idx < 0) return "T?";
            return $"T{idx + 1}";
        }

        private string GetBeneathStackDescription(List<PictureBox> stack)
        {
            if (stack.Count < 2) return null;

            var descs = new List<string>();
            // Od druge do posljednje karte (ispod gornje karte)
            for (int i = 1; i < stack.Count; i++)
            {
                var card = panel.GetCardFromPictureBox(stack[i]);
                if (card != null)
                {
                    descs.Add($"{card.Rank}{card.Suit.ToString()[0]}");
                }
            }
            return string.Join(", ", descs); // Ili neki drugi razdjelnik koji preferirate, npr. "iznad"
        }

        string GetWasteCardSourceDescription(PictureBox cardPb)
        {
            var wasteCards = panel.WastePile.Controls.Cast<PictureBox>().OrderBy(c => c.Left).ToList();
            int idx = wasteCards.IndexOf(cardPb);
            if (idx == -1) return "W?";
            return $"W{idx + 1}";
        }

        private string GetFoundationName(Control foundation)
        {
            int idx = panel.foundations.IndexOf((Panel)foundation);
            return $"A{(idx + 1)}";
        }

        public string GetCardDescription(PictureBox cardPb)
        {
            Card c = panel.GetCardFromPictureBox(cardPb);
            if (c == null) return "Unknown";
            return $"{c.Rank}{c.Suit.ToString()[0]}"; // npr. "K♠" ili "A♥"
        }

    }


}
