Optimizar contexto: analizar consumo, limpiar y compactar para sesiones mas eficientes

# /optimizar - Optimizacion de Contexto y Tokens

> Analiza el estado actual del contexto, identifica fuentes de desperdicio
> y ejecuta limpieza para maximizar la vida util de la sesion.

---

## REGLAS CRITICAS

- NO releer archivos que ya estan en contexto
- NO ejecutar herramientas para "demostrar" el problema — solo analizar y reportar
- Ser CONCISO en el reporte (el propio reporte no debe desperdiciar tokens)

---

## Paso 1: Diagnostico rapido

Analizar mentalmente (sin ejecutar herramientas) el estado de la sesion:

1. **Archivos leidos**: ¿cuantos archivos se han leido completos en esta sesion?
2. **Tool calls**: ¿cuantas llamadas a herramientas se han realizado?
3. **Tareas completadas**: ¿cuantas tareas distintas se han abordado?
4. **Contexto muerto**: ¿hay outputs de builds, tests o logs extensos ya procesados?
5. **Repeticiones**: ¿se ha releido algun archivo mas de una vez?

## Paso 2: Mostrar diagnostico

```
╔══════════════════════════════════════════════════════════════╗
║  🧹 DIAGNOSTICO DE CONTEXTO                                  ║
╠══════════════════════════════════════════════════════════════╣
║  Archivos leidos:     ~XX                                     ║
║  Tool calls:          ~XX                                     ║
║  Tareas completadas:  XX                                      ║
║  Estado:              🟢 Ligero / 🟡 Moderado / 🔴 Pesado    ║
╚══════════════════════════════════════════════════════════════╝
```

**Criterios de estado (ajustar segun modelo):**

Para **Opus 4.6 / Sonnet 4.6** (1M tokens):
- 🟢 **Ligero**: < 40 tool calls, < 10 archivos, 1-2 tareas → no necesita accion
- 🟡 **Moderado**: 40-80 tool calls, 10-25 archivos, 3+ tareas → recomendar /compact
- 🔴 **Pesado**: > 80 tool calls, > 25 archivos, 4+ tareas → urgente compactar

Para **Haiku 4.5** (200K tokens):
- 🟢 **Ligero**: < 15 tool calls, < 5 archivos, 1 tarea → no necesita accion
- 🟡 **Moderado**: 15-30 tool calls, 5-10 archivos, 2+ tareas → recomendar /compact
- 🔴 **Pesado**: > 30 tool calls, > 10 archivos, 3+ tareas → urgente compactar

> **Nota**: Sonnet 4.6 y Haiku 4.5 reciben tracking de tokens (`Token usage: X/Y`).
> Si el modelo tiene esta info, usarla para un diagnostico mas preciso.

## Paso 3: Identificar desperdicios

Listar las fuentes de desperdicio detectadas:

```
📊 FUENTES DE CONSUMO IDENTIFICADAS
─────────────────────────────────────
• [X tokens est.] Output de build/test completo (linea ~XXX)
• [X tokens est.] Archivo XXX.cs leido 2 veces
• [X tokens est.] git diff extenso no acotado
• [X tokens est.] Busqueda abierta con muchos resultados
```

## Paso 4: Recomendar accion

### Si estado es 🟢 Ligero:
```
✅ Contexto saludable. No es necesario compactar ahora.

💡 Tip: Para mantenerlo asi, usa subagentes para operaciones de busqueda
   extensas y limita los outputs con head_limit / --verbosity quiet.
```

### Si estado es 🟡 Moderado:
```
⚠️ Contexto moderado. Recomiendo compactar si vas a cambiar de tarea.

Ejecuta: /compact

O si quieres mantener foco en algo especifico:
  /compact focus on [descripcion de lo que quieres preservar]
```

### Si estado es 🔴 Pesado:
```
🔴 Contexto saturado. Compactacion recomendada AHORA.

Antes de compactar, este es el resumen a preservar:
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
• Evolutivo activo: [codigo o "ninguno"]
• Archivos modificados: [lista breve]
• Decisiones tomadas: [lista breve]
• Proximo paso: [descripcion]
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Ejecuta: /compact
```

## Paso 5: Tips de prevencion

Mostrar SIEMPRE al final:

```
💡 TIPS PARA SESIONES EFICIENTES
─────────────────────────────────
1. Usa subagentes (Agent) para busquedas abiertas y lectura masiva
2. Limita outputs: dotnet build -v quiet, git log --oneline -5
3. Lee solo secciones: Read con offset/limit para archivos grandes
4. Grep antes de Read: confirma que el archivo tiene lo que buscas
5. /compact entre tareas: no acumular contexto de tareas no relacionadas
6. /pausar guarda estado: al retomar con /continuar, el contexto esta limpio
```

---

## Parametros opcionales

| Parametro | Efecto |
|-----------|--------|
| `/optimizar` | Diagnostico completo + recomendacion |
| `/optimizar rapido` | Solo muestra estado (🟢/🟡/🔴) sin detalles |
| `/optimizar compact` | Diagnostico + ejecuta /compact automaticamente |

---

## Integracion con DURAN

Si hay un evolutivo activo (`_duran/ESTADO_PROYECTO.json → evolutivoActivo`), el resumen
de compactacion debe incluir el estado del evolutivo para que `/continuar` funcione correctamente
tras la compactacion.

Si hay `_duran/SESION_ACTUAL.md`, actualizarlo ANTES de compactar con:
- Archivos modificados en esta sesion
- Decisiones tomadas
- Proximo paso

Esto asegura que la informacion critica sobrevive la compactacion.

---

*Comando v3.8.2 - Optimizacion de contexto*
