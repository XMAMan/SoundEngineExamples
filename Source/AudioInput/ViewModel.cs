using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Reactive;
using System.Reactive.Linq;
using SoundEngine;
using Microsoft.Win32;
using AudioWpfControls.Helper;

namespace AudioInput
{
    public class ViewModel : ReactiveObject, IDisposable
    {
        private SoundGenerator soundGenerator = new SoundGenerator();                          // This comes from the XMAMan.SoundEngine-NuGet-Package
        
        private List<float> recordData = new List<float>();
        private static string StartRecord = @"pack://application:,,,/AudioInput;component/StartRecord.png";
        private static string StopRecord = @"pack://application:,,,/AudioInput;component/StopRecord.png";

        public ReactiveCommand<Unit, Unit> StartStopCommand { get; private set; }
        [Reactive] public ImageSource StartStopImage { get; set; } = new BitmapImage(new Uri(ViewModel.StartRecord, UriKind.Absolute));
        public Interaction<string, string> SaveFileDialog { get; private set; } = new Interaction<string, string>(); //Input: Filter (openFileDialog.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";); Output: Dateiname von der Datei, die erzeugt werden soll

        public IEnumerable<string> InputDevices
        {
            get
            {
                return soundGenerator.AudioRecorder.GetAvailableDevices();
            }
        }

        public string SelectedInputDevice
        {
            get { return soundGenerator.AudioRecorder.SelectedDevice; }
            set
            {
                soundGenerator.AudioRecorder.SelectedDevice = value;
                this.RaisePropertyChanged(nameof(SelectedInputDevice));
            }
        }

        public IEnumerable<string> OutputDevices
        {
            get
            {
                return this.soundGenerator.GetAvailableOutputDevices();
            }
        }

        public string SelectedOutputDevice
        {
            get { return this.soundGenerator.SelectedOutputDevice; }
            set
            {
                this.soundGenerator.SelectedOutputDevice = value;
                this.RaisePropertyChanged(nameof(SelectedOutputDevice));
            }
        }

        public float Volume { get { return soundGenerator.AudioRecorder.Volume; } set { soundGenerator.AudioRecorder.Volume = value; } }
        public bool UseDelayEffect { get { return soundGenerator.AudioRecorder.UseDelayEffect; } set { soundGenerator.AudioRecorder.UseDelayEffect = value; } }
        public bool UseHallEffect { get { return soundGenerator.AudioRecorder.UseHallEffect; } set { soundGenerator.AudioRecorder.UseHallEffect = value; } }
        public bool UseGainEffect { get { return soundGenerator.AudioRecorder.UseGainEffect; } set { soundGenerator.AudioRecorder.UseGainEffect = value; } }
        public float Gain { get { return soundGenerator.AudioRecorder.Gain; } set { soundGenerator.AudioRecorder.Gain = value; } }
        public bool UsePitchEffect { get { return soundGenerator.AudioRecorder.UsePitchEffect; } set { soundGenerator.AudioRecorder.UsePitchEffect = value; } }
        public float Pitch { get { return soundGenerator.AudioRecorder.PitchEffect; } set { soundGenerator.AudioRecorder.PitchEffect = value; } }

        public bool UseVolumeLfo { get { return soundGenerator.AudioRecorder.UseVolumeLfo; } set { soundGenerator.AudioRecorder.UseVolumeLfo = value; } }
        public float VolumeLfoFrequency { get { return soundGenerator.AudioRecorder.VolumeLfoFrequency; } set { soundGenerator.AudioRecorder.VolumeLfoFrequency = value; } }

        [Reactive] public double OutputVolume { get; set; } = 0;

        public ViewModel()
        {
            this.SelectedInputDevice = this.InputDevices.FirstOrDefault();

            this.Volume = 0.5f;
            this.VolumeLfoFrequency = 50;

            this.soundGenerator.AudioRecorder.StartRecording();

            this.StartStopCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                if (this.IsRecording)
                {
                    await StopRecording();
                }
                else
                {
                    StartRecording();
                }
            });

            this.soundGenerator.AudioOutputCallback += (sender, buffer) =>
            {
                if (this.IsRecording)
                {
                    this.recordData.AddRange(buffer);
                }

                this.OutputVolume = buffer.Sum(x => Math.Abs(x)) / buffer.Length;
            };
        }

        public void StartRecording()
        {
            this.IsRecording = true;
            this.recordData.Clear();
        }

        public async Task StopRecording()
        {
            this.IsRecording = false;

            string fileName = await Task.Run(() =>
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.Filter = "mp3 (*.mp3)|*.mp3|Wave Format(*.wav)|*.wav|All files (*.*)|*.*";
                saveFileDialog.InitialDirectory = System.IO.Path.GetFullPath(Environment.CurrentDirectory);
                if (saveFileDialog.ShowDialog() == true)
                    return saveFileDialog.FileName;
                return null;
            });

            if (fileName != null)
            {
                this.soundGenerator.AudioFileWriter.ExportAudioStreamToFile(this.recordData.ToArray(), this.soundGenerator.SampleRate, fileName);
            }

            this.recordData.Clear();
        }

        private bool isRecording = false;
        public bool IsRecording
        {
            get => this.isRecording;
            set
            {
                this.isRecording = value;

                if (this.isRecording)
                    StartStopImage = new BitmapImage(new Uri(ViewModel.StopRecord, UriKind.Absolute));
                else
                    StartStopImage = new BitmapImage(new Uri(ViewModel.StartRecord, UriKind.Absolute));

                this.RaisePropertyChanged(nameof(IsRecording));
            }
        }

        public void Dispose()
        {
            this.soundGenerator.Dispose(); //Destroy the AudioTimer inside from this object
        }
    }
}
