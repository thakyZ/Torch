using System.Windows;
using System.Windows.Controls;

namespace Torch.Server.Views.Entities
{
    /// <summary>
    /// Interaction logic for GridView.xaml
    /// </summary>
    public partial class CharacterView : UserControl
    {
        public CharacterView()
        {
            InitializeComponent();

            ThemeControl.UpdateDynamicControls += UpdateResourceDict;
            UpdateResourceDict(ThemeControl.CurrentTheme);
        }

        public void UpdateResourceDict(ResourceDictionary dictionary)
        {
            this.Resources.MergedDictionaries.Clear();
            this.Resources.MergedDictionaries.Add(dictionary);
        }
    }
}
