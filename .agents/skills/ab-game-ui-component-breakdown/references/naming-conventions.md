# Component Semantic Naming Conventions

The repository uses lowercase kebab-case everywhere; do not use `button1.png` or meaningless indices as final delivery names.

## Format

```text
<scope>-<component>-<variant>-<state>.png
```

Examples:

```text
shop-buy-button-normal.png
shop-buy-button-hover.png
shop-buy-button-pressed.png
shop-category-tab-active.png
shop-currency-coin-default.png
shop-sale-badge-default.png
```

## Rules

- `scope`: page or system, e.g. `shop`, `battle`, `inventory`.
- `component`: component responsibility, e.g. `buy-button`, `product-card`.
- `variant`: material, hierarchy or semantic variant; may be omitted when there is none.
- `state`: `default`, `normal`, `hover`, `pressed`, `disabled`, `active`, etc.
- All states of the same component must share a `component_id`.
- Automatic sprite-sheet detection indices are for intermediate results only; they may be approved only after being mapped to semantic names.

Names do not use underscores, to stay consistent with the kebab-case rules for project IDs and contract IDs.
