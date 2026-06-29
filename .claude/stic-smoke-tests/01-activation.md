# Smoke 01 · Activacion de skills

> Verifica que ante prompts tipicos del proyecto, **Claude Code activa la skill
> correcta**. Sin esto, todo lo demas da igual: si el agente ignora la skill,
> ni las convenciones ni la compilacion sirven de nada.

---

## Pre-requisitos

- `claude --debug` funciona en esta maquina.
- `arranque.ps1 update` ejecutado en la ultima semana (skills sincronizadas).
- Al menos **una** solucion compila en este repo (`dotnet build` o `msbuild` OK).

---

## Los tests

**Personaliza** los `{placeholders}` segun este proyecto. Los ejemplos usan
entidades reales de cada dominio; sustitulelos por los tuyos.

### Test 1.1 · CRUD basico

**Prompt**:

> Necesito un CRUD completo para `{ENTITY}` en el modulo de {MODULO}.

**Skill esperada**: `generador-crud`

**Ejemplo sustituido** (GestorLicencias):

> Necesito un CRUD completo para `SolicitudLicencia` en el modulo de licencias.

**Ejemplo sustituido** (Comillas.EuPeace):

> Necesito un CRUD completo para `Nominacion` en el modulo de intercambios EWP.

---

### Test 1.2 · Consulta puntual (NO debe activar CRUD)

**Prompt**:

> Quiero un endpoint que devuelva un listado de `{ENTITY}` activos, solo lectura.

**Skill esperada**: **NO** `generador-crud`. Lo correcto es activar una skill
de query-scaffolding o generar una Razor Page simple sin maquinaria CRUD.

Este test es critico: una skill que sabe decir «esto no es asunto mio» vale mas
que una que fuerza su workflow.

---

### Test 1.3 · Revision arquitectura (aggregate root)

**Prompt**:

> Revisa si `{AGGREGATE}` esta bien modelado como Aggregate Root.

**Skill esperada**: `analisis-arquitectura`

**Ejemplo sustituido** (GestorLicencias):

> Revisa si `ContratoLicencia` esta bien modelado como Aggregate Root.

---

### Test 1.4 · Auditoria seguridad

**Prompt**:

> Audita la seguridad de `{CONTROLLER}` centrandote en SQL Injection y headers.

**Skill esperada**: `security-audit`

---

### Test 1.5 · Sincronizacion Azure DevOps

**Prompt**:

> Configura los Area Paths de este proyecto en Azure DevOps y arregla el
> Kanban que esta vacio.

**Skill esperada**: `azure-devops-sync` (NUEVO v3.8.4).

> Si esta skill NO se activa tras el update, `arranque.ps1` no descargo bien
> la v3.8.4 o el motor no tiene la skill registrada. Re-ejecutar
> `arranque.ps1 update`.

---

### Test 1.6 · NO activar con mencion de Jira

**Prompt**:

> Crea un issue en Jira y vinculalo a este repo.

**Skill esperada**: **NO** `azure-devops-sync`. Debe activar la skill de Jira
(si existe) o responder sin skills cuando Jira no esta configurado.

---

## Como ejecutarlo

### Opcion A · Manual

Por cada test, lanzar Claude en una terminal con `--debug`:

```powershell
claude --debug -p "Necesito un CRUD completo para SolicitudLicencia" 2>&1 |
  Select-String -Pattern "loading skill:|activating skill:|trigger skill:"
```

Si aparece `loading skill: generador-crud` → **PASS**.
Si aparece cualquier otra skill o ninguna → **FAIL**.

### Opcion B · Script (semi-automatico)

Guarda los 6 prompts en un fichero `prompts.txt` y procesalos en bucle:

```powershell
$tests = @(
    @{ prompt = "Necesito un CRUD completo para SolicitudLicencia";        expected = "generador-crud" },
    @{ prompt = "Quiero un endpoint que devuelva licencias activas";       expected = "NONE" },
    @{ prompt = "Revisa si ContratoLicencia es un Aggregate Root";         expected = "analisis-arquitectura" },
    @{ prompt = "Audita SolicitudController contra SQL Injection";         expected = "security-audit" },
    @{ prompt = "Configura Area Paths y arregla el Kanban vacio";          expected = "azure-devops-sync" },
    @{ prompt = "Crea un issue en Jira";                                   expected = "NONE" }
)

$pass = 0
foreach ($t in $tests) {
    $log = claude --debug -p $t.prompt 2>&1
    $match = ($log | Select-String "loading skill: $($t.expected)").Count
    if ($t.expected -eq "NONE") {
        $noSkillMsg = $log | Select-String "loading skill:"
        if (-not $noSkillMsg) { Write-Host "PASS (no skill): $($t.prompt)"; $pass++ }
        else { Write-Host "FAIL (activo skill inesperada): $($t.prompt)" }
    } else {
        if ($match -gt 0) { Write-Host "PASS: $($t.prompt) -> $($t.expected)"; $pass++ }
        else { Write-Host "FAIL: $($t.prompt) (esperado $($t.expected))" }
    }
}
Write-Host ""
Write-Host "Resultado: $pass / $($tests.Count)"
if ($pass -lt $tests.Count) { exit 1 }
```

---

## Criterios de aceptacion

- **PASS** si 5/6 o mas tests pasan.
- **FAIL** si 4/6 o menos pasan → abrir Work Item «Eval gap» en `ClaudeCodeSTIC`.

---

## Troubleshooting

| Sintoma | Causa probable | Accion |
|---|---|---|
| Ninguna skill se activa | `arranque.ps1 update` no copio las skills | Re-ejecutar update |
| Siempre activa la misma skill | Description sobre-pesca | Abrir issue «Eval gap» con los 2 prompts conflictivos |
| `azure-devops-sync` no activa (test 1.5) | Version del ecosistema anterior a v3.8.4 | Actualizar ecosistema |
| `generador-crud` activa en test 1.2 | Description captura consultas | Documentar negativo, abrir Work Item |
