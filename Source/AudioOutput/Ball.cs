using SoundEngine.SoundSnippeds;
using System.Windows.Controls;

namespace AudioOutput
{
    internal class Ball : IDisposable
    {
        public double X { get; private set; }
        public double Y { get; private set; }
        public double Size { get; }
        public double SpeedY { get; }

        private Canvas canvas;
        private System.Windows.Shapes.Ellipse ellipse;
        private ISoundSnipped movingSound;
        private ISoundSnippedWithEndTrigger hitSound;

        public Ball(double x, double y, double size, double speedY, Canvas canvas, ISoundSnipped movingSound, ISoundSnippedWithEndTrigger hitSound)
        {
            this.X = x - size / 2;
            this.Y = y - size / 2;
            this.Size = size;
            this.SpeedY = speedY;
            this.canvas = canvas;

            this.ellipse = new System.Windows.Shapes.Ellipse()
            {
                Width = this.Size,
                Height = this.Size,
                Fill = System.Windows.Media.Brushes.Red,
                Stroke = System.Windows.Media.Brushes.Black,
                StrokeThickness = 2
            };

            this.canvas.Children.Add(this.ellipse);

            UpdatePosition();

            this.movingSound = movingSound;
            this.hitSound = hitSound;

            this.movingSound.Play();
        }

        private void UpdatePosition()
        {
            Canvas.SetLeft(this.ellipse, X);
            Canvas.SetTop(this.ellipse, Y);
        }

        public void Move(float time)
        {
            //If the ball is not visible then the hitSound is playing now
            if (this.ellipse.Visibility != System.Windows.Visibility.Visible) return;

            this.Y += this.SpeedY * time;

            UpdatePosition();

            if (this.Y < 0 || this.Y > this.canvas.ActualHeight - this.Size)
            {
                this.ellipse.Visibility = System.Windows.Visibility.Hidden;
                this.canvas.Children.Remove(this.ellipse);
                this.movingSound.Stop();
                this.hitSound.Play();
            }
        }

        public void Dispose()
        {
            //The Dispose is needet because the SoundGenerator has also a reference to this objects.
            //If the ball-object dosn't exist anymore then the movingSound and hitSound can be removed
            //from the SoundGenerator so save memory.
            this.movingSound.Dispose();
            this.hitSound.Dispose();
        }
    }
}
