using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SoundEngine;
using SoundEngine.SoundSnippeds;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace AudioOutput
{
    internal class ViewModel : ReactiveObject, IDisposable
    {
        private string WorkingDirectory = @"..\..\..\..\..\Data\";


        private List<Ball> balls = new List<Ball>();                    // List of all balls on the canvas
        private System.Windows.Threading.DispatcherTimer timer;         // Timer is needet for moving the ball

        private SoundGenerator soundGenerator;                          // This comes from the XMAMan.SoundEngine-NuGet-Package
        private IMusicFileSnipped backGroundMusic;                      // This controls the background music
        private IFrequenceToneSnipped movingDownSound;                  // This controls the sound when the ball is moving down
        private IFrequenceToneSnipped movingUpSound;                    // This controls the sound when the ball is moving up
        private IAudioFileSnipped hitCeilingSound;                      // This controls the sound when the ball hits the ceiling
        private IMusicFileSnipped hitGroundSound;                       // This controls the sound when the ball hits the ground

        //Properties for the View
        [Reactive] public int BallCount { get; set; }


        //Properties for the Background Music
        public float BackgroundMusicVolume { get { return this.backGroundMusic.Volume; } set { this.backGroundMusic.Volume = value; this.RaisePropertyChanged(nameof(BackgroundMusicVolume)); } }
        public bool BackgroundMusicIsRunning { get { return this.backGroundMusic.IsRunning; } set { SetIsRunning(this.backGroundMusic, value); this.RaisePropertyChanged(nameof(BackgroundMusicIsRunning)); } }
        public bool BackgroundMusicIsLooping { get { return this.backGroundMusic.AutoLoop; } set { this.backGroundMusic.AutoLoop = value; this.RaisePropertyChanged(nameof(BackgroundMusicIsLooping)); } }
        public float BackgroundMusicSpeed { get { return this.backGroundMusic.KeyStrokeSpeed; } set { this.backGroundMusic.KeyStrokeSpeed = value; this.RaisePropertyChanged(nameof(BackgroundMusicSpeed)); } }
        public int BackgroundMusicPitch { get { return this.backGroundMusic.KeyShift; } set { this.backGroundMusic.KeyShift = value; this.RaisePropertyChanged(nameof(BackgroundMusicPitch)); } }


        //Properties for the moving down sound
        public float MovingDownVolume { get { return this.movingDownSound.Volume; } set { this.movingDownSound.Volume = value; this.RaisePropertyChanged(nameof(MovingDownVolume)); } }
        public float MovingDownPitch { get { return this.movingDownSound.Frequency; } set { this.movingDownSound.Frequency = value; this.RaisePropertyChanged(nameof(MovingDownPitch)); } }

        //Properties for the moving up sound
        public float MovingUpVolume { get { return this.movingUpSound.Volume; } set { this.movingUpSound.Volume = value; this.RaisePropertyChanged(nameof(MovingUpVolume)); } }
        public float MovingUpPitch { get { return this.movingUpSound.Frequency; } set { this.movingUpSound.Frequency = value; this.RaisePropertyChanged(nameof(MovingUpPitch)); } }


        //Properties for the Hit Sounds
        public float HitGroundVolume { get { return this.hitGroundSound.Volume; } set { this.hitGroundSound.Volume = value; this.RaisePropertyChanged(nameof(HitGroundVolume)); } }
        public int HitGroundPitch { get { return this.hitGroundSound.KeyShift; } set { this.hitGroundSound.KeyShift = value; this.RaisePropertyChanged(nameof(HitGroundPitch)); } }
        public float HitGroundSpeed { get { return this.hitGroundSound.GetSynthesizer(0).DecayTimeInMs / 1000; } 
            set 
            { 
                //The length and speed of the hitGround-note is defined by this three properties
                this.hitGroundSound.GetSynthesizer(0).DecayTimeInMs = value * 1000; 
                this.hitGroundSound.GetSynthesizer(0).FrequencyRampFactor = -(value * 2);
                this.hitGroundSound.KeyStrokeSpeed = 1 - value;
                this.RaisePropertyChanged(nameof(HitGroundSpeed)); 
            } }

        public float HitCeilingVolume { get { return this.hitCeilingSound.Volume; } set { this.hitCeilingSound.Volume = value; this.RaisePropertyChanged(nameof(HitCeilingVolume)); } }
        public float HitCeilingPitch { get { return this.hitCeilingSound.Pitch; } set { this.hitCeilingSound.Pitch = value; this.RaisePropertyChanged(nameof(HitCeilingPitch)); } }
        public float HitCeilingSpeed { get { return this.hitCeilingSound.Speed; } set { this.hitCeilingSound.Speed = value; this.RaisePropertyChanged(nameof(HitCeilingSpeed)); } }



        private void SetIsRunning(IMusicFileSnipped musicFile, bool value)
        {
            if (value)
            {
                musicFile.Play();
            }
            else
            {
                musicFile.Stop();
            }
        }

        public ViewModel()
        {
            LoadAllMusicData();

            this.timer = new System.Windows.Threading.DispatcherTimer();
            this.timer.Interval = new TimeSpan(0, 0, 0, 0, 30);//30 ms
            this.timer.Tick += Timer_Tick;
            this.timer.Start();
        }

        private void LoadAllMusicData()
        {
            this.soundGenerator = new SoundGenerator();

            if (Directory.Exists(WorkingDirectory) == false) throw new DirectoryNotFoundException("Could not found directory " + WorkingDirectory);

            this.backGroundMusic = soundGenerator.AddMusicFile(WorkingDirectory + "background6.music");
            this.backGroundMusic.IsRunningChanged += (isRunning) => { BackgroundMusicIsRunning = isRunning; };

            var movingSounds = this.soundGenerator.AddSynthSoundCollection(WorkingDirectory + "movingBall2.music");
            this.movingDownSound = movingSounds[0];
            this.movingUpSound = movingSounds[1];

            this.hitCeilingSound = this.soundGenerator.AddSoundFile(WorkingDirectory + "427788__fullmetaljedi__metal-to-glass05-broke-it.wav");

            this.hitGroundSound = this.soundGenerator.AddMusicFile(WorkingDirectory + "Drum.music");

            this.soundGenerator.Volume = 1;     // set the volume for all sounds
            this.backGroundMusic.Volume = 0.5f; // set only for the background music the volume
            this.MovingUpPitch = 50;
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            foreach (var ball in this.balls.ToList())
            {
                ball.Move((float)this.timer.Interval.TotalSeconds); //Move the ball
            }
        }

        public void HandleCanvasMouseClick(Canvas sender, Point point, MouseButtonEventArgs e)
        {
            double speedY;
            ISoundSnipped movingSound;
            ISoundSnippedWithEndTrigger hitSound;

            //GetCopy is needed because every ball creates his own sound so the same sound can be played multiple times
            //if we would use only a single ball then we don't need to use GetCopy
            if (e.ChangedButton == MouseButton.Left)
            {
                //Leftclick: Ball is moving down
                speedY = 50;
                movingSound = this.movingDownSound.GetCopy();
                hitSound = this.hitGroundSound.GetCopy();
            }
            else
            {
                //Rightclick: Ball is moving up
                speedY = -50;
                movingSound = this.movingUpSound.GetCopy();
                hitSound = this.hitCeilingSound.GetCopy();
            }

            var ball = new Ball(point.X, point.Y, 30, speedY, sender, movingSound, hitSound);

            //If the hitSound has finished then the ball is removed from the canvas
            hitSound.EndTrigger += () =>
            {
                this.balls.Remove(ball); //Remove the ball from the ball-List
                this.BallCount = this.balls.Count();
                ball.Dispose();
            };

            this.balls.Add(ball); //Add a new ball to the canvas
            this.BallCount = this.balls.Count();
        }

        public void Dispose()
        {
            this.soundGenerator.Dispose(); //Destroy the AudioTimer inside from this object
        }
    }
}
