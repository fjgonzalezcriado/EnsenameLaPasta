# Tests Automáticos por Tipo de Endpoint

> Patrones de tests Postman para cada verbo HTTP.
> Incluye: GET lista, GET por ID, POST, PUT, DELETE, validación de errores.

---

## GET (Listar)

```javascript
pm.test("Status code is 200", function () {
    pm.response.to.have.status(200);
});

pm.test("Response is array or has items property", function () {
    const jsonData = pm.response.json();
    pm.expect(jsonData).to.be.an('array').or.to.have.property('items');
});

pm.test("Response time is less than 500ms", function () {
    pm.expect(pm.response.responseTime).to.be.below(500);
});
```

---

## GET (Por ID)

```javascript
pm.test("Status code is 200 or 404", function () {
    pm.expect(pm.response.code).to.be.oneOf([200, 404]);
});

pm.test("Response has id property", function () {
    if (pm.response.code === 200) {
        const jsonData = pm.response.json();
        pm.expect(jsonData).to.have.property('id');
    }
});
```

---

## POST (Crear)

```javascript
pm.test("Status code is 201 Created", function () {
    pm.response.to.have.status(201);
});

pm.test("Response has Location header", function () {
    pm.response.to.have.header("Location");
});

pm.test("Response contains created entity", function () {
    const jsonData = pm.response.json();
    pm.expect(jsonData).to.have.property('id');
});

// Guardar ID para tests posteriores
if (pm.response.code === 201) {
    pm.environment.set("lastCreatedId", pm.response.json().id);
}
```

---

## PUT (Actualizar)

```javascript
pm.test("Status code is 200 or 204", function () {
    pm.expect(pm.response.code).to.be.oneOf([200, 204]);
});
```

---

## DELETE (Eliminar)

```javascript
pm.test("Status code is 204 No Content", function () {
    pm.response.to.have.status(204);
});
```

---

## Validación de Errores

```javascript
pm.test("Error response has correct format", function () {
    if (pm.response.code >= 400) {
        const jsonData = pm.response.json();
        pm.expect(jsonData).to.have.property('title');
        pm.expect(jsonData).to.have.property('status');
    }
});
```
