# Normalizacion Automatizada de Base de Datos

Aplicacion web para validar una plantilla Excel y preparar sus datos originales para el analisis de primera, segunda y tercera forma normal.

## Ejecutar el proyecto

```powershell
dotnet run --project .\src\NormalizacionAutomatizada.Web\NormalizacionAutomatizada.Web.csproj
```

Abre `http://127.0.0.1:5000` y descarga la plantilla maestra desde la pagina principal.

La pagina tambien incluye `Plantilla_Prueba_Depuracion_3_Tablas.xlsx`, un libro valido con tres pares de hojas y 11 registros para probar la importacion. Despues de una carga correcta se muestra una vista previa de la estructura, las claves marcadas y los primeros cinco registros de cada tabla.

## Flujo de carga

1. `PaginaInicioModel.OnPost` recibe el archivo en `ArchivoPlantilla`.
2. `ValidadorArchivoExcel.Validar` verifica que sea un archivo `.xlsx` con contenido.
3. `AnalizadorPlantillaExcel.Analizar` valida los pares `Tabla_XX` y `Datos_XX`, y convierte las filas de Excel a objetos del proyecto.
4. `AlmacenEntradaNormalizacionEnSesion.Guardar` conserva la entrada valida para el siguiente paso.

La misma pantalla recupera la entrada con `IAlmacenEntradaNormalizacion.Obtener()` para que la vista previa permanezca disponible mientras la sesion del navegador siga activa.

La estructura disponible despues de una carga correcta es:

```text
EntradaNormalizacion
  Tablas
    TablaNormalizacion
      Columnas
      Registros
        RegistroOriginal.Valores
```

## Para el siguiente integrante

Actualmente **no se usa una base de datos**. Los registros se guardan temporalmente en la sesion HTTP del mismo navegador durante 30 minutos. El acceso debe hacerse mediante la interfaz `IAlmacenEntradaNormalizacion`; no se debe leer la sesion por una clave de texto desde otro modulo.

```csharp
public class ServicioDeNormalizacion
{
    private readonly IAlmacenEntradaNormalizacion almacenEntrada;

    public ServicioDeNormalizacion(IAlmacenEntradaNormalizacion almacenEntrada)
    {
        this.almacenEntrada = almacenEntrada;
    }

    public EntradaNormalizacion ObtenerEntradaCargada()
    {
        return almacenEntrada.Obtener()
            ?? throw new InvalidOperationException("Primero debe cargarse una plantilla valida.");
    }
}
```

Para depurar la carga, coloca puntos de interrupcion en `PaginaInicioModel.OnPost`, `AnalizadorPlantillaExcel.Analizar` y `AlmacenEntradaNormalizacionEnSesion.Guardar`. Para revisar los datos en el siguiente modulo, usa `IAlmacenEntradaNormalizacion.Obtener()` y examina `entrada.Tablas`.

La informacion se pierde si vence la sesion, se reinicia la aplicacion o se usa otro navegador. Cuando el equipo decida persistir resultados, el nuevo almacenamiento debe implementar `IAlmacenEntradaNormalizacion` sin cambiar el analizador ni las paginas.

## Pruebas

```powershell
dotnet test .\NormalizacionAutomatizada.sln
```
