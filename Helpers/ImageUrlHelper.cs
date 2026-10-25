using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ValousWorld.Web.Helpers;

public static class ImageUrlHelper
{
    public static IHtmlContent ResponsiveImage(
        this IHtmlHelper html,
        string? url,
        string alt,
        string cssClass = "",
        string sizes = "(max-width: 500px) 50vw, (max-width: 1100px) 33vw, 16vw",
        bool lazy = true)
    {
        if (string.IsNullOrWhiteSpace(url)) return HtmlString.Empty;

        var altEnc = System.Net.WebUtility.HtmlEncode(alt);
        var lazyAttr = lazy ? "loading=\"lazy\" decoding=\"async\"" : "loading=\"eager\" fetchpriority=\"high\"";

        // Seedha image render karo (FileService pehle se WebP aur optimized bana raha hai)
        return new HtmlString($"<img src=\"{url}\" alt=\"{altEnc}\" class=\"{cssClass}\" {lazyAttr}>");
    }
}