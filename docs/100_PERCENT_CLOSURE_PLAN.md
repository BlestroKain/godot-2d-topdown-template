# Plan de cierre al 100% — NuevoMMO

**Rama base:** `devMMO`

**Rama de adopción Godot-first:** `feature/godot-first-systems`

**Repositorio:** `BlestroKain/godot-2d-topdown-template`  
**Regla:** `main` no se modifica ni se mezcla sin autorización explícita.  
**Canon:** `docs/design.md` + `docs/sources/PROYECTO_MMO_NUEVO_REGLAS_ACTUALIZADAS.md`; *newest canon wins*.

## Qué significa 100%

`100%` no significa que todo el contenido final del MMO esté producido. Significa que **toda mecánica y todo sistema ya definido por el canon y el alcance técnico aprobado tienen una implementación funcional, autoritativa, persistente cuando corresponde, integrada cliente-servidor-editor, verificable y sin stubs silenciosos**.

Un punto que siga marcado como `PENDING DESIGN` no se inventa para inflar el porcentaje. Permanece bloqueado por diseño hasta una decisión explícita.

Un gate solo pasa a `DONE` cuando:

1. La autoridad correcta está en Server/Domain; el cliente envía intención, no resultados.
2. El flujo funciona de punta a punta y no solo como clase aislada.
3. La persistencia sobrevive reconexión/reinicio donde corresponda.
4. Hay pruebas automáticas de invariantes y regresiones críticas.
5. CI compila solución completa y ejecuta la suite en verde.
6. No hay fixtures de desarrollo en la ruta de producción requerida por ese gate.
7. Errores esperables no tumban una sesión completa y los logs permiten diagnosticar la causa sin exponer secretos.
8. La documentación de estado se actualiza con evidencia verificable.

## Línea base verificada

En el inicio de este plan, `devMMO` estaba en `6a361a7ba3e3554895e2737a8dccb33edcb1adc4` y CI estaba verde con **160 comprobaciones**. Ya existen y tienen cobertura, entre otros: movimiento autoritativo/predicción/reconciliación, AOI, multi-map, colisiones semánticas, LOS, proyectiles, pipeline de daño, técnicas, AI básica de mobs, XP/progresión, EventRuntime inicial, inventario/equipo runtime, SQLite, creación/carga de personaje y editor en evolución.

## Gates de cierre

| Gate | Área | Estado inicial | Criterio de cierre |
|---|---|---:|---|
| G0 | Baseline, canon, CI, observabilidad | IN PROGRESS | CI reproducible, logs estructurados, tablero de cierre y ningún split arquitectónico accidental |
| G1 | Cuenta, sesión, personaje, selección, carga, reconexión | IN PROGRESS | ciclo completo con validación recuperable, canon respetado y persistencia real |
| G2 | Inventario, equipo, stats, durabilidad y persistencia | IN PROGRESS | pickup/equip/unequip/stats/save/load en memoria, SQLite y PostgreSQL |
| G3 | Combate, técnicas, efectos, muerte, mobs, AI, loot y XP | IN PROGRESS | loop PvE real sin depender de DevelopmentAttack/fixtures |
| G4 | Interacción universal, EventRuntime/EventExecutor, diálogo y quests | IN PROGRESS | eventos ejecutan comandos propietarios y quests observan/solicitan cambios sin mutar sistemas ajenos |
| G5 | Editor MMO | IN PROGRESS | mapas, layers, tiles/autotiles, colisiones, eventos, NPC/mobs/resources, definitions, validación y test-play |
| G6 | Profesiones, gathering, crafting, agricultura | NOT STARTED/FOUNDATION | loops server-side persistentes según bloques XVIII–XXI |
| G7 | Economía, trade, mercado y logística | NOT STARTED/FOUNDATION | ownership/location/custody, wallet, órdenes/escrow/fills atómicos y auditoría |
| G8 | Social: party, friends, chat, mail, guild, faction, reputation | PARTIAL | sistemas separados, permisos, persistencia y proyección cliente |
| G9 | PvP, territorio, guerra, asedio, crimen, ley, dungeons | NOT STARTED/FOUNDATION | reglas contextuales, autoridad server-side y estados persistentes donde aplique |
| G10 | Mundo dinámico, conocimiento, housing, achievements/cosmetics | NOT STARTED/FOUNDATION | scheduler + systems propietarios + persistencia/proyección adecuada |
| G11 | Producción y escala | IN PROGRESS | PostgreSQL completo, seguridad, métricas/audit, pruebas de red/carga, backup/deploy, móvil/controller |
| G12 | Release gate | BLOCKED | todos los gates requeridos DONE, CI verde, smoke multicliente y cero blocker P0 |

## Orden de ejecución

El orden es por dependencias, no por comodidad:

1. **Cerrar G0–G2:** observabilidad, errores recuperables, persistencia PostgreSQL completa de personaje/inventario/equipo y flujo de personaje canónico.
2. **Cerrar G3:** reemplazar cualquier ruta de desarrollo restante por combate/mobs/loot/death de producción.
3. **Cerrar G4:** unificar interacciones mediante runtime de eventos/comandos y conectar diálogo/quest.
4. **Cerrar G5:** llevar el editor a paridad funcional con lo que necesita el runtime, sin heredar las limitaciones de Intersect.
5. **Cerrar G6–G10:** construir los sistemas MMO superiores en capas sobre ownership, permissions, transactions, scheduler y replication comunes.
6. **Cerrar G11:** hardening, seguridad, concurrencia, observabilidad, pruebas de red/carga y deployment.
7. **G12:** auditoría final, smoke de release y actualización del porcentaje a 100% solo con evidencia.

## Primer bloque de ejecución

### Cierre 01 — persistencia y observabilidad

- [x] Detectado: `PostgresCharacterRepository.SaveInventoryAsync` era un no-op.
- [ ] Añadir `game.characters.inventory_data` mediante migración idempotente.
- [ ] Persistir inventario/equipo al crear, guardar y cargar personaje PostgreSQL.
- [ ] Sustituir logging plano por registros estructurados UTC con redacción de secretos.
- [ ] Añadir regresiones de redacción/formato de logs.
- [ ] Confirmar build + suite completa en CI.

### Cierre 02 — personaje y sesión

- [ ] Hacer que errores de creación/selección esperables sean respuestas no fatales y no desconecten al cliente.
- [ ] Alinear creación con el canon vigente: el personaje nace **Novicio, sin Tradición**; Tradición se aprende diegéticamente.
- [ ] Mantener apariencia server-authoritative y habilitar piezas solo cuando tengan assets publicados.
- [ ] Añadir pruebas create → list → select → load → disconnect → reconnect.
- [ ] Loguear request/reject/success/select/load/join/leave sin tokens/passwords.

### Cierre 03 — vertical slice técnico al 100%

- [ ] Pickup → inventory → equip → stat recalculation → save → reconnect → restore.
- [ ] Combate real → muerte mob → loot/XP → pickup → equip.
- [ ] Muerte/respawn de jugador según canon vigente, sin pérdida permanente de progreso.
- [ ] Smoke de dos clientes y transición multi-map con inventario/equipo intactos.

## Política de progreso

El porcentaje se calcula por gates y criterios cerrados, no por cantidad de archivos ni líneas de código. Un sistema con UI pero sin autoridad/persistencia no cuenta como terminado. Un repositorio que compila pero pierde estado tampoco cuenta como terminado.

Cada lote debe dejar: **commit identificable + CI + criterio cerrado + siguiente blocker concreto**.

La implementación nueva aplica la auditoría `Godot-first, MMO-authoritative`: reutilizar
Godot/addons/fuentes externas antes de reconstruir capacidades genéricas, sin ceder al cliente
la autoridad de gameplay ni deformar los contratos ya cerrados.
