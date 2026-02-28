using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Timers;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace Assets_Editor
{
    public class ShowList : INotifyPropertyChanged
    {
        private uint id;
        private uint cid;
        private String name;

        private List<ImageSource> images;
        private ImageSource image;
        private Timer animationTimer;
        private int currentFrameIndex;
        private const double frameInterval = 100;
        private bool exported;
        public uint Id
        {
            get => id;
            set
            {
                if (id != value)
                {
                    id = value;
                    NotifyPropertyChanged(nameof(Id));
                }
            }
        }
        public uint Cid
        {
            get => cid;
            set
            {
                if (cid != value)
                {
                    cid = value;
                    NotifyPropertyChanged(nameof(Cid));
                }
            }
        }
        public List<ImageSource> Images
        {
            get => images;
            set
            {
                if (images != value)
                {
                    images = value;
                    NotifyPropertyChanged(nameof(Images));
                    ResetAnimation();
                }
            }
        }
        public ImageSource Image
        {
            get => image;
            set
            {
                if (image != value)
                {
                    image = value;
                    NotifyPropertyChanged(nameof(image));
                }
            }
        }
        public bool Exported
        {
            get => exported;
            set
            {
                if (exported != value)
                {
                    exported = value;
                    NotifyPropertyChanged(nameof(exported));
                }
            }
        }
        public String Name
        {
            get => name;
            set
            {
                if (name != value)
                {
                    name = value;
                    NotifyPropertyChanged(nameof(Name));
                }
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;

        public ShowList()
        {
            images = new List<ImageSource>();
            animationTimer = new Timer(frameInterval);
            animationTimer.Elapsed += OnAnimationTick;
            animationTimer.AutoReset = true;
        }


        private void OnAnimationTick(object sender, ElapsedEventArgs e)
        {
            // ensure images exist
            if (Images == null || Images.Count == 0)
                return;

            // ensure dispatcher is accessible
            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher == null || dispatcher.HasShutdownStarted || dispatcher.HasShutdownFinished)
                return;

            // try to animate
            try {
                dispatcher.Invoke(() => {
                    Image = Images[currentFrameIndex];
                    currentFrameIndex = (currentFrameIndex + 1) % Images.Count;
                }, DispatcherPriority.Render);
            } catch {
                // task was cancelled because the user closed the program
                // nothing to do here
            }
        }

        public void StartAnimation()
        {
            if (Images != null && Images.Count > 0)
            {
                currentFrameIndex = 0;
                Image = Images[currentFrameIndex];
                animationTimer.Start();
            }
        }
        public void StopAnimation()
        {
            animationTimer.Stop();
            currentFrameIndex = 0;
            if (Images != null && Images.Count > 0)
            {
                Image = Images[currentFrameIndex];
            }
        }

        private void ResetAnimation()
        {
            StopAnimation();
            StartAnimation();
        }

        protected void NotifyPropertyChanged(string propName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }
    }
}
