using System;
using System.Collections.Generic;
using System.Diagnostics;
////
////
//// The Icons in "mimetypes/*.png" belong to https://github.com/redbooth/free-file-icons
//// Folder was added from icons8.com
//// 7z.png I modified myself
////
////
////
////
////

using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;

namespace WPDevPortal
{
    /// <summary>
    /// Match file extention to icon asset
    /// </summary>
    class CommonFileIcons
    {
        public static BitmapImage IconFromExtention(string FileName)
        {
            var ext = Path.GetExtension(FileName);
            Debug.WriteLine($"MIMETYPE: {ext}");

            switch (ext)
            {
                case ".appx":
                    BitmapImage appx = new BitmapImage(new Uri("ms-appx:///mimetypes/appx.png"));
                    return appx;
                case ".appxbundle":
                    BitmapImage appxbundle = new BitmapImage(new Uri("ms-appx:///mimetypes/appx.png"));
                    return appxbundle;
                case ".aac":
                    BitmapImage aac = new BitmapImage(new Uri("ms-appx:///mimetypes/aac.png"));
                    return aac;
                case ".ai":
                    BitmapImage ai = new BitmapImage(new Uri("ms-appx:///mimetypes/ai.png"));
                    return ai;
                case ".aiff":
                    BitmapImage aiff = new BitmapImage(new Uri("ms-appx:///mimetypes/aiff.png"));
                    return aiff;
                case ".avi":
                    BitmapImage avi = new BitmapImage(new Uri("ms-appx:///mimetypes/avi.png"));
                    return avi;
                case ".bmp":
                    BitmapImage bmp = new BitmapImage(new Uri("ms-appx:///mimetypes/bmp.png"));
                    return bmp;
                case ".c":
                    BitmapImage c = new BitmapImage(new Uri("ms-appx:///mimetypes/c.png"));
                    return c;
                case ".cpp":
                    BitmapImage cpp = new BitmapImage(new Uri("ms-appx:///mimetypes/cpp.png"));
                    return cpp;
                case ".css":
                    BitmapImage css = new BitmapImage(new Uri("ms-appx:///mimetypes/css.png"));
                    return css;
                case ".csv":
                    BitmapImage csv = new BitmapImage(new Uri("ms-appx:///mimetypes/csv.png"));
                    return csv;
                case ".dat":
                    BitmapImage dat = new BitmapImage(new Uri("ms-appx:///mimetypes/dat.png"));
                    return dat;
                case ".dmg":
                    BitmapImage dmg = new BitmapImage(new Uri("ms-appx:///mimetypes/dmg.png"));
                    return dmg;
                case ".doc":
                    BitmapImage doc = new BitmapImage(new Uri("ms-appx:///mimetypes/doc.png"));
                    return doc;
                case ".docx":
                    BitmapImage docx = new BitmapImage(new Uri("ms-appx:///mimetypes/doc.png"));
                    return docx;
                case ".dotx":
                    BitmapImage dotx = new BitmapImage(new Uri("ms-appx:///mimetypes/dotx.png"));
                    return dotx;
                case ".dwg":
                    BitmapImage dwg = new BitmapImage(new Uri("ms-appx:///mimetypes/dwg.png"));
                    return dwg;
                case ".dxf":
                    BitmapImage dxf = new BitmapImage(new Uri("ms-appx:///mimetypes/dxf.png"));
                    return dxf;
                case ".eps":
                    BitmapImage eps = new BitmapImage(new Uri("ms-appx:///mimetypes/eps.png"));
                    return eps;
                case ".exe":
                    BitmapImage exe = new BitmapImage(new Uri("ms-appx:///mimetypes/exe.png"));
                    return exe;
                case ".flv":
                    BitmapImage flv = new BitmapImage(new Uri("ms-appx:///mimetypes/flv.png"));
                    return flv;
                case ".gif":
                    BitmapImage gif = new BitmapImage(new Uri("ms-appx:///mimetypes/gif.png"));
                    return gif;
                case ".h":
                    BitmapImage h = new BitmapImage(new Uri("ms-appx:///mimetypes/h.png"));
                    return h;
                case ".hpp":
                    BitmapImage hpp = new BitmapImage(new Uri("ms-appx:///mimetypes/hpp.png"));
                    return hpp;
                case ".html":
                    BitmapImage html = new BitmapImage(new Uri("ms-appx:///mimetypes/html.png"));
                    return html;
                case ".ics":
                    BitmapImage ics = new BitmapImage(new Uri("ms-appx:///mimetypes/ics.png"));
                    return ics;
                case ".iso":
                    BitmapImage iso = new BitmapImage(new Uri("ms-appx:///mimetypes/iso.png"));
                    return iso;
                case ".java":
                    BitmapImage java = new BitmapImage(new Uri("ms-appx:///mimetypes/java.png"));
                    return java;
                case ".jar":
                    BitmapImage jar = new BitmapImage(new Uri("ms-appx:///mimetypes/java.png"));
                    return jar;
                case ".jpg":
                    BitmapImage jpg = new BitmapImage(new Uri("ms-appx:///mimetypes/jpg.png"));
                    return jpg;
                case ".js":
                    BitmapImage js = new BitmapImage(new Uri("ms-appx:///mimetypes/js.png"));
                    return js;
                case ".key":
                    BitmapImage key = new BitmapImage(new Uri("ms-appx:///mimetypes/key.png"));
                    return key;
                case ".less":
                    BitmapImage less = new BitmapImage(new Uri("ms-appx:///mimetypes/less.png"));
                    return less;
                case ".mid":
                    BitmapImage mid = new BitmapImage(new Uri("ms-appx:///mimetypes/mid.png"));
                    return mid;
                case ".mp3":
                    BitmapImage mp3 = new BitmapImage(new Uri("ms-appx:///mimetypes/mp3.png"));
                    return mp3;
                case ".mp4":
                    BitmapImage mp4 = new BitmapImage(new Uri("ms-appx:///mimetypes/mp4.png"));
                    return mp4;
                case ".mpg":
                    BitmapImage mpg = new BitmapImage(new Uri("ms-appx:///mimetypes/mpg.png"));
                    return mpg;
                case ".odf":
                    BitmapImage odf = new BitmapImage(new Uri("ms-appx:///mimetypes/odf.png"));
                    return odf;
                case ".ods":
                    BitmapImage ods = new BitmapImage(new Uri("ms-appx:///mimetypes/ods.png"));
                    return ods;
                case ".odt":
                    BitmapImage odt = new BitmapImage(new Uri("ms-appx:///mimetypes/odt.png"));
                    return odt;
                case ".otp":
                    BitmapImage otp = new BitmapImage(new Uri("ms-appx:///mimetypes/otp.png"));
                    return otp;
                case ".ots":
                    BitmapImage ots = new BitmapImage(new Uri("ms-appx:///mimetypes/ots.png"));
                    return ots;
                case ".ott":
                    BitmapImage ott = new BitmapImage(new Uri("ms-appx:///mimetypes/ott.png"));
                    return ott;
                case ".pdf":
                    BitmapImage pdf = new BitmapImage(new Uri("ms-appx:///mimetypes/pdf.png"));
                    return pdf;
                case ".php":
                    BitmapImage php = new BitmapImage(new Uri("ms-appx:///mimetypes/php.png"));
                    return php;
                case ".png":
                    BitmapImage png = new BitmapImage(new Uri("ms-appx:///mimetypes/png.png"));
                    return png;
                case ".ppt":
                    BitmapImage ppt = new BitmapImage(new Uri("ms-appx:///mimetypes/ppt.png"));
                    return ppt;
                case ".psd":
                    BitmapImage psd = new BitmapImage(new Uri("ms-appx:///mimetypes/psd.png"));
                    return psd;
                case ".py":
                    BitmapImage py = new BitmapImage(new Uri("ms-appx:///mimetypes/py.png"));
                    return py;
                case ".qt":
                    BitmapImage qt = new BitmapImage(new Uri("ms-appx:///mimetypes/qt.png"));
                    return qt;
                case ".rar":
                    BitmapImage rar = new BitmapImage(new Uri("ms-appx:///mimetypes/rar.png"));
                    return rar;
                case ".rb":
                    BitmapImage rb = new BitmapImage(new Uri("ms-appx:///mimetypes/rb.png"));
                    return rb;
                case ".rtf":
                    BitmapImage rtf = new BitmapImage(new Uri("ms-appx:///mimetypes/rtf.png"));
                    return rtf;
                case ".sass":
                    BitmapImage sass = new BitmapImage(new Uri("ms-appx:///mimetypes/sass.png"));
                    return sass;
                case ".scss":
                    BitmapImage scss = new BitmapImage(new Uri("ms-appx:///mimetypes/scss.png"));
                    return scss;
                case ".sql":
                    BitmapImage sql = new BitmapImage(new Uri("ms-appx:///mimetypes/sql.png"));
                    return sql;
                case ".tga":
                    BitmapImage tga = new BitmapImage(new Uri("ms-appx:///mimetypes/tga.png"));
                    return tga;
                case ".tgz":
                    BitmapImage tgz = new BitmapImage(new Uri("ms-appx:///mimetypes/tgz.png"));
                    return tgz;
                case ".tiff":
                    BitmapImage tiff = new BitmapImage(new Uri("ms-appx:///mimetypes/tiff.png"));
                    return tiff;
                case ".txt":
                    BitmapImage txt = new BitmapImage(new Uri("ms-appx:///mimetypes/txt.png"));
                    return txt;
                case ".wav":
                    BitmapImage wav = new BitmapImage(new Uri("ms-appx:///mimetypes/wav.png"));
                    return wav;
                case ".xls":
                    BitmapImage xls = new BitmapImage(new Uri("ms-appx:///mimetypes/xls.png"));
                    return xls;
                case ".xlsx":
                    BitmapImage xlsx = new BitmapImage(new Uri("ms-appx:///mimetypes/xlsx.png"));
                    return xlsx;
                case ".xml":
                    BitmapImage xml = new BitmapImage(new Uri("ms-appx:///mimetypes/xml.png"));
                    return xml;
                case ".yml":
                    BitmapImage yml = new BitmapImage(new Uri("ms-appx:///mimetypes/yml.png"));
                    return yml;
                case ".zip":
                    BitmapImage zip = new BitmapImage(new Uri("ms-appx:///mimetypes/zip.png"));
                    return zip;
                case ".7z":
                    BitmapImage sevenz = new BitmapImage(new Uri("ms-appx:///mimetypes/7z.png"));
                    return sevenz;
                default:

                    BitmapImage defaultImage = new BitmapImage(new Uri("ms-appx:///mimetypes/blank.png"));
                    return defaultImage;
            }

        }
    }
}
