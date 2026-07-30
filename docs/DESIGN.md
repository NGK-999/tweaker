# DESIGN — ApexTweaker (WPF)

Register: **product tool** (design serves the product).

## Tokens

- Surfaces: cool-neutral dark (`WindowBg` / `ContentBg` / `CardBg`); light theme optional via `AppThemeManager`
- Accent teal ≤10% of chrome (`AccentBrush`)
- Typography: Segoe UI Variable; Cascadia Mono only for metrics/data
- Radius: 8–10px max on containers
- Cards: flat or hairline border; never nest cards

## Rules

1. One primary CTA per page when possible
2. Section labels in sentence case (not shouting CAPS)
3. Nav active = thin accent bar + quiet fill (not saturated block)
4. Motion: opacity 150–200 ms ease-out; no decorative scale on page chrome
5. Busy banners: discrete info surface, not hero chrome

## Out of scope here

WPF-UI NuGet, Minecraft wizard redesign, backend contracts.
