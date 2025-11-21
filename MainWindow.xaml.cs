using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives; // Potrzebne dla UniformGrid
using System.Windows.Media.Imaging;

namespace MemoryGameWPF
{
    public partial class MainWindow : Window
    {
        // Zmienne gry
        private Button firstCard = null;
        private Button secondCard = null;
        private bool isChecking = false;
        private int moves = 0;

        // --- NOWE: Aktualny rozmiar siatki (domyślnie 4x4) ---
        private int currentGridSize = 4;

        // --- NOWE: Rozszerzona lista nazw plików ---
        // UWAGA: Aby gra 8x8 działała, musisz mieć pliki od karta1.png do karta32.png!
        // Tutaj generuję listę automatycznie dla wygody, zakładając nazwy "karta1"..."karta32"
        private List<string> nazwyPlikow = new List<string>();

        // Zmienne do przechowywania załadowanych grafik
        private BitmapImage obrazTylu;
        private Dictionary<string, BitmapImage> obrazyPrzodow = new Dictionary<string, BitmapImage>();

        public MainWindow()
        {
            InitializeComponent();

            // Generujemy nazwy plików od karta1 do karta32
            for (int i = 1; i <= 32; i++)
            {
                nazwyPlikow.Add($"karta{i}");
            }

            ZaladujObrazy();
            StartNewGame();
        }

        private void ZaladujObrazy()
        {
            try
            {
                var uriTyl = new Uri("zdjecia/tyl.png", UriKind.Relative);
                obrazTylu = new BitmapImage();
                obrazTylu.BeginInit();
                obrazTylu.UriSource = uriTyl;
                obrazTylu.CacheOption = BitmapCacheOption.OnLoad;
                obrazTylu.EndInit();

                foreach (string nazwa in nazwyPlikow)
                {
                    // UWAGA: Tutaj program oczekuje, że pliki fizycznie istnieją.
                    // Jeśli nie masz 32 grafik, kod wyrzuci błąd w trybie 8x8.
                    // Dla testów możesz zduplikować swoje 8 grafik zmieniając im nazwy.
                    var uriPrzod = new Uri($"zdjecia/{nazwa}.png", UriKind.Relative);

                    BitmapImage przod = new BitmapImage();
                    przod.BeginInit();
                    przod.UriSource = uriPrzod;
                    przod.CacheOption = BitmapCacheOption.OnLoad;
                    przod.EndInit();

                    obrazyPrzodow.Add(nazwa, przod);
                }
            }
            catch (Exception e)
            {
                MessageBox.Show($"Błąd wczytywania obrazków.\nPotrzebujesz plików od karta1.png do karta32.png dla trybu 8x8!\n\nSzczegóły: {e.Message}");
                Application.Current.Shutdown();
            }
        }

        // --- NOWE: Obsługa zmiany poziomu trudności w ComboBox ---
        private void DifficultyComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Zabezpieczenie przed uruchomieniem przy starcie okna (zanim wszystko się załaduje)
            if (GameGrid == null) return;

            ComboBox cmb = sender as ComboBox;
            ComboBoxItem selectedItem = cmb.SelectedItem as ComboBoxItem;

            if (selectedItem != null)
            {
                // Pobieramy rozmiar z Tagu (ustawionego w XAML: "4", "6", "8")
                int size = int.Parse(selectedItem.Tag.ToString());
                currentGridSize = size;
                StartNewGame();
            }
        }

        private void BtnRestart_Click(object sender, RoutedEventArgs e)
        {
            StartNewGame();
        }

        // --- ZMODYFIKOWANA METODA STARTU ---
        private void StartNewGame()
        {
            if (GameGrid == null) return;

            GameGrid.Children.Clear();
            moves = 0;
            UpdateMovesUI();
            firstCard = null;
            secondCard = null;
            isChecking = false;

            // 1. Ustawiamy siatkę (UniformGrid)
            GameGrid.Rows = currentGridSize;
            GameGrid.Columns = currentGridSize;

            // 2. Obliczamy ile par potrzebujemy
            int totalCells = currentGridSize * currentGridSize; // np. 4x4=16, 6x6=36, 8x8=64
            int pairsNeeded = totalCells / 2;                   // np. 8, 18, 32

            // 3. Wybieramy odpowiednią ilość obrazków z naszej dużej listy
            // Take(pairsNeeded) bierze pierwsze X elementów z listy
            var wybraneNazwy = nazwyPlikow.Take(pairsNeeded).ToList();

            // 4. Sprawdzenie bezpieczeństwa (czy mamy dość grafik)
            if (wybraneNazwy.Count < pairsNeeded)
            {
                MessageBox.Show($"Za mało grafik! Potrzeba {pairsNeeded} par, a załadowano {wybraneNazwy.Count}.");
                return;
            }

            Random rnd = new Random();

            // 5. Tworzymy pary i tasujemy
            var pary = wybraneNazwy.Concat(wybraneNazwy).ToList();
            var potasowane = pary.OrderBy(x => rnd.Next()).ToList();

            foreach (var nazwaPliku in potasowane)
            {
                Button btn = new Button();

                // Opcjonalnie: Zmniejsz marginesy dla dużych plansz, żeby się mieściło
                btn.Margin = new Thickness(2);

                // Styl przycisku (jeśli masz go w XAML, odkomentuj)
                // btn.Style = (Style)FindResource("CardButtonStyle"); 

                Image img = new Image();
                img.Source = obrazTylu;
                img.Stretch = System.Windows.Media.Stretch.Uniform;

                btn.Content = img;
                btn.Tag = nazwaPliku;
                btn.Click += Card_Click;

                GameGrid.Children.Add(btn);
            }
        }

        private async void Card_Click(object sender, RoutedEventArgs e)
        {
            Button clickedBtn = sender as Button;

            if (isChecking || !clickedBtn.IsEnabled || clickedBtn == firstCard)
                return;

            string nazwaKarty = (string)clickedBtn.Tag;
            ((Image)clickedBtn.Content).Source = obrazyPrzodow[nazwaKarty];

            if (firstCard == null)
            {
                firstCard = clickedBtn;
                firstCard.IsEnabled = false;
            }
            else
            {
                secondCard = clickedBtn;
                secondCard.IsEnabled = false;
                moves++;
                UpdateMovesUI();
                await CheckMatch();
            }
        }

        private async Task CheckMatch()
        {
            isChecking = true;
            string tag1 = firstCard.Tag.ToString();
            string tag2 = secondCard.Tag.ToString();

            if (tag1 == tag2)
            {
                CheckWinCondition();
            }
            else
            {
                await Task.Delay(1000);
                ((Image)firstCard.Content).Source = obrazTylu;
                ((Image)secondCard.Content).Source = obrazTylu;
                firstCard.IsEnabled = true;
                secondCard.IsEnabled = true;
            }

            firstCard = null;
            secondCard = null;
            isChecking = false;
        }

        private void CheckWinCondition()
        {
            bool allDisabled = true;
            foreach (Button btn in GameGrid.Children)
            {
                if (btn.IsEnabled)
                {
                    allDisabled = false;
                    break;
                }
            }

            if (allDisabled)
            {
                MessageBox.Show($"Gratulacje! Poziom {currentGridSize}x{currentGridSize} ukończony w {moves} ruchach.");
            }
        }

        private void UpdateMovesUI()
        {
            txtMoves.Text = $"Ruchy: {moves}";
        }
    }
}