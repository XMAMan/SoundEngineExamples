using AudioWpfControls.Helper;
using Microsoft.Win32;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SignalAnalyser;
using SoundEngine;
using SoundEngine.SoundSnippeds;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace AudioPlayer
{
    internal class ViewModel : ReactiveObject, IDisposable
    {
        private string WorkingDirectory = @"..\..\..\..\..\Data\";

        private SoundGenerator soundGenerator;                          // This comes from the XMAMan.SoundEngine-NuGet-Package

        private IAudioFileSnipped audioFile = null;
        private AudioFileAnalyser analyser = null;
        private IDisposable timer;

        [Reactive] public double PlayPosition { get; set; } = 0;
        [Reactive] public int ImageWidth { get; set; } = 600;
        [Reactive] public int ImageHeight { get; set; } = 50;
        [Reactive] public bool FileIsLoaded { get; set; } = false;
        [Reactive] public BitmapImage SampleImage { get; set; }
        [Reactive] public float[] FrequencySpectrum { get; set; }
        [Reactive] public int YSteps { get; set; } = 20; //This is how many boxes are drawn per column
        [Reactive] public string TextOutput { get; set; } = "";
        public float Volume { get { return audioFile != null ? audioFile.Volume : 0; } set { if (audioFile != null) audioFile.Volume = value; } }
        public float Speed { get { return audioFile != null ? audioFile.Speed : 0; } set { if (audioFile != null) audioFile.Speed = value; } }

        public ReactiveCommand<Unit, Unit> OpenFileCommand { get; private set; }
        public ReactiveCommand<Unit, Unit> PlayCommand { get; private set; }
        public ReactiveCommand<Unit, Unit> BreakCommand { get; private set; }
        public ReactiveCommand<Unit, Unit> StopCommand { get; private set; }

        public ViewModel()
        {
            this.soundGenerator = new SoundGenerator();

            this.timer = Observable.Interval(TimeSpan.FromMilliseconds(100))
                .Subscribe(x =>
                {
                    //Timer-Action
                    if (this.audioFile != null)
                    {
                        double position = this.audioFile.SampleIndex / this.audioFile.SampleCount;
                        this.PlayPosition = this.ImageWidth * position;

                        if (this.analyser != null)
                        {
                            this.FrequencySpectrum = this.analyser.GetFrequenceSpectrumFromTime(position);
                        }
                    }
                });

            OpenFileCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.Filter = "mp3/wav/wma|*.mp3;*.wav;*.wma|All files (*.*)|*.*";
                openFileDialog.InitialDirectory = System.IO.Path.GetFullPath(Environment.CurrentDirectory + "\\" + WorkingDirectory);
                if (openFileDialog.ShowDialog() == true)
                {
                    this.TextOutput = $"Load '{Path.GetFileName(openFileDialog.FileName)}' ...";

                    if (this.audioFile != null)
                    {
                        this.audioFile.Dispose();
                    }
                    this.audioFile = await Task.Run(() =>
                    {
                        return soundGenerator.AddSoundFile(openFileDialog.FileName);
                    });

                    this.FileIsLoaded = true;

                    this.TextOutput = "Analyse the signal...";

                    this.analyser = await Task.Run(() =>
                    {
                        return new AudioFileAnalyser(this.audioFile.AudioFileSamples, this.audioFile.SampleRate);
                    });

                    Bitmap bitmap = SignalImageCreator.GetLowPassSignalImage(analyser, this.ImageWidth, this.ImageHeight);
                    this.SampleImage = bitmap.ToBitmapImage();

                    //Since these are only getters, I have to trigger the PropertyChanged events manually
                    this.RaisePropertyChanged(nameof(Volume));
                    this.RaisePropertyChanged(nameof(Speed));

                    this.TextOutput = "";
                }
            });

            PlayCommand = ReactiveCommand.Create(() =>
            {
                Play();
            }, this.WhenAnyValue(x => x.FileIsLoaded));

            BreakCommand = ReactiveCommand.Create(() =>
            {
                Break();
            }, this.WhenAnyValue(x => x.FileIsLoaded));

            StopCommand = ReactiveCommand.Create(() =>
            {
                Stop();
            }, this.WhenAnyValue(x => x.FileIsLoaded));
        }

        private void Play()
        {
            if (this.audioFile == null) return;

            this.audioFile.Play();
        }

        private void Break()
        {
            if (this.audioFile == null) return;

            this.audioFile.Stop();
        }

        private void Stop()
        {
            if (this.audioFile == null) return;

            this.audioFile.Stop();
            this.audioFile.SampleIndex = 0;
        }

        public void Dispose()
        {
            this.soundGenerator.Dispose(); //Destroy the AudioTimer inside from this object
        }
    }
}
