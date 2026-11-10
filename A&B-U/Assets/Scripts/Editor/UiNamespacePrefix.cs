using UnityEditor.UIElements;

// Maps the runtime presentation namespace to the UXML prefix used by Main.uxml.
// UxmlNamespacePrefix lives in the editor assembly; the imported UXML asset stores
// the resolved element type, so the player build does not need this attribute.
[assembly: UxmlNamespacePrefix("ABRevival.Presentation", "app")]
