using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Assets_Editor
{
    public class ShowList : INotifyPropertyChanged
    {
        private uint id;
        private uint cid;
        private ImageSource image;
        private int pos;
        private String name;
        private Storyboard storyboard = null;
        public uint Id
        {
            get { return id; }
            set
            {
                if (id != value)
                {
                    id = value;
                    NotifyPropertyChanged("Id");
                }
            }
        }
        public uint Cid
        {
            get { return cid; }
            set
            {
                if (cid != value)
                {
                    cid = value;
                    NotifyPropertyChanged("Cid");
                }
            }
        }
        public ImageSource Image
        {
            get { return image; }
            set
            {
                if (image != value)
                {
                    image = value;
                    NotifyPropertyChanged("Image");
                }
            }
        }
        public int Pos
        {
            get { return pos; }
            set
            {
                if (pos != value)
                {
                    pos = value;
                    NotifyPropertyChanged("Pos");
                }
            }
        }
        public String Name
        {
            get { return name; }
            set
            {
                if (name != value)
                {
                    name = value;
                    NotifyPropertyChanged("Name");
                }
            }
        }
        public Storyboard Storyboard
        {
            get { return storyboard; }
            set
            {
                if (storyboard != value)
                {
                    storyboard = value;
                    NotifyPropertyChanged("Storyboard");
                }
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string propName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }
    }
}
