using Avalonia.Media.Imaging;

namespace Anu.Core.Models;

public class MessageContentPart
{
    private MessageContentPartKind _kind;

    public MessageContentPartKind Kind => _kind;

    public string? Text { get; }

    public Bitmap? Image { get; }

    private MessageContentPart(
        MessageContentPartKind kind,
        string? text = null,
        Bitmap? image = null)
    {
        _kind = kind;
        Text = text;
        Image = image;
    }

    public static MessageContentPart CreateTextPart(string text)
    {
        return new MessageContentPart(MessageContentPartKind.Text, text);
    }

    public static MessageContentPart CreateImagePart(Bitmap? image)
    {
        return new MessageContentPart(
            kind: MessageContentPartKind.Image,
            image: image
        );
    }
}