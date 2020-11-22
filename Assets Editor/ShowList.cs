using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Windows.Media;

namespace Assets_Editor
{
    public class ShowList : INotifyPropertyChanged
    {
        private uint id;
        private ImageSource image;
        private int pos;
        private String name;
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
        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string propName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }
    }
}
