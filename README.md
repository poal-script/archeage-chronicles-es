<p align="center"><img src="icono.png" width="160" alt="poal-script"></p>

# ArcheAge Chronicles — Traducción al español (no oficial)

Traducción de fans de **ArcheAge Chronicles** al español: interfaz, misiones, diálogos, objetos, habilidades y descripciones (más de 54.000 textos). Las voces no cambian.

**[⬇ Descargar la última versión](https://github.com/poal-script/archeage-chronicles-es/releases/latest)**

## Instalación

1. Descarga `AAC-Traduccion-ES.exe` desde la [última release](https://github.com/poal-script/archeage-chronicles-es/releases/latest).
2. Cierra el juego y su lanzador.
3. Ejecuta el instalador (pide permisos de administrador porque escribe en la carpeta del juego de Steam) y pulsa **Instalar / Actualizar**.
4. Abre el juego desde Steam.

Si siguiera en inglés: Steam → ArcheAge Chronicles → Propiedades → Opciones de lanzamiento → `-culture=es-ES`.

## Actualizaciones

Cuando el juego reciba un parche, se publicará una versión nueva de la traducción aquí. Abre el instalador de nuevo y pulsa **Buscar actualización** e **Instalar / Actualizar**: descarga solo la traducción nueva.

## Volver al inglés

Abre el instalador y pulsa **Volver al inglés**. Los archivos de la traducción quedan en su sitio para reactivarla cuando quieras.

## Qué hace exactamente

- Copia `String.locres` en `X5\Content\Localization\String\es-ES\` dentro de la carpeta del juego.
- Cambia `Culture=es-ES` en `%LOCALAPPDATA%\X5\Saved\Config\Windows\GameUserSettings.ini` (guarda antes una copia: `GameUserSettings.ini.antes-de-traduccion`).
- **No modifica** los archivos `.pak` ni el ejecutable del juego.
- Comprueba la integridad (SHA-256) de lo que descarga.

El código del instalador está en [`Instalador.cs`](Instalador.cs).

## Notas de traducción

- **Traits** y **Raid** se mantienen en inglés a propósito.
- Glosario: Guild = Gremio, Company = Compañía, Plot = Parcela, HP/SP = PV/PH.
- ¿Has visto un error? Abre un [issue](https://github.com/poal-script/archeage-chronicles-es/issues) con una captura y dónde aparece.

## Apoya el proyecto

La traducción es y será siempre gratuita. Si te resulta útil y quieres ayudar a mantenerla al día con cada parche:

**[☕ Ko-fi: poalscript](https://ko-fi.com/poalscript)**

## Aviso

Proyecto de fans, no oficial. Sin relación con XLGAMES ni Kakao Games. ArcheAge Chronicles y sus contenidos pertenecen a sus respectivos propietarios. Úsalo bajo tu responsabilidad.

Contacto: poal.script@proton.me
