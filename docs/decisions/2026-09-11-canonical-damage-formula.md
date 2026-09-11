# Decisión canónica — fórmula de daño

Fecha: 2026-09-11
Estado: canónico / vigente

## Fórmula madre

El daño ofensivo sigue una estructura inspirada en la fórmula clásica de Dofus:

```text
DañoOfensivo = floor(DB × (100 + SA + P) / 100) + DF
```

- `DB`: daño base de la acción.
- `SA`: característica efectiva aportada por los atributos de escalado de esa acción.
- `P`: Potencia y modificadores equivalentes. El dominio/maestría de arma, cuando aplique, se suma aquí y no crea una fórmula aparte.
- `DF`: daños fijos aplicables a la acción. El sistema podrá resolver daño fijo general + daño fijo elemental antes de entrar al pipeline.

Potencia NO multiplica el daño final. Funciona como puntos adicionales de característica exclusivamente para la etapa que escala el daño base.

## Orden defensivo y modificadores

Después del daño ofensivo:

```text
TrasReducción = max(0, DañoOfensivo - ReducciónFija)
TrasResistencia = floor(TrasReducción × (100 - Resistencia%) / 100)
TrasCrítico = floor(TrasResistencia × MultiplicadorCrítico)
DañoFinal = floor(TrasCrítico × MultiplicadorFinal)
```

Las resistencias negativas funcionan naturalmente como vulnerabilidades. El cap positivo PvE permanece centralizado en `CanonicalDamageRules` y actualmente es 80%.

`MultiplicadorFinal` queda reservado para modificadores situacionales explícitos como daño contra una familia, distancia, caída de daño de AoE, estados o reglas futuras. No deben mezclarse con Potencia.

## Elemento y atributo de escalado

Son conceptos independientes. La asociación canónica común es:

- Tierra → STR
- Fuego → INT
- Aire → AGI
- Agua → SPI
- Neutral → el atributo que declare la acción; no se fuerza automáticamente a STR.

Una técnica puede contener varias acciones y cada acción puede usar elemento, atributo y coeficiente distintos.

## Defensa

Se elimina Hard DEF / Soft DEF tipo Ragnarok del pipeline canónico. VIT, equipo, buffs y efectos pueden producir defensa, pero esa defensa debe resolverse a estadísticas explícitas como reducción fija, resistencia porcentual u otro modificador definido, no a una segunda fórmula defensiva escondida.

## Ejemplo de regresión

Con DB 6–50, característica 566, Potencia 104 y +34 daños fijos:

```text
SA + P = 670
6  × 7.70 + 34 = 80 (tras truncado por etapa)
50 × 7.70 + 34 = 419
```

El test automatizado debe conservar el resultado `80–419`.

## Curas

Las curas usarán la misma filosofía aditiva, pero su atributo de escalado será declarado por la acción de curación y no se fuerza globalmente a INT. La fórmula detallada de curas se cerrará como decisión separada para permitir Tradiciones que escalen, por ejemplo, con SPI.
