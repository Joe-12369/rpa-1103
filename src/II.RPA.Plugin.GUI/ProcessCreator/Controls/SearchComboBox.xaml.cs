using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace II.RPA.Plugin.GUI.ProcessCreator.Controls
{
    public partial class SearchComboBox : ComboBox
    {
        readonly SerialDisposable disposable = new SerialDisposable();

        TextBox editableTextBoxCache;

        Predicate<object> defaultItemsFilter;
        public SearchComboBox()
        {
            InitializeComponent();
            AddHandler(TextBoxBase.TextChangedEvent, new TextChangedEventHandler(OnTextChanged));
            AddHandler(LoadedEvent, new RoutedEventHandler(OnLoadedEvent));
            //AddHandler(EditableTextBox.GotFocusEvent, new RoutedEventHandler(OnTextGotFocus));

        }

        private void OnLoadedEvent(object sender, RoutedEventArgs e)
        {
            EditableTextBox.GotFocus += EditableTextBox_GotFocus;
        }

        private void EditableTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            ShowSearchSuggestion();
        }
        private void ShowSearchSuggestion()
        {
            if (string.IsNullOrEmpty(EditableTextBox.Text))
            {
                SuggestionTextVisibility = Visibility.Visible;
                Items.Filter = (item) => { return SuggestionItems.Contains(item); };
                IsDropDownOpen = true;
            }
        }


        public TextBox EditableTextBox
        {
            get
            {
                if (editableTextBoxCache == null)
                {
                    const string name = "PART_EditableTextBox";
                    editableTextBoxCache = (TextBox)VisualTreeModule.FindChild(this, name);
                }
                return editableTextBoxCache;
            }
        }

        /// <summary>
        /// Gets text to match with the query from an item.
        /// Never null.
        /// </summary>
        /// <param name="item"/>
        string TextFromItem(object item)
        {
            if (item == null) return string.Empty;

            var d = new DependencyVariable<string>();
            d.SetBinding(item, TextSearch.GetTextPath(this));
            return d.Value ?? string.Empty;
        }
        TagList<string> TextsFromItem(object item)
        {
            if (item == null) return null;

            var d = new DependencyVariable<TagList<string>>();
            d.SetBinding(item, TextSearch.GetTextPath(this));
            return d.Value ?? null;
        }
        protected override void OnItemsSourceChanged(IEnumerable oldValue, IEnumerable newValue)
        {
            base.OnItemsSourceChanged(oldValue, newValue);

            defaultItemsFilter = newValue is ICollectionView cv ? cv.Filter : null;
        }

        #region Setting
        static readonly DependencyProperty settingProperty =
            DependencyProperty.Register(
                "Setting",
                typeof(SearchComboBoxSetting),
                typeof(SearchComboBox)
            );

        public static DependencyProperty SettingProperty
        {
            get { return settingProperty; }
        }

        public SearchComboBoxSetting Setting
        {
            get { return (SearchComboBoxSetting)GetValue(SettingProperty); }
            set { SetValue(SettingProperty, value); }
        }

        SearchComboBoxSetting SettingOrDefault
        {
            get { return Setting ?? SearchComboBoxSetting.Default; }
        }
        #endregion
        #region SuggestionItems
        static readonly DependencyProperty suggestionItemsProperty =
            DependencyProperty.Register(
                "SuggestionItems",
                typeof(IEnumerable<ComponentModel>),
                typeof(SearchComboBox)
            );

        public static DependencyProperty SuggestionItemsProperty
        {
            get { return suggestionItemsProperty; }
        }

        public IEnumerable<ComponentModel> SuggestionItems
        {
            get { return (IEnumerable<ComponentModel>)GetValue(SuggestionItemsProperty); }
            set { SetValue(SuggestionItemsProperty, value); }
        }
        #endregion
        #region SuggestionItems
        static readonly DependencyProperty suggestionTextVisibilityProperty =
            DependencyProperty.Register(
                "SuggestionTextVisibility",
                typeof(Visibility),
                typeof(SearchComboBox)
            );

        public static DependencyProperty SuggestionTextVisibilityProperty
        {
            get { return suggestionTextVisibilityProperty; }
        }

        public Visibility SuggestionTextVisibility
        {
            get { return (Visibility)GetValue(SuggestionTextVisibilityProperty); }
            set { SetValue(SuggestionTextVisibilityProperty, value); }
        }
        #endregion

        #region OnTextChanged
        long revisionId;
        string previousText;

        struct TextBoxStatePreserver
            : IDisposable
        {
            readonly TextBox textBox;
            readonly int selectionStart;
            readonly int selectionLength;
            readonly string text;

            public void Dispose()
            {
                textBox.Text = text;
                textBox.Select(selectionStart, selectionLength);
            }

            public TextBoxStatePreserver(TextBox textBox)
            {
                this.textBox = textBox;
                selectionStart = textBox.SelectionStart;
                selectionLength = textBox.SelectionLength;
                text = textBox.Text;
            }
        }

        static int CountWithMax<T>(IEnumerable<T> xs, Predicate<T> predicate, int maxCount)
        {
            var count = 0;
            foreach (var x in xs)
            {
                if (predicate(x))
                {
                    count++;
                    if (count > maxCount) return count;
                }
            }
            return count;
        }

        void Unselect()
        {
            var textBox = EditableTextBox;
            textBox.Select(textBox.SelectionStart + textBox.SelectionLength, 0);
        }

        void UpdateFilter(Predicate<object> filter)
        {
            using (new TextBoxStatePreserver(EditableTextBox))
            using (Items.DeferRefresh())
            {
                // Can empty the text box. I don't why.
                Items.Filter = filter;
            }
        }

        void OpenDropDown(Predicate<object> filter)
        {
            SuggestionTextVisibility = Visibility.Collapsed;
            UpdateFilter(filter);
            IsDropDownOpen = true;
            Unselect();
        }

        void OpenDropDown()
        {
            var filter = GetColloctionFilter();
            OpenDropDown(filter);
        }

        void UpdateSuggestionList()
        {
            var text = Text;
            if (text == previousText) return;
            previousText = text;
            if (string.IsNullOrEmpty(text))
            {
                IsDropDownOpen = false;
                SelectedItem = null;

                using (Items.DeferRefresh())
                {
                    //Items.Filter = defaultItemsFilter;
                    ShowSearchSuggestion();
                }
            }
            else if (SelectedItem != null && TextsFromItem(SelectedItem).Contains(text))
            {
                // It seems the user selected an item.
                // Do nothing.
            }
            else
            {
                using (new TextBoxStatePreserver(EditableTextBox))
                {
                    SelectedItem = null;
                    IsDropDownOpen = false;
                }

                var filter = GetColloctionFilter();
                var maxCount = SettingOrDefault.MaxSuggestionCount;
                var count = CountWithMax(ItemsSource?.Cast<object>() ?? Enumerable.Empty<object>(), filter, maxCount);

                if (count > maxCount) return;
                if (count == 0) return;

                OpenDropDown(filter);
            }
        }

        void OnTextChanged(object sender, TextChangedEventArgs e)
        {
            var id = unchecked(++revisionId);
            var setting = SettingOrDefault;

            if (setting.Delay <= TimeSpan.Zero)
            {
                UpdateSuggestionList();
                return;
            }

            disposable.Content =
                new Timer(
                    state =>
                    {
                        Dispatcher.InvokeAsync(() =>
                        {
                            if (revisionId != id) return;
                            UpdateSuggestionList();
                        });
                    },
                    null,
                    setting.Delay,
                    Timeout.InfiniteTimeSpan
                );
        }
        #endregion

        void ComboBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control) && e.Key == Key.Space)
            {
                OpenDropDown();
                e.Handled = true;
            }
        }

        Predicate<object> GetFilter()
        {
            var filter = SettingOrDefault.GetFilter(Text, TextFromItem);

            return defaultItemsFilter != null
                ? i => defaultItemsFilter(i) && filter(i)
                : filter;
        }
        Predicate<object> GetColloctionFilter()
        {
            var filter = SettingOrDefault.GetFilterByCollection(Text, TextsFromItem);

            return defaultItemsFilter != null
                ? i => defaultItemsFilter(i) && filter(i)
                : filter;
        }
    }

    sealed class SerialDisposable
            : IDisposable
    {
        IDisposable content;

        public IDisposable Content
        {
            get { return content; }
            set
            {
                if (content != null)
                {
                    content.Dispose();
                }

                content = value;
            }
        }

        public void Dispose()
        {
            Content = null;
        }
    }
    static class VisualTreeModule
    {
        public static FrameworkElement FindChild(DependencyObject obj, string childName)
        {
            if (obj == null) return null;

            var queue = new Queue<DependencyObject>();
            queue.Enqueue(obj);

            while (queue.Count > 0)
            {
                obj = queue.Dequeue();

                var childCount = VisualTreeHelper.GetChildrenCount(obj);
                for (var i = 0; i < childCount; i++)
                {
                    var child = VisualTreeHelper.GetChild(obj, i);

                    var fe = child as FrameworkElement;
                    if (fe != null && fe.Name == childName)
                    {
                        return fe;
                    }

                    queue.Enqueue(child);
                }
            }

            return null;
        }
    }
    sealed class DependencyVariable<T>
            : DependencyObject
    {
        static readonly DependencyProperty valueProperty =
            DependencyProperty.Register(
                "Value",
                typeof(T),
                typeof(DependencyVariable<T>)
            );

        public static DependencyProperty ValueProperty
        {
            get { return valueProperty; }
        }

        public T Value
        {
            get { return (T)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        public void SetBinding(Binding binding)
        {
            BindingOperations.SetBinding(this, ValueProperty, binding);
        }

        public void SetBinding(object dataContext, string propertyPath)
        {
            SetBinding(new Binding(propertyPath) { Source = dataContext });
        }
    }
}
