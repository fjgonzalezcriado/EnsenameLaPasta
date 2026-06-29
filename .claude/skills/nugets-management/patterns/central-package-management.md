# Central Package Management (CPM)

> Skill: nugets-management | Version: 3.1.0

Guia para gestionar versiones de paquetes NuGet de forma centralizada.

---

## Que es CPM

CPM centraliza las versiones de paquetes en un unico archivo `Directory.Packages.props`
en la raiz de la solucion. Los .csproj solo declaran el paquete, no la version.

## Activar CPM

```xml
<!-- Directory.Packages.props -->
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>
  <ItemGroup>
    <PackageVersion Include="Microsoft.EntityFrameworkCore" Version="9.0.*" />
    <PackageVersion Include="Serilog" Version="4.*" />
  </ItemGroup>
</Project>
```

## En .csproj (sin Version)

```xml
<PackageReference Include="Microsoft.EntityFrameworkCore" />
<PackageReference Include="Serilog" />
```

## Override por proyecto

```xml
<PackageReference Include="Newtonsoft.Json" VersionOverride="13.0.3" />
```

---

*Pattern v3.1.0*
