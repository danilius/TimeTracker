using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace TimeTracker.Utils
{

    public class TrulyObservableCollection<T> : ObservableCollection<T> where T : INotifyPropertyChanged
    {
        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (INotifyPropertyChanged item in e.NewItems)
                {
                    item.PropertyChanged += ItemPropertyChanged;
                }
            }

            if (e.OldItems != null)
            {
                foreach (INotifyPropertyChanged item in e.OldItems)
                {
                    item.PropertyChanged -= ItemPropertyChanged;
                }
            }

            base.OnCollectionChanged(e);
        }


        private void ItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

    }
}