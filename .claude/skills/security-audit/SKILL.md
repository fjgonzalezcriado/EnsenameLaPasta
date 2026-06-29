---
name: security-audit
description: >
  Performs comprehensive code security audits following OWASP Top 10 2025,
  SAST analysis, CVSS scoring, CWE classification, and secret detection.
  Covers .NET, Java, Python, PHP, Ruby, Go, JavaScript, mobile (OWASP MASTG),
  and LLM/AI security patterns.
  USE FOR: security audits, OWASP review, vulnerability scanning, secrets detection,
  SAST analysis, authentication flow review, CORS policy analysis, security headers,
  auditar seguridad, revisar vulnerabilidades, escanear secretos.
  DO NOT USE FOR: generating code or implementing fixes (use generador-crud),
  architecture analysis (use analisis-arquitectura),
  Azure AD deep audit (use identity-auditor agent).
allowed-tools: Read, Grep, Glob
context: fork
agent: general-purpose
---

# Auditoria de Seguridad - Comillas

> Basado en OWASP Top 10 (2025), OWASP WSTG, OWASP MASTG, CWE/NVD y NIST.
> Documento fuente: Oficina de Infraestructuras TI y Seguridad Digital.

---

## Cuando Usar Este Skill

1. **Revision de codigo** - Detectar vulnerabilidades y secrets expuestos
2. **Compliance** - Verificar OWASP Top 10 2025
3. **Hardening** - Configurar headers de seguridad y CSP
4. **Migracion de secrets** - Mover secretos a Key Vault
5. **Auditoria de APIs** - Verificar autenticacion, autorizacion, validacion
6. **Seguridad movil** - Verificar apps Android/iOS/MAUI
7. **Seguridad IA/LLM** - Proteger integraciones con modelos de lenguaje
8. **Escaneo SAST** - Integrar herramientas de analisis estatico en CI/CD
9. **Post-remediacion** - Verificar correcciones aplicadas
10. **Informe ejecutivo** - Generar reporte con clasificacion CVSS y semaforos

---

## Activacion del Skill

### Por Contexto (Automatico)

Claude activa este skill automaticamente cuando detecta:
- Usuario menciona "revisa la seguridad" o "auditoria de seguridad"
- Se solicita "buscar secrets expuestos" o "detectar credenciales"
- Se menciona "OWASP Top 10" o vulnerabilidades web
- Se pide configurar "Azure Key Vault" para secretos
- Se trabaja con `appsettings.json`, `Web.config`, o codigo con posibles secrets
- Se menciona "XSS", "SQL injection", "CSRF", "SSRF" u otro ataque
- Se pide "escanear dependencias" o "analizar seguridad del pipeline"
- Se trabaja con integraciones LLM/IA y se pregunta por inyeccion de prompt
- Se solicita informe de seguridad o plan de mitigacion
- Se pregunta por seguridad movil, certificate pinning o MASTG

---

## Metodologia de Auditoria

### Fase 1: Reconocimiento y Alcance
1. Identificar lenguajes, frameworks y tecnologias
2. Mapear la arquitectura (cliente-servidor, microservicios, movil)
3. Identificar limites de confianza y puntos de entrada
4. Determinar superficie de ataque → `patterns/attack-surface-mapping.md`

### Fase 2: Analisis Estatico (SAST)
1. Escanear patrones de vulnerabilidad OWASP Top 10 → `owasp/owasp-top10-2025.md`
2. Detectar secretos hardcodeados → `patterns/secret-detection.md`
3. Verificar dependencias vulnerables → `tools/sast-integration.md`
4. Revisar configuracion de headers → `patterns/secure-headers.md`

### Fase 3: Evaluacion de Vulnerabilidades
1. Clasificar hallazgos por severidad CVSS → `reports/report-format.md`
2. Mapear hallazgos a CWE → `reports/cwe-references.md`
3. Evaluar explotabilidad → `templates/AttackSimulation.md`
4. Verificar con Context7 → `tools/context7-security.md`

### Fase 4: Remediacion
1. Generar plan de mitigacion priorizado → `reports/mitigation-plan.md`
2. Aplicar correcciones con ejemplos de codigo seguro
3. Verificar post-remediacion → `checklists/post-remediation.md`

---

## Recursos del Skill

### owasp/ - OWASP Top 10 y Frameworks

| Archivo | Descripcion |
|---------|-------------|
| `owasp/owasp-top10-2025.md` | OWASP Top 10 2025 completo, multi-lenguaje (C#, Java, Python, PHP, Go, JS) |
| `owasp/owasp-dotnet.md` | OWASP Top 10 especifico para .NET 10 con ejemplos C# |
| `owasp/owasp-frameworks.md` | Seguridad especifica por framework (Django, Express, Laravel, Rails, Spring) |

### attacks/ - Ataques en Profundidad

| Archivo | Descripcion |
|---------|-------------|
| `attacks/xss-in-depth.md` | XSS: 3 tipos, 20+ vectores, prevencion por framework |
| `attacks/xxe-attacks.md` | XXE: payloads, prevencion en 5 parsers Java + C# |
| `attacks/jwt-security.md` | JWT: alg:none, confusion RS256/HS256, validacion segura |
| `attacks/ssrf-attacks.md` | SSRF: metadatos cloud, red interna, whitelist de dominios |
| `attacks/csrf-attacks.md` | CSRF: anti-forgery tokens, SameSite cookies, por framework |
| `attacks/path-traversal.md` | Path Traversal: variantes de codificacion, Path.GetFileName |
| `attacks/mass-assignment.md` | Mass Assignment: DTOs explicitos, $fillable, Strong Parameters |
| `attacks/clickjacking.md` | Clickjacking: X-Frame-Options, CSP frame-ancestors |

### patterns/ - Patrones de Seguridad

| Archivo | Descripcion |
|---------|-------------|
| `patterns/authentication-patterns.md` | Patrones de autenticacion Azure AD / Identity |
| `patterns/secure-headers.md` | Headers HTTP obligatorios + CSP + Cross-Origin |
| `patterns/secret-detection.md` | Regex de deteccion, resumen de exposicion, gestion de secretos |
| `patterns/secure-coding-checklist.md` | 40 verificaciones en 9 categorias (OWASP Secure Coding) |
| `patterns/attack-surface-mapping.md` | Plantilla de mapeo de endpoints, datos y pipeline |

### checklists/ - Checklists de Verificacion

| Archivo | Descripcion |
|---------|-------------|
| `checklists/pre-deployment.md` | Checklist pre-despliegue |
| `checklists/code-review-security.md` | Checklist de revision de codigo |
| `checklists/api-security.md` | Checklist de seguridad de APIs |
| `checklists/wstg-checklist.md` | OWASP WSTG: 9 categorias de pruebas de seguridad web |
| `checklists/http-methods-security.md` | Verificacion de metodos HTTP permitidos |
| `checklists/post-remediation.md` | 11 verificaciones post-correccion |

### templates/ - Plantillas

| Archivo | Descripcion |
|---------|-------------|
| `templates/SecurityHeaders.cs.template` | Middleware de headers de seguridad (.NET 10) |
| `templates/SecureStartup.cs.template` | Program.cs con configuracion segura (.NET 10) |
| `templates/AttackSimulation.md` | Plantilla de simulacion de explotabilidad y matriz de riesgo |

### reports/ - Informes

| Archivo | Descripcion |
|---------|-------------|
| `reports/report-format.md` | Formato de informe con CVSS v3.1 y sistema de semaforos |
| `reports/cwe-references.md` | 21 CWEs comunes con mapeo a OWASP Top 10 |
| `reports/mitigation-plan.md` | Plan de mitigacion en 4 horizontes temporales |

### mobile/ - Seguridad Movil

| Archivo | Descripcion |
|---------|-------------|
| `mobile/owasp-mastg.md` | OWASP MASTG: Android, iOS, .NET MAUI, Frida |

### ai-security/ - Seguridad IA

| Archivo | Descripcion |
|---------|-------------|
| `ai-security/llm-security.md` | 7 vectores de ataque LLM, pipeline de defensa en 3 capas |

### tools/ - Herramientas

| Archivo | Descripcion |
|---------|-------------|
| `tools/sast-integration.md` | Semgrep, Bandit, GoSec, SecurityCodeScan, CI/CD |
| `tools/context7-security.md` | Verificacion de hallazgos con MCP Context7 |

---

## Metricas del Skill

| Metrica | Valor |
|---------|-------|
| Archivos totales | 33 |
| Subdirectorios | 9 |
| OWASP version | 2025 |
| Ataques documentados | 8 |
| CWEs referenciados | 21 |
| Lenguajes con ejemplos | 7 (C#, Java, Python, PHP, Ruby, Go, JavaScript) |
| Frameworks cubiertos | 7 (.NET, Django, Express, Laravel, Rails, Spring, React) |

---

*Skill security-audit v3.7.0*
