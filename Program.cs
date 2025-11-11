using System.Diagnostics;
using System.Runtime.InteropServices;
using HtmlAgilityPack;

namespace DesktopWallpaper;

class Program
{
    const string wallpaperHostUrl = "https://4kwallpapers.com/random-wallpapers/";
    private const string wallpaperFolderPath = "D:/wallpapers";
    
    private static Queue<string> wallpaperQueue = new();

    static void Main(string[] args)
    {
        MakeRunBackground();
            
        while (true)
        {
            Thread.Sleep(TimeSpan.FromMinutes(1));
            
            if (wallpaperQueue.Count == 0)
            {
                var html = DownloadHtml(wallpaperHostUrl);
                var wallpaperUrls = ListWallpaperUrls(html);
                foreach (var url in wallpaperUrls)
                {
                    html = DownloadHtml(url);
                    var imgUrl = GetWallpaperImageUrl(html);
                    var imgPath = DownloadWallpaper(imgUrl);
                    wallpaperQueue.Enqueue(imgPath);
                }
            }
            
            SetAsDesktopWallpaper(wallpaperQueue.Dequeue());
        }
    }
    
    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    private static void MakeRunBackground()
    {
        const int SW_HIDE = 0;
        
        IntPtr handle = Process.GetCurrentProcess().MainWindowHandle;
        ShowWindow(handle, SW_HIDE);
    }

    private static List<string> ListWallpaperUrls(string html)
    {
        var htmlDoc = new HtmlDocument();
        htmlDoc.LoadHtml(html);
        
        // Find the div with id="pics-list"
        var picsListDiv = htmlDoc.DocumentNode.SelectSingleNode("//div[@id='pics-list']");
        
        // Find all <a> elements with class="wallpapers__canvas_image" within picsListDiv
        var wallpaperLinks = picsListDiv.SelectNodes(".//a[@class='wallpapers__canvas_image']");
        
        var urls = new List<string>();
        foreach (var link in wallpaperLinks)
        {
            var href = link.GetAttributeValue("href", "");
            urls.Add(href);
        }
        
        return urls;
    }
    
    private static string GetWallpaperImageUrl(string html)
    {
        var htmlDoc = new HtmlDocument();
        htmlDoc.LoadHtml(html);
        
        // Find the span with id="res-56"
        var resSpan = htmlDoc.DocumentNode.SelectSingleNode("//span[@id='res-56']");
        
        // Find 'a' element within resSpan that has href containing '1920x1080'
        var linkNode = resSpan.SelectSingleNode(".//a[contains(@href, '1920x1080')]");
        
        var imageUrl = linkNode.GetAttributeValue("href", "");
        
        return $"https://4kwallpapers.com{imageUrl}";
    }
    
    private static string DownloadHtml(string url)
    {
        using (var client = new System.Net.WebClient())
        {
            return client.DownloadString(url);
        }
    }
    
    private static string DownloadWallpaper(string url)
    {
        var now = DateTime.Now;
        var nowAsString = $"{now.Day}-{now.Month}-{now.Year}";
        var directoryPath = $"{wallpaperFolderPath}/{nowAsString}";
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }
        
        // Extract filename from URL
        var fileName = Path.GetFileName(url);
        var path = $"{directoryPath}/{fileName}";
        
        using (var client = new System.Net.WebClient())
        {
            client.DownloadFile(url, path);
        }
        return path;
    }
    
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SystemParametersInfo(
        uint uiAction, uint uiParam, string pvParam, uint fWinIni);
    
    private static void SetAsDesktopWallpaper(string path)
    {
        const uint SPI_SETDESKWALLPAPER = 0x0014;
        const uint SPIF_UPDATEINIFILE = 0x01;
        const uint SPIF_SENDCHANGE = 0x02;
        
        SystemParametersInfo(
            SPI_SETDESKWALLPAPER, 0, path, 
            SPIF_UPDATEINIFILE | SPIF_SENDCHANGE);
    }
}