namespace Search.OpenAI.RagAudio.Web.Components.Layout;

public sealed partial class NavMenu(ILocalStorageService localStorage, AppJSModule js)
{
    private Dialog? _dialog;
    private ElementReference? _audioElement;

    private string? _selectedMicrophone;
    private string? _selectedSpeaker;
    private string? _selectedSpeakerLabel;
    private string? _selectedVoice;

    private MediaDeviceInfo[] _microphones = [];
    private MediaDeviceInfo[] _speakers = [];
    private readonly string[] _voices =
    [
        nameof(ConversationVoice.Alloy),
        nameof(ConversationVoice.Echo),
        nameof(ConversationVoice.Shimmer)
    ];

    private IOptionValue[] _microphoneOptions = [];
    private IOptionValue[] _speakerOptions = [];
    private IOptionValue[] _voiceOptions = [];

    private Task ShowOpenAIDialogAsync(MouseEventArgs args) => InvokeAsync(async () =>
    {
        _microphones = await js.GetClientMicrophonesAsync();
        _speakers = await js.GetClientSpeakersAsync();

        _selectedMicrophone = await localStorage.GetItemAsync<string>("microphone") ?? _microphones.FirstOrDefault()?.DeviceId;
        _selectedSpeaker = await localStorage.GetItemAsync<string>("speaker") ?? _speakers.FirstOrDefault()?.DeviceId;
        _selectedSpeakerLabel = _speakers.FirstOrDefault(s => s.DeviceId == _selectedSpeaker)?.Label;
        _selectedVoice = await localStorage.GetItemAsync<string>("voice") ?? _voices.FirstOrDefault();

        _microphoneOptions = _microphones.ToOptionValues(m => m.DeviceId, m => m.Label, m => m.DeviceId == _selectedMicrophone);
        _speakerOptions = _speakers.ToOptionValues(s => s.DeviceId, s => s.Label, s => s.DeviceId == _selectedSpeaker);
        _voiceOptions = _voices.ToOptionValues(v => v, v => v, v => v == _selectedVoice);

        if (_dialog is not null)
        {
            await _dialog.ShowAsync(new DialogOptions(
                Title: "⚙️ OpenAI: Realtime API",
                Message: "Configure realtime microphone, speaker, and voice:",
                PrimaryButtonText: "Save",
                SecondaryButtonText: "Close",
                ButtonClicked: OnButtonClicked
            ));
        }

        if (_audioElement.HasValue && _selectedSpeaker is not null)
        {
            await js.SetAudioOutputAsync(_audioElement.Value, _selectedSpeaker);
        }

        StateHasChanged();
    });

    private Task OnButtonClicked(DialogButtonChoice choice) => InvokeAsync(async () =>
    {
        // Save settings when user selects "Save"
        if (choice is DialogButtonChoice.Primary)
        {
            await localStorage.SetItemAsync("microphone", _selectedMicrophone);
            await localStorage.SetItemAsync("speaker", _selectedSpeaker);
            await localStorage.SetItemAsync("voice", _selectedVoice);
        }

        StateHasChanged();
    });

    private Task OnMicrophoneSelected(IOptionValue microphoneOption) => InvokeAsync(() =>
    {
        _selectedMicrophone = microphoneOption?.Value;

        StateHasChanged();
    });

    private Task OnSpeakerSelected(IOptionValue speakerOption) => InvokeAsync(async () =>
    {
        _selectedSpeaker = speakerOption?.Value;
        _selectedSpeakerLabel = speakerOption?.Name;

        if (_audioElement.HasValue && _selectedSpeaker is not null)
        {
            await js.SetAudioOutputAsync(_audioElement.Value, _selectedSpeaker);
        }

        StateHasChanged();
    });

    private Task OnVoiceSelected(IOptionValue voiceOption) => InvokeAsync(() =>
    {
        _selectedVoice = voiceOption?.Value;

        StateHasChanged();
    });
}
