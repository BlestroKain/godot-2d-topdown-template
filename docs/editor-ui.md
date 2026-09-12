# NuevoMMO Editor — referencia UX/UI

La referencia funcional y visual del editor de mapas es un editor MMO clásico de escritorio: interfaz densa, mapa como área de trabajo principal, herramientas agrupadas y paneles laterales acoplables.

## Distribución canónica

- Izquierda: herramientas del mapa agrupadas por `Tiles`, `Atributos`, `Luces`, `Eventos` y `Entidades`.
- Debajo de las herramientas: paleta de tilesets, selector de tileset, modo de autotile y zoom.
- Centro: documento de mapa, ocupando la mayor superficie disponible.
- Derecha superior: árbol de mundo/mapas.
- Derecha inferior: propiedades del objeto o mapa seleccionado.
- Explorador general de contenido y problemas: paneles secundarios/autohide, no deben competir permanentemente con el viewport.

## Flujo de trabajo

- Tile seleccionado → pintar directamente sobre el mapa.
- Herramienta de atributo → editar colisiones, portales, regiones y zonas.
- Luz → colocar/editar luces.
- Evento → colocar/editar eventos.
- Entidad → colocar Mob, NPC o Resource a partir de una definición.
- Selección de mapa en el árbol → propiedades; doble clic/Enter → abrir mapa.
- Clic derecho sobre un mapa → abrir, propiedades y crear mapas vecinos.
- Los editores grandes de Items, Mobs, NPCs, Técnicas, Tradiciones, Profesiones, Recetas, Loot, Spawn, Quests, Eventos, Dungeons y Tilesets viven en el menú `Editores` y abren documentos dedicados.

## Estética

- Paleta gris carbón, superficies oscuras y acento cian.
- Controles compactos y poco padding.
- El color de acento se reserva para selección/estado activo.
- El viewport debe destacar sobre el chrome de la aplicación.
- Se prioriza legibilidad y densidad de herramienta de producción por encima de una UI decorativa de videojuego.

## Restricciones de arquitectura

La interfaz no cambia la autoridad ni el modelo del motor. El editor sigue trabajando sobre `Core.Definitions`, `EditorApplication`, `MapWorldGrid` y los servicios actuales. La topología en coordenadas y conexiones de mapas permanece en el modelo aunque la navegación principal se presente como árbol.

Las futuras carpetas/regiones jerárquicas del árbol deberán tener metadata explícita en el modelo; no se inferirán ni se inventarán regiones a partir de coordenadas.
