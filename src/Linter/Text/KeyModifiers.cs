using System;

namespace Linter.Text;

[Flags]
internal enum KeyModifiers
{
    None     = 0,
    Shift    = 1 << 0,
    Control  = 1 << 1,
    Alt      = 1 << 2,
    Win      = 1 << 3,
    CapsLock = 1 << 4,
}
