using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Popups;

namespace ExceptionHelper
{
    public static class Exceptions
    {
        public static async void ThrownExceptionError(System.Exception ex)
        {

            var ThrownException = new MessageDialog($"{ex.Message}\n\n{ex.Source}\n\n{ex.ToString()}\n\n{ex.StackTrace}");
            ThrownException.Commands.Add(new UICommand("Close"));
            await ThrownException.ShowAsync();
        }
        public static async void ThrownExceptionErrorExtended(System.Exception ex)
        {

            var ThrownException = new MessageDialog($"{ex.Message}\n\n{ex.Source}\n\n{ex.Data}\n\n{ex.StackTrace}\n\n{ex.InnerException}");
            ThrownException.Commands.Add(new UICommand("Close"));
            await ThrownException.ShowAsync();
        }

        public static async Task URLNameBlank(string textbox, Exception ex)
        {
            var UrlException = new MessageDialog($"{textbox} is blank! Please enter a URL\n\n {ex}");
            UrlException.Commands.Add(new UICommand("Close"));
            await UrlException.ShowAsync();
        }

        public static async void DownloadCompleted(string filename)
        {
            var DLComplete = new MessageDialog($"{filename} has downloaded Successfully");
            DLComplete.Commands.Add(new UICommand("Close"));
            await DLComplete.ShowAsync();
        }
        public static async void AstoriaSetupError()
        {
            var AstoriaErr = new MessageDialog($"Make sure both Download and Android Storage Folders are set!");
            AstoriaErr.Commands.Add(new UICommand("Close"));
            await AstoriaErr.ShowAsync();
        }
        public static async void CustomException(string String)
        {
            var CustErr = new MessageDialog(String);
            CustErr.Commands.Add(new UICommand("Close"));
            await CustErr.ShowAsync();
        }
        public static async void WDPException(string String)
        {
            var wdpErr = new MessageDialog(String);
            wdpErr.Commands.Add(new UICommand("Close"));
            await wdpErr.ShowAsync();
        }
        public static async void WDPLocalAppDataError(string pkgFullName)
        {
            var wdpErr = new MessageDialog($"ERROR: Issue accessing {pkgFullName}, you may not have permission to view this folder.");
            wdpErr.Commands.Add(new UICommand("Close"));
            await wdpErr.ShowAsync();
        }
        public static async void WDPSuccessDownload(string fileName, string Output)
        {
            var wdpErr = new MessageDialog($"{fileName} was successfully saved to {Output}");
            wdpErr.Commands.Add(new UICommand("Close"));
            await wdpErr.ShowAsync();
        }
        public static async void WDPDownloadFail(string fileName, string Output, Exception ex)
        {
            var wdpErr = new MessageDialog($"{fileName} failed to save to {Output}\n\nERROR: {ex.Message}\n\n{ex.StackTrace}");
            wdpErr.Commands.Add(new UICommand("Close"));
            await wdpErr.ShowAsync();
        }
        public static async void WDPUploadSuccess(string Output, string wdpaddress)
        {
            var wdpErr = new MessageDialog($"Upload to {wdpaddress}:{Output} successfully, Check device to confirm");
            wdpErr.Commands.Add(new UICommand("Close"));
            await wdpErr.ShowAsync();
        }
        public static async void WDPUploadSuccess1(string Output, string wdpaddress)
        {
            var wdpErr = new MessageDialog($"Upload to {wdpaddress}:{Output} finished, but internal errors were returned, please check to see if file transfered.");
            wdpErr.Commands.Add(new UICommand("Close"));
            await wdpErr.ShowAsync();
        }
        public static async void WDPUploadFail(Exception ex)
        {
            var wdpErr = new MessageDialog($"Failed to Upload\n\nERROR: {ex.Message}\n\n{ex.StackTrace}");
            wdpErr.Commands.Add(new UICommand("Close"));
            await wdpErr.ShowAsync();
        }
    }
}
