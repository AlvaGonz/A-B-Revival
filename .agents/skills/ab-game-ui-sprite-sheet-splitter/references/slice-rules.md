# Slicing Modes and 9-Slice Rules

## Default slicing modes

- Button, Frame, Popup, Panel, Progress: `9-slice`
- Card, Badge, Character, NPC, Decoration, Background: `full`
- Icon, Currency: `1:1`
- Repeatable textures: `tile`

Defaults live in the repository's `config/slice-rules.yaml`.

## 9-slice

```yaml
slice:
  type: 9-slice
  margins: [left, right, top, bottom]
```

Requirements:

- `left + right < width`
- `top + bottom < height`
- Corners and outlines must not enter the central stretch region
- Text must not be baked into a stretchable backplate
- Margins must be manually previewed at the actual target size

The built-in tool only stores and validates margins; it does not infer the stretch region from pixels. Automatic nine-patch analysis is roadmap capability.
