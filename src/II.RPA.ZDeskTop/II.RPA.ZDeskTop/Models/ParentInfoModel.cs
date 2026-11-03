using Newtonsoft.Json;
using System;
using System.Drawing;

namespace II.RPA.ZDeskTop.Models;

public class ParentWindowModel
{
    [JsonProperty("hwnd")]
    public int WindowHandle
    {
        get;
        set;
    }

    [JsonProperty("rect")]
    public JsonRect Rectangle 
    { 
        get; 
        set; 
    }

    [JsonProperty("innerRect")]
    public JsonRect InnerRectangle
    {
        get;
        set;
    }

    [JsonProperty("screen")]
    public int Screen
    {
        get;
        set;
    }

    public nint Handle
    {
        get
        {
            return new IntPtr(WindowHandle);
        }
    }
    public Rectangle ToScreenCoordinate()
    {
        return new Rectangle(Rectangle.X + InnerRectangle.X,
            Rectangle.Y + InnerRectangle.Y,
            InnerRectangle.Width,
            InnerRectangle.Height);
    }
}
public struct JsonRect
{
    [JsonProperty("x")]
    public int X
    {
        get;
        set;
    }

    [JsonProperty("y")]
    public int Y
    {
        get;
        set;
    }

    [JsonProperty("w")]
    public int Width
    {
        get;
        set;
    }

    [JsonProperty("h")]
    public int Height
    {
        get;
        set;
    }

    public readonly Rectangle ToRectangle()
    {
        return new Rectangle(X, Y, Width, Height);
    }
}
