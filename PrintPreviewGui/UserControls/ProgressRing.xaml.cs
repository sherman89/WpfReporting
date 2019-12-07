using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace Sherman.WpfReporting.Gui.UserControls
{
    public partial class ProgressRing
    {
        #region Dependency Property registrations

        public static readonly DependencyProperty DashesProperty = DependencyProperty.Register("Dashes", typeof(int), typeof(ProgressRing), new PropertyMetadata(32, DashesChanged));

        public static readonly DependencyProperty DiameterProperty = DependencyProperty.Register("Diameter", typeof(double), typeof(ProgressRing), new PropertyMetadata(150.00, DiameterChanged));

        public static readonly DependencyProperty DashHeightProperty = DependencyProperty.Register("DashHeight", typeof(double), typeof(ProgressRing), new PropertyMetadata(20.00, DashHeightChanged));

        public static readonly DependencyProperty DashWidthProperty = DependencyProperty.Register("DashWidth", typeof(double), typeof(ProgressRing), new PropertyMetadata(5.00, DashWidthChanged));

        public static readonly DependencyProperty DashFillProperty = DependencyProperty.Register("DashFill", typeof(SolidColorBrush), typeof(ProgressRing), new PropertyMetadata(Brushes.DimGray, DashFillChanged));

        public static readonly DependencyProperty ProgressFillProperty = DependencyProperty.Register("ProgressFill", typeof(SolidColorBrush), typeof(ProgressRing), new PropertyMetadata(Brushes.White, DashAnimationFillChanged));

        public static readonly DependencyProperty TailSizeProperty = DependencyProperty.Register("TailSize", typeof(int), typeof(ProgressRing), new PropertyMetadata(10, TailSizeChanged));

        public static readonly DependencyProperty AnimationSpeedProperty = DependencyProperty.Register("AnimationSpeed", typeof(double), typeof(ProgressRing), new PropertyMetadata(50.00, AnimationSpeedChanged));

        public static readonly DependencyProperty IsPlayingProperty = DependencyProperty.Register("IsPlaying", typeof(bool), typeof(ProgressRing), new PropertyMetadata(false, IsPlayingChanged));

        #endregion Dependency Property registrations

        private readonly Storyboard glowAnimationStoryBoard = new Storyboard();

        public ProgressRing()
        {
            Loaded += OnLoaded;
            InitializeComponent();
        }

        #region Dependency Properties

        public int Dashes
        {
            get => (int) GetValue(DashesProperty);
            set => SetValue(DashesProperty, value);
        }

        public double Diameter
        {
            get => (double) GetValue(DiameterProperty);
            set => SetValue(DiameterProperty, value);
        }

        public double Radius => Diameter / 2;

        public double DashHeight
        {
            get => (double) GetValue(DashHeightProperty);
            set => SetValue(DashHeightProperty, value);
        }

        public double DashWidth
        {
            get => (double) GetValue(DashWidthProperty);
            set => SetValue(DashWidthProperty, value);
        }

        public Brush DashFill
        {
            get => (SolidColorBrush) GetValue(DashFillProperty);
            set => SetValue(DashFillProperty, value);
        }

        public Brush ProgressFill
        {
            get => (SolidColorBrush) GetValue(ProgressFillProperty);
            set => SetValue(ProgressFillProperty, value);
        }

        public int TailSize
        {
            get => (int) GetValue(TailSizeProperty);
            set => SetValue(TailSizeProperty, value);
        }

        public double AnimationSpeed
        {
            get => (double) GetValue(AnimationSpeedProperty);
            set => SetValue(AnimationSpeedProperty, value);
        }

        public bool IsPlaying
        {
            get => (bool) GetValue(IsPlayingProperty);
            set => SetValue(IsPlayingProperty, value);
        }

        #endregion Dependency Properties

        private static void OnLoaded(object sender, RoutedEventArgs e)
        {
            var thisControl = sender as ProgressRing;
            Recreate(thisControl);
        }

        #region Dependency Property callbacks

        private static void DashesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var thisControl = d as ProgressRing;
            Recreate(thisControl);
        }

        private static void DiameterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var thisControl = d as ProgressRing;
            Recreate(thisControl);
        }

        private static void DashHeightChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var thisControl = d as ProgressRing;
            Recreate(thisControl);
        }

        private static void DashWidthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var thisControl = d as ProgressRing;
            Recreate(thisControl);
        }

        private static void DashFillChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var thisControl = d as ProgressRing;
            Recreate(thisControl);
        }

        private static void DashAnimationFillChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var thisControl = d as ProgressRing;
            Recreate(thisControl);
        }

        private static void TailSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var thisControl = d as ProgressRing;
            Recreate(thisControl);
        }

        private static void AnimationSpeedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ProgressRing thisControl && thisControl.IsLoaded)
            {
                thisControl.glowAnimationStoryBoard.Stop();
                thisControl.glowAnimationStoryBoard.Children.Clear();

                ApplyAnimations(thisControl);

                thisControl.glowAnimationStoryBoard.Begin();
            }
        }

        private static void IsPlayingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ProgressRing thisControl && thisControl.IsLoaded)
            {
                var isPlaying = (bool)e.NewValue;
                if (isPlaying)
                {
                    thisControl.glowAnimationStoryBoard.Begin();
                }
                else
                {
                    thisControl.glowAnimationStoryBoard.Stop();
                }
            }
        }

        #endregion Dependency Property callbacks

        private static void Recreate(ProgressRing thisControl)
        {
            if (thisControl.IsLoaded)
            {
                thisControl.glowAnimationStoryBoard.Stop();
                thisControl.glowAnimationStoryBoard.Children.Clear();
                thisControl.RootCanvas.Children.Clear();

                Validate(thisControl);
                BuildRing(thisControl);

                ApplyAnimations(thisControl);

                if (thisControl.IsPlaying)
                {
                    thisControl.glowAnimationStoryBoard.Begin();
                }
                else
                {
                    thisControl.glowAnimationStoryBoard.Stop();
                }
            }
        }

        private static void Validate(ProgressRing thisControl)
        {
            if (thisControl == null)
            {
                throw new ArgumentNullException(nameof(thisControl));
            }

            if (thisControl.TailSize > thisControl.Dashes)
            {
                throw new Exception("TailSize cannot be larger than amount of dashes");
            }
        }

        private static void BuildRing(ProgressRing thisControl)
        {
            var angleStep = (double) 360 / thisControl.Dashes;

            for (double i = 0; i < 360; i += angleStep)
            {
                var rect = new Rectangle
                {
                    Fill = thisControl.DashFill,
                    Height = thisControl.DashHeight,
                    Width = thisControl.DashWidth
                };

                //Rotate dash to follow circles circumference 
                var centerY = thisControl.Radius;
                var centerX = thisControl.DashWidth / 2;
                var rotateTransform = new RotateTransform(i, centerX, centerY);
                rect.RenderTransform = rotateTransform;

                var offset = thisControl.Radius - thisControl.DashWidth / 2;
                rect.SetValue(Canvas.LeftProperty, offset);

                thisControl.RootCanvas.Children.Add(rect);
            }

            thisControl.RootCanvas.Width = thisControl.Diameter;
            thisControl.RootCanvas.Height = thisControl.Diameter;
        }

        private static void ApplyAnimations(ProgressRing thisControl)
        {
            var baseColor = ((SolidColorBrush) thisControl.DashFill).Color;
            var animatedColor = ((SolidColorBrush) thisControl.ProgressFill).Color;

            var dashes = thisControl.RootCanvas.Children.OfType<Rectangle>().ToList();

            var animationPeriod = thisControl.AnimationSpeed;
            var glowDuration = animationPeriod * thisControl.TailSize;

            for (var i = 0; i < dashes.Count; i++)
            {
                var beginTime = TimeSpan.FromMilliseconds(animationPeriod * i);

                var colorAnimation = new ColorAnimationUsingKeyFrames
                {
                    BeginTime = beginTime,
                    RepeatBehavior = RepeatBehavior.Forever
                };

                var toFillColor = new LinearColorKeyFrame(animatedColor, TimeSpan.Zero);
                colorAnimation.KeyFrames.Add(toFillColor);

                var dimToBase = new LinearColorKeyFrame(baseColor, TimeSpan.FromMilliseconds(glowDuration));
                colorAnimation.KeyFrames.Add(dimToBase);

                var restingTime = animationPeriod * dashes.Count;
                var delay = new LinearColorKeyFrame(baseColor, TimeSpan.FromMilliseconds(restingTime));
                colorAnimation.KeyFrames.Add(delay);

                Storyboard.SetTarget(colorAnimation, dashes[i]);
                Storyboard.SetTargetProperty(colorAnimation, new PropertyPath("(Fill).(SolidColorBrush.Color)"));

                thisControl.glowAnimationStoryBoard.Children.Add(colorAnimation);
            }
        }
    }
}
