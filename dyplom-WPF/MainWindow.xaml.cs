// WYKONAWCA:  Stanisław Rachwał
// INDEKS:     180504
// KIERUNEK:   Inżynieria Biomedyczna, spec. Fizyka Medyczna
// STOPIEŃ:    1
// SEMESTR:    7

// ---------------------------------------------------------------------------------------------------------------------
//                                             IMPLEMENTACJE NUGETÓW
// ---------------------------------------------------------------------------------------------------------------------

using System;
using System.Linq;
using System.IO;
using System.Windows;
using System.IO.Ports;
using System.Windows.Media;
using DispatcherPriority = System.Windows.Threading.DispatcherPriority;

// ---------------------------------------------------------------------------------------------------------------------
//                                           OKREŚLENIE PRZESTRZENI NAZW
// ---------------------------------------------------------------------------------------------------------------------

namespace dyplom_WPF;

// ---------------------------------------------------------------------------------------------------------------------
//                                              GŁÓWNA KLASA PROGRAMU
// ---------------------------------------------------------------------------------------------------------------------

public partial class MainWindow
{
    private static SerialPort? _port;
    private double[][]? _dataArray;

    public MainWindow()
    {
        InitializeComponent();
    }
    
// ---------------------------------------------------------------------------------------------------------------------
//                             METODY OBSŁUGUJĄCE ZDARZENIA ZWIĄZANE Z ODBIOREM DANYCH
// ---------------------------------------------------------------------------------------------------------------------

    private void DataReceived(object sender, SerialDataReceivedEventArgs e)
    {
        var sp = (SerialPort)sender;
        var data = sp.ReadExisting();
        Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action<string>)Update, data);
    }

    private void Update(string data)
    {
        TxtStats.Text += data;
    }

// ---------------------------------------------------------------------------------------------------------------------
//                             BEZPIECZNA METODA WYSYŁANIA INFORMACJI PORTEM SZEREGOWYM
// ---------------------------------------------------------------------------------------------------------------------
    
    private static void PortWrite(string message)
    {
        if (_port is { IsOpen: true })
        {
            _port.Write(message);
        }
    }
    
// ---------------------------------------------------------------------------------------------------------------------
//                                  METODA SPRAWDZAJĄCA POŁĄCZENIE Z ARDUINO
// ---------------------------------------------------------------------------------------------------------------------

    private void BtnCheck_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            _port = new SerialPort("COM3", 38400);
            _port.Open();
            LblName.Foreground = Brushes.Green;
            LblName.Content = "Arduino UNO";
                
            BtnCheck.IsEnabled = false;
            BtnFile.IsEnabled = true;
            BtnReset.IsEnabled = true;
            BtnShow.IsEnabled = true;
            BtnStart.IsEnabled = true;
            CmbTime.IsEnabled = true;
            CmbTs.IsEnabled = true;
        }
        catch (IOException)
        {
            LblName.Foreground = Brushes.Crimson;
            LblName.Content = "Brak płytki";
        }
    }
    
// ---------------------------------------------------------------------------------------------------------------------
//                                     METODA ZARZĄDZAJĄCA NASTAWAMI POMIARÓW
// ---------------------------------------------------------------------------------------------------------------------

    private void BtnStart_OnClick(object sender, RoutedEventArgs e)
    {
         
        switch (CmbTime.Text)
        {
            case "Pojedynczy":
                PortWrite("0");
                TxtStats.Clear();
                BtnShow.IsEnabled = false;
                break;
            case "1 sekunda" when CmbTs.Text == "10 ms":
                PortWrite("1");
                TxtStats.Clear();
                BtnShow.IsEnabled = true;
                break;
            case "1 sekunda" when CmbTs.Text == "100 ms":
                PortWrite("11");
                TxtStats.Clear();
                BtnShow.IsEnabled = true;
                break;
            case "10 sekund" when CmbTs.Text == "10 ms":
                PortWrite("2");
                TxtStats.Clear();
                BtnShow.IsEnabled = true;
                break;
            case "10 sekund" when CmbTs.Text == "100 ms":
                PortWrite("22");
                TxtStats.Clear();
                BtnShow.IsEnabled = true;
                break;
        }
        if (_port != null) _port.DataReceived += DataReceived;
    }
    
// ---------------------------------------------------------------------------------------------------------------------
//                                     METODA ZAPISUJĄCA POMIARY DO PLIKU .TXT
// ---------------------------------------------------------------------------------------------------------------------

    private void BtnFile_OnClick(object sender, RoutedEventArgs e)
    {
        var appPath = Directory.GetParent(Environment.CurrentDirectory)?.Parent?.Parent?.FullName;
        var path =
            $"{appPath}\\logs\\logfile_{DateTime.Now:yyyy-MM-dd_HH-mm-ss-fff}.txt";
        File.WriteAllText(path, TxtStats.Text);
    }
    
// ---------------------------------------------------------------------------------------------------------------------
//                                        METODA KREŚLĄCA CHARAKTERYSTYKI
// ---------------------------------------------------------------------------------------------------------------------

    private void BtnShow_OnClick(object sender, RoutedEventArgs e)
    {
        var data = TxtStats.Text;
        var temp = data.Split('\n').Select(x => x.Split('\t')).ToArray();
        _dataArray = new double[temp.Length][];

        for (var i = 1; i < temp.Length; i++)
        {
            for (var j = 0; j < temp[0].Length; j++)
            {
                _dataArray[i - 1][j] = double.Parse(temp[i][j]);
            }
        }
    }

// ---------------------------------------------------------------------------------------------------------------------
//                                            METODA REGULUJĄCA OFFSET
// ---------------------------------------------------------------------------------------------------------------------

    private void BtnReset_OnClick(object sender, RoutedEventArgs e)
    {
        PortWrite("9");
        TxtStats.Clear();
        if (_port != null) _port.DataReceived += DataReceived;
    }
}