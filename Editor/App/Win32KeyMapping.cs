using ImGuiNET;
using T3.SystemUi;

namespace T3.Editor.App;

/// <summary>
/// Maps TiXL's <see cref="Key"/> enum (which mirrors Win32 virtual-key codes)
/// to ImGui's <see cref="ImGuiKey"/> enum.
/// </summary>
/// <remarks>
/// Required since ImGui 1.87 introduced first-class <see cref="ImGuiKey"/> values
/// that are no longer Win32 VK codes. The legacy compatibility layer that allowed
/// casting native VK codes directly to <see cref="ImGuiKey"/> was removed in 1.90
/// (<c>IMGUI_DISABLE_OBSOLETE_KEYIO</c> is enabled by default).
/// </remarks>
internal static class Win32KeyMapping
{
    public static ImGuiKey ToImGuiKey(this Key key)
    {
        return (int)key switch
               {
                   // Digits (top row)
                   48 => ImGuiKey._0,
                   49 => ImGuiKey._1,
                   50 => ImGuiKey._2,
                   51 => ImGuiKey._3,
                   52 => ImGuiKey._4,
                   53 => ImGuiKey._5,
                   54 => ImGuiKey._6,
                   55 => ImGuiKey._7,
                   56 => ImGuiKey._8,
                   57 => ImGuiKey._9,

                   // Letters
                   65 => ImGuiKey.A,
                   66 => ImGuiKey.B,
                   67 => ImGuiKey.C,
                   68 => ImGuiKey.D,
                   69 => ImGuiKey.E,
                   70 => ImGuiKey.F,
                   71 => ImGuiKey.G,
                   72 => ImGuiKey.H,
                   73 => ImGuiKey.I,
                   74 => ImGuiKey.J,
                   75 => ImGuiKey.K,
                   76 => ImGuiKey.L,
                   77 => ImGuiKey.M,
                   78 => ImGuiKey.N,
                   79 => ImGuiKey.O,
                   80 => ImGuiKey.P,
                   81 => ImGuiKey.Q,
                   82 => ImGuiKey.R,
                   83 => ImGuiKey.S,
                   84 => ImGuiKey.T,
                   85 => ImGuiKey.U,
                   86 => ImGuiKey.V,
                   87 => ImGuiKey.W,
                   88 => ImGuiKey.X,
                   89 => ImGuiKey.Y,
                   90 => ImGuiKey.Z,

                   // Function keys
                   112 => ImGuiKey.F1,
                   113 => ImGuiKey.F2,
                   114 => ImGuiKey.F3,
                   115 => ImGuiKey.F4,
                   116 => ImGuiKey.F5,
                   117 => ImGuiKey.F6,
                   118 => ImGuiKey.F7,
                   119 => ImGuiKey.F8,
                   120 => ImGuiKey.F9,
                   121 => ImGuiKey.F10,
                   122 => ImGuiKey.F11,
                   123 => ImGuiKey.F12,

                   // Editing / navigation
                   8 => ImGuiKey.Backspace,
                   9 => ImGuiKey.Tab,
                   13 => ImGuiKey.Enter,
                   20 => ImGuiKey.CapsLock,
                   27 => ImGuiKey.Escape,
                   32 => ImGuiKey.Space,
                   33 => ImGuiKey.PageUp,
                   34 => ImGuiKey.PageDown,
                   35 => ImGuiKey.End,
                   36 => ImGuiKey.Home,
                   37 => ImGuiKey.LeftArrow,
                   38 => ImGuiKey.UpArrow,
                   39 => ImGuiKey.RightArrow,
                   40 => ImGuiKey.DownArrow,
                   45 => ImGuiKey.Insert,
                   46 => ImGuiKey.Delete,

                   // Modifiers — TiXL's Key enum uses the generic VK codes
                   // (no L/R distinction). Map to the Left variant; ImGui's
                   // ImGuiMod_* events are sent separately for shortcut matching.
                   16 => ImGuiKey.LeftShift,
                   17 => ImGuiKey.LeftCtrl,
                   18 => ImGuiKey.LeftAlt,

                   // OEM punctuation (US layout)
                   186 => ImGuiKey.Semicolon,    // VK_OEM_1
                   187 => ImGuiKey.Equal,        // VK_OEM_PLUS  (Key.Plus / Key.Equal)
                   188 => ImGuiKey.Comma,        // VK_OEM_COMMA
                   189 => ImGuiKey.Minus,        // VK_OEM_MINUS
                   190 => ImGuiKey.Period,       // VK_OEM_PERIOD
                   191 => ImGuiKey.Slash,        // VK_OEM_2
                   192 => ImGuiKey.GraveAccent,  // VK_OEM_3 (Key.Pipe — name is misleading)
                   219 => ImGuiKey.LeftBracket,  // VK_OEM_4
                   220 => ImGuiKey.Backslash,    // VK_OEM_5 (Key.HashTag — name is misleading)
                   221 => ImGuiKey.RightBracket, // VK_OEM_6
                   226 => ImGuiKey.Apostrophe,   // VK_OEM_102 (Key.Apostrophe — note: VK_OEM_7 = 222 is the real apostrophe; this matches the existing TiXL Key enum)

                   _ => ImGuiKey.None,
               };
    }
}
