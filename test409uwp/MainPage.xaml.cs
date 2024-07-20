using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System;
using Windows.Web.Http;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace test409uwp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
        }


        private string _defaultHttpAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36 Edg/126.0.0.0";

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            uiMsg.Text = "starting...";

            var oHandler = new System.Net.Http.HttpClientHandler();
            oHandler.CookieContainer = new System.Net.CookieContainer();

            var _oHttp = new System.Net.Http.HttpClient(oHandler);
            _oHttp.DefaultRequestHeaders.UserAgent.TryParseAdd(_defaultHttpAgent);
            _oHttp.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("*/*"));
            _oHttp.DefaultRequestHeaders.AcceptLanguage.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("en-US"));
            _oHttp.DefaultRequestHeaders.AcceptLanguage.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("en"));
            //' _oHttp.DefaultRequestHeaders.AcceptEncoding.Add(New Net.Http.Headers.StringWithQualityHeaderValue("gzip"));// ' Accept - Encoding: gzip, deflate

            //'_oHttp.DefaultRequestHeaders.AcceptEncoding.Add(New Net.Http.Headers.StringWithQualityHeaderValue("deflate"))
            //'_oHttp.DefaultRequestHeaders.Connection.Add("Keep-alive")

            System.Net.Http.HttpResponseMessage oResp;// = As Net.Http.HttpResponseMessage

            //' przygotuj pContent, będzie przy redirect używany ponownie
            oResp = await _oHttp.GetAsync("http://www.skyscrapercity.com/login");

            if (oResp.IsSuccessStatusCode)
                uiMsg.Text = "Success!";
            else
                uiMsg.Text = "Error code: " + oResp.StatusCode.ToString();

            var iHttp = new HttpClient();
            var iRestp = await iHttp.GetAsync(new Uri("http://www.skyscrapercity.com/login"));

            if (iRestp.IsSuccessStatusCode)
                uiMsg.Text += "Second: OK";
            else
uiMsg.Text += "second error" + oResp.StatusCode.ToString();

        }
    }
}
