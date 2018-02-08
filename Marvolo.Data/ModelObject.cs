using System;
using System.Collections;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Marvolo.Data
{
    public abstract class ModelObject : INotifyPropertyChanged, INotifyPropertyChanging, INotifyDataErrorInfo
    {
        protected ModelObject()
        {
            ErrorInfo.Changed += (sender, e) => ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(e.PropertyName));
        }

        public ModelObjectErrorInfo ErrorInfo { get; } = new ModelObjectErrorInfo();

        event EventHandler<DataErrorsChangedEventArgs> INotifyDataErrorInfo.ErrorsChanged
        {
            add => ErrorsChanged += value;
            remove => ErrorsChanged -= value;
        }

        public void Invalidate(string propertyName)
        {
            OnPropertyChanged(propertyName);
        }

        IEnumerable INotifyDataErrorInfo.GetErrors(string propertyName)
        {
            return ErrorInfo.GetErrors(propertyName);
        }

        bool INotifyDataErrorInfo.HasErrors => ErrorInfo.HasErrors;

        event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
        {
            add => PropertyChanged += value;
            remove => PropertyChanged -= value;
        }

        event PropertyChangingEventHandler INotifyPropertyChanging.PropertyChanging
        {
            add => PropertyChanging += value;
            remove => PropertyChanging -= value;
        }

        protected void SetProperty<T>(out T backingField, T value, [CallerMemberName] string propertyName = null)
        {
            OnPropertyChanging(propertyName);
            backingField = value;
            OnPropertyChanged(propertyName);
        }

        protected void OnPropertyChanging([CallerMemberName] string propertyName = null)
        {
            PropertyChanging?.Invoke(this, new PropertyChangingEventArgs(propertyName));
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private event PropertyChangedEventHandler PropertyChanged;

        private event PropertyChangingEventHandler PropertyChanging;

        private event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;
    }
}