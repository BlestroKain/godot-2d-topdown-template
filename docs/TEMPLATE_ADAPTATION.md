# Auditoría inicial del template

Revisión local: ea95789514fe28532d4ed444bdd06a9d4cddad31. Los archivos se conservan en client/; el repositorio de origen conserva su licencia en la raíz y los componentes sus propios avisos.

| Archivo o grupo | Tratamiento | Estado |
| --- | --- | --- |
| scenes/, components/, tilesets/, particles/, shaders/, dialogues/ | KEEP: presentación y recursos reutilizables | Conservados |
| scripts/state_machine/ | KEEP/ADAPT: solo estados visuales en el cliente MMO | Conservados; revisión de cada consumidor pendiente |
| entities/player/player_entity.gd y controller | ADAPT: InputFrame y proyección servidor | Fuera de la escena inicial; pendiente |
| scripts/autoloads/DataManager.gd | REPLACE autoridad de guardado | Carga/guardado de partida bloqueados en modo MMO |
| scripts/SaveFileManager.gd | REPLACE persistencia local de GameState | Bloqueado incluso si se llama directamente en modo MMO |
| scripts/user_prefs.gd | KEEP: audio/idioma local | Conservado |
| scripts/autoloads/debugger.gd | ADAPT: no activar cheats single-player | Atajos desactivados en modo MMO |
| inventario, daño, enemigos, loot y niveles playground | ADAPT/REPLACE autoridad | Ejemplos preservados; no instanciados por el arranque MMO |
| project.godot | ADAPT: client/ y nueva escena inicial | Preparado |
| .github/workflows/release.yml | Ajustar rutas al mover proyecto | Rutas actualizadas; no ejecutado ni publicado |

`mmo/server_authoritative=true` separa la preparación MMO de las funciones demo. Es configuración local de presentación, no un mecanismo de seguridad servidor. Cuando exista servidor, toda intención deberá validarse allí independientemente de este flag.

## Deuda observada

- Godot .NET 4.7.1 informa al cerrar dos objetos y un recurso GDScript retenidos; el log verbose identifica `scripts/state_machine/states/state.gd`. Se observa tanto al importar como al ejecutar la escena. No se modificó el patrón de estados para ocultar el aviso. **TECHNICAL DEBT**.
- El menú de preferencias mantiene idiomas en/it del template. Localización completa del MMO: **FUTURE FEATURE**.
- La demo conserva reglas single-player fuera de la escena inicial. Su existencia no significa adaptación MMO terminada. **TECHNICAL DEBT**.
