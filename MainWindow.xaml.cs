using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MemoryGameWPF
{
    public partial class MainWindow : Window
    {
        private Button firstCard = null;
        private Button secondCard = null;
        private bool isChecking = false;
        private int moves = 0;

        private List<string> symbols = new List<string>()
        {
            "🚀", "🚀", "🌙", "🌙", "⭐", "⭐", "🪐", "🪐",
            "👽", "👽", "☄️", "☄️", "🛸", "🛸", "🌌", "🌌"
        };

        public MainWindow()
        {
            InitializeComponent();
            StartNewGame();
        }

        private void BtnRestart_Click(object sender, RoutedEventArgs e)
        {
            StartNewGame();
        }

        private void StartNewGame()
        {
            GameGrid.Children.Clear();
            moves = 0;
            UpdateMovesUI();
            firstCard = null;
            secondCard = null;
            isChecking = false;

            Random col = new Random();
            var shuffledSymbols = symbols.OrderBy(x => col.Next()).ToList();

            foreach (var symbol in shuffledSymbols)
            {
                Button btn = new Button();
                // Przypisanie stylu zdefiniowanego w XAML
                btn.Style = (Style)FindResource("CardButtonStyle");
                btn.Content = ""; // Pusty tekst na start
                btn.Tag = symbol;
                btn.Click += Card_Click;

                GameGrid.Children.Add(btn);
            }
        }

        private async void Card_Click(object sender, RoutedEventArgs e)
        {
            Button clickedBtn = sender as Button;

            // Jeśli zablokowane, już odkryte lub już znalezione (IsEnabled=false)
            if (isChecking || clickedBtn.Content.ToString() != "" || !clickedBtn.IsEnabled)
                return;

            // Odkryj kartę
            clickedBtn.Content = clickedBtn.Tag;
            clickedBtn.Background = Brushes.White;     // Tło odkrytej karty
            clickedBtn.Foreground = Brushes.Black;     // Kolor emoji

            if (firstCard == null)
            {
                firstCard = clickedBtn;
            }
            else
            {
                secondCard = clickedBtn;
                moves++;
                UpdateMovesUI();
                await CheckMatch();
            }
        }

        private async Task CheckMatch()
        {
            isChecking = true;

            if (firstCard.Tag.ToString() == secondCard.Tag.ToString())
            {
                // PARY PASUJĄ
                // Zmieniamy kolor na "sukces" (lekko przezroczysty lub zielony)
                var successBrush = new SolidColorBrush(Color.FromRgb(46, 204, 113));
                firstCard.Background = successBrush;
                secondCard.Background = successBrush;
                firstCard.IsEnabled = false;
                secondCard.IsEnabled = false;

                CheckWinCondition();
            }
            else
            {
                // NIE PASUJĄ - czekamy
                await Task.Delay(1000);

                // Reset wyglądu karty
                ResetCardAppearance(firstCard);
                ResetCardAppearance(secondCard);
            }

            firstCard = null;
            secondCard = null;
            isChecking = false;
        }

        private void ResetCardAppearance(Button btn)
        {
            btn.Content = ""; // Ukryj symbol
            // Przywróć kolor "tyłu" karty zdefiniowany w zasobach XAML
            btn.Background = (SolidColorBrush)FindResource("CardBack");
            btn.Foreground = (SolidColorBrush)FindResource("TextLight");
        }

        private void CheckWinCondition()
        {
            bool allDisabled = true;
            foreach (Button btn in GameGrid.Children)
            {
                if (btn.IsEnabled) { allDisabled = false; break; }
            }

            if (allDisabled)
            {
                MessageBox.Show($"Gratulacje! Ukonczyłeś grę w {moves} ruchach.", "Koniec gry");
            }
        }

        private void UpdateMovesUI()
        {
            txtMoves.Text = $"Ruchy: {moves}";
        }
    }
}