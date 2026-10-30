# World cartography

`WorldCountries.json` contains Natural Earth 1:110m Admin 0 country polygon boundaries, with coordinates rounded to four decimal places and unused attributes removed. It includes 177 country/territory shapes; only the ten locations in `WorldLocations.cs` are playable in MVP 0.2.

- Source: https://raw.githubusercontent.com/nvkelso/natural-earth-vector/master/geojson/ne_110m_admin_0_countries.geojson
- Retrieved: 2026-10-30
- Source file SHA-256: `6866c877d39cba9c357620878839b336d569f8c662d3cfab4cb1dbe2d39c977f`
- License: **public domain**; https://www.naturalearthdata.com/about/terms-of-use/
- Conversion: `Tools/ImportWorldMap.ps1`. Coordinates remain longitude/latitude; no fictional device or security statistics are sourced from Natural Earth.

The supplied reference image guided the dark cartographic style. The game renders vector boundaries locally; it does not load online maps or contact real networks.
