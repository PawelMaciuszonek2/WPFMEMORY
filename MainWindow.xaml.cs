    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media.Imaging; // Do obsługi obrazków

    namespace MemoryGameWPF
    {
        public partial class MainWindow : Window
        {
            // Zmienne gry
            private Button firstCard = null;
            private Button secondCard = null;
            private bool isChecking = false;
            private int moves = 0;

            // Lista nazw plików (bez rozszerzenia .png)
            private List<string> nazwyPlikow = new List<string>()
            {
                "karta1", "karta2", "karta3", "karta4",
                "karta5", "karta6", "karta7", "karta8"
            };

            // Zmienne do przechowywania załadowanych grafik
            private BitmapImage obrazTylu;
            private Dictionary<string, BitmapImage> obrazyPrzodow = new Dictionary<string, BitmapImage>();

            public MainWindow()
            {
                InitializeComponent();
                ZaladujObrazy(); // Najpierw ładujemy grafiki
                StartNewGame();  // Potem startujemy grę
            }

            // --- POPRAWIONA METODA ŁADOWANIA OBRAZÓW ---
            private void ZaladujObrazy()
            {
                try
                {
                    // 1. Ładujemy tył karty
                    // UriKind.Relative oznacza, że szukamy w folderze obok pliku .exe
                    var uriTyl = new Uri("zdjecia/tyl.png", UriKind.Relative);
                    obrazTylu = new BitmapImage();
                    obrazTylu.BeginInit();
                    obrazTylu.UriSource = uriTyl;
                    obrazTylu.CacheOption = BitmapCacheOption.OnLoad; // Wczytaj do pamięci od razu
                    obrazTylu.EndInit();

                    // 2. Ładujemy przody kart
                    foreach (string nazwa in nazwyPlikow)
                    {
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
                    MessageBox.Show($"Błąd wczytywania obrazków.\nUpewnij się, że folder 'zdjecia' jest w folderze gry, a pliki mają ustawione 'Copy to Output Directory'.\n\nSzczegóły: {e.Message}");
                    Application.Current.Shutdown();
                }
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

                Random rnd = new Random();

                // Tworzymy pary i tasujemy
                var pary = nazwyPlikow.Concat(nazwyPlikow).ToList();
                var potasowane = pary.OrderBy(x => rnd.Next()).ToList();

                foreach (var nazwaPliku in potasowane)
                {
                    Button btn = new Button();
                    btn.Style = (Style)FindResource("CardButtonStyle");

                    // Tworzymy kontrolkę Image wewnątrz przycisku
                    Image img = new Image();
                    img.Source = obrazTylu; // Na początku widać tył
                    img.Stretch = System.Windows.Media.Stretch.Uniform; // Skalowanie obrazka

                    btn.Content = img;
                    btn.Tag = nazwaPliku; // Zapamiętujemy co to za karta w Tagu
                    btn.Click += Card_Click;

                    GameGrid.Children.Add(btn);
                }
            }

            private async void Card_Click(object sender, RoutedEventArgs e)
            {
                Button clickedBtn = sender as Button;

                // Blokada: jeśli sprawdzamy, przycisk wyłączony lub to ta sama karta
                if (isChecking || !clickedBtn.IsEnabled || clickedBtn == firstCard)
                    return;

                // Odkryj kartę (zmień źródło obrazka)
                string nazwaKarty = (string)clickedBtn.Tag;
                ((Image)clickedBtn.Content).Source = obrazyPrzodow[nazwaKarty];

                if (firstCard == null)
                {
                    firstCard = clickedBtn;
                    firstCard.IsEnabled = false; // Zablokuj pierwszą klikniętą
                }
                else
                {
                    secondCard = clickedBtn;
                    secondCard.IsEnabled = false; // Zablokuj drugą klikniętą

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
                    // PARY PASUJĄ
                    // Karty zostają odkryte i zablokowane (IsEnabled = false ustawiliśmy wcześniej)
                    CheckWinCondition();
                }
                else
                {
                    // NIE PASUJĄ
                    await Task.Delay(1000); // Czekaj 1 sekundę

                    // Zakryj karty z powrotem
                    ((Image)firstCard.Content).Source = obrazTylu;
                    ((Image)secondCard.Content).Source = obrazTylu;

                    // Odblokuj przyciski
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
                    MessageBox.Show($"Gratulacje! Wygrałeś w {moves} ruchach.");
                }
            }

            private void UpdateMovesUI()
            {
                txtMoves.Text = $"Ruchy: {moves}";
            }
        }
    }