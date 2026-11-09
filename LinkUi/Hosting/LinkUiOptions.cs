using System.Numerics;
using Dalamud.Interface;

namespace LinkUi;

/// <summary>What a plugin gives the host: its accent colour and the icon its window headers show.</summary>
public sealed record LinkUiOptions(Vector4 Accent, FontAwesomeIcon? HeaderIcon = null);
