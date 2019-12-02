using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Net;
using System.Net.Http;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.ObjectModel;
using System.Threading;
using System.Windows.Threading;
using System.Collections.Specialized;
using Microsoft.Win32;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;


namespace HttpScraper
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        string site_url;//holds site_url as mentioned
        ObservableCollection<String> bidnos = new ObservableCollection<String>();//bid number codes sent to the server for email addr
        ObservableCollection<String> company_names = new ObservableCollection<string>();//company names
        ObservableCollection<String> myEmails = new ObservableCollection<String>();//email addr
        OpenFileDialog dlg;//browse file to append
        string filename;//hold file name

        int to_val = 10;
        int from_val = 2;
        bool first = true;
        bool isUrlChanged = false;
        string changedUrl = "";
        bool chkbx = false;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            
        }

        private void searchbtn_Click(object sender, RoutedEventArgs e)
        {
            site_url = searchtxt.Text;//retrieve url link
            if(site_url=="" || site_url==null){
                MessageBox.Show("Something is wrong!!!\n Check the Url box");
            }
            else
            {
                chkbx = auto_append_chkbx.IsChecked.Value;
                myEmails.Clear();//clear the emails for a fresh start
                bidnos.Clear();//same here
                company_names.Clear();//same here
                from_value_txtbx.IsEnabled = false;
                to_value_txtbx.IsEnabled = false;
                auto_append_chkbx.IsEnabled = false;
                progressbar.IsEnabled = true;//progress starts simulating work done
                progressbar.Visibility = Visibility.Visible;//progressbar visible
                
                //Start the thread
                Thread th = new Thread(searchEngine);
                th.Start();
                searchbtn.IsEnabled = false;//disable the search button until the work is done
            }
        }

        private void searchEngine()
        {
            //Update the progress bar
            this.Dispatcher.BeginInvoke(DispatcherPriority.Normal,(ThreadStart)delegate(){progressbar.Value = 10; });
            String responseString;//The string variable holds the html page returned
            bool isappending = true;//for stopping a loop when done appending

            if (chkbx)
            {
                this.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (ThreadStart)delegate() { 
                    total_progressbar.Visibility = Visibility.Visible;
                    total_progressbar.Value = 5;
                });
                try
                {
                    this.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (ThreadStart)delegate()
                    {
                        to_val = Int32.Parse(to_value_txtbx.Text);
                        from_val = Int32.Parse(from_value_txtbx.Text);
                    });
                    
                }
                catch (Exception ex) {
                    this.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (ThreadStart)delegate()
                    {
                        MessageBox.Show(ex.Message.ToString());
                    });
                     }
                do
                {
                    if (first == true)
                    {
                        this.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (ThreadStart)delegate() { total_progressbar.Value = 15; });
                        generateEmail(site_url);
                        first = false;
                        this.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (ThreadStart)delegate() { total_progressbar.Value = 35; });
                    }
                    else
                    {
                        for (int i = from_val; i <= to_val; i++)
                        {
                            this.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (ThreadStart)delegate() { progressbar.Value = 15; });
                            string output = Regex.Replace(changedUrl, "p=.", "p="+i);
                            generateEmail(output);
                            this.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (ThreadStart)delegate() { total_progressbar.Value+=5; });
                        }
                        isappending = false;
                        this.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (ThreadStart)delegate() { 
                            total_progressbar.Value = 100;
                            total_progressbar.Visibility = Visibility.Hidden;
                            progressbar.Visibility = Visibility.Hidden;
                            searchbtn.IsEnabled = true;
                            auto_append_chkbx.IsEnabled = true;
                            to_value_txtbx.IsEnabled = true;
                            from_value_txtbx.IsEnabled = true;
                            MessageBox.Show("Finished downloading emails \n Save now", "Search Completed");
                        });
                    }
                } while (isappending == true);
                
            }
            else if (chkbx == false)
            {
                try
                {
                    using (var client = new WebClient())
                    {
                        responseString = client.DownloadString(@site_url);//fetch html page
                        this.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (ThreadStart)delegate() { progressbar.Value = 25; });//update progress bar
                        //match and get the bid codes for the request of their respective email
                        MatchCollection m1 = Regex.Matches(responseString, "onclick=\\\"lemail\\((.*?)\\)\\\"", RegexOptions.Singleline);
                        MatchCollection m4 = Regex.Matches(responseString, "<a href=\\\"(.*?)\\\" class=\\\"astyle13_2\\\">", RegexOptions.Singleline);
                        
                        this.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (ThreadStart)delegate() { progressbar.Value = 40; });

                        //MatchCollection m2 = Regex.Matches(responseString, "<div class=\\\".*?\\\" style=\\\"position:relative;float:left;clear:both;width:auto;max-width:288px;height:20px;margin-top:10px;margin-left:10px;text-align:left;overflow:hidden;display:block;\\\">(.*?)</div>", RegexOptions.Singleline);
                        foreach (Match m in m1)//loop through to process and get the email addresses
                        {
                            string bdcode = m.Groups[1].Value;//get each bid code

                            Random rd = new Random();//needed as a prerequisite
                            DateTime dt = new DateTime();//same here
                            //same as this
                            double rndKey = rd.Next() * dt.ToUniversalTime().Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
                            using (var client2 = new WebClient())//send request for the email address
                            {
                                var values = new NameValueCollection();
                                values["bid"] = bdcode;//given the bid code
                                values["ps"] = "/directory/search-results.asp";
                                values["st"] = "1";
                                values["RandomKey"] = rndKey.ToString();//and random key generated
                                //save response which is a html page
                                var response = client2.UploadValues("http://www.listedin.biz/directory/ajax/process/other/eaddress.asp", values);
                                var responseString2 = Encoding.Default.GetString(response);
                                //match to get the email address from the html page
                                string bidCode;
                                MatchCollection m3 = Regex.Matches(responseString2, "value=\\\"(.*?)\\\"", RegexOptions.Singleline);
                                foreach (Match mt in m3)
                                {
                                    bidCode = mt.Groups[1].Value;
                                    bidnos.Add(bidCode);
                                }
                                this.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (ThreadStart)delegate() { progressbar.Value += 2.5; });
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message.ToString());
                }

                this.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (ThreadStart)delegate() { progressbar.Value = 90; });
                this.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (ThreadStart)delegate()
                {
                    progressbar.Value = 100;
                    for (int i = 0; i < bidnos.Count; i++)
                    {
                        myEmails.Add(bidnos[i]);//save to the observable collection
                    }
                    celistview.ItemsSource = myEmails;//bind to the listview
                    searchbtn.IsEnabled = true;//enable the search button
                    progressbar.IsEnabled = false;//disable the progress bar
                    progressbar.Visibility = Visibility.Hidden;//hide as well
                });
            }
        }

        private void generateEmail(String url)
        {
            string responseString;
            try
            {
                using (var client = new WebClient())
                {
                    
                    responseString = client.DownloadString(@url);//fetch html page
                    this.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (ThreadStart)delegate() { progressbar.Value = 25; });//update progress bar
                    //match and get the bid codes for the request of their respective email
                    MatchCollection m1 = Regex.Matches(responseString, "onclick=\\\"lemail\\((.*?)\\)\\\"", RegexOptions.Singleline);
                    
                    if (isUrlChanged == false)
                    {
                        MatchCollection m4 = Regex.Matches(responseString, "<a href=\\\"(.*?)\\\" class=\\\"astyle13_2\\\">", RegexOptions.Singleline);
                        changedUrl = m4[1].Groups[1].Value;
                        changedUrl = "http://www.listedin.biz" + changedUrl;
                        isUrlChanged = true;
                        
                    }
                    
                    this.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (ThreadStart)delegate() { progressbar.Value = 40; });
                    
                    foreach (Match m in m1)//loop through to process and get the email addresses
                    {
                        string bdcode = m.Groups[1].Value;//get each bid code
                        Random rd = new Random();//needed as a prerequisite
                        DateTime dt = new DateTime();//same here
                        //same as this
                        double rndKey = rd.Next() * dt.ToUniversalTime().Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
                        using (var client2 = new WebClient())//send request for the email address
                        {
                            var values = new NameValueCollection();
                            values["bid"] = bdcode;//given the bid code
                            values["ps"] = "/directory/search-results.asp";
                            values["st"] = "1";
                            values["RandomKey"] = rndKey.ToString();//and random key generated
                            //save response which is a html page
                            var response = client2.UploadValues("http://www.listedin.biz/directory/ajax/process/other/eaddress.asp", values);
                            var responseString2 = Encoding.Default.GetString(response);
                            //match to get the email address from the html page
                            string bidCode;
                            MatchCollection m3 = Regex.Matches(responseString2, "value=\\\"(.*?)\\\"", RegexOptions.Singleline);
                            foreach (Match mt in m3)
                            {
                                bidCode = mt.Groups[1].Value;
                                bidnos.Add(bidCode);
                            }
                            this.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (ThreadStart)delegate() { progressbar.Value += 2.5; });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message.ToString());
            }

            this.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (ThreadStart)delegate() { progressbar.Value = 90; });
            this.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (ThreadStart)delegate()
            {
                progressbar.Value = 100;
                for (int i = 0; i < bidnos.Count; i++)
                {
                    myEmails.Add(bidnos[i]);//save to the observable collection
                }
                celistview.ItemsSource = myEmails;//bind to the listview
                progressbar.Value = 0;
            });
        }

        private void browsefile_btn_Click(object sender, RoutedEventArgs e)
        {
            //OpenFileDialog initialization
            dlg = new OpenFileDialog();
            dlg.Filter = "Office Files|*.csv";
            dlg.Title = "csv files only";
            dlg.ShowDialog();
            //Start the appending to file chosen from the file dialog
            filename = dlg.FileName;
            filenameappend_txt.Text = filename;
            browsefile_btn.IsEnabled = false;
            Thread th = new Thread(appendtofile);//Thread
            th.Start();//Started
        }

        private void appendtofile()
        {
            FileStream f;
            try
            {
               f = new FileStream(filename, FileMode.Open);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message.ToString());
                this.Dispatcher.BeginInvoke(DispatcherPriority.Normal,(ThreadStart)delegate(){browsefile_btn.IsEnabled = true;});
                return;
            }
            //Reader to know current status of the file,that is length
            StreamReader sr = new StreamReader(f);
            int count = 0;
            while (sr.ReadLine()!=null)
            {
                count += 1;
            }
            //Write to the file
            using (StreamWriter wr = new StreamWriter(f))
            {
                foreach (string m in myEmails)
                {
                    wr.WriteLine(count + "  ,  " + m);
                    count += 1;
                }
            }
            
            f.Close();
            sr.Close();

            this.Dispatcher.BeginInvoke(DispatcherPriority.Normal,(ThreadStart)delegate(){
                browsefile_btn.IsEnabled=true;
                MessageBox.Show("File Updated\n The file has been appended as requested");
            });
        }

        private void savefile_btn_Click(object sender, RoutedEventArgs e)
        {
            int count = 1;//numbers the email addresses
            //naming of file
            Random rd=new Random();
            string nwfilename = "Listedin" + rd.Next(9000) + ".csv";
            string newfile = @"..\..\..\..\..\..\..\Documents\" + nwfilename;
            FileStream f = new FileStream(newfile, FileMode.Create);
            //writing to file
            using (StreamWriter sw = new StreamWriter(f))
            {
                sw.WriteLine("No , Email");
                foreach (string E in myEmails)
                {
                    sw.WriteLine(count + "  ,  " + E);
                    count++;
                }
            }
            //Displaying file info
            FileInfo f_info = new FileInfo(newfile);
            this.ShowMessageAsync("Saved File Info", "The file name is: " + nwfilename + "\n File path is:" + f_info.DirectoryName);
            f.Close();
            
        }

        private void searchtxt_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            searchtxt.Text = "";//Clears text box
        }

        private void auto_append_chkbx_Checked(object sender, RoutedEventArgs e)
        {
            if (auto_append_chkbx.IsChecked == true)
            {
                from_value_txtbx.IsEnabled = true;
                to_value_txtbx.IsEnabled = true;
                from_value_txtbx.Text = "2";
                to_value_txtbx.Text = "10";
            }
            else
            {
                from_value_txtbx.Clear();
                to_value_txtbx.Clear();
                from_value_txtbx.IsEnabled = false;
                to_value_txtbx.IsEnabled = false;
            }
        }
    }
}
