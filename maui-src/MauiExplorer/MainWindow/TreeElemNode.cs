using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace MauiTestTree
{
    public class TreeElemNode : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        //
        // 3 Properties for important strings
        //

        // 

        /// <summary>
        /// Tag is the "header" of the column 
        /// </summary>
        public string Tag
        {
            get => _tag;
            set { _tag = value; OnPropertyChanged(); }
        }
        string _tag = "";

        /// <summary>
        /// Caption is the bold part 
        /// </summary>
        public string Caption
        {
            get => _caption;
            set { _caption = value; OnPropertyChanged(); }
        }
        string _caption = "";

        /// <summary>
        /// Info is the (may be lengthy) rest of the story
        /// </summary>
        public string Info
        {
            get => _info;
            set { _info = value; OnPropertyChanged(); }
        }
        string _info = "";

        /// <summary>
        /// Expanded means, the childs are potentially visible
        /// </summary>
        public bool IsExpanded
        {
            get => _isExpanded;
            set { _isExpanded = value; OnPropertyChanged(); }
        }
        bool _isExpanded = true;

        /// <summary>
        /// Background color of the text part of the element line
        /// </summary>
        public Color ColorElemBg
        {
            get => _colorElemBg;
            set { _colorElemBg = value; OnPropertyChanged(); }
        }
        Color _colorElemBg = Color.FromUint(0xffdbe2ff);

        /// <summary>
        /// Border color of the text part of the element line
        /// </summary>
        public Color ColorElemBorder
        {
            get => _colorElemBorder;
            set { _colorElemBorder = value; OnPropertyChanged(); }
        }
        Color _colorElemBorder = Colors.Transparent;

        /// <summary>
        /// Foreground color of the text part of the element line
        /// </summary>
        public Color ColorElemFg
        {
            get => _colorElemFg;
            set { _colorElemFg = value; OnPropertyChanged(); }
        }
        Color _colorElemFg = Colors.Black;

        /// <summary>
        /// Background color of the tag part of the element line
        /// </summary>
        public Color ColorTagBg
        {
            get => _colorTagBg;
            set { _colorTagBg = value; OnPropertyChanged(); }
        }
        Color _colorTagBg = Color.FromUint(0xff0128cb);

        /// <summary>
        /// Foreground color of the tag part of the element line
        /// </summary>
        public Color ColorTagFg
        {
            get => _colorTagFg;
            set { _colorTagFg = value; OnPropertyChanged(); }
        }
        Color _colorTagFg = Colors.White;

        public ObservableCollection<TreeElemNode> Children { get; set; }
            = new ObservableCollection<TreeElemNode>();
    }

}
