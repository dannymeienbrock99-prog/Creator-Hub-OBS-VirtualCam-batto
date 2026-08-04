using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
using CreatorHubLive.Models;
using CreatorHubLive.Services;

namespace CreatorHubLive;

public partial class MainWindow : Window
{
    private readonly SettingsService _settingsService = new();
    private readonly ObsWebSocketClient _obs = new();
    private readonly MultistreamService _multistream = new();
    private AppSettings _settings;
    private bool _live;

    public ObservableCollection<StreamTarget> Targets { get; }

    public MainWindow()
    {
        InitializeComponent();
        _settings = _settingsService.Load();
        Targets = new ObservableCollection<StreamTarget>(_settings.Targets);
        TargetsGrid.ItemsSource = Targets;
        LoadSettingsIntoUi();
        AddStatus("Creator Hub Live bereit.");
        AddStatus("OBS muss auf den lokalen Relay-Eingang streamen.");
    }

    private void LoadSettingsIntoUi()
    {
        HostTextBox.Text = _settings.ObsHost;
        PortTextBox.Text = _settings.ObsPort.ToString();
        ObsPasswordBox.Password = _settings.ObsPassword;
        FfmpegPathTextBox.Text = _settings.FfmpegPath;
        RelayUrlTextBox.Text = _settings.RelayListenUrl;
        VirtualCamCheckBox.IsChecked = _settings.StartVirtualCamera;
    }

    private void ReadUiIntoSettings()
    {
        if (!int.TryParse(PortTextBox.Text, out int port))
            throw new InvalidOperationException("Der OBS-Port ist ungültig.");

        _settings.ObsHost = HostTextBox.Text.Trim();
        _settings.ObsPort = port;
        _settings.ObsPassword = ObsPasswordBox.Password;
        _settings.FfmpegPath = FfmpegPathTextBox.Text.Trim();
        _settings.RelayListenUrl = RelayUrlTextBox.Text.Trim();
        _settings.StartVirtualCamera = VirtualCamCheckBox.IsChecked == true;
        _settings.Targets = Targets.ToList();
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            TargetsGrid.CommitEdit();
            ReadUiIntoSettings();
            _settingsService.Save(_settings);
            AddStatus("Einstellungen verschlüsselt gespeichert.");
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    private void StartObsButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            ObsProcessService.StartObs();
            AddStatus("OBS Studio gestartet oder bereits aktiv.");
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    private async void ConnectButton_Click(object sender, RoutedEventArgs e)
    {
        await RunUiActionAsync(async () =>
        {
            ReadUiIntoSettings();
            AddStatus($"Verbinde mit OBS unter {_settings.ObsHost}:{_settings.ObsPort} ...");
            await _obs.ConnectAsync(_settings.ObsHost, _settings.ObsPort, _settings.ObsPassword);
            ConnectButton.Content = "OBS verbunden";
            AddStatus("OBS WebSocket verbunden.");
        });
    }

    private async void LiveButton_Click(object sender, RoutedEventArgs e)
    {
        await RunUiActionAsync(async () =>
        {
            TargetsGrid.CommitEdit();
            ReadUiIntoSettings();
            _settingsService.Save(_settings);

            if (!_live)
            {
                if (!_obs.IsConnected)
                    await _obs.ConnectAsync(_settings.ObsHost, _settings.ObsPort, _settings.ObsPassword);

                if (_settings.StartVirtualCamera)
                {
                    try
                    {
                        await _obs.StartVirtualCamAsync();
                        AddStatus("OBS Virtual Camera: AKTIV");
                    }
                    catch (Exception ex)
                    {
                        AddStatus("Virtual Camera: " + ex.Message);
                    }
                }

                await _multistream.StartAsync(
                    _settings.FfmpegPath,
                    _settings.RelayListenUrl,
                    Targets,
                    AddStatus);

                await _obs.StartStreamAsync();
                _live = true;
                LiveButton.Content = "ALLE STREAMS BEENDEN";
                LiveButton.Background = Brushes.DarkSlateGray;
                TargetsGrid.Items.Refresh();
                AddStatus("OBS-Eingang und alle ausgewählten Plattformausgaben sind gestartet.");
            }
            else
            {
                _multistream.Stop(AddStatus);
                try { await _obs.StopStreamAsync(); }
                catch (Exception ex) { AddStatus("OBS Stream Stop: " + ex.Message); }

                if (_settings.StartVirtualCamera)
                {
                    try { await _obs.StopVirtualCamAsync(); }
                    catch (Exception ex) { AddStatus("Virtual Camera Stop: " + ex.Message); }
                }

                foreach (StreamTarget target in Targets)
                    target.Status = "Beendet";

                _live = false;
                LiveButton.Content = "LIVE GEHEN";
                LiveButton.Background = Brushes.Red;
                TargetsGrid.Items.Refresh();
                AddStatus("Alle Streams beendet.");
            }
        });
    }

    private async Task RunUiActionAsync(Func<Task> action)
    {
        try
        {
            IsEnabled = false;
            await action();
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
        finally
        {
            IsEnabled = true;
        }
    }

    private void ShowError(Exception ex)
    {
        AddStatus("FEHLER: " + ex.Message);
        MessageBox.Show(ex.Message, "Creator Hub Live", MessageBoxButton.OK, MessageBoxImage.Error);
    }

    private void AddStatus(string text)
    {
        Dispatcher.Invoke(() => StatusList.Items.Insert(0, $"[{DateTime.Now:HH:mm:ss}] {text}"));
    }

    protected override async void OnClosed(EventArgs e)
    {
        _multistream.Dispose();
        await _obs.DisposeAsync();
        base.OnClosed(e);
    }
}
