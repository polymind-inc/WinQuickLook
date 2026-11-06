using Microsoft.UI.Reactor.Wrappers;
using Microsoft.UI.Xaml.Controls;

namespace WinQuickLook.App;

// The built-in Image factory takes a URI. This wrapper accepts in-memory images
// so previews and shell thumbnails never keep a file open.
[GenerateReactorWrapper(typeof(Image), AutoDiscover = false)]
public partial record BitmapImageElement;
