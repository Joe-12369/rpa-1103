using System;
using System.Windows.Input;

namespace II.RPA.Plugin.GUI.ProcessCreator.Mvvm
{
    /// <summary>
    /// ICommand实现
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class NMCommand<T> : ICommand
    {
        private readonly Func<T, bool> _canExecute;
        private readonly Action<T> _execute;
        public NMCommand(Action<T> execute)
            : this(execute, null)
        {
        }
        public NMCommand(Action<T> execute, Func<T, bool> canExecute)
        {
            if (execute == null)
            {
                throw new ArgumentNullException("execute");
            }
            this._execute = new Action<T>(execute);
            if (canExecute != null)
            {
                this._canExecute = new Func<T, bool>(canExecute);
            }
        }
        public bool CanExecute(object parameter)
        {
            if (this._canExecute == null)
            {
                return true;
            }
            if ((parameter == null) && typeof(T).IsValueType)
            {
                return this._canExecute(default(T));
            }
            if ((parameter == null) || (parameter is T))
            {
                return this._canExecute((T)parameter);
            }
            return false;
        }
        public event EventHandler CanExecuteChanged;
        public void Execute(object parameter)
        {
            object obj2 = parameter;
            if (this.CanExecute(obj2) && (this._execute != null))
            {
                if (obj2 == null)
                {
                    if (typeof(T).IsValueType)
                    {
                        this._execute(default(T));
                    }
                    else
                    {
                        this._execute((T)obj2);
                    }
                }
                else
                {
                    this._execute((T)obj2);
                }
            }
        }
        public void RaiseCanExecuteChanged()
        {
            this.CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
