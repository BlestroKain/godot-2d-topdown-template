# Contrato visual para PixelLab

Estado: pendiente de decisiones artísticas  
Dependencia: `docs/game-vision.md`

## Propósito

Este documento será la referencia obligatoria para cada generación de PixelLab. Su función es impedir que personajes, mapas, objetos y UI parezcan pertenecer a juegos diferentes.

## Especificaciones confirmadas

- Pixel art para juego 2D top-down 3/4.
- Tile base de 32 × 32 píxeles.
- Ítems de inventario de 32 × 32 píxeles.
- Objetos alineados a múltiplos de 32 píxeles cuando formen parte del mapa.
- Fondo transparente para sprites, objetos sueltos e ítems.
- El juego debe poder leerse con claridad en pantallas pequeñas y Steam Deck.
- Las siluetas y estados interactivos deben distinguirse sin depender únicamente del color.

## Especificaciones por aprobar

- Resolución del personaje: 32 × 48 o 48 × 64.
- Número de direcciones: cuatro u ocho.
- Estilo de contorno.
- Paleta maestra y paletas por región/estación.
- Saturación, contraste y temperatura.
- Dirección de la luz y fuerza de las sombras.
- Proporciones humanas y grado de caricatura.
- Densidad de textura en terreno y edificios.
- Influencia arquitectónica y materiales principales.
- Forma visual de la magia y la tecnología.

## Reglas de producción

1. Aprobar primero una pequeña muestra de estilo.
2. Reutilizar imágenes aprobadas como referencias de estilo cuando la herramienta lo permita.
3. Mantener tamaño, perspectiva, dirección de luz y contorno en cada familia de recursos.
4. Generar una familia completa solo después de aceptar su pieza piloto.
5. Conservar IDs de trabajos, semillas, prompts y decisiones de selección.
6. Revisar transparencia, cuadrícula, bordes, pivote y legibilidad antes de importar en Godot.
7. No pagar generaciones de animación costosa sin presentar antes su coste y recibir aprobación.

## Ficha obligatoria de cada recurso

- Nombre interno.
- Categoría.
- Función jugable.
- Dimensiones.
- Direcciones necesarias.
- Animaciones necesarias.
- Región o contexto.
- Referencias visuales aprobadas.
- Paleta.
- Prompt utilizado.
- Semilla e ID de PixelLab.
- Estado: prueba, aprobado o descartado.
- Ruta final dentro del proyecto Godot.

## Primera prueba visual

La primera prueba no busca producir contenido definitivo. Debe resolver coherencia de escala, perspectiva, color y detalle mediante seis recursos:

- Protagonista base.
- NPC de oficio.
- Enemigo del bosque.
- Terreno con transición.
- Edificio pequeño.
- Tres ítems relacionados con una misma actividad.

No se producirán animaciones completas hasta aprobar los sprites estáticos y la escala dentro de una escena de Godot.
