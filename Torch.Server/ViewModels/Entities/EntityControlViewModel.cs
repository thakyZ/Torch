using System.Windows;

namespace Torch.Server.ViewModels.Entities
{
    public class EntityControlViewModel : ViewModel
    {
        internal const string SIGNAL_PROPERTY_INVALIDATE_CONTROL =
            "InvalidateControl-4124a476-704f-4762-8b5e-336a18e2f7e5";

        internal void InvalidateControl()
        {
            // ReSharper disable once ExplicitCallerInfoArgument
            OnPropertyChanged(SIGNAL_PROPERTY_INVALIDATE_CONTROL);
        }

        private bool _hide;

        /// <summary>
        /// Should this element be forced into the <see cref="Visibility.Collapsed"/>
        /// </summary>
        public bool Hide
        {
            get => _hide;
            protected set
            {
                if (_hide == value)
                    return;
                _hide = value;
                OnPropertyChanged();
            }
        }
    }
}
