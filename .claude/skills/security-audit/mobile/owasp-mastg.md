# Seguridad Movil - OWASP MASTG

> Skill: security-audit | Version: 3.5.0

Verificaciones de seguridad para aplicaciones moviles basadas en OWASP Mobile Application Security Testing Guide.

> Ver tambien: `owasp/owasp-top10-2025.md`, `patterns/secure-coding-checklist.md`

---

## C# / .NET MAUI

```csharp
// SEGURO - Almacenamiento seguro en MAUI
await SecureStorage.Default.SetAsync("auth_token", token);
var token = await SecureStorage.Default.GetAsync("auth_token");

// INSEGURO - Preferences (no cifrado)
// Preferences.Default.Set("auth_token", token); // NO usar para datos sensibles

// SEGURO - Validar certificado SSL en HttpClient
var handler = new HttpClientHandler
{
    ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
    {
        // Validar contra CA conocida
        return errors == System.Net.Security.SslPolicyErrors.None;
    }
};
```

---

## Android (Java/Kotlin)

### Verificaciones de Seguridad

```java
// INSEGURO - Aceptar todos los certificados SSL
TrustManager[] trustAllCerts = new TrustManager[] {
    new X509TrustManager() {
        public void checkServerTrusted(X509Certificate[] chain, String authType) {}
        // Vacio = acepta todo!
    }
};

// INSEGURO - WebView ignorando errores SSL
webView.setWebViewClient(new WebViewClient() {
    public void onReceivedSslError(WebView view, SslErrorHandler handler, SslError error) {
        handler.proceed();  // Nunca hacer esto en produccion!
    }
});

// INSEGURO - Inyeccion SQL local
String sql = "SELECT * FROM users WHERE username = '" + username + "'";
Cursor c = db.rawQuery(sql, null);

// SEGURO - Consulta parametrizada
String sql = "SELECT * FROM users WHERE username = ?";
Cursor c = db.rawQuery(sql, new String[]{username});
```

### Bastionado de WebView

```java
webView.getSettings().setAllowFileAccess(false);
webView.getSettings().setAllowFileAccessFromFileURLs(false);
webView.getSettings().setAllowUniversalAccessFromFileURLs(false);
webView.getSettings().setAllowContentAccess(false);
```

### Configuracion de Seguridad de Red (Android)

```xml
<!-- INSEGURO - confiar en CAs del usuario -->
<network-security-config>
    <domain-config>
        <domain includeSubdomains="false">ejemplo.org</domain>
        <trust-anchors>
            <certificates src="system" />
            <certificates src="user" />  <!-- ELIMINAR ESTO -->
        </trust-anchors>
    </domain-config>
</network-security-config>
```

---

## iOS

```xml
<!-- INSEGURO - Deshabilitar ATS (App Transport Security) -->
<key>NSAppTransportSecurity</key>
<dict>
    <key>NSAllowsArbitraryLoads</key>
    <true/>  <!-- Debe ser false en produccion -->
</dict>
```

---

## Tecnicas de Pruebas Moviles

```
- Hooking de APIs (Frida) para HttpUrlConnection, NSURLSession
- Hooking de funciones TLS (SSL_read, SSL_write)
- Intercepcion mediante proxy (Burp Suite, ZAP, mitmproxy)
- Captura de trafico de red (tcpdump, Wireshark)
- MITM via ARP spoofing (bettercap)
```

---

## Checklist MASTG

### Almacenamiento de Datos
- [ ] Datos sensibles en almacenamiento seguro (Keychain/Keystore/SecureStorage)
- [ ] Sin datos sensibles en logs
- [ ] Sin datos sensibles en backups no cifrados
- [ ] Clipboard limpiado tras copiar datos sensibles

### Red
- [ ] TLS 1.2+ obligatorio
- [ ] Certificate pinning implementado
- [ ] Sin aceptacion de certificados invalidos
- [ ] ATS habilitado (iOS)

### Autenticacion
- [ ] Biometria como segundo factor (no unico)
- [ ] Tokens con expiracion corta
- [ ] Sesion invalidada en servidor al cerrar sesion

### Codigo
- [ ] Ofuscacion del codigo habilitada
- [ ] Deteccion de root/jailbreak
- [ ] Sin informacion de debug en release
- [ ] WebView bastionado (sin acceso a archivos)

---

*Pattern v3.7.0*
