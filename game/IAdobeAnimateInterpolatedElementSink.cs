using Godot;

internal interface IAdobeAnimateInterpolatedElementSink
{
	bool TryAppendInterpolatedElement(AdobeAnimateRuntimeDefinition definition, AdobeAnimateSprite source, int mediaId, Transform2D transform, Color color);
}
