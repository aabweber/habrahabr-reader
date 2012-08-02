using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using System.Windows.Media.Imaging;
using System.IO;
using System.Windows.Resources;
using Microsoft.Phone;
using System.Windows.Threading;
using MSPToolkit.Controls;
using System.Windows.Data;
using System.Globalization;

namespace habrahabr
{
    public partial class MainPage : PhoneApplicationPage
    {

        // Конструктор
        public MainPage()
        {
            InitializeComponent();
        }

        private int loaded_pages = 0;
        private int loaded_qa_pages = 0;
        private void PhoneApplicationPage_Loaded(object sender, RoutedEventArgs e)
        {
            LoadNextPage();
            LoadNextQAPage();
            LoadHubs();
            //LoadFavorites();
        }

        private void LoadFavorites()
        {
            ObservableCollection<Post> fav = null;
            //favoritesList.Items.Clear();
            if (AppSettings.TryGetSetting("favorites", out fav) && fav != null && fav.Count > 0)
            {
                noFavorites.Visibility = System.Windows.Visibility.Collapsed;
                favoritesList.ItemsSource = fav;
                
            }
            else
            {
                fav = new ObservableCollection<Post>();
                noFavorites.Visibility = System.Windows.Visibility.Visible;
                AppSettings.StoreSetting("favorites", fav);
            }
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            (Application.Current as App).current_qa = null;
            (Application.Current as App).current_post = null;
            LoadFavorites();
            ClearSelectionInList(postsList);
            ClearSelectionInList(qasList);
            base.OnNavigatedTo(e);
        }

        private void LoadHubs()
        {
            hubsList.ItemsSource = Hubs.hubs;
            /*
            foreach (Hub hub in Hubs.hubs){
                hubsList.Items.Add(hub);
            }
             * */
        }

        private HttpWebRequest qas_page_loader = null;
        private void LoadQAsPage(int page)
        {
            progBar.IsEnabled = true;
            if (page == 1)
            {
                qasList.Items.Clear();
                (Application.Current as App).current_qas.Clear();
            }
            string url = "http://habrahabr.ru/qa/page" + page + "/";
            if ((Application.Current as App).current_hub != null)
            {
                url = "http://habrahabr.ru/hub/" + (Application.Current as App).current_hub.EnName + "/qa/page" + page + "/";
            }
            if (qas_page_loader != null)
            {
                qas_page_loader.Abort();
            }

            qas_page_loader = Request.New(url, (c, h) =>
            {
                Dispatcher.BeginInvoke(() =>
                {

                    qas_page_loader = null;
                    if (c == null)
                    {
                        InternetError(() =>
                        {
                            LoadQAsPage(page);
                        });
                    }
                    else
                    {
                        progBar.IsEnabled = false;
                        if (page == 1)
                        {
                            qasList.Items.Clear();
                            (Application.Current as App).current_qas.Clear();
                        }
                        else
                        {
                            qasList.Items.RemoveAt(qasList.Items.Count - 1);
                        }
                        ParseQAs(c);
                    }
                });
            });

        }

        private void ParseQAs(string c)
        {
            List<int> readen;
            if (!AppSettings.TryGetSetting<List<int>>("readen_qas", out readen))
            {
                readen = new List<int>();
                AppSettings.StoreSetting<List<int>>("readen_qas", readen);
            }

            MatchCollection mc = new Regex("<div[^>]+class\\s*=\\s*['\"][^'\"]*\\bpost\\b[^'\"]*['\"][^>]*>.+?<a[^>]+href\\s*=\\s*['\"]([^'\"]+)['\"][^>]*>(.+?)</a>.+?<div[^>]+class\\s*=\\s*['\"][^'\"]*\\bcontent\\b[^'\"]*['\"][^>]*>(.*?(<div.+?</div>)*.*?)</div>", RegexOptions.Singleline).Matches(c);
            for (int i = 0; i < mc.Count; i++)
            {
                int id = Int32.Parse(new Regex("<div[^>]+id=\"post_(\\d+)\"").Match(mc[i].Groups[0].Value).Groups[1].Value);
                string url = mc[i].Groups[1].Value;
                string name = mc[i].Groups[2].Value;
                string content = mc[i].Groups[3].Value;
                QA qa = new QA(id, url, name, content);
                qa.readen = readen.Contains(id);
                qasList.Items.Add(qa);
                (Application.Current as App).current_qas.Add(qa);
            }
            if (mc.Count == 20)
            {
                var t = new HTMLTextBox();
                t.Html = "<b>Ещё</b>";
                t.Height = 90;
                t.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
                t.HorizontalContentAlignment = System.Windows.HorizontalAlignment.Center;
                t.Width = qasList.ActualWidth;
                t.Tag = "more";
                qasList.Items.Add(t);
            }

        }


        private void LoadNextQAPage()
        {
            LoadQAsPage(++loaded_qa_pages);
        }

        private void LoadNextPage()
        {
            LoadPage(++loaded_pages);
        }

        private HttpWebRequest page_loader = null;
        private void LoadPage(int page){
            progBar.IsEnabled = true;
            if (page == 1)
            {
                postsList.Items.Clear();
                (Application.Current as App).current_posts.Clear();
            }
            string url = "http://habrahabr.ru/posts/top/page" + page + "/";
            if ((Application.Current as App).current_hub != null)
            {
                url = "http://habrahabr.ru/hub/" + (Application.Current as App).current_hub.EnName + "/posts/page" + page + "/";
            }
            if (page_loader != null)
            {
                page_loader.Abort();
            }

            page_loader = Request.New(url, (c, h) =>
            {
                Dispatcher.BeginInvoke(() =>
                {

                    page_loader = null;
                    if (c == null)
                    {
                        InternetError(() =>
                        {
                            LoadPage(page);
                        });
                    }
                    else
                    {
                        progBar.IsEnabled = false;
                        if (page == 1)
                        {
                            postsList.Items.Clear();
                            (Application.Current as App).current_posts.Clear();
                        }
                        else
                        {
                            postsList.Items.RemoveAt(postsList.Items.Count - 1);
                        }
                        ParsePosts(c);
                    }
                });
            });
        }

        private void ParsePosts(string c)
        {
            List<int> readen;
            if (!AppSettings.TryGetSetting<List<int>>("readen", out readen))
            {
                readen = new List<int>();
                AppSettings.StoreSetting<List<int>>("readen", readen);
            }

            MatchCollection mc = new Regex("<div[^>]+class\\s*=\\s*['\"][^'\"]*\\bpost\\b[^'\"]*['\"][^>]*>.+?<a[^>]+href\\s*=\\s*['\"]([^'\"]+)['\"][^>]*>(.+?)</a>.+?<div[^>]+class\\s*=\\s*['\"][^'\"]*\\bcontent\\b[^'\"]*['\"][^>]*>(.*?(<div.+?</div>)*.*?)</div>", RegexOptions.Singleline).Matches(c);
            for (int i = 0; i < mc.Count; i++)
            {
                int id = Int32.Parse(new Regex("<div[^>]+id=\"post_(\\d+)\"").Match(mc[i].Groups[0].Value).Groups[1].Value);
                string url = mc[i].Groups[1].Value;
                string name = mc[i].Groups[2].Value;
                string content = mc[i].Groups[3].Value;
                Post post = new Post(id, url, name, content);
                post.readen = readen.Contains(id);
                postsList.Items.Add(post);
                (Application.Current as App).current_posts.Add(post);
            }
            if (mc.Count == 10)
            {
                var t = new HTMLTextBox();
                t.Html = "<b>Ещё</b>";
                t.Height = 90;
                t.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
                t.HorizontalContentAlignment = System.Windows.HorizontalAlignment.Center;
                t.Width = postsList.ActualWidth;
                t.Tag = "more";
                postsList.Items.Add(t);
            }
        }

        private bool error_displayed_ = false;
        private bool error_displayed { 
            get {
                return error_displayed_;
            }
            set{
                error_displayed_ = value;
                Dispatcher.BeginInvoke(() =>{
                    if (value)
                    {
                        errorLabel.Visibility = System.Windows.Visibility.Visible;
                    }
                    else
                    {
                        errorLabel.Visibility = System.Windows.Visibility.Collapsed;
                    }
                });

            }}

        public void InternetError(Action retry)
        {
            Dispatcher.BeginInvoke(() =>
            {
                if (!error_displayed)
                {
                    error_displayed = true;
                    MessageBoxResult result = MessageBox.Show("Ошибка интернет-соединения");
                }
                DispatcherTimer timer = new DispatcherTimer();
                timer.Interval = new TimeSpan(0, 0, 0, 2, 0);
                timer.Tick += delegate(object s, EventArgs args){
                    retry(); 
                    timer.Stop();
                };
                timer.Start();
                
            });
        }

        private void ClearSelectionInList(ListBox list)
        {
            for (int i = 0; i < list.Items.Count; i++)
            {
                try
                {
                    var lbi = (ListBoxItem)(list.ItemContainerGenerator.ContainerFromIndex(i));
                    lbi.Background = null;
                }
                catch { }
            }

        }

        private void qasList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (qasList.SelectedIndex == -1)
                return;
            int SelectedIndex = qasList.SelectedIndex;
            var lbi = (ListBoxItem)(qasList.ItemContainerGenerator.ContainerFromIndex(SelectedIndex));
            ClearSelectionInList(qasList);
            lbi.Background = new SolidColorBrush(Color.FromArgb(128, 0x80, 0x80, 0x80));
            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = new TimeSpan(0, 0, 0, 0, 100);
            timer.Tick +=
                delegate(object s, EventArgs args)
                {
                    Dispatcher.BeginInvoke(() =>
                    {
                        error_displayed = false;
                        if (SelectedIndex == qasList.Items.Count - 1)
                        {//more
                            lbi.IsEnabled = false;
                            LoadNextQAPage();
                        }
                        else
                        {
                            QA qa = qasList.Items[SelectedIndex] as QA;
                            (Application.Current as App).current_qa = qa;
                            NavigationService.Navigate(new Uri("/ViewPost.xaml", UriKind.Relative));
                        }
                        ClearSelectionInList(qasList);
                        timer.Stop();
                        qasList.SelectedIndex = -1;
                    });
                };
            timer.Start();
        }


        private void postsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (postsList.SelectedIndex == -1)
                return;
            int SelectedIndex = postsList.SelectedIndex;
            var lbi = (ListBoxItem)(postsList.ItemContainerGenerator.ContainerFromIndex(SelectedIndex));
            ClearSelectionInList(postsList);
            lbi.Background = new SolidColorBrush(Color.FromArgb(128, 0x80, 0x80, 0x80));
            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = new TimeSpan(0, 0, 0, 0, 100);
            timer.Tick +=
                delegate(object s, EventArgs args)
                {
                    Dispatcher.BeginInvoke(() =>
                    {
                        error_displayed = false;
                        if (SelectedIndex == postsList.Items.Count - 1)
                        {//more
                            lbi.IsEnabled = false;
                            LoadNextPage();
                        }
                        else
                        {
                            Post post = postsList.Items[SelectedIndex] as Post;
                            (Application.Current as App).current_post = post;
                            post.readen = true;
                            /*
                            ListBoxItem item = postsList.ItemContainerGenerator.ContainerFromIndex(SelectedIndex) as ListBoxItem;
                            HTMLTextBox txt;
                            if (item != null)
                            {
                                txt = FindFirstElementInVisualTree<HTMLTextBox>(item);
                                if (txt != null)
                                {
                                    txt.FontWeight = FontWeights.Normal;
                                }
                            }
                             * */
                            postsList.Items.RemoveAt(SelectedIndex);
                            postsList.Items.Insert(SelectedIndex, post);
                            NavigationService.Navigate(new Uri("/ViewPost.xaml", UriKind.Relative));
                        }
                        ClearSelectionInList(postsList);
                        timer.Stop();
                        postsList.SelectedIndex = -1;
                    });
                };
            timer.Start();
        }



        private void Image_Loaded(object sender, RoutedEventArgs args)
        {
            Image img = (Image)sender;
            (img.Source as BitmapImage).ImageOpened += new EventHandler<RoutedEventArgs>((a1,a2) =>
            {
                WriteableBitmap wBitmap = new WriteableBitmap((BitmapSource)a1);
                if (wBitmap.PixelWidth != 90 || wBitmap.PixelHeight != 90)
                {
                    MemoryStream ms = new MemoryStream();
                    int w, h;
                    if (wBitmap.PixelHeight > wBitmap.PixelWidth)
                    {
                        w = 90 * wBitmap.PixelWidth / wBitmap.PixelHeight;
                        h = 90;
                    }
                    else
                    {
                        h = 90 * wBitmap.PixelHeight / wBitmap.PixelWidth;
                        w = 90;
                    }
                    wBitmap.SaveJpeg(ms, w, h, 0, 75);
                    (img.Source as BitmapImage).SetSource(ms);
                }
            });
        }


        private void ApplicationBarIconButton_Click(object sender, EventArgs e)
        {
            switch ((pivot.SelectedItem as PivotItem).Tag as string)
            {
                case "posts":
                    postsList.Items.Clear();
                    loaded_pages = 0;
                    LoadNextPage();
                    break;
                case "qas":
                    qasList.Items.Clear();
                    loaded_qa_pages = 0;
                    LoadNextQAPage();
                    break;
            }
        }

        private void pivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch ((pivot.SelectedItem as PivotItem).Tag as string)
            {
                case "posts":
                    if (page_loader != null)
                    {
                        progBar.IsEnabled = true;
                    }
                    ApplicationBar.IsVisible = true;
                    break;
                case "hubs":
                    progBar.IsEnabled = false;
                    ApplicationBar.IsVisible = false;
                    break;
                case "qas":
                    if (qas_page_loader != null)
                    {
                        progBar.IsEnabled = true;
                    }
                    ApplicationBar.IsVisible = true;
                    break;
                case "fav":
                    progBar.IsEnabled = false;
                    ApplicationBar.IsVisible = false;
                    break;
            }
        }

        private T FindFirstElementInVisualTree<T>(DependencyObject parentElement) where T : DependencyObject
        {
            var count = VisualTreeHelper.GetChildrenCount(parentElement);
            if (count == 0)
                return null;

            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parentElement, i);

                if (child != null && child is T)
                {
                    return (T)child;
                }
                else
                {
                    var result = FindFirstElementInVisualTree<T>(child);
                    if (result != null)
                        return result;

                }
            }
            return null;
        }

        private void ExpanderView_Expanded(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < hubsList.Items.Count; i++ )
            {
                ListBoxItem item = hubsList.ItemContainerGenerator.ContainerFromIndex(i) as ListBoxItem;
                ExpanderView ew;
                if (item != null)
                {
                    ew = FindFirstElementInVisualTree<ExpanderView>(item);
                    if (ew != null && ew != sender)
                    {
                        ew.IsExpanded = false;
                    }
                }
            }
        }

        private void Button_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if ((sender as TextBlock).Tag as string == "")
            {
                (Application.Current as App).current_hub = null;
            }
            else
            {
                (Application.Current as App).current_hub = Hubs.GetByTag((sender as TextBlock).Tag as string);
            }
            loaded_pages = 0;
            LoadNextPage();
            
            loaded_qa_pages = 0;
            LoadNextQAPage();
            
            pivot.SelectedItem = postsPivot;
        }

        private void ExpanderView_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
        }

        private void info_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/About.xaml", UriKind.Relative));
        }

        private void ProgressBar_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (progBar.IsEnabled)
            {
                loadingLabel.Visibility = System.Windows.Visibility.Visible;
                progBar.Visibility = System.Windows.Visibility.Visible;
            }
            else
            {
                loadingLabel.Visibility = System.Windows.Visibility.Collapsed;
                progBar.Visibility = System.Windows.Visibility.Collapsed;
            }
        }

        private void favList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (favoritesList.SelectedIndex == -1)
                return;
            int SelectedIndex = favoritesList.SelectedIndex;
            var lbi = (ListBoxItem)(favoritesList.ItemContainerGenerator.ContainerFromIndex(SelectedIndex));
            ClearSelectionInList(favoritesList);
            lbi.Background = new SolidColorBrush(Color.FromArgb(128, 0x80, 0x80, 0x80));
            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = new TimeSpan(0, 0, 0, 0, 100);
            timer.Tick +=
                delegate(object s, EventArgs args)
                {
                    Dispatcher.BeginInvoke(() =>
                    {
                        string type = qasList.Items[SelectedIndex].GetType().ToString();
                        if (type == "habrahabr.QA")
                        {
                            QA qa = qasList.Items[SelectedIndex] as QA;
                            (Application.Current as App).current_qa = qa;
                        }
                        else
                        {
                            Post post = postsList.Items[SelectedIndex] as Post;
                            (Application.Current as App).current_post = post;
                        }
                        NavigationService.Navigate(new Uri("/ViewPost.xaml", UriKind.Relative));
                        ClearSelectionInList(favoritesList);
                        timer.Stop();
                        favoritesList.SelectedIndex = -1;
                    });
                };
            timer.Start();

        }

    }

    public class PostConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo language)
        {
            return (bool)value ? FontWeights.Normal : FontWeights.Bold;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo language)
        {
            return value;
        }
    }

    [DataContract]
    public class QA
    {
        public override bool Equals(object other)
        {
            if (other == null) return false;
            return this.url == (other as QA).url || this.id == (other as QA).id;
        }


        [DataMember]
        public int id { get; set; }
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public string url { get; set; }
        [DataMember]
        public bool readen { get; set; }

        public QA(int id_, string url_, string name_, string content_)
        {
            id = id_;
            Name = name_;
            url = url_;
        }
    }
    
    [DataContract]
    public class Post
    {
        public override bool Equals(object other)
        {
            if (other == null) return false;
            if (other.GetType().ToString() != "habrahabr.Post") return false;
            return this.url == (other as Post).url || this.id == (other as Post).id;
        }


        [DataMember]
        public int id { get; set; }
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public string Image { get; set; }
        [DataMember]
        public string url { get; set; }
        [DataMember]
        public bool readen { get; set; }

        public Post(int id_, string url_, string name_, string content_)
        {
            id = id_;
            Image = "";
            Name = name_;
            url = url_;
            Match m = new Regex("<img[^>]+src\\s*=\\s*['\"]([^'\"]+(jpg|jpeg|png))['\"][^>]*>").Match(content_);
            if(m.Success){
                Image = m.Groups[1].Value;
            }else{
                var app = (Application.Current as App);
                Image = "/icons/default_icon" + (app.theme == Themes.Dark ? "_w" : "") + ".png";
            }
        }
    }
}