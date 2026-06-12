using System.Collections.Generic;

namespace Renomeador.Models;

internal sealed record PresetRecommendation(
    HardwareTier Tier,
    PresetKind RecommendedPreset,
    string Title,
    string Reason,
    IReadOnlyList<string> Notes);
