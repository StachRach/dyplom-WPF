// WYKONAWCA:  Stanisław Rachwał
// KIERUNEK:   Inżynieria Biomedyczna, spec. Fizyka Medyczna
// STOPIEŃ:    1
// SEMESTR:    7

// ---------------------------------------------------------------------------------------------------------------------
//                                             IMPLEMENTACJE NUGETÓW
// ---------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Windows;
using System.IO.Ports;
using System.Windows.Media;
using InteractiveDataDisplay.WPF;
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
            _port = new SerialPort("COM3", 115200);
            _port.Open();
            LblName.Foreground = Brushes.Green;
            LblName.Content = "Arduino UNO";
                
            BtnCheck.IsEnabled = false;
            BtnFile.IsEnabled = true;
            BtnReset.IsEnabled = true;
            BtnShow.IsEnabled = true;
            BtnStart.IsEnabled = true;
            CmbType.IsEnabled = true;
            CmbAng.IsEnabled = true;
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
         
        switch (CmbType.Text)
        {
            case "Położenie":
                PortWrite("0");
                TxtStats.Clear();
                break;
            case "Szum":
                PortWrite("0");
                TxtStats.Clear();
                Plt.Title = "Pomiar szumu";
                break;
            case "Zmiana kątów RPY":
                PortWrite("1");
                TxtStats.Clear();
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
//                                           METODA KREŚLĄCA CHARAKTERYSTYKI
// ---------------------------------------------------------------------------------------------------------------------

    private void BtnShow_OnClick(object sender, RoutedEventArgs e)
    {
        var data = TxtStats.Text;
        var dataArray = data.Split('\n').Select(x => x.Split('\t')).ToArray();

        var x = ModifyData(dataArray, 0, dataArray.Length);
        
        List<double> y1;
        List<double> y2;

        switch (CmbType.Text)
        {
            case "Zmiana kątów RPY":
                y1 = ModifyData(dataArray, 1, dataArray.Length);
                y2 = ModifyData(dataArray, 2, dataArray.Length);
                var y3 = ModifyData(dataArray, 3, dataArray.Length);

                var line = new LineGraph
                {
                    Stroke = Brushes.Magenta,
                    StrokeThickness = 2
                };

                switch (CmbAng.Text)
                {
                    case "Yaw (OZ)":
                        line.Plot(x, y3);
                        break;
                    case "Roll (OY)":
                        line.Plot(x, y2);
                        break;
                    default:
                        line.Plot(x, y1);
                        break;
                }

                Plt.LegendVisibility = Visibility.Hidden;
                Grid.Children.Clear();
                Grid.Children.Add(line);
                break;
            default:
                switch (CmbAng.Text)
                {
                    case "Pitch (OX)":
                        y1 = ModifyData(dataArray, 1, dataArray.Length);
                        y2 = ModifyData(dataArray, 3, dataArray.Length);
                        PlotData(x, y1, y2);
                        break;
                    case "Roll (OY)":
                        y1 = ModifyData(dataArray, 2, dataArray.Length);
                        y2 = ModifyData(dataArray, 4, dataArray.Length);
                        PlotData(x, y1, y2);
                        break;
                    case "Yaw (OZ)":
                        MessageBox.Show("Brak odpowiednich danych.");
                        break;
                }
                break;
        }
    }

    private void PlotData(List<double> a, IEnumerable<double> b, IEnumerable<double> c)
    {
        var line1 = new LineGraph
        {
            Stroke = Brushes.Magenta,
            Description = "Pomiar bez filtra",
            StrokeThickness = 2
        };
        var line2 = new LineGraph
        {
            Stroke = Brushes.Cyan,
            Description = "Pomiar z filtrem",
            StrokeThickness = 2
        };
                
        line1.Plot(a, b);
        line2.Plot(a, c);
        
        Grid.Children.Clear();
        Grid.Children.Add(line1);
        Grid.Children.Add(line2);
    }
    
// ---------------------------------------------------------------------------------------------------------------------
//                                             METODA OBRABIAJĄCA DANE
// ---------------------------------------------------------------------------------------------------------------------

    private List<double> ModifyData(IReadOnlyList<string[]> d, int col, int len)
    {
        var temp = new List<double>();

        var index = CmbType.Text is "Położenie" or "Szum" ? 21 : 1;
        
        for (var i = index; i < len - 1; i++)
        {
            if (col != 0)
            {
                d[i][col] = d[i][col].Replace(".", ",");
            }
            
            temp.Add(double.Parse(d[i][col]));
        }

        return temp;
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