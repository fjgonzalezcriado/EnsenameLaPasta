# Ataques XXE (XML External Entity)

> Skill: security-audit | Version: 3.5.0

Payloads XXE, prevencion en 5 parsers Java y C#.

> Ver tambien: `owasp/owasp-top10-2025.md` (A05), `reports/cwe-references.md` (CWE-611)

---

## Payloads de Ataque

```xml
<!-- Divulgacion de archivos -->
<?xml version="1.0"?>
<!DOCTYPE foo [
  <!ENTITY xxe SYSTEM "file:///etc/passwd">
]>
<foo>&xxe;</foo>

<!-- SSRF -->
<!ENTITY xxe SYSTEM "https://192.168.1.1/privado">

<!-- DoS (Billion Laughs) -->
<!ENTITY xxe SYSTEM "file:///dev/random">
```

---

## Prevencion C# / .NET

```csharp
// XmlReaderSettings seguro (.NET 10)
var settings = new XmlReaderSettings
{
    DtdProcessing = DtdProcessing.Prohibit,
    XmlResolver = null
};
using var reader = XmlReader.Create(stream, settings);

// XmlDocument seguro
var doc = new XmlDocument();
doc.XmlResolver = null;  // Deshabilitar resolucion de entidades externas
doc.Load(stream);
```

---

## Prevencion Java (5 Parsers)

### DocumentBuilderFactory

```java
DocumentBuilderFactory dbf = DocumentBuilderFactory.newInstance();
dbf.setFeature("http://xml.org/sax/features/external-general-entities", false);
dbf.setFeature("http://xml.org/sax/features/external-parameter-entities", false);
dbf.setFeature("http://apache.org/xml/features/nonvalidating/load-external-dtd", false);
dbf.setFeature("http://apache.org/xml/features/disallow-doctype-decl", true);
dbf.setFeature("http://javax.xml.XMLConstants/feature/secure-processing", true);
```

### TransformerFactory

```java
TransformerFactory tf = TransformerFactory.newInstance();
tf.setAttribute(XMLConstants.ACCESS_EXTERNAL_DTD, "");
tf.setAttribute(XMLConstants.ACCESS_EXTERNAL_STYLESHEET, "");
```

### XMLInputFactory (StAX)

```java
xmlInputFactory.setProperty(XMLInputFactory.SUPPORT_DTD, false);
xmlInputFactory.setProperty(XMLConstants.ACCESS_EXTERNAL_DTD, "");
xmlInputFactory.setProperty("javax.xml.stream.isSupportingExternalEntities", false);
```

### XMLReader (SAX)

```java
XMLReader reader = XMLReaderFactory.createXMLReader();
reader.setFeature("http://apache.org/xml/features/disallow-doctype-decl", true);
reader.setFeature("http://apache.org/xml/features/nonvalidating/load-external-dtd", false);
reader.setFeature("http://xml.org/sax/features/external-general-entities", false);
reader.setFeature("http://xml.org/sax/features/external-parameter-entities", false);
```

### SAXParserFactory

```java
SAXParserFactory spf = SAXParserFactory.newInstance();
spf.setFeature("http://apache.org/xml/features/disallow-doctype-decl", true);
spf.setFeature("http://xml.org/sax/features/external-general-entities", false);
```

---

## APIs Java Vulnerables a Revisar

```
javax.xml.parsers.DocumentBuilder
javax.xml.parsers.DocumentBuildFactory
org.xml.sax.EntityResolver
org.dom4j.*
javax.xml.parsers.SAXParser
javax.xml.parsers.SAXParserFactory
TransformerFactory
SAXReader, SAXBuilder, SAXParserFactory
XMLReaderFactory, XMLInputFactory
SchemaFactory, DocumentBuilderFactoryImpl
```

---

*Pattern v3.7.0*
