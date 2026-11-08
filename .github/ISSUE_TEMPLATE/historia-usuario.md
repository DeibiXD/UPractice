---
name: Historia de usuario
about: Funcionalidad vista desde un usuario (HU-xx)
title: "HU-XX · "
labels: historia
---

**Como** <rol> **quiero** <acción> **para** <beneficio>.

### Criterios de aceptación
- [ ] Dado … cuando … entonces …
- [ ] …

### Contrato
- Endpoint(s): `MÉTODO /api/v1/...`
- DTO: `...Request` / `...Dto`

### Notas técnicas
- Tabla(s):
- Regla de tenant:

### Definition of Done
- [ ] Cumple todos los criterios de aceptación
- [ ] Validador FluentValidation + esquema Zod simétrico
- [ ] Prueba de aislamiento entre empresas (si toca datos de empresa)
- [ ] Swagger actualizado
- [ ] PR aprobado, fusionado y desplegado