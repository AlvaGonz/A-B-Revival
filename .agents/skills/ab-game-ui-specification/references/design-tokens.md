# Game UI Design Tokens

`style-contract.yaml` is the single source of truth for global design tokens; do not create a parallel design system.

## Layers

1. Raw values: `colors`, `typography`, `geometry`, `effects`
2. Semantic aliases: `tokens.color`, `tokens.spacing`, `tokens.motion`
3. Component references: `style_dependencies` in `component-contract.yaml`

Example:

```yaml
colors:
  primary: "#58C36A"
tokens:
  color:
    action-primary: "#58C36A"
  spacing:
    sm: 8
    md: 16
```

Rules:

- Token names use lowercase kebab-case.
- Page prompts reference semantic purpose, not just bare color values.
- When switching style, change global values and semantic aliases first, then mark affected assets as `stale`.
- API tokens or access keys must be called "credentials" to avoid confusion with design tokens.
