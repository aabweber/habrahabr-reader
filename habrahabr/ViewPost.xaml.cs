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
using System.IO;
using System.Text;
using System.IO.IsolatedStorage;
using Microsoft.Phone.Shell;
using System.Text.RegularExpressions;
using Microsoft.Phone.Tasks;
using System.Windows.Controls.Primitives;
using System.Collections.ObjectModel;
using System.Windows.Threading;

namespace habrahabr
{
    public partial class ViewPost : PhoneApplicationPage
    {
        string prepared_html;


        private Post current_post;
        private QA current_qa;
        public ViewPost()
        {
            InitializeComponent();
        }

        public static string ConvertExtendedASCII(string HTML)
        {
            return new Regex("[^\\u0000-\\u007F]").Replace(HTML, (Match m) =>
            {
                return "&#" + Convert.ToInt16(m.Value[0]) + ";";
            });
        }

        private HttpWebRequest post_loader = null;
        private void LoadPost(Post post)
        {
            List<int> readen = null;
            try
            {
                AppSettings.TryGetSetting<List<int>>("readen", out readen);
                readen.Add(post.id);
                AppSettings.StoreSetting<List<int>>("readen", readen);
            }
            catch { }

            progBar.IsEnabled = true;
            if (post_loader != null)
            {
                post_loader.Abort();
            }
            post_loader = Request.New(post.url, (html, headers) =>
            {
                post_loader = null;
                System.Windows.Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    if (html != null)
                    {
                        error_displayed = false;
                        progBar.IsEnabled = false;
                        prepared_html = PrepareHabrahabrHtml(html);
                        prepared_html = ConvertExtendedASCII(prepared_html);
                        webBrowser.NavigateToString(prepared_html);
                    }
                    else
                    {
                        InternetConnectionError(() =>
                        {
                            LoadPost(post);
                        });
                    }
                });

            });
        }

        private string BuildCommentsHTML(List<CommentItem> comments, int level = 0)
        {
            if (comments == null) return "";
            string html = "";
            foreach (CommentItem comment in comments)
            {
                html += "<div style='padding:5px;margin:10px;" + (level != 0 ? (level >= 2 ? "margin:5px 2px 5px 2px; padding:2px;" : "margin-left:20px;") : "") + "border:1px solid gray;'>";
                html += "<div style='font-size:10px; color:gray;'>";
                html += "<b>Автор:</b> " + comment.author + " <b>Дата:</b> " + comment.date + "";
                html += "</div>";
                html += comment.content;
                html += BuildCommentsHTML(comment.childs, level + 1);
                html += "</div>";
            }
            return html;
        }

        private string PrepareHabrahabrHtml(string html)
        {
            Match m = new Regex("<div[^>]+class\\s*=\\s*['\"][^'\"]*\\bcontent\\b[^'\"]*['\"][^>]*>(.*?(<div.+?</div>)*.*?)</div>", RegexOptions.Singleline).Match(html);
            string result = "";
            result += "<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.0 Transitional//EN\" \"http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd\">\n";
            result += "<html xmlns=\"http://www.w3.org/1999/xhtml\" dir=\"ltr\" lang=\"ru-RU\">\n";
            result += " <head profile=\"http://gmpg.org/xfn/11\">\n";
	        result += "     <meta http-equiv=\"Content-Type\" content=\"text/html; charset=UTF-8\" />\n";
			result += "     <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0, maximum-scale=1.0, user-scalable=no\" />\n";
            result += "     <style>\n";
            result += "     *{max-width:480px;}\n";
            result += "     </style>\n";
            result += "     <script>\n";
            result += "	    function resizer(){\n";
            result += "		    var iRules = document.styleSheets[0].rules.length;\n";
            result += "		    for(i=0;i<iRules;i++){\n";
            result += "			    document.styleSheets[0].removeRule(i);\n";
            result += "		    }\n";
            //result += "		    document.styleSheets[0].addRule(\"*\", \"max-width: 100px;\");\n";
            result += "	        document.styleSheets[0].insertRule(\"*{max-width: \" + document.body.clientWidth + \"px;}\", document.styleSheets[0].cssRules.length);\n";
            result += "	    }\n";
            result += "	    </script>\n";
            result += "</head>\n";

            result += "<body onresize=\"javascript:resizer();\" onresize=\"javascript:resizer();\">\n";
            result += "<script>\n";
	        result += "resizer();\n";
            result += "</script>\n";
            //result += "<div id=\"ddd\"></div>";
            result += m.Groups[1].Value;

            List<CommentItem> comments = new List<CommentItem>();

            if ((Application.Current as App).current_qa != null)
            {
                MatchCollection ms = new Regex(
                    "<div[^>]+class\\s*=\\s*['\"][^'\"]*\\banswer\\b[^'\"]*['\"][^>]*>" +
                    "(" +
                    ".+?" +
                    ")" +
                    "<div[^>]+class\\s*=\\s*['\"][^'\"]*\\breply\\b[^'\"]*['\"][^>]*>", RegexOptions.IgnoreCase | RegexOptions.Singleline).Matches(html);
                foreach (Match match in ms)
                {
                    CommentItem comment = new CommentItem();
                    Match text_m = new Regex("class\\s*=\\s*\"[^\"]*\\bmessage\\b[^\"]*\">(.+?)</div>", RegexOptions.Singleline).Match(match.Groups[1].Value);
                    Match user_m = new Regex("class\\s*=\\s*\"[^\"]*\\busername\\b[^\"]*\">(.+?)</a>", RegexOptions.Singleline).Match(match.Groups[1].Value);
                    Match time_m = new Regex("datetime\\s*=\\s*\"(.+?)\"", RegexOptions.Singleline).Match(match.Groups[1].Value);
                    comment.author = user_m.Groups[1].Value;
                    comment.content = text_m.Groups[1].Value;
                    comment.date = time_m.Groups[1].Value;
                    comments.Add(comment);
                }
            }
            else
            {
                MatchCollection ms = new Regex(
                    "<div[^>]+class\\s*=\\s*['\"][^'\"]*\\bcomment_item\\b[^'\"]*['\"][^>]*>" +
                    "(" +
                    ".+?" +
                    ")" +
                    "<div[^>]+class\\s*=\\s*['\"][^'\"]*\\breply\\b[^'\"]*['\"][^>]*>", RegexOptions.IgnoreCase | RegexOptions.Singleline).Matches(html);
                foreach (Match match in ms)
                {
                    CommentItem comment = new CommentItem();
                    Match time_m = new Regex("datetime\\s*=\\s*\"(.+?)\"", RegexOptions.Singleline).Match(match.Groups[1].Value);
                    Match user_m = new Regex("class\\s*=\\s*\"[^\"]*\\busername\\b[^\"]*\">(.+?)</a>", RegexOptions.Singleline).Match(match.Groups[1].Value);
                    Match text_m = new Regex("class\\s*=\\s*\"[^\"]*\\bmessage\\b[^\"]*\">(.+?)</div>", RegexOptions.Singleline).Match(match.Groups[1].Value);
                    Match id_m = new Regex("rel\\s*=\\s*\"(\\d+)\"", RegexOptions.Singleline).Match(match.Groups[1].Value);
                    Match parent_m = new Regex("data-parent_id\\s*=\\s*\"(\\d+)\"", RegexOptions.Singleline).Match(match.Groups[1].Value);

                    comment.author = user_m.Groups[1].Value;
                    comment.content = text_m.Groups[1].Value;
                    comment.date = time_m.Groups[1].Value;
                    comment.id = Int32.Parse(id_m.Groups[1].Value);
                    if (parent_m.Success) comment.parent = Int32.Parse(parent_m.Groups[1].Value);
                    comments.Add(comment);
                }
                comments = PrepareComments(comments);
            }

            result += BuildCommentsHTML(comments);

            result += " </body>";
            result += "</html>";
            return result;
        }

        private static List<CommentItem> PrepareComments(List<CommentItem> list, int p = 0)
        {
            List<CommentItem> childs = new List<CommentItem>();
            foreach (CommentItem c in list)
            {
                if (c.parent == p)
                {
                    c.childs = PrepareComments(list, c.id);
                    childs.Add(c);
                }
            }
            return childs;
        }


        private void PhoneApplicationPage_Loaded(object sender, RoutedEventArgs e)
        {
            progBar.IsEnabled = true;
            var app = (Application.Current as App);
            current_post = app.current_post;
            if (current_post==null)
            {
                current_post = new Post(app.current_qa.id, "http://habrahabr.ru"+app.current_qa.url, app.current_qa.Name, "");
                current_qa = app.current_qa;
                //app.current_qa;
            }
            LoadPost(current_post);

            updateNavButtons();
            string tmp;
            AppSettings.TryGetSetting<string>("uname", out tmp); if (tmp != null) loginTxt.Text = tmp;
            AppSettings.TryGetSetting<string>("upwd", out tmp); if (tmp != null) passwordTxt.Password = tmp;

            ObservableCollection<Post> fav = null;
            AppSettings.TryGetSetting("favorites", out fav);
            fav_button = ((ApplicationBarIconButton)ApplicationBar.Buttons[3]);
            if (fav.Contains(current_post))
            {
                fav_button.Text = "Удалить";
                fav_button.IconUri = new Uri("/icons/appbar.favs.remove.rest.png", UriKind.Relative);
            }else{
                fav_button.Text = "Добавить";
                fav_button.IconUri = new Uri("/icons/appbar.favs.addto.rest.png", UriKind.Relative);
            }
                
        }

        /*
        private object errorOccured_lock = null;
        private static bool errorOccured;
        public delegate void InternetErrorDelegate();
        private void InternetConnectionError(InternetErrorDelegate a)
        {
            if (errorOccured)
            {
                lock (errorOccured_lock)
                {
                    a();
                }
            }
            else
            {
                errorOccured = true;
                lock (errorOccured_lock)
                {
                    MessageBoxResult result = MessageBox.Show("Ошибка соединения", "Повторите попытку", MessageBoxButton.OK);
                    a();
                }
                a();
            }
        }
        */
        private bool error_displayed_ = false;
        private bool error_displayed
        {
            get
            {
                return error_displayed_;
            }
            set
            {
                error_displayed_ = value;
                Dispatcher.BeginInvoke(() =>
                {
                    if (value)
                    {
                        errorLabel.Visibility = System.Windows.Visibility.Visible;
                    }
                    else
                    {
                        errorLabel.Visibility = System.Windows.Visibility.Collapsed;
                    }
                });

            }
        }
        public void InternetConnectionError(Action retry)
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
                timer.Tick += delegate(object s, EventArgs args)
                {
                    retry();
                    timer.Stop();
                };
                timer.Start();

            });
        }


        private void webBrowser_Navigated(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            progBar.IsEnabled = false;
        }

        private void webBrowser_Navigating(object sender, NavigatingEventArgs e)
        {
            progBar.IsEnabled = true;
        }

        
        private void ApplicationBarIcon_Click_Fav(object sender, EventArgs e)
        {
            fav_button = ((ApplicationBarIconButton)ApplicationBar.Buttons[3]);
            ObservableCollection<Post> fav;
            if (fav_button.Text == "Добавить")
            {
                fav_button.Text = "Удалить";
                fav_button.IconUri = new Uri("/icons/appbar.favs.remove.rest.png", UriKind.Relative);
                if (AppSettings.TryGetSetting("favorites", out fav))
                {
                    try
                    {
                        if (!fav.Contains(current_post))
                        {
                            //current_post.Equals
                            fav.Add(current_post);
                        }
                    }
                    catch { }
                }
            }
            else
            {
                fav_button.Text = "Добавить";
                fav_button.IconUri = new Uri("/icons/appbar.favs.addto.rest.png", UriKind.Relative);
                if (AppSettings.TryGetSetting("favorites", out fav))
                {
                    fav.Remove(current_post);
                }
            }
            AppSettings.StoreSetting("favorites", fav);
            
        }
        

        private void ApplicationBarIcon_Click_Reload(object sender, EventArgs e)
        {
            LoadPost(current_post);
        }

        private void ApplicationBarIcon_Click_Forward(object sender, EventArgs e)
        {
            if (((ApplicationBarIconButton)ApplicationBar.Buttons[1]).Text == "Вперед")
            {
                if ((Application.Current as App).current_qa!=null)
                {
                    int index = (Application.Current as App).current_qas.IndexOf(current_qa);
                    current_qa = (Application.Current as App).current_qas[index + 1];
                    current_post = new Post(current_qa.id, "http://habrahabr.ru"+current_qa.url, current_qa.Name, "");
                }else{
                    int index = (Application.Current as App).current_posts.IndexOf(current_post);
                    current_post = (Application.Current as App).current_posts[index + 1];
                }
                LoadPost(current_post);
                updateNavButtons();
            }
            else
            {
                userInfoPopup.IsOpen = false;
                RestoreAppBarButtons();
                ApplicationBar.IsVisible = true;
            }
        }

        private void ApplicationBarIcon_Click_Back(object sender, EventArgs e)
        {
            if (((ApplicationBarIconButton)ApplicationBar.Buttons[0]).Text == "Назад")
            {
                if ((Application.Current as App).current_qa != null)
                {
                    int index = (Application.Current as App).current_qas.IndexOf(current_qa);
                    current_qa = (Application.Current as App).current_qas[index - 1];
                    current_post = new Post(current_qa.id, "http://habrahabr.ru" + current_qa.url, current_qa.Name, "");
                }
                else
                {
                    current_post = (Application.Current as App).current_posts[(Application.Current as App).current_posts.IndexOf(current_post) - 1];
                }
                LoadPost(current_post);
                updateNavButtons();
            }
            else
            {
                // user info apply
                SaveUserInfo(loginTxt.Text, passwordTxt.Password);
                ApplicationBar.IsVisible = true;
            }

        }



        private void updateNavButtons()
        {
            bool back = (Application.Current as App).current_posts.Count > 0 && (Application.Current as App).current_posts[0].id != current_post.id;
            bool forw = (Application.Current as App).current_posts.Count > 0 && (Application.Current as App).current_posts[(Application.Current as App).current_posts.Count - 1].id != current_post.id;
            ((ApplicationBarIconButton)ApplicationBar.Buttons[0]).IsEnabled = back;
            ((ApplicationBarIconButton)ApplicationBar.Buttons[1]).IsEnabled = forw;
        }

        private void RestoreAppBarButtons()
        {
            ((ApplicationBarIconButton)ApplicationBar.Buttons[0]).Text = "Назад";
            ((ApplicationBarIconButton)ApplicationBar.Buttons[0]).IconUri = new Uri("icons/appbar.back.rest.png", UriKind.Relative);
            ((ApplicationBarIconButton)ApplicationBar.Buttons[1]).Text = "Вперед";
            ((ApplicationBarIconButton)ApplicationBar.Buttons[1]).IconUri = new Uri("icons/appbar.next.rest.png", UriKind.Relative);
            ApplicationBarIconButton b = new ApplicationBarIconButton(new Uri("icons/appbar.refresh.rest.png", UriKind.Relative));
            b.Text = "Обновить";
            b.Click += ApplicationBarIcon_Click_Reload;
            ApplicationBar.Buttons.Add(b);
            updateNavButtons();
        }



        private void email_Click(object sender, EventArgs e)
        {
            EmailComposeTask task = new EmailComposeTask();
            task.Subject = "habrahabr: " + HttpUtility.HtmlDecode(current_post.Name);
            task.Body = "Читать на хабре: " + current_post.url;
            task.Show();
        }

        private void fb_Click(object sender, EventArgs e)
        {
            ShareLinkTask shareLinkTask = new ShareLinkTask();
            shareLinkTask.LinkUri = new Uri(current_post.url, UriKind.Absolute);
            shareLinkTask.Message = HttpUtility.HtmlDecode(current_post.Name);
            shareLinkTask.Show();
        }

        private void twitter_Click(object sender, EventArgs e)
        {
            ShareLinkTask shareLinkTask = new ShareLinkTask();
            shareLinkTask.LinkUri = new Uri(current_post.url, UriKind.Absolute);
            shareLinkTask.Message = HttpUtility.HtmlDecode(current_post.Name);
            shareLinkTask.Show();
        }

        private void ShowUserInfoDialog()
        {
            //userInfoGrid.Width = userInfoPopup.Width = System.Windows.Application.Current.Host.Content.ActualWidth;
            //userInfoGrid.Height = userInfoPopup.Height = System.Windows.Application.Current.Host.Content.ActualHeight;
            userInfoPopup.IsOpen = true;
            ((ApplicationBarIconButton)ApplicationBar.Buttons[0]).IsEnabled = true;
            ((ApplicationBarIconButton)ApplicationBar.Buttons[0]).IconUri = new Uri("icons/appbar.check.rest.png", UriKind.Relative);
            ((ApplicationBarIconButton)ApplicationBar.Buttons[0]).Text = "Отправить";
            ((ApplicationBarIconButton)ApplicationBar.Buttons[1]).IsEnabled = true;
            ((ApplicationBarIconButton)ApplicationBar.Buttons[1]).IconUri = new Uri("icons/appbar.cancel.rest.png", UriKind.Relative);
            ((ApplicationBarIconButton)ApplicationBar.Buttons[1]).Text = "Отмена";
            while (ApplicationBar.Buttons.Count>2)
            {
                ApplicationBar.Buttons.RemoveAt(2);
            }
            loginTxt.Focus();
        }

        private void ApplicationBarMenuItem_Click(object sender, EventArgs e)
        {
            string login, password;
            AppSettings.TryGetSetting<string>("uname", out login); if (login == null) login = "";
            AppSettings.TryGetSetting<string>("upwd", out password); if (password == null) password = "";
            progBar.IsEnabled = true;
            if (login == "" || password == "")
            {
                ShowUserInfoDialog();
                progBar.IsEnabled = false;
            }
            else
            {
                Request.New("https://readitlaterlist.com/v2/add?username=" + HttpUtility.UrlEncode(login) + "&password=" + HttpUtility.UrlEncode(password) + "&apikey=67fg3Ld4pgY51A1a2fT2c4fTb1A1p785&url=" + HttpUtility.UrlEncode(current_post.url) + "&title=" + HttpUtility.UrlEncode(current_post.Name) + "", (c, h) =>
                {
                    System.Windows.Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        progBar.IsEnabled = false;
                        if (c != null)
                        {
                            if (new Regex("200").Match(c).Success)
                            {
                                userInfoPopup.IsOpen = false;
                                MessageBoxResult result = MessageBox.Show("сохранено в pocket", "", MessageBoxButton.OK);
                            }
                            else
                            {
                                ShowUserInfoDialog();
                            }
                        }
                        else
                        {
                            ShowUserInfoDialog();
                        }
                    });

                }, "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/535.19 (KHTML, like Gecko) Chrome/18.0.1025.142 Safari/535.19", true);
            }
        }

        private void SaveUserInfo(string login, string password)
        {
            Focus();
            if (login == "")
            {
                MessageBoxResult result = MessageBox.Show("Заполните данные", "Введите имя пользователя", MessageBoxButton.OK);
                loginTxt.Focus();
            }
            else if (password == "")
            {
                MessageBoxResult result = MessageBox.Show("Заполните данные", "Введите пароль", MessageBoxButton.OK);
                passwordTxt.Focus();
            }
            else
            {
                AppSettings.StoreSetting("uname", login);
                AppSettings.StoreSetting("upwd", password);
                userInfoPopup.IsOpen = true;
                ApplicationBarMenuItem_Click(null, null);
                userInfoPopup.IsOpen = false;
                RestoreAppBarButtons();
            }
        }

        private static bool focused = false;

        private void check_popup_focus()
        {
            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = new TimeSpan(0, 0, 0, 0, 100);
            timer.Tick +=
                delegate(object s, EventArgs args)
                {
                    System.Windows.Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        if (!focused)
                        {
                            userInfoPopup.IsOpen = false;
                            RestoreAppBarButtons();
                        }
                    });
                    timer.Stop();
                };
            timer.Start();
        }

        private void loginTxt_GotFocus(object sender, RoutedEventArgs e)
        {
            focused = true;
            if (loginTxt.Text == "Введите логин")
            {
                loginTxt.Text = "";
            }
        }

        private void passwordTxt_GotFocus(object sender, RoutedEventArgs e)
        {
            focused = true;
        }

        private void loginTxt_LostFocus(object sender, RoutedEventArgs e)
        {
            if (loginTxt.Text == "")
            {
                loginTxt.Text = "Введите логин";
            }
            focused = false;
            check_popup_focus();
        }

        private void passwordTxt_LostFocus(object sender, RoutedEventArgs e)
        {
            focused = false;
            check_popup_focus();
        }


        private void ApplicationBarIconFav_Click(object sender, EventArgs e)
        {
            SaveUserInfo(loginTxt.Text, passwordTxt.Password);
        }

        private void loginTxt_TextInput(object sender, TextCompositionEventArgs e)
        {
            if (loginTxt.Text == "")
            {
                MessageBoxResult result = MessageBox.Show("Заполните данные", "Введите имя пользователя", MessageBoxButton.OK);
                loginTxt.Focus();
            }
            else
            {
                passwordTxt.Focus();
            }
        }

        private void passwordTxt_TextInput(object sender, TextCompositionEventArgs e)
        {
            SaveUserInfo(loginTxt.Text, passwordTxt.Password);
        }

        private void PhoneApplicationPage_BackKeyPress(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (userInfoPopup.IsOpen)
            {
                userInfoPopup.IsOpen = false;
            }
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

        private void PhoneApplicationPage_BackKeyPress_1(object sender, System.ComponentModel.CancelEventArgs e)
        {
            userInfoPopup.IsOpen = false;
        }




    }

    public class CommentItem
    {
        public int id { get; set; }
        public string author { get; set; }
        public string date { get; set; }
        public string content { get; set; }
        public int parent { get; set; }

        public List<CommentItem> childs;
    }


}
