public interface IElementUpgrade
{
    AnchorElement Element { get; }

    void OnElementAttached(AnchorBase anchor);
    void OnElementDetached();
}