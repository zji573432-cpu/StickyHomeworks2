using Windows.Win32.Foundation;
using Windows.Win32.UI.Accessibility;
//using static Windows.Win32.PInvoke;


namespace StickyHomeworks.Services;

public class WindowFocusObserverService
{
    public event EventHandler? FocusChanged;

    public WindowFocusObserverService()
    {
        //SetWinEventHook(
        //    EVENT_SYSTEM_FOREGROUND, EVENT_SYSTEM_FOREGROUND,
        //    HMODULE.Null, (hook, @event, hwnd, idObject, child, thread, time) =>
        //    {
        //        FocusChanged?.Invoke(this, EventArgs.Empty);
        //    },
        //    0, 0,
        //    WINEVENT_OUTOFCONTEXT | WINEVENT_SKIPOWNPROCESS);
    }
}