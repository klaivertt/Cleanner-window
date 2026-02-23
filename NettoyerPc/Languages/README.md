# Adding a translation to PC Clean

## Quick Start

1. Copy `_template.json` to a new file named after your language tag, e.g. `de-DE.json`
2. Edit the `_meta` section with your language info
3. Translate every value (keep the keys unchanged)
4. Place the file in this `Languages/` folder next to `NettoyerPc.exe`
5. The new language will appear automatically in **Settings → Language**

## File format

```json
{
  "_meta": {
    "tag":     "de-DE",
    "name":    "Deutsch",
    "author":  "Your Name",
    "version": "1.0"
  },
  "app.subtitle": "Leistung · Speicher freigeben · Systemsicherheit · Gaming",
  "btn.settings": "Einstellungen",
  ...
}
```

## Rules

- The `_meta.tag` must be a valid BCP-47 code (e.g. `de-DE`, `es-ES`, `ja-JP`)
- Any key not present in your file falls back to **French** automatically
- You can also **override individual keys** of an existing language (FR or EN)
  by using the same tag as an existing language (`fr-FR` or `en-US`)
- The file **must be UTF-8 encoded**

## Overriding a built-in language

To change a single string in French without creating a full translation,
create `fr-FR.json` containing only the keys you want to override:

```json
{
  "_meta": { "tag": "fr-FR", "name": "Français (custom)", "author": "Me" },
  "btn.quick": "NETTOYER MAINTENANT"
}
```

## Available keys

See `_template.json` for the full list of available keys with their English values.
